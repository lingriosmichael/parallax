using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-053 (D-072) §2.4: on click, records the just-finished level's completion
    /// exactly like NextLevelButton does, then loads LevelSelect through LevelSceneLoader (§2.2).
    /// Shown on every level-complete screen, including the last listed level (which has no Next
    /// level button).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class LevelsButton : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] Button button;

        string currentLevelId;
        int deaths;
        IReadOnlyList<string> orderedLevelIds;

        void OnEnable() { if (button != null) button.onClick.AddListener(GoToLevelSelect); }
        void OnDisable() { if (button != null) button.onClick.RemoveListener(GoToLevelSelect); }

        public void Configure(string currentLevelId, int deaths, IReadOnlyList<string> orderedLevelIds)
        {
            this.currentLevelId = currentLevelId;
            this.deaths = deaths;
            this.orderedLevelIds = orderedLevelIds;
        }

        public void GoToLevelSelect()
        {
            RecordAndSaveProgress();
            LevelSceneLoader.Load(LevelSceneLoader.LevelSelectSceneName);
        }

        // Split out so tests can verify the recording side effect without also triggering a
        // scene load, matching NextLevelButton's own split.
        public void RecordAndSaveProgress()
        {
            if (orderedLevelIds == null) return;
            LevelProgress progress = LevelProgressStore.Load(orderedLevelIds);
            progress.RecordCompletion(currentLevelId, deaths);
            LevelProgressStore.Save(progress, orderedLevelIds);
        }

        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
