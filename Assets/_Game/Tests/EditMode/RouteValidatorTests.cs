using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-075 (D-079) §5.2-§5.4: the route rule on every LevelLayouts entry and Trap Lab room 3. Each room is
    // validated once per fixture (R12 lever 1) and the per-aspect tests read the cached report.
    public sealed class RouteValidatorTests
    {
        static readonly string[] Rooms = { "L001", "L002", "L003", "L004", "TrapLab3" };
        IDisposable session;
        readonly Dictionary<string, object> reports = new();

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            foreach (string id in Rooms) reports[id] = Call(T("RouteValidator"), "Run", session, id, Room(id), Routes(id));
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        internal static object Room(string id)
        {
            if (id == "TrapLab3") return ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[3];
            return ((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null))[id];
        }

        internal static object Routes(string id)
        {
            if (id == "TrapLab3") return Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room3");
            return ((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelRoutes, Parallax.Editor").GetField("ById").GetValue(null))[id];
        }

        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        [TestCase("L001")] [TestCase("L002")] [TestCase("L003")] [TestCase("L004")] [TestCase("TrapLab3")]
        public void Solution_CompletesTheRoom_WithEveryTimedWindowAndMarginAtLeastTwelve(string id)
        {
            object report = reports[id];
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            foreach (object w in (IEnumerable)F(report, "Windows")) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
            foreach (object m in (IEnumerable)F(report, "Margins")) Assert.IsTrue((bool)F(m, "Passed"), m + "\n" + Summary(report));
            Assert.IsTrue(((IList)F(report, "Windows")).Count + ((IList)F(report, "Margins")).Count > 0, id + " declares no timed step or margin");
        }

        [TestCase("L001")] [TestCase("L002")] [TestCase("L003")] [TestCase("L004")] [TestCase("TrapLab3")]
        public void EveryBetrayal_DiesAtItsElementAndCause_WithALeadOfAtLeastSix(string id)
        {
            object report = reports[id];
            IList leads = (IList)F(report, "Leads");
            Assert.Greater(leads.Count, 0, id + " declares no betrayal");
            foreach (object l in leads)
            {
                Assert.AreEqual(F(l, "ExpectedKiller"), F(l, "Killer"), l.ToString());
                Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
                Assert.AreEqual(F(l, "ExpectedCause"), F(l, "Cause"), l.ToString());
                Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            }
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        // §5.2: two replays of the same route give the same per-tick record.
        [TestCase("L001")] [TestCase("L002")] [TestCase("L003")] [TestCase("L004")] [TestCase("TrapLab3")]
        public void TwoReplaysOfTheSolution_GiveTheSamePerTickRecord(string id)
        {
            Assert.IsTrue((bool)F(reports[id], "Deterministic"), Summary(reports[id]));
            object solution = F(Routes(id), "Solution");
            object a = ReplayRoute(session, Room(id), solution), b = ReplayRoute(session, Room(id), solution);
            Assert.IsTrue((bool)Call(T("RouteValidator"), "SameRecord", a, b), "the two records differ");
            Assert.Greater(Records(a).Count, 100, "the replay ran");
        }

        // §5.3 / R25: the fixtures follow the harness. The boundary is x 19.30 at exactly 12 and passes; the
        // red fixture is 19.20 at 11. (The pre-PAX-078 x 19.55 gives 14 in the real code; D-079.)
        [TestCase(19.30f, 12, true)]
        [TestCase(19.20f, 11, false)]
        public void L002WithTheLiftTriggerMoved_LiftMarginIsQuoted_AndFailsBelowTwelve(float triggerX, int expected, bool passes)
        {
            object room = Call(T("RouteFixtures"), "L002WithLiftTriggerX", triggerX);
            object solution = F(Routes("L002"), "Solution");
            object replay = ReplayRoute(session, room, solution);
            object margin = ((IList)Call(T("RouteValidator"), "Margins", replay, solution))[0];
            Assert.AreEqual(expected, (int)F(margin, "Value"), margin.ToString());
            Assert.AreEqual(passes, (bool)F(margin, "Passed"), margin.ToString());
        }

        // §5.4 / R5: ArrowB with a 3-tick tell measures a lead of 3 (fire + tell - first visible change) and fails.
        [Test]
        public void TrapLabRoom3WithArrowBTellThree_FailsTheLeadCheck()
        {
            object room = Call(T("RouteFixtures"), "TrapLabRoom3WithArrowBTell", 3);
            object betrayal = ((IEnumerable)F(Routes("TrapLab3"), "Betrayals")).Cast<object>().Single(b => (string)F(b, "Killer") == "ArrowB");
            var errors = new List<string>();
            object lead = Call(T("RouteValidator"), "CheckBetrayal", session, "TrapLab3 tell 3", room, betrayal, errors);
            Assert.AreEqual("ArrowB", F(lead, "Killer"), lead.ToString());
            Assert.AreEqual(3, (int)F(lead, "Lead"), lead.ToString());
            Assert.IsTrue(errors.Any(e => e.Contains("lead 3 ticks is below 6")), string.Join("\n", errors));
        }
    }

    // D-069 (1): every LevelLayouts entry has routes, and ValidateRoutes (which opens its own session) passes them.
    public sealed class ValidateRoutesTests
    {
        [SetUp] public void FreshScene() => FreshScratchScene();

        [Test]
        public void ValidateRoutes_EveryLevelLayoutsEntry_HasRoutesAndNoErrors()
        {
            Type validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
            var layouts = (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null);
            var errors = new List<string>();
            foreach (DictionaryEntry entry in layouts)
                errors.AddRange((List<string>)validator.GetMethod("ValidateRoutes", new[] { typeof(string), entry.Value.GetType() }).Invoke(null, new[] { entry.Key, entry.Value }));
            Assert.AreEqual(4, layouts.Count);
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void ValidateRoutes_ALevelWithoutRoutes_Fails()
        {
            Type validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
            object room = RouteValidatorTests.Room("L001");
            var errors = (List<string>)validator.GetMethod("ValidateRoutes", new[] { typeof(string), room.GetType() }).Invoke(null, new[] { "L999", room });
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("no routes", errors[0]);
        }
    }
}
