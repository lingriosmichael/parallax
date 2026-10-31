using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-123 (D-276): a trident emitter, hosted in a solid, on its Periodic timing: three fixed beams from its muzzle,
    /// one along downAngle and two spreadDegrees to either side. From each fire: warnTicks the three lines flicker, thin and harmless
    /// (the tell); then fireTicks each is a lethal beam to the first solid along its line (a raycast against the reality's colliders,
    /// the cat's excepted); then it rests. The phase is a function of ticks (PulseGateMath), so a reset and a rewind redraw it with
    /// nothing to keep by value (D-091). The kill is an oriented box per beam (also the route harness's).</summary>
    public sealed class TridentTrap : RoomTrap
    {
        public const int Beams = 3;

        [SerializeField] ObserverSet observers;
        [SerializeField] int warnTicks = 12, fireTicks = 20;
        [SerializeField] float downAngle = -90f, spreadDegrees = 35f, beamWidth = .3f, warnWidth = .06f, range = 40f;
        [SerializeField] SpriteRenderer[] lines = new SpriteRenderer[Beams];
        [SerializeField] Color warnColor = new(1f, .25f, .25f, .7f), fireColor = new(1f, .1f, .1f, 1f);

        ContactFilter2D filter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        readonly Collider2D[] results = new Collider2D[8];
        readonly float[] length = new float[Beams];

        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Zapped;

        public PulseGatePhase Phase { get; private set; }
        public float BeamAngle(int i) => TrapPath.TridentAngle(downAngle, spreadDegrees, i);
        public float BeamLength(int i) => length[i];
        Vector2 Muzzle => transform.position;
        static Vector2 Along(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        protected override void Awake()
        {
            base.Awake();
            if (observers == null || Reality == null || lines == null || lines.Length != Beams || System.Array.IndexOf(lines, null) >= 0)
            { Debug.LogError($"TridentTrap '{name}': missing observers, reality or its {Beams} lines.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            Apply(RoomLifeTick);
        }

        // The distance along `direction` to the first collider that isn't the cat or this trap (range when none).
        float Reach(Vector2 direction)
        {
            Collider2D cat = TryGetLocalHumanCollider(out Collider2D c) ? c : null;
            int count = Physics2D.Raycast(Muzzle, direction, filter, hits, range);
            float best = range;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null || hits[i].collider == cat || hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance > 1e-3f && hits[i].distance < best) best = hits[i].distance;
            }
            return best;
        }

        bool TryGetLocalHumanCollider(out Collider2D cat)
        {
            cat = null;
            ObserverContext o = observers != null && Reality != null ? observers.Get(Reality.Id) : null;
            if (o == null || o.Cat == null) return false;
            cat = Parallax.Gameplay.Player.CatBodyCollider.Of(o.Cat);
            return cat != null;
        }

        void Apply(int roomTick)
        {
            int since = IsTimingEffectActive && LatestFireTick >= 0 ? roomTick - LatestFireTick : -1;
            Phase = PulseGateMath.PhaseSince(since, warnTicks, fireTicks);
            for (int i = 0; i < Beams; i++)
            {
                SpriteRenderer line = lines[i];
                if (line == null) continue;
                line.enabled = Phase != PulseGatePhase.Off;
                if (!line.enabled) continue;
                float a = BeamAngle(i);
                length[i] = Reach(Along(a));
                line.transform.position = (Vector3)(Muzzle + Along(a) * (length[i] * .5f)) + new Vector3(0f, 0f, line.transform.position.z);
                line.transform.rotation = Quaternion.Euler(0f, 0f, a);
                line.size = new Vector2(length[i], Phase == PulseGatePhase.On ? beamWidth : warnWidth);   // a sliced sprite: its drawn size
                Color col = Phase == PulseGatePhase.On ? fireColor : warnColor;
                if (Phase == PulseGatePhase.Warn && PulseGateMath.WarnLit(since)) col.a *= .5f;
                line.color = col;
            }
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(false);
            Apply(RoomLifeTick);
            if (BeamHits()) Death.Kill(Reality.Id, DeathCause.Hazard, this);
        }

        /// <summary>A beam, while firing, overlaps the cat (the kill test, also the route harness's).</summary>
        public bool BeamHits()
        {
            if (!enabled || Phase != PulseGatePhase.On) return false;
            for (int i = 0; i < Beams; i++)
            {
                float a = BeamAngle(i);
                var beam = new Bounds(Muzzle + Along(a) * (length[i] * .5f), new Vector3(length[i], beamWidth));
                if (IsLocalHumanOverlapping(beam, a, observers, filter, results, out _)) return true;
            }
            return false;
        }

        protected override void OnReset() => Apply(RoomLifeTick);
        protected override void OnTimingRearmed() => Apply(RoomLifeTick);
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Apply(roomTick);
    }
}
