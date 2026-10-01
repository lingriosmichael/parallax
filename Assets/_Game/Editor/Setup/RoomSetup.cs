using System.Collections.Generic;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class RoomSetup
    {
        const string DoorArtPath = "Assets/_Game/Art/Doors/A_Door_Exit.png";
        const int DoorArtSortingOrder = -2;
        static readonly Vector2 DoorSize = new Vector2(0.6f, 1.5f);

        [MenuItem("PARALLAX/Setup/Rooms (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject rootAObject = GameObject.Find("RealityRoot_A");
            RealityRoot rootA = rootAObject == null ? null : rootAObject.GetComponent<RealityRoot>();
            GameObject systems = GameObject.Find("Systems");
            CheckpointManager checkpoints = Object.FindAnyObjectByType<CheckpointManager>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            DebugPanel debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (rootA == null || systems == null || checkpoints == null || observers == null)
            {
                Debug.LogError("RoomSetup: RealityRoot_A, Systems, CheckpointManager and ObserverSet are required. Run the checkpoint setup first.");
                return;
            }

            Transform groundTransform = rootA.transform.Find("Geometry/Ground");
            BoxCollider2D ground = groundTransform == null ? null : groundTransform.GetComponent<BoxCollider2D>();
            SpawnPoint spawn = rootA.GetComponentInChildren<SpawnPoint>(true);
            if (ground == null || spawn == null)
            {
                Debug.LogError("RoomSetup: Reality A Geometry/Ground collider and SpawnPoint are required.");
                return;
            }

            RoomManager manager = SetupUtility.Ensure<RoomManager>(systems, changes);
            SetEnum(manager, "soloReality", (int)ObserverId.A, changes);
            SetupUtility.SetObject(manager, "checkpoints", checkpoints, changes);
            SetupUtility.SetObject(manager, "observers", observers, changes);

            float groundTop = rootA.ToLocal((Vector2)ground.bounds.max).y;
            float groundMinX = rootA.ToLocal((Vector2)ground.bounds.min).x;
            float groundMaxX = rootA.ToLocal((Vector2)ground.bounds.max).x;
            float spawnX = rootA.ToLocal(spawn.Position).x;
            float y = groundTop + DoorSize.y * 0.5f;

            float door1X = spawnX + 8f;
            float halfWidth = DoorSize.x * 0.5f;
            if (door1X + halfWidth > groundMaxX)
            {
                door1X = groundMaxX - halfWidth - 0.25f;
                Debug.Log($"RoomSetup: spawnX+8 is off the continuous ground (ground x {groundMinX:F2}..{groundMaxX:F2}); Door_1 moved to x={door1X:F2}.");
            }

            BuildDoor(rootA, manager, 0, new Vector2(spawnX + 3f, y), changes);
            BuildDoor(rootA, manager, 1, new Vector2(door1X, y), changes);

            if (debugPanel != null) SetupUtility.SetObject(debugPanel, "rooms", manager, changes);

            Debug.Log($"RoomSetup: ground x {groundMinX:F2}..{groundMaxX:F2} top {groundTop:F2}; Door_0=({spawnX + 3f:F2},{y:F2}); Door_1=({door1X:F2},{y:F2}).");
            if (changes.Count == 0) Debug.Log("RoomSetup: no changes.");
            else
            {
                Debug.Log("RoomSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }

            RealityIsolationValidator.Validate();
            CheckpointValidator.Validate();
            GravityValidator.Validate();
            RoomValidator.Validate();
        }

        static void BuildDoor(RealityRoot root, RoomManager manager, int id, Vector2 localPosition, List<string> changes) =>
            BuildDoorCore(SetupUtility.EnsureChild(root.transform, "Doors", LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id)), changes), root, $"Door_{id}", manager, id, localPosition, DoorSize, new Color(0.9f, 0.5f, 1f, 1f), null, changes);

        internal static RoomDoor BuildDoorCore(Transform parent, RealityRoot root, string name, RoomManager manager, int id, Vector2 localPosition, Vector2 size, Color color, int? sortingOrder, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            GameObject doorObject = SetupUtility.EnsureChild(parent, name, layer, changes).gameObject;
            SetupUtility.SetLocalPosition(doorObject.transform, localPosition, changes);
            SpriteRenderer greybox = SetupUtility.SetVisual(doorObject, root, size, color, changes);
            if (sortingOrder.HasValue && greybox.sortingOrder != sortingOrder.Value) { greybox.sortingOrder = sortingOrder.Value; changes.Add("set " + doorObject.name + ".sortingOrder"); }
            BuildDoorArt(doorObject.transform, greybox, root, size, changes);
            RoomDoor door = SetupUtility.Ensure<RoomDoor>(doorObject, changes);
            SetInt(door, "roomId", id, changes);
            SetupUtility.SetVector2(door, "zoneSize", size, changes);
            SetupUtility.SetObject(door, "manager", manager, changes);
            return door;
        }

        static void BuildDoorArt(Transform door, SpriteRenderer greybox, RealityRoot root, Vector2 doorSize, List<string> changes)
        {
            // PAX-A15 (ruling 5, Phase 1 answer 2): with the environment kit on, the door is ENV-24's lit frame (centre
            // pivot) with a warm glow behind it; otherwise the A_Door_Exit art (bottom pivot) as before.
            Sprite kitSprite = SoloRoomSkin.Enabled ? Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Door") : null;
            Sprite sprite = kitSprite != null ? kitSprite : AssetDatabase.LoadAssetAtPath<Sprite>(DoorArtPath);
            if (sprite == null)
            {
                Debug.LogError($"RoomSetup: door art not found at {DoorArtPath}; {door.name} keeps its greybox.");
                return;
            }

            Transform art = SetupUtility.EnsureChild(door, "Art", door.gameObject.layer, changes);
            float lift = kitSprite != null ? -sprite.bounds.min.y : 0f;
            SetupUtility.SetLocalPosition(art, new Vector2(0f, -doorSize.y * 0.5f + lift), changes);
            BuildDoorGlow(art, kitSprite != null, root, changes);
            var renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
            if (renderer.sprite != sprite)
            {
                renderer.sprite = sprite;
                changes.Add("set " + door.name + ".Art.sprite");
            }
            // Gameplay band like the cat, but order -2 keeps the door behind it and above the midground.
            string sortingLayer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changes.Add("set " + door.name + ".Art.sortingLayer");
            }
            if (renderer.sortingOrder != DoorArtSortingOrder)
            {
                renderer.sortingOrder = DoorArtSortingOrder;
                changes.Add("set " + door.name + ".Art.sortingOrder");
            }
            if (greybox != null && greybox.enabled)
            {
                greybox.enabled = false;
                changes.Add("disabled " + door.name + " greybox");
            }
        }

        // PAX-A15 (A6): a soft warm glow behind the lit door (the glow layer is reserved for the door, a crossed
        // checkpoint and trap tells), unlit so the grade's light never dims it.
        static void BuildDoorGlow(Transform art, bool on, RealityRoot root, List<string> changes)
        {
            var look = AssetDatabase.LoadAssetAtPath<Parallax.Editor.Levels.LevelLookConfig>(Parallax.Editor.Levels.LevelLookConfig.AssetPath);
            bool ready = on && look != null;
            // A soft dark pocket behind the door, then a warm glow around its opening: the lit doorway pops (A6).
            DoorBacking(art, "Shade", ready ? Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Halo") : null, ready ? look.DoorShadeSize : 0f, new Color(1f, 1f, 1f, ready ? look.DoorShadeAlpha : 0f), DoorArtSortingOrder - 2, root, changes);
            DoorBacking(art, "Glow", ready ? Parallax.Editor.Art.EnvironmentKit.Sprite("ENV_Glow") : null, ready ? look.DoorGlowSize : 0f, new Color(1f, 0.86f, 0.58f, ready ? look.DoorGlowAlpha : 0f), DoorArtSortingOrder - 1, root, changes);
        }

        static void DoorBacking(Transform art, string name, Sprite sprite, float size, Color color, int order, RealityRoot root, List<string> changes)
        {
            Transform existing = art.Find(name);
            if (sprite == null)
            {
                if (existing != null) { Object.DestroyImmediate(existing.gameObject); changes.Add("removed " + art.parent.name + " door " + name); }
                return;
            }
            Transform t = SetupUtility.EnsureChild(art, name, art.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(t, Vector2.zero, changes);
            float scale = size / sprite.bounds.size.x;
            if (t.localScale != new Vector3(scale, scale, 1f)) { t.localScale = new Vector3(scale, scale, 1f); changes.Add("set " + art.parent.name + " " + name + " scale"); }
            var r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
            r.sprite = sprite;
            // PAX-A16 §3.1: the glow is additive (HDR, so it blooms); the pocket stays a plain soft shade.
            r.sharedMaterial = name == "Glow" && EnvironmentStackSetup.GlowMaterial != null ? EnvironmentStackSetup.GlowMaterial : Parallax.Editor.Art.EnvironmentKit.UnlitMaterial;
            r.color = color;
            r.sortingLayerName = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            r.sortingOrder = order;
        }

        static void SetInt(Object target, string name, int value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property.intValue == value) return;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }

        static void SetEnum(Object target, string name, int value, List<string> changes)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property.enumValueIndex == value) return;
            property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set " + target.name + "." + name);
        }
    }
}
