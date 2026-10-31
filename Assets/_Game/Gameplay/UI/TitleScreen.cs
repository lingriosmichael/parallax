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
    /// (D-157 amendment 1). With no progress START GAME is hidden and LEVELS takes its place. D-280: the letters arrive one by one,
    /// light crosses them, the buttons rise in turn, one letter slips now and then, and leaving fades to black first. Every pose is a
    /// function of the clock, so Seek shows any moment.</summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] TitleScreenConfig config;
        [SerializeField] LevelListConfig levelList;
        [SerializeField] Image cover;
        [SerializeField] LetterSpacedText wordmark;
        [Tooltip("The soft darkening behind the wordmark; it fades in with it, up to its own alpha.")]
        [SerializeField] Image shade;
        [SerializeField] RectTransform rule;
        [SerializeField] TitleAtmosphere atmosphere;
        [Tooltip("D-280: the buttons, from the middle out, in the order they rise.")]
        [SerializeField] RectTransform[] menuItems = new RectTransform[0];
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
        Vector2[] menuRest;
        string leavingTo;
        float leaveAt;

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
            menuRest = new Vector2[menuItems.Length];
            for (int i = 0; i < menuItems.Length; i++) if (menuItems[i] != null) menuRest[i] = menuItems[i].anchoredPosition;
            Apply();
        }

        void Update()
        {
            if (config == null) return;
            TitleIntroTimes times = config.Times;
            if (TitleIntroTimeline.IsPlaying(times, elapsed) && PressedThisFrame()) elapsed = TitleIntroTimeline.SkipTo(times);
            else elapsed += Time.unscaledDeltaTime;
            Apply();
            if (leavingTo == null) return;
            float left = config.LeaveSeconds > 0f ? (Time.unscaledTime - leaveAt) / config.LeaveSeconds : 1f;
            if (cover != null)
            {
                cover.enabled = true;
                cover.color = new Color(0f, 0f, 0f, Mathf.Clamp01(left));
            }
            if (left >= 1f) LevelSceneLoader.Load(leavingTo);
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
                wordmark.Spacing = TitleIntroTimeline.Tracking(t, s);
                TitleSlip slip = TitleIntroTimeline.Slip(t, s, config.SlipOrder);
                for (int i = 0; i < wordmark.Count; i++)
                {
                    bool slipping = i == slip.Letter;
                    float lift = -config.LetterRise * TitleIntroTimeline.LetterRise(t, s, i, wordmark.Count) - (slipping ? config.SlipDrop * slip.Drop : 0f);
                    wordmark.SetLetter(i, TitleIntroTimeline.Letter(t, s, i, wordmark.Count), lift, slipping ? config.SlipTilt * slip.Tilt : 0f);
                }
                wordmark.SetSheen(TitleIntroTimeline.Sheen(t, s));
            }
            if (shade != null && shadeAlpha >= 0f)
            {
                Color c = shade.color;
                c.a = shadeAlpha * TitleIntroTimeline.Wordmark(t, s);
                shade.color = c;
            }
            if (rule != null) rule.localScale = new Vector3(TitleIntroTimeline.Rule(t, s), 1f, 1f);
            if (atmosphere != null) atmosphere.Apply(s, TitleIntroTimeline.Wordmark(t, s));
            if (tagline != null)
            {
                Color c = tagline.color;
                c.a = TitleIntroTimeline.Tagline(t, s);
                tagline.color = c;
            }
            if (menu != null)
            {
                menu.alpha = TitleIntroTimeline.Menu(t, s);
                bool live = TitleIntroTimeline.MenuInteractable(t, s) && leavingTo == null;
                menu.interactable = live;
                menu.blocksRaycasts = live;
            }
            for (int i = 0; menuRest != null && i < menuItems.Length; i++)
                if (menuItems[i] != null)
                    menuItems[i].anchoredPosition = menuRest[i] + Vector2.down * (config.MenuRise * (1f - TitleIntroTimeline.MenuItem(t, s, i)));
        }

        // D-280: the fade before a scene change. Outside Play Mode nothing runs Update, so it loads at once.
        void Leave(string scene)
        {
            if (leavingTo != null) return;
            if (!Application.isPlaying || config == null || config.LeaveSeconds <= 0f) { LevelSceneLoader.Load(scene); return; }
            leavingTo = scene;
            leaveAt = Time.unscaledTime;
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
            // D-280: the row closes up: PLAY and LEVELS stay side by side, centred.
            if (levelsButton != null && playButton != null)
            {
                var play = (RectTransform)playButton.transform;
                var levels = (RectTransform)levelsButton.transform;
                float shift = (levels.anchoredPosition.x - ((RectTransform)startButton.transform).anchoredPosition.x) * 0.25f;
                play.anchoredPosition += Vector2.left * shift;
                levels.anchoredPosition += Vector2.left * shift;
            }
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
            if (confirmPanel != null) confirmPanel.SetActive(false);
            Leave(firstScene);
        }

        public void Play()
        {
            if (string.IsNullOrEmpty(playScene))
            {
                Debug.LogError("TitleScreen: no level to play (is LevelListConfig empty?).", this);
                return;
            }
            Leave(playScene);
        }

        public void OpenLevels() => Leave(LevelSceneLoader.LevelSelectSceneName);

        static bool PressedThisFrame()
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        }
    }
}
