using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-V06: game feel's curves, pure and deterministic (presentation only: nothing in gameplay reads them).</summary>
    public static class FeelMath
    {
        /// <summary>A beat's squash `t` seconds into its `duration`: `amount` at once, easing back through rest to a small rebound
        /// (about 15 % of it the other way) and settling at 0. Positive squashes (wider, lower), negative stretches.</summary>
        public static float Squash(float amount, float t, float duration)
        {
            if (duration <= 0f || t < 0f || t >= duration) return 0f;
            float u = t / duration;
            return amount * (1f - u) * (1f - u) * Mathf.Cos(u * Mathf.PI * 1.5f);
        }

        /// <summary>The drawn scale for squash `s`: wider by s and lower by s (a stretch the reverse), about the paws.</summary>
        public static Vector2 ScaleOf(float s) => new Vector2(1f + s, 1f - s);

        /// <summary>A translational shake's offset `t` seconds into its `duration` (never a rotation, D-020): up to `amplitude`
        /// world units, fading out by the end. The same seed and time give the same offset.</summary>
        public static Vector2 Shake(float amplitude, float t, float duration, int seed, float frequency)
        {
            if (duration <= 0f || t < 0f || t >= duration || amplitude <= 0f) return Vector2.zero;
            float fade = (1f - t / duration) * (1f - t / duration);
            float phase = 2f * Mathf.PI * frequency * t;
            return amplitude * fade * new Vector2(Mathf.Sin(phase + seed * 1.7f), Mathf.Sin(phase * 1.31f + seed * 2.9f));
        }

        /// <summary>How much of an effect reaches the cat `distance` away: all of it beside it, none from `within` on.</summary>
        public static float Falloff(float distance, float within) => within <= 0f ? 0f : 1f - Mathf.Clamp01(distance / within);

        /// <summary>A mover's sudden stop: it was moving at `before` (u/s) or faster than `impactSpeed`, and now moves at under a
        /// fifth of that. A mover easing to rest, or a slow one, never counts.</summary>
        public static bool Impact(float before, float now, float impactSpeed) => before >= impactSpeed && now < before * 0.2f;
    }
}
