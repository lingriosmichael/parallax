using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [CreateAssetMenu(menuName = "PARALLAX/Cat Motor Config")]
    public sealed class CatMotorConfig : ScriptableObject
    {
        [Tooltip("Top horizontal speed along the axis perpendicular to gravity, in units/second.")]
        [SerializeField] float maxSpeed = 6f;

        [Tooltip("Horizontal acceleration toward the commanded speed, in units/second^2.")]
        [SerializeField] float acceleration = 60f;

        [Tooltip("Horizontal deceleration applied when there is no move command, in units/second^2.")]
        [SerializeField] float deceleration = 80f;

        [Tooltip("Maximum fall speed along the gravity direction, in units/second.")]
        [SerializeField] float maxFallSpeed = 20f;

        [Tooltip("Peak height of a jump, in world units. Converted to launch speed via JumpMath.")]
        [SerializeField] float jumpHeight = 3.2f;

        [Tooltip("Time after leaving the ground, in seconds, during which a jump is still allowed.")]
        [SerializeField] float coyoteTime = 0.10f;

        [Tooltip("Time before landing, in seconds, during which a jump press is remembered and fires on landing.")]
        [SerializeField] float jumpBufferTime = 0.12f;

        [Tooltip("Distance, in world units, the ground cast probes below the cat along gravity.")]
        [SerializeField] float groundProbeDistance = 0.05f;

        [Tooltip("Horizontal capsule dimensions for every cat, in world units.")]
        [SerializeField] Vector2 colliderSize = new Vector2(1f, 0.56f);

        [Tooltip("Horizontal capsule centre relative to the cat root, in world units.")]
        [SerializeField] Vector2 colliderOffset = new Vector2(0f, -0.12f);

        [Tooltip("Minimum alignment (dot product) between a hit's normal and -gravity for it to count as ground.")]
        [SerializeField] float groundNormalThreshold = 0.7f;

        [Tooltip("PAX-087 (D-089): climbing speed at full Climb, in units/second (screen-vertical).")]
        [SerializeField] float climbSpeed = Parallax.Core.ClimbState.DefaultClimbSpeed;

        [Tooltip("PAX-087 (D-089): motor steps after a release or leap during which the same vine can't be grabbed again.")]
        [SerializeField] int regrabLockTicks = Parallax.Core.ClimbState.DefaultRegrabLockTicks;

        [Tooltip("PAX-087 (D-089): the Climb magnitude that grabs a vine (up only while grounded).")]
        [SerializeField] float grabThreshold = Parallax.Core.ClimbState.DefaultGrabThreshold;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float MaxFallSpeed => maxFallSpeed;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float GroundProbeDistance => groundProbeDistance;
        public Vector2 ColliderSize => colliderSize;
        public Vector2 ColliderOffset => colliderOffset;
        public float ColliderBottom => colliderOffset.y - colliderSize.y * 0.5f;
        public float GroundNormalThreshold => groundNormalThreshold;
        public float ClimbSpeed => climbSpeed;
        public int RegrabLockTicks => regrabLockTicks;
        public float GrabThreshold => grabThreshold;
    }
}
