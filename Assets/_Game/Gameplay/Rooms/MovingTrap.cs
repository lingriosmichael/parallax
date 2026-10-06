using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public enum MovingTrapKind { Hazard, Solid }
    // PAX-093 (D-095): how a Solid treats a cat standing on it. Legacy: as before PAX-093 (not carried; every existing element);
    // Carry: the cat rides it (RoomManager applies this tick's Displacement); Slip: it moves out from under the cat (not carried).
    public enum SurfaceMotion { Legacy, Carry, Slip }

    // D-119: a Solid may be a hinge floor: it swings 90 degrees about a corner over hingeTicks (tipping a cat on it, by
    // physics), then moves by offset as any mover, pushing (pushes) a cat in its way. Its pose is a function of the ticks
    // since the fire, so a reset and a rewind restore it exactly.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class MovingTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] BoxCollider2D trigger;
        [SerializeField] MovingTrapKind kind;
        [SerializeField] Vector2 offset;
        [SerializeField] int moveTicks = 1, holdTicks, returnTicks;
        [SerializeField] float crushDepth;
        [SerializeField] CrushConfig crushConfig;
        [Header("PAX-093 floors that move")]
        [SerializeField] SurfaceMotion surfaceMotion;
        [SerializeField] bool pushes;
        [SerializeField] Collider2D crushPartner;
        // A PAX-093 floor's delay from its trigger to its move (a drop-and-return floor is harmless until it goes). The builder
        // writes it only for PAX-093 floors: every MovingTrap before them keeps the 0 it always had (every pin identical).
        [SerializeField] int delayTicks;
        [Header("D-119 the hinge floor (off at hingeTicks 0)")]
        [Tooltip("Ticks to swing 90 degrees about hingePivot before the move (offset) starts.")]
        [SerializeField] int hingeTicks;
        [Tooltip("The hinge, local to the start pose (a top corner of the flat floor).")]
        [SerializeField] Vector2 hingePivot;
        [Tooltip("+1 swings counter-clockwise, -1 clockwise.")]
        [SerializeField] float hingeSign = 1f;
        Rigidbody2D body; BoxCollider2D box; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector2 start; float startAngle;

        // PAX-V07 §4: a Solid mover (push wall, crusher, lift, drop-and-return, falling ceiling) crushes; a Hazard one spikes.
        protected override CatDeathKind DeclaredDeathKind => kind == MovingTrapKind.Solid ? CatDeathKind.Crushed : CatDeathKind.Spiked;

        public SurfaceMotion Motion => surfaceMotion;
        protected override int DelayTicks => delayTicks;
        // PAX-093 (D-095): this room tick's move, pose(t) - pose(t-1); zero on a tick it didn't move. Valid only on
        // DisplacementTick (the RoomLifeTick it was computed on): a room that isn't live doesn't step its traps.
        public Vector2 Displacement { get; private set; }
        public int DisplacementTick { get; private set; } = -1;
        // D-115: the room tick before's move (zero when this trap didn't step then), so a floor that slows or stops can
        // settle the cat riding it (RoomManager, CatMotor2D.MatchSlowingFloor).
        public Vector2 PreviousDisplacement { get; private set; }
        public bool IsSolid => kind == MovingTrapKind.Solid;
        public Collider2D Box => box;

        protected override void Awake()
        {
            base.Awake(); body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            if (body == null || box == null || observers == null || Reality == null || (UsesOverlapSource && !IsPeriodic && trigger == null) || (kind == MovingTrapKind.Solid && crushDepth <= 0f && crushConfig == null))
            { Debug.LogError($"MovingTrap '{name}': missing required body, box, observers, reality, overlap trigger, or crush config.", this); enabled = false; return; }
            body.bodyType = RigidbodyType2D.Kinematic; start = body.position; startAngle = body.rotation;
            // PAX-A14: drawn between its tick poses, like the cat, so a floor moving at 50 Hz doesn't judder on a 60 Hz screen.
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            PreviousDisplacement = DisplacementTick == RoomLifeTick - 1 ? Displacement : Vector2.zero;
            Displacement = Vector2.zero; DisplacementTick = RoomLifeTick;
            bool overlap = trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _);
            StepTiming(overlap);
            if (!IsTimingEffectActive) return;
            int since = RoomLifeTick - LatestFireTick;
            Vector2 position = PositionAt(since);
            // Physics has resolved the cat against this pose. Checking it before the next
            // MovePosition distinguishes a pinned cat from one the solid safely pushed clear.
            Bounds pose = PoseBounds(body.position, body.rotation);
            // D-119: a hinge floor swinging up tips the cat by physics; it neither crushes nor pushes until it stands.
            if (Hinges && since <= hingeTicks)
            {
                Displacement = position - body.position;
                body.MovePosition(position);
                body.MoveRotation(AngleAt(since));
                return;
            }
            if (kind == MovingTrapKind.Hazard)
            {
                Bounds nextPose = new(position + box.offset, box.size);
                if (IsLocalHumanOverlapping(nextPose, observers, filter, results, out _)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
                body.MovePosition(position);
                return;
            }
            float depth = crushDepth > 0f ? crushDepth : crushConfig.DefaultCrushDepth;
            if (TryGetLocalHumanColliderBounds(observers, out Bounds catBounds) && TrapMotion.Crushes(catBounds, pose, depth)) { Death.Kill(Reality.Id, DeathCause.Hazard, this); return; }
            if (pushes && Push(position, pose, depth)) return;
            Displacement = position - body.position;
            body.MovePosition(position);
        }

        bool Hinges => hingeTicks > 0;

        // D-119: the angle `ticks` after the fire: the start's, swung 90 degrees over hingeTicks (none without a hinge).
        float AngleAt(int ticks) => !Hinges || ticks <= 0 ? startAngle : startAngle + hingeSign * 90f * TrapMotion.Progress(ticks, hingeTicks);

        // The body's position `ticks` after the fire: a hinge floor swings about its hinge, then moves by `offset` as any mover.
        Vector2 PositionAt(int ticks)
        {
            if (!Hinges) return start + TrapMotion.MovingOffset(offset, ticks, moveTicks, holdTicks, returnTicks);
            Vector2 pivot = start + hingePivot;
            Vector2 swung = pivot + (Vector2)(Quaternion.Euler(0f, 0f, AngleAt(ticks) - startAngle) * (start - pivot));
            return swung + TrapMotion.MovingOffset(offset, ticks - hingeTicks, moveTicks, holdTicks, returnTicks);
        }

        // The box's world bounds at a body pose (D-119: turned with the body).
        Bounds PoseBounds(Vector2 position, float angle)
        {
            float r = angle * Mathf.Deg2Rad, c = Mathf.Abs(Mathf.Cos(r)), sn = Mathf.Abs(Mathf.Sin(r));
            Vector2 centre = position + (Vector2)(Quaternion.Euler(0f, 0f, angle) * box.offset);
            return new Bounds(centre, new Vector3(c * box.size.x + sn * box.size.y, sn * box.size.x + c * box.size.y));
        }

        // PAX-093 (D-095) Q6: a push wall moves a cat in its way flush against its leading edge (the kit decides the shift, not
        // a physics shove). A cat beside it, not on or under it, and ahead of its centre along the push is in its way. Pushed
        // into the named crush partner by the crush depth or more, the cat dies (D-055 (3)); true when it killed.
        bool Push(Vector2 next, Bounds pose, float depth)
        {
            const float eps = 1e-4f;
            float dx = next.x - body.position.x;
            int sign = dx > eps ? 1 : dx < -eps ? -1 : 0;
            if (sign == 0 || !TryGetLocalHumanColliderBounds(observers, out Bounds cat)) return false;
            if (cat.max.y <= pose.min.y + eps || cat.min.y >= pose.max.y - eps) return false;
            if (sign > 0 ? cat.center.x < pose.center.x : cat.center.x > pose.center.x) return false;
            Bounds nextPose = PoseBounds(next, body.rotation);
            float shift = MovingFloorMath.PushOut(sign > 0 ? nextPose.max.x : nextPose.min.x, sign > 0 ? cat.min.x : cat.max.x, sign);
            if (Mathf.Abs(shift) <= eps) return false;
            observers.Get(Reality.Id).Cat.ApplyPush(new Vector2(shift, 0f));
            cat.center += new Vector3(shift, 0f, 0f);
            if (crushPartner == null || !TrapMotion.Crushes(cat, crushPartner.bounds, depth)) return false;
            Death.Kill(Reality.Id, DeathCause.Hazard, this);
            return true;
        }

        protected override void OnReset() { if (body != null) { body.position = start; body.rotation = startAngle; } }
        protected override void OnTimingRearmed() { if (body != null) { body.position = start; body.rotation = startAngle; } }
        // PAX-090 (D-091) Q2: as FallingBlockTrap, the pose at roomTick from its formula.
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            if (body == null) return;
            int since = roomTick - LatestFireTick;
            Vector2 pose = IsTimingEffectActive ? PositionAt(since) : start;
            float angle = IsTimingEffectActive ? AngleAt(since) : startAngle;
            body.position = pose; body.rotation = angle;
            body.MovePosition(pose); body.MoveRotation(angle);
        }
    }
}
