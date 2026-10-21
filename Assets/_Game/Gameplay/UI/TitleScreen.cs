using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>PAX-108 (D-157): the Title scene's intro and menu. Plays TitleIntroTimeline on unscaled seconds (presentation
    /// only; nothing here touches a gameplay tick or Time.timeScale); a press anywhere during the intro jumps it to its end.
    /// PLAY loads TitleContinue's level and LEVELS the level list, both through LevelSceneLoader (D-072). Once there is
    /// progress, PLAY reads CONTINUE and START GAME appears: it asks first, then erases the progress and loads the first level
    /// (D-157 amendment 1). With no progress START GAME is hidden and LEVELS takes its place.</summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] TitleScreenConfig config;
        [SerializeField] LevelListConfig levelList;
        [SerializeField] Image cover;
        [SerializeField] LetterSpacedText wordmark;
        [Tooltip("The soft darkening behind the wordmark; it fades in with it, up to its own alpha.")]
        [SerializeField] Image shade;
        [SerializeField] RectTransform rule;
        [SerializeField] Text tagline;
        [SerializeField] CanvasGroup menu;
        [SerializeField] Button playButton;
        [SerializeField] Text playLabel;
        [SerializeField] Button levelsButton;
        [SerializeField] Button startButton;
        [Tooltip("Asks before START GAME erases the progress; hidden until then.")]
        [SerializeField] GameObject confirmPanel;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        float elapsed;
        float shadeAlpha = -1f;
        string playScene;
        string firstScene;
        bool hasProgress;

        public float Elapsed => elapsed;

        void OnEnable()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (levelsButton != null) levelsButton.onClick.AddListener(OpenLevels);
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmStartGame);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelStartGame);
        }

        void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Play);
            if (levelsButton != null) levelsButton.onClick.RemoveListener(OpenLevels);
            if (startButton != null) startButton.onClick.RemoveListener(StartGame);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmStartGame);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelStartGame);
        }

        void Start()
        {
            if (config == null) Debug.LogError("TitleScreen: no TitleScreenConfig assigned; run PARALLAX/Setup/Title Screen.", this);
            if (tagline != null && config != null) tagline.text = config.Tagline;
            if (shade != null && shadeAlpha < 0f) shadeAlpha = shade.color.a;
            if (confirmPanel != null) confirmPanel.SetActive(false);
            ResolvePlay();
            ShowStart();
            Apply();
        }

        void Update()
        {
            if (config == null) return;
            TitleIntroTimes times = config.Times;
            if (TitleIntroTimeline.IsPlaying(times, elapsed) && PressedThisFrame()) elapsed = TitleIntroTimeline.SkipTo(times);
            else elapsed += Time.unscaledDeltaTime;
            Apply();
        }

        /// <summary>Jumps the intro to the given second (tests, and the skip).</summary>
        public void Seek(float seconds)
        {
            elapsed = Mathf.Max(0f, seconds);
            Apply();
        }

        void Apply()
        {
            if (config == null) return;
            TitleIntroTimes t = config.Times;
            float s = elapsed;
            if (cover != null)
            {
                Color c = cover.color;
                c.a = TitleIntroTimeline.Cover(t, s);
                cover.color = c;
                cover.enabled = c.a > 0f;
            }
            if (wordmark != null)
            {
                wordmark.SetAlpha(TitleIntroTimeline.Wordmark(t, s));
                wordmark.Spacing = TitleIntroTimeline.Tracking(t, s);
            }
            if (shade != null && shadeAlpha >= 0f)
            {
                Color c = shade.color;
                c.a = shadeAlpha * TitleIntroTimeline.Wordmark(t, s);
                shade.color = c;
            }
            if (rule != null) rule.localScale = new Vector3(TitleIntroTimeline.Rule(t, s), 1f, 1f);
            if (tagline != null)
            {
                Color c = tagline.color;
                c.a = TitleIntroTimeline.Tagline(t, s);
                tagline.color = c;
            }
            if (menu != null)
            {
                menu.alpha = TitleIntroTimeline.Menu(t, s);
                bool live = TitleIntroTimeline.MenuInteractable(t, s);
                menu.interactable = live;
                menu.blocksRaycasts = live;
            }
        }

        void ResolvePlay()
        {
            if (levelList == null)
            {
                Debug.LogError("TitleScreen: no LevelListConfig assigned; PLAY can't pick a level.", this);
                return;
            }
            IReadOnlyList<string> ids = levelList.OrderedIds();
            LevelProgress progress = LevelProgressStore.Load(ids);
            string id = TitleContinue.Target(ids, progress);
            foreach (LevelEntry entry in levelList.Levels)
                if (entry.Id == id) { playScene = entry.SceneName; break; }
            firstScene = levelList.Levels.Count > 0 ? levelList.Levels[0].SceneName : null;
            hasProgress = TitleContinue.HasProgress(ids, progress);
            if (playLabel != null) playLabel.text = hasProgress ? "CONTINUE" : "PLAY";
        }

        // START GAME only means something once there is progress to start over from; without it LEVELS moves up into its place.
        void ShowStart()
        {
            if (startButton == null || hasProgress) return;
            if (levelsButton != null)
                ((RectTransform)levelsButton.transform).anchoredPosition = ((RectTransform)startButton.transform).anchoredPosition;
            startButton.gameObject.SetActive(false);
        }

        public bool HasProgress => hasProgress;

        /// <summary>START GAME: asks first when there is progress to lose; with none, starts at once.</summary>
        public void StartGame()
        {
            if (hasProgress && confirmPanel != null) { confirmPanel.SetActive(true); return; }
            ConfirmStartGame();
        }

        public void CancelStartGame()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        /// <summary>Erases the saved progress and loads the first level.</summary>
        public void ConfirmStartGame()
        {
            if (levelList == null || string.IsNullOrEmpty(firstScene))
            {
                Debug.LogError("TitleScreen: no first level to start (is LevelListConfig empty?).", this);
                return;
            }
            LevelProgressStore.Clear(levelList.OrderedIds());
            LevelSceneLoader.Load(firstScene);
        }

        public void Play()
        {
            if (string.IsNullOrEmpty(playScene))
            {
                Debug.LogError("TitleScreen: no level to play (is LevelListConfig empty?).", this);
                return;
            }
            LevelSceneLoader.Load(playScene);
        }

        public void OpenLevels() => LevelSceneLoader.Load(LevelSceneLoader.LevelSelectSceneName);

        static bool PressedThisFrame()
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        }
    }
}
