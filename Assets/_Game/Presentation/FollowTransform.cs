using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A16 gauntlet: keeps a presentation object (a 2D light) at a target's position plus an offset, after the
    /// camera has moved: the cat's key light follows the cat, the sun's backlight follows the camera. Presentation only;
    /// it never touches the target.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(210)]
    public sealed class FollowTransform : MonoBehaviour
    {
        [SerializeField] Transform target;
        [Tooltip("Offset from the target, in units.")]
        [SerializeField] Vector2 offset;

        public Transform Target => target;

        public void Configure(Transform follow, Vector2 by)
        {
            target = follow; offset = by;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = target.position;
            transform.position = new Vector3(p.x + offset.x, p.y + offset.y, transform.position.z);
        }
    }
}
