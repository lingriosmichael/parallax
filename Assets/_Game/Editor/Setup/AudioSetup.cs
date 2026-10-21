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
    /// <summary>PAX-064 (D-158). Idempotent: makes the AudioCueBank asset if it's missing, then gives _LevelTemplate, Title and
    /// LevelSelect a root "GameAudio" (GameAudio, plus LevelAudioEvents wired to the RoomManager, RoomDeath and ObserverSet in
    /// a level), saving each scene it changes. Rebuild All Levels then carries the template's into every Level_NNN. Like
    /// Rebuild All, it opens those scenes itself, so it refuses while the open scene has unsaved changes.</summary>
    public static class AudioSetup
    {
        public const string RootName = "GameAudio";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string LevelSelectPath = "Assets/_Game/Scenes/" + LevelSceneLoader.LevelSelectSceneName + ".unity";

        [MenuItem("PARALLAX/Setup/Audio (PAX-064)")]
        public static void Configure()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError("AudioSetup: '" + active.name + "' has unsaved changes; save or discard them first."); return; }
            string originalPath = active.path;

            var changes = new List<string>();
            EnsureBank(changes);
            foreach (string path in new[] { TemplatePath, TitleSetup.ScenePath, LevelSelectPath })
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) { changes.Add(path + " doesn't exist, skipped"); continue; }
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var sceneChanges = new List<string>();
                // Loaded again after each open: opening a scene unloads unreferenced assets, the bank included.
                AddTo(scene, EnsureBank(changes), sceneChanges);
                if (sceneChanges.Count == 0) { changes.Add(scene.name + ": no changes"); continue; }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changes.Add(scene.name + ": " + string.Join(", ", sceneChanges) + " (saved)");
            }

            if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) != null)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("AudioSetup:\n" + string.Join("\n", changes) + "\nRun PARALLAX/Setup/Levels/Rebuild All Levels next, so every level gets the template's audio.");
        }

        public static AudioCueBank EnsureBank(List<string> changes)
        {
            var bank = AssetDatabase.LoadAssetAtPath<AudioCueBank>(AudioCueBank.AssetPath);
            if (bank != null) return bank;
            bank = ScriptableObject.CreateInstance<AudioCueBank>();
            AssetDatabase.CreateAsset(bank, AudioCueBank.AssetPath);
            AssetDatabase.SaveAssets();
            changes.Add("created " + AudioCueBank.AssetPath);
            return bank;
        }

        /// <summary>Find-or-create the scene's GameAudio root and wire it; a scene with a RoomManager also gets LevelAudioEvents.</summary>
        public static void AddTo(Scene scene, AudioCueBank bank, List<string> changes)
        {
            GameObject root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName) { root = candidate; break; }
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                changes.Add("created " + RootName);
            }

            GameAudio gameAudio = SetupUtility.Ensure<GameAudio>(root, changes);
            TrapKitSetup.Write(gameAudio, changes, ("bank", bank));

            RoomManager rooms = Find<RoomManager>(scene);
            if (rooms == null) return;
            RoomDeath death = Find<RoomDeath>(scene);
            ObserverSet observers = Find<ObserverSet>(scene);
            if (death == null || observers == null) { Debug.LogError("AudioSetup: '" + scene.name + "' has a RoomManager but no RoomDeath or ObserverSet; its level events stay unwired."); return; }
            LevelAudioEvents events = SetupUtility.Ensure<LevelAudioEvents>(root, changes);
            TrapKitSetup.Write(events, changes, ("gameAudio", gameAudio), ("rooms", rooms), ("death", death), ("observers", observers));
        }

        static T Find<T>(Scene scene) where T : Component
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
