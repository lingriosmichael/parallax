using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-085 (D-087): touching it swaps the LocalHuman cat's left and right for durationTicks motor steps.
    /// LocalHumanDriver negates the motor's Move while InvertsMove is true; jump is untouched. The timer counts room
    /// ticks, so the death hold and the pause freeze it; the room reset and the room ending (no longer live) clear it.
    /// The cue (ring behind the cat, mark over it) is switched in the room step and follows the cat in LateUpdate.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class InverterTrap : RoomTrap, IControlModifier
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;          // its own body, or a separate child box
        [SerializeField] SpriteRenderer orb;             // the honest look; null when disguised
        [SerializeField] SpriteRenderer ring;
        [SerializeField] SpriteRenderer mark;
        [SerializeField] int durationTicks = ControlInversion.DefaultDurationTicks;
        [SerializeField] int delayTicks;
        [SerializeField] float markHeight = .75f;        // above the cat's collider centre, against gravity

        readonly ControlInversion inversion = new ControlInversion();
        readonly Collider2D[] results = new Collider2D[8];
        ContactFilter2D filter;
        Collider2D catCollider;   // presentation only: where LateUpdate draws the cue

        public SpriteRenderer Ring => ring;
        public SpriteRenderer Mark => mark;
        public bool IsCueShown => ring != null && ring.enabled;

        // R3: only while this room is live, the death hold isn't running, and the upcoming motor step (the next room
        // tick) is inside the window.
        public bool InvertsMove => enabled && IsRoomLive && Death != null && !Death.IsHolding && inversion.IsActive(RoomLifeTick + 1);

        protected override int DelayTicks => delayTicks;

        protected override void Awake()
        {
            base.Awake();
            if (trigger == null) trigger = GetComponent<BoxCollider2D>();
            if (Reality == null) Debug.LogError($"InverterTrap on '{name}': missing RealityRoot", this);
            if (observers == null) Debug.LogError($"InverterTrap on '{name}': missing ObserverSet", this);
            if (ring == null || mark == null) Debug.LogError($"InverterTrap on '{name}': missing cue renderers (ring, mark)", this);
            if (Reality == null || observers == null) { enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            ApplyVisuals(-1);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (enabled && observers != null) observers.Stepped += OnStepped;
        }

        protected override void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
            // Latched state goes with the component: no cue left on the cat, no window to resume.
            inversion.Clear();
            ApplyVisuals(-1);
            base.OnDisable();
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            bool overlap = trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _);
            if (StepTiming(overlap)) inversion.Fire(RoomLifeTick, durationTicks);
            ApplyVisuals(RoomLifeTick);
        }

        protected override void OnReset()
        {
            inversion.Clear();
            ApplyVisuals(-1);
        }

        // The room ended (door, level complete) with the window still open: nothing steps a room that isn't live, so
        // the timer and the cue are cleared here: in the same tick when this runs after RoomManager's step, otherwise on
        // the next (Stepped subscription order). InvertsMove is false from the tick the room stops being live either way.
        void OnStepped(int tick)
        {
            if (IsRoomLive || !inversion.HasFired) return;
            inversion.Clear();
            ApplyVisuals(-1);
        }

        void ApplyVisuals(int roomTick)
        {
            bool cue = inversion.IsCueVisible(roomTick);
            if (ring != null) ring.enabled = cue;
            if (mark != null) mark.enabled = cue;
            if (orb != null) orb.enabled = State == TrapState.Armed;
        }

        // Presentation only: nothing in the game or the route harness reads these positions.
        void LateUpdate()
        {
            if (!IsCueShown || observers == null || Reality == null) return;
            ObserverContext observer = observers.Get(Reality.Id);
            if (observer == null || observer.Cat == null) return;
            if (catCollider == null || catCollider.gameObject != observer.Cat.gameObject) catCollider = observer.Cat.GetComponent<Collider2D>();
            Vector3 centre = catCollider != null ? catCollider.bounds.center : observer.Cat.transform.position;
            Vector2 up = observer.Gravity != null ? -observer.Gravity.Direction : Vector2.up;
            ring.transform.position = new Vector3(centre.x, centre.y, ring.transform.position.z);
            // The mark stays upright (it never rotates); it sits on the cat's head side.
            mark.transform.position = new Vector3(centre.x + up.x * markHeight, centre.y + up.y * markHeight, mark.transform.position.z);
        }
    }
}
