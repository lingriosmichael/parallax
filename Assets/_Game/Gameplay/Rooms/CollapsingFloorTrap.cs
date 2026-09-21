using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class CollapsingFloorTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] int delayTicks = 12;
        [SerializeField] float touchSkin = .05f;
        BoxCollider2D box; SpriteRenderer visual; TrapCountdown countdown; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8];
        protected override void Awake()
        {
            base.Awake(); box = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
            if (box == null) Debug.LogError($"{GetType().Name} on '{name}': missing BoxCollider2D", this);
            if (visual == null) Debug.LogError($"{GetType().Name} on '{name}': missing SpriteRenderer", this);
            if (Reality == null) Debug.LogError($"{GetType().Name} on '{name}': missing RealityRoot", this);
            if (observers == null) Debug.LogError($"{GetType().Name} on '{name}': missing ObserverSet", this);
            if (box == null || visual == null || Reality == null || observers == null) { enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false }; countdown = new TrapCountdown(delayTicks);
        }
        protected override void OnLiveRoomStep() { if (!enabled) return; Bounds bounds = box.bounds; bounds.Expand(touchSkin * 2f); bool touched = IsLocalHumanOverlapping(bounds, observers, filter, results, out _); if (countdown.Step(touched)) { State = TrapState.Fired; box.enabled = false; visual.enabled = false; } }
        protected override void OnReset() { countdown.Reset(); box.enabled = true; visual.enabled = true; }
    }
}
