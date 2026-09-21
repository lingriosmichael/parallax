using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GravityFlipTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers; [SerializeField] GravityFlipMode mode = GravityFlipMode.Flip; [SerializeField] int delayTicks; [SerializeField] bool rearmOnExit;
        BoxCollider2D trigger; TrapCountdown countdown; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        protected override void Awake() { base.Awake(); trigger = GetComponent<BoxCollider2D>(); if (trigger == null) Debug.LogError($"{GetType().Name} on '{name}': missing BoxCollider2D", this); if (Reality == null) Debug.LogError($"{GetType().Name} on '{name}': missing RealityRoot", this); if (observers == null) Debug.LogError($"{GetType().Name} on '{name}': missing ObserverSet", this); if (trigger == null || Reality == null || observers == null) { enabled = false; return; } trigger.isTrigger = true; filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false }; countdown = new TrapCountdown(delayTicks, rearmOnExit); }
        protected override void OnLiveRoomStep() { if (!enabled) return; bool overlap = IsLocalHumanOverlapping(trigger, observers, filter, results, out _); if (countdown.Step(overlap)) { State = TrapState.Fired; ObserverContext observer = observers.Get(Reality.Id); if (observer == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman || observer.Cat == null) return; GravityReceiver gravity = observer.Cat.GetComponent<GravityReceiver>(); if (gravity != null) gravity.SetTargetDirection(GravityFlipRule.Resolve(mode, gravity.TargetDirection)); } if (countdown.State == TrapCountdownState.Armed) State = TrapState.Armed; }
        protected override void OnReset() { countdown.Reset(); }
    }
}
