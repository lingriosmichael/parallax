using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public abstract class RoomTrap : MonoBehaviour, IRoomResettable
    {
        [SerializeField] int roomId;
        [SerializeField] TrapState initialState = TrapState.Armed;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] RoomManager rooms;

        public int RoomId => roomId;
        public TrapState State { get; protected set; }
        protected bool IsRoomLive => roomDeath != null && roomDeath.IsRoomLive(roomId);

        protected virtual void Awake() => State = initialState;

        protected virtual void OnEnable()
        {
            if (roomDeath == null)
            {
                Debug.LogError($"RoomTrap '{name}' has no RoomDeath assigned. Disabling.", this);
                enabled = false;
                return;
            }
            roomDeath.ResetRegistry.Register(this);
            if (rooms == null)
            {
                Debug.LogError($"RoomTrap '{name}' has no RoomManager assigned. Disabling.", this);
                enabled = false;
                return;
            }
            rooms.RegisterTrap(this);
        }

        protected virtual void OnDisable()
        {
            if (roomDeath != null) roomDeath.ResetRegistry.Unregister(this);
            if (rooms != null) rooms.UnregisterTrap(this);
        }

        public void StepIfLive()
        {
            if (IsRoomLive && State == TrapState.Armed) OnLiveRoomStep();
        }

        public void ResetToInitial()
        {
            State = initialState;
            OnReset();
        }

        protected abstract void OnReset();
        protected abstract void OnLiveRoomStep();
    }
}
