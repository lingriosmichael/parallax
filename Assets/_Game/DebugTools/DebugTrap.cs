using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.DebugTools
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class DebugTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] Vector2 zoneSize = new Vector2(0.8f, 0.8f);

        BoxCollider2D box;
        SpriteRenderer visual;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];

        protected override void Awake()
        {
            base.Awake();
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = zoneSize;
            visual = GetComponent<SpriteRenderer>();
            filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << gameObject.layer, useTriggers = false };
            ApplyVisual();
        }

        protected override void OnLiveRoomStep()
        {
            if (State != TrapState.Armed) return;
            if (observers == null) return;
            ObserverContext observer = observers.Get(ObserverId.A);
            if (observer == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman || observer.Cat == null) return;
            Collider2D catCollider = observer.Cat.GetComponent<Collider2D>();
            if (catCollider == null) return;
            int count = Physics2D.OverlapBox(transform.position, box.size, transform.eulerAngles.z, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] != catCollider) continue;
                State = TrapState.Fired;
                ApplyVisual();
                return;
            }
        }

        protected override void OnReset() => ApplyVisual();

        void ApplyVisual()
        {
            if (visual != null) visual.color = State == TrapState.Armed ? Color.gray : new Color(1f, 0.45f, 0f);
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, State.ToString().ToUpperInvariant());
#endif
        }
    }
}
