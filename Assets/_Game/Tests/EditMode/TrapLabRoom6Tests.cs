using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-084 (D-086) §5: Trap Lab room 6, the spear room. Its routes are validated once per fixture and the per-aspect
    // tests read the cached report.
    public sealed class TrapLabRoom6Tests
    {
        static readonly string[] Spears = { "Spear_1", "Spear_2", "Spear_3" };
        IDisposable session;
        object report;

        static object Room() => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[6];
        static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room6");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab6", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab6", room));
            foreach (string arrow in new[] { "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" })
                errors.AddRange(Rule(arrow, "TrapLab6", room));
            errors.AddRange(Rule("ValidateSpear", "TrapLab6", room, sizes));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab6", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab6", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab6", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses);
        }

        [Test]
        public void Layout_HasThreeSpearsAtRisingHeights_AndDeclaresEveryClimbAsARequiredJump()
        {
            var elements = ((IEnumerable)F(Room(), "Elements")).Cast<object>().ToArray();
            object[] spears = Spears.Select(n => elements.Single(e => (string)F(e, "Name") == n)).ToArray();
            float[] heights = spears.Select(e => (float)F(F(F(e, "Settings"), "Arrow"), "LaneY")).ToArray();
            foreach (object e in spears) Assert.IsTrue((bool)F(F(F(e, "Settings"), "Arrow"), "Spear"), F(e, "Name") + " is a spear");
            CollectionAssert.IsOrdered(heights);
            Assert.AreEqual(3, heights.Distinct().Count());
            var jumps = ((IEnumerable)F(Room(), "RequiredJumps")).Cast<object>().Select(j => (string)F(j, "DestinationName")).ToArray();
            CollectionAssert.IsSubsetOf(Spears, jumps, "R4: every landing on a stuck spear is an ordinary RequiredJump");
        }

        [Test]
        public void Solution_ClimbsEveryStuckSpear_CompletesTheRoom_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
            object replay = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> records = Records(replay);
            foreach (string spear in Spears)
                Assert.IsTrue(records.Any(r => r.Grounded && r.Ground == spear + "_Shaft"), spear + " was never stood on\n" + Dump(replay));
        }

        [Test]
        public void EveryBetrayal_DiesAtItsSpear_WithALeadOfAtLeastSix()
        {
            IList leads = (IList)F(report, "Leads");
            Assert.AreEqual(2, leads.Count, Summary(report));
            foreach (object l in leads)
            {
                Assert.AreEqual(F(l, "ExpectedKiller"), F(l, "Killer"), l.ToString());
                Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
                Assert.AreEqual(F(l, "ExpectedCause"), F(l, "Cause"), l.ToString());
                Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            }
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        // Pinned as Trap Lab 3-5 are: the windows and the leads.
        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room6Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room6Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        // The climb out of the Dip can move 25 ticks either way (the sweep's range, open at both ends): in that span a cat
        // rising out of the Dip stays clear of the volley (Spear_3's lane is above its jump). Both leads are the tell.
        static readonly int[] Room6Windows = { 51 };
        static readonly string[] Room6Leads = { "Spear_1=8", "Spear_2=8" };

        [Test]
        public void PassesTheCameraTellRule_AtEveryAspect()
        {
            var errors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            List<object> results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "TrapLab6", Room(), Routes(), camera, errors)).Cast<object>().ToList();
            TestContext.Out.WriteLine(string.Join("\n", results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }
    }
}
