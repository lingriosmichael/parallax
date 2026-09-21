using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GravityReceiver : MonoBehaviour
    {
        [SerializeField] Vector2 initialDirection = Vector2.down;
        [SerializeField] float strength = 30f;

        Vector2 target;
        public Vector2 Direction { get; private set; }
        public Vector2 TargetDirection => target;
        public float Strength => strength;
        public GravitySide Side => VerticalGravity.SideOf(Direction, GravitySide.Down);

        void Awake()
        {
            Direction = target = VerticalGravity.Quantize(initialDirection, Vector2.down);
        }

        public void SetTargetDirection(Vector2 dir, bool snap = false)
        {
            target = VerticalGravity.Quantize(dir, target);
            if (snap) Direction = target;
        }

        public void FixedTick(float dt)
        {
            Direction = target;
        }

        public void Flip()
        {
            GravitySide targetSide = VerticalGravity.SideOf(target, GravitySide.Down);
            SetTargetDirection(VerticalGravity.ToVector(VerticalGravity.Flip(targetSide)));
        }
    }
}
