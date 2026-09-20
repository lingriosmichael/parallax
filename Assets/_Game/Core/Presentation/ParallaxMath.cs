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

        public static float BackgroundTileOffsetY(float tileHeight, float maxAbsCameraY, float orthoSize, float screenSpeed, float margin, out float shortfall)
        {
            float halfEnvelope = orthoSize + screenSpeed * maxAbsCameraY + margin;
            float requiredHeight = 2f * halfEnvelope;
            shortfall = Mathf.Max(0f, requiredHeight - tileHeight);
            return shortfall <= 0f ? 0f : -halfEnvelope + tileHeight * .5f;
        }
    }
}
