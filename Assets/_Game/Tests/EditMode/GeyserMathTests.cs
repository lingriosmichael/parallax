using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-086 (D-088) §5: the geyser's cycle (idle -> tell -> erupt -> idle, from room start plus phase), the launch
    // velocity and the discrete rise, as pure functions.
    public sealed class GeyserMathTests
    {
        const float Dt = .02f;

        [Test]
        public void ThePhase_BeforeTheFirstFire_IsIdle()
        {
            for (int t = 0; t < 30; t++) Assert.AreEqual(GeyserPhase.Idle, GeyserMath.PhaseAt(t, 100, 30, 25, 40), $"t{t}");
        }

        [Test]
        public void ThePhase_IsTellThenEruptThenIdle_AtExactTicks_FromThePhaseOffset()
        {
            // period 100, phase 30, tell 25, erupt 40: tell 30-54, erupt 55-94, idle 95-129, tell again from 130.
            Assert.AreEqual(GeyserPhase.Tell, GeyserMath.PhaseAt(30, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Tell, GeyserMath.PhaseAt(54, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Erupt, GeyserMath.PhaseAt(55, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Erupt, GeyserMath.PhaseAt(94, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Idle, GeyserMath.PhaseAt(95, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Idle, GeyserMath.PhaseAt(129, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Tell, GeyserMath.PhaseAt(130, 100, 30, 25, 40));
            Assert.AreEqual(GeyserPhase.Erupt, GeyserMath.PhaseAt(155, 100, 30, 25, 40));
        }

        [Test]
        public void PhaseZero_StartsTheTellOnRoomTickZero()
        {
            Assert.AreEqual(GeyserPhase.Tell, GeyserMath.PhaseAt(0, 50, 0, 6, 1));
            Assert.AreEqual(GeyserPhase.Erupt, GeyserMath.PhaseAt(6, 50, 0, 6, 1));
            Assert.AreEqual(GeyserPhase.Idle, GeyserMath.PhaseAt(7, 50, 0, 6, 1));
            Assert.AreEqual(GeyserPhase.Tell, GeyserMath.PhaseAt(50, 50, 0, 6, 1));
        }

        // The runtime reads the phase from ticks since the latest fire (TrapTiming's Periodic fire is the tell's first tick);
        // over several periods both forms agree tick for tick.
        [Test]
        public void PhaseSince_AgreesWithPhaseAt_ThroughTrapTimingsPeriodicFires()
        {
            var timing = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Periodic, 0, 0, 100, 30);
            for (int t = 0; t < 400; t++)
            {
                timing.Step(t, false);
                int since = timing.LatestFireTick < 0 ? -1 : t - timing.LatestFireTick;
                Assert.AreEqual(GeyserMath.PhaseAt(t, 100, 30, 25, 40), GeyserMath.PhaseSince(since, 25, 40), $"t{t}");
            }
            Assert.AreEqual(GeyserPhase.Idle, GeyserMath.PhaseSince(-1, 25, 40), "never fired");
        }

        [Test]
        public void Push_IsUpOrDown()
        {
            Assert.AreEqual(Vector2.up, GeyserMath.Push(GeyserDirection.Up));
            Assert.AreEqual(Vector2.down, GeyserMath.Push(GeyserDirection.Down));
        }

        // Absolute, not added: the component along the push becomes the speed whatever it was; the cross component stays.
        [TestCase(0f, 0f)]
        [TestCase(4f, -9f)]
        [TestCase(-6f, 20f)]
        public void Launch_SetsTheComponentAlongTheDirection_AndKeepsTheCrossComponent(float vx, float vy)
        {
            Vector2 up = GeyserMath.Launch(new Vector2(vx, vy), Vector2.up, 14f);
            Assert.AreEqual(vx, up.x, 1e-5f); Assert.AreEqual(14f, up.y, 1e-5f);
            Vector2 down = GeyserMath.Launch(new Vector2(vx, vy), Vector2.down, 14f);
            Assert.AreEqual(vx, down.x, 1e-5f); Assert.AreEqual(-14f, down.y, 1e-5f);
        }

        // The cat is pushed on every tick its collider starts inside the column: bottoms 0, 0.28, ..., 1.40 < 1.5.
        [Test]
        public void PushTicks_ForTheDefaultColumn_IsSix()
        {
            Assert.AreEqual(6, GeyserMath.PushTicks(1.5f, 14f, Dt));
            Assert.AreEqual(1, GeyserMath.PushTicks(.1f, 14f, Dt));
        }

        // Q5 (as ruled): after the last push the motor adds g x dt before each move, so the rise is the sum of
        // (v - k g dt) dt over the ticks that still rise: 23 ticks, 3.128 u (the continuous v^2/2g is 3.27).
        [Test]
        public void Rise_IsTheDiscreteSum_TwentyThreeTicks_ThreePointOneTwoEight()
        {
            float rise = GeyserMath.Rise(14f, 30f, Dt, out int ticks);
            Assert.AreEqual(23, ticks);
            Assert.AreEqual(3.128f, rise, 1e-3f);
            Assert.Less(rise, 14f * 14f / 60f);
        }
    }
}
