using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-050 (D-063): on click, records the just-finished level's completion (best
    /// death count, next-level unlock) and loads the next level's scene. Mirrors RestartButton's
    /// ITouchReservedRegion pattern; unlike Restart it has no Editor keyboard shortcut, so it
    /// never double-binds Enter with Restart. PAX-053 (D-072) §2.2: routes through
    /// LevelSceneLoader, the one seam every level/menu scene change goes through. PAX-066 (D-279): a next level that isn't playable yet
    /// opens the unlock panel instead of loading.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class NextLevelButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] Button button;
        [SerializeField] UnlockPanel unlockPanel;

        string currentLevelId;
        string nextLevelId;
        int deaths;
        string nextSceneName;
        IReadOnlyList<string> orderedLevelIds;

        void OnEnable() { if (button != null) button.onClick.AddListener(GoToNextLevel); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(GoToNextLevel); }

        public void Configure(string currentLevelId, int deaths, string nextSceneName, IReadOnlyList<string> orderedLevelIds, string nextLevelId = null)
        {
            this.nextLevelId = nextLevelId;
            this.currentLevelId = currentLevelId;
            this.deaths = deaths;
            this.nextSceneName = nextSceneName;
            this.orderedLevelIds = orderedLevelIds;
        }

        public void GoToNextLevel()
        {
            if (string.IsNullOrEmpty(nextSceneName)) return;
            RecordAndSaveProgress();
            if (unlockPanel != null && !unlockPanel.Allows(nextLevelId)) { unlockPanel.Show(nextLevelId, nextSceneName); return; }
            LevelSceneLoader.Load(nextSceneName);
        }

        // Split out from GoToNextLevel so tests can verify the recording side effect without
        // also triggering an actual scene load.
        public void RecordAndSaveProgress()
        {
            if (orderedLevelIds == null) return;
            LevelProgress progress = LevelProgressStore.Load(orderedLevelIds);
            progress.RecordCompletion(currentLevelId, deaths);
            LevelProgressStore.Save(progress, orderedLevelIds);
        }

        // Same rationale as RestartButton: read (RectTransform)transform directly rather than a
        // cached field, so this doesn't depend on Awake having run while hidden under an
        // initially-inactive panel. Hidden (or otherwise disabled) never reserves screen space.
        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
