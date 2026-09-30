using System;
using System.Collections.Generic;
using System.Reflection;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0 (§11 R8): the capture harness's rig, built from the same public parts as the route
    /// harness (RouteReplay.cs is not edited): the room by SoloRoomBuilder, the real Cat_Player.prefab, the gameplay part of
    /// _LevelTemplate, and a scripted command source queued into the real CatInputRouter. One tick is
    /// ObserverSet.FixedUpdate then Physics2D.Simulate, as the player loop runs it. Lives in a RouteSession's scene.</summary>
    sealed class CatCaptureRig
    {
        const string CatPrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string MotorConfigPath = "Assets/_Game/Data/CatMotorConfig_Default.asset";
        const string SafetyConfigPath = "Assets/_Game/Data/RoomSafetyConfig.asset";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo ObserverSetFixedUpdate = typeof(ObserverSet).GetMethod("FixedUpdate", Instance);

        public ObserverSet Observers; public RoomManager Rooms; public RoomDeath Death; public CheckpointManager Checkpoints;
        public RealityRoot Root; public CatMotor2D Cat; public Rigidbody2D Body; public Collider2D CatCollider; public GravityReceiver Gravity;
        public CatVisualPresenter Presenter; public SpriteRenderer BodyRenderer; public CatRespawn Respawn;
        public CatMotorConfig Motor; public Vector2 Origin; public int Tick;
        public Vector3 VisualRest;   // item 3: the Visual's authored local position (its rest pose), read before any frame
        public int Respawns;
        ScriptedInput input;

        public static CatCaptureRig Build(SoloRoomDefinition room, float? startX)
        {
            var rig = new CatCaptureRig();
            var changes = new List<string>();
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A));
            CatMotorConfig motor = Load<CatMotorConfig>(MotorConfigPath);
            RoomSafetyConfig safety = Load<RoomSafetyConfig>(SafetyConfigPath);
            GameObject catPrefab = Load<GameObject>(CatPrefabPath);

            var rootGo = new GameObject("RealityRoot_A") { layer = layer };
            rig.Root = rootGo.AddComponent<RealityRoot>();
            Set(rig.Root, "id", ObserverId.A);

            var systems = new GameObject("Systems");
            rig.Checkpoints = systems.AddComponent<CheckpointManager>();
            rig.Rooms = systems.AddComponent<RoomManager>();
            rig.Death = systems.AddComponent<RoomDeath>();

            var observersGo = new GameObject("Observers");
            rig.Observers = observersGo.AddComponent<ObserverSet>();
            var observerGo = new GameObject("Observer_A");
            observerGo.transform.SetParent(observersGo.transform, false);
            ObserverContext observer = observerGo.AddComponent<ObserverContext>();

            GameObject catGo = Object.Instantiate(catPrefab, rootGo.transform);
            catGo.name = "Cat_A";
            foreach (Transform t in catGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            rig.Cat = catGo.GetComponent<CatMotor2D>();
            rig.Body = catGo.GetComponent<Rigidbody2D>();
            rig.CatCollider = CatBodyCollider.Of(rig.Cat);
            rig.Gravity = catGo.GetComponent<GravityReceiver>();
            rig.Respawn = catGo.GetComponent<CatRespawn>();
            rig.Presenter = catGo.GetComponentInChildren<CatVisualPresenter>(true);
            if (rig.Presenter == null) throw new InvalidOperationException("CatCapture: Cat_Player.prefab has no CatVisualPresenter.");
            rig.BodyRenderer = rig.Presenter.GetComponent<SpriteRenderer>();
            rig.VisualRest = rig.Presenter.transform.localPosition;
            rig.Motor = motor;

            Set(rig.Checkpoints, "rootA", rig.Root);
            Set(rig.Rooms, "soloReality", ObserverId.A);
            Set(rig.Rooms, "checkpoints", rig.Checkpoints);
            Set(rig.Rooms, "observers", rig.Observers);
            Set(rig.Rooms, "roomDeath", rig.Death);
            Set(rig.Death, "checkpoints", rig.Checkpoints);
            Set(rig.Death, "rooms", rig.Rooms);
            Set(rig.Death, "observers", rig.Observers);
            Set(rig.Death, "config", safety);
            Set(observer, "id", ObserverId.A);
            Set(observer, "reality", rig.Root);
            Set(observer, "cat", rig.Cat);
            Set(rig.Observers, "observerA", observer);

            var inputGo = new GameObject("DeviceInput");
            CatInputRouter router = inputGo.AddComponent<CatInputRouter>();

            // The room, exactly as LevelSetup.BuildRoomInScene (and the route harness) builds it.
            Transform parent = SetupUtility.EnsureChild(rootGo.transform, "Rooms_PAX043", layer, changes);
            SoloRoomBuilder.BuildRoom(parent, rig.Root, room, rig.Checkpoints, rig.Rooms, rig.Death, rig.Observers, motor, changes);
            Transform roomRoot = parent.Find($"Room_{room.Id + 1}");
            if (roomRoot == null) throw new InvalidOperationException($"CatCapture: SoloRoomBuilder built no Room_{room.Id + 1} (see the console).");
            SoloRoomBuilder.AssignBounds(rig.Rooms, new[] { room }, rig.Root, safety, changes);

            SoloRoomElement checkpoint = Array.Find(room.Elements, e => e.Kind == SoloRoomElementKind.Checkpoint);
            rig.Origin = rig.Root.ToWorld(room.Origin);
            Vector2 start = room.Origin + checkpoint.Position + new Vector2(0f, -motor.ColliderBottom);
            catGo.transform.position = rig.Root.ToWorld(start);

            // Lifecycle, in dependency order, as a loaded scene would run it (EditMode runs none of it). The presenter's
            // Awake runs after the motor's, as the prefab's order has it; it needs no observer (R9).
            Invoke(rig.Root, "Awake");
            Invoke(rig.Gravity, "Awake");
            Invoke(rig.Cat, "Awake");
            Invoke(rig.Respawn, "Awake");
            Invoke(rig.Presenter, "Awake");
            // PAX-V07 item 6: the presenter's scene signals subscribe to the cat's respawn in OnEnable (EditMode runs none).
            foreach (CatPresentationSignals signals in catGo.GetComponentsInChildren<CatPresentationSignals>(true)) Invoke(signals, "OnEnable");
            Invoke(observer, "Awake");
            Invoke(rig.Death, "Awake");
            Invoke(router, "Awake");
            rig.input = new ScriptedInput();
            ((List<ICatCommandSource>)typeof(CatInputRouter).GetField("validSources", Instance).GetValue(router)).Add(rig.input);
            Invoke(rig.Rooms, "OnEnable");
            foreach (MonoBehaviour behaviour in roomRoot.GetComponentsInChildren<MonoBehaviour>(true)) { Invoke(behaviour, "Awake"); Invoke(behaviour, "OnEnable"); }
            if (room.Id != 0) rig.Checkpoints.Activate(room.Id);
            observer.SetDriver(new LocalHumanDriver(router));
            if (rig.Respawn != null) rig.Respawn.Respawned += () => rig.Respawns++;

            // Scenario start: the cat standing at `startX` (room-local, its collider centre) instead of the checkpoint.
            if (startX.HasValue)
                catGo.transform.position = rig.Root.ToWorld(new Vector2(room.Origin.x + startX.Value, start.y));
            Physics2D.SyncTransforms();
            return rig;
        }

        /// <summary>One tick with this screen-relative command (D-049, the same projection as the keyboard and the route
        /// harness): ObserverSet.FixedUpdate, then Physics2D.Simulate.</summary>
        public void Step(CatCommand screen)
        {
            Vector2 up = -Gravity.Direction;
            Vector2 catRight = new(up.y, -up.x);
            screen.Move = VirtualStick.ToMove(new Vector2(screen.Move, 0f), catRight, StickProjection.ScreenRelative);
            input.Queue(screen);
            Tick++;
            ObserverSetFixedUpdate.Invoke(Observers, null);
            if (Observers.Tick != Tick) throw new InvalidOperationException($"CatCapture: ObserverSet.Tick is {Observers.Tick} at harness tick {Tick}.");
            if (!Physics2D.Simulate(Time.fixedDeltaTime))
                throw new InvalidOperationException("CatCapture: Physics2D.Simulate did not run (simulationMode must be Script; RouteSession sets it).");
        }

        public Vector2 ColliderCentre => CatCollider.bounds.center;

        // The command source. Read() returns this tick's command and clears the edges, as ICatCommandSource requires.
        sealed class ScriptedInput : ICatCommandSource
        {
            CatCommand next;
            public void Queue(CatCommand command) => next = command;
            public CatCommand Read() { CatCommand c = next; next.JumpPressed = false; next.InteractPressed = false; return c; }
            public void ResetTransientState() => next = CatCommand.None;
        }

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"CatCapture: {path} not found.");
            return asset;
        }

        static void Set(object target, string name, object value)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, Instance | BindingFlags.DeclaredOnly);
                if (f == null) continue;
                f.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }

        static void Invoke(Component target, string method)
        {
            if (target == null) return;
            for (Type t = target.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                MethodInfo m = t.GetMethod(method, Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (m == null) continue;
                m.Invoke(target, null);
                return;
            }
        }
    }
}
