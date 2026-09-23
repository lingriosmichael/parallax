using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-079 (D-077): JumpWindows and TickTime.ToWholeTicks at the helper level. The
    /// equivalence test pins the new int logic to a copy of the float logic CatMotor2D ran before
    /// PAX-079, at the committed step, with the shipped 0.10 s / 0.12 s windows.</summary>
    public sealed class JumpWindowsTests
    {
        // Unity 6's stored Fixed Timestep, 2822399/141120000 s (D-075 (4)).
        const float CommittedStep = 2822399f / 141120000f;
        const float OldCoyoteSeconds = .10f, OldBufferSeconds = .12f;
        const int CoyoteTicks = 5, BufferTicks = 6;

        [TearDown] public void TearDown() => TickTime.ResetToDefault();

        // Pre-PAX-079 CatMotor2D, timer logic only: SetCommand (:50-53), the countdowns (:110-111),
        // and the check and jump (:119-123).
        sealed class FloatModel
        {
            float coyoteTimer, jumpBufferTimer;

            public bool Step(bool grounded, bool pressed, float dt)
            {
                if (pressed) jumpBufferTimer = OldBufferSeconds;
                coyoteTimer = grounded ? OldCoyoteSeconds : Mathf.Max(0f, coyoteTimer - dt);
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
                if (jumpBufferTimer > 0f && coyoteTimer > 0f)
                {
                    jumpBufferTimer = 0f;
                    coyoteTimer = 0f;
                    return true;
                }
                return false;
            }
        }

        // Index of the first step where the two disagree, or -1.
        static int FirstDifference(IReadOnlyList<(bool grounded, bool pressed)> sequence, int coyoteTicks, int bufferTicks)
        {
            var model = new FloatModel();
            int coyote = 0, buffer = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                bool expected = model.Step(sequence[i].grounded, sequence[i].pressed, CommittedStep);
                bool actual = JumpWindows.Step(ref coyote, ref buffer, sequence[i].grounded, sequence[i].pressed, coyoteTicks, bufferTicks);
                if (expected != actual) return i;
            }
            return -1;
        }

        // One token per step: G/A = grounded/airborne, P = pressed.
        static string Describe(IReadOnlyList<(bool grounded, bool pressed)> sequence, int step) =>
            string.Join(" ", sequence.Select(s => (s.grounded ? "G" : "A") + (s.pressed ? "P" : "."))) + $" (first difference at step {step})";

        // ---------- R3 ----------

        [Test]
        public void JumpWindows_MatchesFloatModel_AtCommittedStep()
        {
            var sequence = new (bool, bool)[8];
            for (int bits = 0; bits < 1 << 16; bits++)
            {
                for (int i = 0; i < 8; i++) sequence[i] = (((bits >> (2 * i)) & 1) == 1, ((bits >> (2 * i + 1)) & 1) == 1);
                int step = FirstDifference(sequence, CoyoteTicks, BufferTicks);
                if (step >= 0) Assert.Fail("JumpWindows differs from the float model: " + Describe(sequence, step));
            }

            var random = new System.Random(79);
            var longSequence = new (bool, bool)[60];
            for (int n = 0; n < 2000; n++)
            {
                for (int i = 0; i < longSequence.Length; i++) longSequence[i] = (random.Next(2) == 1, random.Next(2) == 1);
                int step = FirstDifference(longSequence, CoyoteTicks, BufferTicks);
                if (step >= 0) Assert.Fail($"JumpWindows differs from the float model on random sequence {n}: " + Describe(longSequence, step));
            }
        }

        // ---------- R6 ----------

        [Test]
        public void ZeroCoyote_GroundJumpAllowed_FirstAirborneStepRefused()
        {
            int coyote = 0, buffer = 0;

            Assert.IsTrue(JumpWindows.Step(ref coyote, ref buffer, grounded: true, pressed: true, 0, BufferTicks),
                "with 0 coyote ticks a grounded press must still jump");
            Assert.IsFalse(JumpWindows.Step(ref coyote, ref buffer, grounded: true, pressed: false, 0, BufferTicks));
            Assert.IsFalse(JumpWindows.Step(ref coyote, ref buffer, grounded: false, pressed: true, 0, BufferTicks),
                "with 0 coyote ticks the first airborne step must refuse a jump");
        }

        // ---------- R4 ----------

        [Test]
        public void ToWholeTicks_SameAtBothSteps_ForEveryHundredthUpToOneSecond()
        {
            var differences = new StringBuilder();
            for (int k = 0; k <= 100; k++)
            {
                float seconds = (float)(k / 100.0);
                TickTime.SecondsPerTickSource = () => .02f;
                int atExact = TickTime.ToWholeTicks(seconds);
                TickTime.SecondsPerTickSource = () => CommittedStep;
                int atCommitted = TickTime.ToWholeTicks(seconds);
                if (atExact != atCommitted) differences.Append($" {seconds:0.00} s: {atExact} at 0.02f, {atCommitted} at the committed step;");
            }

            Assert.IsEmpty(differences.ToString(), "ToWholeTicks depends on the step:" + differences);
        }

        [TestCase(50f, .10f, 5)]
        [TestCase(50f, .12f, 6)]
        [TestCase(60f, .10f, 6)]
        [TestCase(60f, .12f, 7)]
        public void ToWholeTicks_FollowsConfigAndRate(float hz, float seconds, int expectedTicks)
        {
            TickTime.SecondsPerTickSource = () => 1f / hz;

            Assert.AreEqual(expectedTicks, TickTime.ToWholeTicks(seconds));
        }
    }
}
