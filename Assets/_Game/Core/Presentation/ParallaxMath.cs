using UnityEngine;

namespace Parallax.Core.Presentation
{
    /// <summary>Pure, origin-relative parallax placement shared by both realities.</summary>
    public static class ParallaxMath
    {
        public static Vector2 LayerLocalOffset(Vector2 cameraWorld, Vector2 origin, float screenSpeed) =>
            (cameraWorld - origin) * (1f - screenSpeed);

        public static Vector2 CameraLocal(Vector2 cameraWorld, Vector2 origin, Vector2 layerOffset) =>
            cameraWorld - origin - layerOffset;

        public static float TileLocalX(float cameraLocalX, float width, int index, int middle) =>
            Mathf.Round(cameraLocalX / width) * width + (index - middle) * width;

        public static bool TileCoversCamera(float tileLocalX, float cameraLocalX, float width) =>
            Mathf.Abs(tileLocalX - cameraLocalX) <= width * 0.5f;
    }
}
