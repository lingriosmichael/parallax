using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-114/115/116 (D-187): the path mover, the glitch wall, the flip platform and the spinner: their pure math, their rules,
    // and each through the real game code in NewMoverFixtures' rooms (solution, betrayals, killers, and the section rewinds exact).
    public sealed class NewMoverTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        IDisposable session;
        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Fixture(string method, params object[] args) => Call(T("NewMoverFixtures"), method, args);
        static string Rules(object room) => string.Join("\n", (List<string>)Validator.GetMethod("ValidateNewMovers").Invoke(null, new[] { "NM", room }));

        void AssertRoutesPass(object room, object routes)
        {
            object report = Call(T("RouteValidator"), "Run", session, "NM", room, routes);
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), (string)report.GetType().GetMethod("Summary").Invoke(report, null));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
        }

        string Killer(object room, object routes, int betrayal)
        {
            object b = ((IList)F(routes, "Betrayals"))[betrayal];
            object replay = ReplayRoute(session, room, F(b, "Route"));
            return (string)F(F(replay, "Kill"), "Killer");
        }

        // ---------- math ----------

        [Test]
        public void APath_IsWalkedByArcLength()
        {
            var square = new[] { new Vector2(4f, 0f), new Vector2(4f, 2f), new Vector2(0f, 2f), Vector2.zero };
            Assert.AreEqual(12f, TrapPath.Length(square), 1e-4f);
            Assert.AreEqual(new Vector2(3f, 0f), TrapPath.Along(square, .25f));
            Assert.AreEqual(new Vector2(4f, 2f), TrapPath.Along(square, .5f));
            Assert.AreEqual(Vector2.zero, TrapPath.Along(square, 1f));
        }

        [Test]
        public void AGlitchWall_SitsOnEachPointAndJumpsEvenly()
        {
            var points = new[] { new Vector2(0f, 3f), Vector2.zero };
            Assert.AreEqual(Vector2.zero, TrapPath.Offset(points, true, 39, 80, 0, 0));
            Assert.AreEqual(new Vector2(0f, 3f), TrapPath.Offset(points, true, 40, 80, 0, 0));
            Assert.AreEqual(Vector2.zero, TrapPath.Offset(points, true, 80, 80, 0, 0));
            Assert.IsFalse(TrapPath.GlitchWarning(points, 29, 80, 0, 0, 10, out _));
            Assert.IsTrue(TrapPath.GlitchWarning(points, 30, 80, 0, 0, 10, out int next));
            Assert.AreEqual(1, next);
        }

        [Test]
        public void AFlipAndASpinner_AreFunctionsOfTicks()
        {
            Assert.AreEqual(90f, TrapPath.FlipAngle(5, 10, 40, 10), 1e-4f);
            Assert.AreEqual(180f, TrapPath.FlipAngle(30, 10, 40, 10), 1e-4f);
            Assert.AreEqual(0f, TrapPath.FlipAngle(60, 10, 40, 10), 1e-4f);
            Assert.AreEqual(30f, TrapPath.SpinAngle(0f, 3f, 10), 1e-4f);
            Assert.AreEqual(350f, TrapPath.SpinAngle(10f, -2f, 10), 1e-4f);
        }

        // ---------- rules ----------

        [Test] public void TheFixtureRooms_PassEveryNewMoverRule()
        {
            Assert.IsEmpty(Rules(Fixture("GlitchRoom")));
            Assert.IsEmpty(Rules(Fixture("FlipRoom")));
            Assert.IsEmpty(Rules(Fixture("SpinnerRoom")));
        }

        [Test] public void AGlitchWarningShorterThanSixTicks_IsRejected() => StringAssert.Contains("glitch warning is 3 ticks", Rules(Fixture("GlitchRoom", 3)));
        [Test] public void AFlipFasterThanSixTicks_IsRejected() => StringAssert.Contains("flips over 4 ticks", Rules(Fixture("FlipRoom", 4)));
        [Test] public void ASpinnerTooFast_IsRejected() => StringAssert.Contains("tip moves", Rules(Fixture("SpinnerRoom", 20f)));

        // ---------- the real game code ----------

        [Test] public void TheGlitchRoutes_PassTheRouteValidator() => AssertRoutesPass(Fixture("GlitchRoom"), Fixture("GlitchRoutes"));
        [Test] public void AGlitchWallAppearingOnTheCat_IsTheKiller() => Assert.AreEqual("Gate", Killer(Fixture("GlitchRoom"), Fixture("GlitchRoutes"), 1));

        [Test] public void TheFlipRoutes_PassTheRouteValidator() => AssertRoutesPass(Fixture("FlipRoom"), Fixture("FlipRoutes"));
        [Test] public void AFlipsSpikedUnderside_KillsAtRest() => Assert.AreEqual("Flip_C", (string)F(F(ReplayRoute(session, Fixture("FlipRoom"), Fixture("FlipJumpUnder")), "Kill"), "Killer"));

        [Test] public void TheSpinnerRoutes_PassTheRouteValidator() => AssertRoutesPass(Fixture("SpinnerRoom"), Fixture("SpinnerRoutes"));
    }
}
