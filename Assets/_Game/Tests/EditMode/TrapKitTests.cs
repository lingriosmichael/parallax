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
        [Test] public void GravityRule_ResolvesAllModes() { Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.Flip, Vector2.down)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.Flip, Vector2.up)); Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.ForceUp, Vector2.down)); Assert.AreEqual(Vector2.up, GravityFlipRule.Resolve(GravityFlipMode.ForceUp, Vector2.up)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.ForceDown, Vector2.up)); Assert.AreEqual(Vector2.down, GravityFlipRule.Resolve(GravityFlipMode.ForceDown, Vector2.down)); }
    }
}
