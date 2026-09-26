using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-087 (D-089): a climbable vine (KIT-8; not the frozen co-op VineInteractable/VineManifestation). Its
    /// trigger box is the grab box; CatClimber tests it with bounds math, never a physics query. A snap vine (snaps) fires
    /// through the ordinary trap timing (Overlap on its Trigger child, or its own box, or Chain; Once; delayTicks): it
    /// vanishes (every segment renderer off), can't be grabbed, and releases the LocalHuman cat in this room step, so the
    /// cat falls from the next motor step. The room reset restores it.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ClimbVine : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] bool snaps;
        [SerializeField] BoxCollider2D trigger;          // a snap's Overlap box; null: the vine's own box
        [SerializeField] int delayTicks;
        [SerializeField] SpriteRenderer[] segments = System.Array.Empty<SpriteRenderer>();

        BoxCollider2D box;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[16];

        public bool Snaps => snaps;
        public bool IsSnapped { get; private set; }
        public bool IsGrabbable => isActiveAndEnabled && !IsSnapped;
        protected override int DelayTicks => delayTicks;

        /// <summary>The grab box in world space, from the transform and the box (no physics sync needed).</summary>
        public Rect GrabRect
        {
            get
            {
                if (box == null) box = GetComponent<BoxCollider2D>();
                Vector2 centre = (Vector2)transform.position + box.offset;
                return new Rect(centre - box.size * .5f, box.size);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            box = GetComponent<BoxCollider2D>();
            if (Reality == null) Debug.LogError($"ClimbVine on '{name}': missing RealityRoot", this);
            if (snaps && observers == null) Debug.LogError($"ClimbVine on '{name}': a snap vine needs its ObserverSet", this);
            if (Reality != null) filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled || !snaps || IsSnapped || Reality == null || observers == null) return;
            bool overlapping = UsesOverlapSource && IsLocalHumanOverlapping(trigger != null ? trigger.bounds : (Bounds)ToBounds(GrabRect), observers, filter, results, out _);
            if (StepTiming(overlapping)) Snap();
        }

        void Snap()
        {
            IsSnapped = true;
            ApplyVisuals();
            ObserverContext observer = observers.Get(Reality.Id);
            if (observer != null && observer.Driver != null && observer.Driver.Kind == InputSourceKind.LocalHuman && observer.Cat != null)
                observer.Cat.ReleaseClimb(this);
        }

        // A Rearm snap vine grows back when its timing rearms (the validator allows Once only; this keeps it honest).
        protected override void OnTimingRearmed()
        {
            IsSnapped = false;
            ApplyVisuals();
        }

        protected override void OnReset()
        {
            IsSnapped = false;
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            foreach (SpriteRenderer segment in segments) if (segment != null) segment.enabled = !IsSnapped;
        }

        static Bounds ToBounds(Rect r) => new(r.center, r.size);
    }
}
