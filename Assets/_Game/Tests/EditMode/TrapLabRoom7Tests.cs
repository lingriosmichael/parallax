using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
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
    // PAX-085 (D-087) §5, R3, R4: Trap Lab room 7, the inverter room, through the route harness (the real LocalHumanDriver).
    // Its routes are validated once per fixture and the tests read the cached report.
    public sealed class TrapLabRoom7Tests
    {
        const int Duration = 150;
        const string CatInverted = "Cat.Inverted";
        IDisposable session;
        object report;

        static object Room() => InverterValidatorTests.Room7();
        static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room7");
        static object Betrayal(int index) => ((IList)F(Routes(), "Betrayals"))[index];
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab7", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        // Everything this fixture builds goes into the session's scene; Clear empties it (as every replay does first).
        void ClearSession() => session.GetType().GetMethod("Clear").Invoke(session, null);

        static int FireTick(List<Rec> records, int inverter) => records.First(r => r.FireTick[inverter] >= 0).Tick;

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab7", room));
            errors.AddRange(Rule("ValidateInverter", "TrapLab7", room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab7", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab7", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab7", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses);
        }

        [Test]
        public void Solution_WaitsOutTheInversion_CompletesTheRoom_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
        }

        [Test]
        public void TheDiesBetrayal_DiesAtSpikesBack_RevealedByCatInverted_WithALeadOfAtLeastSix_AndTheRecoversBetrayalRecovers()
        {
            IList leads = (IList)F(report, "Leads");
            Assert.AreEqual(1, leads.Count, Summary(report));
            object l = leads[0];
            Assert.AreEqual("Spikes_Back", F(l, "Killer"), l.ToString());
            Assert.AreEqual(CatInverted, F(l, "RevealedBy"), l.ToString());
            Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
            Assert.AreEqual(DeathCause.Hazard, F(l, "Cause"), l.ToString());
            Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            IList recoveries = (IList)F(report, "Recoveries");
            Assert.AreEqual(1, recoveries.Count, Summary(report));
            Assert.IsTrue((bool)F(recoveries[0], "Passed"), recoveries[0].ToString());
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        // R2/R4: Cat.Inverted's first visible change is the fire tick; the harness's cat decelerates from the next step.
        [Test]
        public void InTheHarness_HoldRight_TurnsLeftFromTheStepAfterTheFire_AndCatInvertedChangesAtTheFireTick()
        {
            object replay = ReplayRoute(session, Room(), F(Betrayal(0), "Route"));
            List<Rec> records = Records(replay);
            int inverter = ElementIndex(replay, "Inverter");
            Assert.GreaterOrEqual(ElementIndex(replay, CatInverted), 0, "Cat.Inverted is an element of every replay\n" + Dump(replay));
            int t = FireTick(records, inverter);
            Assert.AreEqual(t, (int)replay.GetType().GetMethod("FirstVisibleChange").Invoke(replay, new object[] { CatInverted }), Dump(replay));
            Rec at = records.Single(r => r.Tick == t), before = records.Single(r => r.Tick == t - 1), next = records.Single(r => r.Tick == t + 1);
            Assert.AreEqual(1, at.Move); Assert.AreEqual(1, next.Move);
            Assert.GreaterOrEqual(at.Vx, before.Vx - 1e-4f, "the fire tick's own step still pushes right\n" + Dump(replay));
            Assert.Less(next.Vx, at.Vx, "the first inverted step pushes left\n" + Dump(replay));
            Assert.IsTrue(records.Last().Dead, "and it runs back into Spikes_Back\n" + Dump(replay));
        }

        // The solution releases at T+1 and waits 150 ticks: at T+151 it holds right and goes right; before it, still.
        [Test]
        public void InTheHarness_TheSolutionsRestartAtTPlus151_GoesRight()
        {
            object replay = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> records = Records(replay);
            int t = FireTick(records, ElementIndex(replay, "Inverter"));
            Rec last = records.Single(r => r.Tick == t + Duration), first = records.Single(r => r.Tick == t + Duration + 1);
            Assert.AreEqual(0, last.Move); Assert.AreEqual(1, first.Move);
            Assert.Greater(first.Vx, last.Vx, "T+151 is not inverted\n" + Dump(replay));
        }

        // Jump is never inverted: the Recovers route jumps the pit while inverted.
        [Test]
        public void InTheHarness_AJumpWhileInverted_RisesAsUsual_AndHoldLeftGoesRight()
        {
            object replay = ReplayRoute(session, Room(), F(Betrayal(1), "Route"));
            List<Rec> records = Records(replay);
            int t = FireTick(records, ElementIndex(replay, "Inverter"));
            Rec jump = records.Last(r => r.JumpPressed);
            Assert.Greater(jump.Tick, t); Assert.LessOrEqual(jump.Tick, t + Duration, "the jump is inside the window");
            Assert.Greater(jump.Vy, 0f, "rises\n" + Dump(replay));
            Assert.AreEqual(-1, jump.Move);
            Assert.Greater(jump.Vx, 0f, "holding left, it goes right\n" + Dump(replay));
            Assert.IsTrue(records.Last().Complete, Dump(replay));
        }

        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room7Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room7Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        // The restart after the wait can start up to 24 ticks early (it walks left, still inverted, and turns before
        // Spikes_Back) and at least 25 late (the sweep's range). The lead is the fire (t63) to the kill at Spikes_Back (t97).
        static readonly int[] Room7Windows = { 50 };
        static readonly string[] Room7Leads = { CatInverted + "=34" };

        [Test]
        public void PassesTheCameraTellRule_AtEveryAspect()
        {
            var errors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            List<object> results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "TrapLab7", Room(), Routes(), camera, errors)).Cast<object>().ToList();
            TestContext.Out.WriteLine(string.Join("\n", results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // R3: room 7 built by SoloRoomBuilder (as the Trap Lab menu builds it); a driver activated on that reality holds the
        // inverter in its list.
        [Test]
        public void InABuiltRoom7_TheDriversList_ContainsTheInverter_AfterActivation()
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
            var motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");

            Type builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
            Assert.NotNull(builder, "SoloRoomBuilder not found");
            var parent = new GameObject("Rooms") { layer = layer };
            parent.transform.SetParent(rootGo.transform, false);
            builder.GetMethod("BuildRoom", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { parent.transform, root, Room(), checkpoints, rooms, death, observers, motor, new List<string>() });
            InverterTrap built = rootGo.GetComponentInChildren<InverterTrap>(true);
            Assert.NotNull(built, "the builder made no InverterTrap");

            ObserverContext observer = new GameObject("Observer_A").AddComponent<ObserverContext>();
            PauseTestRig.SetPrivate(observer, "id", ObserverId.A);
            PauseTestRig.SetPrivate(observer, "reality", root);
            CatInputRouter router = new GameObject("DeviceInput").AddComponent<CatInputRouter>();
            observer.SetDriver(new LocalHumanDriver(router));
            IControlModifier[] modifiers = PauseTestRig.GetPrivate<IControlModifier[]>(observer.Driver, "modifiers");
            CollectionAssert.AreEqual(new IControlModifier[] { built }, modifiers);
            ClearSession();
        }
    }
}
