using System.Collections.Generic;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class HazardSetup
    {
        static readonly Vector2 HazardSize = new Vector2(0.5f, 0.3f);

        [MenuItem("PARALLAX/Setup/Hazards and Room Reset (PAX-041)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject systems = GameObject.Find("Systems");
            RealityRoot rootA = GameObject.Find("RealityRoot_A")?.GetComponent<RealityRoot>();
            CheckpointManager checkpoints = Object.FindAnyObjectByType<CheckpointManager>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            RoomManager rooms = Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include);
            TransportHost transport = Object.FindAnyObjectByType<TransportHost>(FindObjectsInactive.Include);
            DebugPanel panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (systems == null || rootA == null || checkpoints == null || observers == null || rooms == null || transport == null)
            {
                Debug.LogError("HazardSetup: Systems, RealityRoot_A, CheckpointManager, ObserverSet, RoomManager and TransportHost are required.");
                return;
            }

            RoomDeath death = SetupUtility.Ensure<RoomDeath>(systems, changes);
            SetupUtility.SetObject(death, "checkpoints", checkpoints, changes);
            SetupUtility.SetObject(death, "rooms", rooms, changes);
            SetupUtility.SetObject(death, "observers", observers, changes);
            SetupUtility.SetObject(death, "transportHost", transport, changes);
            SetupUtility.SetObject(rooms, "roomDeath", death, changes);
            if (panel != null) SetupUtility.SetObject(panel, "roomDeath", death, changes);
            foreach (FallResetVolume volume in Object.FindObjectsByType<FallResetVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SetupUtility.SetObject(volume, "roomDeath", death, changes);

            Transform groundTransform = rootA.transform.Find("Geometry/Ground");
            BoxCollider2D ground = groundTransform == null ? null : groundTransform.GetComponent<BoxCollider2D>();
            if (ground == null)
            {
                Debug.LogError("HazardSetup: RealityRoot_A/Geometry/Ground BoxCollider2D is required.");
                return;
            }
            float groundTop = rootA.ToLocal((Vector2)ground.bounds.max).y;
            BuildHazard(rootA, death, observers, 0, 1.5f, groundTop, changes);
            BuildHazard(rootA, death, observers, 1, 6.5f, groundTop, changes);
            BuildDebugTrap(rootA, death, observers, groundTop, changes);
            BuildAnchorBinding(rootA, death, changes);

            if (changes.Count == 0) Debug.Log("HazardSetup: no changes.");
            else
            {
                Debug.Log("HazardSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }
        }

        static void BuildHazard(RealityRoot root, RoomDeath death, ObserverSet observers, int id, float x, float groundTop, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform folder = SetupUtility.EnsureChild(root.transform, "Hazards", layer, changes);
            GameObject go = SetupUtility.EnsureChild(folder, $"Hazard_{id}", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(go.transform, new Vector2(x, groundTop + HazardSize.y * 0.5f), changes);
            SetupUtility.SetVisual(go, root, HazardSize, Color.red, changes);
            BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(go, changes);
            box.isTrigger = true;
            SetupUtility.SetColliderSize(box, HazardSize, changes);
            Hazard hazard = SetupUtility.Ensure<Hazard>(go, changes);
            SetupUtility.SetObject(hazard, "observers", observers, changes);
            SetupUtility.SetObject(hazard, "roomDeath", death, changes);
            SetupUtility.SetObject(hazard, "rooms", Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include), changes);
        }

        static void BuildDebugTrap(RealityRoot root, RoomDeath death, ObserverSet observers, float groundTop, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform folder = SetupUtility.EnsureChild(root.transform, "DebugTraps", layer, changes);
            GameObject go = SetupUtility.EnsureChild(folder, "DebugTrap_0", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(go.transform, new Vector2(-2f, groundTop + 0.4f), changes);
            SetupUtility.SetVisual(go, root, new Vector2(0.8f, 0.8f), Color.gray, changes);
            BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(go, changes);
            box.isTrigger = true;
            SetupUtility.SetColliderSize(box, new Vector2(0.8f, 0.8f), changes);
            DebugTrap trap = SetupUtility.Ensure<DebugTrap>(go, changes);
            SetupUtility.SetObject(trap, "observers", observers, changes);
            SetupUtility.SetObject(trap, "roomDeath", death, changes);
            SetupUtility.SetObject(trap, "rooms", Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include), changes);
            SetInt(trap, "roomId", 0, changes);
            SetEnum(trap, "initialState", (int)TrapState.Armed, changes);
        }

        static void BuildAnchorBinding(RealityRoot root, RoomDeath death, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            GameObject go = SetupUtility.EnsureChild(root.transform, "RoomAnchorBinding_Debug", layer, changes).gameObject;
            RoomAnchorBinding binding = SetupUtility.Ensure<RoomAnchorBinding>(go, changes);
            SetupUtility.SetObject(binding, "roomDeath", death, changes);
            SetInt(binding, "roomId", 0, changes);
            SetInt(binding, "anchorId", 65535, changes);
            SetupUtility.SetFloat(binding, "initialValue", 0f, changes);
        }

        static void SetInt(Object target, string propertyName, int value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.intValue == value) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + propertyName);
        }

        static void SetEnum(Object target, string propertyName, int value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.enumValueIndex == value) return;
            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + propertyName);
        }
    }
}
