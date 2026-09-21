using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // Read-only. Changes nothing.
    public static class RoomValidator
    {
        [MenuItem("PARALLAX/Validate/Rooms")]
        public static int Validate()
        {
            int problems = 0;

            RoomManager[] managers = Object.FindObjectsByType<RoomManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (managers.Length != 1)
            {
                Debug.LogError($"Rooms: expected exactly one RoomManager on Systems, found {managers.Length}.");
                return managers.Length + 1;
            }
            RoomManager manager = managers[0];
            var managerSerialized = new SerializedObject(manager);
            ObserverId solo = (ObserverId)managerSerialized.FindProperty("soloReality").enumValueIndex;
            if (managerSerialized.FindProperty("checkpoints").objectReferenceValue == null || managerSerialized.FindProperty("observers").objectReferenceValue == null)
            {
                Debug.LogError("Rooms: RoomManager is missing checkpoints or observers.", manager);
                problems++;
            }

            var doorIds = new HashSet<int>();
            foreach (RoomDoor door in Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot root = door.GetComponentInParent<RealityRoot>();
                var serialized = new SerializedObject(door);
                int id = serialized.FindProperty("roomId").intValue;
                if (root == null || root.Id != solo)
                {
                    Debug.LogError($"Rooms: door '{door.name}' is not under the solo reality's root.", door);
                    problems++;
                    continue;
                }
                if (serialized.FindProperty("manager").objectReferenceValue != manager)
                {
                    Debug.LogError($"Rooms: door '{door.name}' is not wired to the RoomManager.", door);
                    problems++;
                }
                if (root.gameObject.layer != LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id)) || door.gameObject.layer != root.gameObject.layer)
                {
                    Debug.LogError($"Rooms: door '{door.name}' layer mismatch.", door);
                    problems++;
                }
                if (!doorIds.Add(id))
                {
                    Debug.LogError($"Rooms: duplicate door id {id}.", door);
                    problems++;
                }
            }

            var markerIds = new HashSet<int>();
            foreach (CheckpointMarker marker in Object.FindObjectsByType<CheckpointMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot root = marker.GetComponentInParent<RealityRoot>();
                if (root != null && root.Id == solo) markerIds.Add(marker.Id);
            }

            for (int i = 0; i < doorIds.Count; i++)
            {
                if (!doorIds.Contains(i))
                {
                    Debug.LogError($"Rooms: door ids are not contiguous from 0 (missing {i}).");
                    problems++;
                    break;
                }
            }

            foreach (int id in doorIds)
            {
                if (!markerIds.Contains(id))
                {
                    Debug.LogError($"Rooms: door {id} has no CheckpointMarker with id {id} in reality {solo}.");
                    problems++;
                }
            }

            if (problems == 0) Debug.Log("Rooms: OK");
            return problems;
        }
    }
}
