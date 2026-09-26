using UnityEngine;

namespace Parallax.Core
{
    public enum StickProjection { CatRelative, ScreenRelative }

    public static class VirtualStick
    {
        /// Returns the stick vector in screen space, magnitude 0..1.
        /// Zero inside `deadZone`; rescaled so the dead zone edge maps to 0 and `radius` maps to 1.
        public static Vector2 Evaluate(Vector2 origin, Vector2 current, float radius, float deadZone)
        {
            Vector2 delta = current - origin;
            float distance = delta.magnitude;

            float deadZoneDistance = deadZone * radius;
            if (distance <= deadZoneDistance || radius <= 0f) return Vector2.zero;

            float rescaled = (distance - deadZoneDistance) / (radius - deadZoneDistance);
            rescaled = Mathf.Clamp01(rescaled);

            return delta.normalized * rescaled;
        }

        // Collapses the 2D stick to CatCommand.Move in [-1, 1].
        //
        // The camera is world-aligned and never rotates (D-020), so world
        // orientation equals screen orientation and no camera transform is
        // needed here. If Q-7 ever puts a rotating camera behind the world,
        // `stick` must first be rotated into that camera's screen space or
        // ScreenRelative will read the wrong axis.
        public static float ToMove(Vector2 stick, Vector2 catRight, StickProjection mode)
        {
            float value = mode == StickProjection.CatRelative
                ? stick.x
                : Vector2.Dot(stick, catRight);

            return Mathf.Clamp(value, -1f, 1f);
        }

        /// PAX-087 (D-089): the climb dead zone on the stick's y, higher than x's so a slightly diagonal run never climbs.
        public const float DefaultClimbDeadZone = .35f;

        // PAX-087 (D-089) R2: the stick's screen y (already dead-zoned and rescaled by Evaluate) to CatCommand.Climb,
        // screen-up positive. 0 inside `deadZone`; rescaled so its edge maps to 0 and full y to 1.
        public static float ToClimb(Vector2 stick, float deadZone)
        {
            float y = Mathf.Abs(stick.y);
            if (y <= deadZone || deadZone >= 1f) return 0f;
            return Mathf.Sign(stick.y) * Mathf.Clamp01((y - deadZone) / (1f - deadZone));
        }
    }
}
