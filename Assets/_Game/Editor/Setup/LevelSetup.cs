using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // PAX-051 (D-066): the one-room level authoring pipeline. Build Level Template makes
    // _LevelTemplate.unity by copying and stripping Level_Solo01 (never opening the original for
    // writing). New Level / Rebuild Current Level / Rebuild All Levels build a level's single
    // room from its LevelLayouts entry via SoloRoomBuilder, the same builder SoloRoomsSetup uses.
    public static class LevelSetup
    {
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string SourceScenePath = "Assets/_Game/Scenes/Level_Solo01.unity";
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        const string LevelListConfigPath = "Assets/_Game/Data/LevelListConfig.asset";
        const string RoomSafetyConfigPath = "Assets/_Game/Data/RoomSafetyConfig.asset";
        static readonly string[] GuardedScenes = { "Sandbox_Realities", "Level_Solo01" };

        [MenuItem("PARALLAX/Setup/Levels/Build Level Template")]
        public static void BuildTemplate()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TemplatePath) != null)
            {
                Debug.LogError("LevelSetup: " + TemplatePath + " already exists; delete it manually first to rebuild it.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(LevelsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes")) { Debug.LogError("LevelSetup: Assets/_Game/Scenes is missing."); return; }
                AssetDatabase.CreateFolder("Assets/_Game/Scenes", "Levels");
            }
            if (!AssetDatabase.CopyAsset(SourceScenePath, TemplatePath))
            {
                Debug.LogError("LevelSetup: failed to copy " + SourceScenePath + " to " + TemplatePath + ".");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Additive);
            var rootNames = new List<string>();
            RoomManager rooms = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                rootNames.Add(root.name);
                RoomManager candidate = root.GetComponentInChildren<RoomManager>(true);
                if (candidate != null) rooms = candidate;
                if (root.name == "RealityRoot_A")
                {
                    Transform roomsParent = root.transform.Find("Rooms_PAX043");
                    if (roomsParent != null) Object.DestroyImmediate(roomsParent.gameObject);
                }
            }
            var changes = new List<string>();
            if (rooms != null) SoloRoomBuilder.ClearBounds(rooms, changes);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("LevelSetup: built " + TemplatePath + ". Root objects kept: " + string.Join(", ", rootNames));
        }

        // PAX-051: execute_menu_item takes no arguments, so this non-interactive entry point
        // builds the first registered level (LevelLayouts, ordered by id) that has no scene yet.
        // Run it once per level to build; a run with nothing left to build is a no-op, keeping
        // the menu idempotent as the ticket requires.
        [MenuItem("PARALLAX/Setup/Levels/New Level...")]
        public static void NewLevelMenu()
        {
            string nextId = LevelLayouts.ById.Keys.OrderBy(id => id)
                .FirstOrDefault(id => !AssetDatabase.LoadAssetAtPath<Object>(ScenePathFor(id)) && !SceneFileExists(id));
            if (nextId == null) { Debug.Log("LevelSetup: no registered level is missing a scene."); return; }
            NewLevel(nextId);
        }

        public static void NewLevel(string levelId)
        {
            if (!LevelLayouts.TryGet(levelId, out SoloRoomDefinition layout)) { Debug.LogError("LevelSetup: " + levelId + " is not registered in LevelLayouts."); return; }
            string activeName = EditorSceneManager.GetActiveScene().name;
            if (GuardedScenes.Contains(activeName)) { Debug.LogError("LevelSetup: refuses to run with '" + activeName + "' active."); return; }

            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (config == null) { Debug.LogError("LevelSetup: LevelListConfig asset is missing; run PARALLAX/Setup/Level List (PAX-050) first."); return; }
            if (AssetDatabase.LoadAssetAtPath<Object>(TemplatePath) == null) { Debug.LogError("LevelSetup: " + TemplatePath + " is missing; run Build Level Template first."); return; }

            int existingIndex = IndexOf(config, levelId);
            string sceneName = existingIndex >= 0 ? config.Levels[existingIndex].SceneName : DefaultSceneName(levelId);
            string scenePath = $"{LevelsFolder}/{sceneName}.unity";
            if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) != null) { Debug.LogError("LevelSetup: " + scenePath + " already exists; use Rebuild Current Level instead."); return; }

            if (!AssetDatabase.CopyAsset(TemplatePath, scenePath)) { Debug.LogError("LevelSetup: failed to copy template to " + scenePath + "."); return; }

            var changes = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BuildRoomInScene(scene, layout, changes);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (existingIndex < 0) AppendToLevelList(config, levelId, sceneName, DefaultDisplayName(levelId), changes);
            AddToBuildSettings(scenePath, changes);

            Debug.Log("LevelSetup: built " + levelId + " (" + scenePath + "). " + string.Join("; ", changes));
        }

        [MenuItem("PARALLAX/Setup/Levels/Rebuild Current Level")]
        public static void RebuildCurrent()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (GuardedScenes.Contains(scene.name)) { Debug.LogError("LevelSetup: refuses to run with '" + scene.name + "' active."); return; }
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (config == null || !config.TryGetBySceneName(scene.name, out LevelEntry entry)) { Debug.LogError("LevelSetup: '" + scene.name + "' is not a listed level scene."); return; }
            if (!LevelLayouts.TryGet(entry.Id, out SoloRoomDefinition layout)) { Debug.LogError("LevelSetup: " + entry.Id + " is not registered in LevelLayouts."); return; }

            var changes = new List<string>();
            RebuildRoomInScene(scene, layout, changes);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("LevelSetup: rebuilt " + entry.Id + " (" + scene.name + "). " + string.Join("; ", changes));
        }

        [MenuItem("PARALLAX/Setup/Levels/Rebuild All Levels")]
        public static void RebuildAll()
        {
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (config == null) { Debug.LogError("LevelSetup: LevelListConfig asset is missing."); return; }
            string originalPath = EditorSceneManager.GetActiveScene().path;

            var report = new List<string>();
            foreach (LevelEntry entry in config.Levels)
            {
                string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) { report.Add(entry.Id + ": no scene, skipped"); continue; }
                if (!LevelLayouts.TryGet(entry.Id, out SoloRoomDefinition layout)) { report.Add(entry.Id + ": not registered in LevelLayouts, skipped"); continue; }

                var changes = new List<string>();
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                RebuildRoomInScene(scene, layout, changes);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.Add(entry.Id + ": " + (changes.Count == 0 ? "no changes" : string.Join("; ", changes)));
            }

            if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("LevelSetup: Rebuild All Levels\n" + string.Join("\n", report));
        }

        static void RebuildRoomInScene(Scene scene, SoloRoomDefinition layout, List<string> changes)
        {
            RealityRoot root = FindComponent<RealityRoot>(scene, "RealityRoot_A");
            if (root == null) { Debug.LogError("LevelSetup: RealityRoot_A missing from " + scene.name + "."); return; }
            Transform roomsParent = root.transform.Find("Rooms_PAX043");
            if (roomsParent != null) Object.DestroyImmediate(roomsParent.gameObject);
            RoomManager rooms = FindComponentAnywhere<RoomManager>(scene);
            if (rooms != null) SoloRoomBuilder.ClearBounds(rooms, changes);
            BuildRoomInScene(scene, layout, changes);
        }

        static void BuildRoomInScene(Scene scene, SoloRoomDefinition layout, List<string> changes)
        {
            RealityRoot root = FindComponent<RealityRoot>(scene, "RealityRoot_A");
            CheckpointManager checkpoints = FindComponentAnywhere<CheckpointManager>(scene);
            RoomManager rooms = FindComponentAnywhere<RoomManager>(scene);
            RoomDeath death = FindComponentAnywhere<RoomDeath>(scene);
            ObserverSet observers = FindComponentAnywhere<ObserverSet>(scene);
            CatMotorConfig config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            if (root == null || checkpoints == null || rooms == null || death == null || observers == null || config == null)
            {
                Debug.LogError("LevelSetup: " + scene.name + " is missing RealityRoot_A, CheckpointManager, RoomManager, RoomDeath, ObserverSet or CatMotorConfig_Default.");
                return;
            }

            RoomSafetyConfig safety = SoloRoomBuilder.EnsureRoomSafetyConfig(RoomSafetyConfigPath, changes);
            SoloRoomBuilder.Wire(death, "config", safety, changes);
            Transform parent = SetupUtility.EnsureChild(root.transform, "Rooms_PAX043", root.gameObject.layer, changes);
            var layoutRooms = new[] { layout };
            SoloRoomBuilder.BuildRooms(parent, root, layoutRooms, checkpoints, rooms, death, observers, config, changes);
            SoloRoomBuilder.AssignBounds(rooms, layoutRooms, root, safety, changes);

            ObserverContext observer = observers.Get(Parallax.Core.ObserverId.A);
            if (observer?.Cat != null)
            {
                Vector2 start = root.ToWorld(new Vector2(2f, -config.ColliderBottom));
                if ((Vector2)observer.Cat.transform.position != start) { observer.Cat.transform.position = start; changes.Add("set Cat A root position"); }
            }
        }

        static int IndexOf(LevelListConfig config, string id)
        {
            for (int i = 0; i < config.Levels.Count; i++) if (config.Levels[i].Id == id) return i;
            return -1;
        }

        static void AppendToLevelList(LevelListConfig config, string id, string sceneName, string displayName, List<string> changes)
        {
            var so = new SerializedObject(config);
            SerializedProperty levels = so.FindProperty("levels");
            int index = levels.arraySize;
            levels.arraySize++;
            SerializedProperty entry = levels.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("Id").stringValue = id;
            entry.FindPropertyRelative("SceneName").stringValue = sceneName;
            entry.FindPropertyRelative("DisplayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("appended " + id + " to LevelListConfig");
        }

        static void AddToBuildSettings(string scenePath, List<string> changes)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == scenePath)) return;
            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
            changes.Add("added " + scenePath + " to Build Settings");
        }

        static bool SceneFileExists(string id) => AssetDatabase.LoadAssetAtPath<Object>(ScenePathFor(id)) != null;
        static string ScenePathFor(string id)
        {
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (config != null)
            {
                int index = IndexOf(config, id);
                if (index >= 0) return $"{LevelsFolder}/{config.Levels[index].SceneName}.unity";
            }
            return $"{LevelsFolder}/{DefaultSceneName(id)}.unity";
        }
        static string DefaultSceneName(string id) => "Level_" + id.TrimStart('L');
        static string DefaultDisplayName(string id) => "Level " + int.Parse(id.TrimStart('L'));

        static T FindComponent<T>(Scene scene, string rootName) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == rootName) return root.GetComponent<T>();
            return null;
        }

        static T FindComponentAnywhere<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
