using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class GravityMathTests
    {
        [Test]
        public void DownPlusOne_IsRight()
        {
            Vector2 result = GravityMath.Rotate90(Vector2.down, 1);
            Assert.AreEqual(Vector2.right, result);
        }

        [Test]
        public void DownMinusOne_IsLeft()
        {
            Vector2 result = GravityMath.Rotate90(Vector2.down, -1);
            Assert.AreEqual(Vector2.left, result);
        }

        [Test]
        public void DownPlusTwo_IsUp()
        {
            Vector2 result = GravityMath.Rotate90(Vector2.down, 2);
            Assert.AreEqual(Vector2.up, result);
        }

        [Test]
        public void DownPlusFour_IsDown()
        {
            Vector2 result = GravityMath.Rotate90(Vector2.down, 4);
            Assert.AreEqual(Vector2.down, result);
        }

        [Test]
        public void DownMinusThree_EqualsDownPlusOne()
        {
            Vector2 minusThree = GravityMath.Rotate90(Vector2.down, -3);
            Vector2 plusOne    = GravityMath.Rotate90(Vector2.down, 1);
            Assert.AreEqual(plusOne, minusThree);
            Assert.AreEqual(Vector2.right, minusThree);
        }

        [Test]
        public void Results_AreExact_NoFloatingDrift()
        {
            Vector2 result = GravityMath.Rotate90(Vector2.down, 1);
            Assert.AreEqual(1f, result.x);
            Assert.AreEqual(0f, result.y);
        }
    }
}
