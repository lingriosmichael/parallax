using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-108 (D-157): a word drawn one Text per letter, so its letter spacing can be set (uGUI's Text has none).
    /// The letters are laid out side by side from their own preferred widths plus Spacing ems between them, centred on this
    /// RectTransform.</summary>
    [ExecuteAlways]
    public sealed class LetterSpacedText : MonoBehaviour
    {
        [SerializeField] Text[] letters = new Text[0];
        [Tooltip("The gap between letters, in ems (multiples of the font size).")]
        [SerializeField] float spacing = 0.4f;

        public float Spacing
        {
            get => spacing;
            set { if (Mathf.Approximately(spacing, value)) return; spacing = value; Layout(); }
        }

        public void SetAlpha(float alpha)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                if (letters[i] == null) continue;
                Color c = letters[i].color;
                c.a = alpha;
                letters[i].color = c;
            }
        }

        void OnEnable() => Layout();

        public void Layout()
        {
            if (letters == null || letters.Length == 0) return;
            float total = 0f;
            for (int i = 0; i < letters.Length; i++)
            {
                if (letters[i] == null) continue;
                total += letters[i].preferredWidth;
                if (i > 0) total += spacing * letters[i].fontSize;
            }
            float x = -total * 0.5f;
            for (int i = 0; i < letters.Length; i++)
            {
                Text letter = letters[i];
                if (letter == null) continue;
                if (i > 0) x += spacing * letter.fontSize;
                float width = letter.preferredWidth;
                var rect = letter.rectTransform;
                rect.anchoredPosition = new Vector2(x + width * 0.5f, 0f);
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                x += width;
            }
        }
    }
}
