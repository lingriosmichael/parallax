using UnityEngine;

namespace Parallax.Core
{
    public enum ArrowDirection { Right, Left }

    /// <summary>PAX-074 (D-078): the arrow as a pure function of s, the ticks since its fire (s = 0
    /// is the fire tick). Tell for s in [0, T-1]: visible at the mouth, harmless. Lethal for s in
    /// [T, T+N], N = FlightTicks: the first lethal pose is the tell pose, the last is the arrival at the
    /// lane end. Stopped from T+N+1: harmless, visible until reset or rearm. Offsets are measured from
    /// the mouth (the launcher face) along the fire direction to the arrow's back face.</summary>
    public static class ArrowMath
    {
        // Keeps N stable when travel / v is a whole number (6.0 / 0.3f is 20.0000008 in float).
        public const float FlightEpsilon = 1e-4f;

        public static float Sign(ArrowDirection direction) => direction == ArrowDirection.Left ? -1f : 1f;

        // Distance the arrow's back face covers from the mouth until its front face meets the lane end.
        public static float Travel(float mouthX, float laneEndX, float length) => Mathf.Abs(laneEndX - mouthX) - length;

        public static int FlightTicks(float travel, float unitsPerTick) =>
            travel <= 0f || unitsPerTick <= 0f ? 0 : Mathf.CeilToInt(travel / unitsPerTick - FlightEpsilon);

        public static bool IsTell(int s, int tellTicks) => s >= 0 && s < tellTicks;
        public static bool IsLethal(int s, int tellTicks, int flightTicks) => s >= tellTicks && s <= tellTicks + flightTicks;
        public static bool IsStopped(int s, int tellTicks, int flightTicks) => s > tellTicks + flightTicks;

        public static float Offset(int s, int tellTicks, float unitsPerTick, float travel) => TrapMotion.Travel(s - tellTicks, unitsPerTick, travel);

        public static float CentreX(float mouthX, ArrowDirection direction, float length, float offset) => mouthX + Sign(direction) * (offset + length * .5f);

        // No pass-through between two samples: per tick the arrow and a cat running towards it close
        // v + run; they must overlap on some tick while that stays within the arrow's length plus the
        // capsule's narrowest width in the band (its straight section, width - height). One more run
        // tick is kept as margin.
        public static float MaxUnitsPerTick(float length, Vector2 colliderSize, float runPerTick) =>
            length + (colliderSize.x - colliderSize.y) - 2f * runPerTick;
    }
}
