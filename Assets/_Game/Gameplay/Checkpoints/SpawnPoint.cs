using UnityEngine;
using Parallax.Core;

namespace Parallax.Gameplay.Checkpoints
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] Vector2 gravityDirection = Vector2.down;

        public Vector2 Position => transform.position;
        public Vector2 GravityDirection => VerticalGravity.Quantize(gravityDirection, Vector2.down);

        void OnDrawGizmos()
        {
            Vector2 pos = Position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos, 0.25f);

            Vector2 dir = GravityDirection;
            if (dir.sqrMagnitude > 1e-4f)
            {
                Vector2 tip = pos + dir * 0.75f;
                Gizmos.DrawLine(pos, tip);

                Vector2 right = new Vector2(-dir.y, dir.x);
                Gizmos.DrawLine(tip, tip - dir * 0.2f + right * 0.15f);
                Gizmos.DrawLine(tip, tip - dir * 0.2f - right * 0.15f);
            }
        }
    }
}
