using UnityEngine;

namespace Parallax.Core
{
    public static class GravityFrame
    {
        public static Vector2 Right(Vector2 gravityDown) => new Vector2(-gravityDown.y, gravityDown.x);

        public static Vector2 Up(Vector2 gravityDown) => -gravityDown;

        public static float Along(Vector2 velocity, Vector2 gravityDown) =>
            Vector2.Dot(velocity, Right(gravityDown));

        public static float UpSpeed(Vector2 velocity, Vector2 gravityDown) =>
            Vector2.Dot(velocity, Up(gravityDown));

        /// <summary>PAX-107 (D-118): a world side (+1 right, -1 left) as a side in the gravity frame (+1 the frame's right).
        /// With gravity up the frame's right is world left, so the side flips.</summary>
        public static int LocalSide(int worldSide, Vector2 gravityDown) =>
            Right(gravityDown).x < 0f ? -worldSide : worldSide;

        public static float RotationAngle(Vector2 gravityDown) =>
            Vector2.SignedAngle(Vector2.down, gravityDown);
    }
}
