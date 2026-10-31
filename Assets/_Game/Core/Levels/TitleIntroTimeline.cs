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
        public float LetterStep;       // D-280: each letter starts this long after the one nearer the middle
        public float MenuStep;         // D-280: each button follows the last by this long
        public float SheenStart;       // D-280: the light first crosses the wordmark here
        public float SheenSeconds;     // ...taking this long
        public float SheenEvery;       // ...and again this often
        public float SlipDelay;        // D-280: the first letter slips this long after the intro ends
        public float SlipEvery;        // ...and another this often
        public float SlipSeconds;      // ...each slip lasting this long
    }

    /// <summary>D-280: one letter's moment of betrayal. Letter is -1 when none is slipping. Drop runs 0 to 1 (and past 1 as it snaps back
    /// in, a small overshoot), Tilt -1 to 1.</summary>
    public readonly struct TitleSlip
    {
        public readonly int Letter;
        public readonly float Drop, Tilt;

        public TitleSlip(int letter, float drop, float tilt) { Letter = letter; Drop = drop; Tilt = tilt; }
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
            Mathf.Max(t.CoverFade, t.WordmarkStart + Mathf.Max(t.Fade, t.TrackingSeconds), t.RuleStart + t.Fade, t.TaglineStart + t.Fade,
                t.MenuStart + 2f * t.MenuStep + t.Fade);

        public static bool IsPlaying(in TitleIntroTimes t, float s) => s < SkipTo(t);

        /// <summary>The buttons answer only InputLock after the menu is fully shown, so the tap that skipped the intro can't
        /// also press one.</summary>
        public static bool MenuInteractable(in TitleIntroTimes t, float s) => s >= SkipTo(t) + t.InputLock;

        /// <summary>D-280: a letter's own reveal, 0 to 1: the middle letters first, the ends last.</summary>
        public static float Letter(in TitleIntroTimes t, float s, int index, int count) =>
            Ramp(s, t.WordmarkStart + Mathf.Abs(index - (count - 1) * 0.5f) * t.LetterStep, t.Fade);

        /// <summary>D-280: how far a letter still has to rise, 1 to 0.</summary>
        public static float LetterRise(in TitleIntroTimes t, float s, int index, int count)
        {
            float x = 1f - EaseOut(Letter(t, s, index, count));
            return x * x;
        }

        /// <summary>D-280: a button's entrance, 0 to 1, in turn from the middle.</summary>
        public static float MenuItem(in TitleIntroTimes t, float s, int index) => EaseOut(Ramp(s, t.MenuStart + index * t.MenuStep, t.Fade));

        /// <summary>D-280: where the light is across the wordmark, 0 to 1, or -1 when it isn't crossing.</summary>
        public static float Sheen(in TitleIntroTimes t, float s)
        {
            if (s < t.SheenStart || t.SheenSeconds <= 0f) return -1f;
            float into = t.SheenEvery > 0f ? (s - t.SheenStart) % t.SheenEvery : s - t.SheenStart;
            return into < t.SheenSeconds ? into / t.SheenSeconds : -1f;
        }

        /// <summary>D-280: the title lies too. Every SlipEvery a letter, in the fixed order given, shudders, drops and hangs, then snaps
        /// back. Never during the intro.</summary>
        public static TitleSlip Slip(in TitleIntroTimes t, float s, int[] order)
        {
            float start = SkipTo(t) + t.SlipDelay;
            if (order == null || order.Length == 0 || t.SlipEvery <= 0f || t.SlipSeconds <= 0f || s < start) return new TitleSlip(-1, 0f, 0f);
            int turn = (int)((s - start) / t.SlipEvery);
            float into = (s - start - turn * t.SlipEvery) / t.SlipSeconds;
            if (into >= 1f) return new TitleSlip(-1, 0f, 0f);
            int letter = order[turn % order.Length];
            float side = turn % 2 == 0 ? 1f : -1f;
            if (into < 0.14f) return new TitleSlip(letter, 0f, side * 0.08f * Mathf.Sin(into * 200f));   // the shudder
            if (into < 0.3f) { float f = (into - 0.14f) / 0.16f; return new TitleSlip(letter, f * f, side * f); }   // the fall
            if (into < 0.76f) return new TitleSlip(letter, 1f, side * (1f + 0.05f * Mathf.Sin((into - 0.3f) * 30f)));   // it hangs
            float back = (into - 0.76f) / 0.24f;
            float overshoot = 1f - EaseOut(back);
            return new TitleSlip(letter, overshoot + 0.14f * Mathf.Sin(back * Mathf.PI) * -1f, side * overshoot);
        }

        /// <summary>D-280: the camera's push in, 0 at the start to 1, easing out.</summary>
        public static float Push(float s, float seconds) => EaseOut(Ramp(s, 0f, seconds));

        /// <summary>The camera's slow pan: a sine of the given range (half the swing) and period, starting at the centre.</summary>
        public static float Drift(float s, float range, float period) =>
            period <= 0f ? 0f : range * Mathf.Sin(2f * Mathf.PI * s / period);

        // Exactly 1 from the end of the ramp on (the division alone can land a hair under it).
        static float Ramp(float s, float start, float length) =>
            s >= start + length ? 1f : length <= 0f || s <= start ? 0f : Mathf.Clamp01((s - start) / length);

        static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);
    }
}
