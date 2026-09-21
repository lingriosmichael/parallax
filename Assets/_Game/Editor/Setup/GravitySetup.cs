using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class GravitySetup
    {
        [MenuItem("PARALLAX/Setup/Gravity Up-Down (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            QuantizeDirections<GravityReceiver>("initialDirection", changes);
            QuantizeDirections<CheckpointMarker>("gravityDirection", changes);
            QuantizeDirections<SpawnPoint>("gravityDirection", changes);
            SetScreenRelativeProjection(changes);

            if (changes.Count == 0)
            {
                Debug.Log("GravitySetup: no changes.");
            }
            else
            {
                Debug.Log("GravitySetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }

            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
            ControlValidator.Validate();
            CheckpointValidator.Validate();
            GravityValidator.Validate();
        }

        static void QuantizeDirections<T>(string propertyName, List<string> changes) where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(component);
                SerializedProperty directionProperty = serialized.FindProperty(propertyName);
                Vector2 before = directionProperty.vector2Value;
                Vector2 after = VerticalGravity.Quantize(before, Vector2.down);
                if (before == after) continue;
                SetupUtility.SetVector2(component, propertyName, after, changes);
            }
        }

        static void SetScreenRelativeProjection(List<string> changes)
        {
            foreach (TouchStickCatInput input in Object.FindObjectsByType<TouchStickCatInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(input);
                SerializedProperty projection = serialized.FindProperty("projection");
                if (projection.intValue == (int)StickProjection.ScreenRelative) continue;
                projection.intValue = (int)StickProjection.ScreenRelative;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changes.Add($"set {RealityIsolationValidator.Path(input.transform)}.projection to ScreenRelative");
            }
        }
    }
}
