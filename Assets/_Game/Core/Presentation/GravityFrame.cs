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

        public static float RotationAngle(Vector2 gravityDown) =>
            Vector2.SignedAngle(Vector2.down, gravityDown);
    }
}
