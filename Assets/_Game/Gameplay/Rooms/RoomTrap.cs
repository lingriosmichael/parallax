using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
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
        protected RoomDeath Death => roomDeath;
        protected RealityRoot Reality { get; private set; }

        protected virtual void Awake()
        {
            State = initialState;
            Reality = GetComponentInParent<RealityRoot>();
        }

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
            if (IsRoomLive) OnLiveRoomStep();
        }

        public void ResetToInitial()
        {
            State = initialState;
            OnReset();
        }

        protected abstract void OnReset();
        protected abstract void OnLiveRoomStep();

        protected bool IsLocalHumanOverlapping(BoxCollider2D box, ObserverSet observers, ContactFilter2D filter, Collider2D[] results, out ObserverContext observer)
        {
            return IsLocalHumanOverlapping(box.bounds, observers, filter, results, out observer);
        }
        protected bool IsLocalHumanOverlapping(Bounds bounds, ObserverSet observers, ContactFilter2D filter, Collider2D[] results, out ObserverContext observer)
        {
            observer = null;
            if (Reality == null || observers == null) return false;
            ObserverContext candidate = observers.Get(Reality.Id);
            if (candidate == null || candidate.Driver == null || candidate.Driver.Kind != InputSourceKind.LocalHuman || candidate.Cat == null) return false;
            Collider2D cat = candidate.Cat.GetComponent<Collider2D>();
            if (cat == null) return false;
            int count = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, filter, results);
            for (int i = 0; i < count; i++) if (results[i] == cat) { observer = candidate; return true; }
            return false;
        }
    }
}
