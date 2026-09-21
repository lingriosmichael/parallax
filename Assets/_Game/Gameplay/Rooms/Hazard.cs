using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Hazard : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] RoomDeath roomDeath;
        [SerializeField] RoomManager rooms;

        BoxCollider2D box;
        RealityRoot reality;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];
        [SerializeField] bool armed = true;
        public bool Armed => armed;
        public void SetArmed(bool value) => armed = value;

        void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            reality = GetComponentInParent<RealityRoot>();
            if (reality == null || observers == null || roomDeath == null || rooms == null)
            {
                Debug.LogError($"Hazard '{name}' requires RealityRoot, ObserverSet, RoomDeath and RoomManager. Disabling.", this);
                enabled = false;
                return;
            }
            filter = new ContactFilter2D { useLayerMask = true, layerMask = reality.PhysicsMask, useTriggers = false };
        }

        void OnEnable()
        {
            if (rooms != null) rooms.RegisterHazard(this);
        }

        void OnDisable()
        {
            if (rooms != null) rooms.UnregisterHazard(this);
        }

        public void KillOverlappingCat()
        {
            if (!enabled || !armed || box == null || reality == null || observers == null || roomDeath == null) return;
            ObserverContext observer = observers.Get(reality.Id);
            if (observer == null || observer.Cat == null) return;
            Collider2D catCollider = observer.Cat.GetComponent<Collider2D>();
            if (catCollider == null) return;

            Bounds bounds = box.bounds;
            int count = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] != catCollider) continue;
                roomDeath.Kill(observer.Id, DeathCause.Hazard);
                return;
            }
        }
    }
}
