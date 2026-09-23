using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class SoloRoomsSetup
    {
        const string RoomSafetyConfigPath = "Assets/_Game/Data/RoomSafetyConfig.asset";

        [MenuItem("PARALLAX/Setup/Solo Rooms (PAX-043)")]
        public static void Configure()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Level_Solo01") { Debug.LogError("SoloRoomsSetup: active scene must be Level_Solo01."); return; }
            var changes = new List<string>();
            RealityRoot root = GameObject.Find("RealityRoot_A")?.GetComponent<RealityRoot>();
            CheckpointManager checkpoints = Object.FindAnyObjectByType<CheckpointManager>(FindObjectsInactive.Include);
            RoomManager rooms = Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include);
            RoomDeath death = Object.FindAnyObjectByType<RoomDeath>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            CatMotorConfig config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            if (root == null || checkpoints == null || rooms == null || death == null || observers == null || config == null) { Debug.LogError("SoloRoomsSetup: RealityRoot_A, CheckpointManager, RoomManager, RoomDeath, ObserverSet and CatMotorConfig_Default are required."); return; }
            RoomSafetyConfig safety = EnsureRoomSafetyConfig(changes);
            Wire(death, "config", safety, changes);
            Transform parent = SetupUtility.EnsureChild(root.transform, "Rooms_PAX043", root.gameObject.layer, changes);
            SoloRoomBuilder.BuildRooms(parent, root, SoloRoomsLayout.Rooms, checkpoints, rooms, death, observers, config, changes);
            AssignBounds(rooms, SoloRoomsLayout.Rooms, root, safety, changes);
            ObserverContext observer = observers.Get(Parallax.Core.ObserverId.A);
            if (observer?.Cat != null)
            {
                Vector2 start = root.ToWorld(new Vector2(2f, SoloRoomsLayout.FloorTop - config.ColliderBottom));
                if ((Vector2)observer.Cat.transform.position != start) { observer.Cat.transform.position = start; changes.Add("set Cat A root position"); }
            }
            if (changes.Count == 0) { Debug.Log("SoloRoomsSetup: no changes."); return; }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("SoloRoomsSetup created: " + string.Join("; ", changes));
        }

        // PAX-051 (D-066): the bodies below moved to SoloRoomBuilder; these forward to it so
        // TrapLabSetup.cs (which calls SoloRoomsSetup.* directly) and the reflection-based tests
        // that look up SoloRoomsSetup.ComputeRoomBounds keep working unchanged.
        internal static RoomSafetyConfig EnsureRoomSafetyConfig(List<string> changes) => SoloRoomBuilder.EnsureRoomSafetyConfig(RoomSafetyConfigPath, changes);
        internal static void AssignBounds(RoomManager rooms, IReadOnlyList<SoloRoomDefinition> layoutRooms, RealityRoot root, RoomSafetyConfig safety, List<string> changes) => SoloRoomBuilder.AssignBounds(rooms, layoutRooms, root, safety, changes);
        public static Bounds ComputeRoomBounds(SoloRoomDefinition room, float margin) => SoloRoomBuilder.ComputeRoomBounds(room, margin);
        internal static void Wire(Object target, string field, Object value, List<string> changes) => SoloRoomBuilder.Wire(target, field, value, changes);
        internal static void BuildRoom(Transform parent, RealityRoot root, SoloRoomDefinition room, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes) => SoloRoomBuilder.BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
    }
}
