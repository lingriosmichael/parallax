using UnityEngine;

namespace Parallax.Core
{
    /// <summary>PAX-108 (D-157): the intro's beats, in seconds from the title scene's start. Set from TitleScreenConfig.</summary>
    public struct TitleIntroTimes
    {
        public float CoverFade;        // the black cover lifts over [0, CoverFade]
        public float WordmarkStart;    // the wordmark fades in from here, over Fade
        public float TrackingSeconds;  // its letter spacing closes from WordmarkStart over this long
        public float TrackingFrom;     // letter spacing at the start, in ems
        public float TrackingTo;       // the key-art spacing it settles on, in ems
        public float RuleStart;        // the rule under the wordmark draws from here, over Fade
        public float TaglineStart;     // the tagline fades in from here, over Fade
        public float MenuStart;        // the buttons fade in from here, over Fade
        public float Fade;             // the length of each fade
        public float InputLock;        // the menu takes input this long after it is fully shown
    }

    /// <summary>PAX-108 (D-157): pure timing for the title intro. Each value is a function of the elapsed seconds, so a skip
    /// is just a jump of the clock to SkipTo, and every frame of the intro is reproducible in a test.</summary>
    public static class TitleIntroTimeline
    {
        /// <summary>The black cover's alpha: 1 at the start, 0 once the world is revealed.</summary>
        public static float Cover(in TitleIntroTimes t, float s) => 1f - Ramp(s, 0f, t.CoverFade);

        public static float Wordmark(in TitleIntroTimes t, float s) => Ramp(s, t.WordmarkStart, t.Fade);

        /// <summary>The wordmark's letter spacing in ems, easing out from TrackingFrom to TrackingTo.</summary>
        public static float Tracking(in TitleIntroTimes t, float s) =>
            Mathf.Lerp(t.TrackingFrom, t.TrackingTo, EaseOut(Ramp(s, t.WordmarkStart, t.TrackingSeconds)));

        /// <summary>How much of the rule under the wordmark is drawn, 0 to 1 from its centre out.</summary>
        public static float Rule(in TitleIntroTimes t, float s) => EaseOut(Ramp(s, t.RuleStart, t.Fade));

        public static float Tagline(in TitleIntroTimes t, float s) => Ramp(s, t.TaglineStart, t.Fade);

        public static float Menu(in TitleIntroTimes t, float s) => Ramp(s, t.MenuStart, t.Fade);

        /// <summary>The moment the menu is fully shown; a skip jumps the clock here, so everything is on screen at once.</summary>
        public static float SkipTo(in TitleIntroTimes t) =>
            Mathf.Max(t.CoverFade, t.WordmarkStart + Mathf.Max(t.Fade, t.TrackingSeconds), t.RuleStart + t.Fade, t.TaglineStart + t.Fade, t.MenuStart + t.Fade);

        public static bool IsPlaying(in TitleIntroTimes t, float s) => s < SkipTo(t);

        /// <summary>The buttons answer only InputLock after the menu is fully shown, so the tap that skipped the intro can't
        /// also press one.</summary>
        public static bool MenuInteractable(in TitleIntroTimes t, float s) => s >= SkipTo(t) + t.InputLock;

        /// <summary>The camera's slow pan: a sine of the given range (half the swing) and period, starting at the centre.</summary>
        public static float Drift(float s, float range, float period) =>
            period <= 0f ? 0f : range * Mathf.Sin(2f * Mathf.PI * s / period);

        // Exactly 1 from the end of the ramp on (the division alone can land a hair under it).
        static float Ramp(float s, float start, float length) =>
            s >= start + length ? 1f : length <= 0f || s <= start ? 0f : Mathf.Clamp01((s - start) / length);

        static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);
    }
}
