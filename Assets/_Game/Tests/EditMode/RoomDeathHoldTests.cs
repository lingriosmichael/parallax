using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-047 (D-058) §8 tests 3, 5 and the bounds-kill/flipped-gravity rig cases from
    /// Phase 1b item 2. Builds a minimal solo-room rig by hand (RealityRoot_A, one cat, one
    /// checkpoint, RoomManager/RoomDeath, no scene/prefab), matching the GameObject-rig pattern
    /// already used by GravityReceiverTests/CatColliderConfigTests. ObserverSet.FixedUpdate is
    /// driven directly by reflection, once per simulated tick, since EditMode does not run the
    /// player loop.</summary>
    public sealed class RoomDeathHoldTests
    {
        sealed class FakeLocalHumanDriver : IObserverDriver
        {
            public bool MoveCommandEachTick;
            public InputSourceKind Kind => InputSourceKind.LocalHuman;
            public void Activate(ObserverContext observer) { }
            public void Deactivate() { }
            public void FixedTick(int tick)
            {
                if (!MoveCommandEachTick) return;
                var command = new CatCommand { Move = 1f, JumpPressed = true };
                Observer.Cat.Step(in command, 0.02f);
            }
            public ObserverContext Observer;
        }

        sealed class FakeResettable : IRoomResettable
        {
            public int RoomId { get; }
            public int ResetCount { get; private set; }
            public FakeResettable(int roomId) => RoomId = roomId;
            public void ResetToInitial() => ResetCount++;
        }

        sealed class Rig
        {
            public GameObject RealityRootGo, CatGo, ObserverContextGo, ObserverSetGo, RoomManagerGo, RoomDeathGo, CheckpointManagerGo, CheckpointMarkerGo, RoomDoorGo;
            public RealityRoot RealityRoot;
            public CatMotor2D Cat;
            public Rigidbody2D CatBody;
            public BoxCollider2D CatCollider;
            public ObserverContext ObserverContext;
            public ObserverSet Observers;
            public RoomManager Rooms;
            public RoomDeath Death;
            public CheckpointManager Checkpoints;
            public RoomDoor Door;
            public FakeLocalHumanDriver Driver;
            public FakeResettable Resettable;
            public Vector2 SpawnPosition;

            static readonly MethodInfo ObserverSetFixedUpdate =
                typeof(ObserverSet).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            public void Tick() => ObserverSetFixedUpdate.Invoke(Observers, null);

            // Unity does not run Awake/OnEnable synchronously for GameObjects built (or
            // reactivated) inside a plain EditMode [Test] method — confirmed empirically. So
            // every component here has its private fields wired by reflection first, then its
            // Awake/OnEnable is invoked by reflection too, once, in dependency order — matching
            // what Unity would have called, just driven by hand instead of by the player loop.
            public static Rig Build(int holdTicks, Vector2 spawnPosition, bool includeDoor, bool moveCommandEachTick = false)
            {
                var rig = new Rig { SpawnPosition = spawnPosition };

                rig.RealityRootGo = new GameObject("RealityRoot_A");
                rig.RealityRoot = rig.RealityRootGo.AddComponent<RealityRoot>();
                SetPrivate(rig.RealityRoot, "id", ObserverId.A);
                Invoke(rig.RealityRoot, "Awake");

                rig.CatGo = new GameObject("Cat");
                rig.CatGo.transform.SetParent(rig.RealityRootGo.transform, false);
                rig.CatGo.layer = LayerMask.NameToLayer("RealityA");
                rig.CatBody = rig.CatGo.AddComponent<Rigidbody2D>();
                rig.CatCollider = rig.CatGo.AddComponent<BoxCollider2D>();
                rig.CatCollider.size = new Vector2(1f, 0.56f);
                rig.CatCollider.offset = new Vector2(0f, -0.3f);
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
                Invoke(rig.Death, "Awake"); // constructs `hold` from `config`, now that it's set

                SetPrivate(rig.Rooms, "roomDeath", rig.Death);
                Invoke(rig.Rooms, "OnEnable"); // subscribes to observers.Stepped and roomDeath.Died

                rig.CheckpointMarkerGo = new GameObject("Checkpoint_0");
                rig.CheckpointMarkerGo.transform.SetParent(rig.RealityRootGo.transform, false);
                rig.CheckpointMarkerGo.transform.localPosition = spawnPosition;
                var marker = rig.CheckpointMarkerGo.AddComponent<CheckpointMarker>();
                SetPrivate(marker, "checkpointId", 0);
                SetPrivate(marker, "manager", rig.Checkpoints);
                SetPrivate(marker, "observers", rig.Observers);
                Invoke(marker, "OnEnable"); // registers the spawn with CheckpointManager

                if (includeDoor)
                {
                    rig.RoomDoorGo = new GameObject("Door");
                    rig.RoomDoorGo.transform.SetParent(rig.RealityRootGo.transform, false);
                    rig.Door = rig.RoomDoorGo.AddComponent<RoomDoor>();
                    SetPrivate(rig.Door, "roomId", 0);
                    SetPrivate(rig.Door, "manager", rig.Rooms);
                    Invoke(rig.Door, "OnEnable"); // registers the door with RoomManager
                }

                rig.Driver = new FakeLocalHumanDriver { Observer = rig.ObserverContext, MoveCommandEachTick = moveCommandEachTick };
                rig.ObserverContext.SetDriver(rig.Driver);
                rig.Resettable = new FakeResettable(0);
                rig.Death.ResetRegistry.Register(rig.Resettable);

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

        // ---------- §8.3 Reset parity ----------

        [TestCase(0)]
        [TestCase(30)]
        public void Reset_AfterHold_MatchesResetWithHoldTicksZero(int holdTicks)
        {
            Vector2 spawn = new Vector2(0f, 0f);
            Vector2 deathSpot = new Vector2(20f, 0f);
            Rig rig = Rig.Build(holdTicks, spawn, includeDoor: false, moveCommandEachTick: true);
            try
            {
                rig.CatGo.transform.position = deathSpot;
                RigidbodyConstraints2D preFreeze = RigidbodyConstraints2D.None;
                rig.CatBody.constraints = preFreeze;

                rig.Tick(); // tick 1: nothing happens yet, establishes RoomLifeTick == 0

                rig.Death.Kill(ObserverId.A, DeathCause.Hazard);

                if (holdTicks > 0)
                {
                    Assert.IsTrue(rig.Death.IsHolding, "hold should be active right after a kill with HoldTicks > 0");
                    Assert.IsTrue(rig.Cat.IsFrozen, "cat should be frozen during the hold");
                    Assert.AreEqual(RigidbodyConstraints2D.FreezeAll, rig.CatBody.constraints, "constraints should be FreezeAll while frozen");
                    Assert.AreEqual(Vector2.zero, rig.CatBody.linearVelocity, "velocity should be zero while frozen, even though the driver keeps commanding movement");
                    Assert.AreEqual(deathSpot, (Vector2)rig.CatGo.transform.position, "the cat must not move mid-hold");
                    Assert.AreEqual(0, rig.Resettable.ResetCount, "the room must not reset until the hold ends");

                    for (int i = 1; i < holdTicks; i++)
                    {
                        rig.Tick();
                        Assert.IsTrue(rig.Death.IsHolding, $"still holding at hold-tick {i}");
                        Assert.AreEqual(deathSpot, (Vector2)rig.CatGo.transform.position, $"cat moved mid-hold at tick {i}");
                    }

                    rig.Tick(); // the Hth tick: the deferred reset runs here
                }

                Assert.IsFalse(rig.Death.IsHolding, "hold must have ended by now");
                Assert.AreEqual(1, rig.Resettable.ResetCount, "the trap reset must have run exactly once");
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "one death should be counted");
                Assert.IsFalse(rig.Cat.IsFrozen, "the cat must be unfrozen by the reset");
                Assert.AreEqual(preFreeze, rig.CatBody.constraints, "constraints must be restored to their pre-freeze value");
                Assert.AreEqual(spawn, (Vector2)rig.CatGo.transform.position, "the cat must be back at its checkpoint");

                rig.Tick(); // the first live tick after the reset
                Assert.AreEqual(0, rig.Rooms.RoomLifeTick, "RoomLifeTick must be 0 on the first live tick after the reset");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §8.5 Door during a hold ----------

        [Test]
        public void DoorTouch_DuringHold_DoesNothing()
        {
            const int holdTicks = 5;
            Vector2 spawn = new Vector2(0f, 0f);
            Vector2 doorPosition = new Vector2(10f, 0f);
            Rig rig = Rig.Build(holdTicks, spawn, includeDoor: true);
            try
            {
                SetPrivate(rig.Door, "roomId", 0);
                rig.RoomDoorGo.transform.localPosition = doorPosition;
                rig.CatGo.transform.position = doorPosition; // the cat sits exactly in the door's zone

                bool roomCompleted = false;
                rig.Rooms.RoomCompleted += _ => roomCompleted = true;

                rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
                Assert.IsTrue(rig.Death.IsHolding);

                for (int i = 0; i < holdTicks; i++)
                {
                    rig.Tick();
                    Assert.IsFalse(rig.Rooms.CurrentDoorTouched, $"door must not register as touched during the hold (tick {i})");
                    Assert.IsFalse(roomCompleted, "the room must not complete during the hold");
                }

                Assert.IsFalse(rig.Death.IsHolding, "hold should have ended by now");
                Assert.IsFalse(roomCompleted, "the room must not complete from a hold-time overlap");
            }
            finally { rig.Dispose(); }
        }

        // ---------- Out-of-bounds kill, plus the flipped-gravity collider-centre case (Phase 1b item 2 / C) ----------

        [Test]
        public void OutOfBounds_CatInsideRoomBounds_NoDeath()
        {
            Rig rig = Rig.Build(0, Vector2.zero, includeDoor: false);
            try
            {
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, new Vector2(5f, 0f), new Vector2(4f, 4f)) });
                rig.CatGo.transform.position = new Vector2(5f, 0f); // collider centre at (5, -0.3), well inside
                Physics2D.SyncTransforms(); // Collider2D.bounds only reflects a script-set transform after this

                rig.Tick();

                Assert.AreEqual(0, rig.Death.DeathsIn(0));
            }
            finally { rig.Dispose(); }
        }

        [TestCase(-3f)]
        [TestCase(3f)]
        public void OutOfBounds_InsideOnXY_NonZeroZ_NoDeath(float z)
        {
            // Room bounds are stored/authored in 2D (Vector2 Center/Size); the containment
            // check must ignore z entirely, not just tolerate z == 0.
            Rig rig = Rig.Build(0, Vector2.zero, includeDoor: false);
            try
            {
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, new Vector2(5f, 0f), new Vector2(4f, 4f)) });
                rig.CatGo.transform.position = new Vector3(5f, 0f, z);
                Physics2D.SyncTransforms();

                rig.Tick();

                Assert.AreEqual(0, rig.Death.DeathsIn(0));
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void OutOfBounds_CatOutsideRoomBounds_KillsWithCauseOutOfBounds()
        {
            // HoldTicks 0 so Died fires synchronously and the cause can be observed directly.
            Rig rig = Rig.Build(0, Vector2.zero, includeDoor: false);
            try
            {
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, new Vector2(5f, 0f), new Vector2(4f, 4f)) });
                rig.CatGo.transform.position = new Vector2(20f, 0f);
                Physics2D.SyncTransforms();

                DeathInfo? death = null;
                rig.Death.Died += info => death = info;

                rig.Tick();

                Assert.AreEqual(1, rig.Death.DeathsIn(0));
                Assert.IsTrue(death.HasValue);
                Assert.AreEqual(DeathCause.OutOfBounds, death.Value.Cause);
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void OutOfBounds_WithHoldTicksAboveZero_StartsTheHoldImmediately()
        {
            Rig rig = Rig.Build(10, Vector2.zero, includeDoor: false);
            try
            {
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, new Vector2(5f, 0f), new Vector2(4f, 4f)) });
                rig.CatGo.transform.position = new Vector2(20f, 0f);
                Physics2D.SyncTransforms();

                rig.Tick();

                Assert.AreEqual(1, rig.Death.DeathsIn(0), "the death is counted immediately, at the kill");
                Assert.IsTrue(rig.Death.IsHolding, "the hold must already be running");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void OutOfBounds_FlippedGravity_UsesTheRotatedColliderCentre_NotRootPositionPlusOffset()
        {
            // Collider offset is (0, -0.3) in the cat's local space. Bounds are a tight
            // (5,5)/(1,1) box, i.e. valid on y in [4.5, 5.5].
            // Root at (5, 5.3), unrotated: naive (position + offset) AND true collider centre
            // both land at (5, 5.0) -> inside either way, not a useful case.
            // Root at (5, 5.3), rotated 180 degrees (gravity up): the local offset now points
            // the other way in world space, so the TRUE collider centre is (5, 5.6) -> outside
            // -- while a naive position+offset reading would still say (5, 5.0) -> inside.
            // Only a check based on collider.bounds.center (not transform.position + offset)
            // kills the cat here.
            Rig rig = Rig.Build(0, Vector2.zero, includeDoor: false);
            try
            {
                SetPrivate(rig.Rooms, "bounds", new[] { new RoomBoundsEntry(0, new Vector2(5f, 5f), new Vector2(1f, 1f)) });
                rig.CatGo.transform.position = new Vector2(5f, 5.3f);
                rig.CatGo.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                Physics2D.SyncTransforms();

                Vector2 naiveCentre = (Vector2)rig.CatGo.transform.position + rig.CatCollider.offset;
                Vector2 trueCentre = rig.CatCollider.bounds.center;
                Assert.That(naiveCentre.x, Is.EqualTo(5f).Within(.001f), "sanity: the naive reading lands inside");
                Assert.That(naiveCentre.y, Is.EqualTo(5.0f).Within(.001f), "sanity: the naive reading lands inside");
                Assert.That(trueCentre.x, Is.EqualTo(5f).Within(.001f), "sanity: the true rotated collider centre lands outside");
                Assert.That(trueCentre.y, Is.EqualTo(5.6f).Within(.001f), "sanity: the true rotated collider centre lands outside");

                rig.Tick();

                Assert.AreEqual(1, rig.Death.DeathsIn(0), "the kill must follow the true (rotated) collider centre, not the naive position+offset one");
            }
            finally { rig.Dispose(); }
        }
    }
}
