using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>Premium plan Phase 6 (D-314, the roadmap's Decide 2): the game's small settings screen, opened from the title. Music and
    /// Sound step through 100, 75, 50, 25 % and off on each tap; Vibration and Motion (Reduce motion, PAX-V06) flip. All four are saved
    /// at once (LevelProgressStore) and read live by the game, so nothing needs applying. It sits on the always-active canvas and shows
    /// its panel; the pause panel keeps its own Vibration and Motion buttons. Written by PARALLAX/Setup/Title Screen.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        static readonly float[] Steps = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        [SerializeField] GameObject panel;
        [SerializeField] Button opener;
        [SerializeField] Button closeButton;
        [SerializeField] Button musicButton;
        [SerializeField] Text musicLabel;
        [SerializeField] Button soundButton;
        [SerializeField] Text soundLabel;
        [SerializeField] Button vibrationButton;
        [SerializeField] Text vibrationLabel;
        [SerializeField] Button motionButton;
        [SerializeField] Text motionLabel;

        public bool IsShown => panel != null && panel.activeSelf;

        void OnEnable()
        {
            if (opener != null) opener.onClick.AddListener(Show);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (musicButton != null) musicButton.onClick.AddListener(StepMusic);
            if (soundButton != null) soundButton.onClick.AddListener(StepSound);
            if (vibrationButton != null) vibrationButton.onClick.AddListener(ToggleVibration);
            if (motionButton != null) motionButton.onClick.AddListener(ToggleMotion);
            Refresh();
        }

        void OnDisable()
        {
            if (opener != null) opener.onClick.RemoveListener(Show);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (musicButton != null) musicButton.onClick.RemoveListener(StepMusic);
            if (soundButton != null) soundButton.onClick.RemoveListener(StepSound);
            if (vibrationButton != null) vibrationButton.onClick.RemoveListener(ToggleVibration);
            if (motionButton != null) motionButton.onClick.RemoveListener(ToggleMotion);
        }

        public void Show()
        {
            if (panel == null) { Debug.LogError($"SettingsPanel '{name}' has no panel assigned; run PARALLAX/Setup/Title Screen.", this); return; }
            Refresh();
            panel.SetActive(true);
        }

        public void Hide() { if (panel != null) panel.SetActive(false); }

        public void StepMusic() { LevelProgressStore.MusicVolume = Next(LevelProgressStore.MusicVolume); Refresh(); }
        public void StepSound() { LevelProgressStore.SoundVolume = Next(LevelProgressStore.SoundVolume); Refresh(); }
        public void ToggleVibration() { LevelProgressStore.HapticsEnabled = !LevelProgressStore.HapticsEnabled; Refresh(); }
        public void ToggleMotion() { LevelProgressStore.ReduceMotion = !LevelProgressStore.ReduceMotion; Refresh(); }

        /// <summary>The step after `volume`'s (the nearest step's next; off wraps to full).</summary>
        public static float Next(float volume)
        {
            int nearest = 0;
            for (int i = 1; i < Steps.Length; i++) if (Mathf.Abs(Steps[i] - volume) < Mathf.Abs(Steps[nearest] - volume)) nearest = i;
            return Steps[(nearest + 1) % Steps.Length];
        }

        static string Percent(float volume) => volume <= 0f ? "Off" : Mathf.RoundToInt(volume * 100f) + "%";

        void Refresh()
        {
            if (musicLabel != null) musicLabel.text = "Music: " + Percent(LevelProgressStore.MusicVolume);
            if (soundLabel != null) soundLabel.text = "Sound: " + Percent(LevelProgressStore.SoundVolume);
            if (vibrationLabel != null) vibrationLabel.text = LevelProgressStore.HapticsEnabled ? "Vibration: On" : "Vibration: Off";
            if (motionLabel != null) motionLabel.text = LevelProgressStore.ReduceMotion ? "Motion: Reduced" : "Motion: Full";
        }
    }
}
