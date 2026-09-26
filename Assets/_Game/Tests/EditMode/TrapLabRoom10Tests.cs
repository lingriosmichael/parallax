using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-088 (D-090) §5, §2.7, §2.8: Trap Lab room 10, the storm cloud room, through the route harness (the real input
    // router, motor, room step and Physics2D.Simulate). Its routes are validated once per fixture and the tests read the
    // cached report. The probes walk in from the checkpoint (the cloud only wakes on its trigger, x 5.5-6.0) and read the
    // per-tick records. The cloud: 2 x 0.8 at (4, 5.5), range x 1.5-24; the Rise x 11-17 (top 0.8); Fake_Overhang x 6.5-8.5
    // and the real Overhang x 19.5-22.5, both y 1.4-1.9; the Door at x 26.
    public sealed class TrapLabRoom10Tests
    {
        const int FirstStrike = 75, Tell = 25;
        IDisposable session;
        object report;

        internal static object Room() => StormCloudValidatorTests.Room10();
        internal static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room10");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab10", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object S(string method, params object[] args) => Call(T("R"), method, args);
        static object Until(string condition, params object[] args) => S("Until", S(condition, args));

        static object MakeRoute(string name, IEnumerable<object> steps)
        {
            object[] list = steps.ToArray();
            Array array = Array.CreateInstance(T("RouteStep"), list.Length);
            for (int i = 0; i < list.Length; i++) array.SetValue(list[i], i);
            return Activator.CreateInstance(T("Route"), name, array);
        }

        object Probe(params object[] steps) => ReplayRoute(session, Room(), MakeRoute("probe", steps));

        static object Kill(object replay) => F(replay, "Kill");

        // The harness tick on which the cloud woke (its FireTick first becomes >= 0).
        static int WakeTick(object replay)
        {
            int cloud = ElementIndex(replay, "StormCloud");
            return Records(replay).First(r => r.FireTick[cloud] >= 0).Tick;
        }

        // ---------- the layout and the routes ----------

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab10", room));
            errors.AddRange(Rule("ValidateStormCloud", "TrapLab10", room, Motor()));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab10", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab10", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab10", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses, "no learned bypass");
        }

        [Test]
        public void Solution_OutrunsTheCloud_OverTheRise_ToTheDoor_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
        }

        [Test]
        public void BothBetrayals_DieToTheCloud_ByHazard_RevealedByIt_WithALeadOfAtLeastItsCharge()
        {
            IList leads = (IList)F(report, "Leads");
            Assert.AreEqual(2, leads.Count, Summary(report));
            foreach (object l in leads)
            {
                Assert.AreEqual("StormCloud", F(l, "Killer"), l.ToString());
                Assert.AreEqual("StormCloud", F(l, "RevealedBy"), l.ToString());
                Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
                Assert.AreEqual(DeathCause.Hazard, F(l, "Cause"), l.ToString());
                Assert.GreaterOrEqual((int)F(l, "Lead"), Tell, l.ToString());
            }
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room10Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room10Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        // Measured (§11 E). The jump onto the Rise can move 25 ticks either way (open both ends: the cloud never reaches a
        // running cat, and the Rise is 0.8 high). Both betrayals: the wake at t28, the cloud's first follow step (its first
        // visible change) at t29, the first strike's kill at t103 = wake + 75.
        static readonly int[] Room10Windows = { 51 };
        static readonly string[] Room10Leads = { "StormCloud=74", "StormCloud=74" };

        [Test]
        public void PassesTheCameraTellRule_AtEveryAspect()
        {
            var errors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            List<object> results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "TrapLab10", Room(), Routes(), camera, errors)).Cast<object>().ToList();
            TestContext.Out.WriteLine(string.Join("\n", results));
            Assert.AreEqual(2 * 3, results.Count, "two dying betrayals at three aspects");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // ---------- the harness (§5) ----------

        // Walks in, stops as the cloud wakes and stands still: the cloud settles over it and the first strike (wake + 75)
        // kills it; the harness names the cloud through its own column test.
        [Test]
        public void ACatStandingStill_DiesOnTheFirstStrike_AndTheHarnessNamesTheCloud()
        {
            object replay = Probe(S("Hold", 1), Until("Fired", "StormCloud"), S("Release"), Until("Dead"));
            object kill = Kill(replay);
            Assert.NotNull(kill, Dump(replay));
            Assert.AreEqual(WakeTick(replay) + FirstStrike, (int)F(kill, "Tick"), Dump(replay));
            CollectionAssert.AreEqual(new[] { "StormCloud" }, (IEnumerable)F(kill, "Candidates"));
        }

        // The same cat, standing still until the charge's first tick (wake + 50), then running left: it clears the column in
        // the 25-tick charge and survives the strike.
        [Test]
        public void ACatThatStartsMovingOnTheChargeTick_Survives()
        {
            object replay = Probe(S("Hold", 1), Until("Fired", "StormCloud"), S("Release"), S("For", 49), S("Hold", -1), S("For", 60));
            Assert.IsNull(Kill(replay), Dump(replay));
            List<Rec> r = Records(replay);
            int wake = WakeTick(replay);
            int firstLeft = r.First(x => x.Move < 0).Tick;
            Assert.AreEqual(wake + 50, firstLeft, "the run starts on the charge's first tick\n" + Dump(replay));
            Assert.AreEqual(0f, r[firstLeft - 1].Vx, 1e-3f, "from rest");
            Assert.Greater(r.Last().Tick, wake + FirstStrike + 6, "lived through the strike");
        }

        // Under the real Overhang the strike stops on its top: a cat that waits there through several strikes, with the cloud
        // settled right over it, lives.
        [Test]
        public void ACatWaitingUnderTheRealOverhang_SurvivesTheStrikesOverIt()
        {
            object replay = Probe(S("Hold", 1), Until("XAtLeast", 10f), S("Jump"), Until("GroundedOn", "Rise"), Until("XAtLeast", 20.6f), S("Release"), Until("Still"), S("For", 400));
            Assert.IsNull(Kill(replay), Dump(replay));
            List<Rec> r = Records(replay);
            Assert.GreaterOrEqual(r.Last().Tick - WakeTick(replay), 3 * 100 + FirstStrike + 6, "several strikes ran");
            int cloud = ElementIndex(replay, "StormCloud");
            object last = ((IList)F(replay, "Records"))[r.Count - 1];
            Rect drawn = ((Rect[])F(last, "RenderBounds"))[cloud];
            Assert.AreEqual(r.Last().X, drawn.center.x, .01f, "the cloud has settled right over the cat");
            Assert.That(r.Last().X, Is.InRange(19.5f + .5f, 22.5f - .5f), "wholly under the Overhang");
        }

        [Test]
        public void TwoReplaysOfTheStandingStillProbe_AreIdentical()
        {
            object a = Probe(S("Hold", 1), Until("Fired", "StormCloud"), S("Release"), Until("Dead"));
            object b = Probe(S("Hold", 1), Until("Fired", "StormCloud"), S("Release"), Until("Dead"));
            IList ra = (IList)F(a, "Records"), rb = (IList)F(b, "Records");
            Assert.AreEqual(ra.Count, rb.Count);
            for (int i = 0; i < ra.Count; i++) Assert.IsTrue((bool)ra[i].GetType().GetMethod("SameAs").Invoke(ra[i], new[] { rb[i] }), $"record {i}");
        }
    }
}
