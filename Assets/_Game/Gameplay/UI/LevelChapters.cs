using System;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>Premium plan Phase 6 (D-314): Level Select in chapters. One tab per chapter (a district of ten levels, in list
    /// order), and the chapter's own art behind its levels: its landmark rising from the foot of the screen, over the sky tinted to
    /// its time of day. LevelSelectController shows only the chosen chapter's levels and opens on the chapter the player is up to.
    /// Presentation only: it changes no unlock rule and saves nothing. Written by PARALLAX/Setup/Level Select.</summary>
    public sealed class LevelChapters : MonoBehaviour
    {
        [Serializable]
        public struct Chapter
        {
            public string Name;
            public int Count;
            public Sprite Art;
            public Color Sky;
        }

        [SerializeField] Chapter[] chapters = new Chapter[0];
        [SerializeField] Button[] tabs = new Button[0];
        [SerializeField] Image art;
        [SerializeField] Image sky;
        [SerializeField] Color tabOn = Color.white;
        [SerializeField] Color tabOff = new Color(1f, 1f, 1f, 0.55f);

        int current = -1;

        /// <summary>The player chose another chapter (LevelSelectController rebuilds its cells).</summary>
        public event Action Changed;

        public bool HasChapters => chapters != null && chapters.Length > 0;
        public int Current => current;

        void OnEnable()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i] != null) tabs[i].onClick.AddListener(() => Select(index));
            }
        }

        void OnDisable()
        {
            foreach (Button tab in tabs) if (tab != null) tab.onClick.RemoveAllListeners();
        }

        /// <summary>The levels (by list index) the chosen chapter holds.</summary>
        public void Range(out int first, out int count)
        {
            first = 0;
            for (int i = 0; i < current && i < chapters.Length; i++) first += chapters[i].Count;
            count = current >= 0 && current < chapters.Length ? chapters[current].Count : 0;
        }

        /// <summary>Opens, once, on the chapter holding list index `levelIndex` (the level the player is up to). No Changed.</summary>
        public void OpenAt(int levelIndex)
        {
            if (current >= 0 || !HasChapters) return;
            int first = 0, chosen = chapters.Length - 1;
            for (int i = 0; i < chapters.Length; i++)
            {
                if (levelIndex < first + chapters[i].Count) { chosen = i; break; }
                first += chapters[i].Count;
            }
            Show(chosen);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= chapters.Length || index == current) return;
            Show(index);
            Changed?.Invoke();
        }

        void Show(int index)
        {
            current = index;
            Chapter c = chapters[index];
            if (art != null) { art.sprite = c.Art; art.enabled = c.Art != null; art.preserveAspect = true; }
            if (sky != null) sky.color = c.Sky;
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i] != null && tabs[i].targetGraphic != null) tabs[i].targetGraphic.color = i == index ? tabOn : tabOff;
        }
    }
}
