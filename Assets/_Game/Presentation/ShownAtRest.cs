using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A18 W3 (2026-10-17, the developer: "the box on the left goes slightly to far and over the edge", "Cat is
    /// overlapping the rubble"): the stone a trap lends its neighbours (SoloRoomSkin's TrapReach: over a fixed face's broken
    /// strip, a broken top corner, a hole's rim) shows exactly while the trap's skin is shown at its rest pose. It's the
    /// neighbour's stone, not the trap's: once the trap moves, it stays behind (hidden), so it never rides out past the trap's
    /// collider. A vine's reach to the stone over it (SoloRoomSkin) works the same way. Runs after ShownWith. Presentation only.</summary>
    [DefaultExecutionOrder(1001)]
    public sealed class ShownAtRest : MonoBehaviour
    {
        [SerializeField] SpriteRenderer owner;
        [Tooltip("The owner's world position at rest (where the lent stone belongs).")]
        [SerializeField] Vector2 rest;
        [SerializeField] SpriteRenderer[] renderers = new SpriteRenderer[0];

        public SpriteRenderer Owner => owner;
        public SpriteRenderer[] Renderers => renderers;

        void LateUpdate() => Apply();

        public void Apply()
        {
            bool shown = owner != null && owner.enabled && owner.gameObject.activeInHierarchy
                && ((Vector2)owner.transform.position - rest).sqrMagnitude < 1e-6f && Mathf.Abs(owner.transform.eulerAngles.z) < 1e-3f;
            foreach (SpriteRenderer r in renderers) if (r != null) r.enabled = shown;
        }
    }
}
