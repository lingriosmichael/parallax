using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-086 (D-088): a vent flush in a Floor (Up) or a Ceiling (Down) that erupts on a Periodic cycle. The fire
    /// tick is the tell's first tick (the vent turns tellColor, harmless); then, for eruptTicks, a LocalHuman cat whose
    /// collider overlaps the column (columnWidth x columnHeight from the vent's face) has its velocity along the push set to
    /// launchSpeed through CatMotor2D.ApplyLaunch, in this room step: after the tick's motor step, before physics. The
    /// phase is a pure function of room ticks since the latest fire, so the death hold and the pause freeze it and the room
    /// reset restarts it from room start. The vent itself is a trigger: the host Floor or Ceiling holds the cat.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GeyserTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] SpriteRenderer vent;            // its authored colour is the idle look
        [SerializeField] SpriteRenderer column;          // shown while erupting
        [SerializeField] GeyserDirection direction;
        [SerializeField] float columnWidth = GeyserMath.DefaultColumnWidth, columnHeight = GeyserMath.DefaultColumnHeight;
        [SerializeField] int tellTicks = GeyserMath.DefaultTellTicks, eruptTicks = GeyserMath.DefaultEruptTicks;
        [SerializeField] float launchSpeed = GeyserMath.DefaultLaunchSpeed;
        [SerializeField] Color tellColor = new(1f, .55f, .15f, 1f);

        BoxCollider2D box;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[16];
        Color idleColor;

        public GeyserPhase Phase { get; private set; }
        public SpriteRenderer Vent => vent;
        public SpriteRenderer Column => column;

        /// <summary>The column in world space, from the vent's face along the push.</summary>
        public Bounds ColumnBounds
        {
            get
            {
                if (box == null) box = GetComponent<BoxCollider2D>();
                Vector2 push = GeyserMath.Push(direction);
                Vector2 centre = (Vector2)transform.position + box.offset + push * (box.size.y * .5f + columnHeight * .5f);
                return new Bounds(centre, new Vector3(columnWidth, columnHeight));
            }
        }

        protected override void Awake()
        {
            base.Awake();
            box = GetComponent<BoxCollider2D>();
            if (Reality == null) Debug.LogError($"GeyserTrap on '{name}': missing RealityRoot", this);
            if (observers == null) Debug.LogError($"GeyserTrap on '{name}': missing ObserverSet", this);
            if (!IsPeriodic) Debug.LogError($"GeyserTrap on '{name}': repeat mode must be Periodic (D-088); it never erupts otherwise.", this);
            if (Reality == null || observers == null) { enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            if (vent != null) idleColor = vent.color;
            ApplyVisuals();
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(false);
            Phase = GeyserMath.PhaseSince(LatestFireTick < 0 ? -1 : RoomLifeTick - LatestFireTick, tellTicks, eruptTicks);
            ApplyVisuals();
            if (Phase != GeyserPhase.Erupt) return;
            if (IsLocalHumanOverlapping(ColumnBounds, observers, filter, results, out ObserverContext observer))
                observer.Cat.ApplyLaunch(GeyserMath.Push(direction), launchSpeed);
        }

        // Disabled mid-cycle: nothing left drawn, and the phase starts from idle if it's enabled again.
        protected override void OnDisable()
        {
            Phase = GeyserPhase.Idle;
            ApplyVisuals();
            base.OnDisable();
        }

        protected override void OnReset()
        {
            Phase = GeyserPhase.Idle;
            ApplyVisuals();
        }

        void ApplyVisuals()
        {
            if (vent != null) vent.color = Phase == GeyserPhase.Idle ? idleColor : tellColor;
            if (column != null) column.enabled = Phase == GeyserPhase.Erupt;
        }
    }
}
