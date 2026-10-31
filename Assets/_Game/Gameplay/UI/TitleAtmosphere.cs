using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>D-280: the air over the title. Light shafts that breathe, golden motes that rise and twinkle, the sigil's two rings turning
    /// against each other, a glow that pulses behind the wordmark, and foreground leaves that sway. Every value is a function of the
    /// clock TitleScreen passes in; nothing here is random or touches gameplay.</summary>
    public sealed class TitleAtmosphere : MonoBehaviour
    {
        [SerializeField] Image[] rays = new Image[0];
        [SerializeField] Image[] motes = new Image[0];
        [SerializeField] RectTransform[] ferns = new RectTransform[0];
        [SerializeField] Image sigilOuter;
        [SerializeField] Image sigilInner;
        [SerializeField] Image glow;
        [SerializeField] float sigilAlpha = 0.5f;
        [SerializeField] float rayAlpha = 0.17f;
        [SerializeField] float moteAlpha = 0.75f;
        [SerializeField] float outerTurn = 2.2f;
        [SerializeField] float innerTurn = -3.4f;
        [Tooltip("The canvas's reference size, which the motes' 0 to 1 positions scale to.")]
        [SerializeField] Vector2 field = new Vector2(2400f, 1080f);

        float glowAlpha = -1f;

        /// <summary>The atmosphere at second s; reveal, 0 to 1, brings in what belongs to the wordmark.</summary>
        public void Apply(float s, float reveal)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i] == null) continue;
                Color c = rays[i].color;
                c.a = rayAlpha * Mathf.Clamp01(s / 2f) * (0.62f + 0.38f * Mathf.Sin(s * (0.31f + i * 0.13f) + i * 2.1f));
                rays[i].color = c;
            }
            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i] == null) continue;
                float seed = Frac(i * 0.61803f), speed = 0.012f + 0.022f * Frac(i * 0.37139f);
                float y = Frac(Frac(i * 0.27183f) + s * speed);
                float x = Frac(seed + 0.012f * Mathf.Sin(s * 0.5f + i)) + 0.018f * Mathf.Sin(s * (0.3f + seed) + i * 1.7f);
                motes[i].rectTransform.anchoredPosition = new Vector2((x - 0.5f) * field.x, (y - 0.5f) * field.y);
                Color c = motes[i].color;
                float twinkle = 0.55f + 0.45f * Mathf.Sin(s * (1.1f + seed) + i * 3.3f);
                c.a = moteAlpha * Mathf.Sin(y * Mathf.PI) * twinkle * Mathf.Clamp01(s / 2f);
                motes[i].color = c;
            }
            for (int i = 0; i < ferns.Length; i++)
                if (ferns[i] != null) ferns[i].localRotation = Quaternion.Euler(0f, 0f, 1.4f * Mathf.Sin(s * (0.42f + i * 0.09f) + i));
            Ring(sigilOuter, s * outerTurn, reveal);
            Ring(sigilInner, s * innerTurn, reveal);
            if (glow != null)
            {
                if (glowAlpha < 0f) glowAlpha = glow.color.a;
                Color c = glow.color;
                c.a = glowAlpha * reveal * (0.8f + 0.2f * Mathf.Sin(s * 0.8f));
                glow.color = c;
            }
        }

        void Ring(Image ring, float degrees, float reveal)
        {
            if (ring == null) return;
            ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, degrees);
            Color c = ring.color;
            c.a = sigilAlpha * reveal;
            ring.color = c;
        }

        static float Frac(float x) => x - Mathf.Floor(x);
    }
}
