using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public sealed class DoorRetreatTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers; [SerializeField] BoxCollider2D trigger; [SerializeField] Transform doorRoot; [SerializeField] Vector2 offset = new Vector2(2f, 0f); [SerializeField] int moveTicks = 10; [SerializeField] int delayTicks;
        TrapCountdown countdown; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector3 start;
        protected override void Awake() { base.Awake(); if (trigger == null) Debug.LogError($"{GetType().Name} on '{name}': missing Trigger", this); if (doorRoot == null) Debug.LogError($"{GetType().Name} on '{name}': missing doorRoot", this); if (Reality == null) Debug.LogError($"{GetType().Name} on '{name}': missing RealityRoot", this); if (observers == null) Debug.LogError($"{GetType().Name} on '{name}': missing ObserverSet", this); if (trigger == null || doorRoot == null || Reality == null || observers == null) { enabled = false; return; } filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false }; countdown = new TrapCountdown(delayTicks); start = doorRoot.position; }
        protected override void OnLiveRoomStep() { if (!enabled) return; if (countdown.Step(IsLocalHumanOverlapping(trigger, observers, filter, results, out _))) State = TrapState.Fired; if (countdown.State == TrapCountdownState.Fired) doorRoot.position = start + (Vector3)(offset * TrapMotion.Progress(countdown.TicksSinceFired, moveTicks)); }
        protected override void OnReset() { countdown.Reset(); doorRoot.position = start; }
    }
}
