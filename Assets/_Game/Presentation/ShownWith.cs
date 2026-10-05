using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A15: renderers that show exactly when an owner renderer does: the caps, edges and dressing on a
    /// disguised trap (its art skin is the owner), and the dark backing behind a flip ring. They are the owner's children,
    /// so they move with it; this only mirrors its visibility (a trap art presenter switches the owner's renderer, not its
    /// object). Runs after every presenter's LateUpdate. With followWidth, horizontal trims also keep the owner's width
    /// (a shrinking floor's cap shrinks with it), a plain sprite as wide as the owner (its face shade) narrows with it, and
    /// any other plain sprite (a tuft, a fern, ivy) shows only while its centre is over what's left of the owner (D-111: no
    /// plants left floating where the floor has gone).</summary>
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

        // followWidth: the owner's full width and each renderer's scale, taken on the first Apply (the floor is whole then).
        float fullWidth;
        Vector3[] baseScale;

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
                if (!shown || !followWidth) continue;
                if (r.drawMode != SpriteDrawMode.Simple)
                {
                    if (!Mathf.Approximately(r.size.x, owner.size.x)) r.size = new Vector2(owner.size.x, r.size.y);
                    continue;
                }
                CacheWidths();
                int i = System.Array.IndexOf(renderers, r);
                Bounds left = owner.bounds;
                if (fullWidth > 0f && Mathf.Abs(baseScale[i].x) * SpriteWidth(r) >= fullWidth * 0.9f)
                {
                    Vector3 s = baseScale[i];
                    r.transform.localScale = new Vector3(s.x * owner.size.x / fullWidth, s.y, s.z);
                    r.transform.position = new Vector3(left.center.x, r.transform.position.y, r.transform.position.z);
                    continue;
                }
                float x = r.transform.position.x;
                r.enabled = x >= left.min.x && x <= left.max.x;
            }
        }

        void CacheWidths()
        {
            if (baseScale != null) return;
            fullWidth = owner.size.x;
            baseScale = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseScale[i] = renderers[i] != null ? renderers[i].transform.localScale : Vector3.one;
        }

        // A plain sprite's width at unit scale (its sprite's own width).
        static float SpriteWidth(SpriteRenderer r) => r.sprite != null ? r.sprite.bounds.size.x : 0f;
    }
}
