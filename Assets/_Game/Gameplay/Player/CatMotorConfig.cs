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

        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float MaxFallSpeed => maxFallSpeed;
    }
}
