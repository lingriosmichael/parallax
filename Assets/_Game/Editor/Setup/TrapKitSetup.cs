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
    public static partial class TrapKitSetup
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

        static void BuildGravityFlip(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildGravityFlipCore(parent, root, "GravityFlip", new(-4f, top + .75f), new(1f, 1.5f), FlipPurple, 0, rooms, death, observers, GravityFlipMode.Flip, 0, false, null, changes);

        static void BuildHiddenSpikes(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildHiddenSpikesCore(parent, root, "HiddenSpikes", new(-.5f, top + .15f), new(.5f, .3f), SpikesRed, 0, rooms, death, observers, "Trigger", new(-.6f, 0f), new(.7f, .6f), 0, null, changes);

        static void BuildDoorRetreat(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildDoorRetreatCore(parent, root, "DoorRetreat", new(2.25f, top + .75f), new(.7f, 1.5f), 0, rooms, death, observers, GameObject.Find("Door_0")?.transform, new Vector2(2f, 0f), 10, 0, changes);

        static void BuildFallingBlock(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes) =>
            BuildFallingBlockCore(parent, root, "FallingBlock", new(8f, top + 3f), new(.8f, .8f), BlockGrey, 1, rooms, death, observers, "Trigger", new(0f, -2.25f), new(1.6f, 1.5f), FallingBlockDirection.Down, 0, .3f, 2.6f, null, changes);

        internal static GameObject CreateTrap<T>(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, bool trigger, int? sortingOrder, List<string> changes, bool box = true) where T : Component
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
                if (box)
                {
                    BoxCollider2D collider = SetupUtility.Ensure<BoxCollider2D>(go, changes);
                    SetupUtility.SetColliderSize(collider, size, changes);
                    collider.isTrigger = trigger;
                }
            }
            else if (box && go.GetComponent<BoxCollider2D>() == null)
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
