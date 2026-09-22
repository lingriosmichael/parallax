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

        internal static void BuildRoom(Transform parent, RealityRoot root, SoloRoomDefinition room, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            if (!TrapLayoutValidator.TryValidate(room, out string error)) { Debug.LogError("SoloRoomsSetup: " + error); return; }
            Transform roomRoot = SetupUtility.EnsureChild(parent, $"Room_{room.Id + 1}", root.gameObject.layer, changes);
            foreach (SoloRoomElement element in room.Elements) BuildElement(roomRoot, root, room, element, checkpoints, rooms, death, observers, config, changes);
            BuildGeometry(roomRoot, root, "Wall_Left", room.Origin + new Vector2(-.5f, 2f), new Vector2(1f, 12f), changes);
            BuildGeometry(roomRoot, root, "Wall_Right", room.Origin + new Vector2(room.Width + .5f, 2f), new Vector2(1f, 12f), changes);
        }

        static void BuildElement(Transform parent, RealityRoot root, SoloRoomDefinition room, SoloRoomElement e, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            Vector2 position = room.Origin + e.Position;
            switch (e.Kind)
            {
                case SoloRoomElementKind.Floor: case SoloRoomElementKind.Ceiling: case SoloRoomElementKind.Wall: case SoloRoomElementKind.PitBottom: BuildGeometry(parent, root, e.Name, position, e.Size, changes); break;
                case SoloRoomElementKind.Checkpoint: CheckpointSetup.BuildMarkerCore(parent, root, e.Name, checkpoints, observers, room.Id, position + Vector2.up * -config.ColliderBottom, Vector2.down, changes); break;
                case SoloRoomElementKind.Door: RoomSetup.BuildDoorCore(parent, root, e.Name, rooms, room.Id, position, e.Size, new Color(.9f,.5f,1f,1f), -2, changes); break;
                case SoloRoomElementKind.Hazard: HazardSetup.BuildHazardCore(parent, root, e.Name, death, observers, rooms, position, e.Size, Red, -2, changes); break;
                case SoloRoomElementKind.CollapsingFloor: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildCollapsingFloorCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, e.Settings.DelayTicks, -2, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.HiddenSpikes: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildHiddenSpikesCore(parent, root, e.Name, position, e.Size, Red, room.Id, rooms, death, observers, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings.RevealDelayTicks, -2, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.FallingBlock: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildFallingBlockCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings.Direction, e.Settings.DelayTicks, e.Settings.UnitsPerTick, e.Settings.TravelDistance, -2, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.GravityFlip: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildGravityFlipCore(parent, root, e.Name, position, e.Size, e.Settings.RendererEnabled ? Purple : Color.clear, room.Id, rooms, death, observers, e.Settings.GravityMode, e.Settings.DelayTicks, e.Settings.RearmOnExit, e.Settings.RendererEnabled ? -2 : null, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.DoorRetreat: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildDoorRetreatCore(parent, root, e.Name, position, e.Size, room.Id, rooms, death, observers, parent.GetComponentInChildren<RoomDoor>(true)?.transform, e.Settings.Offset, e.Settings.MoveTicks, e.Settings.DelayTicks, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.MovingTrap: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildMovingTrapCore(parent, root, e.Name, position, e.Size, e.Settings.MovingKind == MovingTrapKind.Hazard ? Red : Ground, room.Id, rooms, death, observers, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings, AssetDatabase.LoadAssetAtPath<CrushConfig>("Assets/_Game/Data/CrushConfig_Default.asset"), -2, changes), e.Settings, parent, changes); break;
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
