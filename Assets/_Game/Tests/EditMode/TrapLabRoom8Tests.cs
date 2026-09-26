using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-086 (D-088) §5, Q2, Q5, R4, R6: Trap Lab room 8, the geyser room, through the route harness (the real motor, room
    // step and Physics2D.Simulate). Its routes are validated once per fixture and the tests read the cached report. The
    // other replays stand the cat on the vent (or beside it) and read the per-tick records. Harness tick k is room tick
    // k - 1 (no deaths). The Geyser: period 100, phase 80, tell 25, erupt 40 (erupting room ticks 105-144, 205-244...).
    public sealed class TrapLabRoom8Tests
    {
        const int Phase = 80, Tell = 25, Erupt = 40;
        const float Speed = 14f, VentX = 9f;
        IDisposable session;
        object report;

        static object Room() => GeyserValidatorTests.Room8();
        static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room8");
        static object Betrayal(int index) => ((IList)F(Routes(), "Betrayals"))[index];
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);
        static float Dt => TickTime.SecondsPerTick;
        static float PushStep => Speed * Dt;

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab8", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        void ClearSession() => session.GetType().GetMethod("Clear").Invoke(session, null);

        // ---------- route vocabulary, by reflection ----------

        static object S(string method, params object[] args) => Call(T("R"), method, args);
        static object Until(string condition, params object[] args) => S("Until", S(condition, args));

        static object MakeRoute(string name, IEnumerable<object> steps)
        {
            object[] list = steps.ToArray();
            Array array = Array.CreateInstance(T("RouteStep"), list.Length);
            for (int i = 0; i < list.Length; i++) array.SetValue(list[i], i);
            return Activator.CreateInstance(T("Route"), name, array);
        }

        static object StartAt(Vector2 centre)
        {
            object o = Activator.CreateInstance(T("ReplayOptions"));
            Set(o, "StartCentre", (Vector2?)centre);
            return o;
        }

        // Stands (or hangs) the cat at a point and runs the given steps from tick 1.
        object ReplayFrom(Vector2 centre, params object[] steps) => ReplayRoute(session, Room(), MakeRoute("probe", steps), StartAt(centre));

        static bool Pushed(Rec r) => Mathf.Abs(r.Vy - Speed) < 1e-3f;

        // ---------- the layout and the routes ----------

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab8", room));
            errors.AddRange(Rule("ValidateGeyser", "TrapLab8", room, Motor()));
            errors.AddRange(Rule("ValidateGeyserEnvelope", "TrapLab8", room, Motor(), Gravity(), Routes()));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab8", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab8", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab8", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses, "R4: no learned bypass");
        }

        [Test]
        public void Solution_RidesTheGeyser_CompletesTheRoom_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
        }

        [Test]
        public void TheDiesBetrayal_DiesInTheCeilingSpikes_WithALeadOfAtLeastSix_AndTheRecoversBetrayalRecovers()
        {
            IList leads = (IList)F(report, "Leads");
            Assert.AreEqual(1, leads.Count, Summary(report));
            object l = leads[0];
            Assert.AreEqual("Ceiling_Spikes", F(l, "Killer"), l.ToString());
            Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
            Assert.AreEqual(DeathCause.Hazard, F(l, "Cause"), l.ToString());
            Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            IList recoveries = (IList)F(report, "Recoveries");
            Assert.AreEqual(1, recoveries.Count, Summary(report));
            Assert.IsTrue((bool)F(recoveries[0], "Passed"), recoveries[0].ToString());
            Assert.AreEqual("Geyser", F(recoveries[0], "RevealedBy"));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room8Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room8Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        // The steer onto the Ledge can start 9 ticks early (earlier, the cat meets the Ledge's face below its top) and at
        // most 24 late. The lead is the spikes' reveal (t119) to the kill (t127).
        static readonly int[] Room8Windows = { 34 };
        static readonly string[] Room8Leads = { "Ceiling_Spikes=8" };

        [Test]
        public void PassesTheCameraTellRule_AtEveryAspect()
        {
            var errors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            List<object> results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "TrapLab8", Room(), Routes(), camera, errors)).Cast<object>().ToList();
            TestContext.Out.WriteLine(string.Join("\n", results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // ---------- the push, in the harness ----------

        // A cat standing on the vent: harmless through the tell, pushed at exactly LaunchSpeed from the first erupting
        // room tick for PushTicks ticks, then ballistic. Its apex (collider bottom above where it stood) is the discrete
        // prediction, PushTicks x push step + Rise, within one push tick (Q5 as ruled).
        [Test]
        public void OnTheVent_TheTellIsHarmless_ThePushIsExact_AndTheApexIsTheDiscretePrediction()
        {
            object replay = ReplayFrom(new Vector2(VentX, .3f), S("For", 250));
            List<Rec> r = Records(replay);
            foreach (Rec x in r.Where(x => x.Tick > 5 && x.RoomLifeTick >= Phase && x.RoomLifeTick < Phase + Tell))
            {
                Assert.IsTrue(x.Grounded, $"t{x.Tick} (room {x.RoomLifeTick}): the tell doesn't lift the cat\n" + Dump(replay));
                Assert.AreEqual(0f, x.Vy, 1e-3f, $"t{x.Tick}: the tell is harmless");
            }
            Rec first = r.First(Pushed);
            Assert.AreEqual(Phase + Tell, first.RoomLifeTick, "the first push is the first erupting room tick\n" + Dump(replay));
            int pushTicks = GeyserMath.PushTicks(1.5f, Speed, Dt);
            List<Rec> pushes = r.Where(x => x.Tick >= first.Tick && x.Tick < first.Tick + pushTicks + 3).TakeWhile(Pushed).ToList();
            Assert.AreEqual(pushTicks, pushes.Count, "pushed while its collider starts inside the column\n" + Dump(replay));

            Rec stood = r.Single(x => x.Tick == first.Tick - 1);
            float apex = r.Where(x => x.Tick >= first.Tick && x.Tick < first.Tick + 60).Max(x => x.Y) - stood.Y;
            float predicted = pushTicks * PushStep + GeyserMath.Rise(Speed, Gravity(), Dt, out int riseTicks);
            TestContext.Out.WriteLine($"PAX-086 measured apex {apex:F3} u above the stand (predicted {predicted:F3}: {pushTicks} pushes x {PushStep:F3} + rise over {riseTicks} ticks)");
            Assert.AreEqual(predicted, apex, PushStep, DumpRange(replay, first.Tick - 2, first.Tick + 40));
            int apexTick = r.Where(x => x.Tick >= first.Tick && x.Tick < first.Tick + 60).OrderByDescending(x => x.Y).First().Tick;
            Assert.AreEqual(pushTicks + riseTicks - 1, apexTick - first.Tick, 1, "flight ticks from the first push to the apex (R2's count)");
        }

        // Steering during the push is the motor's: from the first push tick, holding left, the horizontal velocity climbs by
        // acceleration x tick to run speed while every push still sets the vertical. Nine ticks keep the cat clear of the
        // Ledge's face (x 7.5).
        [Test]
        public void OnTheVent_HoldingLeft_SteersAtRunSpeed_WhileThePushHoldsTheVertical()
        {
            int t = FirstPushTick();
            object replay = ReplayFrom(new Vector2(VentX, .3f), S("For", t - 1), S("Hold", -1), S("For", 9));
            List<Rec> r = Records(replay);
            Assert.AreEqual(t, r.First(Pushed).Tick);
            float accel = Motor().Acceleration * Dt;
            float vx = 0f;
            foreach (Rec x in r.Where(x => x.Tick >= t))
            {
                vx = Mathf.Max(vx - accel, -Motor().MaxSpeed);
                Assert.AreEqual(vx, x.Vx, 1e-3f, $"t{x.Tick}\n" + DumpRange(replay, t - 2, t + 10));
            }
            Assert.IsTrue(r.Where(x => x.Tick >= t && x.Tick < t + GeyserMath.PushTicks(1.5f, Speed, Dt)).All(Pushed), "still pushed while steering");
            Assert.AreEqual(-Motor().MaxSpeed, r.Last().Vx, 1e-3f, "run speed");
        }

        [Test]
        public void BesideTheColumn_TheCatIsNeverPushed()
        {
            // Collider x [7.2, 8.2]; the column is x [8.5, 9.5].
            object replay = ReplayFrom(new Vector2(VentX - 1.3f, .3f), S("For", Phase + Tell + Erupt + 20));
            List<Rec> r = Records(replay);
            Assert.IsTrue(r.Any(x => x.RoomLifeTick >= Phase + Tell && x.RoomLifeTick < Phase + Tell + Erupt), "the replay covers an eruption");
            foreach (Rec x in r.Where(x => x.Tick > 5)) Assert.AreEqual(0f, x.Vy, 1e-3f, $"t{x.Tick}\n" + Dump(replay));
        }

        // ---------- Q2: launch vs coyote and buffer, tick by tick ----------

        int FirstPushTick()
        {
            List<Rec> r = Records(ReplayFrom(new Vector2(VentX, .3f), S("For", Phase + Tell + 10)));
            return r.First(Pushed).Tick;
        }

        [Test]
        public void AJumpPressedOnTheFirstPushTick_IsOverwrittenByTheLaunch()
        {
            int t = FirstPushTick();
            object replay = ReplayFrom(new Vector2(VentX, .3f), S("For", t - 1), S("Jump"), S("For", 40));
            List<Rec> r = Records(replay);
            Rec press = r.Single(x => x.JumpPressed);
            Assert.AreEqual(t, press.Tick);
            Assert.IsTrue(Pushed(press), "the launch, not the jump\n" + DumpRange(replay, t - 2, t + 10));
            Assert.IsTrue(r.Where(x => x.Tick > t && x.Tick < t + GeyserMath.PushTicks(1.5f, Speed, Dt)).All(Pushed));
        }

        // A press on T+1: the launch cleared coyote, so it only waits in the buffer (and expires in the air). After the last
        // push the cat is ballistic: each tick loses g x tick, with no jump anywhere before it lands back on the vent.
        [Test]
        public void APressOnTheTickAfterTheLaunch_IsNoCoyoteJump_TheFlightIsBallistic()
        {
            int t = FirstPushTick();
            object replay = ReplayFrom(new Vector2(VentX, .3f), S("For", t), S("Jump"), S("For", 60));
            List<Rec> r = Records(replay);
            Assert.AreEqual(t + 1, r.Single(x => x.JumpPressed).Tick);
            int last = r.Where(x => x.Tick >= t && x.Tick < t + 12).TakeWhile(Pushed).Last().Tick;
            float g = Gravity() * Dt;
            for (int k = last + 1; k < last + 40; k++)
            {
                Rec now = r.Single(x => x.Tick == k), before = r.Single(x => x.Tick == k - 1);
                if (now.Grounded) break;
                Assert.AreEqual(before.Vy - g, now.Vy, 1e-3f, $"t{k}: ballistic, no jump\n" + DumpRange(replay, t - 2, last + 40));
            }
        }

        // A press 3 ticks before the solution lands on the Ledge fills the buffer; the jump fires on the landing.
        [Test]
        public void ABufferedPressBeforeLandingOnTheLedge_JumpsOnTheLanding()
        {
            object solution = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> s = Records(solution);
            int land = s.First(x => x.Grounded && x.Ground == "Ledge").Tick;
            var starts = (Dictionary<int, int>)F(solution, "StepStartTick");
            object[] authored = ((IEnumerable)F(F(Routes(), "Solution"), "Steps")).Cast<object>().ToArray();
            int holdLeft = Array.FindIndex(authored, step => (string)F(step, "Label") == "Hold(Left)");
            Assert.GreaterOrEqual(holdLeft, 0);
            int from = starts[holdLeft];
            var steps = authored.Take(holdLeft + 1).Select(step => step.GetType().GetMethod("Timed").Invoke(step, new[] { Enum.Parse(T("TimedMode"), "None") })).ToList();
            steps.AddRange(new[] { S("For", land - 3 - from), S("Jump"), S("For", 15) });
            object replay = ReplayRoute(session, Room(), MakeRoute("buffered press", steps));
            List<Rec> r = Records(replay);
            Rec press = r.Single(x => x.JumpPressed);
            Assert.AreEqual(land - 3, press.Tick, DumpRange(replay, land - 6, land + 6));
            Assert.IsFalse(press.Grounded);
            Assert.Less(press.Vy, 0f, "falling when pressed: no mid-air jump");
            float jumpSpeed = Mathf.Sqrt(2f * Gravity() * Motor().JumpHeight);
            Assert.IsTrue(r.Any(x => x.Tick > land && x.Tick <= land + 3 && Mathf.Abs(x.Vy - jumpSpeed) < 1e-2f), "the buffered jump fires on the landing\n" + DumpRange(replay, land - 6, land + 6));
        }

        // ---------- R6: a Down geyser in a gravity-up room ----------

        // A 16-wide room with a low ceiling (underside 3.0). The cat starts inside a gravity flip under a Down geyser flush in
        // the ceiling (column y [1.5, 3.0]): it flips, falls up onto the ceiling, and the eruption pushes it down, away from it.
        static object DownRoom()
        {
            object flip = GeyserValidatorTests.Timing(TrapRepeatMode.Once, 1, 0);
            object[] elements =
            {
                GeyserValidatorTests.Element("Ceiling", "Ceiling", new Vector2(8f, 3.5f), new Vector2(16f, 1f)),
                GeyserValidatorTests.Element("Checkpoint", "Checkpoint", new Vector2(2f, 0f), Vector2.zero),
                GeyserValidatorTests.Element("Floor", "Floor", new Vector2(8f, -.5f), new Vector2(16f, 1f)),
                GeyserValidatorTests.Element("Door", "Door", new Vector2(15f, .75f), new Vector2(.6f, 1.5f)),
                GeyserValidatorTests.Element("GravityFlip", "Flip", new Vector2(8f, 1.5f), new Vector2(1f, 3f), settings: flip),
                GeyserValidatorTests.Element("Geyser", "Geyser", new Vector2(8f, 3.15f), new Vector2(1f, .3f), settings: GeyserValidatorTests.GeyserSettings(GeyserDirection.Down, period: 200, phase: 40)),
            };
            Type elementType = GeyserValidatorTests.EditorType("SoloRoomElement");
            Array array = Array.CreateInstance(elementType, elements.Length);
            for (int i = 0; i < elements.Length; i++) array.SetValue(elements[i], i);
            ConstructorInfo ctor = GeyserValidatorTests.EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            return ctor.Invoke(new object[] { 0, 0f, 16f, array, Array.CreateInstance(GeyserValidatorTests.EditorType("SoloRoomOpening"), 0), Array.CreateInstance(GeyserValidatorTests.EditorType("RequiredJump"), 0) });
        }

        [Test]
        public void ADownGeyser_InAGravityUpRoom_PushesTheCatDownAwayFromTheCeiling()
        {
            CollectionAssert.IsEmpty(Rule("ValidateGeyser", "DOWN", DownRoom(), Motor()), "the fixture's geyser is well formed");
            object replay = ReplayRoute(session, DownRoom(), MakeRoute("hang under the vent", new[] { S("For", 140) }), StartAt(new Vector2(8f, .3f)));
            List<Rec> r = Records(replay);
            Rec onCeiling = r.First(x => x.Tick > 5 && x.GravityUp && x.Grounded);
            Assert.Less(onCeiling.RoomLifeTick, 65, "on the ceiling before the eruption\n" + Dump(replay));
            // The first run of pushes (this low room's floor bounces the cat back up into the column later in the eruption).
            Rec start = r.First(x => Mathf.Abs(x.Vy + Speed) < 1e-3f);
            List<Rec> pushed = r.Where(x => x.Tick >= start.Tick).TakeWhile(x => Mathf.Abs(x.Vy + Speed) < 1e-3f).ToList();
            Assert.AreEqual(GeyserMath.PushTicks(1.5f, Speed, Dt), pushed.Count, Dump(replay));
            Assert.AreEqual(65, pushed[0].RoomLifeTick, "from the first erupting room tick");
            Assert.IsTrue(pushed.All(x => x.GravityUp), "pushed against its own gravity");
        }

        // ---------- the built room ----------

        [Test]
        public void InABuiltRoom8_TheGeyserIsWiredAsTheLayoutSays()
        {
            ClearSession();
            int layer = LayerMask.NameToLayer("RealityA");
            var rootGo = new GameObject("RealityRoot_A") { layer = layer };
            RealityRoot root = rootGo.AddComponent<RealityRoot>();
            PauseTestRig.SetPrivate(root, "id", ObserverId.A);
            PauseTestRig.Invoke(root, "Awake");
            var systems = new GameObject("Systems");
            CheckpointManager checkpoints = systems.AddComponent<CheckpointManager>();
            RoomManager rooms = systems.AddComponent<RoomManager>();
            RoomDeath death = systems.AddComponent<RoomDeath>();
            ObserverSet observers = new GameObject("Observers").AddComponent<ObserverSet>();

            Type builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
            var parent = new GameObject("Rooms") { layer = layer };
            parent.transform.SetParent(rootGo.transform, false);
            builder.GetMethod("BuildRoom", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { parent.transform, root, Room(), checkpoints, rooms, death, observers, Motor(), new List<string>() });
            GeyserTrap built = rootGo.GetComponentInChildren<GeyserTrap>(true);
            Assert.NotNull(built, "the builder made no GeyserTrap");
            Assert.AreEqual(GeyserDirection.Up, PauseTestRig.GetPrivate<GeyserDirection>(built, "direction"));
            Assert.AreEqual(Tell, PauseTestRig.GetPrivate<int>(built, "tellTicks"));
            Assert.AreEqual(Erupt, PauseTestRig.GetPrivate<int>(built, "eruptTicks"));
            Assert.AreEqual(Speed, PauseTestRig.GetPrivate<float>(built, "launchSpeed"));
            Assert.AreEqual(1f, PauseTestRig.GetPrivate<float>(built, "columnWidth"));
            Assert.AreEqual(1.5f, PauseTestRig.GetPrivate<float>(built, "columnHeight"));
            Assert.AreEqual(TrapRepeatMode.Periodic, PauseTestRig.GetPrivate<TrapRepeatMode>(built, "repeatMode"));
            Assert.AreEqual(100, PauseTestRig.GetPrivate<int>(built, "periodTicks"));
            Assert.AreEqual(Phase, PauseTestRig.GetPrivate<int>(built, "phaseTicks"));
            Assert.AreSame(observers, PauseTestRig.GetPrivate<ObserverSet>(built, "observers"));
            Assert.NotNull(built.Vent, "the vent's look");
            Assert.NotNull(built.Column, "the column's look");
            Assert.IsFalse(built.Column.enabled, "the column is hidden until it erupts");
            Assert.IsTrue(built.GetComponent<BoxCollider2D>().isTrigger, "the vent is never solid: the Floor holds the cat");
            ClearSession();
        }

        static string DumpRange(object replay, int from, int to) => (string)replay.GetType().GetMethod("Dump").Invoke(replay, new object[] { from, to });
    }
}
