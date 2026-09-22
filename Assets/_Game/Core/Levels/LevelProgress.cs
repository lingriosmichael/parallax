using System.Collections.Generic;

namespace Parallax.Core
{
    /// <summary>PAX-050 (D-063): pure unlock/best-deaths logic, no UnityEngine or persistence
    /// dependency. The first id in the given order is always unlocked (whether or not it was in
    /// saved data); RecordCompletion keeps the minimum death count seen for that id and unlocks
    /// the next id in order.</summary>
    public sealed class LevelProgress
    {
        public const int NeverCompleted = -1;

        readonly IReadOnlyList<string> orderedIds;
        readonly HashSet<string> unlocked = new HashSet<string>();
        readonly Dictionary<string, int> bestDeaths = new Dictionary<string, int>();

        public LevelProgress(
            IReadOnlyList<string> orderedLevelIds,
            IEnumerable<string> savedUnlocked = null,
            IEnumerable<KeyValuePair<string, int>> savedBestDeaths = null)
        {
            orderedIds = orderedLevelIds;
            if (savedUnlocked != null) foreach (string id in savedUnlocked) unlocked.Add(id);
            if (savedBestDeaths != null) foreach (KeyValuePair<string, int> kv in savedBestDeaths) bestDeaths[kv.Key] = kv.Value;
            if (orderedIds.Count > 0) unlocked.Add(orderedIds[0]);
        }

        public bool IsUnlocked(string id) => unlocked.Contains(id);

        public int BestDeaths(string id) => bestDeaths.TryGetValue(id, out int deaths) ? deaths : NeverCompleted;

        public void RecordCompletion(string id, int deaths)
        {
            int existing = BestDeaths(id);
            if (existing == NeverCompleted || deaths < existing) bestDeaths[id] = deaths;

            int index = IndexOf(id);
            if (index >= 0 && index + 1 < orderedIds.Count) unlocked.Add(orderedIds[index + 1]);
        }

        public IEnumerable<string> UnlockedIds() => unlocked;

        public IEnumerable<KeyValuePair<string, int>> BestDeathsEntries() => bestDeaths;

        int IndexOf(string id)
        {
            for (int i = 0; i < orderedIds.Count; i++) if (orderedIds[i] == id) return i;
            return -1;
        }
    }
}
