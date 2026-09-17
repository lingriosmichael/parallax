using UnityEngine;

namespace Parallax.Core
{
    public static class FlipbookMath
    {
        public static float ClampFps(float fps, float minFps, float maxFps) =>
            Mathf.Clamp(fps, Mathf.Min(minFps, maxFps), Mathf.Max(minFps, maxFps));

        public static int FrameIndex(float elapsedSeconds, float fps, int loopStart, int loopEnd)
        {
            int length = loopEnd - loopStart + 1;
            if (length <= 1 || fps <= 0f) return loopStart;

            int frameOffset = Mathf.FloorToInt(elapsedSeconds * fps) % length;
            if (frameOffset < 0) frameOffset += length;
            return loopStart + frameOffset;
        }

        public static float FpsForSpeed(float speed, float referenceSpeed, float walkFps, float minFps, float maxFps)
        {
            if (referenceSpeed <= 0f) return ClampFps(walkFps, minFps, maxFps);

            float scaled = walkFps * (Mathf.Abs(speed) / referenceSpeed);
            return ClampFps(scaled, minFps, maxFps);
        }
    }
}
