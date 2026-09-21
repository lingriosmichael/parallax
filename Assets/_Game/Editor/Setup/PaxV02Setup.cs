using System.Collections.Generic;
using Parallax.Gameplay;
using Parallax.Gameplay.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static class PaxV02Setup
    {
        [MenuItem("PARALLAX/Setup/PAX-V02 Runtime Cleanup (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            ConfigureHudDevOnly(changes);
            DisablePlaceholderSquares(changes);
            DisableTouchDebugOverlay(changes);
            AnchorForeground("RealityRoot_A", changes);
            AnchorForeground("RealityRoot_B", changes);

            if (changes.Count == 0)
            {
                Debug.Log("PaxV02Setup: no changes.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("PaxV02Setup:\n - " + string.Join("\n - ", changes));
        }

        static void ConfigureHudDevOnly(List<string> changes)
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogError("PaxV02Setup: HUD is missing.");
                return;
            }

            EnsureDevOnly(hud.transform.Find("SwitchButton"), changes);
            EnsureDevOnly(hud.transform.Find("RecordButton"), changes);
            EnsureDevOnly(hud.transform.Find("EchoTimeline"), changes);
        }

        static void EnsureDevOnly(Transform target, List<string> changes)
        {
            if (target == null)
            {
                Debug.LogError("PaxV02Setup: expected HUD dev control is missing.");
                return;
            }

            SetupUtility.Ensure<DevOnly>(target.gameObject, changes);
        }

        static void DisablePlaceholderSquares(List<string> changes)
        {
            SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer.sprite == null || renderer.sprite.name != "Square" || renderer.gameObject.name == "Goal_B") continue;
                if (!renderer.enabled) continue;

                renderer.enabled = false;
                changes.Add("disabled Square renderer at " + RealityIsolationValidator.Path(renderer.transform));
            }
        }

        static void DisableTouchDebugOverlay(List<string> changes)
        {
            TouchStickCatInput input = Object.FindFirstObjectByType<TouchStickCatInput>(FindObjectsInactive.Include);
            if (input == null)
            {
                Debug.LogError("PaxV02Setup: TouchStickCatInput is missing.");
                return;
            }

            var serialized = new SerializedObject(input);
            SerializedProperty property = serialized.FindProperty("showDebugOverlay");
            if (property == null || !property.boolValue) return;

            property.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set TouchStickCatInput.showDebugOverlay = false");
        }

        static void AnchorForeground(string rootName, List<string> changes)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.LogError("PaxV02Setup: " + rootName + " is missing.");
                return;
            }

            Transform ceiling = root.transform.Find("Geometry/GravityArena/Ceiling");
            Transform foreground = root.transform.Find("Foreground");
            SpriteRenderer art = foreground == null ? null : foreground.GetComponentInChildren<SpriteRenderer>(true);
            Collider2D ceilingCollider = ceiling == null ? null : ceiling.GetComponent<Collider2D>();
            if (foreground == null || art == null || ceilingCollider == null)
            {
                Debug.LogError("PaxV02Setup: could not anchor Foreground for " + rootName + ".");
                return;
            }

            float offset = ceilingCollider.bounds.min.y - art.bounds.max.y;
            if (Mathf.Approximately(offset, 0f)) return;

            foreground.position += Vector3.up * offset;
            changes.Add("anchored " + RealityIsolationValidator.Path(foreground) + ".top to Ceiling underside");
        }
    }
}
