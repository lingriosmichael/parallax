using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class FlipbookMathTests
    {
        [Test]
        public void FrameIndex_WrapsWithinLoopRange()
        {
            Assert.AreEqual(0, FlipbookMath.FrameIndex(0f, 10f, 0, 9));
            Assert.AreEqual(5, FlipbookMath.FrameIndex(0.5f, 10f, 0, 9));
            Assert.AreEqual(0, FlipbookMath.FrameIndex(1.0f, 10f, 0, 9));
            Assert.AreEqual(3, FlipbookMath.FrameIndex(1.3f, 10f, 0, 9));
        }

        [Test]
        public void FrameIndex_RespectsLoopOffset()
        {
            Assert.AreEqual(2, FlipbookMath.FrameIndex(0f, 10f, 2, 6));
            Assert.AreEqual(6, FlipbookMath.FrameIndex(0.4f, 10f, 2, 6));
            Assert.AreEqual(2, FlipbookMath.FrameIndex(0.5f, 10f, 2, 6));
        }

        [Test]
        public void FrameIndex_ZeroFps_HoldsLoopStart()
        {
            Assert.AreEqual(4, FlipbookMath.FrameIndex(1.23f, 0f, 4, 9));
        }

        [Test]
        public void FrameIndex_SingleFrameLoop_HoldsThatFrame()
        {
            Assert.AreEqual(3, FlipbookMath.FrameIndex(5f, 10f, 3, 3));
        }

        [Test]
        public void ClampFps_ClampsToRange()
        {
            Assert.AreEqual(2f, FlipbookMath.ClampFps(0.5f, 2f, 18f));
            Assert.AreEqual(18f, FlipbookMath.ClampFps(50f, 2f, 18f));
            Assert.AreEqual(10f, FlipbookMath.ClampFps(10f, 2f, 18f));
        }

        [Test]
        public void FpsForSpeed_ScalesWithSpeedAndClamps()
        {
            float fps = FlipbookMath.FpsForSpeed(3f, 6f, 10f, 2f, 18f);
            Assert.AreEqual(5f, fps, 0.001f);

            Assert.AreEqual(2f, FlipbookMath.FpsForSpeed(0f, 6f, 10f, 2f, 18f));
            Assert.AreEqual(18f, FlipbookMath.FpsForSpeed(100f, 6f, 10f, 2f, 18f));
        }

        [Test]
        public void FpsForSpeed_ZeroReferenceSpeed_HoldsWalkFpsClamped()
        {
            Assert.AreEqual(10f, FlipbookMath.FpsForSpeed(5f, 0f, 10f, 2f, 18f));
        }
    }
}
