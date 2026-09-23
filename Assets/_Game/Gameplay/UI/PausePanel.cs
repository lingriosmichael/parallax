using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-054 (D-073): the full-screen pause panel with Resume, Restart and Levels.
    /// While shown it is one reserved touch region covering the whole screen, so no touch that
    /// begins while paused is ever claimed: its Began is reserved, and after the resume it has no
    /// new Began (TouchStickCatInput only claims a finger on its Began). Restart and Levels abandon the attempt: they load through
    /// LevelSceneLoader and record nothing - Levels deliberately does not reuse LevelsButton,
    /// which records a completion.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PausePanel : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] LevelPause levelPause;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button levelsButton;

        void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (levelsButton != null) levelsButton.onClick.AddListener(GoToLevelSelect);
        }

        void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (levelsButton != null) levelsButton.onClick.RemoveListener(GoToLevelSelect);
        }

        public void Resume()
        {
            if (levelPause == null)
            {
                Debug.LogError($"PausePanel '{name}' has no LevelPause assigned.", this);
                return;
            }
            levelPause.Resume();
        }

        public void Restart() => LevelSceneLoader.Load(SceneManager.GetActiveScene().name);

        public void GoToLevelSelect() => LevelSceneLoader.Load(LevelSceneLoader.LevelSelectSceneName);

        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
