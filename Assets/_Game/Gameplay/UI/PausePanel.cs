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
    /// which records a completion. PAX-065 (D-278): Vibration flips the saved haptics setting; the game has no other settings screen.
    /// PAX-066 (D-279): Restore purchase asks the scene's store, and its label says how that went.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PausePanel : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] LevelPause levelPause;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button levelsButton;
        [SerializeField] Button vibrationButton;
        [SerializeField] Text vibrationLabel;
        [SerializeField] StoreHost store;
        [SerializeField] Button restorePurchaseButton;
        [SerializeField] Text restorePurchaseLabel;

        void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (levelsButton != null) levelsButton.onClick.AddListener(GoToLevelSelect);
            if (vibrationButton != null) vibrationButton.onClick.AddListener(ToggleVibration);
            ShowVibration();
            if (restorePurchaseButton != null) restorePurchaseButton.onClick.AddListener(RestorePurchase);
            if (store != null && store.Service != null) store.Service.Changed += ShowRestore;
            ShowRestore();
        }

        void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (levelsButton != null) levelsButton.onClick.RemoveListener(GoToLevelSelect);
            if (vibrationButton != null) vibrationButton.onClick.RemoveListener(ToggleVibration);
            if (restorePurchaseButton != null) restorePurchaseButton.onClick.RemoveListener(RestorePurchase);
            if (store != null && store.Service != null) store.Service.Changed -= ShowRestore;
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

        public void RestorePurchase() { if (store != null) store.Service?.Restore(); }

        void ShowRestore()
        {
            if (restorePurchaseLabel == null) return;
            IapStatus status = store != null && store.Service != null ? store.Service.Status : IapStatus.Idle;
            restorePurchaseLabel.text = status switch
            {
                IapStatus.Working => "Restoring...",
                IapStatus.Unlocked => "Purchase restored",
                IapStatus.NothingToRestore => "Nothing to restore",
                IapStatus.Unavailable => "Store unavailable",
                _ => "Restore purchase",
            };
        }

        public void ToggleVibration()
        {
            LevelProgressStore.HapticsEnabled = !LevelProgressStore.HapticsEnabled;
            ShowVibration();
        }

        void ShowVibration()
        {
            if (vibrationLabel != null) vibrationLabel.text = LevelProgressStore.HapticsEnabled ? "Vibration: On" : "Vibration: Off";
        }

        public bool ContainsScreenPoint(Vector2 screenPos) =>
            isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screenPos, null);
    }
}
