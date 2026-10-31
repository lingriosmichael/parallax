using System.Collections.Generic;
using Parallax.Gameplay.Audio;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-065 (D-278). Idempotent: makes the HapticsConfig asset if it's missing, then gives _LevelTemplate a wired LevelHaptics
    /// on its GameAudio root and the pause panel's Vibration button (PauseMenuSetup.BuildPauseUI), saving the template. Rebuild All
    /// Levels then carries both into every Level_NNN. Run PARALLAX/Setup/Audio (PAX-064) first: the root is its. It opens the template
    /// itself, so it refuses while the open scene has unsaved changes.</summary>
    public static class HapticsSetup
    {
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string HudCanvasName = "HUD";

        [MenuItem("PARALLAX/Setup/Haptics (PAX-065)")]
        public static void Configure()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError("HapticsSetup: '" + active.name + "' has unsaved changes; save or discard them first."); return; }
            string originalPath = active.path;

            var changes = new List<string>();
            EnsureConfig(changes);
            Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            // Loaded after the open: opening a scene unloads unreferenced assets, the config included.
            AddTo(scene, EnsureConfig(changes), changes);
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == HudCanvasName) PauseMenuSetup.BuildPauseUI(root.transform, changes);
            if (changes.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) != null)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("HapticsSetup:\n" + (changes.Count > 0 ? string.Join("\n", changes) : "no changes")
                + "\nRun PARALLAX/Setup/Levels/Rebuild All Levels next, so every level gets the template's haptics.");
        }

        public static HapticsConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<HapticsConfig>(HapticsConfig.AssetPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<HapticsConfig>();
            AssetDatabase.CreateAsset(config, HapticsConfig.AssetPath);
            AssetDatabase.SaveAssets();
            changes.Add("created " + HapticsConfig.AssetPath);
            return config;
        }

        /// <summary>Adds LevelHaptics to the scene's GameAudio root and wires it. A scene without that root, or a RoomManager, is left alone.</summary>
        public static void AddTo(Scene scene, HapticsConfig config, List<string> changes)
        {
            GameObject root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == AudioSetup.RootName) { root = candidate; break; }
            if (root == null) { Debug.LogError("HapticsSetup: '" + scene.name + "' has no " + AudioSetup.RootName + " root; run PARALLAX/Setup/Audio (PAX-064) first."); return; }

            RoomManager rooms = AudioSetup.Find<RoomManager>(scene);
            RoomDeath death = AudioSetup.Find<RoomDeath>(scene);
            ObserverSet observers = AudioSetup.Find<ObserverSet>(scene);
            if (rooms == null || death == null || observers == null) { Debug.LogError("HapticsSetup: '" + scene.name + "' has no RoomManager, RoomDeath or ObserverSet; no haptics."); return; }
            LevelHaptics haptics = SetupUtility.Ensure<LevelHaptics>(root, changes);
            TrapKitSetup.Write(haptics, changes, ("config", config), ("rooms", rooms), ("death", death), ("observers", observers));
        }
    }
}
