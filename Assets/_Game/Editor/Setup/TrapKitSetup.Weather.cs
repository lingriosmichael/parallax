using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // PAX-089: the storm cloud builder (PAX-088), split out of TrapKitSetup.cs unchanged.
    public static partial class TrapKitSetup
    {
        // PAX-088 (D-090): the storm cloud. Its object stays at the authored pose with no renderer of its own; its children
        // move: "Cloud" (the cloud, CloudGrey; the trap darkens it while charging), "Target" (the faint charge line) and
        // "Bolt" (the strike), both hidden until the trap shows them. The wake trigger is the secondary box (Overlap). The
        // strike profile is baked from the room's static elements (LevelLayoutValidator.StrikeProfile), as offsets from the
        // authored pose. Placeholder art.
        static readonly Color CloudGrey = new(.78f, .80f, .86f, 1f), TargetColor = new(1f, .95f, .55f, .35f), BoltColor = new(1f, .95f, .55f, 1f);
        internal const int CloudSortingOrder = 1;

        internal static StormCloudTrap BuildStormCloudCore(Transform parent, RealityRoot root, SoloRoomDefinition room, SoloRoomElement e, RoomManager rooms, RoomDeath death, ObserverSet observers, List<string> changes)
        {
            StormCloudSettings c = e.Settings.Cloud;
            // PAX-089 E.1: no box of its own (the trap never used one); a cloud built before PAX-089 loses it here.
            GameObject go = CreateTrap<StormCloudTrap>(parent, root, e.Name, room.Origin + e.Position, e.Size, Color.clear, room.Id, rooms, death, observers, true, null, changes, box: false);
            BoxCollider2D leftover = go.GetComponent<BoxCollider2D>();
            if (leftover != null) { Object.DestroyImmediate(leftover); changes.Add("removed " + e.Name + "'s own box"); }
            SpriteRenderer cloud = BuildCloudChild(go.transform, root, "Cloud", e.Size, CloudGrey, true, changes);
            SpriteRenderer target = BuildCloudChild(go.transform, root, "Target", new Vector2(.08f, 1f), TargetColor, false, changes);
            SpriteRenderer bolt = BuildCloudChild(go.transform, root, "Bolt", new Vector2(c.StrikeWidth, 1f), BoltColor, false, changes);
            BoxCollider2D trigger = e.Settings.TriggerSource == TrapTriggerSource.Overlap
                ? CreateTrigger(go.transform, root, e.Settings.TriggerName, e.SecondaryPosition - e.Position, e.SecondarySize, changes) : null;
            StormCloudTrap trap = go.GetComponent<StormCloudTrap>();
            Write(trap, changes, ("trigger", trigger), ("cloud", cloud), ("target", target), ("bolt", bolt), ("cloudSize", e.Size),
                ("rangeMin", c.MinX - e.Position.x), ("rangeMax", c.MaxX - e.Position.x), ("followSpeed", c.FollowSpeed), ("strikeWidth", c.StrikeWidth),
                ("firstStrikeDelay", c.FirstStrikeDelay), ("strikePeriod", c.StrikePeriod), ("tellTicks", c.TellTicks), ("strikeTicks", c.StrikeTicks), ("delayTicks", e.Settings.DelayTicks));
            Vector3[] profile = LevelLayoutValidator.StrikeProfile(room);
            for (int i = 0; i < profile.Length; i++) profile[i] -= new Vector3(e.Position.x, e.Position.x, e.Position.y);
            SerializedObject serialized = new(trap);
            SerializedProperty list = serialized.FindProperty("profile");
            bool same = list.arraySize == profile.Length;
            for (int i = 0; same && i < profile.Length; i++) same = list.GetArrayElementAtIndex(i).vector3Value == profile[i];
            if (!same)
            {
                list.arraySize = profile.Length;
                for (int i = 0; i < profile.Length; i++) list.GetArrayElementAtIndex(i).vector3Value = profile[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(trap);
                changes.Add("baked " + e.Name + ".profile");
            }
            return trap;
        }

        static SpriteRenderer BuildCloudChild(Transform parent, RealityRoot root, string name, Vector2 size, Color color, bool shown, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(transform, Vector2.zero, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(transform.gameObject, root, size, color, changes);
            if (visual == null) return null;
            if (visual.sortingOrder != CloudSortingOrder) { visual.sortingOrder = CloudSortingOrder; changes.Add("set " + name + ".sortingOrder"); }
            if (visual.enabled != shown) { visual.enabled = shown; changes.Add((shown ? "showed " : "hid ") + parent.name + "." + name); }
            return visual;
        }
    }
}
