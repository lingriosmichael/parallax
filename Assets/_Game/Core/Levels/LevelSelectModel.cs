using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-053 (D-063) §2.3/§5.5: one entry per level, in list order, for the level
    /// select screen. Pure logic only - unlock/best-deaths rules stay in LevelProgress
    /// (PAX-050); this only shapes that data for display, so it needs no LevelListConfig
    /// reference (Core can't reference Gameplay) - callers pass plain (id, sceneName,
    /// displayName) tuples instead.</summary>
    public readonly struct LevelSelectEntry
    {
        public readonly string Id;
        public readonly string SceneName;
        public readonly string DisplayName;
        public readonly bool IsUnlocked;
        public readonly string BestDeathsText;
        // PAX-066 (D-279): not playable yet: tapping the row opens the unlock panel.
        public readonly bool IsPaywalled;

        public LevelSelectEntry(string id, string sceneName, string displayName, bool isUnlocked, string bestDeathsText, bool isPaywalled = false)
        {
            Id = id;
            SceneName = sceneName;
            DisplayName = displayName;
            IsUnlocked = isUnlocked;
            BestDeathsText = bestDeathsText;
            IsPaywalled = isPaywalled;
        }
    }

    public static class LevelSelectModel
    {
        public static LevelSelectEntry[] Build(
            IReadOnlyList<(string Id, string SceneName, string DisplayName)> levels,
            LevelProgress progress,
            Entitlements entitlements = null,
            long now = 0)
        {
            var entries = new LevelSelectEntry[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                (string id, string sceneName, string displayName) = levels[i];
                int best = progress.BestDeaths(id);
                string bestText = best == LevelProgress.NeverCompleted ? "—" : best.ToString(System.Globalization.CultureInfo.InvariantCulture);
                bool paywalled = entitlements != null && !entitlements.IsPlayable(id, now);
                if (paywalled) bestText = "Locked";
                entries[i] = new LevelSelectEntry(id, sceneName, displayName, progress.IsUnlocked(id), bestText, paywalled);
            }
            return entries;
        }
    }
}
