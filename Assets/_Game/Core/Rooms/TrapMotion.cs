using UnityEngine;

namespace Parallax.Core
{
    public static class TrapMotion
    {
        public static float Progress(int ticks, int durationTicks) => durationTicks <= 0 ? 1f : Mathf.Clamp01((float)ticks / durationTicks);
        public static float Travel(int ticks, float unitsPerTick, float distance) => Mathf.Min(Mathf.Max(0, ticks) * Mathf.Max(0f, unitsPerTick), Mathf.Max(0f, distance));
        public static Vector2 MovingOffset(Vector2 offset, int ticksSinceFire, int moveTicks, int holdTicks, int returnTicks)
        {
            if (ticksSinceFire <= 0) return Vector2.zero;
            if (ticksSinceFire <= moveTicks) return offset * Progress(ticksSinceFire, moveTicks);
            int holdEnd = moveTicks + Mathf.Max(0, holdTicks);
            if (ticksSinceFire <= holdEnd || returnTicks <= 0) return offset;
            return offset * (1f - Progress(ticksSinceFire - holdEnd, returnTicks));
        }
        // 2D only: Bounds.Expand would also shrink z, and 2D bounds have zero depth, so the
        // shrunk box would never intersect anything. Shrink x and y explicitly instead.
        public static bool Crushes(Bounds catBounds, Bounds solidPose, float depth)
        {
            float d = Mathf.Max(0f, depth);
            float minX = solidPose.min.x + d, maxX = solidPose.max.x - d;
            float minY = solidPose.min.y + d, maxY = solidPose.max.y - d;
            if (minX >= maxX || minY >= maxY) return false;
            return catBounds.min.x < maxX && catBounds.max.x > minX
                && catBounds.min.y < maxY && catBounds.max.y > minY;
        }
    }
}
