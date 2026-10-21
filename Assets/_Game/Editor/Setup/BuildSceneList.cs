using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-053 (D-072). The only code allowed to write EditorBuildSettings.scenes.
    /// Compute is a pure function - given the scenes currently in Build Settings, the level
    /// scenes LevelListConfig lists (in order), and the Scenes/Levels folder path - so it can be
    /// unit-tested with fixture data, never the real project settings. Sync() is the thin,
    /// impure wrapper: it validates every scene actually exists on disk (refusing and writing
    /// nothing otherwise), calls Compute, assigns the result, then explicitly persists it to
    /// ProjectSettings/EditorBuildSettings.asset - EditorBuildSettings.scenes = ... only dirties
    /// the in-memory settings; without this call the change can be lost (PAX-051's Level_004
    /// drift), exactly as AssetDatabase.SaveAssets() is Unity's documented "writes all unsaved
    /// asset changes to disk" call.</summary>
    public static class BuildSceneList
    {
        public const string LevelSelectScenePath = "Assets/_Game/Scenes/" + LevelSceneLoader.LevelSelectSceneName + ".unity";
        /// <summary>PAX-108 (D-157): the scene the app starts on, once PARALLAX/Setup/Title Screen has made it.</summary>
        public const string TitleScenePath = "Assets/_Game/Scenes/Title.unity";
        const string LevelListConfigPath = "Assets/_Game/Data/LevelListConfig.asset";
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";

        /// <summary>Produces: Title first when titleFirst (PAX-108: the app starts on it), then LevelSelect (enabled), then
        /// configScenePaths in order
        /// (enabled), then every other scene from currentEntries that isn't under levelsFolder
        /// (kept, with its own enabled state and relative order) - deduplicated against paths
        /// already placed. Any scene under levelsFolder that isn't in configScenePaths (the
        /// template, or an unlisted level scene) is always dropped.</summary>
        public static EditorBuildSettingsScene[] Compute(
            EditorBuildSettingsScene[] currentEntries,
            IReadOnlyList<string> configScenePaths,
            string levelsFolder,
            bool titleFirst)
        {
            var result = new List<EditorBuildSettingsScene>();
            var placed = new HashSet<string>();

            if (titleFirst) AddIfNew(result, placed, TitleScenePath, true);
            AddIfNew(result, placed, LevelSelectScenePath, true);
            foreach (string path in configScenePaths) AddIfNew(result, placed, path, true);

            string folderPrefix = levelsFolder.TrimEnd('/') + "/";
            foreach (EditorBuildSettingsScene scene in currentEntries)
            {
                if (placed.Contains(scene.path)) continue;
                if (scene.path.StartsWith(folderPrefix, StringComparison.Ordinal)) continue;
                AddIfNew(result, placed, scene.path, scene.enabled);
            }

            return result.ToArray();
        }

        static void AddIfNew(List<EditorBuildSettingsScene> result, HashSet<string> placed, string path, bool enabled)
        {
            if (!placed.Add(path)) return;
            result.Add(new EditorBuildSettingsScene(path, enabled));
        }

        [MenuItem("PARALLAX/Setup/Levels/Sync Build Scene List")]
        public static void SyncMenuItem() => Sync();

        /// <summary>Returns true only if it actually wrote Build Settings. Refuses (returning
        /// false, writing nothing) if LevelListConfig, LevelSelect.unity, or any listed level's
        /// scene file doesn't exist yet - callers (LevelSetup.NewLevel) must check this rather
        /// than assume a sync always succeeds.</summary>
        public static bool Sync()
        {
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (config == null) { Debug.LogError("BuildSceneList: LevelListConfig asset is missing."); return false; }

            if (AssetDatabase.LoadAssetAtPath<Object>(LevelSelectScenePath) == null)
            {
                Debug.LogError("BuildSceneList: '" + LevelSelectScenePath + "' does not exist; run PARALLAX/Setup/Levels/Level Select first. Wrote nothing.");
                return false;
            }

            var configScenePaths = new List<string>();
            foreach (LevelEntry entry in config.Levels)
            {
                string path = $"{LevelsFolder}/{entry.SceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                {
                    Debug.LogError("BuildSceneList: " + entry.Id + "'s scene '" + path + "' does not exist. Wrote nothing.");
                    return false;
                }
                configScenePaths.Add(path);
            }

            bool titleExists = AssetDatabase.LoadAssetAtPath<Object>(TitleScenePath) != null;
            if (!titleExists) Debug.LogWarning("BuildSceneList: '" + TitleScenePath + "' does not exist yet; the app starts on LevelSelect until PARALLAX/Setup/Title Screen makes it.");
            EditorBuildSettingsScene[] updated = Compute(EditorBuildSettings.scenes, configScenePaths, LevelsFolder, titleExists);
            EditorBuildSettings.scenes = updated;
            // Explicit persist call (named per pax-ticket review): EditorBuildSettings.scenes'
            // setter only dirties the in-memory list. AssetDatabase.SaveAssets() flushes it to
            // ProjectSettings/EditorBuildSettings.asset immediately, with no File -> Save Project
            // needed - verify this yourself (developer step) by diffing the file before/after.
            AssetDatabase.SaveAssets();

            Debug.Log("BuildSceneList: synced " + updated.Length + " scene(s) to Build Settings: " + string.Join(", ", updated.Select(s => s.path)));
            return true;
        }
    }
}
