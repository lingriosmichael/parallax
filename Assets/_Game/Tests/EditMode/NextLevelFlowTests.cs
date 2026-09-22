using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-050 (D-063) §5 tests 5-6. Same GameObject-rig pattern as
    /// LevelCompleteScreenTests.cs (PAX-049), extended with a LevelListConfig and a
    /// NextLevelButton. The active EditMode test scene has no name matching any real level, so
    /// LevelListConfig entries are seeded with SceneManager.GetActiveScene().name as their
    /// SceneName to make the "current level" lookup resolve deterministically in-test.</summary>
    public sealed class NextLevelFlowTests
    {
        sealed class FakeLocalHumanDriver : IObserverDriver
        {
            public InputSourceKind Kind => InputSourceKind.LocalHuman;
            public void Activate(ObserverContext observer) { }
            public void Deactivate() { }
            public void FixedTick(int tick) { }
        }

        sealed class Rig
        {
            public GameObject RealityRootGo, CatGo, ObserverContextGo, ObserverSetGo, RoomManagerGo, RoomDeathGo, CheckpointManagerGo, CheckpointMarkerGo, RoomDoorGo, ScreenGo, NextLevelGo;
            public ObserverSet Observers;
            public RoomManager Rooms;
            public RoomDeath Death;
            public RoomDoor Door;
            public LevelCompleteScreen Screen;
            public NextLevelButton NextLevelButton;
            public LevelListConfig LevelList;

            static readonly MethodInfo ObserverSetFixedUpdate =
                typeof(ObserverSet).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            public void Tick() => ObserverSetFixedUpdate.Invoke(Observers, null);

            public static Rig Build(bool hasNextLevel)
            {
                var rig = new Rig();
                Vector2 spawnPosition = Vector2.zero;

                var realityRootGo = new GameObject("RealityRoot_A");
                var realityRoot = realityRootGo.AddComponent<RealityRoot>();
                SetPrivate(realityRoot, "id", ObserverId.A);
                Invoke(realityRoot, "Awake");
                rig.RealityRootGo = realityRootGo;

                rig.CatGo = new GameObject("Cat");
                rig.CatGo.transform.SetParent(realityRootGo.transform, false);
                rig.CatGo.layer = LayerMask.NameToLayer("RealityA");
                rig.CatGo.AddComponent<Rigidbody2D>();
                var catCollider = rig.CatGo.AddComponent<BoxCollider2D>();
                catCollider.size = new Vector2(1f, 0.56f);
                catCollider.offset = new Vector2(0f, -0.3f);
                GravityReceiver gravity = rig.CatGo.AddComponent<GravityReceiver>();
                Invoke(gravity, "Awake");
                CatMotor2D cat = rig.CatGo.AddComponent<CatMotor2D>();
                SetPrivate(cat, "config", ScriptableObject.CreateInstance<CatMotorConfig>());
                Invoke(cat, "Awake");
                CatRespawn respawn = rig.CatGo.AddComponent<CatRespawn>();
                Invoke(respawn, "Awake");

                rig.ObserverContextGo = new GameObject("ObserverA");
                var observerContext = rig.ObserverContextGo.AddComponent<ObserverContext>();
                SetPrivate(observerContext, "id", ObserverId.A);
                SetPrivate(observerContext, "reality", realityRoot);
                SetPrivate(observerContext, "cat", cat);
                Invoke(observerContext, "Awake");
                observerContext.SetDriver(new FakeLocalHumanDriver());

                rig.ObserverSetGo = new GameObject("Observers");
                rig.Observers = rig.ObserverSetGo.AddComponent<ObserverSet>();
                SetPrivate(rig.Observers, "observerA", observerContext);
                Invoke(rig.Observers, "Awake");

                rig.CheckpointManagerGo = new GameObject("Checkpoints");
                var checkpoints = rig.CheckpointManagerGo.AddComponent<CheckpointManager>();
                SetPrivate(checkpoints, "rootA", realityRoot);

                rig.RoomManagerGo = new GameObject("RoomManager");
                rig.Rooms = rig.RoomManagerGo.AddComponent<RoomManager>();
                SetPrivate(rig.Rooms, "soloReality", ObserverId.A);
                SetPrivate(rig.Rooms, "checkpoints", checkpoints);
                SetPrivate(rig.Rooms, "observers", rig.Observers);

                rig.RoomDeathGo = new GameObject("RoomDeath");
                rig.Death = rig.RoomDeathGo.AddComponent<RoomDeath>();
                SetPrivate(rig.Death, "checkpoints", checkpoints);
                SetPrivate(rig.Death, "rooms", rig.Rooms);
                SetPrivate(rig.Death, "observers", rig.Observers);
                var safety = ScriptableObject.CreateInstance<RoomSafetyConfig>();
                SetPrivate(safety, "holdTicks", 0);
                SetPrivate(rig.Death, "config", safety);
                Invoke(rig.Death, "Awake");

                SetPrivate(rig.Rooms, "roomDeath", rig.Death);
                Invoke(rig.Rooms, "OnEnable");

                rig.CheckpointMarkerGo = new GameObject("Checkpoint_0");
                rig.CheckpointMarkerGo.transform.SetParent(realityRootGo.transform, false);
                rig.CheckpointMarkerGo.transform.localPosition = spawnPosition;
                var marker = rig.CheckpointMarkerGo.AddComponent<CheckpointMarker>();
                SetPrivate(marker, "checkpointId", 0);
                SetPrivate(marker, "manager", checkpoints);
                SetPrivate(marker, "observers", rig.Observers);
                Invoke(marker, "OnEnable");

                rig.RoomDoorGo = new GameObject("Door");
                rig.RoomDoorGo.transform.SetParent(realityRootGo.transform, false);
                rig.Door = rig.RoomDoorGo.AddComponent<RoomDoor>();
                SetPrivate(rig.Door, "roomId", 0);
                SetPrivate(rig.Door, "manager", rig.Rooms);
                Invoke(rig.Door, "OnEnable");

                // LevelListConfig: SceneName matches the active EditMode test scene, so
                // LevelCompleteScreen's lookup-by-active-scene-name resolves it.
                string activeScene = SceneManager.GetActiveScene().name;
                rig.LevelList = ScriptableObject.CreateInstance<LevelListConfig>();
                var entries = hasNextLevel
                    ? new[]
                    {
                        new LevelEntry { Id = "current", SceneName = activeScene, DisplayName = "Current" },
                        new LevelEntry { Id = "next", SceneName = "SomeOtherScene", DisplayName = "Next" },
                    }
                    : new[] { new LevelEntry { Id = "current", SceneName = activeScene, DisplayName = "Current" } };
                SetPrivate(rig.LevelList, "levels", entries);

                // No Button component: these tests call the public API directly rather than
                // simulating a UI click, so the serialized `button` field can stay null (the
                // same null-guarded path OnEnable already takes for an unwired button).
                rig.NextLevelGo = new GameObject("NextLevelButton", typeof(RectTransform));
                rig.NextLevelButton = rig.NextLevelGo.AddComponent<NextLevelButton>();
                rig.NextLevelGo.SetActive(false);

                rig.ScreenGo = new GameObject("LevelCompleteScreen");
                rig.Screen = rig.ScreenGo.AddComponent<LevelCompleteScreen>();
                SetPrivate(rig.Screen, "rooms", rig.Rooms);
                SetPrivate(rig.Screen, "roomDeath", rig.Death);
                SetPrivate(rig.Screen, "checkpoints", checkpoints);
                SetPrivate(rig.Screen, "observers", rig.Observers);
                SetPrivate(rig.Screen, "levelList", rig.LevelList);
                SetPrivate(rig.Screen, "nextLevelButton", rig.NextLevelButton);
                Invoke(rig.Screen, "OnEnable");
                Invoke(rig.Screen, "Start");

                rig.CatGo.transform.position = spawnPosition;
                Physics2D.SyncTransforms();

                return rig;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(RealityRootGo);
                Object.DestroyImmediate(ObserverContextGo);
                Object.DestroyImmediate(ObserverSetGo);
                Object.DestroyImmediate(RoomManagerGo);
                Object.DestroyImmediate(RoomDeathGo);
                Object.DestroyImmediate(CheckpointManagerGo);
                Object.DestroyImmediate(ScreenGo);
                Object.DestroyImmediate(NextLevelGo);
            }
        }

        static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? target.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"{target.GetType().Name}.{method} not found");
            m.Invoke(target, null);
        }

        static void CleanUpProgress(params string[] ids)
        {
            foreach (string id in ids)
            {
                PlayerPrefs.DeleteKey("PARALLAX_LEVEL_UNLOCKED_" + id);
                PlayerPrefs.DeleteKey("PARALLAX_LEVEL_BEST_" + id);
            }
        }

        void CompleteTheOnlyRoom(Rig rig)
        {
            Vector2 doorPos = new Vector2(10f, 0f);
            rig.RoomDoorGo.transform.localPosition = doorPos;
            rig.CatGo.transform.position = doorPos;
            Physics2D.SyncTransforms();
            rig.Tick();
        }

        // ---------- §5 test 5 ----------

        [Test]
        public void NoNextLevelInList_NextLevelButtonStaysHidden()
        {
            Rig rig = Rig.Build(hasNextLevel: false);
            try
            {
                CompleteTheOnlyRoom(rig);

                Assert.IsFalse(rig.NextLevelGo.activeSelf, "with no next level in the list, the button must not be shown");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5 test 6 ----------

        [Test]
        public void NextLevelInList_ButtonShown_ClickRecordsCompletionWithCorrectIdAndDeaths()
        {
            CleanUpProgress("current", "next");
            Rig rig = Rig.Build(hasNextLevel: true);
            try
            {
                Vector2 doorPos = new Vector2(3f, 0f);
                rig.CatGo.transform.position = doorPos;
                Physics2D.SyncTransforms();
                rig.Death.Kill(ObserverId.A, DeathCause.Hazard); // one recorded death before clearing

                CompleteTheOnlyRoom(rig);

                Assert.IsTrue(rig.NextLevelGo.activeSelf, "with a next level in the list, the button must be shown");

                rig.NextLevelButton.RecordAndSaveProgress(); // the click handler's side effect, without an actual scene load

                LevelProgress progress = LevelProgressStore.Load(new[] { "current", "next" });
                Assert.AreEqual(1, progress.BestDeaths("current"), "the recorded death count must match this run's total");
                Assert.IsTrue(progress.IsUnlocked("next"));
            }
            finally
            {
                rig.Dispose();
                CleanUpProgress("current", "next");
            }
        }
    }
}
