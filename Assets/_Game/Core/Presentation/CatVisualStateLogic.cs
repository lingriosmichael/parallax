using UnityEngine;

namespace Parallax.Core
{
    public enum CatVisualState : byte { Idle, Walk, Air }

    public static class CatVisualStateLogic
    {
        public static CatVisualState Select(
            CatVisualState current,
            float alongSpeed,
            float upSpeed,
            float dt,
            float idleSpeedThreshold,
            float airThreshold,
            float idleDwell,
            bool teleported,
            ref float belowIdleTime)
        {
            if (teleported)
            {
                belowIdleTime = 0f;
                return CatVisualState.Idle;
            }

            if (Mathf.Abs(upSpeed) > airThreshold)
            {
                belowIdleTime = 0f;
                return CatVisualState.Air;
            }

            if (Mathf.Abs(alongSpeed) > idleSpeedThreshold)
            {
                belowIdleTime = 0f;
                return CatVisualState.Walk;
            }

            belowIdleTime += Mathf.Max(0f, dt);
            return current == CatVisualState.Walk && belowIdleTime < idleDwell
                ? CatVisualState.Walk
                : CatVisualState.Idle;
        }

        public static bool ShouldFlip(float alongSpeed, float hysteresis, bool facingRight) =>
            Mathf.Abs(alongSpeed) > hysteresis && (alongSpeed > 0f) != facingRight;
    }
}
