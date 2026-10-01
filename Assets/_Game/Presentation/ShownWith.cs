using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A15: renderers that show exactly when an owner renderer does: the caps, edges and dressing on a
    /// disguised trap (its art skin is the owner), and the dark backing behind a flip ring. They are the owner's children,
    /// so they move with it; this only mirrors its visibility (a trap art presenter switches the owner's renderer, not its
    /// object). Runs after every presenter's LateUpdate. With followWidth, horizontal trims also keep the owner's width
    /// (a shrinking floor's cap shrinks with it).</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class ShownWith : MonoBehaviour
    {
        [SerializeField] SpriteRenderer owner;
        [SerializeField] SpriteRenderer[] renderers = new SpriteRenderer[0];
        [SerializeField] bool followWidth;
        [Tooltip("A collapsing floor's first shard: while it's shown and still at holdAt (the reveal tick, A13 R8: that frame " +
                 "matches the one before), the renderers stay up; they go when the shards start to fall.")]
        [SerializeField] SpriteRenderer holdWith;
        [SerializeField] Vector2 holdAt;

        public SpriteRenderer Owner => owner;
        public SpriteRenderer[] Renderers => renderers;

        void LateUpdate() => Apply();

        public void Apply()
        {
            bool shown = owner != null && owner.enabled && owner.gameObject.activeInHierarchy;
            if (!shown && holdWith != null && holdWith.enabled && ((Vector2)holdWith.transform.position - holdAt).sqrMagnitude < 1e-8f
                && Mathf.Abs(holdWith.transform.eulerAngles.z) < 1e-3f) shown = true;
            foreach (SpriteRenderer r in renderers)
            {
                if (r == null) continue;
                r.enabled = shown;
                if (shown && followWidth && r.drawMode != SpriteDrawMode.Simple && !Mathf.Approximately(r.size.x, owner.size.x))
                    r.size = new Vector2(owner.size.x, r.size.y);
            }
        }
    }
}
