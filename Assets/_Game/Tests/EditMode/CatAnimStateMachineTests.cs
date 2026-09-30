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

        // Ruled 2026-09-30: the turn flips on the most symmetrical frame, holding it one frame (the presenter picks the frame).
        [Test]
        public void V07_Turn_HoldsOneFrame_ThenFlips()
        {
            var m = new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, 1f / 60f, FlipHysteresis));
            for (int i = 0; i < 3; i++) Ground(m, 1f);
            Assert.AreEqual(CatAnimState.Turn, Ground(m, -1f));
            Assert.AreEqual(1, m.Facing, "the hold frame still faces the old way");
            Assert.AreNotEqual(CatAnimState.Turn, Ground(m, -1f), "one frame only");
            Assert.AreEqual(-1, m.Facing);
        }

        // Ruled 2026-09-30: Walk↔Run switch at the matching foot position, not on the first frame over the threshold.
        [Test]
        public void V07_WalkToRun_WaitsForTheSwitchFrame()
        {
            var m = CreateV07();
            for (int i = 0; i < 3; i++) Ground(m, 1f);
            Assert.AreEqual(CatAnimState.Walk, m.Step(new CatAnimInput(true, false, 3f, 0f, F, gaitSwitchReady: false)), "over the threshold, paws don't match: Walk");
            Assert.AreEqual(CatAnimState.Walk, m.Step(new CatAnimInput(true, false, 3f, 0f, F, gaitSwitchReady: false)));
            Assert.AreEqual(CatAnimState.Run, m.Step(new CatAnimInput(true, false, 3f, 0f, F, gaitSwitchReady: true)), "the switch frame: Run");
        }

        [Test]
        public void V07_RunToWalk_WaitsForTheSwitchFrame_ButAStopDoesnt()
        {
            var m = CreateV07();
            for (int i = 0; i < 3; i++) Ground(m, 3f);
            Assert.AreEqual(CatAnimState.Run, m.State);
            Assert.AreEqual(CatAnimState.Run, m.Step(new CatAnimInput(true, false, 1.5f, 0f, F, gaitSwitchReady: false)), "below RunExit, paws don't match: Run");
            Assert.AreEqual(CatAnimState.Walk, m.Step(new CatAnimInput(true, false, 1.5f, 0f, F, gaitSwitchReady: true)));
            var stop = CreateV07();
            for (int i = 0; i < 3; i++) Ground(stop, 3f);
            Assert.AreNotEqual(CatAnimState.Run, stop.Step(new CatAnimInput(true, false, 0.05f, 0f, F, gaitSwitchReady: false)), "a stop doesn't wait");
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
            Assert.AreEqual(CatAnimState.Fall, Ground(m, 0f), "item 2: the first grounded frame keeps the air pose (contact)");
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

        // ---------- PAX-V07 item 2: the air rows (TakeOff, Rise / Apex / Fall, Land / HardLand) ----------

        const float HardLandDistance = 2.75f, TakeOffDuration = 2f / 60f, HardLandDuration = 3f / 12f, AirGrace = 0.03f;
        const float F = 1f / 60f;

        static CatAnimStateMachine CreateAir() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: 0.5f));

        // Touchdown: the first grounded frame keeps the air pose (its contact frame); returns the state on the frame after.
        static CatAnimState Touch(CatAnimStateMachine m, float speed, float height, float gravitySign = -1f, bool holding = false, float body = float.NaN)
        {
            CatAnimState contact = Frame(m, true, speed, 0f, height, gravitySign: gravitySign);
            Assert.That(contact is CatAnimState.Rise or CatAnimState.Apex or CatAnimState.Fall, "the contact frame keeps the air pose, was " + contact);
            return Frame(m, true, speed, 0f, height, gravitySign: gravitySign, holding: holding, body: body);
        }

        // One presentation frame. `height` is the drawn root's height against gravity; `alongGravity` > 0 = falling.
        static CatAnimState Frame(CatAnimStateMachine m, bool grounded, float speed, float alongGravity, float height,
            bool jumped = false, float gravitySign = -1f, float dt = F, bool holding = false, float body = float.NaN) =>
            m.Step(new CatAnimInput(grounded, false, speed, alongGravity, dt, jumped, gravitySign, height, holding, bodySurfaceSpeed: body));

        // A jump from standing at `floor`: the jump frame (grounded flag still set, JumpedThisStep), then the arc at 60 fps
        // with gravity 30 and jump speed 9.8, until the height comes back to `land`. Returns the states shown in the air.
        static System.Collections.Generic.List<CatAnimState> Jump(CatAnimStateMachine m, float floor, float land, float speed = 0f,
            float gravitySign = -1f, float jumpSpeed = 9.8f)
        {
            var seen = new System.Collections.Generic.List<CatAnimState>();
            for (int i = 0; i < 3; i++) Frame(m, true, speed, 0f, floor, gravitySign: gravitySign);
            float v = -jumpSpeed, h = floor;
            for (int n = 0; ; n++)
            {
                h -= v * F; v += 30f * F;
                if (n > 2 && h <= land && v > 0f) break;
                seen.Add(Frame(m, n == 0, speed, v, h, jumped: n < 2, gravitySign: gravitySign));
            }
            return seen;
        }

        [Test]
        public void Air_Jump_ShowsTakeOff_OnTheJumpFrame_ThenRise()
        {
            var m = CreateAir();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f);
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, true, 0f, -9.8f, 0.1f, jumped: true), "the jump frame");
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, false, 0f, -9.6f, 0.26f, jumped: true), "the clip's second frame");
            Assert.AreEqual(CatAnimState.Rise, Frame(m, false, 0f, -9.3f, 0.42f), "the clip ended: Rise");
        }

        [Test]
        public void Air_TakeOff_AlsoFromAMissedJumpFlag_AtALowFrameRate_ButAGeyserLaunchShowsRise()
        {
            var slow = CreateAir();
            Frame(slow, true, 0f, 0f, 0f, dt: 1f / 30f);
            Assert.AreEqual(CatAnimState.TakeOff, Frame(slow, false, 0f, -9.2f, 0.4f, dt: 1f / 30f),
                "two ticks in one frame: grounded last frame, rising above airThreshold now (R16)");
            var launched = CreateAir();
            Frame(launched, true, 0f, 0f, 0f);
            Assert.AreEqual(CatAnimState.Rise, Frame(launched, false, 0f, -14f, 0.23f),
                "at 60 fps every jump tick is seen: rising without a jump is a launch (geyser), which shows Rise");
        }

        [Test]
        public void Air_ApexBand_ShowsApex_BetweenRiseAndFall()
        {
            var m = CreateAir();
            var seen = Jump(m, 0f, 0f);
            int rise = seen.IndexOf(CatAnimState.Rise), apex = seen.IndexOf(CatAnimState.Apex), fall = seen.IndexOf(CatAnimState.Fall);
            Assert.That(rise >= 0 && apex > rise && fall > apex, "TakeOff, Rise, Apex, Fall in order: " + string.Join(",", seen));
            Assert.AreEqual(fall, seen.LastIndexOf(CatAnimState.Apex) + 1, "Apex shows only in the band, once");

            var band = CreateAir();
            Frame(band, false, 0f, -2f, 1f);
            Assert.AreEqual(CatAnimState.Rise, band.State);
            Assert.AreEqual(CatAnimState.Apex, Frame(band, false, 0f, -0.9f, 1.1f));
            Assert.AreEqual(CatAnimState.Apex, Frame(band, false, 0f, 0.9f, 1.1f));
            Assert.AreEqual(CatAnimState.Fall, Frame(band, false, 0f, 1.1f, 1.08f));
        }

        [Test]
        public void Air_WalkOffALedge_ShowsFall_NotApex_AfterTheGrace()
        {
            var m = CreateAir();
            for (int i = 0; i < 4; i++) Frame(m, true, 2f, 0f, 1f);
            Assert.AreEqual(CatAnimState.Walk, Frame(m, false, 2f, 0.6f, 1f - 0.01f), "just off the ledge: inside the grace");
            Assert.AreEqual(CatAnimState.Fall, Frame(m, false, 2f, 1.2f, 1f - 0.04f), "dropped past the grace: Fall (no Apex without a rise)");
            Assert.AreEqual(CatAnimState.Fall, Frame(m, false, 2f, 0.9f, 1f - 0.05f), "a walk-off never shows Apex");
        }

        [Test]
        public void Air_AOneTickGroundBlip_NeverFlashesAnAirState()
        {
            var m = CreateAir();
            for (int i = 0; i < 4; i++) Frame(m, true, 2f, 0f, 0f);
            Assert.AreEqual(CatAnimState.Walk, Frame(m, false, 2f, 0.6f, -0.006f));
            Assert.AreEqual(CatAnimState.Walk, Frame(m, false, 2f, 0.3f, -0.008f));
            Assert.AreEqual(CatAnimState.Walk, Frame(m, true, 2f, 0f, 0f), "back on the ground: no Land");
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void Air_LandOrHardLand_ByTheFallDistanceAlongGravity(float gravitySign)
        {
            var jump = CreateAir();
            Jump(jump, 0f, 0f, gravitySign: gravitySign);
            Assert.AreEqual(CatAnimState.Land, Touch(jump, 0f, 0f, gravitySign), "a normal jump's own fall (1.6 u) is a plain Land");
            Assert.AreEqual(1.6f, jump.LastFallDistance, 0.1f, "the arc here is integrated per frame: 1.68");

            var up = CreateAir();
            Jump(up, 0f, 1f, gravitySign: gravitySign);
            Assert.AreEqual(CatAnimState.Land, Touch(up, 0f, 1f, gravitySign), "a jump onto a 1 u ledge: 0.6 u fall");
            Assert.AreEqual(0.6f, up.LastFallDistance, 0.1f);

            var off = CreateAir();
            Jump(off, 1.2f, 0f, gravitySign: gravitySign);
            Assert.AreEqual(CatAnimState.HardLand, Touch(off, 0f, 0f, gravitySign), "a jump off a 1.2 u ledge: 2.8 u fall");
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void Air_HardLand_AtOrPastTheDistance_LandBelowIt(float gravitySign)
        {
            foreach ((float from, CatAnimState expect) in new[] { (2.74f, CatAnimState.Land), (2.75f, CatAnimState.HardLand), (5f, CatAnimState.HardLand) })
            {
                var m = CreateAir();
                for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, from, gravitySign: gravitySign);
                float h = from, v = 1.5f;   // pushed off a ledge: falling from the start
                while (h > 0f) { Frame(m, false, 0f, v, h, gravitySign: gravitySign); h -= v * F; v += 0.5f; }
                Assert.AreEqual(expect, Touch(m, 0f, 0f, gravitySign), $"a {from} u drop");
            }
        }

        [Test]
        public void Air_LedgeWalkOff_CountsFromTheLedgeHeight()
        {
            var m = CreateAir();
            for (int i = 0; i < 4; i++) Frame(m, true, 1f, 0f, 3f);
            float h = 3f, v = 0.3f;
            while (h > 0f) { Frame(m, false, 1f, v, h); h -= v * F; v += 0.5f; }
            Assert.AreEqual(CatAnimState.HardLand, Touch(m, 0.1f, 0f));
            Assert.AreEqual(3f, m.LastFallDistance, 0.05f);
        }

        [Test]
        public void Air_AGravityFlipInTheAir_ResetsTheHighPoint()
        {
            var m = CreateAir();
            var seen = Jump(m, 0f, 1.5f);   // up to 1.6, falling at 1.5 ...
            // ... gravity flips: heights are now measured against the new gravity (world y 1.5 -> -1.5), the high point resets
            float h = -1.5f, v = 0f;
            while (h > -3f) { Frame(m, false, 0f, v, h, gravitySign: 1f); h -= Mathf.Max(v, 0.5f) * F; v += 0.5f; }
            Assert.AreEqual(CatAnimState.Land, Touch(m, 0f, -3f, 1f),
                "1.5 u from the flip to the ceiling: Land (without the reset it would be 1.6 + 3 = 4.6 u, a HardLand)");
            Assert.AreEqual(1.5f, m.LastFallDistance, 0.05f);
        }

        // §3 (ruled 2026-09-30): any movement input cancels the landing animation on the same frame.
        [Test]
        public void Air_RunningLanding_GoesStraightToTheGait_NoLandCrouchSliding()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f, speed: 6f);
            Assert.AreEqual(CatAnimState.Run, Touch(m, 6f, 0f), "running on at touchdown: the motion implies Run");
        }

        [Test]
        public void Air_WalkingLanding_GoesStraightToWalk()
        {
            var walk = CreateAir();
            Jump(walk, 0f, 0f, speed: 1f);
            Assert.AreEqual(CatAnimState.Walk, Touch(walk, 1f, 0f), "moving on at touchdown: no Land (§3)");
        }

        [Test]
        public void Air_AMovePressedOnTheLandingTick_CancelsTheLanding_OnThatFrame()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Walk, Touch(m, 0f, 0f, body: 1.2f), "the body moves on the landing tick: Walk at once");
        }

        [Test]
        public void Air_AMovePressedDuringLand_CancelsItOnTheSameFrame()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Land, Touch(m, 0f, 0f));
            Assert.AreEqual(CatAnimState.Land, Frame(m, true, 0f, 0f, 0f));
            Assert.AreEqual(CatAnimState.Walk, Frame(m, true, 0f, 0f, 0f, body: 1.2f), "movement input: Land ends on this frame");
        }

        // A reversal pressed on the landing tick goes straight to Turn (no Land shown for one frame).
        [Test]
        public void Air_AReversalPressedOnTheLandingTick_Turns()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Turn, Touch(m, -0.3f, 0f, body: -1.2f));
        }

        // Released on touchdown (the body already slower than the drawn cat, braking), the landing skids in Land: no input.
        [Test]
        public void Air_ReleasedOnTouchdown_SkidsInLand_NoGaitStutter()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f, speed: 6f);
            Assert.AreEqual(CatAnimState.Land, Touch(m, 5.7f, 0f, body: 4.4f), "released: Land, not Run");
            foreach (float v in new[] { 4.4f, 2.8f, 1.2f, 0f, 0f })
                Assert.AreEqual(CatAnimState.Land, Frame(m, true, v, 0f, 0f, body: Mathf.Max(0f, v - 1.6f)), "the skid stays in Land (braking isn't acting)");
        }

        [Test]
        public void Air_HardLandingWhileRunning_GoesStraightToRun()
        {
            var m = CreateAir();
            for (int i = 0; i < 3; i++) Frame(m, true, 6f, 0f, 3.2f);
            float h = 3.2f, v = 1.5f;
            while (h > 0f) { Frame(m, false, 6f, v, h); h -= v * F; v += 0.5f; }
            Assert.AreEqual(CatAnimState.Run, Touch(m, 6f, 0f), "still running: the gait at once (§3)");
            Assert.GreaterOrEqual(m.LastFallDistance, HardLandDistance);
        }

        // The landing's length comes from the frame it enters on (the presenter matches the fall's last pose).
        [Test]
        public void Air_LandLasts_FromTheMatchedEntryFrame()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Frame(m, true, 0f, 0f, 0f);   // the contact frame keeps the air pose
            int frames = 0;
            while (m.Step(new CatAnimInput(true, false, 0f, 0f, F, landDuration: 3f / 60f)) == CatAnimState.Land) frames++;
            Assert.AreEqual(3, frames, 1, "three frames from the matched entry, not the whole clip");
        }

        // Round 2 (critic #1): the facing is decided at the jump from the body's motion, on the TakeOff's first frame; it never
        // mirrors later in the push. Both gravities.
        [TestCase(-1f)]
        [TestCase(1f)]
        public void Air_AJumpAgainstTheFacing_TurnsOnTheGather_NotMidPush(float gravitySign)
        {
            var m = CreateAir();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f, gravitySign: gravitySign);
            Assert.AreEqual(1, m.Facing);
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, true, 0f, -9.8f, 0.1f, jumped: true, gravitySign: gravitySign, body: -1.2f),
                "the drawn cat hasn't moved yet; the body has");
            Assert.AreEqual(-1, m.Facing, "turned on the TakeOff's first frame");
            Frame(m, false, -1.2f, -9.6f, 0.26f, jumped: true, gravitySign: gravitySign, body: -2.4f);
            Assert.AreEqual(-1, m.Facing);
        }

        [Test]
        public void Air_Land_PlaysItsDuration_ThenIdle()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Land, Touch(m, 0f, 0f));
            int frames = 1;
            while (Frame(m, true, 0f, 0f, 0f) == CatAnimState.Land) frames++;
            Assert.AreEqual(Mathf.RoundToInt(LandDuration * 60f), frames, 1);
            Assert.AreEqual(CatAnimState.Idle, m.State);
        }

        [Test]
        public void Air_HardLand_RecoversThroughLand_ThenIdle()
        {
            var m = CreateAir();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 4f);
            float h = 4f, v = 1.5f;
            while (h > 0f) { Frame(m, false, 0f, v, h); h -= v * F; v += 0.5f; }
            var seen = new System.Collections.Generic.List<CatAnimState> { Touch(m, 0f, 0f) };
            for (int i = 0; i < 60; i++) seen.Add(Frame(m, true, 0f, 0f, 0f));
            int hard = seen.LastIndexOf(CatAnimState.HardLand), land = seen.IndexOf(CatAnimState.Land), idle = seen.IndexOf(CatAnimState.Idle);
            Assert.AreEqual(0, seen.IndexOf(CatAnimState.HardLand));
            Assert.AreEqual(hard + 1, land, "HardLand's crouch recovers through Land");
            Assert.Greater(idle, land);
            Assert.AreEqual(Mathf.RoundToInt(HardLandDuration * 60f), hard + 1, 1);
        }

        [Test]
        public void Air_DeathDuringLand_WinsAtOnce()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Land, Touch(m, 0f, 0f));
            Assert.AreEqual(CatAnimState.Death, Frame(m, true, 0f, 0f, 0f, holding: true), "the hold (priority 2) beats Land (priority 9)");
        }

        [Test]
        public void Air_Acting_CancelsLandAndHardLand_OnTheSameFrame()
        {
            foreach (float from in new[] { 0f, 4f })   // Land, HardLand
            {
                CatAnimStateMachine Landed()
                {
                    var m = CreateAir();
                    for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, from);
                    float h = from, v = 1.5f;
                    if (from == 0f) Jump(m, 0f, 0f);
                    else while (h > 0f) { Frame(m, false, 0f, v, h); h -= v * F; v += 0.5f; }
                    CatAnimState landed = Touch(m, 0f, 0f);
                    Assert.AreEqual(from == 0f ? CatAnimState.Land : CatAnimState.HardLand, landed);
                    Frame(m, true, 0f, 0f, 0f);
                    return m;
                }
                Assert.AreEqual(CatAnimState.Walk, Frame(Landed(), true, 0.6f, 0f, 0f, body: 1.2f), "a move: the body already faster than drawn, so Walk on that frame");
                Assert.AreEqual(from == 0f ? CatAnimState.Land : CatAnimState.HardLand, Frame(Landed(), true, 0.3f, 0f, 0f, body: 0.3f),
                    "a slow drift with no input (body not leading) doesn't cancel");
                Assert.AreEqual(CatAnimState.TakeOff, Frame(Landed(), true, 0f, -9.8f, 0.1f, jumped: true), "a jump: TakeOff on that frame");
                CatAnimStateMachine turning = Landed();
                Assert.AreEqual(CatAnimState.Turn, Frame(turning, true, -1.2f, 0f, 0f, body: -2.4f), "a reversal: Turn on that frame");
            }
        }

        [Test]
        public void Air_TakeOff_EndsAtOnce_WhenTheMotionStopsRising()
        {
            var m = CreateAir();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f);
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, true, 0f, -9.8f, 0.1f, jumped: true));
            Assert.AreEqual(CatAnimState.Fall, Frame(m, false, 0f, 1.2f, 0.12f, jumped: true), "a head bump: the cat falls, so TakeOff gives way");
            var r = CreateAir();
            for (int i = 0; i < 3; i++) Frame(r, true, 0f, 0f, 0f);
            Frame(r, true, 0f, -9.8f, 0.1f, jumped: true, body: 0f);
            Frame(r, false, -0.5f, -9.6f, 0.26f, jumped: true);
            Assert.AreEqual(1, r.Facing, "round 2: no mirror mid-push; the facing was decided at the jump");
            Frame(r, false, -0.1f, -9.0f, 0.4f);
            Assert.AreEqual(1, r.Facing, "a drift below walkEnter in the air doesn't mirror the cat");
            Frame(r, false, -1.2f, -8.4f, 0.55f);
            Assert.AreEqual(-1, r.Facing, "the air motion clearly reversed (above walkEnter): the cat mirrors");
        }

        [Test]
        public void Air_AJumpOnTheTickAfterTouchdown_GoesStraightToTakeOff_NoOneFrameLand()
        {
            var m = CreateAir();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Fall, Frame(m, true, 0f, 0f, 0f), "contact");
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, true, 0f, -9.8f, 0.1f, jumped: true), "the jump on the next tick");
        }

        [Test]
        public void Air_RollingOffALedgesCorner_LeavesTheGroundPose_NoGrace()
        {
            var m = CreateAir();
            for (int i = 0; i < 4; i++) Frame(m, true, 2f, 0f, 1f);
            Assert.AreEqual(CatAnimState.Fall, m.Step(new CatAnimInput(false, false, 2f, 0.6f, F, false, -1f, 1f - 0.012f, rollingOff: true)),
                "the motor still grounded, the capsule rolling off the corner: the paws would draw into it, so no ground pose holds");
            var turning = CreateAir();
            Frame(turning, true, 1f, 0f, 0f);
            Frame(turning, true, 1f, 0f, 0f);
            Assert.AreEqual(CatAnimState.Turn, Frame(turning, true, -1f, 0f, 0f));
            Assert.AreEqual(CatAnimState.Walk, Frame(turning, false, -1f, 0.6f, -0.005f), "a ground blip during Turn: no Turn off the ground");
        }

        [Test]
        public void Air_NoAirStateWhileGrounded_AfterTheJumpFrame()
        {
            var m = CreateAir();
            for (int i = 0; i < 60; i++)
                Assert.That(Frame(m, true, i % 20 < 10 ? 2f : 0f, 0f, 0f) is not (CatAnimState.Rise or CatAnimState.Apex or CatAnimState.Fall or CatAnimState.TakeOff));
        }

        // ---------- PAX-V07 item 3: Climb / Hang (row 5), Leap (row 6), grab and release ----------

        // The motor's numbers: climb 4 u/s, jump 9.8 u/s at gravity 30; a leap is recognized between 6.9 and 12.7 u/s against
        // gravity (halfway from the climb speed to the jump speed, either side of the jump speed).
        const float ClimbSpeed = 4f, LeapMin = 6.9f, LeapMax = 12.7f, LeapDuration = 0.27f, ClimbStill = 0.02f;

        static CatAnimStateMachine CreateClimb() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: 0.5f,
                climbStillSpeed: ClimbStill, leapSpeedMin: LeapMin, leapSpeedMax: LeapMax, leapDuration: LeapDuration));

        // One frame on the vine, moving `upTheVine` u/s against gravity (negative: down the vine). `surface` is the drawn speed
        // along the surface (the grab's x snap shows there for a frame).
        static CatAnimState Vine(CatAnimStateMachine m, float upTheVine, float height = 1f, float gravitySign = -1f, float surface = 0f, bool holding = false) =>
            m.Step(new CatAnimInput(false, true, surface, -upTheVine, F, false, gravitySign, height, holding, bodySurfaceSpeed: 0f, bodyVelocityAlongGravity: -upTheVine));

        // One frame off the vine: `alongGravity` is the drawn velocity along gravity, `body` the body's (the motor's) after the tick.
        static CatAnimState Off(CatAnimStateMachine m, bool grounded, float alongGravity, float body, float height = 1f, float bodySurface = 0f,
            bool jumped = false, float gravitySign = -1f) =>
            m.Step(new CatAnimInput(grounded, false, bodySurface, alongGravity, F, jumped, gravitySign, height, bodySurfaceSpeed: bodySurface, bodyVelocityAlongGravity: body));

        static CatAnimStateMachine OnTheVine(float upTheVine = ClimbSpeed)
        {
            var m = CreateClimb();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f);
            for (int i = 0; i < 6; i++) Vine(m, upTheVine);
            return m;
        }

        [Test]
        public void Climb_MovingOnTheVine_ShowsClimb_StillShowsHang_AfterMinStateFrames()
        {
            var m = CreateClimb();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f);
            Assert.AreEqual(CatAnimState.Climb, Vine(m, ClimbSpeed), "the grab while climbing");
            for (int i = 0; i < 5; i++) Assert.AreEqual(CatAnimState.Climb, Vine(m, ClimbSpeed));
            Assert.AreEqual(CatAnimState.Climb, Vine(m, 0f), "the first still frame holds Climb (MinStateFrames 2)");
            Assert.AreEqual(CatAnimState.Hang, Vine(m, 0f), "still: Hang");
            Assert.AreEqual(CatAnimState.Hang, Vine(m, 0f));
            Assert.AreEqual(CatAnimState.Climb, Vine(m, 0.1f), "moving again climbs at once");
        }

        [Test]
        public void Climb_TheSlowestAnalogClimb_StillShowsClimb()
        {
            // The stick just past the 0.35 dead zone: Climb 0.015 x 4 u/s.
            var m = OnTheVine(0.06f);
            Assert.AreEqual(CatAnimState.Climb, Vine(m, 0.06f));
        }

        [Test]
        public void Climb_AReversalThroughZero_NeverFlashesHang()
        {
            var m = OnTheVine();
            Assert.AreEqual(CatAnimState.Climb, Vine(m, 0f), "the drawn speed passes through zero on one frame");
            Assert.AreEqual(CatAnimState.Climb, Vine(m, -ClimbSpeed));
            Assert.AreEqual(CatAnimState.Climb, Vine(m, -ClimbSpeed));
        }

        [Test]
        public void Climb_GrabbingWithoutMoving_ShowsHang()
        {
            var m = CreateClimb();
            Frame(m, false, 0f, 3f, 2f);
            Frame(m, false, 0f, 3f, 1.95f);
            Assert.AreEqual(CatAnimState.Hang, Vine(m, 0f), "a grab that doesn't move (e.g. at the vine's top) hangs");
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void Climb_DownTheVine_PlaysInReverse(float gravitySign)
        {
            var m = CreateClimb();
            Vine(m, ClimbSpeed, gravitySign: gravitySign);
            Assert.AreEqual(1, m.ClimbDirection, "up the vine (against gravity): forward");
            Vine(m, -ClimbSpeed, gravitySign: gravitySign);
            Assert.AreEqual(-1, m.ClimbDirection, "down the vine (along gravity): reversed");
            Vine(m, 0f, gravitySign: gravitySign);
            Vine(m, 0f, gravitySign: gravitySign);
            Assert.AreEqual(CatAnimState.Hang, m.State);
            Assert.AreEqual(-1, m.ClimbDirection, "hanging keeps the last direction");
        }

        [Test]
        public void Climb_PlayedByDistance_DownTheVine_RunsTheFramesBackwards()
        {
            // The presenter advances the Climb clip by the distance climbed (signed: down the vine is negative), so climbing
            // down shows the frames in reverse order with the paws still planted.
            var clip = new Parallax.Gameplay.Presentation.CatClip("Climb", CatAnimState.Climb, new Sprite[5], 21f, true,
                new[] { .1f, .1f, .1f, .1f, .1f }, null);
            float phase = clip.DistanceAtFrame(2) + .05f;
            var shown = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 4; i++) { phase -= .1f; shown.Add(clip.FrameAtDistance(phase)); }
            CollectionAssert.AreEqual(new[] { 1, 0, 4, 3 }, shown);
        }

        [Test]
        public void Climb_TheFacingHolds_OnTheVine_ThroughTheGrabsSnap()
        {
            var m = CreateClimb();
            for (int i = 0; i < 3; i++) Frame(m, true, 2f, 0f, 0f, body: 2f);
            Assert.AreEqual(1, m.Facing);
            Vine(m, ClimbSpeed, surface: -15f);   // the grab tick snaps x to the vine's centre: a one-frame drawn surface speed
            Vine(m, ClimbSpeed);
            Assert.AreEqual(1, m.Facing, "the snap is not a reversal");
        }

        [Test]
        public void Leap_ClimbingEndsWithTheLaunch_ShowsLeap_ThenTheAirRows()
        {
            var m = OnTheVine();
            float v = -9.8f, h = 3f, t = 0f;
            Assert.AreEqual(CatAnimState.Leap, Off(m, false, v * .6f, v, h, 6f), "the launch frame (the drawn speed still trails the body)");
            var seen = new System.Collections.Generic.List<CatAnimState>();
            for (int i = 0; i < 40; i++)
            {
                v += 30f * F; h -= v * F; t += F;
                seen.Add(Off(m, false, v, v, h, 6f));
            }
            int leapFrames = seen.FindIndex(s => s != CatAnimState.Leap) + 1;
            Assert.AreEqual(Mathf.CeilToInt(LeapDuration / F - 1e-3f), leapFrames, "Leap shows for its duration");
            CatAnimState after = seen[leapFrames - 1];
            Assert.That(after is CatAnimState.Rise or CatAnimState.Apex, "then the air rows by velocity, was " + after);
            Assert.AreEqual(CatAnimState.Fall, seen[^1]);
        }

        [Test]
        public void Leap_AlsoOnJumpedThisStep()
        {
            var m = OnTheVine();
            Assert.AreEqual(CatAnimState.Leap, Off(m, false, -3f, -3f, jumped: true));
        }

        [TestCase(-1f)]
        [TestCase(1f)]
        public void Leap_AgainstTheFacing_TurnsAtTheLeap(float gravitySign)
        {
            var m = CreateClimb();
            for (int i = 0; i < 3; i++) Frame(m, true, 2f, 0f, 0f, gravitySign: gravitySign, body: 2f);
            for (int i = 0; i < 4; i++) Vine(m, ClimbSpeed, gravitySign: gravitySign);
            Assert.AreEqual(1, m.Facing);
            Off(m, false, -5f, -9.8f, bodySurface: -6f, gravitySign: gravitySign);
            Assert.AreEqual(CatAnimState.Leap, m.State);
            Assert.AreEqual(-1, m.Facing, "the leap's own direction, on its first frame");
        }

        [Test]
        public void Release_InTheAir_ShowsTheAirRows_NotLeap()
        {
            var down = OnTheVine(-ClimbSpeed);
            Assert.AreEqual(CatAnimState.Fall, Off(down, false, ClimbSpeed, ClimbSpeed), "climbing down past the vine's end: Fall");
            var snapped = OnTheVine(ClimbSpeed);
            Assert.AreEqual(CatAnimState.Rise, Off(snapped, false, -ClimbSpeed, -ClimbSpeed), "a snap while climbing up keeps the climb speed: Rise, not Leap");
            var still = OnTheVine(0f);
            Assert.AreEqual(CatAnimState.Fall, Off(still, false, .2f, .6f), "a release from Hang: Fall (not Apex)");
        }

        [Test]
        public void Release_AGeyserLaunchOffTheVine_IsNotALeap()
        {
            var m = OnTheVine();
            Assert.AreEqual(CatAnimState.Rise, Off(m, false, -8f, -14f), "a launch faster than the jump band shows Rise (§2)");
        }

        [Test]
        public void Release_OntoTheGround_ShowsIdle_NoLandNoLeap()
        {
            var m = OnTheVine(-ClimbSpeed);
            CatAnimState first = Off(m, true, 0f, 0f, 0f);
            Assert.AreEqual(CatAnimState.Idle, first);
            Assert.AreEqual(CatAnimState.Idle, Off(m, true, 0f, 0f, 0f));
        }

        [Test]
        public void Climb_BeatsLeap_ARegrabDuringTheLeap_Climbs()
        {
            var m = OnTheVine();
            Off(m, false, -9f, -9.8f, 3f, 6f);
            Assert.AreEqual(CatAnimState.Leap, Off(m, false, -9.3f, -9.3f, 3.15f, 6f));
            Assert.AreEqual(CatAnimState.Climb, Vine(m, ClimbSpeed, 3.2f));
        }

        [Test]
        public void Climb_DeathOnTheVine_ShowsDeath()
        {
            var m = OnTheVine();
            Assert.AreEqual(CatAnimState.Death, Vine(m, ClimbSpeed, holding: true));
        }

        [Test]
        public void Climb_Leap_LandingFromTheLeap_ShowsLand()
        {
            var m = OnTheVine();
            Off(m, false, -9.8f, -9.8f, 1f, 6f);
            Off(m, false, -9f, -9f, 1.15f, 6f);
            Assert.AreEqual(CatAnimState.Leap, m.State);
            Off(m, true, 0f, 0f, 0.2f, 6f);   // the contact frame keeps the air pose
            Assert.That(Off(m, true, 0f, 0f, 0.2f, 0f) is CatAnimState.Land or CatAnimState.HardLand, "a leap lands like any air state");
        }

        [Test]
        public void Legacy_Step_Climbing_NeverShowsHangOrLeap()
        {
            var machine = Create();
            Assert.AreEqual(CatAnimState.Climb, machine.Step(true, false, 0f, 0f, 0.02f));
            Assert.AreEqual(CatAnimState.Rise, machine.Step(false, false, 0f, -9.8f, 0.02f));
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

        // ---------- PAX-V07 item 4: Flip (the tucked roll on a gravity flip) ----------

        const float FlipDuration = 4f / 12f;

        static CatAnimStateMachine CreateFlip() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: 0.5f, flipDuration: FlipDuration));

        [TestCase(-1f, 1f)]
        [TestCase(1f, -1f)]
        public void Flip_OnAGravitySignChange_PlaysTheRoll_ThenTheMotionsState(float from, float to)
        {
            var m = CreateFlip();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f, gravitySign: from);
            Assert.AreEqual(CatAnimState.Flip, Frame(m, false, 0f, 0.5f, 0f, gravitySign: to), "the frame gravity flips: Flip");
            int frames = 1;
            CatAnimState s;
            while ((s = Frame(m, false, 0f, 3f, -0.05f * frames, gravitySign: to)) == CatAnimState.Flip) frames++;
            Assert.AreEqual(Mathf.RoundToInt(FlipDuration * 60f), frames, 1, "the roll's length");
            Assert.AreEqual(CatAnimState.Fall, s, "then the motion's state: falling toward the new ground");
        }

        [Test]
        public void Flip_BeatsClimb()
        {
            var m = CreateFlip();
            for (int i = 0; i < 3; i++) m.Step(new CatAnimInput(false, true, 0f, -4f, F, gravitySign: -1f));
            Assert.AreEqual(CatAnimState.Climb, m.State);
            Assert.AreEqual(CatAnimState.Flip, m.Step(new CatAnimInput(false, true, 0f, 4f, F, gravitySign: 1f)));
        }

        [Test]
        public void Flip_NotAfterAReset_ARespawnSnapsGravityWithoutARoll()
        {
            var m = CreateFlip();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f, gravitySign: 1f);
            m.Reset();
            Assert.AreNotEqual(CatAnimState.Flip, Frame(m, true, 0f, 0f, 0f, gravitySign: -1f));
        }

        [Test]
        public void Flip_EndingOnTheGround_Lands()
        {
            var m = CreateFlip();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f, gravitySign: -1f);
            Assert.AreEqual(CatAnimState.Flip, Frame(m, false, 0f, 0.5f, 0f, gravitySign: 1f));
            for (int i = 1; i < 10; i++) Frame(m, false, 0f, 3f, -0.2f * i, gravitySign: 1f);   // falls 1.8 u to the ceiling
            CatAnimState s = Frame(m, true, 0f, 0f, -2f, gravitySign: 1f);
            while (s == CatAnimState.Flip || s == CatAnimState.Fall) s = Frame(m, true, 0f, 0f, -2f, gravitySign: 1f);
            Assert.AreEqual(CatAnimState.Land, s, "a 2 u fall after the flip: Land");
        }

        // ---------- PAX-V07 item 5: idle fidgets (look around -> ear twitch -> sit down) ----------

        const float FidgetDelay = 3f;
        static readonly float[] FidgetDurations = { 8f / 12f, 8f / 12f, 8f / 12f };   // look, ear, sit (sit then holds)

        static CatAnimStateMachine CreateFidget() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: 0.5f, flipDuration: FlipDuration, fidgetDelay: FidgetDelay, fidgetDurations: FidgetDurations));

        static CatAnimState Still(CatAnimStateMachine m) => Frame(m, true, 0f, 0f, 0f, body: 0f);

        // Stands still until the next fidget starts; returns how many frames that took.
        static int UntilFidget(CatAnimStateMachine m)
        {
            int n = 0;
            while (Still(m) != CatAnimState.IdleFidget) { n++; Assert.Less(n, 2000, "no fidget"); }
            return n;
        }

        [Test]
        public void Fidget_NotBeforeTheDelay()
        {
            var m = CreateFidget();
            for (int i = 0; i < Mathf.FloorToInt(FidgetDelay * 60f) - 2; i++) Assert.AreEqual(CatAnimState.Idle, Still(m), $"frame {i}");
        }

        [Test]
        public void Fidget_TheFixedCycle_LookEarSit_TheSitHolds()
        {
            var m = CreateFidget();
            UntilFidget(m);
            Assert.AreEqual(0, m.FidgetIndex, "first: look around");
            int look = 1; while (Still(m) == CatAnimState.IdleFidget) look++;
            Assert.AreEqual(Mathf.RoundToInt(FidgetDurations[0] * 60f), look, 1, "look around plays its clip");
            Assert.AreEqual(CatAnimState.Idle, m.State, "back to Idle: the timer starts again");
            Assert.AreEqual(Mathf.RoundToInt(FidgetDelay * 60f), UntilFidget(m) + 1, 2, "the next fidget after the delay again");
            Assert.AreEqual(1, m.FidgetIndex, "second: ear twitch");
            while (Still(m) == CatAnimState.IdleFidget) { }
            UntilFidget(m);
            Assert.AreEqual(2, m.FidgetIndex, "third: sit down");
            for (int i = 0; i < 600; i++) Assert.AreEqual(CatAnimState.IdleFidget, Still(m), "the sit holds until input");
        }

        [Test]
        public void Fidget_AnyInputCancelsIt_OnTheSameFrame_AndTheCycleRestarts()
        {
            var m = CreateFidget();
            UntilFidget(m);
            Assert.AreEqual(CatAnimState.Walk, Frame(m, true, 0f, 0f, 0f, body: 1.2f), "a move: the body moves, Walk on that frame");
            for (int i = 0; i < 5; i++) Frame(m, true, 0f, 0f, 0f, body: 0f);
            UntilFidget(m);
            Assert.AreEqual(0, m.FidgetIndex, "movement resets the cycle: look around again");
            Assert.AreEqual(CatAnimState.TakeOff, Frame(m, true, 0f, -9.8f, 0.1f, jumped: true), "a jump cancels it on that frame");
        }

        [Test]
        public void Fidget_TheSitEndsOnInput()
        {
            var m = CreateFidget();
            for (int k = 0; k < 3; k++) { UntilFidget(m); if (k < 2) while (Still(m) == CatAnimState.IdleFidget) { } }
            Assert.AreEqual(2, m.FidgetIndex);
            for (int i = 0; i < 120; i++) Still(m);
            Assert.AreEqual(CatAnimState.Walk, Frame(m, true, 0f, 0f, 0f, body: 1.2f), "touching the stick while seated ends the sit at once");
        }

        [Test]
        public void Fidget_ALandingResetsTheCycle()
        {
            var m = CreateFidget();
            UntilFidget(m);
            while (Still(m) == CatAnimState.IdleFidget) { }
            Jump(m, 0f, 0f);
            Touch(m, 0f, 0f);
            while (Still(m) != CatAnimState.Idle) { }
            UntilFidget(m);
            Assert.AreEqual(0, m.FidgetIndex, "after a landing the cycle starts with look around");
        }

        // ---------- PAX-V07 items 6 and 7: Death, Respawn, Door ----------

        const float RespawnDuration = 4f / 20f;

        static CatAnimStateMachine CreateLife() =>
            new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2,
                hardLandDistance: HardLandDistance, takeOffDuration: TakeOffDuration, hardLandDuration: HardLandDuration, airGraceDrop: AirGrace,
                speedLeadTolerance: 0.5f, flipDuration: FlipDuration, fidgetDelay: FidgetDelay, fidgetDurations: FidgetDurations,
                respawnDuration: RespawnDuration));

        static CatAnimState Life(CatAnimStateMachine m, bool holding = false, bool respawned = false, bool complete = false, float body = 0f, float gravitySign = -1f) =>
            m.Step(new CatAnimInput(true, false, 0f, 0f, F, false, gravitySign, 0f, holding, bodySurfaceSpeed: body, respawned: respawned, levelComplete: complete));

        [Test]
        public void Death_DuringLand_BeatsIt()
        {
            var m = CreateLife();
            Jump(m, 0f, 0f);
            Assert.AreEqual(CatAnimState.Land, Touch(m, 0f, 0f));
            Assert.AreEqual(CatAnimState.Death, Life(m, holding: true));
        }

        [Test]
        public void Respawn_AfterTheHold_PlaysItsClip_ThenIdle()
        {
            var m = CreateLife();
            for (int i = 0; i < 10; i++) Life(m, holding: true);
            Assert.AreEqual(CatAnimState.Respawn, Life(m, respawned: true));
            int frames = 1;
            while (Life(m) == CatAnimState.Respawn) frames++;
            Assert.AreEqual(Mathf.RoundToInt(RespawnDuration * 60f), frames, 1);
            Assert.AreEqual(CatAnimState.Idle, m.State);
        }

        [Test]
        public void Respawn_AnyInputEndsIt_OnThatFrame()
        {
            var m = CreateLife();
            Assert.AreEqual(CatAnimState.Respawn, Life(m, respawned: true));
            Assert.AreNotEqual(CatAnimState.Respawn, Life(m, body: 1.2f), "a move ends the Respawn clip");
        }

        [Test]
        public void Respawn_ThatSnapsGravity_NeverRolls()
        {
            var m = CreateLife();
            for (int i = 0; i < 3; i++) Life(m, gravitySign: 1f);
            Assert.AreEqual(CatAnimState.Respawn, Life(m, respawned: true, gravitySign: -1f));
            for (int i = 0; i < 20; i++) Assert.AreNotEqual(CatAnimState.Flip, Life(m, gravitySign: -1f));
        }

        [Test]
        public void Door_OnLevelComplete_BeatsDeath_AndHolds()
        {
            var m = CreateLife();
            Assert.AreEqual(CatAnimState.Death, Life(m, holding: true));
            Assert.AreEqual(CatAnimState.Door, Life(m, holding: true, complete: true), "level complete during Death: Door");
            for (int i = 0; i < 300; i++) Assert.AreEqual(CatAnimState.Door, Life(m, body: i % 2 == 0 ? 0f : 1.2f), "Door never leaves");
        }

        // ---------- final critic fixes ----------

        // The body turns 180 degrees with gravity: the facing flips with it so the cat keeps facing the same way on screen.
        [TestCase(-1f, 1f)]
        [TestCase(1f, -1f)]
        public void Flip_KeepsTheCatFacingTheSameWayOnScreen(float from, float to)
        {
            var m = CreateFlip();
            for (int i = 0; i < 3; i++) Frame(m, true, 0f, 0f, 0f, gravitySign: from);
            int before = m.Facing;
            Frame(m, false, 0f, 0.5f, 0f, gravitySign: to);
            Assert.AreEqual(-before, m.Facing, "local facing flips with the body, so the screen facing holds");
        }

        // A reversal at a run (the motor accelerating the other way, not its release braking) keeps the gallop into the Turn: no
        // Walk flashes between two runs.
        [Test]
        public void RunReversal_KeepsTheGallop_IntoTheTurn()
        {
            var m = new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, 1f / 60f, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2));
            for (int i = 0; i < 5; i++) Ground(m, 6f);
            var seen = new System.Collections.Generic.List<CatAnimState>();
            for (float v = 6f; v > -3f; v -= 1f)
                seen.Add(m.Step(new CatAnimInput(true, false, v, 0f, F, gaitSwitchReady: false, stopping: false)));
            CollectionAssert.DoesNotContain(seen.GetRange(0, seen.IndexOf(CatAnimState.Turn)), CatAnimState.Walk, string.Join(",", seen));
        }

        [Test]
        public void RunStop_StillWalksTheBrakeOut()
        {
            var m = CreateV07();
            var m2 = new CatAnimStateMachine(new CatAnimSettings(RiseExit, FallEnter, WalkEnter, WalkExit, LandDuration,
                RunEnter, RunExit, TurnDuration, FlipHysteresis, snapAcceleration: 20f, minStateFrames: 2));
            for (int i = 0; i < 5; i++) Ground(m2, 6f);
            Assert.AreEqual(CatAnimState.Walk, m2.Step(new CatAnimInput(true, false, 4.4f, 0f, F, stopping: true)), "a stop: Run hands over to Walk");
        }
    }
}
