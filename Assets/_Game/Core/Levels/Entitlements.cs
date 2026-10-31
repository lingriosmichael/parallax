using System;
using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-066 (D-279): who may play what. The first freeLevels are free. Any later level is playable once the full game is
    /// paid for, or once waitSeconds have passed since its wait began (waitStartedAt, unix seconds, negative for not begun). The clock
    /// is the phone's: moving it forward skips a wait, which D-279 accepts, and a clock moved back never makes a wait longer than
    /// waitSeconds. A level is enterable when it's also unlocked by progress. An id that isn't listed is free: nothing here
    /// locks a scene outside the list.</summary>
    public sealed class Entitlements
    {
        readonly Dictionary<string, int> index = new Dictionary<string, int>();
        readonly int freeLevels;
        readonly long waitSeconds;
        readonly Func<string, long> waitStartedAt;

        public bool IsPaidUnlocked { get; }

        public Entitlements(IReadOnlyList<string> orderedIds, int freeLevels, long waitSeconds, bool paidUnlocked, Func<string, long> waitStartedAt)
        {
            for (int i = 0; i < orderedIds.Count; i++) index[orderedIds[i]] = i;
            this.freeLevels = freeLevels;
            this.waitSeconds = waitSeconds;
            this.waitStartedAt = waitStartedAt;
            IsPaidUnlocked = paidUnlocked;
        }

        public bool IsFree(string id) => !index.TryGetValue(id, out int i) || i < freeLevels;

        /// <summary>Seconds until the level is playable; 0 when it is.</summary>
        public long SecondsLeft(string id, long now)
        {
            if (IsFree(id) || IsPaidUnlocked) return 0;
            long started = waitStartedAt(id);
            if (started < 0) return waitSeconds;
            return Math.Max(0, Math.Min(waitSeconds, started + waitSeconds - now));
        }

        public bool IsPlayable(string id, long now) => SecondsLeft(id, now) == 0;

        public bool CanEnter(LevelProgress progress, string id, long now) => progress.IsUnlocked(id) && IsPlayable(id, now);

        /// <summary>A wait as the panel shows it: 11:32:04.</summary>
        public static string Clock(long seconds) =>
            (seconds / 3600).ToString("00") + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
}
