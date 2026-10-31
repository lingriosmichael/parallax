using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L035Layout;

namespace Parallax.Editor.Levels
{
    // D-266: level 35's routes ("Scanline"). The scanners are periodic from the room's start, so each wait is on the room tick:
    // Passed(...) is true once this sweep's beam is west of a slot (or resting at home after it), and DropFor(...) when a cat
    // setting off now lands in a slot a little before the next sweep reaches it. Betrayals branch off the solution's own steps.
    static class L035Routes
    {
        const float Run = .12f;

        // A scanline's schedule.
        readonly struct S
        {
            public readonly float Home; public readonly int Move, Period, Phase;
            public S(float home, int move, int period, int phase) { Home = home; Move = move; Period = period; Phase = phase; }
            int Since(int t) => t < Phase ? -1 : (t - Phase) % Period;
            // The beam is sweeping at tick t, and where.
            public bool Moving(int t, out float x)
            {
                int s = Since(t); x = Home;
                if (s < L.Warn || s > L.Warn + Move + L.ScanHold) return false;
                x = Home - L.ScanSpeed * Mathf.Min(s - L.Warn, Move);
                return true;
            }
            // Resting at home after this cycle's sweep.
            public bool Resting(int t) { int s = Since(t); return s > L.Warn + Move + L.ScanHold; }
            // The next tick, from t on, the beam reaches x (its front at x + cat's half width).
            public int Reaches(int t, float x)
            {
                for (int u = t; u < t + 2 * Period; u++) if (Moving(u, out float b) && b <= x) return u;
                return int.MaxValue;
            }
        }
        static readonly S A = new(L.AHome, L.AMove, L.APeriod, L.APhase), D = new(L.DHome, L.DMove, L.DPeriod, L.DPhase), Ee = new(L.DHome, L.DMove, L.DPeriod, L.DPhase + L.EBehind);

        // The sweep is going on west of the slot at x0: it has just gone over it.
        static RouteCondition Passed(string label, S s, float x0) => new($"{label} past {x0}", v =>
            s.Moving(v.Last.RoomLifeTick, out float b) && b < x0 - 1f);
        // A cat setting off now drops into the slot at x0 between `early` and `late` ticks before the next sweep reaches it.
        static RouteCondition DropFor(string label, S s, float x0, int early, int late) => new($"drop {label} {x0}", v =>
        {
            int t = v.Last.RoomLifeTick, land = t + Mathf.CeilToInt((x0 + .5f - v.Last.X) / Run) + 3 + 8;
            int pass = s.Reaches(t, x0 + L.SlotW + .5f);
            return pass - land <= early && pass - land >= late;
        });

        // Out of a slot (east), on to the slot at `next` and drop in (over Fake_3 first if `overFake`).
        static RouteStep[] Out(RouteCondition when, float next, bool overFake = false)
        {
            var s = new List<RouteStep> { Until(when), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()) };
            if (overFake) s.AddRange(new[] { Until(XAtLeast(L.Fake3X0 - .55f)), Jump(), Until(Airborne()), Until(Grounded()) });
            s.AddRange(new[] { Until(XAtLeast(next + .5f)), Release(), Until(Grounded()), Until(Still()) });
            return s.ToArray();
        }

        // ---- Hall 1 ----
        static RouteStep[] Hall1()
        {
            float[] s1 = L.Slots1;
            var s = new List<RouteStep> { Hold(Right), Until(XAtLeast(s1[0] + .5f)), Release(), Until(Grounded()), Until(Still()) };
            for (int k = 1; k < s1.Length; k++) s.AddRange(Out(Passed("Scan_A", A, s1[k - 1]), s1[k], overFake: k == 2));
            s.AddRange(new[] { Until(Passed("Scan_A", A, s1[s1.Length - 1])), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(L.Checkpoint1X - .3f)) });
            return s.ToArray();
        }

        // ---- Hall 2 ----
        static RouteStep[] Hall2()
        {
            float[] s2 = L.Slots2;
            var s = new List<RouteStep> { Release(), Until(Still()),
                Until(DropFor("Scan_D", D, s2[0], 16, 6)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(s2[0] + .5f)), Release(), Until(Grounded()), Until(Still()) };
            for (int k = 1; k < s2.Length; k++) s.AddRange(Out(Passed("Scan_E", Ee, s2[k - 1]), s2[k]));
            s.AddRange(new[] { Until(Passed("Scan_E", Ee, s2[s2.Length - 1])), Hold(Right), Jump(), Until(Airborne()), Until(RoomComplete()) });
            return s.ToArray();
        }

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L035Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Hall1(), Hall2() };
            var solution = new Route("L035 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int H1 = 0, H2 = 1;
            RouteStep[] h1 = parts[H1], h2 = parts[H2];
            return new RoomRoutes(solution,
                // ---- Hall 1 ----
                new Betrayal("Scan_A [NW]: a cat that waits in Slot_1 through a second sweep is pushed up into it", "Scan_A", DeathCause.Hazard,
                    B("wait", H1, After(h1, "Until(Still)"), Until(Dead())), revealedBy: "Slot_1"),
                new Betrayal("Slot_1 [W]: a cat that climbs out before Scan_A has gone over is caught", "Scan_A", DeathCause.Hazard,
                    B("climb out", H1, After(h1, "Until(Still)"), Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("Shallow_3 [J]: a cat that ducks into the shallow slot is still in the beam's way", "Scan_A", DeathCause.Hazard,
                    B("duck in it", H1, After(h1, "Until(Grounded)", 4), Until(XAtLeast(L.Fake3X0 + .5f)), Release(), Until(Dead()))),
                // ---- Hall 2 ----
                new Betrayal("Scan_E [W]: a cat that climbs out after the first beam is caught by the second", "Scan_E", DeathCause.Hazard,
                    B("climb out", H2, After(h2, "Until(Still)", 2), Until(Passed("Scan_D", D, L.Slots2[0])), Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("Fill_4 [NW]: a cat that stays in the last slot is pushed up into the next scan", "Scan_D", DeathCause.Hazard,
                    B("stay", H2, After(h2, "Until(Still)", 5), Until(Dead())), revealedBy: "Fill_4"));
        }
    }
}
