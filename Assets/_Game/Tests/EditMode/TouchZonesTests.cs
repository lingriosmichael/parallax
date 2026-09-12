using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class TouchZonesTests
    {
        static readonly Rect MoveLeft  = new Rect(0.00f, 0.00f, 0.16f, 0.45f);
        static readonly Rect MoveRight = new Rect(0.16f, 0.00f, 0.16f, 0.45f);
        static readonly Rect Jump      = new Rect(0.78f, 0.00f, 0.22f, 0.45f);

        [Test]
        public void Classify_PointInJumpZone_ReturnsJump()
        {
            TouchZone zone = TouchZones.Classify(new Vector2(0.9f, 0.2f), MoveLeft, MoveRight, Jump);
            Assert.AreEqual(TouchZone.Jump, zone);
        }

        [Test]
        public void Classify_PointInDeadSpaceBetweenMoveAndJumpZones_ReturnsNone()
        {
            TouchZone zone = TouchZones.Classify(new Vector2(0.5f, 0.2f), MoveLeft, MoveRight, Jump);
            Assert.AreEqual(TouchZone.None, zone);
        }

        [Test]
        public void Classify_PointAboveAllZones_ReturnsNone()
        {
            TouchZone zone = TouchZones.Classify(new Vector2(0.08f, 0.9f), MoveLeft, MoveRight, Jump);
            Assert.AreEqual(TouchZone.None, zone);
        }

        [Test]
        public void Classify_PointInOverlapOfMoveAndJumpZones_JumpTakesPriority()
        {
            Rect overlappingJump = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            TouchZone zone = TouchZones.Classify(new Vector2(0.08f, 0.2f), MoveLeft, MoveRight, overlappingJump);
            Assert.AreEqual(TouchZone.Jump, zone);
        }

        [Test]
        public void ToSafeAreaNormalised_ZeroOrigin_MapsDirectly()
        {
            Rect safeArea = new Rect(0f, 0f, 1000f, 500f);
            Vector2 result = TouchZones.ToSafeAreaNormalised(new Vector2(500f, 250f), safeArea);
            Assert.AreEqual(0.5f, result.x, 1e-5f);
            Assert.AreEqual(0.5f, result.y, 1e-5f);
        }

        [Test]
        public void ToSafeAreaNormalised_NonZeroOrigin_OffsetsBeforeNormalising()
        {
            Rect safeArea = new Rect(100f, 50f, 800f, 400f);
            Vector2 result = TouchZones.ToSafeAreaNormalised(new Vector2(500f, 250f), safeArea);
            Assert.AreEqual(0.5f, result.x, 1e-5f);
            Assert.AreEqual(0.5f, result.y, 1e-5f);
        }

        [Test]
        public void ToSafeAreaNormalised_PointOutsideSafeArea_ReturnsValueOutsideZeroOne()
        {
            Rect safeArea = new Rect(0f, 0f, 1000f, 500f);
            Vector2 result = TouchZones.ToSafeAreaNormalised(new Vector2(-100f, 600f), safeArea);
            Assert.Less(result.x, 0f);
            Assert.Greater(result.y, 1f);
        }
    }
}
