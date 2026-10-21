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
            InputLock = inputLock,
        };

        public float DriftRange => driftRange;
        public float DriftPeriod => driftPeriod;
        public string Tagline => tagline;
        public float IdleFps => idleFps;
        public float FidgetFps => fidgetFps;
        public float FidgetEvery => fidgetEvery;
    }
}
