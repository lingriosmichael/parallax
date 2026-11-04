using UnityEngine;

namespace Parallax.Gameplay.Presentation
{
    /// <summary>D-294: a soft shadow along the bottom of the view, a child of the camera, sized to whatever the camera shows on this
    /// screen. Presentation only.</summary>
    public sealed class ViewEdgeShade : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] SpriteRenderer shade;
        [SerializeField, Min(0.01f)] float height = 1.3f;

        const float WidestAspect = 3f;   // wider than any phone, so its ends never show

        public void Configure(Camera camera, SpriteRenderer renderer, float units)
        {
            view = camera;
            shade = renderer;
            height = units;
            Place();
        }

        void LateUpdate() => Place();

        void Place()
        {
            if (view == null || shade == null || shade.sprite == null) return;
            Vector2 native = shade.sprite.bounds.size;
            float half = view.orthographicSize, wide = 2f * half * WidestAspect;
            transform.localScale = new Vector3(wide / native.x, height / native.y, 1f);
            transform.localPosition = new Vector3(0f, height * 0.5f - half, 1f);
        }
    }
}
