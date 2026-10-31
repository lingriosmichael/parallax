using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L034Layout;

namespace Parallax.Editor.Levels
{
    // D-265: level 34's routes ("Phase Shift"). The stones are periodic from the room's start, so each hop waits on the room tick
    // (Beat); the orbs and the glass then carry the cat on, and the solution just keeps moving. Betrayals branch off the solution's
    // own steps.
    static class L034Routes
    {
        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // From a platform whose east edge is `edge`, at room tick `go` (mod the blink's period): run to the edge, jump, let go over
        // the next stone (from `next`), land and stop.
        static RouteStep[] Hop(float edge, float next, int go) => new[] {
            Until(Beat(L.BlinkPeriod, go)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(edge - .55f)), Jump(), Until(Airborne()),
            Until(XAtLeast(next + .6f)), Release(), Until(Grounded()), Until(Still()),
        };
        // Blink_k lights at its phase; a hop onto it leaves HopLead before.
        const int HopLead = 26;

        static RouteStep[] Blink()
        {
            var s = new List<RouteStep> { Hold(Right), Until(XAtLeast(L.StartX1 - 1f)), Release(), Until(Still()) };
            for (int k = 0; k < L.BlinkCount; k++)
            {
                float edge = k == 0 ? L.StartX1 : L.BlinkX0(k - 1) + L.StoneW;
                s.AddRange(Hop(edge, L.BlinkX0(k), ((L.BlinkPhase(k) - HopLead) % L.BlinkPeriod + L.BlinkPeriod) % L.BlinkPeriod));
            }
            // Off the last stone onto Mid before it goes.
            s.AddRange(new[] { Hold(Right), Until(XAtLeast(L.BlinkX0(L.BlinkCount - 1) + L.StoneW - .55f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(L.Checkpoint1X - .3f)) });
            return s.ToArray();
        }

        // Into an orb and onto the next stone: jump off this one's far edge, fall to the next.
        static RouteStep[] Through(float edge, bool toCeiling) => new[] {
            Until(XAtLeast(edge - .55f)), Jump(), Until(toCeiling ? GravityUp() : GravityDown()), Until(Grounded()),
        };

        static RouteStep[] Flip()
        {
            var st = L.FlipStones;
            var s = new List<RouteStep> { Until(XAtLeast(L.MidX1 - .7f)), Release(), Until(Still()),
                Until(Beat(L.FlipPeriod, FlipGo)), Hold(Right).Timed(TimedMode.Shift) };
            s.AddRange(Through(L.MidX1, true));                       // onto Stone_C1
            s.AddRange(Through(st[0].x0 + L.StoneW, false));          // onto Stone_F2
            s.AddRange(Through(st[1].x0 + L.StoneW, true));           // onto Stone_C3, where Inv_C3 turns the stick round: press back
            s.AddRange(new[] { Until(Fired("Inv_C3")), Hold(Left), Until(XAtLeast(st[2].x0 + L.StoneW - .55f)), Jump(), Until(GravityDown()),
                Until(Since("Inv_C3", L.InvTicks)), Hold(Right), Until(Grounded()) });   // onto Stone_F4
            s.AddRange(Through(st[3].x0 + L.StoneW, true));           // onto Stone_C5
            s.AddRange(Through(st[4].x0 + L.StoneW, false));          // onto Land
            // The blackout: run over the glass, up through Orb_B1 onto the ceiling's tiles, down through Orb_B2 to the door.
            s.AddRange(Through(L.Tiles1, true));
            s.AddRange(new[] { Until(XAtLeast(L.CeilTiles1 - .55f)), Jump(), Until(GravityDown()), Until(RoomComplete()) });
            return s.ToArray();
        }
        // The flip stretch's go (mod its period), so the cat reaches each stone as it lights.
        const int FlipGo = 100;

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L034Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Blink(), Flip() };
            var solution = new Route("L034 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int Bl = 0, Fl = 1;
            RouteStep[] blink = parts[Bl], flip = parts[Fl];
            return new RoomRoutes(solution,
                // ---- Blink ----
                new Betrayal("Blink_1 [NW]: a cat that stays on a lit stone falls as it goes out", "Void_Laser", DeathCause.Hazard,
                    B("stay", Bl, After(blink, "Until(Still)", 2), Until(Dead())), revealedBy: "Blink_1"),
                new Betrayal("Blink_2 [W]: a cat that hops on at once lands where the next stone isn't lit yet", "Void_Laser", DeathCause.Hazard,
                    B("hop now", Bl, After(blink, "Until(Still)", 2), Hold(Right), Until(XAtLeast(L.BlinkX0(0) + L.StoneW - .55f)), Jump(), Until(Airborne()),
                        Until(XAtLeast(L.BlinkX0(1) + .6f)), Release(), Until(Dead())), revealedBy: "Blink_2"),
                // ---- Flip ----
                new Betrayal("Stone_C1 [W]: a cat that goes off the beat is on Stone_C1 as it goes out, and falls up into the laser roof", "Roof_Laser", DeathCause.Hazard,
                    B("go now", Fl, After(flip, "Until(Still)") , Until(Beat(L.FlipPeriod, FlipGo + 60)), Hold(Right), Until(XAtLeast(L.MidX1 - .55f)), Jump(), Until(GravityUp()), Until(Dead())), revealedBy: "Stone_C1"),
                new Betrayal("Orb_D2 [J]: a cat that walks off Stone_C1 instead of jumping into Orb_D2 falls up into the laser roof", "Roof_Laser", DeathCause.Hazard,
                    B("walk off", Fl, After(flip, "Until(Grounded)", 1), Until(Dead())), revealedBy: "Stone_C1"),
                new Betrayal("Inv_C3 [B]: a cat that keeps pressing on as Inv_C3 turns the stick round walks back off Stone_C3", "Roof_Laser", DeathCause.Hazard,
                    B("keep pressing on", Fl, After(flip, "Until(Fired(Inv_C3))"), Until(Dead())), revealedBy: "Inv_C3"),
                new Betrayal("Tile_2 [NW]: a cat that stops on the glass falls through as it shatters", "Void_Laser", DeathCause.Hazard,
                    B("stop", Fl, After(flip, "Until(Grounded)", 6), Until(XAtLeast(L.Tiles0 + L.TileW + .8f)), Release(), Until(Dead())), revealedBy: "Tile_2"),
                new Betrayal("Tile_9 [NW]: a cat that stops on the ceiling's glass falls up as it shatters", "Roof_Laser", DeathCause.Hazard,
                    B("stop", Fl, After(flip, "Until(Grounded)", 7), Release(), Until(Dead())), revealedBy: "Tile_9"));
        }
    }
}
