using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-108 (D-157): a word drawn one Text per letter, so its letter spacing can be set (uGUI's Text has none).
    /// The letters are laid out side by side from their own preferred widths plus Spacing ems between them, centred on this
    /// RectTransform. D-280: each letter can also carry its own alpha, lift and tilt, and the word tells them where the light is.</summary>
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

        /// <summary>D-280: one letter's pose: its alpha, its lift off the line (canvas units) and its tilt (degrees).</summary>
        public void SetLetter(int index, float alpha, float lift, float tilt)
        {
            if (index < 0 || index >= letters.Length || letters[index] == null) return;
            Text letter = letters[index];
            Color c = letter.color;
            c.a = alpha;
            letter.color = c;
            RectTransform rect = letter.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, lift);
            rect.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }

        public int Count => letters.Length;

        /// <summary>D-280: the light's place across the word, 0 to 1, or negative for none.</summary>
        public void SetSheen(float position)
        {
            foreach (Text letter in letters)
                if (letter != null && letter.TryGetComponent(out GildedText gilding)) gilding.SetSheen(position, width);
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

        float width;

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
            width = total;
            float x = -total * 0.5f;
            for (int i = 0; i < letters.Length; i++)
            {
                Text letter = letters[i];
                if (letter == null) continue;
                if (i > 0) x += spacing * letter.fontSize;
                float letterWidth = letter.preferredWidth;
                var rect = letter.rectTransform;
                rect.anchoredPosition = new Vector2(x + letterWidth * 0.5f, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(letterWidth, rect.sizeDelta.y);
                x += letterWidth;
            }
        }
    }
}
