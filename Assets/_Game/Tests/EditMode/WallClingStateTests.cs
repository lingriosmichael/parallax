using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110 and its amendment): the wall-cling rules, pure (WallClingState). A face is (collider, side): side +1 a
    // wall on the cat's right, -1 on its left.
    public sealed class WallClingStateTests
    {
        static readonly WallClingState.Face Right = new(1, 1), Left = new(2, -1), OtherSideOfRight = new(1, -1);
        const int Buffer = WallClingState.DefaultGrabBufferTicks;

        // One step: a press (or not), then a latch attempt on `face`, then the step's end.
        static bool TryStep(WallClingState s, bool press, WallClingState.Face face, bool rising = false)
        {
            s.BeginStep(press, Buffer);
            bool latched = s.CanLatch(face, rising);
            if (latched) s.Latch(face);
            s.EndStep();
            return latched;
        }

        static WallClingState Clinging(WallClingState.Face face)
        {
            var s = new WallClingState();
            Assert.IsTrue(TryStep(s, true, face));
            return s;
        }

        [Test]
        public void WithoutAGrabPress_NoFaceLatches_AndTheStateIsIdle()
        {
            var s = new WallClingState();
            Assert.IsTrue(s.Idle);
            for (int i = 0; i < 20; i++) Assert.IsFalse(TryStep(s, false, Right));
            Assert.IsTrue(s.Idle);
        }

        [Test]
        public void AGrabPress_LatchesAFaceTouchedWhileNotRising_AndStartsLatchMode()
        {
            WallClingState s = Clinging(Right);
            Assert.IsTrue(s.IsClinging);
            Assert.IsTrue(s.LatchMode);
            Assert.AreEqual(1, s.Current.Side);
            Assert.AreEqual(0, s.GrabBuffer, "the latch uses the press up");
        }

        [Test]
        public void NoLatch_WhileRising()
        {
            var s = new WallClingState();
            Assert.IsFalse(TryStep(s, true, Right, rising: true));
            Assert.IsFalse(s.IsClinging);
        }

        // The press is honoured on its own step and the next five (as the jump buffer), so a press made while still rising
        // latches at the top of the rise if that comes within the buffer.
        [Test]
        public void APress_IsRemembered_ForItsOwnStepAndTheNextFive()
        {
            for (int late = 0; late <= Buffer; late++)
            {
                var s = new WallClingState();
                s.BeginStep(true, Buffer); s.EndStep();   // the press, rising
                for (int i = 1; i < late; i++) { s.BeginStep(false, Buffer); s.EndStep(); }
                bool latched = late == 0 || TryStep(s, false, Right);
                if (late == 0) continue;
                Assert.AreEqual(late < Buffer, latched, $"a face first reached {late} steps after the press");
            }
        }

        [Test]
        public void InLatchMode_TheNextFaceLatches_WithoutAPress()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(launchHeight: 2f, moveLockTicks: 8);
            Assert.IsFalse(s.IsClinging);
            Assert.IsTrue(s.LatchMode, "a wall jump keeps latch mode");
            Assert.IsTrue(TryStep(s, false, Left), "the facing wall latches without a press");
            Assert.AreEqual(-1, s.Current.Side);
        }

        [Test]
        public void TheFaceJustLeft_NeverRelatches_UntilLandingOrADifferentFace()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(2f, 8);
            Assert.IsFalse(TryStep(s, true, Right), "the same face, even with a press");
            Assert.IsTrue(TryStep(s, false, OtherSideOfRight), "the other side of the same collider is a different face");
            s.WallJump(3f, 8);
            Assert.IsTrue(TryStep(s, false, Right), "after a different face, the first one latches again");

            WallClingState t = Clinging(Right);
            t.Release(endLatchMode: true);
            Assert.IsFalse(TryStep(t, true, Right), "locked after a push-away");
            t.Landed();
            Assert.IsTrue(TryStep(t, true, Right), "landing clears the lock");
        }

        [Test]
        public void LatchMode_EndsAfterAWallJump_OnTheFirstFallingStepBelowTheLaunchHeight()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(launchHeight: 2f, moveLockTicks: 8);
            s.AirStep(3.5f, falling: false);
            s.AirStep(2.5f, falling: true);
            s.AirStep(2f, falling: true);
            Assert.IsTrue(s.LatchMode, "still at or above the launch height");
            s.AirStep(1.99f, falling: false);
            Assert.IsTrue(s.LatchMode, "below it but not falling");
            s.AirStep(1.99f, falling: true);
            Assert.IsFalse(s.LatchMode, "ran out of walls");
            Assert.IsFalse(TryStep(s, false, Left), "a face now needs a new press");
            Assert.IsTrue(TryStep(s, true, Left));
        }

        [Test]
        public void LatchMode_EndsOnLandingAndOnEveryRelease_ButNotOnAWallJump()
        {
            WallClingState a = Clinging(Right);
            a.Landed();
            Assert.IsFalse(a.LatchMode || a.IsClinging, "landing");

            WallClingState b = Clinging(Right);
            b.Release(endLatchMode: true);
            Assert.IsFalse(b.LatchMode || b.IsClinging, "a push-away, sliding off, a moving face, a launch or a push");

            WallClingState c = Clinging(Right);
            c.GravityUp();
            Assert.IsFalse(c.LatchMode || c.IsClinging, "a flip");

            WallClingState d = Clinging(Right);
            d.WallJump(2f, 8);
            Assert.IsTrue(d.LatchMode && !d.IsClinging, "a wall jump");
        }

        [Test]
        public void Clear_ForgetsTheCling_ThePress_TheLatchMode_AndTheLock()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(2f, 8);
            s.BeginStep(true, Buffer);
            s.Clear();
            Assert.IsTrue(s.Idle);
            Assert.IsFalse(s.IsLocked(Right));
            Assert.IsFalse(TryStep(s, false, Right), "no press remembered");
            Assert.IsTrue(TryStep(s, true, Right), "and no lock");
        }

        [Test]
        public void GravityUp_DropsARememberedPress()
        {
            var s = new WallClingState();
            s.BeginStep(true, Buffer);
            s.GravityUp();
            s.EndStep();
            Assert.IsFalse(TryStep(s, false, Right));
        }

        [Test]
        public void TheMoveLock_LastsItsTicks_AgainstTheFaceJustLeft()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(2f, 8);
            Assert.AreEqual(1, s.MoveLockSide);
            for (int i = 0; i < 8; i++) Assert.IsTrue(s.ConsumeMoveLock(), $"step {i + 1} after the wall jump");
            Assert.IsFalse(s.ConsumeMoveLock(), "the ninth");
            Assert.AreEqual(0, s.MoveLock);
            Assert.AreEqual(0f, WallClingState.LockedMove(1f, 1), "toward the right face: ignored");
            Assert.AreEqual(-1f, WallClingState.LockedMove(-1f, 1), "away: kept");
            Assert.AreEqual(1f, WallClingState.LockedMove(1f, -1), "away from a left face: kept");
        }

        [Test]
        public void TheSlide_CapsTheFall_AndAFasterFallIsCutAtOnce()
        {
            float g = 30f, dt = .02f;
            Assert.AreEqual(.6f, WallClingState.SlideFall(0f, g, dt, 2f), 1e-5f, "from the top of a rise: normal gravity");
            Assert.AreEqual(2f, WallClingState.SlideFall(1.9f, g, dt, 2f), 1e-5f);
            Assert.AreEqual(2f, WallClingState.SlideFall(15f, g, dt, 2f), 1e-5f, "a fast fall onto a wall");
        }

        [Test]
        public void TheWallJump_IsTheJumpLaunchUp_PlusTheSideSpeedAwayFromTheFace()
        {
            Assert.AreEqual(new Vector2(-6f, 9.8f), WallClingState.WallJumpVelocity(1, 6f, 9.8f), "off a right wall: left");
            Assert.AreEqual(new Vector2(6f, 9.8f), WallClingState.WallJumpVelocity(-1, 6f, 9.8f), "off a left wall: right");
        }

        [Test]
        public void PushingAway_ReleasesAtTheThreshold_OnlyAwayFromTheFace()
        {
            Assert.IsTrue(WallClingState.PushesAway(-.5f, 1, .5f));
            Assert.IsFalse(WallClingState.PushesAway(-.49f, 1, .5f));
            Assert.IsFalse(WallClingState.PushesAway(1f, 1, .5f), "toward the face");
            Assert.IsTrue(WallClingState.PushesAway(1f, -1, .5f));
        }

        [Test]
        public void AWallNormal_IsWithinTheThresholdOfHorizontal_AndGivesTheFacesSide()
        {
            Assert.IsTrue(WallClingState.IsWallNormal(new Vector2(-1f, 0f), .9f));
            Assert.IsTrue(WallClingState.IsWallNormal(new Vector2(.9f, .43f), .9f));
            Assert.IsFalse(WallClingState.IsWallNormal(new Vector2(.7f, .7f), .9f), "a corner");
            Assert.IsFalse(WallClingState.IsWallNormal(new Vector2(0f, 1f), .9f), "a floor");
            Assert.AreEqual(1, WallClingState.SideOf(new Vector2(-1f, 0f)), "a normal pointing left: the wall is on the right");
            Assert.AreEqual(-1, WallClingState.SideOf(new Vector2(1f, 0f)));
        }
    }
}
