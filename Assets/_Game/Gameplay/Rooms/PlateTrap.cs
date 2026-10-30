using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    /// <summary>PAX-119 (D-197): a pressure plate, a thin trigger flush on a floor top. A LocalHuman cat on it fires it (Overlap, its
    /// own box; Rearm, so standing on it fires it again each cooldown), and gates or any trap chain from it. Its look dips from the
    /// fire until it rearms: a function of its timing, so a reset and a rewind restore it exactly (D-091).</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlateTrap : RoomTrap
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] Transform look;
        [SerializeField] float pressDepth = .1f;
        BoxCollider2D box; ContactFilter2D filter; readonly Collider2D[] results = new Collider2D[8]; Vector3 rest;

        protected override void Awake()
        {
            base.Awake(); box = GetComponent<BoxCollider2D>();
            if (box == null || observers == null || Reality == null || look == null) { Debug.LogError($"PlateTrap '{name}': missing box, observers, reality or look.", this); enabled = false; return; }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = Reality.PhysicsMask, useTriggers = false };
            rest = look.localPosition;
        }

        void Show(bool pressed) { if (look != null) look.localPosition = pressed ? rest + Vector3.down * pressDepth : rest; }

        protected override void OnLiveRoomStep()
        {
            if (!enabled) return;
            StepTiming(IsLocalHumanOverlapping(box, observers, filter, results, out _));
            Show(IsTimingEffectActive);
        }

        protected override void OnReset() => Show(false);
        protected override void OnTimingRearmed() => Show(false);
        protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => Show(IsTimingEffectActive);
    }
}
