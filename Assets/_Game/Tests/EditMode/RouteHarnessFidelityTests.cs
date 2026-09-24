using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-075 (D-079) §5.1, ranges from §11 R11: before any route result is trusted, the harness must
    // reproduce the motor's known numbers and PAX-078's stepped figures. A value outside its range is a
    // stop (R11), so every failure message carries the per-tick record.
    public sealed class RouteHarnessFidelityTests
    {
        // Full-speed take-off for the L004 fast flip (hop SourceSpikes into Flip_A); inside the window.
        const float L004TakeoffX = 22.5f;

        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static Rec At(List<Rec> r, int tick) => r.First(x => x.Tick == tick);

        [Test]
        public void Simulate_AdvancesThePhysicsWorldInEditMode()
        {
            object replay = Replay(session, Case("FlatApex"));
            List<Rec> r = Records(replay);
            Assert.Greater(r.Count, 10, "the harness stepped no ticks.\n" + Dump(replay));
            Assert.Greater(r.Max(x => x.Y) - r[0].Y, 1f, "Physics2D.Simulate did not move the jumping cat (R1 stop).\n" + Dump(replay));
        }

        [Test]
        public void FlatRun_RunPerTickRestToMaxAndStop_MatchTheMotor()
        {
            object replay = Replay(session, Case("FlatRun"));
            List<Rec> r = Records(replay);
            string dump = "\n" + Dump(replay);
            Assert.Less(Math.Abs(At(r, 4).Vx), 5.999f, "rest -> max must take 5 ticks, not 4" + dump);
            Assert.AreEqual(6f, At(r, 5).Vx, 1e-3f, "rest -> max: 5 ticks" + dump);
            Assert.AreEqual(.36f, At(r, 5).X - r[0].X, .005f, "rest -> max covers 0.36 u" + dump);
            Assert.AreEqual(.12f, At(r, 20).X - At(r, 19).X, 1e-4f, "run 0.12 u/tick" + dump);
            int stopped = r.First(x => x.Tick >= 31 && Math.Abs(x.Vx) < 1e-3f).Tick;
            Assert.AreEqual(34, stopped, "release on tick 31: stop takes 4 ticks" + dump);
            Assert.AreEqual(.168f, At(r, 34).X - At(r, 30).X, .005f, "stop covers 0.168 u" + dump);
        }

        [Test]
        public void Apex_IsTwentyFourRisingTicksAnd3339()
        {
            object replay = Replay(session, Case("FlatApex"));
            List<Rec> r = Records(replay);
            int rising = r.Where(x => x.Tick >= 6).TakeWhile(x => x.Vy > 0f).Count();
            Assert.AreEqual(24, rising, "rising ticks from the jump on tick 6\n" + Dump(replay));
            Assert.AreEqual(3.339f, r.Max(x => x.Y) - At(r, 5).Y, .005f, "discrete apex\n" + Dump(replay));
        }

        [Test]
        public void FlatJump_IsFortyEightTicksAnd576()
        {
            object replay = Replay(session, Case("FlatJump"));
            List<Rec> r = Records(replay);
            Rec landing = r.First(x => x.Tick > 21 && x.Grounded);
            Assert.AreEqual(48, landing.Tick - 21 + 1, "air ticks from the jump on tick 21 to the first grounded tick\n" + Dump(replay));
            Assert.AreEqual(5.76f, landing.X - At(r, 20).X, .01f, "full-speed flat jump distance\n" + Dump(replay));
        }

        [Test]
        public void Coyote_AllowsAJumpOnTheFirstFiveAirborneSteps()
        {
            var jumped = new List<int>();
            for (int n = 1; n <= 7; n++)
                if (Records(Replay(session, Case("Coyote", n))).Any(x => x.Vy > 13f)) jumped.Add(n);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, jumped, "airborne steps on which a press still jumps (D-077: 5).");
        }

        [Test]
        public void Buffer_HonoursAPressOnItsOwnStepAndTheNextFive()
        {
            List<Rec> base_ = Records(Replay(session, Case("Buffer", 0)));
            int airborne = base_.First(x => x.Tick > 4 && !x.Grounded).Tick;
            int groundedStep = base_.First(x => x.Tick > airborne && x.Grounded).Tick + 1; // the motor sees it on the next step
            var jumped = new List<int>();
            for (int m = 0; m <= 7; m++)
            {
                int press = groundedStep - m;
                if (Records(Replay(session, Case("Buffer", press - 4))).Any(x => x.Tick >= press && x.Vy > 13f)) jumped.Add(m);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, jumped, $"ticks before the landing step ({groundedStep}) at which a press still jumps (D-077: its own step and the next 5).");
        }

        [Test]
        public void L002_LiftMargin_FireTickMinusLandingTick_Is14To16()
        {
            object replay = Replay(session, Case("L002LiftApproach", 19.8f));
            int margin = LiftMargin(replay);
            Assert.That(margin, Is.InRange(14, 16), "L002 Lift margin (PAX-078: 14-15 stepped)\n" + Dump(replay));
        }

        internal static int LiftMargin(object replay)
        {
            List<Rec> r = Records(replay);
            int lift = ElementIndex(replay, "Lift");
            Assert.GreaterOrEqual(lift, 0, "no Lift element");
            Rec landing = r.FirstOrDefault(x => x.Tick > 0 && x.Grounded && x.Ground == "Lift");
            Rec fire = r.FirstOrDefault(x => x.Tick > 0 && x.FireTick[lift] >= 0);
            Assert.Greater(landing.Tick, 0, "the cat never landed on the Lift\n" + Dump(replay));
            Assert.Greater(fire.Tick, 0, "the Lift never fired\n" + Dump(replay));
            return fire.Tick - landing.Tick;
        }

        // §11 R19 (c): the harness measured the right-holding fast-flip window at 19 (d -15..+3; PAX-078's box
        // model said 14.83-15.83), and no full-speed take-off is killed by Block_1 (the box model's early-flip
        // band dies of CeilingHiddenSpikes or survives). Pinned; Block_1 moves to PAX-080.
        [Test]
        public void L004_FastFlipWindow_IsPinnedAt19_AndBlock1KillsNoFullSpeedTakeoff()
        {
            object c = Case("L004FastFlip", L004TakeoffX);
            object authored = Replay(session, c);
            Assert.IsTrue((bool)F(authored, "Completed"), "the authored take-off must survive\n" + Dump(authored));
            object[] args = { session, F(c, "Room"), F(c, "Route"), authored, 0, null };
            object w = ((System.Collections.IList)Call(T("RouteValidator"), "Sweep", args))[0];
            Assert.AreEqual(19, (int)F(w, "Count"), "L004 fast-flip window: " + w);
            Assert.AreEqual(-15, (int)F(w, "Low"), "early edge: " + w);
            Assert.AreEqual(3, (int)F(w, "High"), "late edge: " + w);

            int t0 = (int)F(w, "AuthoredTick");
            var block1 = new List<string>();
            for (int d = -25; d <= 5; d++)
            {
                object replay = Replay(session, c, Options(2, "Shift", d, t0, resolveCause: false));
                object kill = F(replay, "Kill");
                if (kill != null && ((List<string>)F(kill, "Candidates")).Contains("Block_1")) block1.Add($"d {d}: killed t{F(kill, "Tick")}");
            }
            Assert.IsEmpty(block1, "Block_1 killed a full-speed take-off (R19 (c) pins that it doesn't)");
        }
    }
}
