using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public enum FallingBlockDirection { Down, Up }
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class FallingBlockTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers; [SerializeField] BoxCollider2D trigger; [SerializeField] FallingBlockDirection direction; [SerializeField] int delayTicks; [SerializeField] float unitsPerTick = .3f; [SerializeField] float travelDistance = 3f;
        Rigidbody2D body; BoxCollider2D box; TrapCountdown countdown; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector2 start;
        protected override void Awake()
        {
            base.Awake(); body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
            if (body == null) Debug.LogError($"{GetType().Name} on '{name}': missing Rigidbody2D", this);
            if (box == null) Debug.LogError($"{GetType().Name} on '{name}': missing BoxCollider2D", this);
            if (trigger == null && UsesOverlapSource && !IsPeriodic) Debug.LogError($"{GetType().Name} on '{name}': missing Trigger", this);
            if (Reality == null) Debug.LogError($"{GetType().Name} on '{name}': missing RealityRoot", this);
            if (observers == null) Debug.LogError($"{GetType().Name} on '{name}': missing ObserverSet", this);
            if (body == null || box == null || Reality == null || observers == null || (UsesOverlapSource && !IsPeriodic && trigger == null)) { enabled = false; return; }
            body.bodyType = RigidbodyType2D.Kinematic; filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false }; countdown = new TrapCountdown(delayTicks); start = body.position;
        }
        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(trigger != null && IsLocalHumanOverlapping(trigger, observers, filter, results, out _));
            if (!IsTimingEffectActive) return;
            float travel = TrapMotion.Travel(RoomLifeTick - LatestFireTick, unitsPerTick, travelDistance);
            Vector2 dir = direction == FallingBlockDirection.Up ? Vector2.up : Vector2.down;
            body.MovePosition(start + dir * travel);
            if (travel >= travelDistance) return;
            Bounds bounds = box.bounds; bounds.Expand(-.04f);
            if (IsLocalHumanOverlapping(bounds, observers, filter, results, out _)) Death.Kill(Reality.Id, DeathCause.Hazard);
        }
        protected override void OnReset() { countdown.Reset(); body.position = start; }
        protected override void OnTimingRearmed() { body.position = start; }
        protected override int DelayTicks => delayTicks;
    }
}
