using System.Collections.Generic;
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
    public static class TrapLabSetup
    {
        [MenuItem("PARALLAX/Setup/Trap Lab (PAX-045)")]
        public static void Configure()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Sandbox_TrapLab") { Debug.LogError("TrapLabSetup: active scene must be Sandbox_TrapLab."); return; }
            RealityRoot root = GameObject.Find("RealityRoot_A")?.GetComponent<RealityRoot>();
            CheckpointManager checkpoints = Object.FindAnyObjectByType<CheckpointManager>(FindObjectsInactive.Include);
            RoomManager rooms = Object.FindAnyObjectByType<RoomManager>(FindObjectsInactive.Include);
            RoomDeath death = Object.FindAnyObjectByType<RoomDeath>(FindObjectsInactive.Include);
            ObserverSet observers = Object.FindAnyObjectByType<ObserverSet>(FindObjectsInactive.Include);
            CatMotorConfig config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            if (root == null || checkpoints == null || rooms == null || death == null || observers == null || config == null) { Debug.LogError("TrapLabSetup: missing required solo-room dependencies."); return; }
            var changes = new List<string>();
            RoomSafetyConfig safety = SoloRoomsSetup.EnsureRoomSafetyConfig(changes);
            SoloRoomsSetup.Wire(death, "config", safety, changes);
            Transform parent = SetupUtility.EnsureChild(root.transform, "Rooms_PAX045", root.gameObject.layer, changes);
            foreach (SoloRoomDefinition room in TrapLabLayout.Rooms)
                if (!TrapLayoutValidator.TryValidate(room, out string error)) { Debug.LogError("TrapLabSetup: " + error); return; }
            // PAX-076: bring every room up to date, not only missing ones. A room that no longer matches the layout
            // (a refit, a changed trap setting) was left as it was, so the scene drifted from the validated layout.
            SyncRooms(parent, root, TrapLabLayout.Rooms, checkpoints, rooms, death, observers, config, changes);
            SoloRoomsSetup.AssignBounds(rooms, TrapLabLayout.Rooms, root, safety, changes);
            if (changes.Count == 0) { Debug.Log("TrapLabSetup: no changes."); return; }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene); Debug.Log("TrapLabSetup created: " + string.Join("; ", changes));
        }

        // PAX-076: builds each missing room, and rebuilds each existing room that differs from a fresh build of its
        // layout (every object path, transform, component and serialized field). Nothing outside a room references
        // objects inside it (the builder only wires room objects to the managers), so a rebuild leaves no stale
        // reference. A room that matches is left untouched, so a second run changes nothing.
        public static void SyncRooms(Transform parent, RealityRoot root, IReadOnlyList<SoloRoomDefinition> layoutRooms, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config, List<string> changes)
        {
            foreach (SoloRoomDefinition room in layoutRooms)
            {
                string name = $"Room_{room.Id + 1}";
                Transform existing = parent.Find(name);
                if (existing != null)
                {
                    List<string> differences = DifferencesFromLayout(existing, parent, root, room, checkpoints, rooms, death, observers, config);
                    if (differences.Count == 0) continue;
                    Object.DestroyImmediate(existing.gameObject);
                    changes.Add($"rebuilt {name} ({differences.Count} differences from the layout, first: {differences[0]})");
                }
                SoloRoomsSetup.BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
            }
        }

        // Builds the room fresh under a temporary sibling of parent (same place, same wiring), compares, destroys it.
        public static List<string> DifferencesFromLayout(Transform existing, Transform parent, RealityRoot root, SoloRoomDefinition room, CheckpointManager checkpoints, RoomManager rooms, RoomDeath death, ObserverSet observers, CatMotorConfig config)
        {
            var scratch = new GameObject("__TrapLabSyncCheck");
            try
            {
                scratch.layer = parent.gameObject.layer;
                scratch.transform.SetParent(parent.parent, false);
                scratch.transform.localPosition = parent.localPosition; scratch.transform.localRotation = parent.localRotation; scratch.transform.localScale = parent.localScale;
                SoloRoomBuilder.BuildRoom(scratch.transform, root, room, checkpoints, rooms, death, observers, config, new List<string>());
                Transform fresh = scratch.transform.Find(existing.name);
                var differences = new List<string>();
                if (fresh == null) { differences.Add("the layout builds no " + existing.name); return differences; }
                CompareTrees(existing, fresh, differences);
                return differences;
            }
            finally { Object.DestroyImmediate(scratch); }
        }

        static void CompareTrees(Transform actual, Transform expected, List<string> differences)
        {
            Dictionary<string, Transform> a = Paths(actual), e = Paths(expected);
            foreach (string path in a.Keys) if (!e.ContainsKey(path)) differences.Add(path + ": not in the layout");
            foreach (string path in e.Keys) if (!a.ContainsKey(path)) differences.Add(path + ": missing");
            foreach (KeyValuePair<string, Transform> pair in e)
            {
                if (!a.TryGetValue(pair.Key, out Transform other)) continue;
                Component[] expectedComponents = pair.Value.GetComponents<Component>(), actualComponents = other.GetComponents<Component>();
                if (expectedComponents.Length != actualComponents.Length) { differences.Add(pair.Key + ": components differ"); continue; }
                if (other.gameObject.activeSelf != pair.Value.gameObject.activeSelf || other.gameObject.layer != pair.Value.gameObject.layer) differences.Add(pair.Key + ": active state or layer");
                for (int i = 0; i < expectedComponents.Length; i++)
                {
                    if (expectedComponents[i] == null || actualComponents[i] == null || expectedComponents[i].GetType() != actualComponents[i].GetType()) { differences.Add(pair.Key + ": components differ"); break; }
                    string field = FirstDifferentField(actualComponents[i], actual, expectedComponents[i], expected);
                    if (field != null) differences.Add($"{pair.Key} {expectedComponents[i].GetType().Name}.{field}");
                }
            }
        }

        static Dictionary<string, Transform> Paths(Transform root)
        {
            var paths = new Dictionary<string, Transform>();
            void Walk(Transform t, string path) { paths[path] = t; foreach (Transform c in t) Walk(c, path + "/" + c.name); }
            Walk(root, root.name);
            return paths;
        }

        // The first serialized field that differs; references inside the room compare by path, others by identity.
        static string FirstDifferentField(Component actual, Transform actualRoot, Component expected, Transform expectedRoot)
        {
            SerializedProperty a = new SerializedObject(actual).GetIterator(), e = new SerializedObject(expected).GetIterator();
            bool moreA = a.Next(true), moreE = e.Next(true);
            while (moreA && moreE)
            {
                if (a.propertyPath != e.propertyPath) return e.propertyPath;
                if (a.propertyPath != "m_GameObject" && a.propertyPath != "m_Father" && !a.propertyPath.StartsWith("m_Children") && !a.propertyPath.StartsWith("m_CorrespondingSourceObject") && !a.propertyPath.StartsWith("m_PrefabInstance") && !a.propertyPath.StartsWith("m_PrefabAsset"))
                {
                    if (a.propertyType == SerializedPropertyType.ObjectReference)
                    { if (Reference(a.objectReferenceValue, actualRoot) != Reference(e.objectReferenceValue, expectedRoot)) return e.propertyPath; }
                    else if (!a.hasChildren && !SerializedProperty.DataEquals(a, e)) return e.propertyPath;
                }
                // A reference is compared above as a whole; its m_FileID/m_PathID internals always differ.
                bool enter = a.propertyType != SerializedPropertyType.ObjectReference;
                moreA = a.Next(enter); moreE = e.Next(enter);
            }
            return moreA == moreE ? null : "(field count)";
        }

        static string Reference(Object value, Transform root)
        {
            if (value == null) return "null";
            Transform t = value is Component c ? c.transform : value is GameObject g ? g.transform : null;
            if (t != null && (t == root || t.IsChildOf(root)))
            {
                string path = t.name;
                for (Transform p = t.parent; p != null && p != root.parent; p = p.parent) path = p.name + "/" + path;
                return "room:" + path + ":" + value.GetType().Name;
            }
            return "id:" + value.GetInstanceID();
        }
    }
}
