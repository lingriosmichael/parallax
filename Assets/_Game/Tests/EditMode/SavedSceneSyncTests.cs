using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-083 (item 11): the saved scenes, compared room by room with a fresh build of their layout
    // (TrapLabSetup.DifferencesFromLayout: every object path, transform, component and serialized field).
    // TrapLabSyncTests only builds rooms in an empty scene, so a stale Sandbox_TrapLab.unity passed it twice
    // (PAX-082, PAX-076 review). Red until the developer reruns the Trap Lab menu or Rebuild All Levels after a
    // layout change. Reads scenes without saving and restores the Test Runner's scene, as LevelSceneTimingTests does.
    public sealed class SavedSceneSyncTests
    {
        const string TrapLabScenePath = "Assets/Sandbox_TrapLab.unity";
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        static readonly Type Setup = Type.GetType("Parallax.Editor.Setup.TrapLabSetup, Parallax.Editor");

        [Test]
        public void TheSavedTrapLabScene_EveryRoom_MatchesItsLayout()
        {
            var rooms = (IEnumerable)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);
            var failures = new List<string>();
            int compared = WithScene(TrapLabScenePath, scene => Compare("Sandbox_TrapLab", scene, "Rooms_PAX045", rooms.Cast<object>(), failures));
            Assert.Greater(compared, 0, "no Trap Lab room was compared.");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void EachListedLevelScene_TheRoom_MatchesItsLayout()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist.");
            var registry = (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var failures = new List<string>();
            int compared = 0;
            foreach (LevelEntry entry in config.Levels)
            {
                if (!registry.Contains(entry.Id)) continue;
                string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;
                compared += WithScene(scenePath, scene => Compare(entry.Id, scene, "Rooms_PAX043", new[] { registry[entry.Id] }, failures));
            }
            Assert.Greater(compared, 0, "no level room was compared; is LevelListConfig empty?");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static int Compare(string label, Scene scene, string roomsName, IEnumerable<object> rooms, List<string> failures)
        {
            RealityRoot root = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<RealityRoot>(true)).FirstOrDefault(r => r != null && r.name == "RealityRoot_A");
            Assert.NotNull(root, label + ": no RealityRoot_A.");
            Transform parent = root.transform.Find(roomsName);
            Assert.NotNull(parent, $"{label}: no {roomsName} under RealityRoot_A.");
            object[] wiring = { Find<CheckpointManager>(scene), Find<RoomManager>(scene), Find<RoomDeath>(scene), Find<ObserverSet>(scene),
                AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset") };
            Assert.IsTrue(wiring.All(w => w != null), label + ": missing CheckpointManager, RoomManager, RoomDeath, ObserverSet or CatMotorConfig_Default.");

            int compared = 0;
            foreach (object room in rooms)
            {
                string name = $"Room_{(int)room.GetType().GetField("Id").GetValue(room) + 1}";
                Transform existing = parent.Find(name);
                if (existing == null) { failures.Add($"{label}: no {name} in the saved scene."); continue; }
                var differences = (List<string>)Setup.GetMethod("DifferencesFromLayout").Invoke(null, new object[] { existing, parent, root, room, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4] });
                compared++;
                if (differences.Count > 0) failures.Add($"{label} {name}: {differences.Count} differences from the layout: {string.Join("; ", differences)}");
            }
            return compared;
        }

        static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<T>(true)).FirstOrDefault(c => c != null);

        static int WithScene(string path, Func<Scene, int> body)
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try { return body(EditorSceneManager.OpenScene(path, OpenSceneMode.Single)); }
            finally { RestoreSetup(setup); }
        }

        static void RestoreSetup(string[] paths)
        {
            if (paths.Length == 0)
            {
                // PAX-075 R21: the Test Runner's untitled scene isn't in the setup list; recreate it rather than leave a scene loaded.
                Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                return;
            }
            var setup = new SceneSetup[paths.Length];
            for (int i = 0; i < paths.Length; i++) setup[i] = new SceneSetup { path = paths[i], isActive = i == 0, isLoaded = true };
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
