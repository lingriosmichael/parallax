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
    /// <summary>Creates PAX-042 greybox once. Delete Traps_PAX042 before changing authored placement.</summary>
    public static class TrapKitSetup
    {
        static readonly Color FloorTan = new(.72f, .52f, .28f, 1f);
        static readonly Color SpikesRed = new(.85f, .12f, .10f, 1f);
        static readonly Color BlockGrey = new(.20f, .20f, .23f, 1f);
        static readonly Color FlipPurple = new(.55f, .22f, .75f, .38f);

        [MenuItem("PARALLAX/Setup/Trap Kit (PAX-042)")]
        public static void Configure()
        {
            var changes = new List<string>();
            RealityRoot root = GameObject.Find("RealityRoot_A")?.GetComponent<RealityRoot>();
            RoomManager rooms = Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include);
            RoomDeath death = Object.FindAnyObjectByType<RoomDeath>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            BoxCollider2D ground = root?.transform.Find("Geometry/Ground")?.GetComponent<BoxCollider2D>();
            if (root == null || rooms == null || death == null || observers == null || ground == null)
            {
                Debug.LogError("TrapKitSetup: RealityRoot_A, RoomManager, RoomDeath, ObserverSet and Geometry/Ground are required.");
                return;
            }

            float groundTop = ground.bounds.max.y;
            Transform parent = SetupUtility.EnsureChild(root.transform, "Traps_PAX042", root.gameObject.layer, changes);
            BuildCollapsingFloor(parent, root, rooms, death, observers, groundTop, changes);
            BuildGravityFlip(parent, root, rooms, death, observers, groundTop, changes);
            BuildHiddenSpikes(parent, root, rooms, death, observers, groundTop, changes);
            BuildDoorRetreat(parent, root, rooms, death, observers, groundTop, changes);
            BuildFallingBlock(parent, root, rooms, death, observers, groundTop, changes);

            if (changes.Count == 0) return;
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("TrapKitSetup: " + string.Join("; ", changes));
        }

        static void BuildCollapsingFloor(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildCollapsingFloorCore(parent, root, "CollapsingFloor", new(-8f, top + .85f), new(1.5f, .3f), FloorTan, 0, rooms, death, observers, 12, null, changes);

        internal static CollapsingFloorTrap BuildCollapsingFloorCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, int delayTicks, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<CollapsingFloorTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            CollapsingFloorTrap trap = go.GetComponent<CollapsingFloorTrap>();
            Write(trap, changes, ("delayTicks", delayTicks));
            return trap;
        }

        // PAX-080 (D-080): a fake platform is a CollapsingFloorTrap whose body is a trigger, so it never holds the
        // cat up; with delay 0 it vanishes on the first tick the cat touches it (the collapse's touchSkin). The
        // builder forces Overlap, Once, delay 0; ValidateFakePlatformSettings keeps the data from disagreeing.
        internal static readonly SoloRoomTrapSettings FakePlatformSettings = new(delayTicks: 0);

        internal static CollapsingFloorTrap BuildFakePlatformCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<CollapsingFloorTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            if (!box.isTrigger) { box.isTrigger = true; changes.Add("set " + name + ".isTrigger"); }
            CollapsingFloorTrap trap = go.GetComponent<CollapsingFloorTrap>();
            Write(trap, changes, ("delayTicks", 0));
            return trap;
        }

        static void BuildGravityFlip(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildGravityFlipCore(parent, root, "GravityFlip", new(-4f, top + .75f), new(1f, 1.5f), FlipPurple, 0, rooms, death, observers, GravityFlipMode.Flip, 0, false, null, changes);

        internal static GravityFlipTrap BuildGravityFlipCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, GravityFlipMode mode, int delayTicks, bool rearmOnExit, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<GravityFlipTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            GravityFlipTrap trap = go.GetComponent<GravityFlipTrap>();
            Write(trap, changes, ("mode", (int)mode), ("delayTicks", delayTicks), ("rearmOnExit", rearmOnExit));
            return trap;
        }

        static void BuildHiddenSpikes(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildHiddenSpikesCore(parent, root, "HiddenSpikes", new(-.5f, top + .15f), new(.5f, .3f), SpikesRed, 0, rooms, death, observers, "Trigger", new(-.6f, 0f), new(.7f, .6f), 0, null, changes);

        internal static HiddenSpikesTrap BuildHiddenSpikesCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, string triggerName, Vector2 triggerLocalPosition, Vector2 triggerSize, int revealDelayTicks, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<HiddenSpikesTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, true, sortingOrder, changes);
            Hazard hazard = SetupUtility.Ensure<Hazard>(go, changes);
            Write(hazard, changes, ("observers", (Object)observers), ("roomDeath", death), ("rooms", rooms), ("armed", false));
            BoxCollider2D trigger = CreateTrigger(go.transform, root, triggerName, triggerLocalPosition, triggerSize, changes);
            HiddenSpikesTrap trap = go.GetComponent<HiddenSpikesTrap>();
            Write(trap, changes, ("hazard", hazard), ("trigger", trigger), ("revealDelayTicks", revealDelayTicks));
            return trap;
        }

        static void BuildDoorRetreat(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildDoorRetreatCore(parent, root, "DoorRetreat", new(2.25f, top + .75f), new(.7f, 1.5f), 0, rooms, death, observers, GameObject.Find("Door_0")?.transform, new Vector2(2f, 0f), 10, 0, changes);

        internal static DoorRetreatTrap BuildDoorRetreatCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, Transform doorRoot, Vector2 offset, int moveTicks, int delayTicks, List<string> changes)
        {
            GameObject go = CreateTrap<DoorRetreatTrap>(parent, root, name, position, size, Color.clear, roomId, rooms, death, observers, true, null, changes);
            DoorRetreatTrap trap = go.GetComponent<DoorRetreatTrap>();
            Write(trap, changes, ("trigger", go.GetComponent<BoxCollider2D>()), ("doorRoot", doorRoot), ("offset", offset), ("moveTicks", moveTicks), ("delayTicks", delayTicks));
            return trap;
        }

        static void BuildFallingBlock(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildFallingBlockCore(parent, root, "FallingBlock", new(8f, top + 3f), new(.8f, .8f), BlockGrey, 1, rooms, death, observers, "Trigger", new(0f, -2.25f), new(1.6f, 1.5f), FallingBlockDirection.Down, 0, .3f, 2.6f, null, changes);

        internal static FallingBlockTrap BuildFallingBlockCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, string triggerName, Vector2 triggerLocalPosition, Vector2 triggerSize, FallingBlockDirection direction, int delayTicks, float unitsPerTick, float travelDistance, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<FallingBlockTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes);
            SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D block = go.GetComponent<BoxCollider2D>();
            block.isTrigger = false;
            BoxCollider2D trigger = CreateTrigger(go.transform, root, triggerName, triggerLocalPosition, triggerSize, changes);
            FallingBlockTrap trap = go.GetComponent<FallingBlockTrap>();
            Write(trap, changes, ("trigger", trigger), ("direction", (int)direction), ("delayTicks", delayTicks), ("unitsPerTick", unitsPerTick), ("travelDistance", travelDistance));
            return trap;
        }

        internal static GameObject CreateTrap<T>(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, bool trigger, int? sortingOrder, List<string> changes) where T : Component
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            GameObject go = transform.gameObject;
            bool newTrap = go.GetComponent<T>() == null;
            if (newTrap)
            {
                SetupUtility.SetLocalPosition(transform, position, changes);
                if (color != Color.clear)
                {
                    SpriteRenderer visual = SetupUtility.SetVisual(go, root, size, color, changes);
                    if (sortingOrder.HasValue && visual.sortingOrder != sortingOrder.Value) { visual.sortingOrder = sortingOrder.Value; changes.Add("set " + go.name + ".sortingOrder"); }
                }
                BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(go, changes);
                SetupUtility.SetColliderSize(box, size, changes);
                box.isTrigger = trigger;
            }
            else if (go.GetComponent<BoxCollider2D>() == null)
            {
                SetupUtility.Ensure<BoxCollider2D>(go, changes).isTrigger = trigger;
            }
            T trap = SetupUtility.Ensure<T>(go, changes);
            Write(trap, changes, ("rooms", rooms), ("roomDeath", death), ("observers", observers), ("roomId", roomId));
            return go;
        }

        internal static BoxCollider2D CreateTrigger(Transform parent, RealityRoot root, string name, Vector2 localPosition, Vector2 size, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            if (transform.GetComponent<BoxCollider2D>() == null)
            {
                SetupUtility.SetLocalPosition(transform, localPosition, changes);
                BoxCollider2D box = SetupUtility.Ensure<BoxCollider2D>(transform.gameObject, changes);
                SetupUtility.SetColliderSize(box, size, changes);
                box.isTrigger = true;
            }
            return transform.GetComponent<BoxCollider2D>();
        }

        internal static MovingTrap BuildMovingTrapCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, Vector2 triggerLocalPosition, Vector2 triggerSize, SoloRoomTrapSettings settings, CrushConfig crushConfig, int? sortingOrder, List<string> changes)
        {
            GameObject go = CreateTrap<MovingTrap>(parent, root, name, position, size, color, roomId, rooms, death, observers, false, sortingOrder, changes);
            BoxCollider2D bodyBox = go.GetComponent<BoxCollider2D>();
            bool shouldBeTrigger = settings.MovingKind == MovingTrapKind.Hazard;
            if (bodyBox.isTrigger != shouldBeTrigger) { bodyBox.isTrigger = shouldBeTrigger; changes.Add("set " + name + ".isTrigger"); }
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes); SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D trigger = settings.TriggerSource == TrapTriggerSource.Overlap ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            MovingTrap trap = go.GetComponent<MovingTrap>();
            Write(trap, changes, ("trigger", trigger), ("kind", (int)settings.MovingKind), ("offset", settings.Offset), ("moveTicks", settings.MoveTicks), ("holdTicks", settings.HoldTicks), ("returnTicks", settings.ReturnTicks), ("crushDepth", settings.CrushDepth), ("crushConfig", crushConfig));
            return trap;
        }

        // PAX-085 (D-087): the inverter. Its own body is a trigger box (Overlap on its own body), or a separate child box
        // (the element's secondary box), or neither matters (Chain). Honest: a cyan orb on the body; disguised: no body
        // visual (an invisible trigger, D-056 (4)). Two cue children, off until it fires: Cue_Ring (behind the cat) and
        // Cue_Mark (over it); InverterTrap moves them onto the cat while they're on. Placeholder art.
        static readonly Color InverterCyan = new(.20f, .85f, .90f, 1f);
        static readonly Color CueRingColor = new(.20f, .85f, .90f, .45f);
        static readonly Color CueMarkColor = new(.20f, .85f, .90f, 1f);
        internal static readonly Vector2 CueRingSize = new(1.5f, 1.1f), CueMarkSize = new(.7f, .2f);
        internal const int CueRingSortingOrder = -1, CueMarkSortingOrder = 1;

        internal static InverterTrap BuildInverterCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            bool disguised = settings.Inverter.Disguised;
            GameObject go = CreateTrap<InverterTrap>(parent, root, name, position, size, disguised ? Color.clear : InverterCyan, roomId, rooms, death, observers, true, -1, changes);
            BoxCollider2D trigger = triggerSize != Vector2.zero ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : go.GetComponent<BoxCollider2D>();
            SpriteRenderer ring = BuildCue(go.transform, root, "Cue_Ring", CueRingSize, CueRingColor, CueRingSortingOrder, changes);
            SpriteRenderer mark = BuildCue(go.transform, root, "Cue_Mark", CueMarkSize, CueMarkColor, CueMarkSortingOrder, changes);
            InverterTrap trap = go.GetComponent<InverterTrap>();
            Write(trap, changes, ("trigger", trigger), ("orb", disguised ? null : go.GetComponent<SpriteRenderer>()), ("ring", ring), ("mark", mark),
                ("durationTicks", settings.Inverter.Duration), ("delayTicks", settings.DelayTicks));
            return trap;
        }

        // PAX-086 (D-088): the geyser. Its body is the vent (a trigger: the host Floor or Ceiling holds the cat), drawn over the
        // host in VentGrey; it turns the trap's tell colour through the tell and the eruption. Its child "Column" (the column's
        // box, from the vent's face along the push) shows only while erupting. Placeholder art.
        static readonly Color VentGrey = new(.30f, .30f, .34f, 1f);
        static readonly Color ColumnColor = new(.75f, .92f, 1f, .45f);
        internal const int VentSortingOrder = -1, ColumnSortingOrder = -1;

        internal static GeyserTrap BuildGeyserCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, GeyserSettings geyser, List<string> changes)
        {
            GeyserSettings g = geyser.Resolved;
            GameObject go = CreateTrap<GeyserTrap>(parent, root, name, position, size, VentGrey, roomId, rooms, death, observers, true, VentSortingOrder, changes);
            Vector2 push = GeyserMath.Push(g.Direction);
            Transform columnTransform = SetupUtility.EnsureChild(go.transform, "Column", root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(columnTransform, push * (size.y * .5f + g.ColumnHeight * .5f), changes);
            SpriteRenderer column = SetupUtility.SetVisual(columnTransform.gameObject, root, new Vector2(g.ColumnWidth, g.ColumnHeight), ColumnColor, changes);
            if (column != null)
            {
                if (column.sortingOrder != ColumnSortingOrder) { column.sortingOrder = ColumnSortingOrder; changes.Add("set " + name + ".Column.sortingOrder"); }
                if (column.enabled) { column.enabled = false; changes.Add("hid " + name + ".Column"); }
            }
            GeyserTrap trap = go.GetComponent<GeyserTrap>();
            Write(trap, changes, ("vent", go.GetComponent<SpriteRenderer>()), ("column", column), ("direction", (int)g.Direction),
                ("columnWidth", g.ColumnWidth), ("columnHeight", g.ColumnHeight), ("tellTicks", g.TellTicks), ("eruptTicks", g.EruptTicks), ("launchSpeed", g.LaunchSpeed));
            return trap;
        }

        // PAX-087 (D-089) R3: a climbable vine. Its body is a trigger box, the grab box (width 0.6); no renderer of its own.
        // Its look is the vine sprite stacked in Simple children "Segment_i", each scaled to the box's width, the top one
        // squashed so the vine ends exactly at its top. Not Tiled: the sprite's mesh is Tight and Tiled needs Full Rect
        // (the .meta is the art ticket's). A snap vine (configured settings) gets an Overlap trigger child from the
        // secondary box when it has one; with none it triggers on its own box.
        internal const string VineSpritePath = "Assets/_Game/Art/RealityA/Environment/A_OBJ_Vine.png";
        internal const int VineSortingOrder = -1;
        static readonly Color VinePlaceholder = new(.25f, .55f, .2f, 1f);

        internal static ClimbVine BuildClimbVineCore(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, SoloRoomTrapSettings settings, Vector2 triggerLocalPosition, Vector2 triggerSize, List<string> changes)
        {
            GameObject go = CreateTrap<ClimbVine>(parent, root, name, position, size, Color.clear, roomId, rooms, death, observers, true, null, changes);
            BoxCollider2D box = go.GetComponent<BoxCollider2D>();
            SetupUtility.SetColliderSize(box, size, changes);
            if (!box.isTrigger) { box.isTrigger = true; changes.Add("set " + name + ".isTrigger"); }
            SpriteRenderer[] segments = BuildVineSegments(go.transform, root, size, changes);
            bool snaps = settings.IsConfigured;
            BoxCollider2D trigger = snaps && settings.TriggerSource == TrapTriggerSource.Overlap && triggerSize != Vector2.zero
                ? CreateTrigger(go.transform, root, settings.TriggerName, triggerLocalPosition, triggerSize, changes) : null;
            ClimbVine vine = go.GetComponent<ClimbVine>();
            Write(vine, changes, ("snaps", snaps), ("trigger", trigger), ("delayTicks", snaps ? settings.DelayTicks : 0));
            SerializedObject serialized = new(vine);
            SerializedProperty list = serialized.FindProperty("segments");
            bool same = list.arraySize == segments.Length;
            for (int i = 0; same && i < segments.Length; i++) same = list.GetArrayElementAtIndex(i).objectReferenceValue == segments[i];
            if (!same)
            {
                list.arraySize = segments.Length;
                for (int i = 0; i < segments.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(vine);
                changes.Add("wired " + name + ".segments");
            }
            return vine;
        }

        static SpriteRenderer[] BuildVineSegments(Transform vine, RealityRoot root, Vector2 size, List<string> changes)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VineSpritePath);
            if (sprite == null) Debug.LogError($"TrapKitSetup: no vine sprite at '{VineSpritePath}'; '{vine.name}' uses a placeholder colour.", vine);
            // Placeholder: one greybox segment the size of the box.
            Vector2 natural = sprite != null ? (Vector2)sprite.bounds.size : size;
            float scale = size.x / natural.x;
            float segment = natural.y * scale;
            int count = sprite != null ? Mathf.Max(1, Mathf.CeilToInt(size.y / segment - 1e-4f)) : 1;
            var renderers = new SpriteRenderer[count];
            float bottom = -size.y * .5f;
            for (int i = 0; i < count; i++)
            {
                float height = i < count - 1 ? segment : size.y - segment * (count - 1);
                Transform t = SetupUtility.EnsureChild(vine, "Segment_" + i, root.gameObject.layer, changes);
                SetupUtility.SetLocalPosition(t, new Vector2(0f, bottom + segment * i + height * .5f), changes);
                SpriteRenderer r;
                if (sprite == null) r = SetupUtility.SetVisual(t.gameObject, root, size, VinePlaceholder, changes);
                else
                {
                    Vector3 local = new(scale, scale * height / segment, 1f);
                    if (t.localScale != local) { t.localScale = local; changes.Add("set " + t.name + ".localScale"); }
                    r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                    if (r.sprite != sprite) { r.sprite = sprite; changes.Add("set " + t.name + ".sprite"); }
                    if (r.drawMode != SpriteDrawMode.Simple) { r.drawMode = SpriteDrawMode.Simple; changes.Add("set " + t.name + ".drawMode"); }
                    string layer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
                    if (r.sortingLayerName != layer) { r.sortingLayerName = layer; changes.Add("set " + t.name + ".sortingLayer"); }
                }
                if (r != null && r.sortingOrder != VineSortingOrder) { r.sortingOrder = VineSortingOrder; changes.Add("set " + t.name + ".sortingOrder"); }
                if (r != null && !r.enabled) { r.enabled = true; changes.Add("showed " + t.name); }
                renderers[i] = r;
            }
            // A shorter vine than last time: drop the segments it no longer has.
            for (int i = count; vine.Find("Segment_" + i) != null; i++)
            { Object.DestroyImmediate(vine.Find("Segment_" + i).gameObject); changes.Add("removed " + vine.name + ".Segment_" + i); }
            return renderers;
        }

        static SpriteRenderer BuildCue(Transform parent, RealityRoot root, string name, Vector2 size, Color color, int sortingOrder, List<string> changes)
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(transform, Vector2.zero, changes);
            SpriteRenderer visual = SetupUtility.SetVisual(transform.gameObject, root, size, color, changes);
            if (visual == null) return null;
            if (visual.sortingOrder != sortingOrder) { visual.sortingOrder = sortingOrder; changes.Add("set " + name + ".sortingOrder"); }
            if (visual.enabled) { visual.enabled = false; changes.Add("hid " + parent.name + "." + name); }
            return visual;
        }

        internal static void ConfigureTiming(RoomTrap trap, SoloRoomTrapSettings settings, Transform roomRoot, List<string> changes)
        {
            RoomTrap source = null;
            if (settings.TriggerSource == TrapTriggerSource.Chain)
                foreach (RoomTrap candidate in roomRoot.GetComponentsInChildren<RoomTrap>(true)) if (candidate.name == settings.ChainSource) { source = candidate; break; }
            Write(trap, changes, ("triggerSource", (int)settings.TriggerSource), ("chainSource", source), ("repeatMode", (int)settings.RepeatMode), ("cooldownTicks", settings.CooldownTicks), ("periodTicks", settings.PeriodTicks), ("phaseTicks", settings.PhaseTicks));
            if (settings.TriggerSource == TrapTriggerSource.Overlap && settings.RepeatMode != TrapRepeatMode.Periodic) return;
            Transform trigger = trap.transform.Find(settings.TriggerName);
            if (trigger != null) { Object.DestroyImmediate(trigger.gameObject); changes.Add("removed " + trap.name + "." + settings.TriggerName); }
            else if (trap is DoorRetreatTrap)
            {
                BoxCollider2D box = trap.GetComponent<BoxCollider2D>();
                if (box != null) { Object.DestroyImmediate(box); changes.Add("removed " + trap.name + ".trigger"); }
            }
        }

        internal static void Write(Object target, List<string> changes, params (string name, object value)[] values)
        {
            SerializedObject serialized = new(target);
            bool changed = false;
            foreach ((string name, object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property == null) continue;
                switch (value)
                {
                    case Object reference when property.objectReferenceValue != reference: property.objectReferenceValue = reference; changed = true; break;
                    case int integer when property.intValue != integer: property.intValue = integer; changed = true; break;
                    case float number when !Mathf.Approximately(property.floatValue, number): property.floatValue = number; changed = true; break;
                    case bool boolean when property.boolValue != boolean: property.boolValue = boolean; changed = true; break;
                    case Vector2 vector when property.vector2Value != vector: property.vector2Value = vector; changed = true; break;
                }
            }
            if (!changed) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            changes.Add("configured " + target.name);
        }
    }
}
