using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public enum MovingTrapKind { Hazard, Solid }
    // PAX-093 (D-095): how a Solid treats a cat standing on it. Legacy: as before PAX-093 (not carried; every existing element);
    // Carry: the cat rides it (RoomManager applies this tick's Displacement); Slip: it moves out from under the cat (not carried).
    public enum SurfaceMotion { Legacy, Carry, Slip }

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
        Rigidbody2D body; BoxCollider2D box; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector2 start;

        public SurfaceMotion Motion => surfaceMotion;
        protected override int DelayTicks => delayTicks;
        // PAX-093 (D-095): this room tick's move, pose(t) - pose(t-1); zero on a tick it didn't move. Valid only on
        // DisplacementTick (the RoomLifeTick it was computed on): a room that isn't live doesn't step its traps.
        public Vector2 Displacement { get; private set; }
        public int DisplacementTick { get; private set; } = -1;

        protected override void Awake()
        {
            base.Awake(); body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            if (body == null || box == null || observers == null || Reality == null || (UsesOverlapSource && !IsPeriodic && trigger == null) || (kind == MovingTrapKind.Solid && crushDepth <= 0f && crushConfig == null))
            { Debug.LogError($"MovingTrap '{name}': missing required body, box, observers, reality, overlap trigger, or crush config.", this); enabled = false; return; }
            body.bodyType = RigidbodyType2D.Kinematic; start = body.position;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            Displacement = Vector2.zero; DisplacementTick = RoomLifeTick;
            bool overlap = trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _);
            StepTiming(overlap);
            if (!IsTimingEffectActive) return;
            Vector2 position = start + TrapMotion.MovingOffset(offset, RoomLifeTick - LatestFireTick, moveTicks, holdTicks, returnTicks);
            // Physics has resolved the cat against this pose. Checking it before the next
            // MovePosition distinguishes a pinned cat from one the solid safely pushed clear.
            Bounds pose = new(body.position + box.offset, box.size);
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
            Bounds nextPose = new(next + box.offset, box.size);
            float shift = MovingFloorMath.PushOut(sign > 0 ? nextPose.max.x : nextPose.min.x, sign > 0 ? cat.min.x : cat.max.x, sign);
            if (Mathf.Abs(shift) <= eps) return false;
            observers.Get(Reality.Id).Cat.ApplyPush(new Vector2(shift, 0f));
            cat.center += new Vector3(shift, 0f, 0f);
            if (crushPartner == null || !TrapMotion.Crushes(cat, crushPartner.bounds, depth)) return false;
            Death.Kill(Reality.Id, DeathCause.Hazard, this);
            return true;
        }

        protected override void OnReset() { if (body != null) body.position = start; }
        protected override void OnTimingRearmed() { if (body != null) body.position = start; }
        // PAX-090 (D-091) Q2: as FallingBlockTrap, the pose at roomTick from its formula.
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick)
        {
            if (body == null) return;
            Vector2 pose = IsTimingEffectActive ? start + TrapMotion.MovingOffset(offset, roomTick - LatestFireTick, moveTicks, holdTicks, returnTicks) : start;
            body.position = pose;
            body.MovePosition(pose);
        }
    }
}
