using System.Collections.Generic;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            RealityRoot rootA = GetRoot("RealityRoot_A");
            RealityRoot rootB = GetRoot("RealityRoot_B");
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            SoloSwitchController switchController = Object.FindAnyObjectByType<SoloSwitchController>(FindObjectsInactive.Include);
            if (rootA == null || rootB == null || observers == null || switchController == null)
            {
                Debug.LogError("DebugLabelSetup: RealityRoot_A, RealityRoot_B, ObserverSet, and SoloSwitchController are required.");
                return;
            }

            LabelNamed(rootA, "Anchors/Plate_A", "Plate", changes);
            LabelNamed(rootA, "Anchors/Vine_A", "Vine", changes);
            LabelNamed(rootA, "Interactables/Station_A", "Station", changes);
            LabelNamed(rootA, "Geometry/Ground", "Ground", changes);
            LabelNamed(rootB, "Anchors/Elevator_B", "Elevator", changes);
            LabelNamed(rootB, "Anchors/Gate_B", "Gate", changes);
            LabelNamed(rootB, "Geometry/Ledge_B", "Ledge", changes);
            LabelNamed(rootB, "Geometry/Pillar_BOnly", "Pillar", changes);
            LabelNamed(rootB, "Geometry/Goal_B", "Goal", changes);
            LabelNamed(rootB, "Geometry/Ground", "Ground", changes);
            LabelCheckpoints(rootA, changes);
            LabelCheckpoints(rootB, changes);
            LabelFallVolumes(rootA, changes);
            LabelFallVolumes(rootB, changes);

            DebugLabelOverlay overlay = EnsureOverlay(changes);
            SetupUtility.SetObject(overlay, "observers", observers, changes);
            SetupUtility.SetObject(overlay, "rootA", rootA, changes);
            SetupUtility.SetObject(overlay, "rootB", rootB, changes);
            SetupUtility.SetObject(overlay, "switchController", switchController, changes);

            if (changes.Count == 0)
            {
                Debug.Log("DebugLabelSetup: no changes.");
                return;
            }

            Debug.Log("DebugLabelSetup: " + string.Join("; ", changes));
            EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
        }

        static RealityRoot GetRoot(string name)
        {
            GameObject rootObject = GameObject.Find(name);
            return rootObject == null ? null : rootObject.GetComponent<RealityRoot>();
        }

        static void LabelNamed(RealityRoot root, string path, string text, List<string> changes)
        {
            Transform target = root.transform.Find(path);
            if (target == null) return;
            SetLabel(target.gameObject, text, changes);
        }

        static void LabelCheckpoints(RealityRoot root, List<string> changes)
        {
            foreach (CheckpointMarker marker in root.GetComponentsInChildren<CheckpointMarker>(true))
            {
                SetLabel(marker.gameObject, "Checkpoint", changes);
            }
        }

        static void LabelFallVolumes(RealityRoot root, List<string> changes)
        {
            foreach (FallResetVolume volume in root.GetComponentsInChildren<FallResetVolume>(true))
            {
                SetLabel(volume.gameObject, "Fall reset", changes);
            }
        }

        static void SetLabel(GameObject target, string text, List<string> changes)
        {
            DebugLabel label = SetupUtility.Ensure<DebugLabel>(target, changes);
            var serialized = new SerializedObject(label);
            SerializedProperty property = serialized.FindProperty("text");
            if (property.stringValue == text) return;
            property.stringValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + ".text");
        }

        static DebugLabelOverlay EnsureOverlay(List<string> changes)
        {
            DebugLabelOverlay overlay = Object.FindAnyObjectByType<DebugLabelOverlay>(FindObjectsInactive.Include);
            if (overlay != null) return overlay;

            GameObject overlayObject = new GameObject("DebugLabelOverlay");
            Undo.RegisterCreatedObjectUndo(overlayObject, "PARALLAX Setup");
            changes.Add("created DebugLabelOverlay");
            return overlayObject.AddComponent<DebugLabelOverlay>();
        }
    }
}
