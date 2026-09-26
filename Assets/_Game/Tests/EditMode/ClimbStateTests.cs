using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-087 (D-089) §5: the pure climbing rules (ClimbState).
    public sealed class ClimbStateTests
    {
        const float Threshold = ClimbState.DefaultGrabThreshold, Dt = .02f;
        static readonly Vector2 Down = Vector2.down, Right = Vector2.right;

        // ---------- grab (R5 (1)) ----------

        [Test] public void Grounded_GrabsOnlyWithUpAtTheThreshold()
        {
            Assert.IsTrue(ClimbState.WantsGrab(.5f, true, Threshold));
            Assert.IsTrue(ClimbState.WantsGrab(1f, true, Threshold));
            Assert.IsFalse(ClimbState.WantsGrab(.49f, true, Threshold));
            Assert.IsFalse(ClimbState.WantsGrab(-1f, true, Threshold), "down at a vine's foot does nothing");
            Assert.IsFalse(ClimbState.WantsGrab(0f, true, Threshold));
        }

        [Test] public void Airborne_GrabsWithEitherDirectionAtTheThreshold()
        {
            Assert.IsTrue(ClimbState.WantsGrab(.5f, false, Threshold));
            Assert.IsTrue(ClimbState.WantsGrab(-.5f, false, Threshold));
            Assert.IsFalse(ClimbState.WantsGrab(.49f, false, Threshold));
            Assert.IsFalse(ClimbState.WantsGrab(-.49f, false, Threshold));
            Assert.IsFalse(ClimbState.WantsGrab(0f, false, Threshold), "touching a vine without pushing does nothing");
        }

        [Test] public void TryGrab_SetsTheVineAndTheGravitySide()
        {
            var s = new ClimbState();
            s.BeginStep();
            Assert.IsTrue(s.TryGrab(2, 1f, false, Threshold, gravityUp: true));
            Assert.IsTrue(s.IsClimbing);
            Assert.AreEqual(2, s.Vine);
            Assert.IsTrue(s.GrabbedWithGravityUp);
            Assert.IsFalse(s.TryGrab(3, 1f, false, Threshold, false), "already climbing");
        }

        // ---------- climbing: speed and the top stop (R5 (2)) ----------

        [Test] public void ClimbVelocity_IsClimbTimesSpeed_ScreenUpPositive()
        {
            Assert.AreEqual(4f, ClimbState.ClimbVelocity(1f, 4f, 0f, 10f, Dt), 1e-6f);
            Assert.AreEqual(-4f, ClimbState.ClimbVelocity(-1f, 4f, 0f, 10f, Dt), 1e-6f);
            Assert.AreEqual(2f, ClimbState.ClimbVelocity(.5f, 4f, 0f, 10f, Dt), 1e-6f, "a shallow push climbs slower (R2)");
            Assert.AreEqual(0f, ClimbState.ClimbVelocity(0f, 4f, 0f, 10f, Dt), "Climb 0 hangs");
            Assert.AreEqual(.08f, ClimbState.DefaultClimbSpeed * Dt, 1e-6f, "4 u/s is 0.08 u per tick");
        }

        [Test] public void ClimbVelocity_StopsTheColliderTopExactlyAtTheVinesTop()
        {
            // The third argument is the collider's top: the cat stays on the vine.
            Assert.AreEqual(.05f / Dt, ClimbState.ClimbVelocity(1f, 4f, 4.45f, 4.5f, Dt), 1e-4f, "the last step lands on the top");
            Assert.AreEqual(0f, ClimbState.ClimbVelocity(1f, 4f, 4.5f, 4.5f, Dt), "at the top: stopped");
            Assert.AreEqual(0f, ClimbState.ClimbVelocity(1f, 4f, 4.6f, 4.5f, Dt), "above it: never pushed down by the stop");
            Assert.AreEqual(-4f, ClimbState.ClimbVelocity(-1f, 4f, 4.5f, 4.5f, Dt), "down from the top");
        }

        [Test] public void ReleasesAtBottom_WhenTheCentreIsBelowTheBottom_OrStandingWhilePushingDown()
        {
            Assert.IsTrue(ClimbState.ReleasesAtBottom(-1f, .99f, 1f, false));
            Assert.IsFalse(ClimbState.ReleasesAtBottom(-1f, 1f, 1f, false));
            Assert.IsFalse(ClimbState.ReleasesAtBottom(1f, .5f, 1f, false), "only climbing down releases");
            Assert.IsFalse(ClimbState.ReleasesAtBottom(0f, .5f, 1f, false), "hanging below the bottom's reach doesn't release");
            Assert.IsTrue(ClimbState.ReleasesAtBottom(-1f, 2f, 1f, true), "standing on ground while pushing down");
        }

        // ---------- the leap ----------

        [Test] public void Leap_IsTheJumpLaunch_PlusSignOfMoveTimesMaxSpeed()
        {
            Assert.AreEqual(new Vector2(6f, 9.8f), ClimbState.LeapVelocity(1f, Right, Down, 6f, 9.8f));
            Assert.AreEqual(new Vector2(6f, 9.8f), ClimbState.LeapVelocity(.3f, Right, Down, 6f, 9.8f), "sign only: always full run speed");
            Assert.AreEqual(new Vector2(-6f, 9.8f), ClimbState.LeapVelocity(-1f, Right, Down, 6f, 9.8f));
            Assert.AreEqual(new Vector2(0f, 9.8f), ClimbState.LeapVelocity(0f, Right, Down, 6f, 9.8f), "straight up with no Move");
        }

        [Test] public void Leap_WithAnInvertedMove_GoesTheOtherWay()
        {
            float held = 1f, inverted = -held;   // LocalHumanDriver inverts Move before the motor (D-087); Climb never.
            Assert.AreEqual(-6f, ClimbState.LeapVelocity(inverted, Right, Down, 6f, 9.8f).x);
        }

        [Test] public void Leap_UnderGravityUp_LaunchesDown_AlongTheCatsRight()
        {
            Vector2 down = Vector2.up, right = new(-down.y, down.x);
            Assert.AreEqual(new Vector2(-6f, -9.8f), ClimbState.LeapVelocity(1f, right, down, 6f, 9.8f));
        }

        // ---------- the regrab lock ----------

        [Test] public void AReleaseInsideAStep_LocksThatVineForTheNextTenSteps_OnlyThatVine()
        {
            var s = new ClimbState();
            s.BeginStep();
            s.TryGrab(0, 1f, false, Threshold, false);
            s.Release(10);
            Assert.IsFalse(s.IsClimbing);
            for (int i = 1; i <= 10; i++)
            {
                s.BeginStep();
                Assert.IsFalse(s.TryGrab(0, 1f, false, Threshold, false), $"step {i} after the release: locked");
            }
            s.BeginStep();
            Assert.IsTrue(s.TryGrab(0, 1f, false, Threshold, false), "the eleventh step grabs");
        }

        [Test] public void AReleaseBetweenSteps_LocksTheSameTenSteps()
        {
            var s = new ClimbState();
            s.BeginStep();
            s.TryGrab(0, 1f, false, Threshold, false);
            // e.g. ApplyLaunch in the room step, after the motor step
            s.Release(10);
            for (int i = 0; i < 10; i++) { s.BeginStep(); Assert.IsTrue(s.IsLocked(0)); }
            s.BeginStep();
            Assert.IsFalse(s.IsLocked(0));
        }

        [Test] public void TheLock_DoesntStopAnotherVine()
        {
            var s = new ClimbState();
            s.BeginStep();
            s.TryGrab(0, 1f, false, Threshold, false);
            s.Release(10);
            s.BeginStep();
            Assert.IsTrue(s.TryGrab(1, 1f, false, Threshold, false));
        }

        [Test] public void Clear_EndsTheClimb_WithNoLock()
        {
            var s = new ClimbState();
            s.BeginStep();
            s.TryGrab(0, 1f, false, Threshold, false);
            s.Clear();
            Assert.IsFalse(s.IsClimbing);
            Assert.AreEqual(-1, s.Vine);
            s.BeginStep();
            Assert.IsTrue(s.TryGrab(0, 1f, false, Threshold, false), "a death/reset/respawn leaves nothing locked");
        }

        [Test] public void Release_WhenNotClimbing_LocksNothing()
        {
            var s = new ClimbState();
            s.BeginStep();
            s.Release(10);
            Assert.IsFalse(s.IsLocked(0));
            Assert.IsFalse(s.IsLocked(-1));
        }
    }
}
