using System;

namespace Parallax.Core
{
    /// <summary>PAX-065 (D-278): what the phone can be asked to feel.</summary>
    public enum HapticCue { TrapFire, Death, LevelComplete }

    /// <summary>PAX-065: the device's vibrator (or, in the Editor, a log line).</summary>
    public interface IHaptics
    {
        void Play(HapticCue cue);
    }

    /// <summary>PAX-065 (D-278): turns a level's events into haptic cues. A trap counts only within TrapWithin units of the cat,
    /// and only MinGap after the last cue, so a hall of gates doesn't buzz continuously. A death or a finished level always plays:
    /// a killing trap's own cue mustn't swallow the death's. Off, it makes no calls at all. Times are seconds on any clock that only
    /// moves forward.</summary>
    public sealed class HapticDispatcher
    {
        readonly IHaptics haptics;
        readonly Func<bool> enabled;
        readonly float minGap, trapWithin;
        float lastAt = float.NegativeInfinity;

        public HapticDispatcher(IHaptics haptics, Func<bool> enabled, float minGap, float trapWithin)
        {
            this.haptics = haptics;
            this.enabled = enabled;
            this.minGap = minGap;
            this.trapWithin = trapWithin;
        }

        public void TrapFired(float distanceToCat, float now)
        {
            if (distanceToCat > trapWithin || now - lastAt < minGap) return;
            Play(HapticCue.TrapFire, now);
        }

        public void Died(float now) => Play(HapticCue.Death, now);

        public void LevelCompleted(float now) => Play(HapticCue.LevelComplete, now);

        void Play(HapticCue cue, float now)
        {
            if (!enabled()) return;
            lastAt = now;
            haptics.Play(cue);
        }
    }
}
