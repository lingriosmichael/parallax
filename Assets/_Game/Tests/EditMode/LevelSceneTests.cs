using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-052 (D-070/D-071): reads the built level scenes read-only to check what only the
    // scene, not layout data, can prove - baked RoomManager bounds, the level camera's wiring,
    // and Build Settings GUID stability. Opens scenes Single, one at a time (Level_Solo01 and
    // every L00N scene each carry their own frozen RealityRoot_B global light, so two must never
    // be loaded together), and restores the editor's original scene setup afterwards.
    public sealed class LevelSceneTests
    {
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        const string SoloScenePath = "Assets/_Game/Scenes/Level_Solo01.unity";
        static readonly Type LevelSetupType = Type.GetType("Parallax.Editor.Setup.LevelSetup, Parallax.Editor");
        static readonly Type LevelCameraSetupType = Type.GetType("Parallax.Editor.Setup.LevelCameraSetup, Parallax.Editor");

        static LevelListConfig Config()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist (run PARALLAX/Setup/Level List first).");
            return config;
        }

        static readonly (string levelId, int soloRoomId)[] SeedPairs = { ("L001", 0), ("L002", 1), ("L003", 2), ("L004", 3) };

        // ---------- §2.4 / 5.7 Scene-read bounds ----------

        [Test]
        public void SceneReadBounds_EachLevelScenesBakedRoomManagerBounds_EqualsLevelSolo01RoomNTranslated()
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try
            {
                // Level_Solo01 and every L00N scene each carry their own (untouched, frozen)
                // RealityRoot_B global light on the same sorting layer, so two of these scenes
                // must never be loaded at once - open every scene Single (replaces whatever else
                // is loaded, including each other), one at a time.
                var soloBoundsByRoom = new System.Collections.Generic.Dictionary<int, Bounds>();
                Scene solo = EditorSceneManager.OpenScene(SoloScenePath, OpenSceneMode.Single);
                RoomManager soloRooms = FindComponentAnywhere<RoomManager>(solo);
                Assert.NotNull(soloRooms, "Level_Solo01 must have a RoomManager.");
                foreach ((string _, int soloRoomId) in SeedPairs)
                {
                    Assert.IsTrue(soloRooms.TryGetBounds(soloRoomId, out Bounds bounds), "Level_Solo01 room " + soloRoomId + " bounds missing.");
                    soloBoundsByRoom[soloRoomId] = bounds;
                }

                foreach ((string levelId, int soloRoomId) in SeedPairs)
                {
                    if (!Config().Levels.Any(e => e.Id == levelId)) continue; // not built yet; nothing to read
                    LevelEntry entry = Config().Levels.First(e => e.Id == levelId);
                    string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                    if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;

                    Scene level = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    RoomManager levelRooms = FindComponentAnywhere<RoomManager>(level);
                    Assert.NotNull(levelRooms, levelId + " must have a RoomManager.");

                    Bounds soloBounds = soloBoundsByRoom[soloRoomId];
                    Assert.IsTrue(levelRooms.TryGetBounds(0, out Bounds levelBounds), levelId + " room 0 bounds missing.");

                    // L00N's room lives at local Origin (0,0) (D-066); its content and size are
                    // otherwise identical to Level_Solo01's room N (LevelLayoutTests.SplitFidelity_
                    // already proves that at the layout-data level), so a pure scene comparison -
                    // no layout read at all - is size and y equal, x free (the translation itself
                    // is layout data's job, already covered by BakedBounds_ in LevelLayoutTests).
                    Assert.That(levelBounds.center.y, Is.EqualTo(soloBounds.center.y).Within(.001f), levelId + " bounds centre y");
                    Assert.That(levelBounds.size.x, Is.EqualTo(soloBounds.size.x).Within(.001f), levelId + " bounds size x");
                    Assert.That(levelBounds.size.y, Is.EqualTo(soloBounds.size.y).Within(.001f), levelId + " bounds size y");

                    // The x axis, which the translation actually moves, is still checked - against
                    // this same scene's own baked camera frame (LevelCameraFollow.frameCenter), a
                    // pure scene read that needs no Level_Solo01/layout comparison: both
                    // RoomManager.bounds and the camera frame are ComputeRoomBounds on the same
                    // room with different margins, so their centres must agree exactly.
                    GameObject cameraGO = FindGameObject(level, "Camera_A");
                    var follow = cameraGO == null ? null : cameraGO.GetComponent<LevelCameraFollow>();
                    if (follow != null)
                    {
                        Vector2 frameCenter = new SerializedObject(follow).FindProperty("frameCenter").vector2Value;
                        Assert.That(levelBounds.center.x, Is.EqualTo(frameCenter.x).Within(.001f), levelId + " bounds centre x should match this scene's own baked camera frame centre.");
                    }
                }
            }
            finally
            {
                RestoreSetup(setup);
            }
        }

        // ---------- 5.8 Level scenes ----------

        [Test]
        public void EachListedLevelScene_HasLevelCameraOnCameraA_WiredToItsOwnRoomDeath()
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try
            {
                foreach (LevelEntry entry in Config().Levels)
                {
                    string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                    if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;

                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    GameObject cameraGO = FindGameObject(scene, "Camera_A");
                    Assert.NotNull(cameraGO, entry.Id + ": Camera_A not found.");

                    var follow = cameraGO.GetComponent<LevelCameraFollow>();
                    Assert.NotNull(follow, entry.Id + ": Camera_A has no LevelCameraFollow.");

                    RoomDeath sceneDeath = FindComponentAnywhere<RoomDeath>(scene);
                    Assert.NotNull(sceneDeath, entry.Id + ": scene has no RoomDeath.");

                    var serialized = new SerializedObject(follow);
                    Object wiredDeath = serialized.FindProperty("roomDeath").objectReferenceValue;
                    Assert.AreEqual(sceneDeath, wiredDeath, entry.Id + ": LevelCameraFollow.roomDeath is not this scene's own RoomDeath.");

                    Object wiredTarget = serialized.FindProperty("target").objectReferenceValue;
                    Assert.NotNull(wiredTarget, entry.Id + ": LevelCameraFollow.target is unwired.");

                    // The baked frame itself, not just the references: a level regenerated before
                    // the camera menu had ever run (or with a blank bake) would still pass every
                    // check above with frameCenter/frameSize left at (0,0) - which renders as a
                    // zero-size orthographic camera (CameraMath.ResolveViewHeight(Vector2.zero, ...)
                    // == 0). frameCenter/frameSize come from SoloRoomBuilder.ComputeRoomBounds on
                    // this scene's own room with two different margins (LevelCameraConfig.ViewMargin
                    // vs RoomSafetyConfig.BoundsMargin), so their centres must be exactly equal and
                    // their sizes must differ by exactly twice the margin difference, per axis.
                    LevelCameraConfig cameraConfig = serialized.FindProperty("config").objectReferenceValue as LevelCameraConfig;
                    Assert.NotNull(cameraConfig, entry.Id + ": LevelCameraFollow.config is unwired.");

                    Vector2 frameCenter = serialized.FindProperty("frameCenter").vector2Value;
                    Vector2 frameSize = serialized.FindProperty("frameSize").vector2Value;
                    Assert.Greater(frameSize.x, 0f, entry.Id + ": camera frame width must be baked (non-zero).");
                    Assert.Greater(frameSize.y, 0f, entry.Id + ": camera frame height must be baked (non-zero).");

                    RoomManager rooms = FindComponentAnywhere<RoomManager>(scene);
                    Assert.NotNull(rooms, entry.Id + ": scene has no RoomManager.");
                    Assert.IsTrue(rooms.TryGetBounds(0, out Bounds killBounds), entry.Id + ": room 0 kill bounds missing.");

                    RoomSafetyConfig safety = AssetDatabase.LoadAssetAtPath<RoomSafetyConfig>("Assets/_Game/Data/RoomSafetyConfig.asset");
                    Assert.NotNull(safety, "RoomSafetyConfig asset must exist.");
                    float marginDelta = safety.BoundsMargin - cameraConfig.ViewMargin;

                    Assert.That(frameCenter.x, Is.EqualTo(killBounds.center.x).Within(.001f), entry.Id + ": camera frame centre x must match the kill bounds centre.");
                    Assert.That(frameCenter.y, Is.EqualTo(killBounds.center.y).Within(.001f), entry.Id + ": camera frame centre y must match the kill bounds centre.");
                    Assert.That(frameSize.x, Is.EqualTo(killBounds.size.x - 2f * marginDelta).Within(.001f), entry.Id + ": camera frame width must be the kill bounds width narrowed by the margin difference.");
                    Assert.That(frameSize.y, Is.EqualTo(killBounds.size.y - 2f * marginDelta).Within(.001f), entry.Id + ": camera frame height must be the kill bounds height narrowed by the margin difference.");
                }
            }
            finally
            {
                RestoreSetup(setup);
            }
        }

        [Test]
        public void EachListedLevel_BuildSettingsGuidResolvesToItsScenePath()
        {
            foreach (LevelEntry entry in Config().Levels)
            {
                string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;

                Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == scenePath), entry.Id + ": '" + scenePath + "' is not in Build Settings.");
                EditorBuildSettingsScene listed = EditorBuildSettings.scenes.First(s => s.path == scenePath);
                Assert.AreEqual(scenePath, AssetDatabase.GUIDToAssetPath(listed.guid.ToString()), entry.Id + ": Build Settings GUID does not resolve back to its own scene path.");
            }
        }

        // ---------- 5.9 Guards ----------

        [Test]
        public void RegenerationGuard_RefusesSandboxRealitiesLevelSolo01AndTemplate_AllowsOtherScenes()
        {
            Assert.NotNull(LevelSetupType, "Parallax.Editor.Setup.LevelSetup not found.");
            MethodInfo method = LevelSetupType.GetMethod("IsGuardedScene", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelSetup.IsGuardedScene not found.");

            Assert.IsTrue((bool)method.Invoke(null, new object[] { "Sandbox_Realities" }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { "Level_Solo01" }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { "_LevelTemplate" }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { "Level_001" }));
        }

        [Test]
        public void LevelCameraGuard_RefusesEverySceneExceptTemplate()
        {
            Assert.NotNull(LevelCameraSetupType, "Parallax.Editor.Setup.LevelCameraSetup not found.");
            MethodInfo method = LevelCameraSetupType.GetMethod("RefusesToRun", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelCameraSetup.RefusesToRun not found.");

            Assert.IsTrue((bool)method.Invoke(null, new object[] { "Level_Solo01" }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { "Level_001" }));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { "Sandbox_Realities" }));
            Assert.IsFalse((bool)method.Invoke(null, new object[] { "_LevelTemplate" }));
        }

        static void RestoreSetup(string[] paths)
        {
            if (paths.Length == 0) return;
            var setup = new SceneSetup[paths.Length];
            for (int i = 0; i < paths.Length; i++) setup[i] = new SceneSetup { path = paths[i], isActive = i == 0, isLoaded = true };
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                Transform found = root.transform.Find(name);
                if (found != null) return found.gameObject;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child.gameObject;
            }
            return null;
        }

        static T FindComponentAnywhere<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
