using System;
using System.Globalization;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-053 (D-063) §2.3: one row per LevelListConfig entry, built at runtime by
    /// LevelSelectController (never a hand-placed button per level). A locked row can't be
    /// tapped; an unlocked one loads its own scene through LevelSceneLoader (§2.2). PAX-066 (D-279): a paywalled row, reached or not,
    /// can be tapped, and asks for the unlock panel instead. PAX-A09: a row is drawn as a cell: its number, a lock, a done mark and the best
    /// death count, and a glow on the level that's next.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class LevelSelectRow : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text nameText;
        [SerializeField] Text bestDeathsText;
        [SerializeField] Image lockIcon;
        [SerializeField] Image doneMark;
        [SerializeField] Image glow;
        [SerializeField] Color lockedTint = new Color(1f, 0.97f, 0.9f, 0.45f);
        [SerializeField] Color paywalledTint = new Color(1f, 0.87f, 0.58f, 1f);

        string sceneName;
        bool unlocked;
        Action<LevelSelectEntry> paywalled;
        LevelSelectEntry entry;

        void OnEnable() { if (button != null) button.onClick.AddListener(Tap); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(Tap); }

        public void Configure(LevelSelectEntry entry, Action<LevelSelectEntry> onPaywalled = null, int number = 0)
        {
            this.entry = entry;
            paywalled = onPaywalled;
            sceneName = entry.SceneName;
            unlocked = entry.IsUnlocked;
            LevelCellState state = entry.State;
            bool locked = state == LevelCellState.Locked || state == LevelCellState.Paywalled;
            if (nameText != null)
            {
                nameText.text = number > 0 ? number.ToString(CultureInfo.InvariantCulture) : entry.DisplayName;
                nameText.enabled = !locked;
                nameText.rectTransform.anchorMin = new Vector2(0f, state == LevelCellState.Done ? 0.3f : 0f);   // alone in the cell, it's centred
            }
            if (bestDeathsText != null) bestDeathsText.text = state == LevelCellState.Done ? "Best " + entry.BestDeathsText : "";
            if (lockIcon != null)
            {
                lockIcon.enabled = locked;
                lockIcon.color = state == LevelCellState.Paywalled ? paywalledTint : lockedTint;
            }
            if (doneMark != null) doneMark.enabled = state == LevelCellState.Done;
            if (glow != null) glow.enabled = state == LevelCellState.Open;
            if (button != null) button.interactable = unlocked || (entry.IsPaywalled && paywalled != null);
        }

        public void Tap()
        {
            if (entry.IsPaywalled && paywalled != null) { paywalled(entry); return; }
            if (!unlocked) return;
            LevelSceneLoader.Load(sceneName);
        }
    }
}
