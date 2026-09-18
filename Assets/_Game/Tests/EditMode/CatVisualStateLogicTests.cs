using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class CatVisualStateLogicTests
    {
        [Test]
        public void Select_AboveAirThreshold_IsAir()
        {
            float belowIdle = 0f;
            CatVisualState state = CatVisualStateLogic.Select(CatVisualState.Walk, 1f, 1.01f, 0.02f, 0.15f, 1f, 0.1f, false, ref belowIdle);
            Assert.AreEqual(CatVisualState.Air, state);
        }

        [Test]
        public void Select_BriefStop_KeepsWalkUntilDwellExpires()
        {
            float belowIdle = 0f;
            CatVisualState state = CatVisualStateLogic.Select(CatVisualState.Walk, 0f, 0f, 0.05f, 0.15f, 1f, 0.1f, false, ref belowIdle);
            Assert.AreEqual(CatVisualState.Walk, state);
            Assert.AreEqual(0.05f, belowIdle, 0.0001f);

            state = CatVisualStateLogic.Select(state, 0f, 0f, 0.05f, 0.15f, 1f, 0.1f, false, ref belowIdle);
            Assert.AreEqual(CatVisualState.Idle, state);
        }

        [Test]
        public void Select_Teleport_IsIdleAndResetsDwell()
        {
            float belowIdle = 0.08f;
            CatVisualState state = CatVisualStateLogic.Select(CatVisualState.Walk, 4f, 3f, 0.02f, 0.15f, 1f, 0.1f, true, ref belowIdle);
            Assert.AreEqual(CatVisualState.Idle, state);
            Assert.AreEqual(0f, belowIdle);
        }

        [Test]
        public void ShouldFlip_RequiresHysteresisAndOppositeDirection()
        {
            Assert.IsFalse(CatVisualStateLogic.ShouldFlip(-0.05f, 0.05f, true));
            Assert.IsTrue(CatVisualStateLogic.ShouldFlip(-0.06f, 0.05f, true));
            Assert.IsFalse(CatVisualStateLogic.ShouldFlip(0.06f, 0.05f, true));
        }
    }
}
