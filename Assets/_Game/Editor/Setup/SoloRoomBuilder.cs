using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // PAX-051 (D-066): the room-building part of SoloRoomsSetup.Configure() extracted so any
    // scene (SoloRoomsSetup, TrapLabSetup, the new per-level menus) can build a room set from a
    // list of SoloRoomDefinitions. Behaviour is unchanged from the pre-PAX-051 SoloRoomsSetup:
    // SoloRoomsSetup and TrapLabSetup now forward to these methods instead of owning the bodies.
    public static class SoloRoomBuilder
    {
        static readonly Color Ground = new(.72f, .52f, .28f, 1f);
        static readonly Color Red = new(.85f, .12f, .10f, 1f);
        static readonly Color Purple = new(.55f, .22f, .75f, .38f);
        // PAX-074 (D-078): an honest arrow launcher (the trap kit's block grey) and the arrow itself.
        static readonly Color LauncherGrey = new(.20f, .20f, .23f, 1f);
        // Every fixed geometry element is built with this colour; a disguised launcher copies it from its host.
        static Color GeometryColor => Ground;
        // PAX-059 (D-085, no tells): collapsing floors and fake platforms draw over the geometry and pit hazards (-2) they
        // fill, so a trap floor that fills its shaft hides the shaft until it gives way.
        const int TrapFloorSortingOrder = -1;

        public static RoomSafetyConfig EnsureRoomSafetyConfig(string assetPath, List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<RoomSafetyConfig>(assetPath);
            if (config != null) return config;
            var created = ScriptableObject.CreateInstance<RoomSafetyConfig>();
            AssetDatabase.CreateAsset(created, assetPath);
            changes.Add("created RoomSafetyConfig asset");
            return created;
        }

        public static void BuildRooms(Transform parent, RealityRoot root, IReadOnlyList<SoloRoomDefinition> layoutRooms, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            foreach (SoloRoomDefinition room in layoutRooms) if (parent.Find($"Room_{room.Id + 1}") == null) BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
        }

        public static void AssignBounds(RoomManager rooms, IReadOnlyList<SoloRoomDefinition> layoutRooms, RealityRoot root, RoomSafetyConfig safety, List<string> changes)
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

        public static void ClearBounds(RoomManager rooms, List<string> changes)
        {
            var serialized = new SerializedObject(rooms);
            SerializedProperty array = serialized.FindProperty("bounds");
            if (array.arraySize == 0) return;
            array.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("cleared " + rooms.name + ".bounds");
        }

        public static void Wire(Object target, string field, Object value, List<string> changes)
        {
            if (target == null || value == null) return;
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue == value) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("wired " + target.name + "." + field);
        }

        public static void BuildRoom(Transform parent, RealityRoot root, SoloRoomDefinition room, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            if (!TrapLayoutValidator.TryValidate(room, out string error)) { Debug.LogError("SoloRoomBuilder: " + error); return; }
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
                case SoloRoomElementKind.CollapsingFloor: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildCollapsingFloorCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, e.Settings.DelayTicks, TrapFloorSortingOrder, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.HiddenSpikes: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildHiddenSpikesCore(parent, root, e.Name, position, e.Size, Red, room.Id, rooms, death, observers, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings.RevealDelayTicks, -2, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.FallingBlock: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildFallingBlockCore(parent, root, e.Name, position, e.Size, Ground, room.Id, rooms, death, observers, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings.Direction, e.Settings.DelayTicks, e.Settings.UnitsPerTick, e.Settings.TravelDistance, -2, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.GravityFlip: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildGravityFlipCore(parent, root, e.Name, position, e.Size, e.Settings.RendererEnabled ? Purple : Color.clear, room.Id, rooms, death, observers, e.Settings.GravityMode, e.Settings.DelayTicks, e.Settings.RearmOnExit, e.Settings.RendererEnabled ? -2 : null, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.DoorRetreat: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildDoorRetreatCore(parent, root, e.Name, position, e.Size, room.Id, rooms, death, observers, parent.GetComponentInChildren<RoomDoor>(true)?.transform, e.Settings.Offset, e.Settings.MoveTicks, e.Settings.DelayTicks, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.FakePlatform: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildFakePlatformCore(parent, root, e.Name, position, e.Size, GeometryColor, room.Id, rooms, death, observers, TrapFloorSortingOrder, changes), TrapKitSetup.FakePlatformSettings, parent, changes); break;
                case SoloRoomElementKind.Arrow: TrapKitSetup.ConfigureTiming(BuildArrow(parent, root, room, e, rooms, death, observers, changes), e.Settings, parent, changes); break;
                case SoloRoomElementKind.MovingTrap: TrapKitSetup.ConfigureTiming(TrapKitSetup.BuildMovingTrapCore(parent, root, e.Name, position, e.Size, e.Settings.MovingKind == MovingTrapKind.Hazard ? Red : Ground, room.Id, rooms, death, observers, e.SecondaryPosition - e.Position, e.SecondarySize, e.Settings, AssetDatabase.LoadAssetAtPath<CrushConfig>("Assets/_Game/Data/CrushConfig_Default.asset"), -2, changes), e.Settings, parent, changes); break;
            }
        }

        // PAX-074 (D-078): the launcher (sprite, no collider, ArrowTrap), its child "Arrow" (sprite only,
        // hidden until the fire) and, for a non-Periodic Overlap arrow only, the child trigger box. The
        // launcher never has a collider of its own, so it can never act as a trigger.
        // PAX-084 (D-086): a spear's child is "<name>_Shaft" (the route harness reports ground by object name) and
        // carries the shaft: a non-trigger box the size of the visible shaft, off until the spear sticks.
        static ArrowTrap BuildArrow(Transform parent, RealityRoot root, SoloRoomDefinition room, SoloRoomElement e, RoomManager rooms, RoomDeath death, ObserverSet observers, List<string> changes)
        {
            ArrowLane lane = e.Settings.Arrow;
            Transform launcher = SetupUtility.EnsureChild(parent, e.Name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(launcher, room.Origin + e.Position, changes);
            SpriteRenderer launcherVisual = SetupUtility.SetVisual(launcher.gameObject, root, e.Size, lane.Disguised ? HostColor(room, e) : LauncherGrey, changes);
            SetSortingOrder(launcherVisual, -1, changes);

            float sign = ArrowMath.Sign(lane.Direction);
            Vector2 mouth = new(sign * e.Size.x * .5f, lane.LaneY - e.Position.y);
            Transform arrow = SetupUtility.EnsureChild(launcher, lane.Spear ? e.Name + "_Shaft" : "Arrow", root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(arrow, mouth + new Vector2(sign * lane.Length * .5f, 0f), changes);
            SpriteRenderer arrowVisual = SetupUtility.SetVisual(arrow.gameObject, root, new Vector2(lane.Length, lane.Thickness), Red, changes);
            SetSortingOrder(arrowVisual, 0, changes);
            BoxCollider2D shaft = lane.Spear ? BuildShaft(arrow, new Vector2(lane.Length, lane.Thickness), changes) : null;

            bool overlap = e.Settings.TriggerSource == TrapTriggerSource.Overlap && e.Settings.RepeatMode != TrapRepeatMode.Periodic;
            BoxCollider2D trigger = overlap ? TrapKitSetup.CreateTrigger(launcher, root, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, changes) : null;

            ArrowTrap trap = SetupUtility.Ensure<ArrowTrap>(launcher.gameObject, changes);
            TrapKitSetup.Write(trap, changes, ("rooms", rooms), ("roomDeath", death), ("observers", observers), ("roomId", room.Id), ("trigger", trigger),
                ("launcher", launcherVisual), ("arrow", arrowVisual), ("direction", (int)lane.Direction), ("mouth", mouth),
                ("travel", LevelLayoutValidator.ArrowTravel(e)), ("arrowLength", lane.Length), ("arrowThickness", lane.Thickness),
                ("unitsPerTick", lane.UnitsPerTick), ("tellTicks", lane.TellTicks), ("delayTicks", e.Settings.DelayTicks),
                ("spear", lane.Spear), ("shaft", shaft));
            WriteColor(trap, "honestColor", LauncherGrey, changes);
            return trap;
        }

        static BoxCollider2D BuildShaft(Transform arrow, Vector2 size, List<string> changes)
        {
            BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(arrow.gameObject, changes);
            if (box.isTrigger) { box.isTrigger = false; changes.Add("set " + arrow.name + ".isTrigger"); }
            SetupUtility.SetColliderSize(box, size, changes);
            if (box.enabled) { box.enabled = false; changes.Add("disabled " + arrow.name + " shaft"); }
            return box;
        }

        // The colour this builder gives the fixed geometry the launcher sits in.
        static Color HostColor(SoloRoomDefinition room, SoloRoomElement launcher)
        {
            Rect box = new(launcher.Position - launcher.Size * .5f, launcher.Size);
            foreach (SoloRoomElement host in room.Elements)
            {
                bool geometry = host.Kind == SoloRoomElementKind.Floor || host.Kind == SoloRoomElementKind.Ceiling || host.Kind == SoloRoomElementKind.Wall || host.Kind == SoloRoomElementKind.PitBottom;
                if (geometry && new Rect(host.Position - host.Size * .5f, host.Size).Overlaps(box)) return GeometryColor;
            }
            Debug.LogError($"SoloRoomBuilder: disguised arrow '{launcher.Name}' has no fixed geometry host; using the geometry colour.");
            return GeometryColor;
        }

        static void SetSortingOrder(SpriteRenderer visual, int order, List<string> changes)
        {
            if (visual == null || visual.sortingOrder == order) return;
            visual.sortingOrder = order;
            changes.Add("set " + visual.name + ".sortingOrder");
        }

        static void WriteColor(Object target, string field, Color value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null || property.colorValue == value) return;
            property.colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("configured " + target.name + "." + field);
        }

        static void BuildGeometry(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(transform, position, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(transform.gameObject, root, size, GeometryColor, changes);
            if (visual.sortingOrder != -2) { visual.sortingOrder = -2; changes.Add("set " + name + ".sortingOrder"); }
            BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(transform.gameObject, changes);
            box.isTrigger = false;
            SetupUtility.SetColliderSize(box, size, changes);
        }
    }
}
