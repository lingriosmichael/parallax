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
    // PAX-076 (D-083), amended 2026-10-03: the camera rule. Surprise is allowed (a trap may fire from off screen), but every
    // dying betrayal's killer, in its lethal pose, is on screen at some point before the death hold ends (the camera keeps
    // easing during the hold, D-058 amendment), at 4:3, 16:9 and 20:9, worst of 30/60 fps x 4 phases x 3 starting look
    // directions.
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

        static IEnumerable<string> Levels() => Enumerable.Range(1, 20).Select(i => "L" + i.ToString("000"));

        [TestCaseSource(nameof(Levels))]
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

        // The amendment's point: ArrowX's tell is 34 u ahead of the cat, off screen until the arrow is well into its flight
        // (this failed the old rule, a reveal on screen 6 ticks before it can kill). The arrow reaches the cat, so it's on
        // screen at the kill: the surprise passes.
        [Test]
        public void ATrapThatFiresFromOffScreen_Passes_WhenItsKillerIsOnScreenAtTheDeath()
        {
            var errors = new List<string>();
            List<object> results = Tell("Fixture", Fixture("CameraTellRoom", 60f, 40f, 6f), Fixture("CameraTellRoutes"), errors);
            TestContext.Out.WriteLine(Table(results) + "\n" + string.Join("\n", errors));
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.All(r => !(bool)F(r, "Fit")), "the camera must really follow here\n" + Table(results));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            Assert.IsTrue(results.All(r => (bool)F(r, "KillerOnScreen") && (string)F(r, "Killer") == "ArrowX"), Table(results));
        }

        // The rule's check itself: the same death, with the arrow drawn 100 u away at the kill tick, stays off screen through
        // the whole hold. Red first: unmoved, it's on screen.
        [Test]
        public void AKillerDrawnOffScreen_AtTheKill_Fails()
        {
            object room = Fixture("CameraTellRoom", 60f, 40f, 33f), betrayal = ((IList)F(Fixture("CameraTellRoutes"), "Betrayals"))[0];
            object replay = ReplayRoute(session, room, F(betrayal, "Route"));
            int kill = (int)F(F(replay, "Kill"), "Tick"), arrow = ElementIndex(replay, "ArrowX");
            var centre = new UnityEngine.Vector2(30f, 3f); var size = new UnityEngine.Vector2(62f, 12f);
            bool OnScreen() => (bool)Invoke(Validator, "KillerOnScreen", replay, arrow, kill, 30, centre, size, 16f / 9f, Camera(), 30, 0f, 0f);
            Assert.IsTrue(OnScreen(), "the arrow kills the cat where it stands: on screen");

            object record = ((IList)F(replay, "Records"))[kill];
            var bounds = (Array)F(record, "RenderBounds");
            var b = (UnityEngine.Rect)bounds.GetValue(arrow);
            bounds.SetValue(new UnityEngine.Rect(b.x + 100f, b.y, b.width, b.height), arrow);
            if (F(record, "TurnedCorners") is Array corners) corners.SetValue(null, arrow);
            Assert.IsFalse(OnScreen(), "drawn 100 u away at the kill tick");
        }

        // D-058 amendment: a fall that outruns the camera. The fixture's cat drops 40 u down a shaft into Pit; at the kill
        // tick the camera, smoothed, is still above it. With no hold it's off screen (red first); the camera eases on during
        // the 30-tick hold and shows Pit before the room resets.
        [Test]
        public void AFallThatOutrunsTheCamera_IsShownBeforeTheHoldEnds()
        {
            object room = Fixture("EscapeTellRoom"), betrayal = ((IList)F(Fixture("EscapeTellRoutes"), "Betrayals"))[0];
            object replay = ReplayRoute(session, room, F(betrayal, "Route"));
            int kill = (int)F(F(replay, "Kill"), "Tick"), pit = ElementIndex(replay, (string)F(betrayal, "Killer"));
            Type builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
            var frame = (UnityEngine.Bounds)builder.GetMethod("ComputeRoomBounds").Invoke(null, new object[] { room, Camera().ViewMargin });
            var centre = (UnityEngine.Vector2)frame.center - (UnityEngine.Vector2)F(room, "Origin"); var size = (UnityEngine.Vector2)frame.size;
            bool OnScreen(int hold) => (bool)Invoke(Validator, "KillerOnScreen", replay, pit, kill, hold, centre, size, 16f / 9f, Camera(), 30, 0f, 0f);
            Assert.IsFalse(OnScreen(0), "at the kill tick the camera hasn't followed the fall down to Pit");
            Assert.IsTrue(OnScreen(30), "the camera reaches it during the hold");
        }

        // PAX-083: a replay that stops before the kill (or recorded nothing) fails with a clear message, not a guess.
        [TestCase(-1)]
        [TestCase(0)]
        public void AShortOrEmptyReplay_FailsWithAClearMessage(int keep)
        {
            object room = Fixture("CameraTellRoom", 60f, 40f, 33f), betrayal = ((IList)F(Fixture("CameraTellRoutes"), "Betrayals"))[0];
            object replay = ReplayRoute(session, room, F(betrayal, "Route"));
            int kill = (int)F(F(replay, "Kill"), "Tick");
            var records = (IList)F(replay, "Records");
            int count = keep < 0 ? kill : keep;
            while (records.Count > count) records.RemoveAt(records.Count - 1);

            var e = Assert.Throws<InvalidOperationException>(() => Invoke(Validator, "KillerOnScreen", replay, ElementIndex(replay, "ArrowX"), kill, 30,
                new UnityEngine.Vector2(30f, 3f), new UnityEngine.Vector2(62f, 12f), 16f / 9f, Camera(), 30, 0f, 0f));
            StringAssert.Contains(keep < 0 ? $"has {kill} ticks, short of the kill at t{kill}" : "recorded no ticks", e.Message);
        }

        // PAX-060 (D-097): a betrayal with a declared escape is checked against its last escape tick, not its kill. In the
        // fixture the escape still works long after the camera has followed the falling cat away from Lip: it fails.
        [Test]
        public void AnEscapeBackedReveal_ThatLeavesTheViewBeforeTheLastEscape_Fails()
        {
            var errors = new List<string>();
            List<object> all = Tell("Fixture", Fixture("EscapeTellRoom"), Fixture("EscapeTellRoutes"), errors);
            TestContext.Out.WriteLine(Table(all) + "\n" + string.Join("\n", errors));
            List<object> results = all.Where(r => (bool)F(r, "Escape")).ToList();
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.All(r => !(bool)F(r, "Fit")), "escape-backed rows in follow mode\n" + Table(results));
            List<string> escapeErrors = errors.Where(e => e.Contains("(D-097")).ToList();
            Assert.AreEqual(3, escapeErrors.Count, "one escape failure per aspect\n" + Table(all));
            foreach (object r in results)
            {
                Assert.Greater((int)F(r, "OnScreenLead"), 0, "Lip is on screen as it gives way: " + r);
                Assert.Less((int)F(r, "OnScreenLead"), (int)F(r, "Required"), r.ToString());
            }
        }

        // PAX-060 (D-097): L014's T1 (Lip_1 gives way at the cat's feet) with its escape declared: Lip_1 is on screen from
        // the collapse through the last press that still catches V2 and finishes the level, at every aspect.
        [Test]
        public void L014_T1_WithItsEscapeDeclared_Passes()
        {
            object routes = RouteValidatorTests.Routes("L014");
            object t1 = ((IList)F(routes, "Betrayals"))[0];
            Assert.NotNull(F(t1, "Escape"), "T1 declares its escape");
            Array justT1 = Array.CreateInstance(T("Betrayal"), 1);
            justT1.SetValue(t1, 0);
            object only = Activator.CreateInstance(T("RoomRoutes"), F(routes, "Solution"), justT1);
            var errors = new List<string>();
            List<object> results = Tell("L014", RouteValidatorTests.Room("L014"), only, errors).Where(r => (bool)F(r, "Escape")).ToList();
            TestContext.Out.WriteLine(Table(results));
            Assert.AreEqual(3, results.Count);
            // The escape rule only (the killer-at-death rule is checked with the level, in ShippedLevel_…).
            List<string> escapeErrors = errors.Where(e => e.Contains("(D-097")).ToList();
            CollectionAssert.IsEmpty(escapeErrors, string.Join("\n", escapeErrors));
            foreach (object r in results)
            {
                Assert.IsTrue((bool)F(r, "Escape"), r.ToString());
                Assert.GreaterOrEqual((int)F(r, "LastEscape") - (int)F(r, "Reveal") + 1, 12, "at least WindowTicks on screen by the last escape: " + r);
            }
        }

        // D-104: the fit-mode branch, under a 16 u view (the fixture's frame, 16 x 10, never fits a shipped level's zoomed
        // view): 15 wide, so the frame fits at 4:3.
        [Test]
        public void AFitModeRoom_PassesTrivially()
        {
            var errors = new List<string>();
            var wide = UnityEngine.Object.Instantiate(Camera());
            var so = new SerializedObject(wide);
            so.FindProperty("maxViewHeight").floatValue = 16f;
            so.ApplyModifiedPropertiesWithoutUndo();
            List<object> results;
            try { results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "Fixture", Fixture("CameraTellRoom", 15f, 13f, 6f), Fixture("CameraTellRoutes"), wide, errors)).Cast<object>().ToList(); }
            finally { UnityEngine.Object.DestroyImmediate(wide); }
            TestContext.Out.WriteLine(Table(results));
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.All(r => (bool)F(r, "Fit")));
            CollectionAssert.IsEmpty(errors);
        }
    }
}
