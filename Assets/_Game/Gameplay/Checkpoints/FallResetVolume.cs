using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.Gameplay.Checkpoints
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FallResetVolume : MonoBehaviour
    {
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] ObserverSet observers;

        BoxCollider2D box;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];
        RealityRoot reality;

        void Reset()
        {
            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;

            if (checkpoints == null || observers == null)
            {
                Debug.LogError($"FallResetVolume: '{gameObject.name}' has no CheckpointManager or ObserverSet assigned. Disabling.", this);
                enabled = false;
                return;
            }

            reality = GetComponentInParent<RealityRoot>();
            if (reality == null)
            {
                Debug.LogError($"FallResetVolume: '{gameObject.name}' has no RealityRoot in its parent hierarchy. Overlap query will match nothing.", this);
                enabled = false;
                return;
            }

            filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(reality.PhysicsMask);
        }

        void FixedUpdate()
        {
            int count = box.Overlap(filter, results);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = results[i].attachedRigidbody;
                if (body == null) continue;

                ObserverContext context = observers.Get(reality.Id);
                if (context == null || context.Driver == null || !CheckpointPolicy.FallResets(context.Driver.Kind)) continue;
                if (context.Cat == null || body.gameObject != context.Cat.gameObject) continue;

                checkpoints.Respawn(context);
            }
        }
    }
}
