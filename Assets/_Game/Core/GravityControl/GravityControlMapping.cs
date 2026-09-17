using UnityEngine;

namespace Parallax.Core
{
    public static class GravityControlMapping
    {
        public static Vector2 ToDirection(float value, float maxAngleDeg, float snapDeg)
        {
            float angle = Mathf.Clamp(value, -1f, 1f) * maxAngleDeg;
            if (snapDeg > 0f) angle = Mathf.Round(angle / snapDeg) * snapDeg;
            return (Quaternion.Euler(0f, 0f, -angle) * Vector2.down).normalized;
        }
    }
}
