using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 (D-080) §5.4: Trap Lab room 4, the troll-route room. Its routes are validated once per fixture and the
    // per-aspect tests read the cached report.
    public sealed class TrapLabRoom4Tests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly string[] BetrayalElements = { "Stone_A", "Thin_Collapse", "Ledge_End", "Bridge", "ArrowD" };
        IDisposable session;
        object report;

        static object Room() => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[4];
        static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room4");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);
        static List<string> Rule(string name, params object[] args) => (List<string>)Validator.GetMethod(name).Invoke(null, args);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab4", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            float gravity = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab4", room));
            foreach (string arrow in new[] { "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" })
                errors.AddRange(Rule(arrow, "TrapLab4", room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab4", room, motor, gravity, bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab4", room, motor));
            errors.AddRange(Rule("ValidateFakePlatformSettings", "TrapLab4", room));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab4", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses);
        }

        [Test]
        public void Layout_HasTenToTwelvePlatforms_AndEveryBetrayalKind()
        {
            var kinds = ((IEnumerable)F(Room(), "Elements")).Cast<object>().Select(e => (kind: F(e, "Kind").ToString(), name: (string)F(e, "Name"))).ToArray();
            int platforms = kinds.Count(k => k.kind == "Floor" || k.kind == "FakePlatform" || k.kind == "CollapsingFloor");
            Assert.That(platforms, Is.InRange(10, 12), string.Join(", ", kinds.Select(k => k.name)));
            Assert.AreEqual(2, kinds.Count(k => k.kind == "FakePlatform"));
            Assert.AreEqual(1, kinds.Count(k => k.kind == "Arrow"));
            Assert.GreaterOrEqual(((IEnumerable)F(Room(), "RequiredJumps")).Cast<object>().Count(), 3, "RequiredJumps prove the jumps comfortable (D-056 (2))");
        }

        [Test]
        public void Solution_CompletesTheRoom_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
        }

        [Test]
        public void EveryBetrayal_EndsAsDeclared_WithALeadOfAtLeastSixWhereItDies()
        {
            IList leads = (IList)F(report, "Leads"), recoveries = (IList)F(report, "Recoveries");
            Assert.AreEqual(3, leads.Count, Summary(report));
            Assert.AreEqual(1, recoveries.Count, Summary(report));
            foreach (object l in leads)
            {
                Assert.AreEqual(F(l, "ExpectedKiller"), F(l, "Killer"), l.ToString());
                Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
                Assert.AreEqual(F(l, "ExpectedCause"), F(l, "Cause"), l.ToString());
                Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            }
            foreach (object r in recoveries) Assert.IsTrue((bool)F(r, "Passed"), r.ToString());
            CollectionAssert.AreEquivalent(new[] { "Stone_A", "Thin_Collapse", "ArrowD" }, leads.Cast<object>().Select(l => (string)F(l, "RevealedBy")).ToArray());
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        [Test]
        public void Solution_NeverFiresOrVisiblyChangesABetrayal()
        {
            object replay = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> records = Records(replay);
            Assert.IsTrue((bool)F(replay, "Completed"), Dump(replay));
            foreach (string name in BetrayalElements)
            {
                int e = ElementIndex(replay, name);
                Assert.GreaterOrEqual(e, 0, name + " is not an element");
                Assert.IsFalse(records.Any(r => r.FireTick[e] >= 0), name + " fired on the solution route");
                Assert.AreEqual(-1, (int)replay.GetType().GetMethod("FirstVisibleChange").Invoke(replay, new object[] { name }), name + " changed visibly on the solution route");
            }
        }

        [Test]
        public void WithStoneAMadeSolid_ItsBetrayalFails()   // seen red on the fixture
        {
            object room = Call(T("RouteFixtures"), "TrapLabRoom4WithSolidStoneA");
            object betrayal = ((IEnumerable)F(Routes(), "Betrayals")).Cast<object>().Single(b => (string)F(b, "RevealedBy") == "Stone_A");
            var errors = new List<string>();
            object lead = Call(T("RouteValidator"), "CheckBetrayal", session, "TrapLab4 solid Stone_A", room, betrayal, errors);
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(x => x.Contains("Stone_A never changes visibly")), lead + "\n" + string.Join("\n", errors));
        }
    }
}
