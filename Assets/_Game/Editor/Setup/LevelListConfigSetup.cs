using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-050 (D-063): idempotent find-or-create for the single LevelListConfig asset.
    /// PAX-051 (D-066): seeds a newly-created asset with L001-L004 (Level_Solo01's four rooms,
    /// split into one-room levels). Level_Solo01 is not in the seed but stays in the repo/Build
    /// Settings as reference. If the asset already exists, this menu does not modify it: further
    /// entries (L005+, or anything else) are appended by
    /// PARALLAX/Setup/Levels/New Level..., not by this menu.</summary>
    public static class LevelListConfigSetup
    {
        const string ConfigPath = "Assets/_Game/Data/LevelListConfig.asset";
        static readonly (string id, string scene, string display)[] Seed = {
            ("L001", "Level_001", "Level 1"),
            ("L002", "Level_002", "Level 2"),
            ("L003", "Level_003", "Level 3"),
            ("L004", "Level_004", "Level 4"),
        };

        [MenuItem("PARALLAX/Setup/Level List (PAX-050)")]
        public static void Configure()
        {
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(ConfigPath);
            if (config != null)
            {
                Debug.Log("LevelListConfigSetup: asset already exists; no changes. Add levels via PARALLAX/Setup/Levels/New Level...");
                return;
            }

            config = ScriptableObject.CreateInstance<LevelListConfig>();
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
            AssetDatabase.CreateAsset(config, ConfigPath);

            var so = new SerializedObject(config);
            SerializedProperty levels = so.FindProperty("levels");
            levels.arraySize = Seed.Length;
            for (int i = 0; i < Seed.Length; i++)
            {
                SerializedProperty entry = levels.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Id").stringValue = Seed[i].id;
                entry.FindPropertyRelative("SceneName").stringValue = Seed[i].scene;
                entry.FindPropertyRelative("DisplayName").stringValue = Seed[i].display;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("LevelListConfigSetup: created LevelListConfig asset with L001-L004.");
        }
    }
}
