using System;
using System.Collections.Generic;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-050 (D-063): the only place that touches PlayerPrefs (first use of device
    /// persistence in the project). Key format, per level id: "PARALLAX_LEVEL_UNLOCKED_{id}"
    /// (int, 1/0) and "PARALLAX_LEVEL_BEST_{id}" (int deaths; key absent = never completed).
    /// Load never throws: any read failure logs a warning and falls back to an empty,
    /// first-level-unlocked LevelProgress.</summary>
    public static class LevelProgressStore
    {
        const string UnlockedPrefix = "PARALLAX_LEVEL_UNLOCKED_";
        const string BestPrefix = "PARALLAX_LEVEL_BEST_";

        public static LevelProgress Load(IReadOnlyList<string> orderedLevelIds)
        {
            try
            {
                var unlocked = new List<string>();
                var best = new List<KeyValuePair<string, int>>();
                for (int i = 0; i < orderedLevelIds.Count; i++)
                {
                    string id = orderedLevelIds[i];
                    if (PlayerPrefs.GetInt(UnlockedPrefix + id, 0) == 1) unlocked.Add(id);
                    if (PlayerPrefs.HasKey(BestPrefix + id))
                    {
                        int deaths = PlayerPrefs.GetInt(BestPrefix + id, LevelProgress.NeverCompleted);
                        if (deaths >= 0) best.Add(new KeyValuePair<string, int>(id, deaths));
                    }
                }
                return new LevelProgress(orderedLevelIds, unlocked, best);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LevelProgressStore: failed to load saved progress ({e.Message}); starting from an empty, first-level-unlocked state.");
                return new LevelProgress(orderedLevelIds);
            }
        }

        public static void Save(LevelProgress progress, IReadOnlyList<string> orderedLevelIds)
        {
            for (int i = 0; i < orderedLevelIds.Count; i++)
            {
                string id = orderedLevelIds[i];
                PlayerPrefs.SetInt(UnlockedPrefix + id, progress.IsUnlocked(id) ? 1 : 0);
                int best = progress.BestDeaths(id);
                if (best != LevelProgress.NeverCompleted) PlayerPrefs.SetInt(BestPrefix + id, best);
            }
            PlayerPrefs.Save();
        }
    }
}
