using System.Collections.Generic;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class CheckpointSetup
    {
        const string SystemsName = "Systems";

        [MenuItem("PARALLAX/Setup/Checkpoints (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject rootAObject = GameObject.Find("RealityRoot_A");
            GameObject rootBObject = GameObject.Find("RealityRoot_B");
            RealityRoot rootA = rootAObject == null ? null : rootAObject.GetComponent<RealityRoot>();
            RealityRoot rootB = rootBObject == null ? null : rootBObject.GetComponent<RealityRoot>();
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            DebugPanel debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (rootA == null || rootB == null || observers == null)
            {
                Debug.LogError("CheckpointSetup: RealityRoot_A, RealityRoot_B, and ObserverSet are required.");
                return;
            }

            CheckpointManager manager = BuildManager(rootA, rootB, changes);

            SpawnPoint spawnA = GetSpawnPoint(rootA);
            SpawnPoint spawnB = GetSpawnPoint(rootB);
            if (spawnA == null || spawnB == null) return;

            float groundTopA = GroundTop(rootA);
            float groundTopB = GroundTop(rootB);
            Vector2 spawnLocalA = rootA.ToLocal(spawnA.Position);
            Vector2 spawnLocalB = rootB.ToLocal(spawnB.Position);

            BuildMarker(rootA, manager, observers, 0, spawnLocalA, spawnA.GravityDirection, changes);
            BuildMarker(rootB, manager, observers, 0, spawnLocalB, spawnB.GravityDirection, changes);
            BuildMarker(rootA, manager, observers, 1, new Vector2(spawnLocalA.x + 5f, groundTopA + 1f), Vector2.down, changes);
            BuildMarker(rootB, manager, observers, 1, new Vector2(spawnLocalB.x + 4f, groundTopB + 1f), Vector2.down, changes);

            WireFallResetVolumes(rootA, rootB, manager, observers, changes);
            WireDebugPanel(debugPanel, manager, changes);

            Debug.Log($"CheckpointSetup: A0=({spawnLocalA.x:F2},{spawnLocalA.y:F2}); B0=({spawnLocalB.x:F2},{spawnLocalB.y:F2}); A1=({spawnLocalA.x + 5f:F2},{groundTopA + 1f:F2}); B1=({spawnLocalB.x + 4f:F2},{groundTopB + 1f:F2}).");

            if (changes.Count == 0) Debug.Log("CheckpointSetup: no changes.");
            else
            {
                Debug.Log("CheckpointSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }

            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
            ControlValidator.Validate();
            CheckpointValidator.Validate();
        }

        static CheckpointManager BuildManager(RealityRoot rootA, RealityRoot rootB, List<string> changes)
        {
            GameObject systemsGO = GameObject.Find(SystemsName);
            if (systemsGO == null)
            {
                systemsGO = new GameObject(SystemsName);
                Undo.RegisterCreatedObjectUndo(systemsGO, "PARALLAX Setup");
                changes.Add($"created {SystemsName}");
            }
            CheckpointManager manager = SetupUtility.Ensure<CheckpointManager>(systemsGO, changes);
            SetupUtility.SetObject(manager, "rootA", rootA, changes);
            SetupUtility.SetObject(manager, "rootB", rootB, changes);
            return manager;
        }

        static CheckpointMarker BuildMarker(RealityRoot root, CheckpointManager manager, ObserverSet observers, int id, Vector2 localPosition, Vector2 gravityDirection, List<string> changes) =>
            BuildMarkerCore(SetupUtility.EnsureChild(root.transform, "Checkpoints", LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id)), changes), root, $"Checkpoint_{id}", manager, observers, id, localPosition, gravityDirection, changes);

        internal static CheckpointMarker BuildMarkerCore(Transform parent, RealityRoot root, string name, CheckpointManager manager, ObserverSet observers, int id, Vector2 localPosition, Vector2 gravityDirection, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            GameObject markerObject = SetupUtility.EnsureChild(parent, name, layer, changes).gameObject;
            SetupUtility.SetLocalPosition(markerObject.transform, localPosition, changes);
            CheckpointMarker marker = SetupUtility.Ensure<CheckpointMarker>(markerObject, changes);
            SetIntField(marker, "checkpointId", id, changes);
            SetupUtility.SetVector2(marker, "gravityDirection", gravityDirection, changes);
            SetupUtility.SetObject(marker, "manager", manager, changes);
            SetupUtility.SetObject(marker, "observers", observers, changes);
            return marker;
        }

        static void SetIntField(Object target, string name, int value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property.intValue == value) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }

        static void WireFallResetVolumes(RealityRoot rootA, RealityRoot rootB, CheckpointManager manager, ObserverSet observers, List<string> changes)
        {
            WireFallResetVolumes(rootA, manager, observers, changes);
            WireFallResetVolumes(rootB, manager, observers, changes);
        }

        static void WireFallResetVolumes(RealityRoot root, CheckpointManager manager, ObserverSet observers, List<string> changes)
        {
            foreach (FallResetVolume volume in root.GetComponentsInChildren<FallResetVolume>(true))
            {
                SetupUtility.SetObject(volume, "checkpoints", manager, changes);
                SetupUtility.SetObject(volume, "observers", observers, changes);
            }
        }

        static void WireDebugPanel(DebugPanel debugPanel, CheckpointManager manager, List<string> changes)
        {
            if (debugPanel == null) return;
            SetupUtility.SetObject(debugPanel, "checkpoints", manager, changes);
        }

        static float GroundTop(RealityRoot root)
        {
            Transform groundTransform = root.transform.Find("Geometry/Ground");
            BoxCollider2D ground = groundTransform == null ? null : groundTransform.GetComponent<BoxCollider2D>();
            if (ground == null)
            {
                Debug.LogError($"CheckpointSetup: {root.Id} Geometry/Ground collider is required.");
                return 0f;
            }
            return root.ToLocal((Vector2)ground.bounds.max).y;
        }

        static SpawnPoint GetSpawnPoint(RealityRoot root)
        {
            SpawnPoint spawn = root.GetComponentInChildren<SpawnPoint>(true);
            if (spawn == null) Debug.LogError($"CheckpointSetup: {root.Id} SpawnPoint is required.");
            return spawn;
        }
    }
}
