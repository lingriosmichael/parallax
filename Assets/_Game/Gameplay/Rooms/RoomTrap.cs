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
        [Header("PAX-045 Timing")]
        [SerializeField] TrapTriggerSource triggerSource;
        [SerializeField] RoomTrap chainSource;
        [SerializeField] TrapRepeatMode repeatMode;
        [SerializeField] int cooldownTicks;
        [SerializeField] int periodTicks = 1;
        [SerializeField] int phaseTicks;

        public int RoomId => roomId;
        public TrapState State { get; protected set; }
        protected bool IsRoomLive => roomDeath != null && roomDeath.IsRoomLive(roomId);
        protected RoomDeath Death => roomDeath;
        protected RealityRoot Reality { get; private set; }
        TrapTiming timing;
        public int LatestFireTick => timing == null ? -1 : timing.LatestFireTick;
        protected bool UsesOverlapSource => triggerSource == TrapTriggerSource.Overlap;
        protected bool IsPeriodic => repeatMode == TrapRepeatMode.Periodic;
        protected int RoomLifeTick => rooms != null ? rooms.RoomLifeTick : 0;
        protected bool IsTimingEffectActive => timing != null && timing.IsEffectActive;

        protected virtual void Awake()
        {
            State = initialState;
            Reality = GetComponentInParent<RealityRoot>();
            timing = new TrapTiming(triggerSource, repeatMode, DelayTicks, cooldownTicks, periodTicks, phaseTicks);
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
            timing?.Reset();
            OnReset();
        }

        protected virtual int DelayTicks => 0;
        protected bool StepTiming(bool overlapping)
        {
            if (timing == null) return false;
            bool fired = timing.Step(rooms != null ? rooms.RoomLifeTick : 0, overlapping, chainSource != null ? chainSource.LatestFireTick : -1);
            if (timing.JustRearmed) OnTimingRearmed();
            if (fired) State = TrapState.Fired;
            else if (timing.IsArmed) State = TrapState.Armed;
            return fired;
        }

        protected abstract void OnReset();
        protected abstract void OnLiveRoomStep();
        protected virtual void OnTimingRearmed() { }

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
        protected bool TryGetLocalHumanColliderBounds(ObserverSet observers, out Bounds bounds)
        {
            bounds = default;
            if (Reality == null || observers == null) return false;
            ObserverContext candidate = observers.Get(Reality.Id);
            if (candidate == null || candidate.Driver == null || candidate.Driver.Kind != InputSourceKind.LocalHuman || candidate.Cat == null) return false;
            foreach (Collider2D collider in candidate.Cat.GetComponents<Collider2D>())
            {
                if (collider.isTrigger) continue;
                bounds = collider.bounds;
                return true;
            }
            return false;
        }
    }
}
