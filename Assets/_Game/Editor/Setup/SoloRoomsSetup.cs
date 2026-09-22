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
            foreach (SoloRoomDefinition room in SoloRoomsLayout.Rooms) if (parent.Find($"Room_{room.Id + 1}") == null) BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
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

        internal static RoomSafetyConfig EnsureRoomSafetyConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<RoomSafetyConfig>(RoomSafetyConfigPath);
            if (config != null) return config;
            var created = ScriptableObject.CreateInstance<RoomSafetyConfig>();
            AssetDatabase.CreateAsset(created, RoomSafetyConfigPath);
            changes.Add("created RoomSafetyConfig asset");
            return created;
        }

        internal static void AssignBounds(RoomManager rooms, IReadOnlyList<SoloRoomDefinition> layoutRooms, RealityRoot root, RoomSafetyConfig safety, List<string> changes)
        {
            var entries = new RoomBoundsEntry[layoutRooms.Count];
            for (int i = 0; i < layoutRooms.Count; i++)
            {
                SoloRoomDefinition room = layoutRooms[i];
                Bounds local = ComputeRoomBounds(room, safety.BoundsMargin);
                Vector2 worldCentre = root.ToWorld(new Vector2(local.center.x, local.center.y));
                entries[i] = new RoomBoundsEntry(room.Id, worldCentre, new Vector2(local.size.x, local.size.y));
            }
            WireBounds(rooms, entries, changes);
        }

        // PAX-047 (D-058) §5: union of every element's AABB (both poses for anything that
        // moves), plus the margin. Static kinds (Floor/Ceiling/Wall/PitBottom/Hazard/Checkpoint/
        // Door/HiddenSpikes/CollapsingFloor/GravityFlip) contribute only their authored pose —
        // HiddenSpikes/CollapsingFloor/GravityFlip never reposition themselves, they arm/disable/
        // trigger in place. MovingTrap and FallingBlock (incl. periodic) contribute a second pose
        // at their travel destination. DoorRetreat doesn't move its own element; it moves the
        // room's Door by its offset, so that combination is handled separately.
        public static Bounds ComputeRoomBounds(SoloRoomDefinition room, float margin)
        {
            bool has = false;
            Bounds result = default;

            void Include(Vector2 centre, Vector2 size)
            {
                var next = new Bounds(centre, new Vector2(Mathf.Max(size.x, 0f), Mathf.Max(size.y, 0f)));
                if (!has) { result = next; has = true; }
                else result.Encapsulate(next);
            }

            SoloRoomElement? door = null;
            SoloRoomElement? doorRetreat = null;

            foreach (SoloRoomElement e in room.Elements)
            {
                Vector2 pos = room.Origin + e.Position;
                Include(pos, e.Size);
                if (e.SecondarySize != Vector2.zero) Include(room.Origin + e.SecondaryPosition, e.SecondarySize);

                if (e.Kind == SoloRoomElementKind.MovingTrap) Include(pos + e.Settings.Offset, e.Size);
                if (e.Kind == SoloRoomElementKind.FallingBlock)
                {
                    Vector2 direction = e.Settings.Direction == FallingBlockDirection.Up ? Vector2.up : Vector2.down;
                    Include(pos + direction * e.Settings.TravelDistance, e.Size);
                }
                if (e.Kind == SoloRoomElementKind.Door) door = e;
                if (e.Kind == SoloRoomElementKind.DoorRetreat) doorRetreat = e;
            }

            if (door.HasValue && doorRetreat.HasValue)
            {
                Vector2 doorPos = room.Origin + door.Value.Position;
                Include(doorPos + doorRetreat.Value.Settings.Offset, door.Value.Size);
            }

            if (!has) return new Bounds(room.Origin, Vector3.zero);
            result.Expand(margin * 2f);
            return result;
        }

        static void WireBounds(RoomManager rooms, RoomBoundsEntry[] entries, List<string> changes)
        {
            var serialized = new SerializedObject(rooms);
            SerializedProperty array = serialized.FindProperty("bounds");
            bool changed = array.arraySize != entries.Length;
            if (!changed)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    SerializedProperty element = array.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("RoomId").intValue != entries[i].RoomId
                        || element.FindPropertyRelative("Center").vector2Value != entries[i].Center
                        || element.FindPropertyRelative("Size").vector2Value != entries[i].Size)
                    {
                        changed = true;
                        break;
                    }
                }
            }
            if (!changed) return;

            array.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("RoomId").intValue = entries[i].RoomId;
                element.FindPropertyRelative("Center").vector2Value = entries[i].Center;
                element.FindPropertyRelative("Size").vector2Value = entries[i].Size;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + rooms.name + ".bounds");
        }

        internal static void Wire(Object target, string field, Object value, List<string> changes)
        {
            if (target == null || value == null) return;
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue == value) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + field);
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
