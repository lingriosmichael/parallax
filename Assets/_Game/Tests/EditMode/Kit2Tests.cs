using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-118-121 (D-197), PAX-122 (D-201), D-203: pendulums, plates and gates, portals, aiming turrets and phase platforms: their math, their
    // rules, and each through the real game code in Kit2Fixtures' rooms (solution, betrayal, killer, and the section rewind exact).
    public sealed class Kit2Tests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string method, params object[] args) => Call(T("Kit2Fixtures"), method, args);
        static string Rules(object room) => string.Join("\n", (List<string>)Validator.GetMethod("ValidateNewMovers").Invoke(null, new[] { "K2", room }));
        void Pass(string room, string routes)
        {
            object report = Call(T("RouteValidator"), "Run", session, "K2", Fixture(room), Fixture(routes));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        // ---------- math ----------
        [Test] public void APendulum_SwingsAboutItsCentre()
        {
            Assert.AreEqual(270f, TrapPath.SwingAngle(270f, 50f, 120, 0), 1e-3f);
            Assert.AreEqual(320f, TrapPath.SwingAngle(270f, 50f, 120, 30), 1e-3f);
            Assert.AreEqual(220f, TrapPath.SwingAngle(270f, 50f, 120, 90), 1e-3f);
        }
        [Test] public void ATurret_AimsLocksFiresRests()
        {
            Assert.AreEqual(-1, TrapPath.TurretPhase(-1, 50, 12, 10));
            Assert.AreEqual(0, TrapPath.TurretPhase(49, 50, 12, 10));
            Assert.AreEqual(1, TrapPath.TurretPhase(50, 50, 12, 10));
            Assert.AreEqual(2, TrapPath.TurretPhase(62, 50, 12, 10));
            Assert.AreEqual(3, TrapPath.TurretPhase(72, 50, 12, 10));
        }
        [Test] public void ACeilingTurret_TracksOnlyACatBelowInSightAndInItsZone()
        {
            var any = new UnityEngine.Rect();
            var floor = new UnityEngine.Rect(-10f, -5f, 20f, 4f);
            Assert.IsTrue(TrapPath.TurretTracks(new UnityEngine.Vector2(0f, 5f), false, any, 0f));
            Assert.IsTrue(TrapPath.TurretTracks(new UnityEngine.Vector2(3f, -4f), true, any, 6f));
            Assert.IsFalse(TrapPath.TurretTracks(new UnityEngine.Vector2(3f, 4f), true, any, 6f));
            Assert.IsFalse(TrapPath.TurretTracks(new UnityEngine.Vector2(3f, -4f), true, any, 4.9f));
            Assert.IsTrue(TrapPath.TurretTracks(new UnityEngine.Vector2(9f, -4f), true, floor, 10f));
            Assert.IsFalse(TrapPath.TurretTracks(new UnityEngine.Vector2(11f, -4f), true, floor, 12f));
            Assert.IsFalse(TrapPath.TurretTracks(new UnityEngine.Vector2(3f, -6f), true, floor, 7f));
        }
        [Test] public void APhasePlatform_IsThereThenFlickersThenGoes()
        {
            Assert.AreEqual(0, TrapPath.PhaseState(-1, 80, 12));
            Assert.AreEqual(1, TrapPath.PhaseState(0, 80, 12));
            Assert.AreEqual(1, TrapPath.PhaseState(67, 80, 12));
            Assert.AreEqual(2, TrapPath.PhaseState(68, 80, 12));
            Assert.AreEqual(2, TrapPath.PhaseState(79, 80, 12));
            Assert.AreEqual(0, TrapPath.PhaseState(80, 80, 12));
        }

        // ---------- rules ----------
        [Test] public void TheFixtureRooms_PassEveryRule()
        {
            foreach (string room in new[] { "PendulumRoom", "PlateRoom", "PortalRoom", "TurretRoom", "CeilingTurretRoom", "PhaseRoom" }) Assert.IsEmpty(Rules(Fixture(room)), room);
        }
        [Test] public void APhasePlatformWithAShortFlicker_IsRejected() => StringAssert.Contains("flicker is 3 ticks", Rules(Fixture("PhaseRoom", 3)));
        [Test] public void ACeilingTurretWithItsMuzzleInTheCeiling_IsRejected() => StringAssert.Contains("inside a fixed solid", Rules(Fixture("CeilingTurretRoom", 6.1f)));
        [Test] public void APendulumTooFast_IsRejected() => StringAssert.Contains("tip moves", Rules(Fixture("PendulumRoom", 170f)));
        [Test] public void ANarrowPlate_IsRejected() => StringAssert.Contains("a plate is at least", Rules(Fixture("PlateRoom", .3f)));
        [Test] public void ANarrowPortal_IsRejected() => StringAssert.Contains("is smaller than", Rules(Fixture("PortalRoom", .3f)));
        [Test] public void ATurretWithAShortLock_IsRejected() => StringAssert.Contains("locks for 3 ticks", Rules(Fixture("TurretRoom", 3)));

        // ---------- the real game code ----------
        [Test] public void ThePendulumRoutes_Pass() => Pass("PendulumRoom", "PendulumRoutes");
        [Test] public void ThePlateRoutes_Pass() => Pass("PlateRoom", "PlateRoutes");
        [Test] public void ThePortalRoutes_Pass() => Pass("PortalRoom", "PortalRoutes");
        [Test] public void TheTurretRoutes_Pass() => Pass("TurretRoom", "TurretRoutes");
        [Test] public void TheCeilingTurretRoutes_Pass() => Pass("CeilingTurretRoom", "TurretRoutes");
        [Test] public void ThePhaseRoutes_Pass() => Pass("PhaseRoom", "PhaseRoutes");
    }
}
