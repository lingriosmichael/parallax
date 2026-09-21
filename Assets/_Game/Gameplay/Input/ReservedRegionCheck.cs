using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Gameplay.Input
{
    public static class ReservedRegionCheck
    {
        // True when `screenPos` falls inside any region — such a touch is never
        // claimed as stick or jump input.
        public static bool IsReserved(IReadOnlyList<ITouchReservedRegion> regions, Vector2 screenPos)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                ITouchReservedRegion region = regions[i];
                if (ReferenceEquals(region, null)) continue;

                UnityEngine.Object unityRegion = region as UnityEngine.Object;
                if (!ReferenceEquals(unityRegion, null) && unityRegion == null) continue;

                if (region.ContainsScreenPoint(screenPos)) return true;
            }
            return false;
        }
    }
}
