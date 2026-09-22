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
            var changes = new List<string>(); Transform parent = SetupUtility.EnsureChild(root.transform, "Rooms_PAX045", root.gameObject.layer, changes);
            foreach (SoloRoomDefinition room in TrapLabLayout.Rooms)
            {
                if (!TrapLayoutValidator.TryValidate(room, out string error)) { Debug.LogError("TrapLabSetup: " + error); return; }
                if (parent.Find($"Room_{room.Id + 1}") == null) SoloRoomsSetup.BuildRoom(parent, root, room, checkpoints, rooms, death, observers, config, changes);
            }
            if (changes.Count == 0) { Debug.Log("TrapLabSetup: no changes."); return; }
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene); Debug.Log("TrapLabSetup created: " + string.Join("; ", changes));
        }
    }
}
