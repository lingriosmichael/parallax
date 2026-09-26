using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-088 (D-090): a storm cloud. Dormant (a harmless-looking cloud, D-056 (4)) until its trigger fires
    /// (Overlap or Chain, Once). Awake, each room step it follows the LocalHuman cat's x at followSpeed within its range, then
    /// every cycle stops and charges (darker, a faint target line down its locked x; harmless), then strikes: a lethal
    /// column strikeWidth wide from its bottom to the first static top under it, from the profile the builder baked (no
    /// physics queries). The cat's x and box come from its Rigidbody2D (position, rotation) and collider offset and size,
    /// read in the room step, before this tick's physics: never the Transform, which Play interpolates (§11 Q1, ruling C).
    /// Everything is an offset from this object's authored pose, which never moves; the "Cloud", "Target" and "Bolt"
    /// children do. The phase is a function of room ticks since the wake, so the death hold and the pause freeze it; the
    /// room reset returns it to its authored pose, dormant.</summary>
    public sealed class StormCloudTrap : RoomTrap
    {
        const float TargetWidth = .08f;

        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] SpriteRenderer cloud, target, bolt;
        [SerializeField] Vector2 cloudSize = new(2f, .8f);
        [Tooltip("The range of the cloud's x, as offsets from its authored x.")]
        [SerializeField] float rangeMin, rangeMax;
        [SerializeField] float followSpeed = StormCloudMath.DefaultFollowSpeed, strikeWidth = StormCloudMath.DefaultStrikeWidth;
        [SerializeField] int firstStrikeDelay = StormCloudMath.DefaultFirstStrikeDelay, strikePeriod = StormCloudMath.DefaultStrikePeriod;
        [SerializeField] int tellTicks = StormCloudMath.DefaultTellTicks, strikeTicks = StormCloudMath.DefaultStrikeTicks;
        [SerializeField] int delayTicks;
        [Tooltip("Static tops (xMin, xMax, top), as offsets from the authored pose. Baked by the builder from Floor, Wall, PitBottom and Ceiling.")]
        [SerializeField] Vector3[] profile = new Vector3[0];
        [SerializeField] Color chargeColor = new(.25f, .25f, .32f, 1f);

        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];
        Color idleColor;
        Component cachedCat; Rigidbody2D cachedBody; Collider2D cachedCollider;

        public StormCloudPhase Phase { get; private set; }
        /// <summary>The cloud's x as an offset from its authored x.</summary>
        public float OffsetX { get; private set; }
        public SpriteRenderer Cloud => cloud;
        public SpriteRenderer Target => target;
        public SpriteRenderer Bolt => bolt;

        float CloudBottom => -cloudSize.y * .5f;

        /// <summary>The column under the cloud's current x, in world space (what charges, then strikes).</summary>
        public Rect StrikeColumn
        {
            get
            {
                Vector2 origin = transform.position;
                Rect local = LocalColumn(strikeWidth * .5f);
                return new Rect(local.position + origin, local.size);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Reality == null || observers == null || cloud == null || (UsesOverlapSource && trigger == null))
            { Debug.LogError($"StormCloudTrap '{name}': missing reality, observers, cloud renderer, or overlap trigger.", this); enabled = false; return; }
            if (profile == null || profile.Length == 0) Debug.LogError($"StormCloudTrap '{name}': no baked strike profile; its strikes have no floor to stop on (D-090).", this);
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            idleColor = cloud.color;
            OffsetX = 0f;
            Phase = StormCloudPhase.Dormant;
            ApplyVisuals();
        }

        protected override int DelayTicks => delayTicks;

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            int since = LatestFireTick < 0 ? -1 : RoomLifeTick - LatestFireTick;
            Phase = StormCloudMath.PhaseSince(since, firstStrikeDelay, strikePeriod, tellTicks, strikeTicks);
            if (Phase == StormCloudPhase.Follow && TryGetCat(out Rigidbody2D body, out Collider2D collider))
                OffsetX = StormCloudMath.Follow(OffsetX, CatBox(body, collider).center.x - transform.position.x, followSpeed, rangeMin, rangeMax);
            ApplyVisuals();
            if (Phase == StormCloudPhase.Strike && TryGetCat(out body, out collider) && StrikeHits(body, collider))
                Death.Kill(Reality.Id, DeathCause.Hazard);
        }

        /// <summary>This tick's kill test (ruling C): true only while striking, when the cat's body box strictly overlaps the
        /// column. The route harness names the killer through this same function.</summary>
        public bool StrikeHits(Rigidbody2D body, Collider2D collider) =>
            enabled && Phase == StormCloudPhase.Strike && body != null && collider != null && StormCloudMath.Hits(StrikeColumn, CatBox(body, collider));

        static Rect CatBox(Rigidbody2D body, Collider2D collider) =>
            StormCloudMath.CatBox(body.position, body.rotation, collider.offset, ColliderSize(collider));

        static Vector2 ColliderSize(Collider2D collider) => collider switch
        {
            BoxCollider2D box => box.size,
            CapsuleCollider2D capsule => capsule.size,
            CircleCollider2D circle => Vector2.one * (2f * circle.radius),
            _ => Vector2.zero,
        };

        // The body and the first non-trigger collider, looked up once per cat (no per-tick allocation).
        bool TryGetCat(out Rigidbody2D body, out Collider2D collider)
        {
            body = null; collider = null;
            ObserverContext observer = observers.Get(Reality.Id);
            if (observer == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman || observer.Cat == null) return false;
            if (cachedCat != observer.Cat)
            {
                cachedCat = observer.Cat;
                cachedBody = cachedCat.GetComponent<Rigidbody2D>();
                cachedCollider = null;
                foreach (Collider2D c in cachedCat.GetComponents<Collider2D>()) if (!c.isTrigger) { cachedCollider = c; break; }
            }
            body = cachedBody; collider = cachedCollider;
            return body != null && collider != null;
        }

        // The column in offsets from the authored pose; with no static top below (the validator forbids it) it runs 50 u down.
        Rect LocalColumn(float halfWidth)
        {
            float bottom = StormCloudMath.StrikeBottom(profile, OffsetX, strikeWidth * .5f, CloudBottom, out float top) ? top : CloudBottom - 50f;
            return StormCloudMath.Column(OffsetX, halfWidth, bottom, CloudBottom);
        }

        void ApplyVisuals()
        {
            if (cloud == null) return;
            bool charging = Phase == StormCloudPhase.Charge, striking = Phase == StormCloudPhase.Strike;
            cloud.transform.localPosition = new Vector3(OffsetX, 0f, 0f);
            cloud.color = charging || striking ? chargeColor : idleColor;
            if (target != null) { target.enabled = charging; if (charging) Place(target, LocalColumn(TargetWidth * .5f)); }
            if (bolt != null) { bolt.enabled = striking; if (striking) Place(bolt, LocalColumn(strikeWidth * .5f)); }
        }

        static void Place(SpriteRenderer renderer, Rect local)
        {
            renderer.transform.localPosition = new Vector3(local.center.x, local.center.y, 0f);
            if (renderer.drawMode != SpriteDrawMode.Simple) renderer.size = local.size;
        }

        // Disabled mid-chase: drawn at its authored pose, dormant, as the room reset leaves it.
        protected override void OnDisable()
        {
            OffsetX = 0f;
            Phase = StormCloudPhase.Dormant;
            ApplyVisuals();
            base.OnDisable();
        }

        protected override void OnReset()
        {
            OffsetX = 0f;
            Phase = StormCloudPhase.Dormant;
            ApplyVisuals();
        }
    }
}
