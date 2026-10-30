using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-191, D-209: level 28's routes ("Glitch"). Almost every wait is on the room tick's beat (the presses and the spinners keep their
    // own). Betrayals branch off the solution's own steps.
    static class L028Routes
    {
        const int P = L028Layout.Period;
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // ---- Top ----
        // Wait short of Press_A; under it while it's up; past Spin_A while its arm is up; onto
        // Loop_A at home, ride it up, over and down, off east; down the right shaft past Spin_B.
        static RouteStep[] Top() => new[] {
            Hold(Right), Until(XAtLeast(8.2f)), Release(), Until(Still()),
            Until(Beat(P, PressAGo)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(16f)), Release(), Until(Still()),
            Until(Beat(120, 2)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(22.4f)), Release(), Until(Still()),
            Until(Beat(L028Layout.LoopPeriod, 205)), Hold(Right), Until(XAtLeast(24f)), Release(),
            Until(Beat(L028Layout.LoopPeriod, 136)), Hold(Right), Until(XAtLeast(33f)), Release(), Until(Still()),
            Until(Beat(120, 85)), Hold(Right).Timed(TimedMode.Shift), Until(Airborne()), Release(), Until(Grounded()), Until(Still()),
        };

        // The beat the cat goes under Press_A on (it's up from 2 * Step to Period).
        const int PressAGo = 2 * L028Layout.Step + 4;

        // ---- Bottom ----
        // West under the crushers, each while it's home, stopping between them; onto Lift_L and up.
        static RouteStep[] Bottom() => new[] {
            Hold(Left), Until(XAtMost(28.2f)), Release(), Until(Still()),
            Until(Beat(P, 124)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(22.2f)), Release(), Until(Still()),
            Until(Beat(P, 54)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(16.2f)), Release(), Until(Still()),
            Until(Beat(P, 4)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(4.2f)), Release(), Until(Fired("Lift_L")),
        };

        // ---- Inner ----
        // East over the crushers' holes, each while it's home; past Spin_E as its arm swings away; the door.
        static RouteStep[] Inner() => new[] {
            Until(YAtLeast(L028Layout.EFloor + .2f)), Until(Grounded()), Hold(Right), Until(XAtLeast(13f)), Release(), Until(Still()),
            Until(Beat(P, 4)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(19f)), Release(), Until(Still()),
            Until(Beat(P, 54)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(25f)), Release(), Until(Still()),
            Until(Beat(P, 124)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(27.6f)), Release(), Until(Still()),
            Until(Beat(120, 60)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L028Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Top(), Bottom(), Inner() };
            var solution = new Route("L028 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int T = 0, Bo = 1, I = 2;
            RouteStep[] top = parts[T], bottom = parts[Bo], inner = parts[I];
            return new RoomRoutes(solution,
                new Betrayal("T1 [W]: a cat that goes under Press_A as it comes down is crushed", "Press_A", DeathCause.Hazard,
                    B("go now", T, After(top, "Until(Still)"), Until(Beat(P, L028Layout.Step - 20)), Hold(Right), Until(XAtLeast(9.6f)), Release(), Until(Dead()))),
                new Betrayal("T3 [W]: a cat that walks into Spin_A's arm is struck", "Spin_A", DeathCause.Hazard,
                    B("walk on", T, After(top, "Until(Still)", 2), Until(Beat(120, 62)), Hold(Right), Until(Dead()))),
                new Betrayal("T5 [W]: a cat that walks under Crush_1 as it comes down is crushed", "Crush_1", DeathCause.Hazard,
                    B("go now", Bo, After(bottom, "Until(Still)"), Until(Beat(P, 50)), Hold(Left), Until(XAtMost(26.6f)), Release(), Until(Dead()))),
                new Betrayal("T7 [W]: a cat that walks into Spin_E's arm is struck", "Spin_E", DeathCause.Hazard,
                    B("walk on", I, After(inner, "Until(Still)", 4), Until(Beat(120, 0)), Hold(Right), Until(Dead()))));
        }
    }
}
