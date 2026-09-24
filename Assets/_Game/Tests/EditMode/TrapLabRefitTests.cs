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

namespace Parallax.Tests.EditMode
{
    // PAX-076: Trap Lab rooms 0-2 refitted to D-082's cat (apex 1.6). They declare no routes, so these replays check
    // only that the current cat gets through, from a start past each room's first trap. Seen red on the old layout:
    // room 1 stopped at x 21.00 against the 2-tall FixedPillar, and every room failed a layout rule (1-high triggers,
    // PeriodicSpikes' reveal lead 0).
    public sealed class TrapLabRefitTests
    {
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Room(int index) => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[index];
        static readonly Type R = Type.GetType("Parallax.Editor.Routes.R, Parallax.Editor");
        static object S(string m, params object[] a) => Call(R, m, a);
        static object Route(string name, params object[] steps)
        {
            Type step = Type.GetType("Parallax.Editor.Routes.RouteStep, Parallax.Editor");
            Array array = Array.CreateInstance(step, steps.Length);
            for (int i = 0; i < steps.Length; i++) array.SetValue(steps[i], i);
            return Activator.CreateInstance(Type.GetType("Parallax.Editor.Routes.Route, Parallax.Editor"), name, array);
        }

        object Replay(int room, object route, float startX = -1f)
        {
            object options = null;
            if (startX >= 0f) { options = Activator.CreateInstance(T("ReplayOptions")); Set(options, "StartCentre", (Vector2?)new Vector2(startX, .28f)); }
            return ReplayRoute(session, Room(room), route, options);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Room_PassesEveryLayoutRule(int index)
        {
            object room = Room(index);
            string id = "TrapLab" + index;
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", id, room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", id, room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", id, room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", id, room, AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset")));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Room0_TheFloorReachesTheDoor_AndAStandingJumpLandsOnThinPlatform()
        {
            object run = Replay(0, Route("run", S("Hold", 1), S("Until", S("RoomComplete"))), 8f);
            Assert.IsTrue((bool)F(run, "Completed"), Dump(run));
            object hop = Replay(0, Route("hop up", S("Jump"), S("For", 4), S("Hold", 1), S("For", 10), S("Release"), S("Until", S("Grounded")), S("Until", S("Still"))));
            Assert.IsTrue(Records(hop).Any(r => r.Grounded && r.Ground == "ThinPlatform"), Dump(hop));
        }

        [Test]
        public void Room1_JumpsOverFixedPillarAndCrusher_ReachTheDoor()
        {
            object replay = Replay(1, Route("over the pillar and the crusher", S("Hold", 1), S("Until", S("XAtLeast", 20.6f)), S("Jump"), S("Until", S("Airborne")), S("Until", S("Grounded")),
                S("Until", S("XAtLeast", 24.6f)), S("Jump"), S("Until", S("RoomComplete"))), 18f);
            Assert.IsTrue((bool)F(replay, "Completed"), Dump(replay));
        }

        [Test]
        public void Room2_AJumpOverThePit_ReachesTheDoor()
        {
            object replay = Replay(2, Route("over the pit", S("Hold", 1), S("Until", S("XAtLeast", 21.6f)), S("Jump"), S("Until", S("RoomComplete"))), 12f);
            Assert.IsTrue((bool)F(replay, "Completed"), Dump(replay));
        }
    }
}
