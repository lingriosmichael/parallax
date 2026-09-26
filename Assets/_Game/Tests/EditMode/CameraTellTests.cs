using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §2.6/§6: the camera tell rule. Every dying betrayal's reveal is on screen for at least 6 ticks
    // before it can first kill, at 4:3, 16:9 and 20:9, worst of 30/60 fps x 4 phases x 3 starting look directions.
    public sealed class CameraTellTests
    {
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static LevelCameraConfig Camera() => AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
        static IList LabRooms() => (IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);

        List<object> Tell(string id, object room, object routes, List<string> errors) =>
            ((IEnumerable)Invoke(Validator, "CameraTell", session, id, room, routes, Camera(), errors)).Cast<object>().ToList();

        static string Table(IEnumerable<object> results) => string.Join("\n", results.Select(r => r.ToString()));

        [TestCase("L001")]
        [TestCase("L002")]
        [TestCase("L003")]
        [TestCase("L004")]
        [TestCase("L005")]
        [TestCase("L006")]
        [TestCase("L007")]
        [TestCase("L008")]
        [TestCase("L009")]
        [TestCase("L010")]
        public void ShippedLevel_PassesTheCameraTellRule_AtEveryAspect(string id)
        {
            var errors = new List<string>();
            List<object> results = Tell(id, RouteValidatorTests.Room(id), RouteValidatorTests.Routes(id), errors);
            TestContext.Out.WriteLine(Table(results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void TrapLabRoom_PassesTheCameraTellRule_AtEveryAspect(int index)
        {
            object routes = Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room" + index);
            var errors = new List<string>();
            List<object> results = Tell("TrapLab" + index, LabRooms()[index], routes, errors);
            TestContext.Out.WriteLine(Table(results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // R3: the recorded cat position is the Transform the camera follows: the collider centre minus the collider
        // offset while gravity is down (the prefab's Rigidbody2D interpolation doesn't run in the harness).
        [Test]
        public void TheRecordedCatPosition_IsTheTransform()
        {
            object replay = ReplayRoute(session, RouteValidatorTests.Room("L001"), F(RouteValidatorTests.Routes("L001"), "Solution"));
            var offset = Motor().ColliderOffset;
            int checkedTicks = 0;
            foreach (object r in (IEnumerable)F(replay, "Records"))
            {
                if ((bool)F(r, "GravityUp")) continue;
                Assert.AreEqual((float)F(r, "X") - offset.x, (float)F(r, "CatX"), 1e-4f, "tick " + F(r, "Tick"));
                Assert.AreEqual((float)F(r, "Y") - offset.y, (float)F(r, "CatY"), 1e-4f, "tick " + F(r, "Tick"));
                checkedTicks++;
            }
            Assert.Greater(checkedTicks, 100);
        }

        // Trap Lab rooms 0-2 declare no routes (they predate D-079), so the rule has no reveal to measure there.
        [Test]
        public void TrapLabRooms0To2_DeclareNoRoutes()
        {
            Type routes = Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor");
            for (int i = 0; i <= 2; i++) Assert.IsNull(routes.GetMethod("Room" + i), "Trap Lab room " + i + " now has routes: add it to the camera tell tests");
        }

        // Seen red as built: the reveal (ArrowX's tell) is 34 u ahead of the cat, off screen until the arrow is well
        // into its flight; it can first kill at the end of the tell.
        [Test]
        public void ARevealOffScreen_InFollowMode_Fails()
        {
            var errors = new List<string>();
            List<object> results = Tell("Fixture", Fixture("CameraTellRoom", 60f, 40f, 6f), Fixture("CameraTellRoutes"), errors);
            TestContext.Out.WriteLine(Table(results) + "\n" + string.Join("\n", errors));
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.All(r => !(bool)F(r, "Fit")));
            Assert.AreEqual(3, errors.Count, "one failure per aspect");
            Assert.IsTrue(results.All(r => (int)F(r, "Lead") >= 6), "the plain route lead passes: only the camera catches this");
        }

        // R10: with the reveal in view, follow mode gives the whole lead, as fit mode does: a tell of D-078's minimum 6
        // passes at every aspect (no frame delay is counted).
        [Test]
        public void TheSameRoom_WithTheTriggerMovedNearTheLauncher_Passes_WithTheWholeLeadOnScreen()
        {
            var errors = new List<string>();
            List<object> results = Tell("Fixture", Fixture("CameraTellRoom", 60f, 40f, 33f), Fixture("CameraTellRoutes"), errors);
            TestContext.Out.WriteLine(Table(results));
            Assert.AreEqual(3, results.Count);
            // PAX-083: in fit mode the whole lead passes trivially, so the camera must really follow here.
            Assert.IsTrue(results.All(r => !(bool)F(r, "Fit")), "the room must be in follow mode at every aspect\n" + Table(results));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            foreach (object r in results) Assert.AreEqual((int)F(r, "Lead"), (int)F(r, "OnScreenLead"), r.ToString());
            Assert.AreEqual(6, (int)F(results[0], "Lead"), "the fixture arrow's tell is D-078's minimum");
        }

        // PAX-083: a replay that stops before the lead's end (or recorded nothing) fails with a clear message. Before the
        // guard, the short one read as on screen to the end and overstated the lead; the empty one was an index error.
        [TestCase(-1)]
        [TestCase(0)]
        public void AShortOrEmptyReplay_FailsWithAClearMessage(int keep)
        {
            object room = Fixture("CameraTellRoom", 60f, 40f, 33f), betrayal = ((IList)F(Fixture("CameraTellRoutes"), "Betrayals"))[0];
            object replay = ReplayRoute(session, room, F(betrayal, "Route"));
            object lead = Call(T("RouteValidator"), "Lead", replay, betrayal);
            int reveal = (int)F(lead, "FirstVisibleTick"), end = reveal + (int)F(lead, "Lead");
            var records = (IList)F(replay, "Records");
            int count = keep < 0 ? end - 1 : keep;
            while (records.Count > count) records.RemoveAt(records.Count - 1);

            var e = Assert.Throws<InvalidOperationException>(() => Invoke(Validator, "OnScreenLead", replay, ElementIndex(replay, "ArrowX"), reveal, end,
                new UnityEngine.Vector2(30f, 3f), new UnityEngine.Vector2(62f, 12f), 16f / 9f, Camera(), 30, 0f, 0f));
            StringAssert.Contains(keep < 0 ? $"has {end - 1} ticks, short of the lead's end t{end}" : "recorded no ticks", e.Message);
        }

        [Test]
        public void AFitModeRoom_PassesTrivially()
        {
            var errors = new List<string>();
            List<object> results = Tell("Fixture", Fixture("CameraTellRoom", 18f, 16f, 6f), Fixture("CameraTellRoutes"), errors);
            TestContext.Out.WriteLine(Table(results));
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.All(r => (bool)F(r, "Fit")));
            CollectionAssert.IsEmpty(errors);
        }
    }
}
