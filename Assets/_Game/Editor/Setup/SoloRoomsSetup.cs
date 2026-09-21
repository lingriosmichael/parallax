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
        static readonly Color Ground = new(.72f, .52f, .28f, 1f);
        static readonly Color Red = new(.85f, .12f, .10f, 1f);
        static readonly Color Purple = new(.55f, .22f, .75f, .38f);

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
            Transform parent = SetupUtility.EnsureChild(root.transform, "Rooms_PAX043", root.gameObject.layer, changes);
            foreach (SoloRoomDefinition room in SoloRoomsLayout.Rooms) if (parent.Find($"Room_{room.Id + 1}") == null) BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
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

        static void BuildRoom(Transform parent, RealityRoot root, SoloRoomDefinition room, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            Transform roomRoot = SetupUtility.EnsureChild(parent, $"Room_{room.Id + 1}", root.gameObject.layer, changes);
            foreach (SoloRoomElement element in room.Elements) BuildElement(roomRoot, root, room, element, checkpoints, rooms, death, observers, config, changes);
            if (room.Id == 3)
            {
                BuildGeometry(roomRoot, root, "Ceiling_Left", room.Origin + new Vector2(5.5f, 7.5f), new Vector2(11f, 1f), changes);
                BuildGeometry(roomRoot, root, "Ceiling_Right", room.Origin + new Vector2(19f, 7.5f), new Vector2(10f, 1f), changes);
            }
            else BuildGeometry(roomRoot, root, "Ceiling", room.Origin + new Vector2(12f, 7.5f), new Vector2(24f, 1f), changes);
            BuildGeometry(roomRoot, root, "Wall_Left", room.Origin + new Vector2(-.5f, 2f), new Vector2(1f, 12f), changes);
            BuildGeometry(roomRoot, root, "Wall_Right", room.Origin + new Vector2(24.5f, 2f), new Vector2(1f, 12f), changes);
        }

        static void BuildElement(Transform parent, RealityRoot root, SoloRoomDefinition room, SoloRoomElement e, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            Vector2 position = room.Origin + e.Position;
            switch (e.Kind)
            {
                case SoloRoomElementKind.Floor: case SoloRoomElementKind.PitBottom: BuildGeometry(parent, root, e.Name, position, e.Size, changes); break;
                case SoloRoomElementKind.Checkpoint: CheckpointSetup.BuildMarkerCore(parent, root, e.Name, checkpoints, observers, room.Id, position + Vector2.up * -config.ColliderBottom, Vector2.down, changes); break;
                case SoloRoomElementKind.Door: RoomSetup.BuildDoorCore(parent, root, e.Name, rooms, room.Id, position, e.Size, new Color(.9f,.5f,1f,1f), -2, changes); break;
                case SoloRoomElementKind.Hazard: HazardSetup.BuildHazardCore(parent, root, e.Name, death, observers, rooms, position, e.Size, Red, -2, changes); break;
                case SoloRoomElementKind.CollapsingFloor: TrapKitSetup.BuildCollapsingFloorCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, 12, -2, changes); break;
                case SoloRoomElementKind.HiddenSpikes: TrapKitSetup.BuildHiddenSpikesCore(parent, root, e.Name, position, e.Size, Red, room.Id, rooms, death, observers, "Trigger", e.SecondaryPosition - e.Position, e.SecondarySize, 0, -2, changes); break;
                case SoloRoomElementKind.FallingBlock: TrapKitSetup.BuildFallingBlockCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, "Trigger", e.SecondaryPosition - e.Position, e.SecondarySize, FallingBlockDirection.Down, 3, .3f, 5.5f, -2, changes); break;
                case SoloRoomElementKind.GravityFlip: TrapKitSetup.BuildGravityFlipCore(parent, root, e.Name, position, e.Size, e.Name == "FlipTool" ? Purple : Color.clear, room.Id, rooms, death, observers, e.Name == "HiddenFlip" ? GravityFlipMode.ForceUp : GravityFlipMode.Flip, 0, e.Name == "FlipTool", e.Name == "FlipTool" ? -2 : null, changes); break;
                case SoloRoomElementKind.DoorRetreat: TrapKitSetup.BuildDoorRetreatCore(parent, root, e.Name, position, e.Size, room.Id, rooms, death, observers, parent.Find($"Door_{room.Id}") ?? parent.Find($"Door_{room.Id + 1}"), room.Id == 3 ? new Vector2(0f,5.5f) : new Vector2(10f,0f), 10, 0, changes); break;
            }
        }

        static void BuildGeometry(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(transform, position, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(transform.gameObject, root, size, Ground, changes);
            if (visual.sortingOrder != -2) { visual.sortingOrder = -2; changes.Add("set " + name + ".sortingOrder"); }
            BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(transform.gameObject, changes);
            box.isTrigger = false;
            SetupUtility.SetColliderSize(box, size, changes);
        }
    }
}
