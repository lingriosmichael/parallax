using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-105 (D-110 and its amendments): the wall cling through the route harness (the real game code, 50 Hz ticks, physics
    // simulated), in WallClingFixtures' rooms: a floor top at 0, a grip wall's face at x 14 on the cat's right. Positions are the cat
    // collider's centre; it is 1 wide and 0.56 tall (half 0.5 x 0.28). The measured numbers are printed for D-110's note.
    public sealed class WallClingHarnessTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.WallClingFixtures, Parallax.Editor");
        const float HalfWidth = .5f, HalfHeight = .28f;
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static object Case(string fixture, params object[] args) { Assert.NotNull(Fixtures, "WallClingFixtures not found"); return Call(Fixtures, fixture, args); }

        sealed class W { public int Tick; public float X, Y, Vx, Vy; public bool Grounded, Clinging, Latch, Grab, Dead; public int Side; }

        static List<W> Records(object replay)
        {
            var list = new List<W>();
            foreach (object r in (IEnumerable)F(replay, "Records"))
                list.Add(new W
                {
                    Tick = (int)F(r, "Tick"), X = (float)F(r, "X"), Y = (float)F(r, "Y"), Vx = (float)F(r, "Vx"), Vy = (float)F(r, "Vy"),
                    Grounded = (bool)F(r, "Grounded"), Clinging = (bool)F(r, "IsClinging"), Latch = (bool)F(r, "LatchMode"),
                    Grab = (bool)F(r, "GrabPressed"), Side = (int)F(r, "ClingSide"), Dead = (bool)F(r, "Dead"),
                });
            return list;
        }

        object Run(string fixture, params object[] args) => Replay(session, Case(fixture, args));

        [Test]
        public void JumpAtAWall_Grab_LatchesAtTheTopOfTheRise_SlidesAtTwoUnitsPerSecond_AndLands()
        {
            object replay = Run("GrabSlideLand", true);
            List<W> r = Records(replay);
            int first = r.FindIndex(x => x.Clinging);
            Assert.Greater(first, 0, "never clung\n" + Dump(replay));
            Assert.LessOrEqual(r[first - 1].Vy, 0f, "latched while rising");
            Assert.AreEqual(1, r[first].Side);
            Assert.AreEqual(14f, r[first].X + HalfWidth, .02f, "the collider's right edge on the face");
            List<W> clinging = r.Where(x => x.Clinging).ToList();
            Assert.IsTrue(clinging.All(x => Mathf.Abs(x.Vx) < 1e-4f && x.Vy >= -2f - 1e-4f), "pressed to the wall, never faster than the slide");
            Assert.IsTrue(clinging.Any(x => Mathf.Abs(x.Vy + 2f) < 1e-4f), "reaches the slide speed");
            W last = r[r.Count - 1];
            Assert.IsTrue(last.Grounded && !last.Clinging && !last.Latch, "lands at the wall's foot, out of latch mode\n" + Dump(replay));
            TestContext.Out.WriteLine($"latched t{r[first].Tick} at y {r[first].Y:F3}; clung {clinging.Count} ticks; landed t{r.First(x => x.Grounded && x.Tick > r[first].Tick).Tick}");
        }

        [Test]
        public void WithoutGrab_TheSameJump_NeverClings()
        {
            List<W> r = Records(Run("GrabSlideLand", false));
            Assert.IsFalse(r.Any(x => x.Clinging || x.Latch));
        }

        [Test]
        public void OneWall_CantBeClimbed_TheFaceJustLeftIsLockedUntilLanding()
        {
            object replay = Run("SingleWall");
            List<W> r = Records(replay);
            int jumped = r.FindIndex(x => x.Vx < -5f);
            Assert.Greater(jumped, 0, "no wall jump\n" + Dump(replay));
            float latchY = r.Last(x => x.Tick < r[jumped].Tick && x.Clinging).Y;
            Assert.IsFalse(r.Skip(jumped).Any(x => x.Clinging), "latched the same wall again before landing\n" + Dump(replay));
            Assert.IsTrue(r[r.Count - 1].Grounded);
            float top = r.Skip(jumped).Max(x => x.Y);
            Assert.AreEqual(latchY + 1.7f, top, .01f, "one wall jump: a jump's height above the cling, no more");
            TestContext.Out.WriteLine($"latched at y {latchY:F3}; wall-jump apex {top:F3} (+{top - latchY:F3})");
        }

        [Test]
        public void PushingAway_LetsGo_AndEndsLatchMode()
        {
            List<W> r = Records(Run("PushAway"));
            int clung = r.FindIndex(x => x.Clinging);
            int let = r.FindIndex(clung, x => !x.Clinging);
            Assert.Greater(let, clung);
            Assert.IsFalse(r[let].Latch);
            Assert.IsTrue(r[r.Count - 1].Grounded);
        }

        [Test]
        public void AJumpPressedBeforeTheGrab_WallJumpsOnTheLatchStep()
        {
            object replay = Run("BufferedWallJump");
            List<W> r = Records(replay);
            Assert.IsFalse(r.Any(x => x.Clinging), "the latch and the wall jump are one step\n" + Dump(replay));
            Assert.IsTrue(r.Any(x => x.Vx < -5.9f && x.Vy > 9f), "the wall jump's launch\n" + Dump(replay));
        }

        [Test]
        public void AHazardWall_IsntGrabbed_ItKills()
        {
            object replay = Run("HazardWall");
            List<W> r = Records(replay);
            Assert.IsFalse(r.Any(x => x.Clinging));
            Assert.IsTrue(r.Any(x => x.Dead), "the spikes kill\n" + Dump(replay));
        }

        [Test]
        public void AHalfUnitFace_IsntGrabbed_AOneUnitFaceIs()
        {
            Assert.IsFalse(Records(Run("HangingBlock", .5f)).Any(x => x.Clinging), "0.5 u");
            Assert.IsTrue(Records(Run("HangingBlock", 1f)).Any(x => x.Clinging), "1.0 u");
        }

        // D-110 amendment 2: the same jump and Grab at a plain Wall where the grip wall was: no cling, the cat drops and lands.
        [Test]
        public void APlainWall_IsntGrabbed()
        {
            object replay = Run("PlainWall");
            List<W> r = Records(replay);
            Assert.IsTrue(r.Any(x => x.Grab), "Grab was pressed");
            Assert.IsFalse(r.Any(x => x.Clinging || x.Latch), "a plain wall never latches\n" + Dump(replay));
            Assert.IsTrue(r[r.Count - 1].Grounded);
        }

        // One wall, kicked off with the stick held back: the cat lands on its top when it's 0.2 u inside the reach model's
        // ReturnOntoOwnTop above the grab (WallJumpReach), and not past it: the model errs toward reach, by less than 0.2 u.
        [Test]
        public void KickingOffOneWall_WithTheStickHeldBack_LandsOnItsTop_WithinTheModelsReach()
        {
            object arc = Activator.CreateInstance(Type.GetType("Parallax.Editor.Setup.WallJumpReach, Parallax.Editor"), PrecisionTestApi.Motor(), PrecisionTestApi.Gravity(), 40f);
            float back = (float)arc.GetType().GetProperty("ReturnOntoOwnTop").GetValue(arc);
            float grabBottom = Records(Run("GrabSlideLand", true)).First(x => x.Clinging).Y - HalfHeight;
            float reachable = grabBottom + back - .2f, out_ = grabBottom + back + .02f;
            object onTop = Run("KickBack", reachable), tooHigh = Run("KickBack", out_);
            string groundOnTop = (string)F(((IList)F(onTop, "Records"))[((IList)F(onTop, "Records")).Count - 1], "Ground");
            string groundTooHigh = (string)F(((IList)F(tooHigh, "Records"))[((IList)F(tooHigh, "Records")).Count - 1], "Ground");
            TestContext.Out.WriteLine($"grab at paws {grabBottom:F3}; model: a top up to {back:F3} above it; {reachable:F2} u wall: lands on {groundOnTop}; {out_:F2} u wall: lands on {groundTooHigh}");
            Assert.AreEqual("Wall", groundOnTop, Dump(onTop));
            Assert.AreEqual("Floor", groundTooHigh, Dump(tooHigh));
        }

        // Faces 2 u apart: each wall jump crosses while rising and latches the far wall at the top of its rise (latch mode, one
        // Grab press in all).
        [Test]
        public void ATwoWallShaft_IsClimbed_WallToWall_WithOneGrab()
        {
            object replay = Run("Shaft", 2f, 4);
            List<W> r = Records(replay);
            Assert.IsTrue((bool)F(replay, "Completed"), "didn't reach the plateau\n" + Dump(replay));
            Assert.AreEqual(1, r.Count(x => x.Grab), "one Grab press");
            var latches = new List<W>();
            for (int i = 1; i < r.Count; i++) if (r[i].Clinging && !r[i - 1].Clinging) latches.Add(r[i]);
            Assert.AreEqual(5, latches.Count, "the first grab, then one latch per hop");
            float[] gains = latches.Zip(latches.Skip(1), (a, b) => b.Y - a.Y).ToArray();
            TestContext.Out.WriteLine("latch heights: " + string.Join(", ", latches.Select(x => $"{(x.Side < 0 ? "L" : "R")} {x.Y:F3}")) + "; per hop: " + string.Join(", ", gains.Select(g => g.ToString("F3"))));
            Assert.IsTrue(gains.All(g => g > 1.4f), "each hop gains about a jump's height");
        }

        // The wall jump's arc over open floor with the stick held away: the apex, and how far the cat travels before it falls
        // below the launch height (where latch mode ends). A far face is reached while the cat's leading edge gets there.
        [Test]
        public void TheWallJumpsArc_Measured()
        {
            object replay = Run("KickAcross");
            List<W> r = Records(replay);
            int kick = r.FindIndex(x => x.Vx > 5f);
            Assert.Greater(kick, 0, "no wall jump\n" + Dump(replay));
            W launch = r[kick - 1];
            float apex = r.Skip(kick).Max(x => x.Y) - launch.Y;
            W below = r.Skip(kick).First(x => x.Y < launch.Y && x.Vy < 0f);
            int endsLatch = r.FindIndex(kick, x => !x.Latch);
            float crossing = below.X - launch.X;   // the leading edge's travel; a far face this far beyond the near one is met
            TestContext.Out.WriteLine($"launch y {launch.Y:F3}; apex +{apex:F3} at t+{r.Skip(kick).OrderByDescending(x => x.Y).First().Tick - launch.Tick}; " +
                $"below the launch height after {below.X - launch.X:F3} u (t+{below.Tick - launch.Tick}); latch mode ends t+{r[endsLatch].Tick - launch.Tick}");
            Assert.AreEqual(1.7f, apex, .01f, "a ground jump's height (1.700 at 50 Hz: RouteHarnessFidelityTests' apex)");
            // The motor step reads the position the previous tick left, so the record after the first one below the launch
            // height is the first out of latch mode.
            Assert.AreEqual(below.Tick + 1, r[endsLatch].Tick, "latch mode ends on the first falling step below the launch height");
            Assert.Greater(crossing, 3.5f);
        }
    }
}
