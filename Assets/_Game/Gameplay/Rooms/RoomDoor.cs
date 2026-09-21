using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Rooms
{
    public sealed class RoomDoor : MonoBehaviour
    {
        [SerializeField] int roomId;
        [SerializeField] Vector2 zoneSize = new Vector2(1f, 1.5f);
        [SerializeField] RoomManager manager;

        RealityRoot root;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];
        bool initialized;
        bool missingLogged;

        public int RoomId => roomId;
        public ObserverId Reality => root != null ? root.Id : default;

        void OnEnable()
        {
            EnsureInit();
            if (manager != null) manager.Register(this);
        }

        void OnDisable()
        {
            if (manager != null) manager.Unregister(this);
        }

        void EnsureInit()
        {
            if (initialized) return;
            root = GetComponentInParent<RealityRoot>();
            if (root == null || manager == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogError($"RoomDoor '{name}' needs a RealityRoot parent and a RoomManager.", this);
                }
                return;
            }
            filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = root.PhysicsMask,
                useTriggers = false,
            };
            initialized = true;
        }

        public bool IsTouchedBy(ObserverContext observer)
        {
            EnsureInit();
            if (!initialized || observer == null || observer.Cat == null) return false;

            Collider2D catCollider = observer.Cat.GetComponent<Collider2D>();
            if (catCollider == null) return false;

            int count = Physics2D.OverlapBox(transform.position, zoneSize, transform.eulerAngles.z, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] == catCollider) return true;
            }
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zoneSize);
            Gizmos.matrix = old;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneSize.y * 0.5f + 0.2f), $"Door {roomId}");
#endif
        }
    }
}
