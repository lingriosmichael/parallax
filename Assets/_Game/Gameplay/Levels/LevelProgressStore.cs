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
        const string HapticsKey = "PARALLAX_HAPTICS_ENABLED";
        const string WaitPrefix = "PARALLAX_LEVEL_WAIT_";
        const string PaidKey = "PARALLAX_FULL_UNLOCK";
        const string PaidValue = "granted";

        /// <summary>PAX-066 (D-279): the store's answer, cached so play works offline. Only IapService writes it. Anything but the exact
        /// value reads as not paid (a corrupt cache), and the store is asked again on the next launch. Start Game's Clear keeps it.</summary>
        public static bool PaidUnlocked
        {
            get => PlayerPrefs.GetString(PaidKey, "") == PaidValue;
            set
            {
                if (value) PlayerPrefs.SetString(PaidKey, PaidValue);
                else PlayerPrefs.DeleteKey(PaidKey);
                PlayerPrefs.Save();
            }
        }

        /// <summary>PAX-066 (D-279): when the level's free wait began (unix seconds), beginning it now if it hadn't, or if what's saved
        /// can't be read.</summary>
        public static long WaitStartedAt(string id, long now)
        {
            if (long.TryParse(PlayerPrefs.GetString(WaitPrefix + id, ""), out long started) && started >= 0) return started;
            PlayerPrefs.SetString(WaitPrefix + id, now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            return now;
        }

        /// <summary>PAX-065 (D-278): the pause menu's Vibration setting, on until the player turns it off.</summary>
        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set { PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

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

        /// <summary>PAX-108 (D-157 amendment 1): the title's START GAME. Erases every listed level's unlock and best death
        /// count, so the next Load is a fresh start (only the first level unlocked).</summary>
        public static void Clear(IReadOnlyList<string> orderedLevelIds)
        {
            for (int i = 0; i < orderedLevelIds.Count; i++)
            {
                PlayerPrefs.DeleteKey(UnlockedPrefix + orderedLevelIds[i]);
                PlayerPrefs.DeleteKey(BestPrefix + orderedLevelIds[i]);
                PlayerPrefs.DeleteKey(WaitPrefix + orderedLevelIds[i]);
            }
            PlayerPrefs.Save();
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
