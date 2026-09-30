using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class CatAnimStateMachineTests
    {
        const float RiseExit = 1f;
        const float FallEnter = 1f;
        const float WalkEnter = 0.15f;
        const float WalkExit = 0.1f;
        const float LandDuration = 0.12f;

        [Test]
        public void StartsIdle()
        {
            Assert.AreEqual(CatAnimState.Idle, Create().State);
        }

        [Test]
        public void AirborneAboveRiseThreshold_IsRise()
        {
            var machine = Create();

            CatAnimState state = machine.Step(false, 0f, -1.01f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, state);
        }

        [Test]
        public void AirborneAboveFallThreshold_IsFall()
        {
            var machine = Create();

            CatAnimState state = machine.Step(false, 0f, 1.01f, 0.02f);

            Assert.AreEqual(CatAnimState.Fall, state);
        }

        [Test]
        public void ApexBand_HoldsRiseUntilFallThresholdIsCrossed()
        {
            var machine = Create();
            machine.Step(false, 0f, -2f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -1f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, 1f, 0.02f));
            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, 1.01f, 0.02f));
        }

        [Test]
        public void ApexBand_HoldsFallWithoutFlickeringBackToRise()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);

            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 0f, -1f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -1.01f, 0.02f));
        }

        [Test]
        public void FirstAirTickInsideApexBand_StartsFall()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Fall, machine.Step(false, 2f, -0.5f, 0.02f));
        }

        [Test]
        public void AirToGround_EntersLand()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 0f, 0f, 0.02f));
        }

        [Test]
        public void Land_HoldsForConfiguredDuration()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 0f, 0f, 0.06f));
            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0f, 0f, 0.061f));
        }

        [Test]
        public void Land_IsNotInterruptedByMovement()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Land, machine.Step(true, 10f, 0f, 0.06f));
        }

        [Test]
        public void LeavingGround_InterruptsLandImmediately()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, 0f, -2f, 0.02f));
        }

        [Test]
        public void GroundedSpeedAboveWalkEnter_EntersWalk()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, 0.151f, 0f, 0.02f));
        }

        [Test]
        public void GroundedSpeedBelowWalkExit_EntersIdle()
        {
            var machine = Create();
            machine.Step(true, 1f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0.099f, 0f, 0.02f));
        }

        [Test]
        public void WalkHysteresis_HoldsCurrentGroundStateInsideBand()
        {
            var idleMachine = Create();
            var walkingMachine = Create();
            walkingMachine.Step(true, 1f, 0f, 0.02f);

            Assert.AreEqual(CatAnimState.Idle, idleMachine.Step(true, 0.125f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Walk, walkingMachine.Step(true, 0.125f, 0f, 0.02f));
        }

        [Test]
        public void SurfaceSpeed_IsTreatedAsAbsolute()
        {
            var machine = Create();

            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, -1f, 0f, 0.02f));
        }

        [Test]
        public void SameInputSequence_ProducesSameStates()
        {
            var first = Create();
            var second = Create();
            var inputs = new[]
            {
                new Input(true, 0f, 0f, 0.02f),
                new Input(true, 1f, 0f, 0.02f),
                new Input(false, 1f, -3f, 0.02f),
                new Input(false, 1f, 0f, 0.02f),
                new Input(false, 1f, 3f, 0.02f),
                new Input(true, 1f, 0f, 0.02f),
                new Input(true, 1f, 0f, 0.12f),
            };

            foreach (Input input in inputs)
            {
                CatAnimState firstState = first.Step(
                    input.Grounded,
                    input.SurfaceSpeed,
                    input.GravityVelocity,
                    input.Dt);
                CatAnimState secondState = second.Step(
                    input.Grounded,
                    input.SurfaceSpeed,
                    input.GravityVelocity,
                    input.Dt);

                Assert.AreEqual(firstState, secondState);
            }
        }

        [Test]
        public void Reset_ClearsLandingTimerAndUsesRequestedState()
        {
            var machine = Create();
            machine.Step(false, 0f, 2f, 0.02f);
            machine.Step(true, 0f, 0f, 0.02f);

            machine.Reset(CatAnimState.Walk);

            Assert.AreEqual(CatAnimState.Walk, machine.State);
            Assert.AreEqual(CatAnimState.Idle, machine.Step(true, 0f, 0f, 0.02f));
        }

        // ---------- PAX-V07 item 1: the new table, ground states (Step(in CatAnimInput)) ----------

        const float RunEnter = 2.4f, RunExit = 1.8f, TurnDuration = 0.1f, FlipHysteresis = 0.05f;

        static CatAnimStateMachine CreateV07() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis));

        static CatAnimState Ground(CatAnimStateMachine m, float speed, float dt = 1f / 60f) =>
            m.Step(new CatAnimInput(true, false, speed, 0f, dt));

        static CatAnimState Air(CatAnimStateMachine m, float speed, float alongGravity, float dt = 1f / 60f) =>
            m.Step(new CatAnimInput(false, false, speed, alongGravity, dt));

        [Test]
        public void V07_StartsIdle_FacingRight()
        {
            var m = CreateV07();
            Assert.AreEqual(CatAnimState.Idle, m.State);
            Assert.AreEqual(1, m.Facing);
        }

        [Test]
        public void V07_IdleWalkThresholds_WithHysteresis()
        {
            var idle = CreateV07();
            Assert.AreEqual(CatAnimState.Idle, Ground(idle, 0.125f), "inside the band from Idle stays Idle");
            var m = CreateV07();
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 0.151f));
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 0.125f), "inside the band from Walk stays Walk");
            Assert.AreEqual(CatAnimState.Idle, Ground(m, 0.099f));
        }

        [Test]
        public void V07_Run_EntersAtRunEnter()
        {
            var m = CreateV07();
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 2.39f));
            Assert.AreEqual(CatAnimState.Run, Ground(m, 2.4f));
            Assert.AreEqual(CatAnimState.Run, Ground(m, 6f));
        }

        [Test]
        public void V07_RunWalk_Hysteresis()
        {
            var m = CreateV07();
            Ground(m, 6f);
            Assert.AreEqual(CatAnimState.Run, Ground(m, 2.0f), "inside the band from Run stays Run");
            Assert.AreEqual(CatAnimState.Run, Ground(m, 1.8f), "at runExit still Run");
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 1.79f));
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 2.0f), "inside the band from Walk stays Walk");
            Assert.AreEqual(CatAnimState.Run, Ground(m, 2.4f));
        }

        [Test]
        public void V07_Run_ToIdle_WhenTheSpeedCollapses()
        {
            var m = CreateV07();
            Ground(m, 6f);
            Assert.AreEqual(CatAnimState.Idle, Ground(m, 0f));
        }

        [Test]
        public void V07_Run_IsSymmetric_InDirection()
        {
            var m = CreateV07();
            Ground(m, -0.5f);   // a reversal from rest at walk speed: a Turn
            for (int i = 0; i < 20; i++) Ground(m, -0.5f);
            Assert.AreEqual(-1, m.Facing);
            Assert.AreEqual(CatAnimState.Run, Ground(m, -6f));
        }

        [Test]
        public void V07_Turn_OnGroundReversal_FlipsTheFacingAtItsEnd()
        {
            var m = CreateV07();
            Ground(m, 1f);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -0.5f, 0.02f));
            Assert.AreEqual(1, m.Facing, "the facing holds while Turn plays");
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -1f, 0.05f));
            Assert.AreEqual(1, m.Facing);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -1.5f, 0.04f), "0.07 s shown before this frame");
            Assert.AreEqual(1, m.Facing);
            Assert.AreEqual(CatAnimState.Walk, Ground(m, -1.5f, 0.02f), "Turn ends once it has shown its duration");
            Assert.AreEqual(-1, m.Facing, "the facing flipped at the end of Turn");
        }

        [Test]
        public void V07_Turn_AtRunSpeed()
        {
            var m = CreateV07();
            Ground(m, 6f);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -3f));
            for (int i = 0; i < 8; i++) Ground(m, -6f);
            Assert.AreEqual(CatAnimState.Run, m.State);
            Assert.AreEqual(-1, m.Facing);
        }

        [Test]
        public void V07_Turn_OnlyAboveWalkEnter()
        {
            var m = CreateV07();
            Assert.AreEqual(CatAnimState.Idle, Ground(m, -0.12f), "a creep backward below walkEnter is no Turn");
            Assert.AreEqual(1, m.Facing);
        }

        [Test]
        public void V07_NoTurnInTheAir_FacingFlipsAtOnce()
        {
            var m = CreateV07();
            Ground(m, 3f);
            CatAnimState s = Air(m, -3f, -5f);
            Assert.AreNotEqual(CatAnimState.Turn, s);
            Assert.AreEqual(CatAnimState.Rise, s);
            Assert.AreEqual(-1, m.Facing, "in the air the facing flips at once");
        }

        [Test]
        public void V07_Turn_InterruptedByTakingOff_FlipsAtOnce()
        {
            var m = CreateV07();
            Ground(m, 1f);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -1f));
            Assert.AreEqual(CatAnimState.Rise, Air(m, -1f, -5f));
            Assert.AreEqual(-1, m.Facing, "the pending flip applied when another state interrupted Turn");
        }

        [Test]
        public void V07_Turn_ReversedBackDuringTheTurn_KeepsTheFacing()
        {
            var m = CreateV07();
            Ground(m, 1f);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -1f));
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 1f), "moving the old way again ends the Turn");
            Assert.AreEqual(1, m.Facing);
        }

        [Test]
        public void V07_QuickDoubleTurn_EndsFacingTheMotion()
        {
            var m = CreateV07();
            Ground(m, 6f);
            for (int i = 0; i < 9; i++) Ground(m, -6f);   // 0.15 s left: a whole Turn, then Run left
            Assert.AreEqual(-1, m.Facing);
            for (int i = 0; i < 9; i++) Ground(m, 6f);    // and back
            Assert.AreEqual(1, m.Facing);
            Assert.AreEqual(CatAnimState.Run, m.State);
        }

        [Test]
        public void V07_Land_StillFollowsAFall()
        {
            var m = CreateV07();
            Air(m, 0f, 3f);
            Assert.AreEqual(CatAnimState.Fall, m.State);
            Assert.AreEqual(CatAnimState.Land, Ground(m, 0f));
        }

        [Test]
        public void V07_Climbing_ShowsClimb()
        {
            var m = CreateV07();
            Assert.AreEqual(CatAnimState.Climb, m.Step(new CatAnimInput(false, true, 0f, -4f, 1f / 60f)));
        }

        [Test]
        public void V07_SameInputSequence_ProducesSameStatesAndFacing()
        {
            var a = CreateV07();
            var b = CreateV07();
            float[] speeds = { 0f, 0.5f, 3f, 6f, -2f, -6f, -6f, -1f, 0f, 2.5f, 2f, 1.7f, 0.05f };
            foreach (float v in speeds)
            {
                Assert.AreEqual(Ground(a, v), Ground(b, v));
                Assert.AreEqual(a.Facing, b.Facing);
            }
        }

        static CatAnimStateMachine CreateSnap(int minFrames = 1) =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: minFrames));

        [Test]
        public void V07_HardStopFromRun_WalksTheBrakeOut_ThenIdle()
        {
            var m = CreateSnap();
            for (int i = 0; i < 5; i++) Ground(m, 6f);
            Assert.IsFalse(m.Braking);
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 4.8f), "braking hard: Run hands over to Walk at once");
            Assert.IsTrue(m.Braking);
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 3.2f), "above runEnter, still walking the brake out");
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 1.4f));
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 0f), "the frame the body stops: still braking");
            Assert.AreEqual(CatAnimState.Idle, Ground(m, 0f), "stopped: Idle");
        }

        [Test]
        public void V07_HardReversalFromRun_WalksToTheTurn_NoIdleFlash()
        {
            var m = CreateSnap(minFrames: 2);
            for (int i = 0; i < 5; i++) Ground(m, 6f);
            var seen = new System.Collections.Generic.List<CatAnimState>();
            foreach (float v in new[] { 4.4f, 2.4f, 0.4f, 0f, -1.2f, -2.4f })
                seen.Add(Ground(m, v));
            CollectionAssert.DoesNotContain(seen, CatAnimState.Idle);
            Assert.AreEqual(CatAnimState.Turn, seen[4]);
        }

        [Test]
        public void V07_QuickDoubleTurn_MeetsOnAWalkFrame_NoIdleBetweenTheTurns()
        {
            var m = CreateSnap(minFrames: 2);
            Ground(m, 1f);
            var seen = new System.Collections.Generic.List<CatAnimState>();
            float[] speeds = { -0.5f, -1f, -1f, -1f, -1f, -1f, -1f, -0.3f, 0f, 0.3f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
            foreach (float v in speeds) seen.Add(Ground(m, v));
            CollectionAssert.DoesNotContain(seen, CatAnimState.Idle);
            Assert.AreEqual(1, m.Facing);
        }

        [Test]
        public void V07_MinStateFrames_NoGroundStateShowsForASingleFrame()
        {
            var m = CreateSnap(minFrames: 2);
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 1.2f));
            Assert.AreEqual(CatAnimState.Walk, Ground(m, 2.6f), "Walk has shown one frame: it holds a second");
            Assert.AreEqual(CatAnimState.Run, Ground(m, 3.6f));
            Assert.AreEqual(CatAnimState.Rise, Air(m, 3.6f, -5f), "leaving the ground is never held back");
        }

        [Test]
        public void V07_GentleSlowdown_StillPassesThroughWalk()
        {
            var m = CreateSnap();
            Ground(m, 3f);
            float v = 3f;
            bool walked = false;
            while (v > 0f) { v -= 0.05f; walked |= Ground(m, Mathf.Max(0f, v)) == CatAnimState.Walk; }   // 3 u/s²
            Assert.IsTrue(walked);
        }

        [Test]
        public void Legacy_Step_NeverShowsRunOrTurn()
        {
            var machine = Create();
            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, 6f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Walk, machine.Step(true, -6f, 0f, 0.02f));
        }

        static CatAnimStateMachine Create() =>
            new CatAnimStateMachine(
                RiseExit,
                FallEnter,
                WalkEnter,
                WalkExit,
                LandDuration);

        readonly struct Input
        {
            public Input(bool grounded, float surfaceSpeed, float gravityVelocity, float dt)
            {
                Grounded = grounded;
                SurfaceSpeed = surfaceSpeed;
                GravityVelocity = gravityVelocity;
                Dt = dt;
            }

            public bool Grounded { get; }
            public float SurfaceSpeed { get; }
            public float GravityVelocity { get; }
            public float Dt { get; }
        }
    }
}
