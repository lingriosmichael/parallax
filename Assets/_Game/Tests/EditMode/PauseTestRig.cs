using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073): shared SetUp/TearDown for every pause test. Swaps the two global
    /// seams the pause touches - RunningState.SetTimeScale and LevelSceneLoader's CanLoad/LoadScene -
    /// for recorders, so no test ever writes the real Time.timeScale or loads a scene, and restores
    /// the originals afterwards.</summary>
    public abstract class PauseTestBase
    {
        protected readonly List<float> TimeScaleWrites = new List<float>();
        protected readonly List<string> LoadedScenes = new List<string>();
        protected readonly List<string> CallOrder = new List<string>();
        protected bool CanLoadResult = true;

        Action<float> originalSetTimeScale;
        Func<string, bool> originalCanLoad;
        Action<string> originalLoadScene;

        [SetUp]
        public void SwapSeams()
        {
            originalSetTimeScale = RunningState.SetTimeScale;
            originalCanLoad = LevelSceneLoader.CanLoad;
            originalLoadScene = LevelSceneLoader.LoadScene;
            TimeScaleWrites.Clear();
            LoadedScenes.Clear();
            CallOrder.Clear();
            CanLoadResult = true;
            RunningState.SetTimeScale = value => { TimeScaleWrites.Add(value); CallOrder.Add("timeScale=" + value); };
            LevelSceneLoader.CanLoad = _ => CanLoadResult;
            LevelSceneLoader.LoadScene = name => { LoadedScenes.Add(name); CallOrder.Add("load " + name); };
        }

        [TearDown]
        public void RestoreSeams()
        {
            RunningState.SetTimeScale = originalSetTimeScale;
            LevelSceneLoader.CanLoad = originalCanLoad;
            LevelSceneLoader.LoadScene = originalLoadScene;
        }
    }

    /// <summary>PAX-054: a minimal solo level built by hand (no scene), in the same GameObject-rig
    /// pattern as RoomDeathHoldTests/LevelsButtonFlowTests: Awake/OnEnable are invoked by
    /// reflection because EditMode does not run the player loop, and ObserverSet.FixedUpdate is
    /// invoked once per simulated tick. Unlike those rigs this one uses the real input path
    /// (CatInputRouter + KeyboardCatInput + TouchStickCatInput + LocalHumanDriver) when asked, and
    /// wires a LevelPause into ObserverSet's pause gate.</summary>
    public sealed class PauseTestRig
    {
        public sealed class CommandingDriver : IObserverDriver
        {
            public ObserverContext Observer;
            public InputSourceKind Kind => InputSourceKind.LocalHuman;
            public void Activate(ObserverContext observer) { }
            public void Deactivate() { }
            public void FixedTick(int tick)
            {
                var command = new CatCommand { Move = 1f };
                Observer.Cat.Step(in command, 0.02f);
            }
        }

        public sealed class CountingTrap : RoomTrap
        {
            public int Steps;
            protected override void OnReset() { }
            protected override void OnLiveRoomStep() => Steps++;
        }

        readonly List<GameObject> roots = new List<GameObject>();

        public GameObject CatGo, DoorGo, PanelGo, PauseButtonGo;
        public CatMotor2D Cat;
        public Rigidbody2D CatBody;
        public ObserverContext Observer;
        public ObserverSet Observers;
        public CheckpointManager Checkpoints;
        public RoomManager Rooms;
        public RoomDeath Death;
        public RoomDoor Door;
        public CatInputRouter Router;
        public KeyboardCatInput Keyboard;
        public TouchStickCatInput Touch;
        public LevelPause Pause;
        public PausePanel Panel;
        public PauseButton PauseButton;
        public CountingTrap Trap;

        static readonly MethodInfo ObserverSetFixedUpdate =
            typeof(ObserverSet).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

        public void Tick() => ObserverSetFixedUpdate.Invoke(Observers, null);

        /// <param name="realInput">true: LocalHumanDriver over the real router and sources.
        /// false: a driver that commands Move = 1 every tick.</param>
        public static PauseTestRig Build(bool realInput, int holdTicks = 30, bool withTrap = false)
        {
            var rig = new PauseTestRig();

            GameObject realityGo = rig.Root("RealityRoot_A");
            var reality = realityGo.AddComponent<RealityRoot>();
            SetPrivate(reality, "id", ObserverId.A);
            Invoke(reality, "Awake");

            rig.CatGo = new GameObject("Cat");
            rig.CatGo.transform.SetParent(realityGo.transform, false);
            rig.CatGo.layer = LayerMask.NameToLayer("RealityA");
            rig.CatBody = rig.CatGo.AddComponent<Rigidbody2D>();
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

            rig.Observer = rig.Root("ObserverA").AddComponent<ObserverContext>();
            SetPrivate(rig.Observer, "id", ObserverId.A);
            SetPrivate(rig.Observer, "reality", reality);
            SetPrivate(rig.Observer, "cat", rig.Cat);
            Invoke(rig.Observer, "Awake");

            rig.Observers = rig.Root("Observers").AddComponent<ObserverSet>();
            SetPrivate(rig.Observers, "observerA", rig.Observer);
            Invoke(rig.Observers, "Awake");

            rig.Checkpoints = rig.Root("Checkpoints").AddComponent<CheckpointManager>();
            SetPrivate(rig.Checkpoints, "rootA", reality);

            rig.Rooms = rig.Root("RoomManager").AddComponent<RoomManager>();
            SetPrivate(rig.Rooms, "soloReality", ObserverId.A);
            SetPrivate(rig.Rooms, "checkpoints", rig.Checkpoints);
            SetPrivate(rig.Rooms, "observers", rig.Observers);

            rig.Death = rig.Root("RoomDeath").AddComponent<RoomDeath>();
            SetPrivate(rig.Death, "checkpoints", rig.Checkpoints);
            SetPrivate(rig.Death, "rooms", rig.Rooms);
            SetPrivate(rig.Death, "observers", rig.Observers);
            var safety = ScriptableObject.CreateInstance<RoomSafetyConfig>();
            SetPrivate(safety, "holdTicks", holdTicks);
            SetPrivate(rig.Death, "config", safety);
            Invoke(rig.Death, "Awake");

            SetPrivate(rig.Rooms, "roomDeath", rig.Death);
            Invoke(rig.Rooms, "OnEnable");

            var markerGo = new GameObject("Checkpoint_0");
            markerGo.transform.SetParent(realityGo.transform, false);
            var marker = markerGo.AddComponent<CheckpointMarker>();
            SetPrivate(marker, "checkpointId", 0);
            SetPrivate(marker, "manager", rig.Checkpoints);
            SetPrivate(marker, "observers", rig.Observers);
            Invoke(marker, "OnEnable");

            // The door sits far from the spawn, so the room only completes when a test moves the
            // cat onto it (CompleteLevel).
            rig.DoorGo = new GameObject("Door");
            rig.DoorGo.transform.SetParent(realityGo.transform, false);
            rig.DoorGo.transform.localPosition = new Vector2(50f, 0f);
            rig.Door = rig.DoorGo.AddComponent<RoomDoor>();
            SetPrivate(rig.Door, "roomId", 0);
            SetPrivate(rig.Door, "manager", rig.Rooms);
            Invoke(rig.Door, "OnEnable");

            if (withTrap)
            {
                var trapGo = new GameObject("Trap");
                trapGo.transform.SetParent(realityGo.transform, false);
                rig.Trap = trapGo.AddComponent<CountingTrap>();
                SetPrivate(rig.Trap, "roomId", 0);
                SetPrivate(rig.Trap, "roomDeath", rig.Death);
                SetPrivate(rig.Trap, "rooms", rig.Rooms);
                Invoke(rig.Trap, "Awake");
                Invoke(rig.Trap, "OnEnable");
            }

            GameObject inputGo = rig.Root("DeviceInput");
            rig.Keyboard = inputGo.AddComponent<KeyboardCatInput>();
            rig.Touch = inputGo.AddComponent<TouchStickCatInput>();
            Invoke(rig.Touch, "Awake");
            rig.Router = inputGo.AddComponent<CatInputRouter>();
            SetPrivate(rig.Router, "sources", new MonoBehaviour[] { rig.Touch, rig.Keyboard });
            Invoke(rig.Router, "Awake");

            GameObject hud = rig.Root("HUD", typeof(RectTransform));
            rig.PanelGo = new GameObject("PausePanel", typeof(RectTransform));
            rig.PanelGo.transform.SetParent(hud.transform, false);
            rig.Panel = rig.PanelGo.AddComponent<PausePanel>();
            rig.PanelGo.SetActive(false);
            rig.PauseButtonGo = new GameObject("PauseButton", typeof(RectTransform));
            rig.PauseButtonGo.transform.SetParent(hud.transform, false);
            rig.PauseButton = rig.PauseButtonGo.AddComponent<PauseButton>();

            rig.Pause = rig.Root("LevelPause").AddComponent<LevelPause>();
            SetPrivate(rig.Pause, "rooms", rig.Rooms);
            SetPrivate(rig.Pause, "router", rig.Router);
            SetPrivate(rig.Pause, "panel", rig.PanelGo);
            SetPrivate(rig.Panel, "levelPause", rig.Pause);
            SetPrivate(rig.PauseButton, "levelPause", rig.Pause);
            SetPrivate(rig.Observers, "pauseGate", rig.Pause);

            if (realInput)
            {
                rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
            }
            else
            {
                rig.Observer.SetDriver(new CommandingDriver { Observer = rig.Observer });
            }

            rig.CatGo.transform.position = Vector2.zero;
            Physics2D.SyncTransforms();
            return rig;
        }

        GameObject Root(string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            roots.Add(go);
            return go;
        }

        public void CompleteLevel()
        {
            CatGo.transform.position = DoorGo.transform.position;
            Physics2D.SyncTransforms();
            Tick();
        }

        public void Dispose()
        {
            foreach (GameObject go in roots) if (go != null) Object.DestroyImmediate(go);
        }

        public static void SetPrivate(object target, string field, object value)
        {
            FieldInfo f = FindField(target.GetType(), field);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        public static T GetPrivate<T>(object target, string field)
        {
            FieldInfo f = FindField(target.GetType(), field);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            return (T)f.GetValue(target);
        }

        static FieldInfo FindField(Type type, string field)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f;
            }
            return null;
        }

        public static void Invoke(object target, string method, params object[] args)
        {
            MethodInfo m = null;
            for (Type t = target.GetType(); t != null && m == null; t = t.BaseType)
                m = t.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(m, $"{target.GetType().Name}.{method} not found");
            m.Invoke(target, args.Length == 0 ? null : args);
        }
    }
}
