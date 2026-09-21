using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class GravityValidator
    {
        [MenuItem("PARALLAX/Validate/Gravity")]
        public static int Validate()
        {
            int problems = 0;
            problems += ValidateDirections<GravityReceiver>("initialDirection");
            problems += ValidateDirections<CheckpointMarker>("gravityDirection");
            problems += ValidateDirections<SpawnPoint>("gravityDirection");

            foreach (TouchStickCatInput input in Object.FindObjectsByType<TouchStickCatInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(input);
                SerializedProperty projection = serialized.FindProperty("projection");
                if (projection.intValue == (int)StickProjection.ScreenRelative) continue;
                Debug.LogError($"Gravity: '{RealityIsolationValidator.Path(input.transform)}' does not use ScreenRelative projection.", input);
                problems++;
            }

            if (problems == 0) Debug.Log("Gravity: OK");
            return problems;
        }

        static int ValidateDirections<T>(string propertyName) where T : Component
        {
            int problems = 0;
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(component);
                Vector2 direction = serialized.FindProperty(propertyName).vector2Value;
                if (VerticalGravity.IsVertical(direction)) continue;
                Debug.LogError($"Gravity: '{RealityIsolationValidator.Path(component.transform)}' has non-vertical {propertyName} {direction}.", component);
                problems++;
            }
            return problems;
        }
    }
}
