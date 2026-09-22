using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-049 (D-061) §5 tests 4-7. Builds a minimal one-room solo rig by hand (a
    /// single door, so room 0 is also the level's last room), matching the GameObject-rig
    /// pattern from RoomDeathHoldTests.cs (PAX-047's rig), extended with LevelCompleteScreen.
    /// ObserverSet.FixedUpdate is driven directly by reflection, once per simulated tick, since
    /// EditMode does not run the player loop.</summary>
    public sealed class LevelCompleteScreenTests
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
            public GameObject RealityRootGo, CatGo, ObserverContextGo, ObserverSetGo, RoomManagerGo, RoomDeathGo, CheckpointManagerGo, CheckpointMarkerGo, RoomDoorGo, ScreenGo;
            public RealityRoot RealityRoot;
            public CatMotor2D Cat;
            public ObserverContext ObserverContext;
            public ObserverSet Observers;
            public RoomManager Rooms;
            public RoomDeath Death;
            public CheckpointManager Checkpoints;
            public RoomDoor Door;
            public LevelCompleteScreen Screen;

            static readonly MethodInfo ObserverSetFixedUpdate =
                typeof(ObserverSet).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            public void Tick() => ObserverSetFixedUpdate.Invoke(Observers, null);

            public static Rig Build(int holdTicks, Vector2 spawnPosition)
            {
                var rig = new Rig();

                rig.RealityRootGo = new GameObject("RealityRoot_A");
                rig.RealityRoot = rig.RealityRootGo.AddComponent<RealityRoot>();
                SetPrivate(rig.RealityRoot, "id", ObserverId.A);
                Invoke(rig.RealityRoot, "Awake");

                rig.CatGo = new GameObject("Cat");
                rig.CatGo.transform.SetParent(rig.RealityRootGo.transform, false);
                rig.CatGo.layer = LayerMask.NameToLayer("RealityA");
                rig.CatGo.AddComponent<Rigidbody2D>();
                var catCollider = rig.CatGo.AddComponent<BoxCollider2D>();
                catCollider.size = new Vector2(1f, 0.56f);
                catCollider.offset = new Vector2(0f, -0.3f);
                GravityReceiver gravity = rig.CatGo.AddComponent<GravityReceiver>();
                Invoke(gravity, "Awake");
                rig.Cat = rig.CatGo.AddComponent<CatMotor2D>();
                SetPrivate(rig.Cat, "config", ScriptableObject.CreateInstance<CatMotorConfig>());
                Invoke(rig.Cat, "Awake");
                CatRespawn respawn = rig.CatGo.AddComponent<CatRespawn>();
                Invoke(respawn, "Awake");

                rig.ObserverContextGo = new GameObject("ObserverA");
                rig.ObserverContext = rig.ObserverContextGo.AddComponent<ObserverContext>();
                SetPrivate(rig.ObserverContext, "id", ObserverId.A);
                SetPrivate(rig.ObserverContext, "reality", rig.RealityRoot);
                SetPrivate(rig.ObserverContext, "cat", rig.Cat);
                Invoke(rig.ObserverContext, "Awake");
                rig.ObserverContext.SetDriver(new FakeLocalHumanDriver());

                rig.ObserverSetGo = new GameObject("Observers");
                rig.Observers = rig.ObserverSetGo.AddComponent<ObserverSet>();
                SetPrivate(rig.Observers, "observerA", rig.ObserverContext);
                Invoke(rig.Observers, "Awake");

                rig.CheckpointManagerGo = new GameObject("Checkpoints");
                rig.Checkpoints = rig.CheckpointManagerGo.AddComponent<CheckpointManager>();
                SetPrivate(rig.Checkpoints, "rootA", rig.RealityRoot);

                rig.RoomManagerGo = new GameObject("RoomManager");
                rig.Rooms = rig.RoomManagerGo.AddComponent<RoomManager>();
                SetPrivate(rig.Rooms, "soloReality", ObserverId.A);
                SetPrivate(rig.Rooms, "checkpoints", rig.Checkpoints);
                SetPrivate(rig.Rooms, "observers", rig.Observers);

                rig.RoomDeathGo = new GameObject("RoomDeath");
                rig.Death = rig.RoomDeathGo.AddComponent<RoomDeath>();
                SetPrivate(rig.Death, "checkpoints", rig.Checkpoints);
                SetPrivate(rig.Death, "rooms", rig.Rooms);
                SetPrivate(rig.Death, "observers", rig.Observers);
                var safety = ScriptableObject.CreateInstance<RoomSafetyConfig>();
                SetPrivate(safety, "holdTicks", holdTicks);
                SetPrivate(rig.Death, "config", safety);
                Invoke(rig.Death, "Awake");

                SetPrivate(rig.Rooms, "roomDeath", rig.Death);
                Invoke(rig.Rooms, "OnEnable");

                rig.CheckpointMarkerGo = new GameObject("Checkpoint_0");
                rig.CheckpointMarkerGo.transform.SetParent(rig.RealityRootGo.transform, false);
                rig.CheckpointMarkerGo.transform.localPosition = spawnPosition;
                var marker = rig.CheckpointMarkerGo.AddComponent<CheckpointMarker>();
                SetPrivate(marker, "checkpointId", 0);
                SetPrivate(marker, "manager", rig.Checkpoints);
                SetPrivate(marker, "observers", rig.Observers);
                Invoke(marker, "OnEnable");

                rig.RoomDoorGo = new GameObject("Door");
                rig.RoomDoorGo.transform.SetParent(rig.RealityRootGo.transform, false);
                rig.Door = rig.RoomDoorGo.AddComponent<RoomDoor>();
                SetPrivate(rig.Door, "roomId", 0);
                SetPrivate(rig.Door, "manager", rig.Rooms);
                Invoke(rig.Door, "OnEnable");

                rig.ScreenGo = new GameObject("LevelCompleteScreen");
                rig.Screen = rig.ScreenGo.AddComponent<LevelCompleteScreen>();
                SetPrivate(rig.Screen, "rooms", rig.Rooms);
                SetPrivate(rig.Screen, "roomDeath", rig.Death);
                SetPrivate(rig.Screen, "checkpoints", rig.Checkpoints);
                SetPrivate(rig.Screen, "observers", rig.Observers);
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

        // ---------- §5 test 4 ----------

        [Test]
        public void LastDoorTouch_FiresLevelCompletedExactlyOnce_SummaryMatchesRecordedDeaths()
        {
            Rig rig = Rig.Build(holdTicks: 0, spawnPosition: Vector2.zero);
            try
            {
                rig.CatGo.transform.position = new Vector2(3f, 0f);
                Physics2D.SyncTransforms();
                rig.Death.Kill(ObserverId.A, DeathCause.Hazard); // holdTicks 0: resets synchronously, respawns at (0,0)

                Vector2 doorPos = new Vector2(10f, 0f);
                rig.RoomDoorGo.transform.localPosition = doorPos;
                rig.CatGo.transform.position = doorPos;
                Physics2D.SyncTransforms();

                int completedCount = 0;
                rig.Rooms.LevelCompleted += () => completedCount++;

                rig.Tick();

                Assert.AreEqual(1, completedCount, "LevelCompleted must fire exactly once");
                Assert.IsNotNull(rig.Screen.LastSummary);
                Assert.AreEqual(1, rig.Screen.LastSummary.Length);
                Assert.AreEqual(1, rig.Screen.LastSummary[0].RoomNumber);
                Assert.AreEqual(0, rig.Screen.LastSummary[0].RoomId);
                Assert.AreEqual(1, rig.Screen.LastSummary[0].Deaths);
                Assert.AreEqual(1, rig.Death.DeathsIn(0));
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5 test 5 ----------

        [Test]
        public void AfterLevelComplete_NoPhasesRun_KillNotCounted_CatFrozen()
        {
            Rig rig = Rig.Build(holdTicks: 0, spawnPosition: Vector2.zero);
            try
            {
                Vector2 doorPos = new Vector2(10f, 0f);
                rig.RoomDoorGo.transform.localPosition = doorPos;
                rig.CatGo.transform.position = doorPos;
                Physics2D.SyncTransforms();

                bool levelCompleted = false;
                rig.Rooms.LevelCompleted += () => levelCompleted = true;

                rig.Tick(); // touches the door; completes the only (= last) room and the level

                Assert.IsTrue(levelCompleted);
                Assert.IsTrue(rig.Cat.IsFrozen, "the cat must be frozen once the level completes");

                int roomLifeTickAfterComplete = rig.Rooms.RoomLifeTick;

                for (int i = 0; i < 5; i++) rig.Tick();

                Assert.AreEqual(roomLifeTickAfterComplete, rig.Rooms.RoomLifeTick, "no further room-life tick may run after level complete");
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "no death has occurred yet");

                bool died = false;
                rig.Death.Died += _ => died = true;

                rig.Death.Kill(ObserverId.A, DeathCause.Hazard); // simulates an out-of-band kill (e.g. FallResetVolume)

                Assert.AreEqual(0, rig.Death.DeathsIn(0), "a kill after level complete must not be counted");
                Assert.IsFalse(rig.Death.IsHolding, "no hold may begin after level complete");
                Assert.IsFalse(died, "Died must not be raised after level complete");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5 test 6 ----------

        [Test]
        public void KillAndLastDoorTouch_SameTick_KillWins_LevelNotComplete_DeathCountedOnce()
        {
            Rig rig = Rig.Build(holdTicks: 0, spawnPosition: Vector2.zero);
            try
            {
                Vector2 doorPos = new Vector2(10f, 0f);
                rig.RoomDoorGo.transform.localPosition = doorPos;
                rig.CatGo.transform.position = doorPos; // touches the door...
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, Vector2.zero, new Vector2(1f, 1f)) }); // ...but is out of bounds
                Physics2D.SyncTransforms();

                bool levelCompleted = false;
                rig.Rooms.LevelCompleted += () => levelCompleted = true;

                rig.Tick();

                Assert.IsFalse(levelCompleted, "the kill must win; the level must not complete on this tick");
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "the death is counted exactly once");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5 test 7 ----------

        [Test]
        public void RoomClear_LogsOnePARALLAX_STATSLine()
        {
            Rig rig = Rig.Build(holdTicks: 0, spawnPosition: Vector2.zero);
            try
            {
                Vector2 doorPos = new Vector2(10f, 0f);
                rig.RoomDoorGo.transform.localPosition = doorPos;
                rig.CatGo.transform.position = doorPos;
                Physics2D.SyncTransforms();

                // Start() recorded roomStartTick[0] = 0 (Tick is 0 before any FixedUpdate); the
                // single Tick() below advances Tick to 1, so ticks = 1 - 0 = 1.
                LogAssert.Expect(LogType.Log, "PARALLAX_STATS room_clear room=1 deaths=0 ticks=1");

                rig.Tick();
            }
            finally { rig.Dispose(); }
        }
    }
}
