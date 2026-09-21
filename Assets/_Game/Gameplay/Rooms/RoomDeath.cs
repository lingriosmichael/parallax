using System;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
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

        readonly DeathTickGuard tickGuard = new DeathTickGuard();
        readonly RoomResetRegistry resetRegistry = new RoomResetRegistry();
        readonly List<AnchorReset> anchorResets = new List<AnchorReset>();
        readonly Dictionary<ObserverId, int> killedTicks = new Dictionary<ObserverId, int>();

        public RoomResetRegistry ResetRegistry => resetRegistry;
        public bool IsRoomLive(int roomId) => rooms != null && rooms.IsLive(roomId);
        public event Action<DeathInfo> Died;

        public bool WasKilledThisTick(ObserverId observer, int tick) =>
            killedTicks.TryGetValue(observer, out int killedTick) && killedTick == tick;

        public void Kill(ObserverId observerId, DeathCause cause)
        {
            if (observers == null || checkpoints == null) return;
            ObserverContext observer = observers.Get(observerId);
            if (observer == null || observer.Driver == null || observer.Driver.Kind != InputSourceKind.LocalHuman) return;

            if (rooms == null || observerId != rooms.SoloReality)
            {
                checkpoints.Respawn(observer);
                return;
            }

            int tick = observers.Tick;
            if (!tickGuard.TryAccept(observerId, tick)) return;
            killedTicks[observerId] = tick;

            int room = rooms.CurrentRoom;
            if (checkpoints.Current != room)
            {
                Debug.LogError($"RoomDeath: current room {room} disagrees with checkpoint progress {checkpoints.Current}; using room {room}.", this);
            }
            checkpoints.Respawn(observer);

            anchorResets.Clear();
            RoomResetResult result = resetRegistry.Reset(room, anchorResets);
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

            Died?.Invoke(new DeathInfo(observerId, cause, room, tick, result.TrapsReset, anchorsSent));
        }
    }
}
