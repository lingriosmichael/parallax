using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Gameplay.Checkpoints
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FallResetVolume : MonoBehaviour
    {
        [SerializeField] SpawnPoint spawn;
        [SerializeField] LayerMask catMask = ~0;

        BoxCollider2D box;
        ContactFilter2D filter;
        readonly Collider2D[] results = new Collider2D[8];

        void Reset()
        {
            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
        }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;

            if (spawn == null)
            {
                Debug.LogError($"FallResetVolume: '{gameObject.name}' has no SpawnPoint assigned. Disabling.", this);
                enabled = false;
                return;
            }

            filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(catMask);
        }

        void FixedUpdate()
        {
            int count = box.Overlap(filter, results);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = results[i].attachedRigidbody;
                if (body == null) continue;

                var respawn = body.GetComponent<CatRespawn>();
                if (respawn == null) continue;

                respawn.RespawnAt(spawn.Position, spawn.GravityDirection);
            }
        }
    }
}
