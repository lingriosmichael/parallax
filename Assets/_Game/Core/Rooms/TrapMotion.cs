using UnityEngine;

namespace Parallax.Core
{
    public static class TrapMotion
    {
        public static float Progress(int ticks, int durationTicks) => durationTicks <= 0 ? 1f : Mathf.Clamp01((float)ticks / durationTicks);
        public static float Travel(int ticks, float unitsPerTick, float distance) => Mathf.Min(Mathf.Max(0, ticks) * Mathf.Max(0f, unitsPerTick), Mathf.Max(0f, distance));
    }
}
