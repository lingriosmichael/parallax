using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>Premium plan Phase 6 (D-314): a menu screen opens out of black instead of cutting in: its cover fades from the colour it
    /// was built with to clear over `seconds`, eased, on unscaled time, then turns itself off. It never takes a tap. Menus only (V06:
    /// never during play); the title has its own intro cover. Written by PARALLAX/Setup/Level Select.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenReveal : MonoBehaviour
    {
        [SerializeField] float seconds = 0.45f;

        Image cover;
        float alpha, clock;

        void Awake()
        {
            cover = GetComponent<Image>();
            cover.raycastTarget = false;
            alpha = cover.color.a;
        }

        void OnEnable() => clock = 0f;

        void Update()
        {
            clock += Time.unscaledDeltaTime;
            float u = seconds > 0f ? Mathf.Clamp01(clock / seconds) : 1f;
            Color c = cover.color;
            c.a = alpha * (1f - u) * (1f - u);
            cover.color = c;
            if (u >= 1f) cover.enabled = false;
        }
    }
}
