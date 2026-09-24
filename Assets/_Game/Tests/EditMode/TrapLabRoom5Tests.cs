using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §2.5: Trap Lab room 5, the precision room. Its routes are validated once per fixture against
    // the provisional thresholds (0.85 / 8); the last test checks the committed asset.
    public sealed class TrapLabRoom5Tests
    {
        IDisposable session;
        object report;
        ScriptableObject provisional;

        static object Room() => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[5];
        static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room5");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            provisional = Thresholds();
            report = Call(T("RouteValidator"), "RunWithThresholds", session, "TrapLab5", Room(), Routes(), provisional);
        }

        [OneTimeTearDown] public void Close() { session?.Dispose(); if (provisional != null) Object.DestroyImmediate(provisional); }

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("ValidateWithThresholds", "TrapLab5", room, provisional));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab5", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab5", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab5", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses);
        }

        [Test]
        public void Layout_HasEightToTwelveThinPlatformsInOneSection_ATrap_AndABaitGap()
        {
            object room = Room();
            object section = ((IEnumerable)F(room, "PrecisionSections")).Cast<object>().Single();
            Rect region = (Rect)F(section, "Region");
            var platforms = ((IEnumerable)F(room, "Elements")).Cast<object>().Where(e => F(e, "Kind").ToString() == "Floor" && (string)F(e, "Name") != "Start_Floor").ToArray();
            var inside = platforms.Where(e => region.Contains((Vector2)F(e, "Position"))).ToArray();
            Assert.That(inside.Length, Is.InRange(8, 12));
            foreach (object p in inside) Assert.AreEqual(.5f, ((Vector2)F(p, "Size")).y, 1e-4f, F(p, "Name") + " is thin (PAX-073)");
            Assert.AreEqual(1, ((IEnumerable)F(room, "Elements")).Cast<object>().Count(e => F(e, "Kind").ToString() == "FallingBlock"));
            Assert.AreEqual(1, ((IEnumerable)F(room, "BaitGaps")).Cast<object>().Count());
        }

        [Test]
        public void Solution_Completes_WithEveryTimedWindowAtItsThreshold_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            Assert.Greater(((IList)F(report, "Windows")).Count, 0, "no timed step");
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        // Pinned as TrapLab3/4 are: the windows and the lead at the provisional thresholds.
        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room5Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room5Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        static readonly int[] Room5Windows = { 14, 26 };
        static readonly string[] Room5Leads = { "Block_P5=14" };

        // R2: both timed steps stay inside Precision_Run for their whole window, so they need the section's 8 (both
        // measure well above it); a window whose ticks leave the section keeps D-056's 12.
        [Test]
        public void TimedStepsInsideTheSection_NeedItsSlack_AndAWindowOutsideItNeedsTwelve()
        {
            object solution = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            Type validator = T("RouteValidator");
            foreach (object w in (IEnumerable)F(report, "Windows"))
                Assert.AreEqual(8, (int)Call(validator, "RequiredWindowTicks", Room(), solution, w, provisional), w.ToString());
            object atTheStart = Activator.CreateInstance(T("WindowResult"));
            Set(atTheStart, "AuthoredTick", 5); Set(atTheStart, "High", 10);
            Assert.AreEqual(12, (int)Call(validator, "RequiredWindowTicks", Room(), solution, atTheStart, provisional), "the cat is on Start_Floor, outside the section");
            Assert.AreEqual(12, (int)Call(validator, "RequiredWindowTicks", Room(), solution, ((IList)F(report, "Windows"))[0], null), "no thresholds: D-056's 12");
        }

        // Seen red: the precision jumps need the marker. Without it every 3.2 u jump and the 3.0 u jump up to the Exit
        // fail D-056's 0.75.
        [Test]
        public void WithoutTheMarker_TheRoomFailsReach()
        {
            object room = Room();
            object bare = Activator.CreateInstance(room.GetType(), F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), F(room, "Elements"), F(room, "Openings"), F(room, "RequiredJumps"), F(room, "RequiredSteps"), null, F(room, "BaitGaps"));
            List<string> errors = Rule("ValidateWithThresholds", "TrapLab5 unmarked", bare, provisional);
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(5, errors.Count(e => e.Contains("jump beyond 0.75")), string.Join("\n", errors));
        }

        [Test]
        public void TheBaitAttempt_FromTheBestTakeoff_Dies()
        {
            object route = Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room5BaitAttempt");
            object replay = ReplayRoute(session, Room(), route);
            List<Rec> records = Records(replay);
            object kill = F(replay, "Kill");
            Assert.NotNull(kill, Dump(replay));
            Assert.AreEqual("Pit_Hazard", F(kill, "Killer"), Dump(replay));
            int leftP8 = records.FindLastIndex(r => r.Grounded && r.Ground == "P8");
            Assert.Greater(leftP8, 0, "never stood on P8\n" + Dump(replay));
            Assert.IsTrue(records.Skip(leftP8 + 1).Any(r => r.Vy > 9f), "no jump after leaving P8 (coyote missed)\n" + Dump(replay));
            Assert.IsFalse(records.Skip(leftP8 + 1).Any(r => r.Grounded), "landed somewhere after the bait jump\n" + Dump(replay));
            float furthest = records.Skip(leftP8 + 1).Max(r => r.X);
            // Where the discrete jump comes down through the Exit's top (-0.5): the leading side against the analytic
            // best-take-off reach, edge to edge (P8's edge 37.4 + ValidateBaitGaps' 5.80 = 43.20).
            var motor = Motor();
            float bottomOffset = motor.ColliderSize.y * .5f;
            int jumpTick = records.FindIndex(leftP8 + 1, r => r.Vy > 9f);
            Rec down = records.Skip(jumpTick).First(r => r.Vy < 0f && r.Y - bottomOffset <= -.5f);
            TestContext.Out.WriteLine($"left P8 at t{records[leftP8].Tick} x {records[leftP8].X:F2}; jumped t{records[jumpTick].Tick} ({records[jumpTick].Tick - records[leftP8].Tick} ticks after the last grounded tick); " +
                $"down through y -0.5 at t{down.Tick} with its leading side at x {down.X + motor.ColliderSize.x * .5f:F2} (analytic best 43.20, Exit's edge 43.9); furthest x {furthest:F2}; killed t{F(kill, "Tick")}");
        }

        // The committed asset (made by PARALLAX/Setup/Precision Thresholds (PAX-076), a §9 step) holds the provisional
        // values, and room 5 passes the plain Validate and route rule with it. Red until that menu has been run.
        [Test]
        public void TheCommittedThresholdsAsset_HoldsTheProvisionalValues_AndRoom5PassesWithIt()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Game/Data/PrecisionThresholds.asset");
            Assert.NotNull(asset, "Assets/_Game/Data/PrecisionThresholds.asset is missing: run PARALLAX/Setup/Precision Thresholds (PAX-076).");
            Assert.AreEqual(.85f, (float)F(asset, "ReachFraction"), 1e-6f);
            Assert.AreEqual(8, (int)F(asset, "SlackTicks"));
            List<string> errors = Rule("Validate", "TrapLab5", Room());
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }
    }
}
