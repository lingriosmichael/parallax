using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    public sealed class TrapKitTests
    {
        [Test] public void Countdown_DelayZero_FiresOnTriggerTick() { var c = new TrapCountdown(0); Assert.IsTrue(c.Step(true)); Assert.IsFalse(c.Step(true)); }
        [Test] public void Countdown_DelayOne_FiresOnTickAfterTrigger() { var c = new TrapCountdown(1); Assert.IsFalse(c.Step(true)); Assert.IsTrue(c.Step(false)); Assert.IsFalse(c.Step(false)); }
        [Test] public void Countdown_DelayThree_FiresExactlyThreeTicksAfterTrigger() { var c = new TrapCountdown(3); Assert.IsFalse(c.Step(true)); Assert.IsFalse(c.Step(false)); Assert.IsFalse(c.Step(false)); Assert.IsTrue(c.Step(false)); }
        [Test] public void Countdown_TouchAndLeaveStillFires() { var c = new TrapCountdown(2); Assert.IsFalse(c.Step(true)); Assert.IsFalse(c.Step(false)); Assert.IsTrue(c.Step(false)); }
        [Test] public void Countdown_ResetRearms() { var c = new TrapCountdown(0); Assert.IsTrue(c.Step(true)); c.Reset(); Assert.AreEqual(TrapCountdownState.Armed, c.State); Assert.IsTrue(c.Step(true)); }
        [Test] public void Countdown_RearmsOnlyAfterUntriggeredTick() { var c = new TrapCountdown(0, true); Assert.IsTrue(c.Step(true)); Assert.IsFalse(c.Step(true)); Assert.AreEqual(TrapCountdownState.Fired, c.State); Assert.IsFalse(c.Step(false)); Assert.AreEqual(TrapCountdownState.Armed, c.State); }
        [Test] public void Motion_Clamps() { Assert.AreEqual(0f, TrapMotion.Progress(-1, 2)); Assert.AreEqual(1f, TrapMotion.Progress(3, 2)); Assert.AreEqual(1f, TrapMotion.Progress(0, 0)); Assert.AreEqual(2f, TrapMotion.Travel(20, .3f, 2f)); Assert.AreEqual(0f, TrapMotion.Travel(-1, .3f, 2f)); }
        [Test] public void Timing_RearmAndPresenceFireAgainOnFirstTickAfterCooldown()
        {
            var timing = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Rearm, 2, 3, 1, 0);
            Assert.IsFalse(timing.Step(0, true)); Assert.IsFalse(timing.Step(1, true)); Assert.IsTrue(timing.Step(2, true));
            Assert.IsFalse(timing.Step(3, true)); Assert.IsFalse(timing.Step(4, true)); Assert.IsFalse(timing.Step(5, true)); Assert.IsFalse(timing.Step(6, true)); Assert.IsTrue(timing.Step(7, true));
        }
        [Test] public void Timing_PeriodicUsesRoomLifePhase()
        {
            var timing = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Periodic, 0, 2, 5, 2);
            Assert.IsFalse(timing.Step(0, false)); Assert.IsFalse(timing.Step(1, false)); Assert.IsTrue(timing.Step(2, false));
            Assert.IsFalse(timing.Step(3, false)); Assert.IsTrue(timing.Step(7, false));
        }
        [Test] public void Timing_ChainDelayIsOrderIndependent()
        {
            var source = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 0, 0, 1, 0);
            var target = new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Once, 1, 0, 1, 0);
            Assert.IsTrue(source.Step(0, true)); Assert.IsFalse(target.Step(0, false, source.LatestFireTick)); Assert.IsTrue(target.Step(1, false, source.LatestFireTick));
        }
        [Test] public void Timing_ResetClearsPendingAndRestartsPeriodicPhase()
        {
            var chain = new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Once, 2, 0, 1, 0);
            Assert.IsFalse(chain.Step(0, false, 0)); chain.Reset();
            Assert.IsFalse(chain.Step(2, false, -1)); Assert.AreEqual(-1, chain.LatestFireTick); Assert.IsTrue(chain.IsArmed);
            var periodic = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Periodic, 0, 2, 8, 3);
            Assert.IsTrue(periodic.Step(3, false)); periodic.Reset(); Assert.IsFalse(periodic.Step(0, false)); Assert.IsTrue(periodic.Step(3, false));
        }
        [Test] public void Timing_UnarmedChainTargetIgnoresPriorSourceFireAfterRearm()
        {
            var target = new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Rearm, 1, 1, 1, 0);
            Assert.IsFalse(target.Step(0, false, 0)); Assert.IsTrue(target.Step(1, false, 0));
            // Target rearms on tick 2. A source fire from tick 1 was while it was unarmed.
            Assert.IsFalse(target.Step(2, false, 1)); Assert.IsFalse(target.Step(3, false, 1));
        }
        [Test] public void MovingMotion_IsPiecewiseAndStaysWhenReturnIsZero()
        {
            Assert.AreEqual(Vector2.zero, TrapMotion.MovingOffset(new Vector2(6, 0), 0, 4, 2, 4));
            Assert.AreEqual(new Vector2(3, 0), TrapMotion.MovingOffset(new Vector2(6, 0), 2, 4, 2, 4));
            Assert.AreEqual(new Vector2(6, 0), TrapMotion.MovingOffset(new Vector2(6, 0), 6, 4, 2, 4));
            Assert.AreEqual(new Vector2(3, 0), TrapMotion.MovingOffset(new Vector2(6, 0), 8, 4, 2, 4));
            Assert.AreEqual(new Vector2(6, 0), TrapMotion.MovingOffset(new Vector2(6, 0), 99, 4, 2, 0));
        }
        [Test] public void Crush_OnlyOverlapsShrunkResolvedPose()
        {
            Bounds solid = new(Vector3.zero, new Vector3(2, 2));
            // Right edge is x=1.0; cat half-width is .35.  Touch and .10 penetration
            // remain outside the .15 inset, while .20 penetration reaches it.
            Assert.IsFalse(TrapMotion.Crushes(new Bounds(new Vector3(1.35f, 0), new Vector3(.7f, .5f)), solid, .15f));
            Assert.IsFalse(TrapMotion.Crushes(new Bounds(new Vector3(1.25f, 0), new Vector3(.7f, .5f)), solid, .15f));
            Assert.IsTrue(TrapMotion.Crushes(new Bounds(new Vector3(1.15f, 0), new Vector3(.7f, .5f)), solid, .15f));
        }
        [Test] public void GravityRule_ResolvesAllModes() { Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.Flip, Vector2.down)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.Flip, Vector2.up)); Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.ForceUp, Vector2.down)); Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.ForceUp, Vector2.up)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.ForceDown, Vector2.up)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.ForceDown, Vector2.down)); }
    }
}
