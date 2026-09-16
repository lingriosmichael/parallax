using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Reality
{
    public sealed class RealityRoot : MonoBehaviour
    {
        const float PositionTolerance = 0.001f;

        [SerializeField] ObserverId id;

        LayerMask? physicsMask;

        public ObserverId Id => id;

        // Lazily resolved so it's correct regardless of Awake order across GameObjects
        // (a child cat's Awake can run before its parent RealityRoot's).
        public LayerMask PhysicsMask
        {
            get
            {
                if (physicsMask == null) ResolvePhysicsMask();
                return physicsMask.Value;
            }
        }

        void ResolvePhysicsMask()
        {
            string layerName = RealitySpace.PhysicsLayerName(id);
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogError($"RealityRoot '{gameObject.name}': physics layer '{layerName}' does not exist. Assign it in Project Settings > Tags and Layers.", this);
                physicsMask = 0;
            }
            else
            {
                physicsMask = 1 << layer;
            }
        }

        void Awake()
        {
            ResolvePhysicsMask();

            Vector2 expectedOrigin = RealitySpace.Origin(id);
            if (Vector2.Distance(transform.position, expectedOrigin) > PositionTolerance)
            {
                Debug.LogError($"RealityRoot '{gameObject.name}': position {(Vector2)transform.position} does not match expected origin {expectedOrigin} for {id}.", this);
            }

            if (transform.rotation != Quaternion.identity || transform.localScale != Vector3.one)
            {
                Debug.LogError($"RealityRoot '{gameObject.name}': rotation/scale must be identity (rotation {transform.rotation.eulerAngles}, scale {transform.localScale}).", this);
            }
        }

        public Vector2 ToLocal(Vector2 world) => world - RealitySpace.Origin(id);
        public Vector2 ToWorld(Vector2 local) => local + RealitySpace.Origin(id);

        public Vector2 MapTo(RealityRoot other, Vector2 worldPos) =>
            RealitySpace.MapTo(id, other.Id, worldPos);
    }
}
