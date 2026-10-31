using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L032Layout;

namespace Parallax.Editor.Levels
{
    // D-263: level 32's routes ("Overflow"). The chimneys are climbed by wall jumps (D-110 amendment 4: the cat latches by itself
    // as it comes against a panel); the floods are on the clock from their fire, so the solution just keeps moving. Betrayals
    // branch off the solution's own steps.
    static class L032Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // Up a chimney from standing in its foot: the first jump toward `first` (Right: the east face), `kicks` jumps from face to
        // face, then a jump off the last face over the pillar's top onto the storey (away from that face).
        static RouteStep[] Climb(int first, int kicks)
        {
            var s = new List<RouteStep> { Jump(), Hold(first), Until(first == Right ? ClingingRight() : ClingingLeft()), Release() };
            int side = first;
            for (int k = 0; k < kicks; k++)
            {
                side = -side;
                s.AddRange(new[] { Jump(), For(2), Hold(side), Until(side == Right ? ClingingRight() : ClingingLeft()), Release() });
            }
            s.AddRange(new[] { Jump(), For(2), Hold(-side), Until(Grounded()) });
            return s.ToArray();
        }

        // ---- Lower: S0, Chimney A, S1, Chimney B ----
        static RouteStep[] Lower() => new[] {
            // Over Retract_0 at a run and on into Chimney A's foot.
            new[] { Hold(Right), Until(XAtLeast(L.ChimE0 + .9f)), Release(), Until(Still()) },
            Climb(Right, 2),
            // S1, west: into Bar_1's trigger and stop; let the bar fly over and stick; then over Step_1 and Holo_1 to Chimney B.
            new[] { Hold(Left), Until(XAtMost(L.Bar1TrigX1 - .6f)), Release(), Until(Still()), Until(Fired("Bar_1")), Until(Since("Bar_1", 30)),
                Hold(Left).Timed(TimedMode.Hesitate), Until(XAtMost(L.Step1X1 + .7f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(L.Holo1X1 + .8f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(L.ChimW1 - .9f)), Release(), Until(Still()) },
            Climb(Left, 2),
            new[] { Hold(Right), Until(XAtLeast(L.Checkpoint1X - .3f)) },
        }.SelectMany(x => x).ToArray();

        // ---- Upper: S2, Chimney C, S3 upside down, Chimney D, S4 ----
        static RouteStep[] Upper() => new[] {
            // Under Squeeze_2 at a run, straight into Chimney C's foot.
            new[] { Until(XAtLeast(L.ChimE0 + .9f)), Release(), Until(Still()) },
            Climb(Right, 2),
            // S3, west: Flip_3U up onto the ceiling; jump Flip_3T; Inv_3 turns the stick round: press back till it wears off; Flip_3D
            // down by Chimney D.
            new[] { Hold(Left), Until(GravityUp()), Until(XAtMost(L.Flip3TX + 2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Fired("Inv_3")), Hold(Right),
                Until(Since("Inv_3", L.Inv3Ticks)), Hold(Left), Until(GravityDown()), Until(Grounded()), Until(XAtMost(L.ChimW1 - .9f)), Release(), Until(Still()) },
            Climb(Left, 2),
            // S4: over Retract_4 at a run; jump Bar_4 as it comes from behind; over the Sill to the door.
            new[] { Hold(Right), Until(XAtLeast(L.Bar4TrigX + .5f)), Until(Fired("Bar_4")), Until(Since("Bar_4", 2)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(L.SillX0 - .9f)), Jump(), Until(Airborne()), Until(RoomComplete()) },
        }.SelectMany(x => x).ToArray();

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L032Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Lower(), Upper() };
            var solution = new Route("L032 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int Lo = 0, Up = 1;
            RouteStep[] lo = parts[Lo], up = parts[Up];
            return new RoomRoutes(solution,
                // ---- Lower ----
                new Betrayal("Retract_0 [NW]: a cat that stops on the bridge falls into Pit_0 as it pulls back", "Pit_0_Laser", DeathCause.Hazard,
                    B("stop on it", Lo, After(lo, "Hold(Right)"), Until(XAtLeast((L.Ret0X0 + L.Ret0X1) * .5f - .2f)), Release(), Until(Dead())), revealedBy: "Retract_0"),
                new Betrayal("Flood_1 [NW]: a cat that hangs about at Chimney A's foot drowns", "Flood_1", DeathCause.Hazard,
                    B("hang about", Lo, After(lo, "Until(Still)"), Until(Dead()))),
                new Betrayal("Bar_1 [BAIT]: a cat that jumps Step_1 as Bar_1 fires is hit", "Bar_1", DeathCause.Hazard,
                    B("jump at once", Lo, After(lo, "Until(X<=13.9"), Until(XAtMost(L.Step1X1 + .7f)), Jump(), Until(Dead()))),
                new Betrayal("Holo_1 [J]: a cat that walks onto Holo_1 falls through it into the flood", "Flood_1", DeathCause.Hazard,
                    B("walk on", Lo, After(lo, "Until(Grounded)", 2), Until(Dead())), revealedBy: "Holo_1"),
                new Betrayal("Chimney B [NW]: a cat that hangs about at Chimney B's foot drowns", "Flood_1", DeathCause.Hazard,
                    B("hang about", Lo, After(lo, "Until(Still)", 2), Until(Dead()))),
                // ---- Upper ----
                new Betrayal("Squeeze_2 [NW]: a cat that stops under Squeeze_2 is crushed", "Squeeze_2", DeathCause.Hazard,
                    B("stop under it", Up, 0, Until(XAtLeast((L.Sq2X0 + L.Sq2X1) * .5f - 1f)), Release(), Until(Dead()))),
                new Betrayal("Flip_3T [J]: a cat that walks on into Flip_3T upside down falls into the hole", "Flood_2", DeathCause.Hazard,
                    B("walk on", Up, After(up, "Until(X<=10"), Until(Dead())), revealedBy: CatGravity),
                new Betrayal("Inv_3 [B]: a cat that keeps pressing on as Inv_3 turns the stick round walks back into Flip_3T", "Flood_2", DeathCause.Hazard,
                    B("keep pressing on", Up, After(up, "Until(Fired(Inv_3))"), Until(Dead())), revealedBy: "Inv_3"),
                new Betrayal("Surge [NW]: a cat that waits at Chimney D's foot drowns as the flood surges", "Surge", DeathCause.Hazard,
                    B("wait", Up, After(up, "Until(Still)", 2), Until(Dead()))),
                new Betrayal("Retract_4 [NW]: a cat that stops on Retract_4 falls into the flood", "Flood_2", DeathCause.Hazard,
                    B("stop on it", Up, After(up, "Until(X>=13.5") - 1, Until(XAtLeast((L.Ret4X0 + L.Ret4X1) * .5f)), Release(), Until(Dead())), revealedBy: "Retract_4"),
                new Betrayal("Bar_4 [J]: a cat that runs on at Bar_4 without jumping is hit", "Bar_4", DeathCause.Hazard,
                    B("run on", Up, After(up, "Until(Bar_4 +2"), Until(Dead()))));
        }
    }
}
