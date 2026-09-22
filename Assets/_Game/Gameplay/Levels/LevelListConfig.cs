using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Gameplay.Levels
{
    [Serializable]
    public struct LevelEntry
    {
        public string Id;
        public string SceneName;
        public string DisplayName;
    }

    /// <summary>PAX-050 (D-063): the single source of truth for level order and identity. One
    /// asset under Assets/_Game/Data (PARALLAX/Setup/Level List (PAX-050) creates it); no other
    /// code hardcodes level order.</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Level List")]
    public sealed class LevelListConfig : ScriptableObject
    {
        [SerializeField] LevelEntry[] levels = Array.Empty<LevelEntry>();

        public IReadOnlyList<LevelEntry> Levels => levels;

        public IReadOnlyList<string> OrderedIds()
        {
            var ids = new string[levels.Length];
            for (int i = 0; i < levels.Length; i++) ids[i] = levels[i].Id;
            return ids;
        }

        public bool TryGetBySceneName(string sceneName, out LevelEntry entry)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].SceneName == sceneName) { entry = levels[i]; return true; }
            }
            entry = default;
            return false;
        }

        public bool TryGetNext(string currentId, out LevelEntry next)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].Id != currentId) continue;
                if (i + 1 < levels.Length) { next = levels[i + 1]; return true; }
                break;
            }
            next = default;
            return false;
        }
    }
}
