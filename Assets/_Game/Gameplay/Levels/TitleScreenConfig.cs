using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-108 (D-157): the title screen's tunables. One asset at AssetPath, made by PARALLAX/Setup/Title Screen.</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Title Screen Config")]
    public sealed class TitleScreenConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/TitleScreenConfig.asset";

        [Header("Intro (seconds from the scene's start)")]
        [SerializeField] float coverFade = 1.4f;
        [SerializeField] float wordmarkStart = 0.9f;
        [SerializeField] float trackingSeconds = 2.2f;
        [Tooltip("The wordmark's letter spacing at the start and at rest, in ems.")]
        [SerializeField] float trackingFrom = 1.1f;
        [SerializeField] float trackingTo = 0.42f;
        [SerializeField] float ruleStart = 2.3f;
        [SerializeField] float taglineStart = 2.7f;
        [SerializeField] float menuStart = 3.3f;
        [SerializeField] float fade = 0.8f;
        [Tooltip("The buttons answer only this long after the menu is fully shown (the tap that skipped can't press one).")]
        [SerializeField] float inputLock = 0.2f;

        [Header("Flair (D-280)")]
        [Tooltip("Each letter starts this long after the one nearer the middle.")]
        [SerializeField] float letterStep = 0.1f;
        [Tooltip("How far a letter rises as it arrives, in canvas units.")]
        [SerializeField] float letterRise = 46f;
        [SerializeField] float menuStep = 0.14f;
        [Tooltip("How far a button rises as it arrives, in canvas units.")]
        [SerializeField] float menuRise = 34f;
        [SerializeField] float sheenStart = 2.5f;
        [SerializeField] float sheenSeconds = 1.5f;
        [SerializeField] float sheenEvery = 9f;
        [Tooltip("The camera pushes in over this long, from pushFrom times its size.")]
        [SerializeField] float pushSeconds = 6f;
        [SerializeField] float pushFrom = 1.07f;
        [Tooltip("Seconds of black fade after PLAY, START GAME or LEVELS, before the scene changes.")]
        [SerializeField] float leaveSeconds = 0.35f;

        [Header("The slipping letter (D-280)")]
        [SerializeField] float slipDelay = 5f;
        [SerializeField] float slipEvery = 13f;
        [SerializeField] float slipSeconds = 1.7f;
        [Tooltip("How far a slipped letter falls, in canvas units, and how far it tilts, in degrees.")]
        [SerializeField] float slipDrop = 36f;
        [SerializeField] float slipTilt = 15f;
        [Tooltip("Which letters slip, in turn (0 is the P).")]
        [SerializeField] int[] slipOrder = { 7, 2, 5, 0, 4 };

        [Header("Camera pan")]
        [Tooltip("Half the camera's horizontal swing, in world units.")]
        [SerializeField] float driftRange = 2.5f;
        [SerializeField] float driftPeriod = 48f;

        [Header("Text")]
        [SerializeField] string tagline = "Every room is lying.";

        [Header("Cat")]
        [SerializeField] float idleFps = 7f;
        [SerializeField] float fidgetFps = 12f;
        [Tooltip("Seconds of plain idle between two look-arounds.")]
        [SerializeField] float fidgetEvery = 6f;

        public TitleIntroTimes Times => new TitleIntroTimes
        {
            CoverFade = coverFade, WordmarkStart = wordmarkStart, TrackingSeconds = trackingSeconds, TrackingFrom = trackingFrom,
            TrackingTo = trackingTo, RuleStart = ruleStart, TaglineStart = taglineStart, MenuStart = menuStart, Fade = fade,
            InputLock = inputLock, LetterStep = letterStep, MenuStep = menuStep, SheenStart = sheenStart, SheenSeconds = sheenSeconds,
            SheenEvery = sheenEvery, SlipDelay = slipDelay, SlipEvery = slipEvery, SlipSeconds = slipSeconds,
        };

        public float LetterRise => letterRise;
        public float MenuRise => menuRise;
        public float PushSeconds => pushSeconds;
        public float PushFrom => pushFrom;
        public float LeaveSeconds => leaveSeconds;
        public float SlipDrop => slipDrop;
        public float SlipTilt => slipTilt;
        public int[] SlipOrder => slipOrder;
        public float DriftRange => driftRange;
        public float DriftPeriod => driftPeriod;
        public string Tagline => tagline;
        public float IdleFps => idleFps;
        public float FidgetFps => fidgetFps;
        public float FidgetEvery => fidgetEvery;
    }
}
