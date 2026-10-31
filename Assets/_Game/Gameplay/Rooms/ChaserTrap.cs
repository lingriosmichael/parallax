using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>D-276 amendment 2: the chaser. One laser that chases the cat all level: a line through a pivot at mid-height, each half
    /// a beam from the pivot to the first fixed solid (end to end, no gaps; a trap's own solid never stops it). From its fire it warns
    /// warnTicks at home (harmless), then its pivot runs east at the speed of the zone it is in (zoneX / zoneSpeed: it speeds up and
    /// slows down along the level), from catchUpFromX on never more than maxLag behind the cat (beyond, it runs at catchUpSpeed), to
    /// endX. Upright while it runs; when its pivot reaches spinX it stops and turns a full circle at spinTurn degrees a tick (its
    /// set piece), then runs on, upright. D-276 amendment 3: from closer than minLag it creeps; from swingFromX its line swings to each
    /// tilt in turn, wider each time. Its pose depends on where the cat has been, so it keeps its state by value (the snapshot's
    /// ExtraFloat and ExtraVector) and a rewind restores it exactly (D-091). The kill is an oriented box per beam (also the route
    /// harness's).</summary>
    public sealed class ChaserTrap : RoomTrap
    {
        public const int Arms = 2;
        public enum ChasePhase { Home, Running, Spinning, Done, Parked, Passed }

        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] Transform pivot;
        [SerializeField] SpriteRenderer[] arms = new SpriteRenderer[Arms];
        [SerializeField] int warnTicks = 50;
        [SerializeField] float armWidth = .3f, range = 80f, endX = 100f, maxLag = 9f, catchUpSpeed = .16f, catchUpFromX, spinX = float.MaxValue, spinTurn = -1f;
        [Tooltip("The pivot's speed (u a tick) from each zoneX on (ascending world x).")]
        [SerializeField] float[] zoneX = new float[0], zoneSpeed = new float[0];
        [Tooltip("D-276 amendment 3: closer than minLag ahead of it, the cat makes it creep (its speed times closeScale): always near, never rushing.")]
        [SerializeField] float minLag, closeScale = 1f;
        [Tooltip("D-276 amendment 3: from swingFromX on, its line swings: towards each tilt in turn (degrees off upright; + its top forward) at swingSpeed a tick, then back and forth between the last two.")]
        [SerializeField] float swingFromX = float.MaxValue, swingSpeed = .5f;
        [Tooltip("D-276 amendment 3: past parkFromX it parks (parkSpeed) only once it is ahead of the cat (until then it keeps its zone's speed); once the cat has passed it, it runs on at afterSpeed (and catches up).")]
        [SerializeField] float parkFromX = float.MaxValue, parkSpeed, afterSpeed = .05f;
        [SerializeField] float[] swingTilts = new float[0];
        [SerializeField] Color idleColor = new(.85f, .95f, 1f, .25f), warnColor = new(.85f, .95f, 1f, .7f), liveColor = new(.9f, .97f, 1f, 1f);

        ContactFilter2D filter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        readonly Collider2D[] results = new Collider2D[8];
        readonly float[] reach = new float[Arms];
        float x, angle = 90f, spun;
        ChasePhase phase;

        protected override CatDeathKind DeclaredDeathKind => CatDeathKind.Zapped;

        public ChasePhase Phase => phase;
        public float X => x;
        public float Angle => angle;
        public bool Live => phase != ChasePhase.Home && SinceFire >= warnTicks;
        public float ArmAngle(int i) => angle + i * 180f;
        public float ArmReach(int i) => reach[i];
        Vector2 Home => transform.position;
        Vector2 At => new(x, Home.y);
        int SinceFire => LatestFireTick >= 0 ? RoomLifeTick - LatestFireTick : -1;
        static Vector2 Along(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        protected override void Awake()
        {
            base.Awake();
            if (observers == null || Reality == null || pivot == null || arms == null || arms.Length != Arms || System.Array.IndexOf(arms, null) >= 0 || (UsesOverlapSource && trigger == null))
            { Debug.LogError($"ChaserTrap '{name}': missing observers, reality, pivot, its {Arms} arms or its trigger.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            ResetState();
            Pose();
        }

        void ResetState() { x = Home.x; angle = 90f; spun = 0f; phase = ChasePhase.Home; }

        /// <summary>The zone speed at pivot x.</summary>
        public float ZoneSpeed(float at)
        {
            float speed = zoneSpeed.Length > 0 ? zoneSpeed[0] : 0f;
            for (int i = 0; i < zoneX.Length && i < zoneSpeed.Length; i++) if (at >= zoneX[i]) speed = zoneSpeed[i];
            return speed;
        }

        // The distance along `direction` from the pivot to the first fixed solid (a trap's own solid never stops it; range when none).
        float Reach(Vector2 from, Vector2 direction)
        {
            Collider2D cat = TryGetCat(out Collider2D c) ? c : null;
            int count = Physics2D.Raycast(from, direction, filter, hits, range);
            float best = range;
            for (int i = 0; i < count; i++)
            {
                Collider2D h = hits[i].collider;
                if (h == null || h == cat || h.transform.IsChildOf(transform) || h.GetComponentInParent<RoomTrap>() != null) continue;
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

        /// <summary>One room step of the chase (after the warning).</summary>
        void Advance()
        {
            if (phase == ChasePhase.Home) phase = ChasePhase.Running;
            if (phase == ChasePhase.Spinning)
            {
                angle += spinTurn; spun += Mathf.Abs(spinTurn);
                if (spun >= 360f - 1e-3f) { angle = 90f; phase = ChasePhase.Done; }
                return;
            }
            Swing();
            float speed = ZoneSpeed(x);
            bool seen = TryGetLocalHumanColliderBounds(observers, out Bounds cat);
            float lag = seen ? cat.center.x - x : 0f;
            // The overtake: parked once it is ahead of the cat past parkFromX; running on once the cat has passed it.
            if (phase == ChasePhase.Running && x >= parkFromX && seen && lag < 0f) phase = ChasePhase.Parked;
            if (phase == ChasePhase.Parked && seen && lag > 1f) phase = ChasePhase.Passed;
            if (phase == ChasePhase.Parked) speed = parkSpeed;
            else if (phase == ChasePhase.Passed) speed = afterSpeed;
            if (phase != ChasePhase.Parked && x >= catchUpFromX && seen)
            {
                if (lag > maxLag) speed = Mathf.Max(speed, catchUpSpeed);
                else if (lag >= 0f && lag < minLag && speed <= catchUpSpeed) speed *= closeScale;   // a surge never creeps
            }
            float next = Mathf.Min(x + speed, endX);
            if (phase is ChasePhase.Running && x < spinX && next >= spinX) { next = spinX; phase = ChasePhase.Spinning; }
            x = next;
        }

        /// <summary>D-276 amendment 3: one step of the swing (spun holds the index of the tilt it is swinging to).</summary>
        void Swing()
        {
            if (x < swingFromX || swingTilts.Length == 0 || phase == ChasePhase.Spinning) return;
            int i = Mathf.Clamp(Mathf.RoundToInt(spun), 0, swingTilts.Length - 1);
            float target = 90f - swingTilts[i];
            angle = Mathf.MoveTowards(angle, target, swingSpeed);
            if (Mathf.Abs(angle - target) < 1e-3f) spun = i + 1 < swingTilts.Length ? i + 1 : Mathf.Max(0, swingTilts.Length - 2);
        }

        /// <summary>Places the pivot and the beams (their reach against the fixed solids).</summary>
        void Pose()
        {
            int since = SinceFire;
            Vector2 at = At;
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
            if (SinceFire >= warnTicks) Advance();
            Pose();
            if (ArmHits()) Death.Kill(Reality.Id, DeathCause.Hazard, this);
        }

        /// <summary>A live beam overlaps the cat (the kill test, also the route harness's).</summary>
        public bool ArmHits()
        {
            if (!enabled || !Live) return false;
            Vector2 at = At;
            for (int i = 0; i < Arms; i++)
            {
                float a = ArmAngle(i);
                if (reach[i] <= 1e-3f) continue;
                if (IsLocalHumanOverlapping(new Bounds(at + Along(a) * (reach[i] * .5f), new Vector3(reach[i], armWidth)), a, observers, filter, results, out _)) return true;
            }
            return false;
        }

        protected override void CaptureExtra(ref TrapSnapshot snapshot) { snapshot.ExtraFloat = x; snapshot.ExtraVector = new Vector3(angle, (int)phase, spun); }
        protected override void OnReset() { ResetState(); Pose(); }
        protected override void OnTimingRearmed() => Pose();
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            x = snapshot.ExtraFloat; angle = snapshot.ExtraVector.x; phase = (ChasePhase)Mathf.RoundToInt(snapshot.ExtraVector.y); spun = snapshot.ExtraVector.z;
            Pose();
        }
    }
}
