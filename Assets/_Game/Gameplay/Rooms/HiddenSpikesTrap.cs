using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class HiddenSpikesTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers; [SerializeField] BoxCollider2D trigger; [SerializeField] Hazard hazard; [SerializeField] int revealDelayTicks;
        BoxCollider2D box; SpriteRenderer visual; TrapCountdown countdown; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        protected override void Awake()
        {
            base.Awake(); box = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
            if (trigger == null) trigger = box;
            if (box == null) Debug.LogError($"{GetType().Name} on '{name}': missing BoxCollider2D", this);
            if (visual == null) Debug.LogError($"{GetType().Name} on '{name}': missing SpriteRenderer", this);
            if (hazard == null) Debug.LogError($"{GetType().Name} on '{name}': missing Hazard", this);
            if (Reality == null) Debug.LogError($"{GetType().Name} on '{name}': missing RealityRoot", this);
            if (observers == null) Debug.LogError($"{GetType().Name} on '{name}': missing ObserverSet", this);
            if (box == null || visual == null || hazard == null || Reality == null || observers == null) { enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false }; countdown = new TrapCountdown(revealDelayTicks); hazard.SetArmed(false); visual.enabled = false;
        }
        protected override void OnLiveRoomStep() { if (!enabled) return; if (countdown.Step(IsLocalHumanOverlapping(trigger, observers, filter, results, out _))) { State = TrapState.Fired; hazard.SetArmed(true); visual.enabled = true; } }
        protected override void OnReset() { countdown.Reset(); hazard.SetArmed(false); visual.enabled = false; }
    }
}
