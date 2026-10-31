using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-065 (D-278): a level's feel, beside LevelAudioEvents on the GameAudio root: every trap's Fired, the start of a death
    /// hold (or the death itself when there is no hold), and level complete go to a HapticDispatcher. Read-only: it subscribes and
    /// polls, and never writes gameplay state; removing it changes no test. It runs after ObserverSet's tick, as LevelAudioEvents
    /// does, so it sees a hold on the tick it starts.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class LevelHaptics : MonoBehaviour
    {
        [SerializeField] HapticsConfig config;
        [SerializeField] RoomManager rooms;
        [SerializeField] RoomDeath death;
        [SerializeField] ObserverSet observers;

        readonly List<RoomTrap> traps = new List<RoomTrap>();
        HapticDispatcher dispatcher;
        bool holdFelt;

        void OnEnable()
        {
            if (config == null || rooms == null || death == null || observers == null)
            {
                Debug.LogError("LevelHaptics: HapticsConfig, RoomManager, RoomDeath or ObserverSet is missing; the level stays still. Run PARALLAX/Setup/Haptics (PAX-065).", this);
                enabled = false;
                return;
            }
            dispatcher = new HapticDispatcher(CreateHaptics(), () => LevelProgressStore.HapticsEnabled, config.MinGap, config.TrapWithin);
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (RoomTrap trap in root.GetComponentsInChildren<RoomTrap>(true))
                {
                    trap.Fired += OnTrapFired;
                    traps.Add(trap);
                }
            rooms.LevelCompleted += OnLevelCompleted;
            death.Died += OnDied;
        }

        void OnDisable()
        {
            foreach (RoomTrap trap in traps) if (trap != null) trap.Fired -= OnTrapFired;
            traps.Clear();
            if (rooms != null) rooms.LevelCompleted -= OnLevelCompleted;
            if (death != null) death.Died -= OnDied;
            holdFelt = false;
        }

        IHaptics CreateHaptics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHaptics(config);
#else
            return new EditorHaptics();
#endif
        }

        void FixedUpdate()
        {
            if (!death.IsHolding || holdFelt) return;
            holdFelt = true;
            dispatcher.Died(Time.unscaledTime);
        }

        void OnTrapFired(RoomTrap trap)
        {
            ObserverContext solo = observers.Get(rooms.SoloReality);
            if (solo == null || solo.Cat == null) return;
            dispatcher.TrapFired(Vector2.Distance(trap.transform.position, solo.Cat.transform.position), Time.unscaledTime);
        }

        // The reset after a hold; with no hold (HoldTicks 0) this is the only sign of the death.
        void OnDied(DeathInfo info)
        {
            if (!holdFelt && info.Observer == rooms.SoloReality) dispatcher.Died(Time.unscaledTime);
            holdFelt = false;
        }

        void OnLevelCompleted() => dispatcher.LevelCompleted(Time.unscaledTime);
    }
}
