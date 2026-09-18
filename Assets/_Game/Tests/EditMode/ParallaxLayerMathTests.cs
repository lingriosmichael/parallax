using NUnit.Framework;
using Parallax.Core;
using Parallax.Core.Presentation;
using UnityEngine;

namespace Parallax.Tests
{
    public sealed class ParallaxLayerMathTests
    {
        [Test]
        public void RelativeOffset_RealityAOrigin_UsesCameraPositionRelativeToZero()
        {
            AssertVector(new Vector2(10.2f, -3.4f),
                ParallaxMath.LayerLocalOffset(new Vector2(12f, -4f), Vector2.zero, 0.15f));
        }

        [Test]
        public void RelativeOffset_RealityBOrigin_DoesNotLeakWorldOffset()
        {
            Vector2 origin = RealitySpace.Origin(ObserverId.B);
            AssertVector(new Vector2(10.2f, -3.4f),
                ParallaxMath.LayerLocalOffset(new Vector2(12f, 996f), origin, 0.15f));
        }

        [Test]
        public void CameraLocal_MovesAtRequestedScreenSpeed()
        {
            Vector2 offset = ParallaxMath.LayerLocalOffset(new Vector2(20f, 0f), Vector2.zero, 0.35f);
            AssertVector(new Vector2(7f, 0f), ParallaxMath.CameraLocal(new Vector2(20f, 0f), Vector2.zero, offset));
        }

        [Test]
        public void Tiles_AreWidthMultiples_AndOneCoversCamera()
        {
            const float width = 8f;
            const float cameraLocalX = 13.7f;
            float left = ParallaxMath.TileLocalX(cameraLocalX, width, 0, 1);
            float middle = ParallaxMath.TileLocalX(cameraLocalX, width, 1, 1);
            float right = ParallaxMath.TileLocalX(cameraLocalX, width, 2, 1);
            Assert.That(left / width, Is.EqualTo(Mathf.Round(left / width)).Within(0.0001f));
            Assert.That(middle / width, Is.EqualTo(Mathf.Round(middle / width)).Within(0.0001f));
            Assert.That(right / width, Is.EqualTo(Mathf.Round(right / width)).Within(0.0001f));
            Assert.That(ParallaxMath.TileCoversCamera(left, cameraLocalX, width) || ParallaxMath.TileCoversCamera(middle, cameraLocalX, width) || ParallaxMath.TileCoversCamera(right, cameraLocalX, width), Is.True);
        }

        static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        }
    }
}
