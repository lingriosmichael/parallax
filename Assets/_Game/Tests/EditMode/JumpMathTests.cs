using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests
{
    public class JumpMathTests
    {
        [Test]
        public void SpeedForHeight_KnownValue_ReturnsSqrtTwoGH()
        {
            float result = JumpMath.SpeedForHeight(2f, 30f);
            Assert.AreEqual(System.Math.Sqrt(120), result, 1e-4f);
        }

        [Test]
        public void SpeedForHeight_ZeroHeight_ReturnsZero()
        {
            Assert.AreEqual(0f, JumpMath.SpeedForHeight(0f, 30f));
        }

        [Test]
        public void SpeedForHeight_NegativeHeight_ReturnsZero()
        {
            Assert.AreEqual(0f, JumpMath.SpeedForHeight(-2f, 30f));
        }

        [Test]
        public void SpeedForHeight_NegativeGravity_ReturnsZero()
        {
            Assert.AreEqual(0f, JumpMath.SpeedForHeight(2f, -30f));
        }
    }
}
