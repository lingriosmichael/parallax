using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L033Layout;

namespace Parallax.Editor.Levels
{
    // D-264: level 33's routes ("Wormhole"). Every exit is periodic from the room's start, so each go is on the room tick: Exit(...)
    // is true when a cat setting off now reaches the entry (lead ticks on, from where it stands) as the exit sits where it should,
    // with a few ticks to spare each side. Betrayals branch off the solution's own steps.
    static class L033Routes
    {
        const float Run = .12f;
        const int Spare = 4;

        // A glitching exit's schedule: `count` spots after home, `move` ticks to step through them, every `period` from `phase`.
        readonly struct P
        {
            public readonly int Count, Move, Hold, Period, Phase;
            public P(int count, int move, int hold, int period, int phase) { Count = count; Move = move; Hold = hold; Period = period; Phase = phase; }
            // The spot the exit is on at room tick t (0 home, Count the last).
            public int At(int t) => t < Phase ? 0 : TrapPath.GlitchIndex(Count, TrapPath.Progress((t - Phase) % Period, Move, Hold, 0));
        }
        static readonly P A = new(1, L.AMove, L.AHold, L.APeriod, L.APhase), B = new(1, L.BMove, L.BHold, L.BPeriod, L.BPhase), Cc = new(1, L.CMove, L.CHold, L.CPeriod, L.CPhase);
        static readonly P D = new(L.DSpots.Length, L.DStep * L.DSpots.Length, L.DHold, L.DPeriod, L.DPhase);

        // A run from here to an entry at entryX: the ticks until the cat's centre is in it (a few to get up to speed).
        static int Lead(float x, float entryX, int extra = 0) => Mathf.CeilToInt((entryX - L.PortalW * .5f - x) / Run) + 3 + extra;
        // The exit is on `spot` from Spare before the cat gets there to Spare after.
        static RouteCondition Exit(string label, P p, int spot, float entryX, int extra = 0) => new($"exit {label} {spot}", v =>
        {
            int at = v.Last.RoomLifeTick + 1 + Lead(v.Last.X, entryX, extra);
            for (int t = at - Spare; t <= at + Spare; t++) if (p.At(t) != spot) return false;
            return true;
        });
        // Ferry_X's centre at room tick t.
        static float Ferry(int t)
        {
            if (t < L.FerryPhase) return L.FerryHome;
            return L.FerryHome + L.FerryRun * TrapPath.Progress((t - L.FerryPhase) % L.FerryPeriod, L.FerryMove, L.FerryHold, L.FerryMove);
        }
        // Ferry_X is under Portal_X's exit as a cat setting off now lands from it (about DropTicks after the teleport).
        const int DropTicks = 15;
        static RouteCondition FerryUnder() => new("ferry under the exit", v =>
        {
            int at = v.Last.RoomLifeTick + 1 + Lead(v.Last.X, L.EntryXX, DropTicks);
            for (int t = at - Spare; t <= at + Spare; t++) if (Mathf.Abs(Ferry(t) - L.ExitXX) > .45f) return false;
            return true;
        });
        static RouteCondition Not(RouteCondition c) => new("not " + c.Label, v => !c.Test(v));
        // The ferry is at its far end (holding).
        static RouteCondition FerryAtEnd() => new("ferry at its end", v => Ferry(v.Last.RoomLifeTick) >= L.FerryHome + L.FerryRun - 1e-3f);

        // ---- Near: Cells A and B ----
        static RouteStep[] Near() => new[] {
            // Wait short of Portal_A for its exit to come home; in, and jump Pit_A at once (the cat comes out running).
            Hold(Right), Until(XAtLeast(L.EntryAX - 2.4f)), Release(), Until(Still()),
            Until(Exit("A", A, 0, L.EntryAX)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.ExitAX - .5f)), Until(XAtLeast(L.PitA0 - .55f)), Jump(), Until(Airborne()), Until(Grounded()),
            // Wait on the solid floor; run across the glass into Portal_B as its exit is home; jump Pit_B.
            Until(XAtLeast(L.GlassB0 - 1.1f)), Release(), Until(Still()),
            Until(Exit("B", B, 0, L.EntryBX)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.ExitBX - .5f)), Until(XAtLeast(L.PitB0 - .55f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(L.Checkpoint1X - .3f)),
        };

        // ---- Deep: Cell C, Cell X, the finale ----
        // Into Pit_C: from the edge's jump the cat is in Portal_C's entry about CFall ticks later.
        const int CFall = 38;
        static RouteStep[] Deep() => new[] {
            Until(XAtLeast(L.PitC0 - 1.6f)), Release(), Until(Still()),
            Until(Exit("C", Cc, 0, L.PitC0 - .1f + L.PortalW * .5f, CFall)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.PitC0 - .1f)), Jump(),
            Until(XAtLeast(L.WallC1)), Release(), Until(Grounded()), Until(Still()),
            Hold(Right), Until(XAtLeast(L.PitC20 - .55f)), Jump(), Until(Airborne()), Until(Grounded()),
            // Cell X: into Portal_X as Ferry_X comes under its exit; let go, drop onto it, ride it to its end, and off.
            Until(XAtLeast(L.EntryXX - 1.4f)), Release(), Until(Still()),
            Until(FerryUnder()), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Lake0)), Release(), Until(Grounded()), Until(Still()),
            Until(FerryAtEnd()), Hold(Right), Until(XAtLeast(L.FerryHome + L.FerryRun + .6f)), Jump(), Until(Airborne()), Until(Grounded()),
            // Cell Y: jump into Portal_Y, so the cat comes out rising, and carries over Pit_Y.
            Until(XAtLeast(L.EntryYX - 1.6f)), Jump().Timed(TimedMode.Shift), Until(XAtLeast(L.WallY0 + 1f)), Until(Grounded()),
            // The finale: wait out of Eye_D's sight; run into Portal_D as its exit reaches the ledge, out over the ledge into the door.
            Until(XAtLeast(L.Lake1 + .8f)), Release(), Until(Still()),
            Until(Exit("D", D, L.DSpots.Length, L.EntryDX)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L033Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Near(), Deep() };
            var solution = new Route("L033 solution", parts.SelectMany(p => p).ToArray());
            Route Bt(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int N = 0, Dp = 1;
            RouteStep[] near = parts[N], deep = parts[Dp];
            return new RoomRoutes(solution,
                // ---- Near ----
                new Betrayal("Portal_A [W]: a cat that runs straight into Portal_A comes out over Pit_A", "Pit_A_Laser", DeathCause.Hazard,
                    Bt("run in", N, 0, Hold(Right), Until(Dead())), revealedBy: "Portal_A"),
                new Betrayal("Glass_B [NW]: a cat that waits on the glass by Portal_B falls through it", "Pit_B0_Laser", DeathCause.Hazard,
                    Bt("wait on it", N, After(near, "Until(Still)", 2), Hold(Right), Until(XAtLeast(L.EntryBX - 1.6f)), Release(), Until(Dead())), revealedBy: "Glass_B2"),
                new Betrayal("Portal_B [W]: a cat that runs into Portal_B off its beat comes out over Pit_B", "Pit_B_Laser", DeathCause.Hazard,
                    Bt("go now", N, After(near, "Until(Still)", 2), Until(Exit("B off", B, 1, L.EntryBX)), Hold(Right), Until(Dead())), revealedBy: "Portal_B"),
                // ---- Deep ----
                new Betrayal("Portal_C [W]: a cat that jumps into Pit_C off its beat falls out of Portal_C over Pit_C2", "Pit_C2_Laser", DeathCause.Hazard,
                    Bt("jump now", Dp, After(deep, "Until(Still)"), Until(Exit("C off", Cc, 1, L.PitC0 - .1f + L.PortalW * .5f, CFall)), Hold(Right), Until(XAtLeast(L.PitC0 - .1f)), Jump(),
                        Until(XAtLeast(L.WallC1)), Release(), Until(Dead())), revealedBy: "Portal_C"),
                new Betrayal("Ferry_X [W]: a cat that goes into Portal_X with Ferry_X away drops into the lake", "Lake_X_Laser", DeathCause.Hazard,
                    Bt("go now", Dp, After(deep, "Until(Still)", 3), Until(FerryAtEnd()), Hold(Right), Until(XAtLeast(L.Lake0)), Release(), Until(Dead())), revealedBy: "Ferry_X"),
                new Betrayal("Eye_D [NW]: a cat that waits by Portal_D is shot", "Eye_D", DeathCause.Hazard,
                    Bt("wait by it", Dp, After(deep, "Until(Still)", 5), Hold(Right), Until(XAtLeast(L.EntryDX - 1.6f)), Release(), Until(Dead()))),
                new Betrayal("Portal_D [W]: a cat that runs into Portal_D before its exit reaches the ledge falls into the chasm", "Chasm_Laser", DeathCause.Hazard,
                    Bt("go now", Dp, After(deep, "Until(Still)", 5), Until(Exit("D early", D, 2, L.EntryDX)), Hold(Right), Until(Dead())), revealedBy: "Portal_D"));
        }
    }
}
