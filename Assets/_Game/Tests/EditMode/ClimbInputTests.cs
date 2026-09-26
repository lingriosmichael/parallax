using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-087 (D-089) §5, R1, R2: the Climb axis on every input source: the stick's y (VirtualStick.ToClimb), the keyboard's
    // key mapping, the router's merge, and the climb animation state.
    public sealed class ClimbInputTests
    {
        const float DeadZone = VirtualStick.DefaultClimbDeadZone;

        // ---------- the stick (R2) ----------

        [Test] public void ToClimb_IsZeroInsideTheDeadZone_AndAtItsEdge()
        {
            Assert.AreEqual(0f, VirtualStick.ToClimb(new Vector2(0f, .35f), DeadZone));
            Assert.AreEqual(0f, VirtualStick.ToClimb(new Vector2(1f, 0f), DeadZone), "a flat run never climbs");
            Assert.AreEqual(0f, VirtualStick.ToClimb(new Vector2(.94f, -.34f), DeadZone));
        }

        [Test] public void ToClimb_Rescales_EdgeToZero_FullToOne_ScreenUpPositive()
        {
            Assert.AreEqual(1f, VirtualStick.ToClimb(Vector2.up, DeadZone), 1e-6f);
            Assert.AreEqual(-1f, VirtualStick.ToClimb(Vector2.down, DeadZone), 1e-6f);
            Assert.AreEqual(.5f, VirtualStick.ToClimb(new Vector2(0f, .675f), DeadZone), 1e-5f, "(0.675 - 0.35) / 0.65");
            Assert.AreEqual(-.5f, VirtualStick.ToClimb(new Vector2(0f, -.675f), DeadZone), 1e-5f);
        }

        // Full push at every angle above horizontal (0.1° steps) through the real Evaluate: the smallest angle that reaches the
        // 0.5 grab threshold. R2's stop condition: nothing within 30° of horizontal grabs.
        [Test]
        public void AngleSweep_AFullPush_ReachesTheGrabThresholdOnlyAboveThirty()
        {
            const float radius = 100f;
            float smallest = -1f;
            for (int tenth = 0; tenth <= 900; tenth++)
            {
                float angle = tenth * .1f;
                Vector2 finger = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                Vector2 stick = VirtualStick.Evaluate(Vector2.zero, finger, radius, .15f);
                float climb = VirtualStick.ToClimb(stick, DeadZone);
                if (climb >= ClimbState.DefaultGrabThreshold) { smallest = angle; break; }
            }
            TestContext.Out.WriteLine($"PAX-087 smallest full-push angle that grabs: {smallest:F1}° above horizontal");
            Assert.Greater(smallest, 30f, "STOP (R2): a push within 30° of horizontal grabs");
            Assert.AreEqual(Mathf.Asin(.675f) * Mathf.Rad2Deg, smallest, .1f, "asin(0.675)");
        }

        [Test] public void ADiagonalRun_WithinTwentyDegrees_NeverClimbsAtAll_AtFullPush()
        {
            for (int angle = 0; angle <= 20; angle++)
            {
                Vector2 stick = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Assert.AreEqual(0f, VirtualStick.ToClimb(stick, DeadZone), $"{angle}°: inside the climb dead zone");
            }
        }

        // ---------- the keyboard (R1) ----------

        [Test] public void Axis_MapsTwoOpposingKeys()
        {
            Assert.AreEqual(0f, KeyboardCatInput.Axis(false, false));
            Assert.AreEqual(-1f, KeyboardCatInput.Axis(true, false));
            Assert.AreEqual(1f, KeyboardCatInput.Axis(false, true));
            Assert.AreEqual(0f, KeyboardCatInput.Axis(true, true), "both held: nothing");
        }

        [Test] public void Keyboard_ReadsClimb_AndResetClearsIt()
        {
            var go = new GameObject("KeyboardClimb");
            try
            {
                KeyboardCatInput keyboard = go.AddComponent<KeyboardCatInput>();
                PauseTestRig.SetPrivate(keyboard, "climb", 1f);
                Assert.AreEqual(1f, keyboard.Read().Climb);
                keyboard.ResetTransientState();
                Assert.AreEqual(0f, keyboard.Read().Climb);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ---------- the router ----------

        sealed class Fixed : ICatCommandSource
        {
            public CatCommand Command;
            public CatCommand Read() => Command;
            public void ResetTransientState() { }
        }

        [Test] public void Router_MergesClimbLikeMove_TheLargestMagnitudeWins()
        {
            var go = new GameObject("RouterClimb");
            try
            {
                CatInputRouter router = go.AddComponent<CatInputRouter>();
                var sources = PauseTestRig.GetPrivate<List<ICatCommandSource>>(router, "validSources");
                sources.Add(new Fixed { Command = new CatCommand { Move = 1f, Climb = .3f } });
                sources.Add(new Fixed { Command = new CatCommand { Move = .2f, Climb = -.8f } });
                CatCommand merged = router.Read();
                Assert.AreEqual(1f, merged.Move);
                Assert.AreEqual(-.8f, merged.Climb, "Climb is chosen on its own, not with the Move winner");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test] public void CatCommandNone_HasNoClimb() => Assert.AreEqual(0f, CatCommand.None.Climb);

        // ---------- the climb pose (§2.5) ----------

        [Test] public void AnimState_ClimbWhileClimbing_ThenTheOrdinaryRules()
        {
            var machine = new CatAnimStateMachine(1f, 1f, .5f, .2f, .1f);
            Assert.AreEqual(CatAnimState.Climb, machine.Step(true, false, 0f, -4f, .02f));
            Assert.AreEqual(CatAnimState.Climb, machine.Step(true, true, 3f, 0f, .02f), "climbing wins over ground and speed");
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, false, 6f, -9.8f, .02f), "a leap rises");
            machine.Step(true, false, 0f, 0f, .02f);
            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, false, 0f, 0f, .02f), "a release in the apex band falls");
            machine.Step(true, false, 0f, 0f, .02f);
            Assert.AreEqual(CatAnimState.Idle, machine.Step(false, true, 0f, 0f, .02f), "on the ground: Idle");
        }

        [Test] public void AnimState_ClimbIsAppended_NoRenumbering()
        {
            Assert.AreEqual(0, (int)CatAnimState.Idle);
            Assert.AreEqual(4, (int)CatAnimState.Land);
            Assert.AreEqual(5, (int)CatAnimState.Climb);
        }
    }
}
