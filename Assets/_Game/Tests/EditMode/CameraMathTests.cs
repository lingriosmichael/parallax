using NUnit.Framework;
using Parallax.Core.Cameras;
using UnityEngine;

namespace Parallax.Tests
{
    public class CameraMathTests
    {
        [Test]
        public void TargetInsideDeadZone_ReturnsCentreUnchanged()
        {
            Vector2 centre = new Vector2(1f, 2f);
            Vector2 target = centre + new Vector2(0.5f, -0.5f);
            Vector2 result = CameraMath.ResolveDeadZone(centre, target, new Vector2(2f, 1.6f));
            Assert.AreEqual(centre, result);
        }

        [Test]
        public void TargetPastRightEdge_PushesCentreRightByOvershoot()
        {
            Vector2 centre = Vector2.zero;
            Vector2 halfExtents = new Vector2(2f, 1.6f);
            Vector2 target = new Vector2(3f, 0f);

            Vector2 result = CameraMath.ResolveDeadZone(centre, target, halfExtents);

            Assert.AreEqual(1f, result.x, 1e-5f);
            Assert.AreEqual(0f, result.y, 1e-5f);
        }

        [Test]
        public void TargetPastBottomEdge_MovesYOnly()
        {
            Vector2 centre = new Vector2(5f, 0f);
            Vector2 halfExtents = new Vector2(2f, 1.6f);
            Vector2 target = new Vector2(5f, -3f);

            Vector2 result = CameraMath.ResolveDeadZone(centre, target, halfExtents);

            Assert.AreEqual(5f, result.x, 1e-5f);
            Assert.AreEqual(-1.4f, result.y, 1e-5f);
        }

        [Test]
        public void ClampToBounds_ViewLargerThanBounds_CentresOnBounds()
        {
            Vector2 halfView = new Vector2(10f, 10f);
            Vector2 boundsMin = new Vector2(-1f, -1f);
            Vector2 boundsMax = new Vector2(1f, 1f);

            Vector2 result = CameraMath.ClampToBounds(new Vector2(5f, 5f), halfView, boundsMin, boundsMax);

            Assert.AreEqual(0f, result.x, 1e-5f);
            Assert.AreEqual(0f, result.y, 1e-5f);
        }

        [Test]
        public void ClampToBounds_CentreInsideValidRange_IsUnchanged()
        {
            Vector2 halfView = new Vector2(2f, 2f);
            Vector2 boundsMin = new Vector2(-10f, -10f);
            Vector2 boundsMax = new Vector2(10f, 10f);

            Vector2 result = CameraMath.ClampToBounds(new Vector2(3f, -1f), halfView, boundsMin, boundsMax);

            Assert.AreEqual(3f, result.x, 1e-5f);
            Assert.AreEqual(-1f, result.y, 1e-5f);
        }

        [Test]
        public void ClampToBounds_CentreBeyondEdge_IsClamped()
        {
            Vector2 halfView = new Vector2(2f, 2f);
            Vector2 boundsMin = new Vector2(-10f, -10f);
            Vector2 boundsMax = new Vector2(10f, 10f);

            Vector2 result = CameraMath.ClampToBounds(new Vector2(15f, -15f), halfView, boundsMin, boundsMax);

            Assert.AreEqual(8f, result.x, 1e-5f);
            Assert.AreEqual(-8f, result.y, 1e-5f);
        }
    }
}
