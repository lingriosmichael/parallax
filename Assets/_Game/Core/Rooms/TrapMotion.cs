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
        /// <summary>PAX-107 (D-124): the same timing along an L-shaped path: the vertical part of `offset` first, then the
        /// horizontal; the return retraces it (across, then down). A lift that rises out of its slot before it carries the cat.</summary>
        public static Vector2 RiseFirstOffset(Vector2 offset, int ticksSinceFire, int moveTicks, int holdTicks, int returnTicks)
        {
            float rise = Mathf.Abs(offset.y), across = Mathf.Abs(offset.x), total = rise + across;
            if (total <= 0f) return Vector2.zero;
            float d = MovingOffset(Vector2.right, ticksSinceFire, moveTicks, holdTicks, returnTicks).x * total;
            return d <= rise ? new Vector2(0f, Mathf.Sign(offset.y) * d) : new Vector2(Mathf.Sign(offset.x) * (d - rise), offset.y);
        }
        /// <summary>PAX-107 (D-124): the cat touches the solid (within `skin`): a spiked face kills on contact.</summary>
        public static bool Touches(Bounds catBounds, Bounds solidPose, float skin) =>
            catBounds.min.x < solidPose.max.x + skin && catBounds.max.x > solidPose.min.x - skin
            && catBounds.min.y < solidPose.max.y + skin && catBounds.max.y > solidPose.min.y - skin;
        /// <summary>PAX-107 (D-140): a solid moving down from `from` to `to` lands on the cat when the cat's box overlaps the
        /// region it sweeps this step (any overlap at its sides; at its top, more than `skin`, so a cat riding it is clear).
        /// Physics alone pushes such a cat out sideways (flung, from a corner) or onto its top instead of pinning it, so the
        /// crush test never saw it. D-211 (the developer: "Running into a moving wall shouldnt kill me"): only a cat under it,
        /// its top no higher than the solid's bottom was (plus `skin`); a cat beside its body, walking into its side, is clear.</summary>
        public static bool SlamsOnto(Bounds catBounds, Bounds from, Bounds to, float skin)
        {
            float minX = Mathf.Min(from.min.x, to.min.x), maxX = Mathf.Max(from.max.x, to.max.x);
            float minY = Mathf.Min(from.min.y, to.min.y), maxY = Mathf.Max(from.max.y, to.max.y) - skin;
            if (minX >= maxX || minY >= maxY) return false;
            if (catBounds.max.y > from.min.y + skin) return false;
            return catBounds.min.x < maxX && catBounds.max.x > minX && catBounds.min.y < maxY && catBounds.max.y > minY;
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
