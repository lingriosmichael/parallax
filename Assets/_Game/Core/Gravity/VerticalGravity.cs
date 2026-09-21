using UnityEngine;

namespace Parallax.Core
{
    public static class VerticalGravity
    {
        public const float Threshold = 0.1f;
        const float VerticalTolerance = 0.0001f;

        public static Vector2 ToVector(GravitySide side)
        {
            return side == GravitySide.Up ? Vector2.up : Vector2.down;
        }

        public static GravitySide SideOf(Vector2 direction, GravitySide current)
        {
            if (direction.y < -Threshold) return GravitySide.Down;
            if (direction.y > Threshold) return GravitySide.Up;
            return current;
        }

        public static Vector2 Quantize(Vector2 direction, Vector2 current)
        {
            GravitySide currentSide = SideOf(current, GravitySide.Down);
            return ToVector(SideOf(direction, currentSide));
        }

        public static GravitySide Flip(GravitySide side)
        {
            return side == GravitySide.Up ? GravitySide.Down : GravitySide.Up;
        }

        public static bool IsVertical(Vector2 direction)
        {
            return Vector2.Distance(direction, Vector2.down) <= VerticalTolerance
                || Vector2.Distance(direction, Vector2.up) <= VerticalTolerance;
        }
    }
}
