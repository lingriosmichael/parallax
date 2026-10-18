using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A18 W3 (2026-10-17, the developer: "the box on the left goes slightly to far and over the edge", "Cat is
    /// overlapping the rubble"): the stone a trap lends its neighbours (SoloRoomSkin's TrapReach: over a fixed face's broken
    /// strip, a broken top corner, a hole's rim) shows exactly while the trap's skin is shown at its rest pose. It's the
    /// neighbour's stone, not the trap's: once the trap moves, it stays behind (hidden), so it never rides out past the trap's
    /// collider. A vine's reach to the stone over it (SoloRoomSkin) works the same way; with whenGone, the trims a resting trap
    /// covers show only once it has gone. Runs after ShownWith. Presentation only.</summary>
    [DefaultExecutionOrder(1001)]
    public sealed class ShownAtRest : MonoBehaviour
    {
        [SerializeField] SpriteRenderer owner;
        [Tooltip("The owner's world position at rest (where the lent stone belongs).")]
        [SerializeField] Vector2 rest;
        [SerializeField] SpriteRenderer[] renderers = new SpriteRenderer[0];
        [Tooltip("The other way round: shown once the owner has gone (hidden, moved or turned), hidden while it rests there. A fixed " +
            "top's lit course under a trap floor, which would show below the trap as a stripe through the stone.")]
        [SerializeField] bool whenGone;
        [Tooltip("Shown while the owner is shown away from its rest (its own trims that its resting place hides: a falling block's " +
            "top course, inside the ceiling it hangs from until it falls).")]
        [SerializeField] bool whenMoved;
        [Tooltip("Only hide (when the condition is off); never show. For renderers another component shows and hides (a hazard's " +
            "blades, which SpikeArt draws; switching all of them on drew its unused legacy strip too).")]
        [SerializeField] bool hideOnly;

        public SpriteRenderer Owner => owner;
        public SpriteRenderer[] Renderers => renderers;

        void LateUpdate() => Apply();

        public void Apply()
        {
            bool visible = owner != null && owner.enabled && owner.gameObject.activeInHierarchy;
            bool still = ((Vector2)(owner != null ? owner.transform.position : Vector3.zero) - rest).sqrMagnitude < 1e-6f
                && owner != null && Mathf.Abs(owner.transform.eulerAngles.z) < 1e-3f;
            bool shown = whenMoved ? visible && !still : (visible && still) != whenGone;
            foreach (SpriteRenderer r in renderers) if (r != null && (!hideOnly || !shown)) r.enabled = shown;
        }
    }
}
