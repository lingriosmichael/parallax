using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Checkpoints
{
    public sealed class CheckpointMarker : MonoBehaviour
    {
        [SerializeField] int checkpointId;
        [SerializeField] Vector2 gravityDirection = Vector2.down;
        [SerializeField] Vector2 zoneSize = new Vector2(1.5f, 2f);
        [SerializeField] CheckpointManager manager;
        [SerializeField] ObserverSet observers;

        RealityRoot root;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];
        bool initialized;
        bool missingLogged;

        public int Id => checkpointId;
        public ObserverId Reality => root != null ? root.Id : default;
        public Vector2 LocalPosition => root != null ? root.ToLocal(transform.position) : (Vector2)transform.position;
        public Vector2 GravityDirection => gravityDirection.normalized;

        void OnEnable()
        {
            EnsureInit();
            if (observers != null) observers.Stepped += OnStepped;
            if (manager != null) manager.Register(this);
        }

        void OnDisable()
        {
            if (observers != null) observers.Stepped -= OnStepped;
            if (manager != null) manager.Unregister(this);
        }

        void EnsureInit()
        {
            if (initialized) return;
            root = GetComponentInParent<RealityRoot>();
            if (root == null || manager == null || observers == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogError($"CheckpointMarker '{name}' needs a RealityRoot parent, a CheckpointManager, and an ObserverSet.", this);
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

        void OnStepped(int tick)
        {
            EnsureInit();
            if (!initialized) return;
            if (checkpointId <= manager.Current) return;

            ObserverContext context = observers.Get(root.Id);
            if (context == null || context.Cat == null || context.Driver == null || !CheckpointPolicy.Activates(context.Driver.Kind)) return;

            Collider2D catCollider = context.Cat.GetComponent<Collider2D>();
            if (catCollider == null) return;

            int count = Physics2D.OverlapBox(transform.position, zoneSize, transform.eulerAngles.z, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] == catCollider)
                {
                    manager.Activate(checkpointId);
                    return;
                }
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zoneSize);
            Gizmos.matrix = old;

            Vector2 dir = gravityDirection.normalized;
            if (dir.sqrMagnitude > 1e-4f)
            {
                Vector2 pos = transform.position;
                Vector2 tip = pos + dir * 0.75f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, tip);
                Vector2 right = new Vector2(-dir.y, dir.x);
                Gizmos.DrawLine(tip, tip - dir * 0.2f + right * 0.15f);
                Gizmos.DrawLine(tip, tip - dir * 0.2f - right * 0.15f);
            }
        }
    }
}
