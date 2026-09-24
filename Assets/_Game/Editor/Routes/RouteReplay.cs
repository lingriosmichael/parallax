using System;
using System.Collections.Generic;
using System.Reflection;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Routes
{
    public sealed class ReplayOptions
    {
        public int TimedStep = -1;          // the swept step (index into Route.Steps), or -1
        public TimedMode Mode;
        public int Delta;                   // Shift: +d holds the previous command d more ticks, -d starts d ticks early. Hesitate: +d idle ticks.
        public int AuthoredStartTick = -1;  // the swept step's start tick in the authored replay (needed for Shift -d)
        public bool ResolveCause = true;    // after a kill, run the death hold until RoomDeath reports the cause
        public int MaxTicks = 1500;
        // Start-state override (R18): the cat's collider centre, room-local, and its velocity before tick 1.
        public Vector2? StartCentre;
        public Vector2 StartVelocity;
    }

    // PAX-075 (D-079): replays a route through the real game code, one tick at a time, in the session's
    // empty scene. The room is built by SoloRoomBuilder like a shipped level, the cat is Cat_Player.prefab,
    // and the rest is the gameplay part of _LevelTemplate (§11 R2). Per tick, as the player loop does:
    // ObserverSet.FixedUpdate (LocalHumanDriver -> CatInputRouter -> CatMotor2D, then RoomManager's traps ->
    // hazards -> bounds -> door), then Physics2D.Simulate(Time.fixedDeltaTime). Nothing here decides whether
    // a kill happened; the harness only reads RoomDeath and names the killer afterwards (R6).
    public static class RouteHarness
    {
        const string CatPrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string MotorConfigPath = "Assets/_Game/Data/CatMotorConfig_Default.asset";
        const string SafetyConfigPath = "Assets/_Game/Data/RoomSafetyConfig.asset";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly MethodInfo ObserverSetFixedUpdate = typeof(ObserverSet).GetMethod("FixedUpdate", Instance);

        public static ReplayResult Replay(RouteSession session, SoloRoomDefinition room, Route route, ReplayOptions options = null)
        {
            options ??= new ReplayOptions();
            session.Clear();
            try
            {
                Rig rig = Rig.Build(room, options);
                return Run(rig, route, options);
            }
            finally { session.Clear(); }
        }

        static ReplayResult Run(Rig rig, Route route, ReplayOptions options)
        {
            var result = new ReplayResult { Route = route.Name };
            foreach (Element e in rig.Elements) result.Elements.Add(e.Name);
            result.Elements.Add(R.CatGravity);
            result.Records.Add(rig.Snapshot(0, default));
            var view = new RouteView { Result = result };
            var runner = new RouteRunner(route, options, result);

            if (options.TimedStep >= 0 && options.Mode == TimedMode.Shift && options.Delta < 0 && options.AuthoredStartTick + options.Delta < 1)
            {
                result.Failure = $"forced start at tick {options.AuthoredStartTick + options.Delta}, before tick 1";
                return result;
            }

            for (int k = 1; ; k++)
            {
                if (k > options.MaxTicks) { result.Failure = $"tick cap {options.MaxTicks} reached"; break; }
                if (!runner.Next(view, k, out CatCommand command)) break;
                rig.Input.Queue(rig.ToCatFrame(command));

                ObserverSetFixedUpdate.Invoke(rig.Observers, null);
                if (rig.Observers.Tick != k) throw new InvalidOperationException($"RouteHarness: ObserverSet.Tick is {rig.Observers.Tick} at harness tick {k}.");
                bool killed = rig.Death.WasKilledThisTick(ObserverId.A, k);
                if (killed) result.Kill = new KillInfo { Tick = k, Candidates = rig.Attribute() };
                rig.RecordArrowLethal(result, k);

                if (!Physics2D.Simulate(Time.fixedDeltaTime))
                    throw new InvalidOperationException("RouteHarness: Physics2D.Simulate did not run in EditMode with simulationMode = Script (PAX-075 R1 stop).");
                TickRecord record = rig.Snapshot(k, command);
                result.Records.Add(record);

                if (result.Kill != null)
                {
                    if (options.ResolveCause) ResolveCause(rig, result.Kill);
                    break;
                }
                if (route.Goal.Test(view)) { result.Completed = true; break; }
            }
            return result;
        }

        // The death hold keeps the room frozen; RoomDeath reports the cause when it resets (D-058).
        static void ResolveCause(Rig rig, KillInfo kill)
        {
            for (int i = 0; i < 120 && !rig.DeathReported; i++)
            {
                rig.Input.Queue(default);
                ObserverSetFixedUpdate.Invoke(rig.Observers, null);
                Physics2D.Simulate(Time.fixedDeltaTime);
            }
            if (!rig.DeathReported) return;
            kill.CauseKnown = true;
            kill.Cause = rig.DeathCause;
        }

        sealed class Element
        {
            public string Name; public GameObject Go; public SpriteRenderer[] Renderers;
            public RoomTrap Trap; public Hazard Hazard; public BoxCollider2D Box;
            public FallingBlockTrap Block; public Vector2 BlockLanded;
            public MovingTrap Moving; public bool MovingHazard; public float MovingStep, CrushDepth;
            public ArrowTrap Arrow; public SpriteRenderer ArrowSprite; public float ArrowLength, ArrowThickness; public int ArrowTell;
        }

        sealed class Rig
        {
            public ObserverSet Observers; public RoomManager Rooms; public RoomDeath Death; public CheckpointManager Checkpoints;
            public RealityRoot Root; public CatMotor2D Cat; public Rigidbody2D Body; public Collider2D CatCollider; public GravityReceiver Gravity;
            public CatMotorConfig Motor; public Vector2 Origin;
            public ScriptedInput Input;
            public readonly List<Element> Elements = new();
            public bool DeathReported; public DeathCause DeathCause;
            ContactFilter2D filter;
            readonly RaycastHit2D[] hits = new RaycastHit2D[8];
            readonly Collider2D[] overlaps = new Collider2D[16];

            public static Rig Build(SoloRoomDefinition room, ReplayOptions options)
            {
                var rig = new Rig();
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

                // The cat: the prefab with the template instance's overrides (name, RealityA layer).
                GameObject catGo = Object.Instantiate(catPrefab, rootGo.transform);
                catGo.name = "Cat_A";
                foreach (Transform t in catGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                rig.Cat = catGo.GetComponent<CatMotor2D>();
                rig.Body = catGo.GetComponent<Rigidbody2D>();
                rig.CatCollider = catGo.GetComponent<Collider2D>();
                rig.Gravity = catGo.GetComponent<GravityReceiver>();
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

                // Input through the real router. An Editor-assembly MonoBehaviour can't be added to a scene object,
                // so the scripted source is a plain ICatCommandSource added to validSources after Awake (R2, R24).
                var inputGo = new GameObject("DeviceInput");
                CatInputRouter router = inputGo.AddComponent<CatInputRouter>();

                // The room, exactly as LevelSetup.BuildRoomInScene builds a level.
                Transform parent = SetupUtility.EnsureChild(rootGo.transform, "Rooms_PAX043", layer, changes);
                SoloRoomBuilder.BuildRoom(parent, rig.Root, room, rig.Checkpoints, rig.Rooms, rig.Death, rig.Observers, motor, changes);
                Transform roomRoot = parent.Find($"Room_{room.Id + 1}");
                if (roomRoot == null) throw new InvalidOperationException($"RouteHarness: SoloRoomBuilder built no Room_{room.Id + 1} (see the console).");
                SoloRoomBuilder.AssignBounds(rig.Rooms, new[] { room }, rig.Root, safety, changes);

                SoloRoomElement checkpoint = Array.Find(room.Elements, e => e.Kind == SoloRoomElementKind.Checkpoint);
                rig.Origin = rig.Root.ToWorld(room.Origin);
                catGo.transform.position = rig.Root.ToWorld(room.Origin + checkpoint.Position + new Vector2(0f, -motor.ColliderBottom));

                // Lifecycle, in dependency order, as a loaded scene would run it (EditMode runs none of it).
                Invoke(rig.Root, "Awake");
                Invoke(rig.Gravity, "Awake");
                Invoke(rig.Cat, "Awake");
                Invoke(catGo.GetComponent<CatRespawn>(), "Awake");
                Invoke(observer, "Awake");
                Invoke(rig.Death, "Awake");
                Invoke(router, "Awake");
                var scripted = new ScriptedInput();
                ((List<ICatCommandSource>)typeof(CatInputRouter).GetField("validSources", Instance).GetValue(router)).Add(scripted);
                rig.Input = scripted;
                Invoke(rig.Rooms, "OnEnable");
                foreach (MonoBehaviour behaviour in roomRoot.GetComponentsInChildren<MonoBehaviour>(true)) { Invoke(behaviour, "Awake"); Invoke(behaviour, "OnEnable"); }
                if (room.Id != 0) rig.Checkpoints.Activate(room.Id);
                observer.SetDriver(new LocalHumanDriver(router));
                rig.Death.Died += info => { rig.DeathReported = true; rig.DeathCause = info.Cause; };

                if (options.StartCentre.HasValue)
                    catGo.transform.position = rig.Root.ToWorld(room.Origin + options.StartCentre.Value - motor.ColliderOffset);
                // A loaded scene's bodies start at their transforms; autoSyncTransforms is off in this project.
                Physics2D.SyncTransforms();
                rig.Body.linearVelocity = options.StartVelocity;
                rig.filter = new ContactFilter2D { useLayerMask = true, layerMask = rig.Root.PhysicsMask, useTriggers = false };
                foreach (SoloRoomElement e in room.Elements)
                {
                    Transform t = roomRoot.Find(e.Name);
                    if (t != null) rig.Elements.Add(Describe(e.Name, t.gameObject));
                }
                return rig;
            }

            static Element Describe(string name, GameObject go)
            {
                var e = new Element { Name = name, Go = go, Renderers = go.GetComponentsInChildren<SpriteRenderer>(true) };
                e.Trap = go.GetComponent<RoomTrap>();
                e.Hazard = go.GetComponent<Hazard>();
                e.Box = go.GetComponent<BoxCollider2D>();
                e.Block = go.GetComponent<FallingBlockTrap>();
                if (e.Block != null)
                {
                    Vector2 dir = (FallingBlockDirection)Get(e.Block, "direction") == FallingBlockDirection.Up ? Vector2.up : Vector2.down;
                    e.BlockLanded = go.GetComponent<Rigidbody2D>().position + dir * (float)Get(e.Block, "travelDistance");
                }
                e.Moving = go.GetComponent<MovingTrap>();
                if (e.Moving != null)
                {
                    e.MovingHazard = (MovingTrapKind)Get(e.Moving, "kind") == MovingTrapKind.Hazard;
                    e.MovingStep = ((Vector2)Get(e.Moving, "offset")).magnitude / Mathf.Max(1, (int)Get(e.Moving, "moveTicks"));
                    float depth = (float)Get(e.Moving, "crushDepth");
                    e.CrushDepth = depth > 0f ? depth : ((CrushConfig)Get(e.Moving, "crushConfig")).DefaultCrushDepth;
                }
                e.Arrow = go.GetComponent<ArrowTrap>();
                if (e.Arrow != null)
                {
                    e.ArrowSprite = (SpriteRenderer)Get(e.Arrow, "arrow");
                    e.ArrowLength = (float)Get(e.Arrow, "arrowLength");
                    e.ArrowThickness = (float)Get(e.Arrow, "arrowThickness");
                    e.ArrowTell = (int)Get(e.Arrow, "tellTicks");
                }
                return e;
            }

            // D-049: a route's Move is screen-relative, like the keyboard and the stick. Same projection as
            // KeyboardCatInput.ProjectScreenMove, against the gravity the input would see before this tick.
            public CatCommand ToCatFrame(CatCommand screen)
            {
                Vector2 up = -Gravity.Direction;
                Vector2 catRight = new(up.y, -up.x);
                screen.Move = VirtualStick.ToMove(new Vector2(screen.Move, 0f), catRight, StickProjection.ScreenRelative);
                return screen;
            }

            public TickRecord Snapshot(int tick, CatCommand command)
            {
                Bounds b = CatCollider.bounds;
                Vector2 v = Body.linearVelocity;
                Vector2 down = Gravity.Direction;
                var r = new TickRecord
                {
                    Tick = tick, RoomLifeTick = Rooms.RoomLifeTick,
                    X = b.center.x - Origin.x, Y = b.center.y - Origin.y, Vx = v.x, Vy = v.y,
                    GravityUp = down.y > 0f, Ground = "",
                    Dead = Death.IsHolding || Death.WasKilledThisTick(ObserverId.A, tick), Holding = Death.IsHolding, Complete = Rooms.LevelComplete,
                    Move = Mathf.RoundToInt(command.Move), JumpPressed = command.JumpPressed,
                    FireTick = new int[Elements.Count + 1], Signature = new int[Elements.Count + 1],
                };
                // The same ground test CatMotor2D.UpdateGrounded makes at the start of the next step.
                int count = Body.Cast(down, filter, hits, Motor.GroundProbeDistance);
                for (int i = 0; i < count; i++)
                {
                    if (Vector2.Dot(hits[i].normal, -down) <= Motor.GroundNormalThreshold) continue;
                    r.Grounded = Vector2.Dot(v, down) >= -0.01f;
                    r.Ground = hits[i].collider.gameObject.name;
                    break;
                }
                for (int i = 0; i < Elements.Count; i++)
                {
                    r.FireTick[i] = Elements[i].Trap != null ? Elements[i].Trap.LatestFireTick : -1;
                    r.Signature[i] = Signature(Elements[i]);
                }
                r.FireTick[Elements.Count] = -1;
                r.Signature[Elements.Count] = r.GravityUp ? 1 : 0;
                return r;
            }

            // Q5: what the player can see of an element: every SpriteRenderer in its subtree.
            static int Signature(Element e)
            {
                unchecked
                {
                    int h = 17;
                    foreach (SpriteRenderer s in e.Renderers)
                    {
                        if (s == null) continue;
                        bool visible = s.enabled && s.gameObject.activeInHierarchy;
                        h = h * 31 + (visible ? 1 : 0);
                        if (!visible) continue;
                        Color c = s.color;
                        h = h * 31 + Mathf.RoundToInt(c.r * 1000f); h = h * 31 + Mathf.RoundToInt(c.g * 1000f);
                        h = h * 31 + Mathf.RoundToInt(c.b * 1000f); h = h * 31 + Mathf.RoundToInt(c.a * 1000f);
                        h = h * 31 + (s.sprite != null ? s.sprite.GetInstanceID() : 0);
                        Vector3 p = s.transform.position;
                        h = h * 31 + Mathf.RoundToInt(p.x * 1e4f); h = h * 31 + Mathf.RoundToInt(p.y * 1e4f);
                        h = h * 31 + Mathf.RoundToInt(s.transform.eulerAngles.z * 100f);
                    }
                    return h;
                }
            }

            // R6: names the element(s) whose own kill test holds on the kill tick's poses (before physics).
            public List<string> Attribute()
            {
                var names = new List<string>();
                foreach (Element e in Elements)
                {
                    bool match = false;
                    if (e.Hazard != null && e.Hazard.enabled && e.Hazard.Armed && e.Box != null) match = Overlaps(e.Box.bounds);
                    else if (e.Block != null && e.Trap.LatestFireTick >= 0 && e.Box != null
                        && (e.Block.GetComponent<Rigidbody2D>().position - e.BlockLanded).sqrMagnitude > 1e-8f)
                    { Bounds shrunk = e.Box.bounds; shrunk.Expand(-.04f); match = Overlaps(shrunk); }
                    else if (e.Moving != null && e.Trap.LatestFireTick >= 0 && e.Box != null && e.MovingHazard)
                    { Bounds grown = e.Box.bounds; grown.Expand(2f * e.MovingStep + .02f); match = Overlaps(grown); }
                    else if (e.Moving != null && e.Trap.LatestFireTick >= 0 && e.Box != null)
                        match = TrapMotion.Crushes(CatCollider.bounds, new Bounds(e.Moving.GetComponent<Rigidbody2D>().position + e.Box.offset, e.Box.size), e.CrushDepth);
                    else if (e.Arrow != null && e.ArrowSprite != null && e.ArrowSprite.enabled)
                        match = Overlaps(new Bounds(e.ArrowSprite.transform.position, new Vector3(e.ArrowLength, e.ArrowThickness)));
                    if (match) names.Add(e.Name);
                }
                return names;
            }

            bool Overlaps(Bounds bounds)
            {
                int count = Physics2D.OverlapBox(bounds.center, bounds.size, 0f, filter, overlaps);
                for (int i = 0; i < count; i++) if (overlaps[i] == CatCollider) return true;
                return false;
            }

            // R5: an arrow's first lethal tick is fire + tell (ArrowMath.IsLethal), in harness ticks.
            public void RecordArrowLethal(ReplayResult result, int tick)
            {
                foreach (Element e in Elements)
                {
                    if (e.Arrow == null || e.Trap.LatestFireTick < 0 || result.ArrowFirstLethalTick.ContainsKey(e.Name)) continue;
                    if (Rooms.RoomLifeTick - e.Trap.LatestFireTick == e.ArrowTell) result.ArrowFirstLethalTick[e.Name] = tick;
                }
            }
        }

        // The route's input source. Read() returns this tick's command and clears the jump edge, as
        // ICatCommandSource requires.
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
            if (asset == null) throw new InvalidOperationException($"RouteHarness: {path} not found.");
            return asset;
        }

        static FieldInfo Field(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, Instance | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            throw new MissingFieldException(type.Name, name);
        }

        static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
        static object Get(object target, string name) => Field(target.GetType(), name).GetValue(target);

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

    // Turns a route into one command per tick (Q3). Conditions see the previous tick's record.
    sealed class RouteRunner
    {
        // Q3: an Until that hasn't held after this many ticks fails the route.
        public const int UntilCapTicks = 600;

        readonly Route route; readonly ReplayOptions options; readonly ReplayResult result;
        int index, move, forLeft = -1, insertLeft;
        bool jump, inserted, idle;

        public RouteRunner(Route route, ReplayOptions options, ReplayResult result) { this.route = route; this.options = options; this.result = result; }

        public bool Next(RouteView view, int tick, out CatCommand command)
        {
            if (options.TimedStep >= 0 && options.Mode == TimedMode.Shift && options.Delta < 0
                && tick == options.AuthoredStartTick + options.Delta && index < options.TimedStep)
            { index = options.TimedStep; forLeft = -1; }

            while (true)
            {
                if (index >= route.Steps.Count) { command = default; return false; }
                RouteStep step = route.Steps[index];
                if (!result.StepStartTick.ContainsKey(index)) result.StepStartTick[index] = tick;
                if (index == options.TimedStep && !inserted && options.Delta > 0)
                { inserted = true; insertLeft = options.Delta; idle = options.Mode == TimedMode.Hesitate; }
                if (insertLeft > 0) { insertLeft--; command = new CatCommand { Move = idle ? 0 : move }; return true; }

                switch (step.Kind)
                {
                    case RouteStepKind.Hold: move = step.Direction; index++; continue;
                    case RouteStepKind.Release: move = 0; index++; continue;
                    case RouteStepKind.Jump: jump = true; index++; continue;
                    case RouteStepKind.Margin: index++; continue;
                    case RouteStepKind.Until:
                        if (step.Condition.Test(view)) { index++; continue; }
                        if (tick - result.StepStartTick[index] >= UntilCapTicks)
                        {
                            result.Failure = $"step #{index} {step.Label} not reached within {UntilCapTicks} ticks";
                            command = default;
                            return false;
                        }
                        return Emit(out command);
                    default: // For
                        if (forLeft < 0) forLeft = step.Ticks;
                        if (forLeft == 0) { forLeft = -1; index++; continue; }
                        forLeft--;
                        return Emit(out command);
                }
            }
        }

        bool Emit(out CatCommand command)
        {
            command = new CatCommand { Move = move, JumpPressed = jump };
            jump = false;
            return true;
        }
    }
}
