using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-073 (D-074): creates the PlatformSizeConfig asset once. Idempotent: an existing asset
    // is left exactly as it is.
    public static class PlatformSizeConfigSetup
    {
        public const string AssetPath = "Assets/_Game/Data/PlatformSizeConfig.asset";

        [MenuItem("PARALLAX/Setup/Levels/Platform Size Config")]
        public static void Configure()
        {
            if (AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>(AssetPath) != null)
            {
                Debug.Log("PlatformSizeConfigSetup: " + AssetPath + " already exists; no changes.");
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PlatformSizeConfig>(), AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("PlatformSizeConfigSetup: created " + AssetPath + ".");
        }
    }
}
