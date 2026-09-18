using UnityEngine;

namespace Parallax.Core
{
    public struct EchoFrame
    {
        public Vector2 Position;
        public float Rotation;
        public Vector2 GravityDirection;
        // Legacy, not written or read since D-034; presentation derives facing from motion.
        public bool FacingRight;
    }
}
