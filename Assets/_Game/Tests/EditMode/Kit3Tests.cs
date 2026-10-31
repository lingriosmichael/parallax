using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-123/124 (D-276): the trident and the rotating chaser: their math, their rules, and each through the real game code in
    // Kit3Fixtures' rooms (solution, betrayal, killer, and the section rewind exact).
    public sealed class Kit3Tests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string method, params object[] args) => Call(T("Kit3Fixtures"), method, args);
        static string Rules(object room) => string.Join("\n", (List<string>)Validator.GetMethod("ValidateNewMovers").Invoke(null, new[] { "K3", room }));
        void Pass(string room, string routes)
        {
            object report = Call(T("RouteValidator"), "Run", session, "K3", Fixture(room), Fixture(routes));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        // ---------- math ----------
        [Test] public void ATrident_FiresDownAndToEitherSide()
        {
            Assert.AreEqual(-90f, TrapPath.TridentAngle(-90f, 35f, 0), 1e-4f);
            Assert.AreEqual(-125f, TrapPath.TridentAngle(-90f, 35f, 1), 1e-4f);
            Assert.AreEqual(-55f, TrapPath.TridentAngle(-90f, 35f, 2), 1e-4f);
        }
        [Test] public void AChaser_WaitsThenMovesAndTurns()
        {
            Assert.AreEqual(0f, TrapPath.SweepProgress(-1, 30, 100), 1e-5f);
            Assert.AreEqual(0f, TrapPath.SweepProgress(29, 30, 100), 1e-5f);
            Assert.AreEqual(.5f, TrapPath.SweepProgress(80, 30, 100), 1e-5f);
            Assert.AreEqual(1f, TrapPath.SweepProgress(500, 30, 100), 1e-5f);
            Assert.AreEqual(90f, TrapPath.SweepAngle(90f, 6f, 10, 30), 1e-4f);
            Assert.AreEqual(150f, TrapPath.SweepAngle(90f, 6f, 40, 30), 1e-4f);
        }
        [Test] public void AChaser_CanStartTurningLateAndSpeedUp()
        {
            Assert.AreEqual(90f, TrapPath.SweepAngle(90f, -1f, 500, 600, 200), 1e-4f);    // still upright before it starts
            Assert.AreEqual(65f, TrapPath.SweepAngle(90f, -1f, 700, 600, 200), 1e-4f);    // half way up the ramp: 100^2/(2*200) = 25°
            Assert.AreEqual(-10f + 360f, TrapPath.SweepAngle(90f, -1f, 800, 600, 200), 1e-4f);   // the ramp's 100°
            Assert.AreEqual(-110f + 360f, TrapPath.SweepAngle(90f, -1f, 900, 600, 200), 1e-4f);  // then a degree a tick
        }

        // ---------- rules ----------
        [Test] public void TheFixtureRooms_PassEveryRule()
        {
            foreach (string room in new[] { "TridentRoom", "SweeperRoom" }) Assert.IsEmpty(Rules(Fixture(room)), room);
        }
        [Test] public void ATridentWithAShortWarning_IsRejected() => StringAssert.Contains("warns for 5 ticks", Rules(Fixture("TridentRoom", 5)));
        [Test] public void AChaserTurningTooFast_IsRejected() => StringAssert.Contains("tip moves", Rules(Fixture("SweeperRoom", 20f)));

        // ---------- the real game code ----------
        [Test] public void TheTridentRoutes_Pass() => Pass("TridentRoom", "TridentRoutes");
        [Test] public void TheSweeperRoutes_Pass() => Pass("SweeperRoom", "SweeperRoutes");
    }
}
