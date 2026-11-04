using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-V06. Idempotent: makes the FeelConfig asset if it's missing and gives it Pack I's effect sprites (D-312), wires it
    /// into the cat prefab's presenter (squash and stretch), then gives _LevelTemplate a wired LevelFeel on its own GameFeel root and
    /// the pause panel's Motion button (PauseMenuSetup.BuildPauseUI), saving the template. Rebuild All Levels then carries both into
    /// every Level_NNN. Run PARALLAX/Art/Import Trap Kit first (the sprites are the trap kit's). It opens the template itself, so it
    /// refuses while the open scene has unsaved changes.</summary>
    public static class GameFeelSetup
    {
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string CatPrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string EffectsFolder = "Assets/_Game/Art/Traps/Effects/";
        const string HudCanvasName = "HUD";
        public const string RootName = "GameFeel";

        [MenuItem("PARALLAX/Setup/Game Feel (PAX-V06)")]
        public static void Configure()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError("GameFeelSetup: '" + active.name + "' has unsaved changes; save or discard them first."); return; }
            string originalPath = active.path;

            var changes = new List<string>();
            FeelConfig config = EnsureConfig(changes);
            WireCat(config, changes);
            Scene scene = EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            // Loaded after the open: opening a scene unloads unreferenced assets, the config included.
            AddTo(scene, AssetDatabase.LoadAssetAtPath<FeelConfig>(FeelConfig.AssetPath), changes);
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == HudCanvasName) PauseMenuSetup.BuildPauseUI(root.transform, changes);
            if (changes.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) != null)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("GameFeelSetup:\n" + (changes.Count > 0 ? string.Join("\n", changes) : "no changes")
                + "\nRun PARALLAX/Setup/Levels/Rebuild All Levels next, so every level gets the template's feel.");
        }

        public static FeelConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<FeelConfig>(FeelConfig.AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FeelConfig>();
                AssetDatabase.CreateAsset(config, FeelConfig.AssetPath);
                changes.Add("created " + FeelConfig.AssetPath);
            }
            var serialized = new SerializedObject(config);
            bool changed = SetSprites(serialized, "runPuffs", "FX_Puff_0", "FX_Puff_1", "FX_Puff_2", "FX_Puff_3")
                | SetSprites(serialized, "landPuffs", "FX_DustLand_0", "FX_DustLand_1")
                | SetSprites(serialized, "skidPuffs", "FX_DustSkid_0", "FX_DustSkid_1")
                | SetSprites(serialized, "impactRings", "FX_Impact_0", "FX_Impact_1", "FX_Impact_2")
                | SetSprites(serialized, "chips", Enumerable.Range(0, 8).Select(i => "FX_Debris_" + i).ToArray())
                | SetSprites(serialized, "sparks", "FX_Spark_1", "FX_Spark_2", "FX_Spark_3")
                | SetSprites(serialized, "shards", Enumerable.Range(0, 6).Select(i => "FX_Shard_" + i).ToArray())
                | SetSprites(serialized, "coldSparks", "FX_SparkCyan_1", "FX_SparkCyan_2", "FX_SparkCyan_3")
                | SetSprites(serialized, "leaves", Enumerable.Range(0, 6).Select(i => "TRAP-13_leaf_" + i).ToArray())
                | SetSprite(serialized, "mote", "mote")
                | SetSprite(serialized, "flash", "flash")
                | SetSprite(serialized, "burst", "FX_DoorBurst");
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                changes.Add("set " + FeelConfig.AssetPath + " sprites");
            }
            return config;
        }

        static Sprite Load(string name)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(EffectsFolder + name + ".png");
            if (s == null) Debug.LogError("GameFeelSetup: no sprite at " + EffectsFolder + name + ".png; run PARALLAX/Art/Import Trap Kit first.");
            return s;
        }

        static bool SetSprite(SerializedObject o, string field, string name)
        {
            SerializedProperty p = o.FindProperty(field);
            Sprite s = Load(name);
            if (p == null || p.objectReferenceValue == s) return false;
            p.objectReferenceValue = s;
            return true;
        }

        static bool SetSprites(SerializedObject o, string field, params string[] names)
        {
            SerializedProperty p = o.FindProperty(field);
            Sprite[] sprites = names.Select(Load).Where(s => s != null).ToArray();
            if (p == null || (p.arraySize == sprites.Length && Enumerable.Range(0, sprites.Length).All(i => p.GetArrayElementAtIndex(i).objectReferenceValue == sprites[i]))) return false;
            p.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            return true;
        }

        /// <summary>The cat prefab's presenter gets the config (its squash and stretch).</summary>
        static void WireCat(FeelConfig config, List<string> changes)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CatPrefabPath);
            try
            {
                CatVisualPresenter presenter = root.GetComponentInChildren<CatVisualPresenter>(true);
                if (presenter == null) { Debug.LogError("GameFeelSetup: " + CatPrefabPath + " has no CatVisualPresenter; run PARALLAX/Setup/Cat Visual first."); return; }
                var before = changes.Count;
                TrapKitSetup.Write(presenter, changes, ("feel", config));
                if (changes.Count > before) { PrefabUtility.SaveAsPrefabAsset(root, CatPrefabPath); changes.Add("wired " + CatPrefabPath + " presenter.feel"); }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Adds the GameFeel root with a wired LevelFeel. A scene without a RoomManager, RoomDeath or ObserverSet is left alone.</summary>
        public static void AddTo(Scene scene, FeelConfig config, List<string> changes)
        {
            RoomManager rooms = AudioSetup.Find<RoomManager>(scene);
            RoomDeath death = AudioSetup.Find<RoomDeath>(scene);
            ObserverSet observers = AudioSetup.Find<ObserverSet>(scene);
            LevelCameraFollow follow = AudioSetup.Find<LevelCameraFollow>(scene);
            if (rooms == null || death == null || observers == null) { Debug.LogError("GameFeelSetup: '" + scene.name + "' has no RoomManager, RoomDeath or ObserverSet; no feel."); return; }
            if (follow == null) Debug.LogError("GameFeelSetup: '" + scene.name + "' has no LevelCameraFollow; the feel won't shake the camera.");

            GameObject root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                changes.Add("created " + RootName);
            }
            LevelFeel feel = SetupUtility.Ensure<LevelFeel>(root, changes);
            TrapKitSetup.Write(feel, changes, ("config", config), ("rooms", rooms), ("death", death), ("observers", observers), ("cameraFollow", follow));
            LevelAir air = SetupUtility.Ensure<LevelAir>(root, changes);
            TrapKitSetup.Write(air, changes, ("config", config), ("view", follow != null ? follow.GetComponent<Camera>() : null));
        }

        /// <summary>P5.2/P5.3, at each level's build (LevelSetup): the level's world for what its impacts throw, and its air: motes
        /// in Obsidian Void, leaves in Golden Ruins by its district's plants (most in The Overgrown Quarter). A scene without the
        /// GameFeel root (the menu not run yet) is left alone.</summary>
        public static void BakeLevel(Scene scene, string levelId, List<string> changes)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == RootName);
            if (root == null) return;
            bool obsidian = Parallax.Editor.Levels.Biomes.For(levelId) == Parallax.Editor.Levels.Biome.ObsidianVoid;
            int count = obsidian ? 10 : Mathf.RoundToInt(4f + 8f * Parallax.Editor.Levels.LevelLooks.PlantShare(levelId));
            if (root.TryGetComponent(out LevelFeel feel)) TrapKitSetup.Write(feel, changes, ("obsidian", obsidian));
            if (root.TryGetComponent(out LevelAir air)) TrapKitSetup.Write(air, changes, ("obsidian", obsidian), ("count", count));
        }
    }
}
