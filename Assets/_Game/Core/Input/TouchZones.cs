using UnityEngine;

namespace Parallax.Core
{
    public enum TouchZone : byte { None, MoveLeft, MoveRight, Jump }

    public static class TouchZones
    {
        /// `point` and the zone rects are in normalised safe-area coordinates (0..1).
        /// Tested in priority order: Jump, MoveLeft, MoveRight. Returns None if no hit.
        public static TouchZone Classify(Vector2 point, Rect moveLeft, Rect moveRight, Rect jump)
        {
            if (jump.Contains(point)) return TouchZone.Jump;
            if (moveLeft.Contains(point)) return TouchZone.MoveLeft;
            if (moveRight.Contains(point)) return TouchZone.MoveRight;
            return TouchZone.None;
        }

        /// Converts a pixel position to normalised coordinates within `safeArea`.
        /// Values outside the safe area are returned outside 0..1, not clamped.
        public static Vector2 ToSafeAreaNormalised(Vector2 screenPoint, Rect safeArea)
        {
            return new Vector2(
                (screenPoint.x - safeArea.x) / safeArea.width,
                (screenPoint.y - safeArea.y) / safeArea.height);
        }
    }
}
