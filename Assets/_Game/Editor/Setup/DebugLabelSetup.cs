using System.Collections.Generic;
using Parallax.DebugTools;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class DebugLabelSetup
    {
        [MenuItem("PARALLAX/Setup/Debug Labels (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            DebugPanel panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            SoloSwitchController switchController = Object.FindAnyObjectByType<SoloSwitchController>(FindObjectsInactive.Include);
            TransportHost transportHost = Object.FindAnyObjectByType<TransportHost>(FindObjectsInactive.Include);
            if (panel == null || observers == null || switchController == null || transportHost == null)
            {
                Debug.LogError("DebugLabelSetup: DebugPanel, ObserverSet, SoloSwitchController, and TransportHost are required.");
                return;
            }

            WorldLabelOverlay overlay = SetupUtility.Ensure<WorldLabelOverlay>(panel.gameObject, changes);
            SetupUtility.SetObject(overlay, "observers", observers, changes);
            SetupUtility.SetObject(overlay, "switchController", switchController, changes);
            SetupUtility.SetObject(overlay, "transportHost", transportHost, changes);
            SetupUtility.SetObject(overlay, "debugPanel", panel, changes);

            if (changes.Count == 0)
            {
                Debug.Log("DebugLabelSetup: no changes.");
                return;
            }

            Debug.Log("DebugLabelSetup: " + string.Join("; ", changes));
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        }
    }
}
