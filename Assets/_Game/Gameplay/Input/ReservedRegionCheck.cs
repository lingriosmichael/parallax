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
                if (regions[i] != null && regions[i].ContainsScreenPoint(screenPos)) return true;
            }
            return false;
        }
    }
}
