using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110 and amendment 2): Trap Lab room 14, the wall room (TrapLabLayout.WallCling.cs): its routes pass the route
    // validator (the solution climbs the grip shaft wall to wall; Plain_Wall gives no hold and Spike_Floor kills with a lead of
    // at least 6; Block_F's rise is survived), touching the plain wall never latches (PAX-106, D-110 amendment 4), and its
    // grip walls pass ValidateGripWall.
    public sealed class TrapLabRoom14Tests
    {
        static readonly Type Layout = Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor");
        static readonly Type Routes = Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor");
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        static object Room() => ((IList)Layout.GetField("Rooms").GetValue(null))[14];
        static object RoomRoutes() => Call(Routes, "Room14");

        [SetUp] public void FreshScene() => FreshScratchScene();

        [Test]
        public void TheRoutes_PassTheRouteValidator()
        {
            object routes = RoomRoutes();
            var errors = (List<string>)Validator.GetMethod("ValidateRoutes", new[] { typeof(string), Room().GetType(), routes.GetType() }).Invoke(null, new[] { "TrapLab14", Room(), routes });
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void TheSolution_ClimbsTheShaft_LatchingThreeTimes_WithNoButton()
        {
            using IDisposable session = OpenSession();
            object replay = ReplayRoute(session, Room(), F(RoomRoutes(), "Solution"));
            Assert.IsTrue((bool)F(replay, "Completed"), Dump(replay));
            var records = ((IList)F(replay, "Records")).Cast<object>().ToList();
            int latches = 0;
            for (int i = 1; i < records.Count; i++) if ((bool)F(records[i], "IsClinging") && !(bool)F(records[i - 1], "IsClinging")) latches++;
            Assert.AreEqual(3, latches, "Shaft_L, Shaft_R, Shaft_L");
        }

        // §14 G5, D-110 amendment 4: touching Plain_Wall never latches: the Dies route jumps at it and never clings.
        [Test]
        public void TouchingThePlainWall_NeverLatches()
        {
            using IDisposable session = OpenSession();
            object betrayal = ((IList)F(RoomRoutes(), "Betrayals"))[0];
            object replay = ReplayRoute(session, Room(), F(betrayal, "Route"));
            var records = ((IList)F(replay, "Records")).Cast<object>().ToList();
            Assert.IsFalse(records.Any(r => (bool)F(r, "IsClinging")), Dump(replay));
        }

        [Test]
        public void TheGripWalls_PassValidateGripWall_AndTheRoomHasOnePlainWall()
        {
            var errors = (List<string>)Validator.GetMethod("ValidateGripWall").Invoke(null, new[] { "TrapLab14", Room(), (object)PrecisionTestApi.Motor() });
            CollectionAssert.IsEmpty(errors);
            var kinds = ((IEnumerable)F(Room(), "Elements")).Cast<object>().ToDictionary(e => (string)F(e, "Name"), e => F(e, "Kind").ToString());
            CollectionAssert.AreEquivalent(new[] { "Lone_Wall", "Shaft_L", "Shaft_R" }, kinds.Where(k => k.Value == "GripWall").Select(k => k.Key));
            Assert.AreEqual("Wall", kinds["Plain_Wall"]);
        }
    }
}
