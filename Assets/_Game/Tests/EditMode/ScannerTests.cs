using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-113 (D-178): the scanner (a Hazard mover that warns at home first): its rules, and the scanner through the real game code
    // in ScannerFixtures' corridor: harmless resting and warning, lethal once it moves, crossable once it's home, and the section
    // rewind exact.
    public sealed class ScannerTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        const int Warn = 12, Phase = 80;

        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Room(int warn = Warn, float top = 4f, float rise = 0f) => Call(T("ScannerFixtures"), "Room", warn, top, rise);
        static object Routes() => Call(T("ScannerFixtures"), "Routes");
        static string Rules(object room) => string.Join("\n", (List<string>)Validator.GetMethod("ValidateScanner").Invoke(null, new[] { "SC", room }));

        // ---------- rules ----------

        [Test]
        public void TheFixtureRoom_PassesEveryScannerRule() => Assert.IsEmpty(Rules(Room()));

        [Test]
        public void AWarningShorterThanSixTicks_IsRejected() => StringAssert.Contains("warning is 3 ticks", Rules(Room(warn: 3)));

        [Test]
        public void ABeamShortOfTheCeiling_IsRejected() => StringAssert.Contains("doesn't reach the ceiling", Rules(Room(top: 3f)));

        [Test]
        public void ASweepThatRisesOrFalls_IsRejected() => StringAssert.Contains("sweeps sideways only", Rules(Room(rise: 1f)));

        // ---------- the real game code ----------

        [Test]
        public void TheRoutes_PassTheRouteValidator_WithRewindsExact()
        {
            object report = Call(T("RouteValidator"), "Run", session, "SC", Room(), Routes());
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        [Test]
        public void ACatAtItsHome_DiesAsItSetsOff_NotDuringTheWarning()
        {
            object b = ((IList)F(Routes(), "Betrayals"))[1];
            object replay = ReplayRoute(session, Room(), F(b, "Route"));
            List<Rec> records = Records(replay);
            int scan = ElementIndex(replay, "Scan");
            Rec death = records.First(r => r.Dead);
            Assert.AreEqual(death.FireTick[scan] + Warn, death.RoomLifeTick, "the kill comes on the first moving tick, after the warning:\n" + Dump(replay));
            Assert.AreEqual("Scan", F(F(replay, "Kill"), "Killer"), Dump(replay));
        }

        [Test]
        public void TheWaitingCat_StandsBesideTheSweepUnharmed()
        {
            object replay = ReplayRoute(session, Room(), F(Routes(), "Solution"));
            List<Rec> records = Records(replay);
            Assert.IsTrue(records.Last().Complete, Dump(replay));
            int scan = ElementIndex(replay, "Scan");
            Assert.AreEqual(Phase, records.First(r => r.FireTick[scan] >= 0).FireTick[scan], "the scanner fires on its phase:\n" + Dump(replay));
        }
    }
}
