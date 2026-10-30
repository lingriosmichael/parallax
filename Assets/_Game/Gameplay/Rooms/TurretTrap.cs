using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-121 (D-197): an aiming turret, hosted in a solid, on its Periodic timing. From each fire: aimTicks it tracks the
    /// LocalHuman cat's centre (a thin, harmless line); lockTicks it holds that angle and flickers (the tell); fireTicks a lethal beam
    /// runs along it to the first solid (a raycast against the reality's colliders, the cat's excepted); then it rests. The locked
    /// angle is kept by value (the snapshot's ExtraFloat) and the phase is a function of ticks, so a rewind restores it exactly
    /// (D-091). The kill is an oriented box test along the beam (also the route harness's). D-203: ceiling-mounted, it tracks only a
    /// cat below it in its sight; with an aim zone, only a cat inside it (its floor's standable area). A cycle in which it never tracks the cat shows nothing and
    /// doesn't fire (the developer: "it starts by aiming straight down why?"); once it has, it holds its last angle if it loses the
    /// cat, and locks and fires along it.</summary>
    public sealed class TurretTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] int aimTicks = 60, lockTicks = 12, fireTicks = 10;
        [SerializeField] float beamWidth = .3f, aimWidth = .06f, range = 40f;
        [SerializeField] bool ceilingMount;
        [SerializeField] Vector2 aimZoneMin, aimZoneSize;   // relative to the muzzle; zero size: anywhere
        [SerializeField] SpriteRenderer line;
        [SerializeField] Color aimColor = new(1f, .3f, .3f, .45f), lockColor = new(1f, .2f, .2f, .9f), fireColor = new(1f, .1f, .1f, 1f);

        ContactFilter2D filter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        readonly Collider2D[] results = new Collider2D[8];
        float angle;   // the line's angle (degrees): tracking while aiming, locked after
        float length;  // the line's length this step
        int seenFire = -1;  // the fire tick of the cycle in which it last tracked the cat (-1: none)
        bool Seen => IsTimingEffectActive && seenFire == LatestFireTick;

        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Spiked;

        protected override void Awake()
        {
            base.Awake();
            if (observers == null || Reality == null || line == null) { Debug.LogError($"TurretTrap '{name}': missing observers, reality or line.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            angle = RestAngle;
            Show(-1);
        }

        Vector2 Muzzle => transform.position;
        Vector2 Direction => new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        float RestAngle => ceilingMount ? -90f : 0f;

        // The distance along the line to the first collider that isn't the cat (range when none).
        float Reach() => Reach(Direction);
        float Reach(Vector2 direction)
        {
            Collider2D cat = TryCat(out Collider2D c) ? c : null;
            int count = Physics2D.Raycast(Muzzle, direction, filter, hits, range);
            float best = range;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider == cat || hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance > 1e-3f && hits[i].distance < best) best = hits[i].distance;
            }
            return best;
        }

        bool TryCat(out Collider2D cat)
        {
            cat = null;
            ObserverContext o = observers != null && Reality != null ? observers.Get(Reality.Id) : null;
            if (o == null || o.Cat == null) return false;
            cat = Parallax.Gameplay.Player.CatBodyCollider.Of(o.Cat);
            return cat != null;
        }

        int Phase(int roomTick) => TrapPath.TurretPhase(IsTimingEffectActive ? roomTick - LatestFireTick : -1, aimTicks, lockTicks, fireTicks);

        void Show(int phase)
        {
            if (line == null) return;
            line.enabled = phase >= 0 && phase <= 2 && Seen;
            if (!line.enabled) return;
            float width = phase == 2 ? beamWidth : aimWidth;
            line.transform.position = (Vector3)(Muzzle + Direction * (length * .5f)) + new Vector3(0f, 0f, line.transform.position.z);
            line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            line.size = new Vector2(length, width);   // a sliced sprite: its drawn size, not a scale
            Color c = phase == 0 ? aimColor : phase == 1 ? lockColor : fireColor;
            if (phase == 1 && (RoomLifeTick - LatestFireTick) % 2 == 1) c.a *= .5f;
            line.color = c;
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(false);
            int phase = Phase(RoomLifeTick);
            if (phase == 0 && TryGetLocalHumanColliderBounds(observers, out Bounds cat))
            {
                Vector2 to = (Vector2)cat.center - Muzzle;
                if (to.sqrMagnitude > 1e-6f && TrapPath.TurretTracks(to, ceilingMount, new Rect(aimZoneMin, aimZoneSize), ceilingMount ? Reach(to.normalized) : 0f))
                { angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg; seenFire = LatestFireTick; }
            }
            if (phase >= 0 && phase <= 2) length = Reach();
            Show(phase);
            if (phase == 2 && BeamHits()) Death.Kill(Reality.Id, DeathCause.Hazard, this);
        }

        /// <summary>The beam, while firing, overlaps the cat (the kill test, also the route harness's).</summary>
        public bool BeamHits()
        {
            if (!enabled || Phase(RoomLifeTick) != 2 || !Seen) return false;
            var beam = new Bounds(Muzzle + Direction * (length * .5f), new Vector3(length, beamWidth));
            return IsLocalHumanOverlapping(beam, angle, observers, filter, results, out _);
        }

        protected override void CaptureExtra(ref TrapSnapshot snapshot) { snapshot.ExtraFloat = angle; snapshot.ExtraVector = new Vector3(length, seenFire, 0f); }
        protected override void OnReset() { angle = RestAngle; length = 0f; seenFire = -1; Show(-1); }
        protected override void OnTimingRearmed() => Show(-1);
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) { angle = snapshot.ExtraFloat; length = snapshot.ExtraVector.x; seenFire = Mathf.RoundToInt(snapshot.ExtraVector.y); Show(Phase(roomTick)); }
    }
}
