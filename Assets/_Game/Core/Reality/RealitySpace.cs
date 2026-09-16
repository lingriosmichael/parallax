using UnityEngine;

namespace Parallax.Core
{
    public enum SortingBand { Background, Middle, Gameplay, Foreground }

    public static class RealitySpace
    {
        public static readonly Vector2 OffsetB = new Vector2(0f, 1000f);

        public static Vector2 Origin(ObserverId reality) =>
            reality == ObserverId.A ? Vector2.zero : OffsetB;

        public static Vector2 MapTo(ObserverId from, ObserverId to, Vector2 worldPos) =>
            worldPos - Origin(from) + Origin(to);

        public static string PhysicsLayerName(ObserverId reality) =>
            reality == ObserverId.A ? "RealityA" : "RealityB";

        public static string SortingLayerName(ObserverId reality, SortingBand band)
        {
            string prefix = reality == ObserverId.A ? "A" : "B";
            return $"{prefix}_{band}";
        }
    }
}
