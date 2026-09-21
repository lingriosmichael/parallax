using System.Collections.Generic;
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

        static void BuildCollapsingFloor(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes)
        {
            GameObject go = CreateTrap<CollapsingFloorTrap>(parent, root, "CollapsingFloor", new(-8f, top + .85f), new(1.5f, .3f), FloorTan, 0, rooms, death, observers, false, changes);
            Write(go.GetComponent<CollapsingFloorTrap>(), changes, ("delayTicks", 12));
        }

        static void BuildGravityFlip(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes)
        {
            GameObject go = CreateTrap<GravityFlipTrap>(parent, root, "GravityFlip", new(-4f, top + .75f), new(1f, 1.5f), FlipPurple, 0, rooms, death, observers, true, changes);
            Write(go.GetComponent<GravityFlipTrap>(), changes, ("delayTicks", 0), ("rearmOnExit", false));
        }

        static void BuildHiddenSpikes(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes)
        {
            GameObject go = CreateTrap<HiddenSpikesTrap>(parent, root, "HiddenSpikes", new(-.5f, top + .15f), new(.5f, .3f), SpikesRed, 0, rooms, death, observers, true, changes);
            Hazard hazard = SetupUtility.Ensure<Hazard>(go, changes);
            Write(hazard, changes, ("observers", (Object)observers), ("roomDeath", death), ("rooms", rooms), ("armed", false));
            // Ahead of the spikes, but ending left of the spawn collider to prevent a respawn loop.
            BoxCollider2D trigger = CreateTrigger(go.transform, root, "Trigger", new(-.6f, 0f), new(.7f, .6f), changes);
            Write(go.GetComponent<HiddenSpikesTrap>(), changes, ("hazard", hazard), ("trigger", trigger), ("revealDelayTicks", 0));
        }

        static void BuildDoorRetreat(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes)
        {
            GameObject go = CreateTrap<DoorRetreatTrap>(parent, root, "DoorRetreat", new(2.25f, top + .75f), new(.7f, 1.5f), Color.clear, 0, rooms, death, observers, true, changes);
            Write(go.GetComponent<DoorRetreatTrap>(), changes, ("trigger", go.GetComponent<BoxCollider2D>()), ("doorRoot", GameObject.Find("Door_0")?.transform), ("offset", new Vector2(2f, 0f)), ("moveTicks", 10), ("delayTicks", 0));
        }

        static void BuildFallingBlock(Transform parent, RealityRoot root, RoomManager rooms, RoomDeath death, ObserverSet observers, float top, List<string> changes)
        {
            const float blockHeight = .8f;
            Vector2 start = new(8f, top + 3f);
            GameObject go = CreateTrap<FallingBlockTrap>(parent, root, "FallingBlock", start, new(.8f, blockHeight), BlockGrey, 1, rooms, death, observers, false, changes);
            Rigidbody2D body = SetupUtility.Ensure<Rigidbody2D>(go, changes);
            SetupUtility.SetBodyType(body, RigidbodyType2D.Kinematic, changes);
            BoxCollider2D block = go.GetComponent<BoxCollider2D>();
            block.isTrigger = false;
            BoxCollider2D trigger = CreateTrigger(go.transform, root, "Trigger", new(0f, top + .75f - start.y), new(1.6f, 1.5f), changes);
            float travelDistance = start.y - (blockHeight * .5f) - top;
            Write(go.GetComponent<FallingBlockTrap>(), changes, ("trigger", trigger), ("direction", 0), ("delayTicks", 0), ("unitsPerTick", .3f), ("travelDistance", travelDistance));
        }

        static GameObject CreateTrap<T>(Transform parent, RealityRoot root, string name, Vector2 position, Vector2 size, Color color, int roomId, RoomManager rooms, RoomDeath death, ObserverSet observers, bool trigger, List<string> changes) where T : Component
        {
            Transform transform = SetupUtility.EnsureChild(parent, name, root.gameObject.layer, changes);
            GameObject go = transform.gameObject;
            bool newTrap = go.GetComponent<T>() == null;
            if (newTrap)
            {
                SetupUtility.SetLocalPosition(transform, position, changes);
                if (color != Color.clear) SetupUtility.SetVisual(go, root, size, color, changes);
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

        static BoxCollider2D CreateTrigger(Transform parent, RealityRoot root, string name, Vector2 localPosition, Vector2 size, List<string> changes)
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

        static void Write(Object target, List<string> changes, params (string name, object value)[] values)
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
