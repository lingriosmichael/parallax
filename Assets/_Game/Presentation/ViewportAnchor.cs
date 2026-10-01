using UnityEngine;

namespace Parallax.Presentation
{
    /// <summary>PAX-A16 §3.4 tier 7: pins a camera child to a point of the view (the frame's corners and top edge), whatever
    /// the screen's aspect, and optionally stretches a tiled sprite across the view's width. Presentation only.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(200)]
    public sealed class ViewportAnchor : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [Tooltip("Where in the view, -1..1 on each axis (-1, -1 is the bottom-left corner).")]
        [SerializeField] Vector2 anchor;
        [Tooltip("Offset from the anchor, in units.")]
        [SerializeField] Vector2 offset;
        [Tooltip("A tiled SpriteRenderer here is widened to the view's width plus this margin (0: leave its size).")]
        [SerializeField] float stretchMargin;

        public void Configure(Camera cam, Vector2 at, Vector2 by, float stretch)
        {
            viewCamera = cam; anchor = at; offset = by; stretchMargin = stretch;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (viewCamera == null || !viewCamera.orthographic) return;
            float h = viewCamera.orthographicSize, w = h * viewCamera.aspect;
            transform.localPosition = new Vector3(anchor.x * w + offset.x, anchor.y * h + offset.y, transform.localPosition.z);
            if (stretchMargin > 0f && TryGetComponent(out SpriteRenderer r) && r.drawMode != SpriteDrawMode.Simple)
                r.size = new Vector2(2f * w + stretchMargin, r.size.y);
        }
    }
}
