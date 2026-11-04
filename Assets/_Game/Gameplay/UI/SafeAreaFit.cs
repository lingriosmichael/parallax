using UnityEngine;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-A09: keeps a stretched rect inside the phone's safe area, so nothing in it sits under a camera cutout or a rounded corner.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFit : MonoBehaviour
    {
        Rect applied;

        public static (Vector2 Min, Vector2 Max) Anchors(Rect safeArea, Vector2 screen) => screen.x <= 0f || screen.y <= 0f
            ? (Vector2.zero, Vector2.one)
            : (safeArea.min / screen, safeArea.max / screen);

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != applied) Apply();
        }

        void Apply()
        {
            applied = Screen.safeArea;
            (Vector2 min, Vector2 max) = Anchors(applied, new Vector2(Screen.width, Screen.height));
            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
