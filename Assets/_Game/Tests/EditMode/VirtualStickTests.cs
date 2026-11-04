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

        // PAX-127 (D-322): full speed after a short push, and a centre that trails the thumb past the ring.
        [Test]
        public void ToRun_ReachesFullSpeed_AtFullSpeedAt_AndKeepsTheSign()
        {
            Assert.AreEqual(0f, VirtualStick.ToRun(0f, .25f));
            Assert.AreEqual(.5f, VirtualStick.ToRun(.125f, .25f), 1e-5f);
            Assert.AreEqual(1f, VirtualStick.ToRun(.25f, .25f), 1e-5f);
            Assert.AreEqual(1f, VirtualStick.ToRun(.6f, .25f), 1e-5f);
            Assert.AreEqual(-1f, VirtualStick.ToRun(-.6f, .25f), 1e-5f);
            Assert.AreEqual(-1f, VirtualStick.ToRun(-.01f, 0f), "no ramp: any push is full speed");
        }

        [Test]
        public void Follow_KeepsTheCentre_InsideTheRing()
        {
            Vector2 origin = new Vector2(100f, 100f);
            Assert.AreEqual(origin, VirtualStick.Follow(origin, origin + new Vector2(30f, 20f), 50f));
            Assert.AreEqual(origin, VirtualStick.Follow(origin, origin + new Vector2(50f, 0f), 50f));
        }

        [Test]
        public void Follow_TrailsTheThumb_ByTheRadius_PastTheRing()
        {
            Vector2 origin = Vector2.zero;
            Vector2 followed = VirtualStick.Follow(origin, new Vector2(200f, 0f), 50f);
            Assert.AreEqual(150f, followed.x, 1e-4f);
            Assert.AreEqual(0f, followed.y, 1e-4f);
        }

        [Test]
        public void Follow_ThenAShortFlickBack_ReversesTheRun()
        {
            float radius = 50f, deadZone = .15f;
            Vector2 origin = VirtualStick.Follow(Vector2.zero, new Vector2(200f, 0f), radius);
            Vector2 back = new Vector2(200f - radius - 20f, 0f);
            float move = VirtualStick.ToRun(VirtualStick.ToMove(VirtualStick.Evaluate(origin, back, radius, deadZone), Vector2.right,
                StickProjection.CatRelative), VirtualStick.DefaultFullSpeedAt);
            Assert.AreEqual(-1f, move, 1e-5f, "20 px past the centre is full speed the other way");
        }
    }
}
