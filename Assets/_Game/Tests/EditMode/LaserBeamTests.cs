using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-111 (D-177): beam mode on the arrow lane (the laser turret) and the hard-light bar's lane: the phases (pure), the beam
    // rules, and the beam through the real game code in BeamFixtures' corridor: the ray harmless, the lane lethal for exactly
    // the beam ticks, dark after, and the section rewind exact.
    public sealed class LaserBeamTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type Lane = Type.GetType("Parallax.Editor.Setup.ArrowLane, Parallax.Editor");
        const int Period = 120, Tell = 12, Beam = 40;

        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Room(int tell = Tell, int beam = Beam, int period = Period, int cooldown = Period - 1, bool disguised = false) =>
            Call(T("BeamFixtures"), "Room", tell, beam, period, cooldown, disguised);
        static object Routes() => Call(T("BeamFixtures"), "Routes");
        static List<string> Rule(string rule, object room) => ((IList)Validator.GetMethod(rule).Invoke(null, new[] { "LB", room })).Cast<string>().ToList();
        static string AllArrowRules(object room) => string.Join("\n", new[] { "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" }.SelectMany(r => Rule(r, room)));

        // ---------- pure phases ----------

        [TestCase(-1, ArrowBeamPhase.Idle)]
        [TestCase(0, ArrowBeamPhase.Tell)]
        [TestCase(11, ArrowBeamPhase.Tell)]
        [TestCase(12, ArrowBeamPhase.Beam)]
        [TestCase(51, ArrowBeamPhase.Beam)]
        [TestCase(52, ArrowBeamPhase.Idle)]
        public void BeamPhase_IsTellThenBeamThenIdle(int s, ArrowBeamPhase expected) => Assert.AreEqual(expected, ArrowMath.BeamPhase(s, Tell, Beam));

        [Test]
        public void BeamStopTick_IsTellPlusBeam() => Assert.AreEqual(52, ArrowMath.BeamStopTick(Tell, Beam));

        // ---------- lanes ----------

        [Test]
        public void AnOrdinaryLane_HasNoBeamAndNoRay()
        {
            object plain = Activator.CreateInstance(Lane, ArrowDirection.Left, .3f, 10f, .8f, .16f, .3f, 6, false, false, 0f, false, 0f, 0f);
            Assert.IsFalse((bool)F(plain, "Beam")); Assert.IsFalse((bool)F(plain, "WarningRay")); Assert.AreEqual(0, (int)F(plain, "BeamTicks"));
        }

        [Test]
        public void TheHardLightBar_IsASpearWithTheWarningRay()
        {
            object bar = Lane.GetMethod("HardLightBar").Invoke(null, new object[] { ArrowDirection.Right, 1f, 10f, 1.4f, .4f, 1.2f, 8 });
            Assert.IsTrue((bool)F(bar, "Spear")); Assert.IsTrue((bool)F(bar, "WarningRay")); Assert.IsFalse((bool)F(bar, "Beam"));
        }

        // ---------- rules ----------

        [Test]
        public void TheFixtureRoom_PassesEveryArrowRule() => Assert.IsEmpty(AllArrowRules(Room()));

        [Test]
        public void ACooldownEndingBeforeTheBeamIsDark_IsRejected() => StringAssert.Contains("ends before the arrow stops (tell + flight = 52", AllArrowRules(Room(cooldown: 40)));

        [Test]
        public void ADisguisedBeam_IsRejected() => StringAssert.Contains("is a beam and disguised", AllArrowRules(Room(disguised: true)));

        [Test]
        public void ABeamThatIsNeverLethal_IsRejected() => StringAssert.Contains("never lethal", AllArrowRules(Room(beam: 0)));

        // ---------- the real game code ----------

        [Test]
        public void TheRoutes_PassTheRouteValidator_WithRewindsExact()
        {
            object report = Call(T("RouteValidator"), "Run", session, "LB", Room(), Routes());
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        [Test]
        public void ACatUnderTheTurret_DiesOnTheFirstBeamTick_NotDuringTheRay()
        {
            object b = ((IList)F(Routes(), "Betrayals"))[1];
            object replay = ReplayRoute(session, Room(), F(b, "Route"));
            List<Rec> records = Records(replay);
            int turret = ElementIndex(replay, "Turret");
            Rec death = records.First(r => r.Dead);
            Assert.AreEqual(death.FireTick[turret] + Tell, death.RoomLifeTick, "the kill comes on the first beam tick:\n" + Dump(replay));
            Assert.AreEqual("Turret", F(F(replay, "Kill"), "Killer"), Dump(replay));
        }

        [Test]
        public void TheSolutionCrossesUnderTheDarkLane_WhileTheBeamWouldBeLethalLater()
        {
            object replay = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> records = Records(replay);
            int turret = ElementIndex(replay, "Turret");
            Rec under = records.First(r => Math.Abs(r.X - 16f) < .3f);
            int s = under.RoomLifeTick - under.FireTick[turret];
            Assert.AreEqual(ArrowBeamPhase.Idle, ArrowMath.BeamPhase(s, Tell, Beam), "the solution passes under the lane while it's dark:\n" + Dump(replay));
            Assert.IsTrue(records.Last().Complete, Dump(replay));
        }
    }
}
