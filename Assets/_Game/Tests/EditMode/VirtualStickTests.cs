using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests
{
    public class VirtualStickTests
    {
        [Test]
        public void Evaluate_WithinDeadZone_ReturnsZero()
        {
            Vector2 origin = new Vector2(100f, 100f);
            Vector2 current = origin + new Vector2(5f, 0f);
            Vector2 result = VirtualStick.Evaluate(origin, current, 50f, 0.15f);
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Evaluate_OriginEqualsCurrent_ReturnsZero()
        {
            Vector2 origin = new Vector2(20f, 30f);
            Vector2 result = VirtualStick.Evaluate(origin, origin, 50f, 0.15f);
            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void Evaluate_AtRadius_ReturnsMagnitudeOne()
        {
            Vector2 origin = Vector2.zero;
            Vector2 current = new Vector2(50f, 0f);
            Vector2 result = VirtualStick.Evaluate(origin, current, 50f, 0.15f);
            Assert.AreEqual(1f, result.magnitude, 1e-4f);
        }

        [Test]
        public void Evaluate_BeyondRadius_ClampsToMagnitudeOne()
        {
            Vector2 origin = Vector2.zero;
            Vector2 current = new Vector2(200f, 0f);
            Vector2 result = VirtualStick.Evaluate(origin, current, 50f, 0.15f);
            Assert.AreEqual(1f, result.magnitude, 1e-4f);
        }

        [Test]
        public void Evaluate_AtMidpointBetweenDeadZoneAndRadius_ReturnsHalfMagnitude()
        {
            float radius = 100f;
            float deadZone = 0.2f;
            float deadZoneDistance = deadZone * radius;
            float midpoint = deadZoneDistance + (radius - deadZoneDistance) * 0.5f;

            Vector2 origin = Vector2.zero;
            Vector2 current = new Vector2(midpoint, 0f);
            Vector2 result = VirtualStick.Evaluate(origin, current, radius, deadZone);

            Assert.AreEqual(0.5f, result.magnitude, 1e-4f);
        }

        [Test]
        public void Evaluate_DiagonalBeyondRadius_ClampedToUnitLength()
        {
            Vector2 origin = Vector2.zero;
            Vector2 current = new Vector2(200f, 200f);
            Vector2 result = VirtualStick.Evaluate(origin, current, 50f, 0.15f);
            Assert.AreEqual(1f, result.magnitude, 1e-4f);
        }

        [Test]
        public void ToMove_CatRelative_ReturnsStickX()
        {
            Vector2 stick = new Vector2(0.6f, 0.3f);
            float move = VirtualStick.ToMove(stick, new Vector2(1f, 0f), StickProjection.CatRelative);
            Assert.AreEqual(0.6f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ScreenRelative_FloorCat_ReturnsStickX()
        {
            Vector2 stick = new Vector2(0.6f, 0.3f);
            float move = VirtualStick.ToMove(stick, new Vector2(1f, 0f), StickProjection.ScreenRelative);
            Assert.AreEqual(0.6f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ScreenRelative_CeilingCat_RightStickMovesScreenRight()
        {
            float move = VirtualStick.ToMove(Vector2.right, Vector2.left, StickProjection.ScreenRelative);
            Assert.AreEqual(-1f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ScreenRelative_FloorCat_RightStickMovesScreenRight()
        {
            float move = VirtualStick.ToMove(Vector2.right, Vector2.right, StickProjection.ScreenRelative);
            Assert.AreEqual(1f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ScreenRelative_WallCat_ReturnsStickY()
        {
            Vector2 stick = new Vector2(0.3f, 0.6f);
            float move = VirtualStick.ToMove(stick, new Vector2(0f, 1f), StickProjection.ScreenRelative);
            Assert.AreEqual(0.6f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ScreenRelative_WallCat_StickPushedRight_ReturnsNearZero()
        {
            Vector2 stick = new Vector2(1f, 0f);
            float move = VirtualStick.ToMove(stick, new Vector2(0f, 1f), StickProjection.ScreenRelative);
            Assert.AreEqual(0f, move, 1e-5f);
        }

        [Test]
        public void ToMove_ResultClampedToUnitRange()
        {
            Vector2 stick = new Vector2(1f, 1f);
            float move = VirtualStick.ToMove(stick, new Vector2(1f, 0f), StickProjection.ScreenRelative);
            Assert.LessOrEqual(move, 1f);
            Assert.GreaterOrEqual(move, -1f);
        }
    }
}
