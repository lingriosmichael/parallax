using System.Collections;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-090 (D-091), acceptance question 3: in real Play mode (the player loop's FixedUpdate, physics and frames;
    /// the project enters Play mode without a domain reload), each rewind calls CatRespawn.RespawnAt once, on a later frame
    /// than anything before it, so its same-frame guard never drops it, and the cat ends at the section's checkpoint. Two
    /// deaths after the gate: the second respawn is the one the route harness can't do in one rig. Everything is built at
    /// runtime under one inactive root, wired, then activated, so each component's own Awake/OnEnable runs.</summary>
    public sealed class CheckpointSectionPlayModeTests
    {
        const float Stand = .4f;   // body y standing on y 0: the default collider (offset -0.12, 0.56 tall) has its bottom at -0.4

        sealed class IdleDriver : IObserverDriver
        {
            public ObserverContext Observer;
            public InputSourceKind Kind => InputSourceKind.LocalHuman;
            public void Activate(ObserverContext observer) { }
            public void Deactivate() { }
            public void FixedTick(int tick) => Observer.Cat.Step(CatCommand.None, Time.fixedDeltaTime);
        }

        // PAX-090 §12: stands in for every MonoBehaviour on the cat. Toggling Rigidbody2D.simulated fires no lifecycle message;
        // a SetActive or component toggle would fire OnDisable/OnEnable on all of them, and this counts it.
        public sealed class LifecycleProbe : MonoBehaviour
        {
            public int Enables, Disables;
            void OnEnable() => Enables++;
            void OnDisable() => Disables++;
        }

        static GameObject Child(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("RealityA") };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go;
        }

        [UnityTest]
        public IEnumerator InPlayMode_EachRewind_RespawnsTheCatOnce_AtTheSectionCheckpoint()
        {
            // The Editor barely advances Play mode while it's in the background; this keeps the player loop running for
            // this test only (a runtime property, not Player Settings), restored in the teardown.
            runInBackground = Application.runInBackground;
            Application.runInBackground = true;
            yield return new EnterPlayMode();

            var rig = new GameObject("PlayRig");
            rig.SetActive(false);
            try
            {
                GameObject realityGo = Child(rig.transform, "RealityRoot_A", Vector2.zero);
                RealityRoot reality = realityGo.AddComponent<RealityRoot>();
                PauseTestRig.SetPrivate(reality, "id", ObserverId.A);
                Child(realityGo.transform, "Floor", new Vector2(0f, -.5f)).AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

                GameObject catGo = Child(realityGo.transform, "Cat", new Vector2(0f, Stand));
                Rigidbody2D body = catGo.AddComponent<Rigidbody2D>();
                BoxCollider2D collider = catGo.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, .56f);
                collider.offset = new Vector2(0f, -.12f);
                catGo.AddComponent<GravityReceiver>();
                CatMotor2D cat = catGo.AddComponent<CatMotor2D>();
                PauseTestRig.SetPrivate(cat, "config", AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset"));
                CatRespawn respawn = catGo.AddComponent<CatRespawn>();
                LifecycleProbe probe = catGo.AddComponent<LifecycleProbe>();

                GameObject systems = Child(rig.transform, "Systems", Vector2.zero);
                CheckpointManager checkpoints = systems.AddComponent<CheckpointManager>();
                PauseTestRig.SetPrivate(checkpoints, "rootA", reality);
                ObserverSet observers = Child(rig.transform, "Observers", Vector2.zero).AddComponent<ObserverSet>();
                ObserverContext observer = Child(observers.transform, "Observer_A", Vector2.zero).AddComponent<ObserverContext>();
                PauseTestRig.SetPrivate(observer, "id", ObserverId.A);
                PauseTestRig.SetPrivate(observer, "reality", reality);
                PauseTestRig.SetPrivate(observer, "cat", cat);
                PauseTestRig.SetPrivate(observers, "observerA", observer);

                RoomManager rooms = systems.AddComponent<RoomManager>();
                RoomDeath death = systems.AddComponent<RoomDeath>();
                var safety = ScriptableObject.CreateInstance<RoomSafetyConfig>();
                PauseTestRig.SetPrivate(safety, "holdTicks", 30);
                PauseTestRig.SetPrivate(death, "config", safety);
                PauseTestRig.SetPrivate(death, "checkpoints", checkpoints);
                PauseTestRig.SetPrivate(death, "rooms", rooms);
                PauseTestRig.SetPrivate(death, "observers", observers);
                PauseTestRig.SetPrivate(rooms, "soloReality", ObserverId.A);
                PauseTestRig.SetPrivate(rooms, "checkpoints", checkpoints);
                PauseTestRig.SetPrivate(rooms, "observers", observers);
                PauseTestRig.SetPrivate(rooms, "roomDeath", death);

                CheckpointMarker start = Child(realityGo.transform, "Checkpoint_0", new Vector2(0f, Stand)).AddComponent<CheckpointMarker>();
                PauseTestRig.SetPrivate(start, "manager", checkpoints);
                PauseTestRig.SetPrivate(start, "observers", observers);
                RoomDoor door = Child(realityGo.transform, "Door", new Vector2(50f, 1f)).AddComponent<RoomDoor>();
                PauseTestRig.SetPrivate(door, "manager", rooms);
                SpriteRenderer marker = Child(realityGo.transform, "Gate_1_Marker", new Vector2(6f, 1f)).AddComponent<SpriteRenderer>();
                Vector2 spawn = new(6f, Stand);
                PauseTestRig.SetPrivate(rooms, "sections", new[]
                {
                    new RoomSectionEntry(0, "Start", Vector2.zero, Vector2.zero, new Vector2(0f, Stand), Vector2.down, null),
                    new RoomSectionEntry(0, "Gate_1", new Vector2(5f, 1f), new Vector2(1f, 4f), spawn, Vector2.down, marker),
                });

                LogAssert.Expect(LogType.Warning, "ObserverSet 'Observers' has no observerB assigned.");
                rig.SetActive(true);
                observer.SetDriver(new IdleDriver { Observer = observer });
                int respawns = 0;
                respawn.Respawned += () => respawns++;

                void Teleport(Vector2 p) { body.position = p; catGo.transform.position = p; body.linearVelocity = Vector2.zero; }
                // The test runner's coroutine steps per frame, so wait on the game's own tick (ObserverSet.FixedUpdate).
                IEnumerator Ticks(int n)
                {
                    int until = observers.Tick + n;
                    for (int frames = 0; observers.Tick < until && frames < 2000; frames++) yield return null;
                    Assert.GreaterOrEqual(observers.Tick, until, "the player loop ran the fixed ticks");
                }

                yield return Ticks(5);
                Assert.AreEqual((1, 0), (probe.Enables, probe.Disables), "precondition: enabled once by the activation");
                Teleport(new Vector2(5f, Stand));
                yield return Ticks(2);
                Assert.AreEqual(1, rooms.CurrentSection, $"the gate was passed (tick {observers.Tick}, room tick {rooms.RoomLifeTick}, body {body.position}, bounds {collider.bounds}, timeScale {Time.timeScale}, playing {Application.isPlaying}, active {rig.activeInHierarchy}, enabled {rooms.isActiveAndEnabled})");

                for (int n = 1; n <= 2; n++)
                {
                    Teleport(new Vector2(12f + n, Stand));
                    yield return Ticks(1);
                    int killFrame = Time.frameCount;
                    death.Kill(ObserverId.A, DeathCause.Hazard);
                    Assert.IsTrue(death.IsHolding, $"death {n}: holding");
                    for (int frames = 0; death.IsHolding && frames < 5000; frames++) yield return null;
                    Assert.IsFalse(death.IsHolding, $"death {n}: the hold ended");
                    Assert.AreEqual(n, respawns, $"death {n}: exactly one respawn per death");
                    Assert.Greater(Time.frameCount, killFrame, $"death {n}: the respawn is on a later frame than the kill");
                    yield return Ticks(3);
                    Assert.AreEqual(spawn.x, body.position.x, 1e-3f, $"death {n}: at the section checkpoint's x");
                    Assert.AreEqual(spawn.y, body.position.y, .02f, $"death {n}: standing at the section checkpoint");
                    Assert.AreEqual(1, rooms.CurrentSection, $"death {n}: still in the section");
                    Assert.AreEqual(0, checkpoints.Current, $"death {n}: the room checkpoint is unchanged");
                    Assert.AreEqual((1, 0), (probe.Enables, probe.Disables), $"death {n}: no OnEnable/OnDisable on the cat during the rewind (§12)");
                    Assert.IsTrue(body.simulated, $"death {n}: the body is simulated again");
                }
            }
            finally
            {
                Object.Destroy(rig);
            }
        }

        bool runInBackground;

        // Always back to Edit mode, even after a failed assertion.
        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            Application.runInBackground = runInBackground;
        }
    }
}
