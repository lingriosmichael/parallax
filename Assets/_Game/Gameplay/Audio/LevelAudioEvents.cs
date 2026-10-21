using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-064 (D-158): turns a level's events into cues. Every trap's Fired; the start of a death hold (by what
    /// killed, PAX-V07's death kinds), or the death itself when there is no hold; the respawn; level complete; and the solo
    /// cat's jumps and landings; and (2026-10-20, "Storms needs to sound like thunder") every storm cloud's strike. Read-only: it subscribes and polls, and never writes gameplay state. It runs after
    /// ObserverSet's tick (execution order 1000), so it sees each tick's JumpedThisStep and grounding.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class LevelAudioEvents : MonoBehaviour
    {
        [SerializeField] GameAudio gameAudio;
        [SerializeField] RoomManager rooms;
        [SerializeField] RoomDeath death;
        [SerializeField] ObserverSet observers;
        [Tooltip("Ticks the cat must be airborne before touching down plays a landing (skips bumps and steps).")]
        [SerializeField] int landAfterTicks = 5;
        [Tooltip("Ticks airborne from which a landing is a hard one.")]
        [SerializeField] int hardLandTicks = 40;

        readonly List<RoomTrap> traps = new List<RoomTrap>();
        readonly List<StormCloudTrap> clouds = new List<StormCloudTrap>();
        readonly List<StormCloudPhase> cloudPhases = new List<StormCloudPhase>();
        CatMotor2D cat;
        CatRespawn respawn;
        bool holdHeard;
        int airborne;

        void OnEnable()
        {
            if (gameAudio == null || rooms == null || death == null || observers == null)
            {
                Debug.LogError("LevelAudioEvents: GameAudio, RoomManager, RoomDeath or ObserverSet is missing; the level's events stay silent. Run PARALLAX/Setup/Audio (PAX-064).", this);
                enabled = false;
                return;
            }
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                foreach (RoomTrap trap in root.GetComponentsInChildren<RoomTrap>(true))
                {
                    trap.Fired += OnTrapFired;
                    traps.Add(trap);
                    if (trap is StormCloudTrap cloud) { clouds.Add(cloud); cloudPhases.Add(cloud.Phase); }
                }
            rooms.LevelCompleted += OnLevelCompleted;
            death.Died += OnDied;
        }

        void OnDisable()
        {
            foreach (RoomTrap trap in traps) if (trap != null) trap.Fired -= OnTrapFired;
            traps.Clear();
            clouds.Clear();
            cloudPhases.Clear();
            if (rooms != null) rooms.LevelCompleted -= OnLevelCompleted;
            if (death != null) death.Died -= OnDied;
            if (respawn != null) respawn.Respawned -= OnRespawned;
            respawn = null;
            cat = null;
            holdHeard = false;
            airborne = 0;
        }

        void FixedUpdate()
        {
            if (death.IsHolding)
            {
                if (!holdHeard)
                {
                    holdHeard = true;
                    gameAudio.Play(AudioCues.ForDeath(CatDeathKinds.Resolve(death.HoldKiller, death.HoldCause)), CatX());
                }
                return;
            }
            HearStrikes();
            if (!FindCat() || cat.IsFrozen || rooms.LevelComplete) return;

            if (cat.WallJumpedThisStep) gameAudio.Play(AudioCue.WallJump, CatX());
            else if (cat.JumpedThisStep) gameAudio.Play(AudioCue.Jump, CatX());

            if (cat.IsClimbing || cat.IsClinging) airborne = 0;
            else if (!cat.IsGrounded) airborne++;
            else
            {
                if (airborne >= landAfterTicks) gameAudio.Play(airborne >= hardLandTicks ? AudioCue.HardLand : AudioCue.Land, CatX());
                airborne = 0;
            }
        }

        // A strike's first tick plays thunder where the cloud is (its phase is set in this tick's room step, before this runs).
        void HearStrikes()
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                StormCloudTrap cloud = clouds[i];
                if (cloud == null) continue;
                StormCloudPhase now = cloud.Phase;
                if (now == StormCloudPhase.Strike && cloudPhases[i] != StormCloudPhase.Strike)
                {
                    Vector3 at = cloud.Cloud != null ? cloud.Cloud.transform.position : cloud.transform.position;
                    gameAudio.Play(AudioCue.TrapStormStrike, at.x, at.y);
                }
                cloudPhases[i] = now;
            }
        }

        void OnTrapFired(RoomTrap trap) => gameAudio.Play(TrapCues.For(trap), trap.transform.position.x, trap.transform.position.y);

        // The reset after a hold; with no hold (HoldTicks 0) this is the only sign of the death.
        void OnDied(DeathInfo info)
        {
            if (!holdHeard && info.Observer == rooms.SoloReality)
                gameAudio.Play(AudioCues.ForDeath(CatDeathKinds.Resolve(info.Killer, info.Cause)), CatX());
            holdHeard = false;
        }

        void OnRespawned()
        {
            airborne = 0;
            if (!rooms.LevelComplete) gameAudio.Play(AudioCue.Respawn, CatX());
        }

        void OnLevelCompleted() => gameAudio.Play(AudioCue.LevelComplete, CatX());

        float? CatX() => FindCat() ? cat.transform.position.x : null;

        bool FindCat()
        {
            if (cat != null) return true;
            ObserverContext solo = observers.Get(rooms.SoloReality);
            if (solo == null || solo.Cat == null) return false;
            cat = solo.Cat;
            respawn = cat.GetComponent<CatRespawn>();
            if (respawn != null) respawn.Respawned += OnRespawned;
            return true;
        }
    }
}
