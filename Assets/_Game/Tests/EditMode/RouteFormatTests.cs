using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-075 (D-079) §5.5: each route action's per-tick semantics, on the flat room. The record's
    // Move/JumpPressed are the command the harness queued for that tick.
    public sealed class RouteFormatTests
    {
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static List<Rec> Ticks(object replay) => Records(replay).Where(x => x.Tick > 0).ToList();

        [Test]
        public void HoldFor_EmitsTheHeldDirectionForExactlyNTicks_ThenTheReplayEnds()
        {
            List<Rec> r = Ticks(Replay(session, Case("FormatHoldFor")));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, r.Select(x => x.Tick), "For(3) runs ticks 1-3 and the route then ends.");
            Assert.IsTrue(r.All(x => x.Move == 1 && !x.JumpPressed));
        }

        [Test]
        public void Jump_IsAOneTickPress_AndKeepsTheHeldDirection()
        {
            List<Rec> r = Ticks(Replay(session, Case("FormatJumpEdge")));
            CollectionAssert.AreEqual(new[] { 3 }, r.Where(x => x.JumpPressed).Select(x => x.Tick), "Jump() presses on the tick after For(2), once.");
            Assert.IsTrue(r.All(x => x.Move == 1), "Jump() doesn't change the held direction.");
        }

        [Test]
        public void Until_ReadsThePreviousTicksRecord_BeforeThisTicksMotorStep()
        {
            object replay = Replay(session, Case("FormatUntil", 2.5f));
            List<Rec> r = Records(replay);
            int reached = r.First(x => x.Tick > 0 && x.X >= 2.5f).Tick;
            int released = r.First(x => x.Tick > 0 && x.Move == 0).Tick;
            Assert.AreEqual(reached + 1, released, "the tick after the first record at x >= 2.5 is the first released tick\n" + Dump(replay));
        }

        [Test]
        public void TimedShift_DelaysByHoldingThePreviousCommand_OrStartsEarlier()
        {
            object c = Case("FormatTimed");
            Assert.AreEqual(6, JumpTick(Replay(session, c)), "authored: Jump() on tick 6");
            List<Rec> late = Ticks(Replay(session, c, Options(2, "Shift", 2, 6)));
            Assert.AreEqual(8, late.First(x => x.JumpPressed).Tick, "d +2: jump on tick 8");
            Assert.IsTrue(late.Where(x => x.Tick <= 8).All(x => x.Move == 1), "d +2 holds the previous command (right) meanwhile");
            Assert.AreEqual(4, JumpTick(Replay(session, c, Options(2, "Shift", -2, 6))), "d -2: jump on tick 4");
        }

        [Test]
        public void TimedHesitate_InsertsIdleTicks_ThenRestoresTheHeldDirection()
        {
            List<Rec> r = Ticks(Replay(session, Case("FormatTimed"), Options(2, "Hesitate", 3, 6)));
            CollectionAssert.AreEqual(new[] { 6, 7, 8 }, r.Where(x => x.Move == 0).Select(x => x.Tick), "Hesitate +3: idle on ticks 6-8");
            Rec jump = r.First(x => x.JumpPressed);
            Assert.AreEqual(9, jump.Tick);
            Assert.AreEqual(1, jump.Move, "the held direction resumes with the jump");
        }

        [Test]
        public void Margin_IsTheGapBetweenTwoFirstTicks_AndFailsIfAnEventNeverHappens()
        {
            object c = Case("FormatMargin");
            object replay = Replay(session, c);
            object m = ((IList)Call(T("RouteValidator"), "Margins", replay, F(c, "Route")))[0];
            List<Rec> r = Records(replay);
            int from = r.First(x => x.Tick > 0 && x.Grounded).Tick, to = r.First(x => x.Tick > 0 && x.X >= 2.6f).Tick;
            Assert.AreEqual(from, (int)F(m, "From"));
            Assert.AreEqual(to, (int)F(m, "To"));
            Assert.AreEqual(to - from, (int)F(m, "Value"));
            Assert.IsTrue((bool)F(m, "Passed"));

            object never = ((IList)Call(T("RouteValidator"), "Margins", Replay(session, Case("FormatHoldFor")), F(c, "Route")))[0];
            Assert.AreEqual(-1, (int)F(never, "To"), "x 2.6 is never reached in 3 ticks");
            Assert.IsFalse((bool)F(never, "Passed"), "R7: a margin fails if either event never happens");
        }

        [Test]
        public void PrefixOf_CopiesTheStepsThroughTheLabel_StripsTiming_AndRejectsAnUnknownLabel()
        {
            object source = F(Case("FormatTimed"), "Route");
            object prefix = Call(T("RouteFixtures"), "FormatPrefix", source, "Jump()");
            var steps = ((IEnumerable)F(prefix, "Steps")).Cast<object>().ToList();
            CollectionAssert.AreEqual(new[] { "Hold(Right)", "For(5)", "Jump()", "Release()", "For(2)" }, steps.Select(s => (string)F(s, "Label")));
            Assert.AreEqual("None", F(steps[2], "Timing").ToString(), "a betrayal prefix is never swept");
            Assert.Throws<ArgumentException>(() => Call(T("RouteFixtures"), "FormatPrefix", source, "Until(nothing)"));
        }

        // §11 R17 (D-049): a route's Move is screen-relative, projected like KeyboardCatInput's.
        [Test]
        public void HoldRight_WithGravityUp_MovesTheCatScreenRight()
        {
            object replay = Replay(session, Case("FormatScreenRight"));
            List<Rec> r = Records(replay);
            Rec start = r.First(x => x.Move == 1);
            Rec end = r.Last();
            Assert.IsTrue(start.GravityUp && end.GravityUp, "the cat is on the ceiling with gravity up\n" + Dump(replay));
            Assert.Greater(end.X - start.X, 1.5f, "Hold(Right) on the ceiling moves screen-right\n" + Dump(replay));
        }

        static int JumpTick(object replay) => Ticks(replay).First(x => x.JumpPressed).Tick;
    }
}
