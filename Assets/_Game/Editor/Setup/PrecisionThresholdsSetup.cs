using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-076 (D-083) R6: creates the PrecisionThresholds asset once. Idempotent: an existing asset is left exactly
    // as it is (copied from PlatformSizeConfigSetup).
    public static class PrecisionThresholdsSetup
    {
        public const string AssetPath = "Assets/_Game/Data/PrecisionThresholds.asset";

        [MenuItem("PARALLAX/Setup/Precision Thresholds (PAX-076)")]
        public static void Configure()
        {
            if (AssetDatabase.LoadAssetAtPath<PrecisionThresholds>(AssetPath) != null)
            {
                Debug.Log("PrecisionThresholdsSetup: " + AssetPath + " already exists; no changes.");
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PrecisionThresholds>(), AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("PrecisionThresholdsSetup: created " + AssetPath + ".");
        }
    }
}
