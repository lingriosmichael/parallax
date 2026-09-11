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

        [Tooltip("Minimum alignment (dot product) between a hit's normal and -gravity for it to count as ground.")]
        [SerializeField] float groundNormalThreshold = 0.7f;

        [Tooltip("Layers considered ground by the ground cast.")]
        [SerializeField] LayerMask groundMask = ~0;

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float MaxFallSpeed => maxFallSpeed;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float GroundProbeDistance => groundProbeDistance;
        public float GroundNormalThreshold => groundNormalThreshold;
        public LayerMask GroundMask => groundMask;
    }
}
