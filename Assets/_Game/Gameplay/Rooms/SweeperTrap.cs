using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-124 (D-276): a rotating chaser. A laser bar through a pivot (two arms of armLength, opposite each other) that,
    /// from its fire, warns warnTicks at home (harmless, as a scanner does), then moves its pivot along offset over moveTicks while
    /// turning degreesPerTick, and stays at the end of its path, still turning. D-276 amendment 1: it may hold its angle until
    /// spinFromTicks after its fire and then speed up to its turn over spinRampTicks (a chaser that starts to tilt part way). Each arm is a beam from the pivot outward that stops at
    /// the first fixed solid (a raycast against the reality's colliders, the cat's, its own and every trap's excepted): stone between
    /// the pivot and the cat is cover; a trap's floor (glass, a plate, a hologram) isn't. The pose is a function of ticks since the fire (TrapPath.SweepProgress,
    /// SweepAngle), so the death hold freezes it and a reset and a rewind restore it exactly (D-091). The trap object and its trigger
    /// stay where they were built; the "Pivot" child carries the hub and arms. The kill is an oriented box per arm (also the route
    /// harness's).</summary>
    public sealed class SweeperTrap : RoomTrap
    {
        public const int Arms = 2;

        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] Transform pivot;
        [SerializeField] SpriteRenderer[] arms = new SpriteRenderer[Arms];
        [SerializeField] Vector2 offset;
        [SerializeField] int warnTicks = 50, moveTicks = 600, delayTicks;
        [SerializeField] float armLength = 2.5f, armWidth = .3f, degreesPerTick = 3f, startAngle = 90f;
        [Tooltip("D-276 amendment 1: ticks after the fire when the bar starts to turn (-1: as its warning ends), and over how many it speeds up to degreesPerTick (0: at once).")]
        [SerializeField] int spinFromTicks = -1, spinRampTicks;
        [SerializeField] Color idleColor = new(.85f, .95f, 1f, .25f), warnColor = new(.85f, .95f, 1f, .7f), liveColor = new(.9f, .97f, 1f, 1f);

        ContactFilter2D filter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        readonly Collider2D[] results = new Collider2D[8];
        readonly float[] reach = new float[Arms];

        protected override int DelayTicks => delayTicks;
        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Zapped;

        public float Angle { get; private set; }
        /// <summary>Past its warning: the bar kills.</summary>
        public bool Live { get; private set; }
        public Vector2 Pivot => pivot != null ? (Vector2)pivot.position : (Vector2)transform.position;
        public float ArmAngle(int i) => TrapPath.ArmAngle(Angle, i, Arms);
        public float ArmReach(int i) => reach[i];
        static Vector2 Along(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        protected override void Awake()
        {
            base.Awake();
            if (observers == null || Reality == null || pivot == null || arms == null || arms.Length != Arms || System.Array.IndexOf(arms, null) >= 0 || (UsesOverlapSource && !IsPeriodic && trigger == null))
            { Debug.LogError($"SweeperTrap '{name}': missing observers, reality, pivot, its {Arms} arms or (a triggered, non-Periodic one) its trigger.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            Pose(RoomLifeTick);
        }

        int SinceFire(int roomTick) => LatestFireTick >= 0 ? roomTick - LatestFireTick : -1;

        // The distance along `direction` from the pivot to the first collider that isn't the cat or this trap (armLength when none).
        float Reach(Vector2 from, Vector2 direction)
        {
            Collider2D cat = TryGetCat(out Collider2D c) ? c : null;
            int count = Physics2D.Raycast(from, direction, filter, hits, armLength);
            float best = armLength;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider == cat || hits[i].collider.transform.IsChildOf(transform)) continue;
                // Fixed stone only: a trap's own solid (glass, a plate, a hologram) never shelters the cat, and a rewind restores the
                // traps in no set order, so a trap's pose must not decide the reach.
                if (hits[i].collider.GetComponentInParent<RoomTrap>() != null) continue;
                if (hits[i].distance < best) best = hits[i].distance;
            }
            return best;
        }

        bool TryGetCat(out Collider2D cat)
        {
            cat = null;
            ObserverContext o = observers != null && Reality != null ? observers.Get(Reality.Id) : null;
            if (o == null || o.Cat == null) return false;
            cat = Parallax.Gameplay.Player.CatBodyCollider.Of(o.Cat);
            return cat != null;
        }

        /// <summary>Places the pivot and the arms for `roomTick` (and their reach against the solids).</summary>
        void Pose(int roomTick)
        {
            int since = SinceFire(roomTick);
            Live = since >= warnTicks;
            Vector2 at = (Vector2)transform.position + offset * TrapPath.SweepProgress(since, warnTicks, moveTicks);
            Angle = since < 0 ? startAngle : TrapPath.SweepAngle(startAngle, degreesPerTick, since, spinFromTicks >= 0 ? spinFromTicks : warnTicks, spinRampTicks);
            if (pivot != null) pivot.position = new Vector3(at.x, at.y, pivot.position.z);
            for (int i = 0; i < Arms; i++)
            {
                float a = ArmAngle(i);
                reach[i] = Reach(at, Along(a));
                SpriteRenderer arm = arms[i];
                if (arm == null) continue;
                arm.transform.position = (Vector3)(at + Along(a) * (reach[i] * .5f)) + new Vector3(0f, 0f, arm.transform.position.z);
                arm.transform.rotation = Quaternion.Euler(0f, 0f, a);
                arm.size = new Vector2(Mathf.Max(reach[i], 1e-3f), armWidth);   // a sliced sprite: its drawn size
                Color c = since < 0 ? idleColor : Live ? liveColor : warnColor;
                if (!Live && since >= 0 && PulseGateMath.WarnLit(since)) c.a *= .5f;
                arm.color = c;
            }
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            Pose(RoomLifeTick);
            if (ArmHits()) Death.Kill(Reality.Id, DeathCause.Hazard, this);
        }

        /// <summary>A live arm overlaps the cat (the kill test, also the route harness's).</summary>
        public bool ArmHits()
        {
            if (!enabled || !Live) return false;
            Vector2 at = Pivot;
            for (int i = 0; i < Arms; i++)
            {
                float a = ArmAngle(i);
                if (reach[i] <= 1e-3f) continue;
                if (IsLocalHumanOverlapping(new Bounds(at + Along(a) * (reach[i] * .5f), new Vector3(reach[i], armWidth)), a, observers, filter, results, out _)) return true;
            }
            return false;
        }

        protected override void OnReset() => Pose(RoomLifeTick);
        protected override void OnTimingRearmed() => Pose(RoomLifeTick);
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Pose(roomTick);
    }
}
