using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Transport;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public readonly struct DeathInfo
    {
        public readonly ObserverId Observer;
        public readonly DeathCause Cause;
        public readonly int Room;
        public readonly int Tick;
        public readonly int TrapsReset;
        public readonly int AnchorsReset;

        public DeathInfo(ObserverId observer, DeathCause cause, int room, int tick, int trapsReset, int anchorsReset)
        {
            Observer = observer; Cause = cause; Room = room; Tick = tick; TrapsReset = trapsReset; AnchorsReset = anchorsReset;
        }
    }

    public sealed class RoomDeath : MonoBehaviour
    {
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] RoomManager rooms;
        [SerializeField] ObserverSet observers;
        [SerializeField] TransportHost transportHost;
        [SerializeField] RoomSafetyConfig config;

        readonly DeathTickGuard tickGuard = new DeathTickGuard();
        readonly RoomResetRegistry resetRegistry = new RoomResetRegistry();
        readonly List<AnchorReset> anchorResets = new List<AnchorReset>();
        readonly Dictionary<ObserverId, int> killedTicks = new Dictionary<ObserverId, int>();
        readonly DeathCounter deathCounter = new DeathCounter();

        DeathHold hold;
        ObserverContext pendingObserver;
        int pendingRoom;
        DeathCause pendingCause;
        int pendingTick;

        public RoomResetRegistry ResetRegistry => resetRegistry;
        public bool IsRoomLive(int roomId) => rooms != null && rooms.IsLive(roomId);
        public event Action<DeathInfo> Died;

        public bool IsHolding => hold != null && hold.IsHolding;
        public int DeathsIn(int roomId) => deathCounter.DeathsIn(roomId);

        public bool WasKilledThisTick(ObserverId observer, int tick) =>
            killedTicks.TryGetValue(observer, out int killedTick) && killedTick == tick;

        void Awake()
        {
            int holdTicks = 30;
            if (config == null) Debug.LogWarning("RoomDeath: no RoomSafetyConfig assigned; using default HoldTicks 30.", this);
            else holdTicks = config.HoldTicks;
            hold = new DeathHold(holdTicks);
        }

        public void Kill(ObserverId observerId, DeathCause cause)
        {
            // PAX-049 (D-061): once the level is complete, no further death is recorded, held,
            // or raised. `rooms` is null in Sandbox_Realities (frozen co-op), which keeps its
            // existing behaviour untouched.
            if (rooms != null && rooms.LevelComplete) return;
            if (observers == null || checkpoints == null) return;
            ObserverContext observer = observers.Get(observerId);
            if (observer == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman) return;

            if (rooms == null || observerId != rooms.SoloReality)
            {
                checkpoints.Respawn(observer);
                return;
            }

            // PAX-047 (D-058): a kill during an active hold is ignored outright — this covers
            // every kill source, including ones that don't run through RoomManager's own tick
            // order (e.g. FallResetVolume's independent FixedUpdate).
            if (hold.IsHolding) return;

            int tick = observers.Tick;
            if (!tickGuard.TryAccept(observerId, tick)) return;
            killedTicks[observerId] = tick;

            int room = rooms.CurrentRoom;
            if (checkpoints.Current != room)
            {
                Debug.LogError($"RoomDeath: current room {room} disagrees with checkpoint progress {checkpoints.Current}; using room {room}.", this);
            }

            deathCounter.Record(room);
            rooms.RecordSectionDeath(room);   // PAX-090 (D-091) R6: nothing unless the room has checkpoint sections
            pendingObserver = observer;
            pendingRoom = room;
            pendingCause = cause;
            pendingTick = tick;

            if (hold.Begin() == DeathHoldPhase.ResetNow)
            {
                PerformReset();
            }
            else if (observer.Cat != null)
            {
                observer.Cat.Freeze();
            }
        }

        /// <summary>Called once per tick by RoomManager.OnStepped, only while IsHolding.</summary>
        public void StepHold()
        {
            if (hold.Step() == DeathHoldPhase.ResetNow) PerformReset();
        }

        void PerformReset()
        {
            // PAX-090 (D-091): with a checkpoint section's gate passed, the same reset, then a rewind to the gate: the cat
            // respawns at the section's checkpoint and every trap is restored to its snapshot. Otherwise exactly as before.
            bool rewind = rooms != null && rooms.TryRewind(pendingRoom, out Vector2 spawn, out Vector2 gravity) && RespawnAtSection(spawn, gravity);
            if (!rewind) checkpoints.Respawn(pendingObserver);

            anchorResets.Clear();
            RoomResetResult result = resetRegistry.Reset(pendingRoom, anchorResets);
            if (rewind)
            {
                rooms.RestoreRewind();
                if (anchorResets.Count > 0)
                    Debug.LogError($"RoomDeath: room {pendingRoom} has checkpoint sections and owns {anchorResets.Count} anchor(s); a rewind returns them to their initial values, not to the gate (D-091 forbids anchors in a sectioned room).", this);
            }
            int anchorsSent = 0;
            if (transportHost == null || transportHost.Transport == null || transportHost.Sequencer == null)
            {
                if (anchorResets.Count > 0) Debug.LogError("RoomDeath: missing TransportHost; room anchor resets were not requested.", this);
            }
            else if (!transportHost.Transport.IsSessionAuthority)
            {
                if (anchorResets.Count > 0) Debug.LogError("RoomDeath: non-authority cannot request room anchor resets.", this);
            }
            else
            {
                for (int i = 0; i < anchorResets.Count; i++)
                {
                    AnchorReset reset = anchorResets[i];
                    if (!transportHost.Registry.TryGet(reset.AnchorId, out _)) continue;
                    var request = new AnchorRequest(reset.AnchorId, reset.InitialValue, EventOrigin.System,
                        transportHost.Sequencer.Next(EventOrigin.System));
                    transportHost.Transport.RequestAnchor(request);
                    anchorsSent++;
                }
            }

            Died?.Invoke(new DeathInfo(pendingObserver.Id, pendingCause, pendingRoom, pendingTick, result.TrapsReset, anchorsSent));
        }

        // True once RespawnAt is called. Like today's checkpoints.Respawn, it can't see CatRespawn's same-frame guard (a
        // second respawn in one Time.frameCount is dropped). A rewind comes a whole death hold after anything before it, so
        // in Play it's always a later frame; EditMode rigs don't advance frames, hence one rewind per rig (§11 Q4).
        bool RespawnAtSection(Vector2 spawn, Vector2 gravity)
        {
            CatRespawn respawn = pendingObserver.Cat != null ? pendingObserver.Cat.GetComponent<CatRespawn>() : null;
            if (respawn == null)
            {
                Debug.LogError($"RoomDeath: cat of {pendingObserver.Id} has no CatRespawn; it can't respawn at the section's checkpoint.", this);
                return false;
            }
            respawn.RespawnAt(spawn, gravity);
            // Box2D keeps the cat's contacts (and their warm-start impulses) from where it died; dropping them makes the
            // rewind independent of the death, so it replays the same from the checkpoint every time (D-091 (3)).
            Rigidbody2D body = pendingObserver.Cat.GetComponent<Rigidbody2D>();
            if (body != null) { body.simulated = false; body.simulated = true; }
            return true;
        }
    }
}
