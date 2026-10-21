using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-108 (D-157): the level the title's PLAY button loads. The first unlocked level not yet completed, in list
    /// order; once every unlocked level is completed, the last unlocked one. HasProgress (any level completed) turns the
    /// button's label from PLAY to CONTINUE. Defines no unlock rule of its own (LevelProgress does).</summary>
    public static class TitleContinue
    {
        public static string Target(IReadOnlyList<string> orderedIds, LevelProgress progress)
        {
            if (orderedIds == null || orderedIds.Count == 0) return null;
            string lastUnlocked = orderedIds[0];
            for (int i = 0; i < orderedIds.Count; i++)
            {
                string id = orderedIds[i];
                if (!progress.IsUnlocked(id)) continue;
                if (progress.BestDeaths(id) == LevelProgress.NeverCompleted) return id;
                lastUnlocked = id;
            }
            return lastUnlocked;
        }

        public static bool HasProgress(IReadOnlyList<string> orderedIds, LevelProgress progress)
        {
            if (orderedIds == null) return false;
            for (int i = 0; i < orderedIds.Count; i++)
                if (progress.BestDeaths(orderedIds[i]) != LevelProgress.NeverCompleted) return true;
            return false;
        }
    }
}
