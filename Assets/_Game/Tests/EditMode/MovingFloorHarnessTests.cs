using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-093 (D-095): the floors that move, through the route harness (the real game code, with physics). Rooms and routes are
    // MovingFloorFixtures' harness cases; in each, the cat spawns standing on the pattern. Cat X/Y are its collider centre.
    public sealed class MovingFloorHarnessTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.MovingFloorFixtures, Parallax.Editor");
        static readonly Type Motion = Type.GetType("Parallax.Gameplay.Rooms.SurfaceMotion, Parallax.Gameplay");
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Case(string fixture, params object[] args) { Assert.NotNull(Fixtures, "MovingFloorFixtures not found"); return Call(Fixtures, fixture, args); }
        static object M(string name) => Enum.Parse(Motion, name);
        static object From(string name) => Enum.Parse(Type.GetType("Parallax.Core.ShrinkFrom, Parallax.Core"), name);
        static float HalfHeight => PrecisionTestApi.Motor().ColliderSize.y * .5f;

        // The element's drawn bounds (room-local) at record index i.
        static Rect Drawn(object replay, string element, int i)
        {
            int e = ElementIndex(replay, element);
            Assert.GreaterOrEqual(e, 0, element + " is not a recorded element");
            return ((Rect[])F(((IList)F(replay, "Records"))[i], "RenderBounds"))[e];
        }

        static int FirstMove(object replay, string element, List<Rec> records)
        {
            Rect home = Drawn(replay, element, 0);
            for (int i = 1; i < records.Count; i++) if (Drawn(replay, element, i) != home) return i;
            return -1;
        }

        // ---------- the carry (Q1, Q2) ----------

        [Test]
        public void ACarryMover_CarriesAStandingCat_AllTheWay()
        {
            object replay = Replay(session, Case("Ride", M("Carry")));
            List<Rec> records = Records(replay);
            int moved = FirstMove(replay, "Mover", records);
            Assert.Greater(moved, 0, "the mover never moved\n" + Dump(replay));
            Rec last = records[records.Count - 1];
            TestContext.Out.WriteLine($"moved t{records[moved].Tick}; the cat ends at x {last.X:F3}, grounded on {last.Ground}");
            Assert.IsFalse(records.Any(r => r.Dead), "a carried cat never falls\n" + Dump(replay));
            foreach (Rec r in records.Skip(moved)) Assert.IsTrue(r.Grounded && r.Ground == "Mover", $"t{r.Tick}: on the mover\n" + Dump(replay));
            Assert.AreEqual(8f + 14f, last.X, .02f, "carried the mover's whole 14 u");
        }

        // Legacy (every element before PAX-093) is not carried: the mover leaves and the cat falls, as before.
        [Test]
        public void ALegacyMover_DoesNotCarry_TheCatFalls()
        {
            object replay = Replay(session, Case("Ride", M("Legacy")));
            List<Rec> records = Records(replay);
            Assert.IsTrue(records.Any(r => r.Dead), "a Legacy mover leaves the cat behind\n" + Dump(replay));
            Assert.AreEqual(8f, records.First(r => !r.Grounded).X, .05f, "not carried");
        }

        [Test]
        public void ASlipSlideAway_SlidesOutFromUnderTheCat_ItFallsIntoThePit()
        {
            object replay = Replay(session, Case("SlideAway", M("Slip")));
            List<Rec> records = Records(replay);
            Assert.IsTrue(records.Any(r => r.Dead), "the cat drops\n" + Dump(replay));
            Assert.AreEqual(8f, records.First(r => !r.Grounded).X, .05f, "not carried");
        }

        [Test]
        public void ACarrySlideAway_CarriesTheCatWithIt()
        {
            object replay = Replay(session, Case("SlideAway", M("Carry")));
            List<Rec> records = Records(replay);
            Assert.IsFalse(records.Any(r => r.Dead), Dump(replay));
            Assert.AreEqual(8f + 6f, records[records.Count - 1].X, .02f, "carried the slider's 6 u");
        }

        // Q2: a lift moving away from the ground keeps a Carry cat grounded every tick; a Legacy cat falls behind it.
        [Test]
        public void ACarryLiftGoingDown_KeepsTheCatGrounded_ALegacyOneDoesNot()
        {
            object carry = Replay(session, Case("RideDown", M("Carry"))), legacy = Replay(session, Case("RideDown", M("Legacy")));
            List<Rec> c = Records(carry), l = Records(legacy);
            int moved = FirstMove(carry, "Lift", c);
            Assert.Greater(moved, 0, Dump(carry));
            foreach (Rec r in c.Skip(moved).Take(30)) Assert.IsTrue(r.Grounded && r.Ground == "Lift", $"t{r.Tick}: grounded on the lift\n" + Dump(carry));
            Assert.IsTrue(l.Skip(moved).Take(30).Any(r => !r.Grounded), "a Legacy cat falls behind a lift going down faster than it falls\n" + Dump(legacy));
        }

        // Q2: the push into the cat stays physics', the same with or without Carry. D-115 (supersedes D-056 (3)'s launch, the
        // developer: "when one is on top of it and not moving, it auto jumps"): the cat stops with the lift.
        [Test]
        public void AnUpwardLift_StopsTheCatWithIt_TheSameWhateverItsMotion()
        {
            List<Rec> carry = Records(Replay(session, Case("RideUp", M("Carry")))), legacy = Records(Replay(session, Case("RideUp", M("Legacy"))));
            Assert.AreEqual(legacy.Count, carry.Count);
            for (int i = 0; i < carry.Count; i++) Assert.AreEqual(legacy[i].Y, carry[i].Y, 1e-5f, $"t{carry[i].Tick}");
            float stopTop = .5f + 3f, apex = carry.Max(r => r.Y) - HalfHeight;
            TestContext.Out.WriteLine($"apex {apex - stopTop:F3} above the stopped lift");
            Assert.Less(apex, stopTop + .05f, "no launch on the stop (D-115)");
        }

        // Q3: jumping off a moving Carry floor adds its sideways speed (5 u/s) once, on the jump tick; off Legacy, nothing.
        [Test]
        public void JumpingOffACarryMover_AddsItsSidewaysSpeedOnce()
        {
            List<Rec> carry = Records(Replay(session, Case("JumpOffMover", M("Carry")))), legacy = Records(Replay(session, Case("JumpOffMover", M("Legacy"))));
            int jump = carry.FindIndex(r => r.JumpPressed);
            Assert.Greater(jump, 0);
            TestContext.Out.WriteLine($"jump t{carry[jump].Tick}: vx {carry[jump].Vx:F3} (Carry), {legacy[legacy.FindIndex(r => r.JumpPressed)].Vx:F3} (Legacy)");
            Assert.AreEqual(5f, carry[jump].Vx, .05f, "the mover's speed, added on the jump tick");
            Assert.Less(carry[jump + 1].Vx, carry[jump].Vx + 1e-4f, "added once: it only decays from there with no input");
            Assert.AreEqual(0f, legacy[legacy.FindIndex(r => r.JumpPressed)].Vx, .01f);
        }

        // ---------- drop-and-return (Q4) ----------

        [Test]
        public void ACarryDropAndReturnFloor_RidesDownAndBack_WithAVisibleReturn()
        {
            object replay = Replay(session, Case("DropAndReturn", M("Carry")));
            List<Rec> records = Records(replay);
            int moved = FirstMove(replay, "Drop", records);
            Assert.Greater(moved, 0, Dump(replay));
            Assert.IsFalse(records.Any(r => r.Dead), Dump(replay));
            // Down 2 over 10 ticks, hold 30, back up over 40: halfway up 30 ticks into... 10 + 30 + 20 after its first move.
            float halfway = Drawn(replay, "Drop", moved - 1 + 10 + 30 + 20).yMax;
            TestContext.Out.WriteLine($"moved t{records[moved].Tick}; top halfway back {halfway:F3}");
            Assert.AreEqual(-1f, halfway, .06f, "a visible return, not a snap");
            Assert.AreEqual(0f, Drawn(replay, "Drop", moved - 1 + 10 + 30 + 40).yMax, 1e-3f, "home again when the return ends");
            Assert.IsTrue(records[moved + 5].Grounded && records[moved + 5].Ground == "Drop", "rode it down");
        }

        // ---------- the shrinker (Q5) ----------

        [Test]
        public void AShrinker_ShrinksUnderTheCat_ItFallsOffTheEdgeThatMovesIn()
        {
            object replay = Replay(session, Case("StandOnShrinker", From("Right")));
            List<Rec> records = Records(replay);
            int moved = FirstMove(replay, "Shrink", records);
            Assert.Greater(moved, 0, "the shrinker never shrank\n" + Dump(replay));
            Rect half = Drawn(replay, "Shrink", moved - 1 + 20);
            TestContext.Out.WriteLine($"shrinking from t{records[moved].Tick}; halfway: x [{half.xMin:F3}, {half.xMax:F3}]");
            Assert.AreEqual(2f, half.width, .06f, "half its width halfway");
            Assert.AreEqual(6f, half.xMin, 1e-3f, "the left edge stays");
            Assert.IsTrue(records.Any(r => r.Dead), "the cat near the right edge falls\n" + Dump(replay));
        }

        // ---------- the push wall (Q6) ----------

        [Test]
        public void APushWall_KeepsThePushedCatFlushAgainstItsLeadingEdge()
        {
            object replay = Replay(session, Case("Pushed", false));
            List<Rec> records = Records(replay);
            float half = PrecisionTestApi.Motor().ColliderSize.x * .5f;
            int contact = records.FindIndex(r => r.X > 6f + 1e-3f);
            Assert.Greater(contact, 0, "never pushed\n" + Dump(replay));
            // Exactly flush every tick the wall moves; once it holds, physics' contact skin may part them by a few thousandths.
            for (int i = contact; i < records.Count; i++)
            {
                bool moving = Drawn(replay, "Pusher", i) != Drawn(replay, "Pusher", i - 1);
                Assert.AreEqual(Drawn(replay, "Pusher", i).xMax + half, records[i].X, moving ? 2e-3f : 1e-2f, $"t{records[i].Tick}: flush\n" + Dump(replay));
            }
            Assert.IsFalse(records.Any(r => r.Dead), "open space: pushed, not crushed");
        }

        [Test]
        public void APushWall_CrushesTheCatAgainstItsNamedPartner()
        {
            object replay = Replay(session, Case("Pushed", true));
            object kill = F(replay, "Kill");
            Assert.NotNull(kill, "no kill\n" + Dump(replay));
            var candidates = (List<string>)F(kill, "Candidates");
            TestContext.Out.WriteLine($"killed t{F(kill, "Tick")}: {string.Join(", ", candidates)}");
            CollectionAssert.AreEqual(new[] { "Pusher" }, candidates, Dump(replay));
        }
    }
}
