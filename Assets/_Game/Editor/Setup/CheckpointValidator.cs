using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // Read-only. Changes nothing.
    public static class CheckpointValidator
    {
        [MenuItem("PARALLAX/Validate/Checkpoints")]
        public static int Validate()
        {
            int problems = 0;

            CheckpointManager[] managers = Object.FindObjectsByType<CheckpointManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length != 1)
            {
                Debug.LogError($"Checkpoints: expected exactly one CheckpointManager on Systems, found {managers.Length}.");
                problems++;
            }
            CheckpointManager manager = managers.Length > 0 ? managers[0] : null;

            var idsByReality = new Dictionary<RealityRoot, HashSet<int>>();
            bool hasZeroA = false;
            bool hasZeroB = false;

            foreach (CheckpointMarker marker in Object.FindObjectsByType<CheckpointMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot root = marker.GetComponentInParent<RealityRoot>();
                var serialized = new SerializedObject(marker);
                CheckpointManager wiredManager = serialized.FindProperty("manager").objectReferenceValue as CheckpointManager;
                Object wiredObservers = serialized.FindProperty("observers").objectReferenceValue;
                int id = serialized.FindProperty("checkpointId").intValue;
                Vector2 gravityDirection = serialized.FindProperty("gravityDirection").vector2Value;

                if (!VerticalGravity.IsVertical(gravityDirection))
                {
                    Debug.LogError($"Checkpoints: marker '{RealityIsolationValidator.Path(marker.transform)}' has non-vertical gravity {gravityDirection}.", marker);
                    problems++;
                }

                if (root == null || wiredManager == null || wiredObservers == null || wiredManager != manager)
                {
                    Debug.LogError($"Checkpoints: invalid marker '{marker.name}'.", marker);
                    problems++;
                    continue;
                }

                if (root.gameObject.layer != LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id)))
                {
                    Debug.LogError($"Checkpoints: marker '{marker.name}' reality layer mismatch.", marker);
                    problems++;
                }

                if (!idsByReality.TryGetValue(root, out HashSet<int> ids))
                {
                    ids = new HashSet<int>();
                    idsByReality.Add(root, ids);
                }
                if (!ids.Add(id))
                {
                    Debug.LogError($"Checkpoints: duplicate id {id} in reality {root.Id}.", marker);
                    problems++;
                }

                if (id == 0)
                {
                    if (root.Id == Parallax.Core.ObserverId.A) hasZeroA = true;
                    else hasZeroB = true;
                }
            }

            if (!hasZeroA)
            {
                Debug.LogError("Checkpoints: Reality A has no id 0 marker.");
                problems++;
            }
            if (!hasZeroB)
            {
                Debug.LogError("Checkpoints: Reality B has no id 0 marker.");
                problems++;
            }

            if (problems == 0) Debug.Log("Checkpoints: OK");
            return problems;
        }
    }
}
