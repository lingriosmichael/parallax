using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-112 (D-176): the pulse gate's phases (pure), its layout rules, and the gate through the real game code (the route
    // harness) in PulseGateFixtures' corridor: warn harmless, on lethal, off crossable, and the section rewind exact.
    public sealed class PulseGateTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        const int Period = 120, Warn = 12, On = 48;

        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Room(int warn = Warn, int on = On, int period = Period, float top = 4f) => Call(T("PulseGateFixtures"), "Room", warn, on, period, top);
        static object Routes() => Call(T("PulseGateFixtures"), "Routes");
        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static List<string> Rules(object room) => (List<string>)Validator.GetMethod("ValidatePulseGate").Invoke(null, new object[] { "PG", room, Motor() });

        // ---------- pure phases ----------

        [TestCase(-1, PulseGatePhase.Off)]
        [TestCase(0, PulseGatePhase.Warn)]
        [TestCase(11, PulseGatePhase.Warn)]
        [TestCase(12, PulseGatePhase.On)]
        [TestCase(59, PulseGatePhase.On)]
        [TestCase(60, PulseGatePhase.Off)]
        public void PhaseSince_WarnsThenIsOnThenOff(int since, PulseGatePhase expected) =>
            Assert.AreEqual(expected, PulseGateMath.PhaseSince(since, Warn, On));

        [TestCase(0, PulseGatePhase.Off)]     // before the first fire (phase 10)
        [TestCase(10, PulseGatePhase.Warn)]
        [TestCase(25, PulseGatePhase.On)]
        [TestCase(75, PulseGatePhase.Off)]
        [TestCase(130, PulseGatePhase.Warn)]  // the next period
        public void PhaseAt_FollowsThePeriodicRule(int tick, PulseGatePhase expected) =>
            Assert.AreEqual(expected, PulseGateMath.PhaseAt(tick, Period, 10, Warn, On));

        [Test]
        public void OffTicks_IsWhatWarnAndOnLeave() { Assert.AreEqual(60, PulseGateMath.OffTicks(Period, Warn, On)); Assert.AreEqual(0, PulseGateMath.OffTicks(50, Warn, On)); }

        // ---------- layout rules ----------

        [Test]
        public void TheFixtureRoom_PassesEveryGateRule() => CollectionAssert.IsEmpty(Rules(Room()));

        [Test]
        public void AWarningShorterThanSixTicks_IsRejected() => StringAssert.Contains("warning is 4 ticks", string.Join("\n", Rules(Room(warn: 4))));

        [Test]
        public void AGateThatIsNeverOff_IsRejected() => StringAssert.Contains("never off", string.Join("\n", Rules(Room(on: 108))));

        [Test]
        public void AnOffGapTooShortToCross_IsRejected() => StringAssert.Contains("a cat crossing it at run speed needs", string.Join("\n", Rules(Room(on: 100))));

        [Test]
        public void AGateThatStopsShortOfTheCeiling_IsRejected() => StringAssert.Contains("top end", string.Join("\n", Rules(Room(top: 3f))));

        [Test]
        public void TheGate_IsAKillVolume()
        {
            object room = Room();
            object gate = ((IEnumerable)F(room, "Elements")).Cast<object>().Single(e => (string)F(e, "Name") == "Gate");
            var volumes = (IEnumerable)Validator.GetMethod("KillVolumes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new[] { gate, (object)UnityEngine.Vector2.zero });
            Assert.AreEqual(1, volumes.Cast<object>().Count());
        }

        // ---------- the real game code ----------

        [Test]
        public void TheRoutes_PassTheRouteValidator_WithRewindsExact()
        {
            object report = Call(T("RouteValidator"), "Run", session, "PG", Room(), Routes());
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        [Test]
        public void AStandingCat_DiesOnTheFirstOnTick_NotDuringTheWarning()
        {
            object b = ((IList)F(Routes(), "Betrayals"))[1];
            object replay = ReplayRoute(session, Room(), F(b, "Route"));
            List<Rec> records = Records(replay);
            int gate = ElementIndex(replay, "Gate");
            Rec death = records.First(r => r.Dead);
            int fire = death.FireTick[gate];
            Assert.GreaterOrEqual(fire, 0, Dump(replay));
            Assert.AreEqual(fire + Warn, death.RoomLifeTick, "the kill comes on the first On tick, after the whole warning:\n" + Dump(replay));
            Assert.AreEqual("Gate", F(F(replay, "Kill"), "Killer"), Dump(replay));
        }
    }
}
