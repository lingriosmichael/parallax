using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-168 + D-196 + D-209: level 24's routes ("Ceiling Town", the braid). Route moves are stick input: while an inverter holds,
    // Hold(Left) walks the cat east (D-087 (7)). Waits are on the room tick's beat (the pistons, the spinners). Betrayals branch
    // off the solution's own steps.
    static class L024Routes
    {
        const int PP = L024Layout.PistonPeriod;
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        static RouteCondition Inverted() => new("Inverted", v => v.Last.Signature[v.Result.Elements.IndexOf(CatInverted)] == 1);
        static RouteCondition NotInverted() => new("NotInverted", v => v.Last.Signature[v.Result.Elements.IndexOf(CatInverted)] == 0);
        // East (the stick's way, `dir`) to x, then stop.
        static RouteStep[] To(int dir, float x) => new[] { Hold(dir), Until(XAtLeast(x)), Release(), Until(Still()) };
        // On the beat, east (`dir`) to x, then stop.
        static RouteStep[] OnBeat(int period, int tick, int dir, float x) => new[] { Until(Beat(period, tick)), Hold(dir).Timed(TimedMode.Shift), Until(XAtLeast(x)), Release(), Until(Still()) };
        // On the beat, east into the next orb; land on the other face and stop.
        static RouteStep[] Flip(int period, int tick, bool up) => new[] { Until(Beat(period, tick)), Hold(Right).Timed(TimedMode.Shift), Until(up ? GravityUp() : GravityDown()), Until(Grounded()), Release(), Until(Still()) };

        static RouteStep[] Hall() => new RouteStep[][] {
            // Lane 1 into Flip_U1; lane 2 on the ceiling: Inv_1 swaps the controls (hold Left); past Spin_1; past Piston_1 while it's
            // down (Inv_1 ends past it: hold Right again); into Flip_D1.
            new[] { Hold(Right), Until(GravityUp()), Until(Grounded()), Until(Inverted()), Hold(Left), Until(XAtLeast(10.6f)), Release(), Until(Still()) },
            OnBeat(120, Spin1Go, Left, 16.6f),
            new[] { Until(Beat(PP, Lane2Piston)), Hold(Left).Timed(TimedMode.Shift), Until(NotInverted()), Hold(Right), Until(GravityDown()), Release(), Until(Grounded()), Until(Still()) },
            // Lane 3 on the floor: past Piston_2 while it's up, Spin_2, Piston_3; into Flip_U2.
            To(Right, 23.6f), OnBeat(PP, Lane3Piston, Right, 27.2f), OnBeat(120, Spin2Go, Right, 32.6f), Flip(PP, Lane3Flip, true),
            // Lane 4 on the ceiling: past Piston_4 while it's down, Spin_3; into Flip_D3.
            To(Right, 41.6f), OnBeat(PP, Lane4Piston, Right, 45.2f), Flip(90, Spin3Go, false),
            // Lane 5 on the floor: past Spin_5, Piston_5; into Flip_U5.
            To(Right, 56f), OnBeat(90, Lane5Spin, Right, 62.6f), Flip(PP, Lane5Flip, true),
            // Lane 6 on the ceiling: past Piston_6, Spin_6; into Flip_D6; Inv_F swaps the controls (hold Left); the door.
            To(Right, 72.6f), OnBeat(PP, Lane6Piston, Right, 77.6f), Flip(100, Lane6Spin, false),
            new[] { Hold(Right), Until(Inverted()), Hold(Left), Until(RoomComplete()) },
        }.SelectMany(p => p).ToArray();

        // The beats the cat sets off on (tuned against the solution's replay).
        const int Spin1Go = 110, Lane2Piston = 23, Lane3Piston = 1, Spin2Go = 41, Lane3Flip = 159, Lane4Piston = 119, Spin3Go = 41, Lane5Spin = 76, Lane5Flip = 84,
            Lane6Piston = 39, Lane6Spin = 71;

        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L024Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Hall() };
            var solution = new Route("L024 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int index, params RouteStep[] then) => new(name, Branch(parts, 0, index, then));
            RouteStep[] hall = parts[0];
            int Stop(int n) => After(hall, "Until(Still)", n);
            return new RoomRoutes(solution,
                new Betrayal("T1 [NW]: a cat that keeps holding right when Inv_1 swaps the controls walks back onto Spikes_C0", "Spikes_C0", DeathCause.Hazard,
                    B("keep holding", After(hall, "Until(Inverted)"), Until(Dead())), revealedBy: CatInverted),
                new Betrayal("T2 [W]: a cat that walks into Spin_1's arm is struck", "Spin_1", DeathCause.Hazard,
                    B("go now", Stop(1), Until(Beat(120, Spin1Go - 40)), Hold(Left), Until(Dead()))),
                new Betrayal("T3 [W]: a cat that goes as Piston_1 comes back up is crushed", "Piston_1", DeathCause.Hazard,
                    B("go late", Stop(2), Until(Beat(PP, Lane2Piston + 55)), Hold(Left), Until(Dead()))),
                new Betrayal("T4 [W]: a cat that goes as Piston_2 comes back down is crushed", "Piston_2", DeathCause.Hazard,
                    B("go late", Stop(4), Until(Beat(PP, Lane3Piston + 55)), Hold(Right), Until(Dead()))),
                new Betrayal("T5 [W]: a cat that walks into Spin_2's arm is struck", "Spin_2", DeathCause.Hazard,
                    B("go now", Stop(5), Until(Beat(120, Spin2Go - 40)), Hold(Right), Until(Dead()))),
                new Betrayal("T6 [W]: a cat that goes as Piston_3 comes back down is crushed", "Piston_3", DeathCause.Hazard,
                    B("go late", Stop(6), Until(Beat(PP, Lane3Flip + 50)), Hold(Right), Until(Dead()))),
                new Betrayal("T7 [W]: a cat that goes as Piston_4 comes back up is crushed", "Piston_4", DeathCause.Hazard,
                    B("go late", Stop(8), Until(Beat(PP, Lane4Piston + 55)), Hold(Right), Until(Dead()))),
                new Betrayal("T8 [W]: a cat that walks into Spin_3's arm is struck", "Spin_3", DeathCause.Hazard,
                    B("go now", Stop(9), Until(Beat(90, Spin3Go + 45)), Hold(Right), Until(Dead()))),
                new Betrayal("T9 [W]: a cat that walks into Spin_5's arm is struck", "Spin_5", DeathCause.Hazard,
                    B("go now", Stop(11), Until(Beat(90, Lane5Spin + 45)), Hold(Right), Until(Dead()))),
                new Betrayal("T10 [W]: a cat that goes as Piston_5 comes back down is crushed", "Piston_5", DeathCause.Hazard,
                    B("go late", Stop(12), Until(Beat(PP, Lane5Flip + 50)), Hold(Right), Until(Dead()))),
                new Betrayal("T11 [W]: a cat that goes as Piston_6 comes back up is crushed", "Piston_6", DeathCause.Hazard,
                    B("go late", Stop(14), Until(Beat(PP, Lane6Piston + 58)), Hold(Right), Until(Dead()))),
                new Betrayal("T12 [W]: a cat that walks into Spin_6's arm is struck", "Spin_6", DeathCause.Hazard,
                    B("go now", Stop(15), Until(Beat(100, Lane6Spin - 30)), Hold(Right), Until(Dead()))),
                new Betrayal("T13 [NW]: a cat that keeps holding right when Inv_F swaps the controls walks back onto Spikes_F6", "Spikes_F6", DeathCause.Hazard,
                    B("keep holding", After(hall, "Until(Inverted)", 2), Until(Dead())), revealedBy: CatInverted));
        }
    }
}
