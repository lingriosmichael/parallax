using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public enum MovingTrapKind { Hazard, Solid }

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
        Rigidbody2D body; BoxCollider2D box; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector2 start;

        protected override void Awake()
        {
            base.Awake(); body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            if (body == null || box == null || observers == null || Reality == null || (UsesOverlapSource && trigger == null) || (kind == MovingTrapKind.Solid && crushDepth <= 0f && crushConfig == null))
            { Debug.LogError($"MovingTrap '{name}': missing required body, box, observers, reality, overlap trigger, or crush config.", this); enabled = false; return; }
            body.bodyType = RigidbodyType2D.Kinematic; start = body.position;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
        }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
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
                if (IsLocalHumanOverlapping(nextPose, observers, filter, results, out _)) { Death.Kill(Reality.Id, DeathCause.Hazard); return; }
                body.MovePosition(position);
                return;
            }
            float depth = crushDepth > 0f ? crushDepth : crushConfig.DefaultCrushDepth;
            if (TryGetLocalHumanColliderBounds(observers, out Bounds catBounds) && TrapMotion.Crushes(catBounds, pose, depth)) { Death.Kill(Reality.Id, DeathCause.Hazard); return; }
            body.MovePosition(position);
        }

        protected override void OnReset() { if (body != null) body.position = start; }
        protected override void OnTimingRearmed() { if (body != null) body.position = start; }
    }
}
