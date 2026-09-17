using UnityEngine;

namespace Parallax.Core
{
    public static class DialMath
    {
        public static float ValueFromPointer(Vector2 center, Vector2 pointer, float maxDialDeg)
        {
            Vector2 delta = pointer - center;
            if (delta.magnitude < 8f) return float.NaN;
            return Mathf.Clamp(-Vector2.SignedAngle(Vector2.up, delta) / maxDialDeg, -1f, 1f);
        }
    }
}
