using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110 and its amendment): the wall-cling rules, pure (WallClingState). A face is (collider, side): side +1 a
    // wall on the cat's right, -1 on its left. PAX-106 (D-110 amendment 4): the latch is automatic; no Grab press, buffer or
    // latch mode.
    public sealed class WallClingStateTests
    {
        static readonly WallClingState.Face Right = new(1, 1), Left = new(2, -1), OtherSideOfRight = new(1, -1);

        // One step: a latch attempt on `face`.
        static bool TryStep(WallClingState s, WallClingState.Face face, bool rising = false)
        {
            bool latched = s.CanLatch(face, rising);
            if (latched) s.Latch(face);
            return latched;
        }

        static WallClingState Clinging(WallClingState.Face face)
        {
            var s = new WallClingState();
            Assert.IsTrue(TryStep(s, face));
            return s;
        }

        [Test]
        public void ANewState_IsIdle()
        {
            var s = new WallClingState();
            Assert.IsTrue(s.Idle);
            Assert.IsFalse(s.IsClinging);
        }

        [Test]
        public void AFaceTouchedWhileNotRising_LatchesWithoutAnyInput()
        {
            WallClingState s = Clinging(Right);
            Assert.IsTrue(s.IsClinging);
            Assert.AreEqual(1, s.Current.Side);
            Assert.IsFalse(s.Idle);
        }

        [Test]
        public void NoLatch_WhileRising()
        {
            var s = new WallClingState();
            Assert.IsFalse(TryStep(s, Right, rising: true));
            Assert.IsFalse(s.IsClinging);
            Assert.IsTrue(TryStep(s, Right), "at the top of the rise it latches");
        }

        [Test]
        public void AfterAWallJump_TheFacingWallLatches()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(moveLockTicks: 8);
            Assert.IsFalse(s.IsClinging);
            Assert.IsTrue(TryStep(s, Left), "the facing wall");
            Assert.AreEqual(-1, s.Current.Side);
        }

        [Test]
        public void TheFaceJustLeft_NeverRelatches_UntilLandingOrADifferentFace()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(8);
            Assert.IsFalse(TryStep(s, Right), "the same face");
            Assert.IsTrue(TryStep(s, OtherSideOfRight), "the other side of the same collider is a different face");
            s.WallJump(8);
            Assert.IsTrue(TryStep(s, Right), "after a different face, the first one latches again");

            WallClingState t = Clinging(Right);
            t.Release();
            Assert.IsFalse(TryStep(t, Right), "locked after a push-away");
            t.Landed();
            Assert.IsTrue(TryStep(t, Right), "landing clears the lock");
        }

        [Test]
        public void EveryReleaseCause_LetsGoAndLocksTheFace()
        {
            WallClingState a = Clinging(Right);
            a.Landed();
            Assert.IsFalse(a.IsClinging, "landing");
            Assert.IsFalse(a.IsLocked(Right), "landing clears the lock");

            WallClingState b = Clinging(Right);
            b.Release();
            Assert.IsFalse(b.IsClinging, "a push-away, sliding off, a moving face, a launch or a push");
            Assert.IsTrue(b.IsLocked(Right));

            WallClingState c = Clinging(Right);
            c.GravityUp();
            Assert.IsFalse(c.IsClinging, "a flip");
            Assert.IsTrue(c.IsLocked(Right));

            WallClingState d = Clinging(Right);
            d.WallJump(8);
            Assert.IsFalse(d.IsClinging, "a wall jump");
            Assert.IsTrue(d.IsLocked(Right));
        }

        [Test]
        public void Clear_ForgetsTheCling_TheMoveLock_AndTheLock()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(8);
            s.Clear();
            Assert.IsTrue(s.Idle);
            Assert.IsFalse(s.IsLocked(Right));
            Assert.AreEqual(0, s.MoveLock);
            Assert.IsTrue(TryStep(s, Right), "no lock");
        }

        [Test]
        public void TheMoveLock_LastsItsTicks_AgainstTheFaceJustLeft()
        {
            WallClingState s = Clinging(Right);
            s.WallJump(8);
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
