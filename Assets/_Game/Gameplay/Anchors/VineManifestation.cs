using UnityEngine;

namespace Parallax.Gameplay.Anchors
{
    public sealed class VineManifestation : RealityManifestation
    {
        [SerializeField] Transform knot;
        [SerializeField] float pullDistance = 0.6f;
        [SerializeField] float speed = 3f;
        Vector3 restLocal;
        float target;
        bool initialized;

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            if (knot == null) Debug.LogError($"VineManifestation '{gameObject.name}' has no knot assigned.", this);
            else restLocal = knot.localPosition;
        }

        public override void SetTarget(float value, bool snap)
        {
            EnsureInit();
            target = Mathf.Clamp01(value);
            if (snap && knot != null) knot.localPosition = TargetPosition();
        }

        void FixedUpdate()
        {
            EnsureInit();
            if (knot != null) knot.localPosition = Vector3.MoveTowards(knot.localPosition, TargetPosition(), speed * Time.fixedDeltaTime);
        }

        Vector3 TargetPosition() => restLocal + Vector3.down * (pullDistance * target);
    }
}
