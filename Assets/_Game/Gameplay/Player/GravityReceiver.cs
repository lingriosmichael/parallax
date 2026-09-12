using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GravityReceiver : MonoBehaviour
    {
        [SerializeField] Vector2 initialDirection = Vector2.down;
        [SerializeField] float strength = 30f;
        [SerializeField] float turnSpeedDegPerSec = 360f;

        Vector2 target;
        public Vector2 Direction { get; private set; }
        public Vector2 TargetDirection => target;
        public float Strength => strength;

        void Awake()
        {
            Direction = target = initialDirection.normalized;
        }

        public void SetTargetDirection(Vector2 dir, bool snap = false)
        {
            if (dir.sqrMagnitude <= 1e-4f) return;

            target = dir.normalized;
            if (snap) Direction = target;
        }

        public void FixedTick(float dt)
        {
            float cur  = Vector2.SignedAngle(Vector2.down, Direction);
            float goal = Vector2.SignedAngle(Vector2.down, target);
            float next = Mathf.MoveTowardsAngle(cur, goal, turnSpeedDegPerSec * dt);
            Direction  = Quaternion.Euler(0f, 0f, next) * Vector2.down;
        }
    }
}
