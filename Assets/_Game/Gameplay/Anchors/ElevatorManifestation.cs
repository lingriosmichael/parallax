using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Anchors
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ElevatorManifestation : RealityManifestation
    {
        [SerializeField] Vector2 loweredLocal;
        [SerializeField] Vector2 raisedLocal;
        [SerializeField] float speed = 2f;
        Rigidbody2D body;
        RealityRoot root;
        float target;
        bool initialized;

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            body = GetComponent<Rigidbody2D>();
            if (body != null) body.bodyType = RigidbodyType2D.Kinematic;
            root = GetComponentInParent<RealityRoot>();
            if (root == null) Debug.LogError($"ElevatorManifestation '{gameObject.name}' has no RealityRoot parent.", this);
        }

        public override void SetTarget(float value, bool snap)
        {
            EnsureInit();
            target = Mathf.Clamp01(value);
            if (snap && body != null && root != null) body.position = ToWorld(TargetLocal());
        }

        void FixedUpdate()
        {
            EnsureInit();
            if (body == null || root == null) return;
            body.MovePosition(Vector2.MoveTowards(body.position, ToWorld(TargetLocal()), speed * Time.fixedDeltaTime));
        }

        Vector2 TargetLocal() => Vector2.Lerp(loweredLocal, raisedLocal, target);
        Vector2 ToWorld(Vector2 local) => (Vector2)root.transform.position + local;
    }
}
