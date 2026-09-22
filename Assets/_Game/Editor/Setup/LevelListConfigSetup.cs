using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-050 (D-063): idempotent find-or-create for the single LevelListConfig asset.
    /// Seeds it with the one real level that exists today; adding further entries is later
    /// content work, not this ticket. Re-running never overwrites an already-populated list.</summary>
    public static class LevelListConfigSetup
    {
        const string ConfigPath = "Assets/_Game/Data/LevelListConfig.asset";

        [MenuItem("PARALLAX/Setup/Level List (PAX-050)")]
        public static void Configure()
        {
            LevelListConfig config = AssetDatabase.LoadAssetAtPath<LevelListConfig>(ConfigPath);
            if (config != null)
            {
                Debug.Log("LevelListConfigSetup: no changes.");
                return;
            }

            config = ScriptableObject.CreateInstance<LevelListConfig>();
            var so = new SerializedObject(config);
            SerializedProperty levels = so.FindProperty("levels");
            levels.arraySize = 1;
            SerializedProperty entry = levels.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("Id").stringValue = "Level_Solo01";
            entry.FindPropertyRelative("SceneName").stringValue = "Level_Solo01";
            entry.FindPropertyRelative("DisplayName").stringValue = "Level 1";
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
            AssetDatabase.CreateAsset(config, ConfigPath);
            Debug.Log("LevelListConfigSetup: created LevelListConfig asset with one entry (Level_Solo01).");
        }
    }
}
