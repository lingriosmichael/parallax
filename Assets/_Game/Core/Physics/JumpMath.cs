using System;

namespace Parallax.Core
{
    public static class JumpMath
    {
        public static float SpeedForHeight(float height, float gravityStrength)
        {
            if (height <= 0f || gravityStrength <= 0f) return 0f;
            return (float)Math.Sqrt(2f * gravityStrength * height);
        }
    }
}
