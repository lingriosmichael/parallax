using System;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-053 (D-063) §2.3: one row per LevelListConfig entry, built at runtime by
    /// LevelSelectController (never a hand-placed button per level). A locked row can't be
    /// tapped; an unlocked one loads its own scene through LevelSceneLoader (§2.2). PAX-066 (D-279): a paywalled row, reached or not,
    /// can be tapped, and asks for the unlock panel instead.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class LevelSelectRow : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text nameText;
        [SerializeField] Text bestDeathsText;

        string sceneName;
        bool unlocked;
        Action<LevelSelectEntry> paywalled;
        LevelSelectEntry entry;

        void OnEnable() { if (button != null) button.onClick.AddListener(Tap); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(Tap); }

        public void Configure(LevelSelectEntry entry, Action<LevelSelectEntry> onPaywalled = null)
        {
            this.entry = entry;
            paywalled = onPaywalled;
            sceneName = entry.SceneName;
            unlocked = entry.IsUnlocked;
            if (nameText != null) nameText.text = entry.DisplayName;
            if (bestDeathsText != null) bestDeathsText.text = entry.BestDeathsText;
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
