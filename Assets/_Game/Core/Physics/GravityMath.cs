using UnityEngine;

namespace Parallax.Core
{
    public static class GravityMath
    {
        public static Vector2 Rotate90(Vector2 dir, int steps)
        {
            int k = ((steps % 4) + 4) % 4;

            Vector2 result;
            switch (k)
            {
                case 1:  result = new Vector2(-dir.y, dir.x);  break; // 90 CCW
                case 2:  result = new Vector2(-dir.x, -dir.y); break; // 180
                case 3:  result = new Vector2(dir.y, -dir.x);  break; // 270 CCW (90 CW)
                default: result = dir;                         break; // 0
            }

            return result.normalized;
        }
    }
}
