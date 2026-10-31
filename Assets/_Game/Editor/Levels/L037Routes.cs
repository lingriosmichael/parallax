using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L037Layout;

namespace Parallax.Editor.Levels
{
    // D-268: level 37's routes ("Transit"). Every platform is periodic from the room's start, so each hop waits on where the next
    // platform is (L037Layout.M's pose, the runtime's own TrapPath): When(...) on the room tick, Over(m) once the cat is over a
    // platform (release there and drop on), WarnStarts(m) on the first tick a glitch platform flickers. Betrayals branch off the
    // solution's own steps.
    static class L037Routes
    {
        static RouteCondition When(string label, Func<int, bool> f) => new(label, v => f(v.Last.RoomLifeTick));
        static RouteCondition Over(L.M m) => new($"over {m.Name}", v => v.Last.X >= m.Left(v.Last.RoomLifeTick) + .6f);
        static bool Warns(L.M m, int t)
        {
            int s = m.Since(t);
            return s >= 0 && TrapPath.GlitchWarning(m.Points, s, m.Move, m.Hold, m.Ret, L.Warn, out int next) && next > TrapPath.GlitchIndex(m.Points.Length, TrapPath.Progress(s, m.Move, m.Hold, m.Ret));
        }
        static RouteCondition WarnStarts(L.M m) => When($"{m.Name} flickers", t => Warns(m, t) && !Warns(m, t - 1));

        // From the platform the cat is on, jump at a run and drop onto `m`.
        static RouteStep[] HopTo(L.M m) => new[] { Hold(Right), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Over(m)), Release(), Until(Grounded()) };

        // ---- Section 0 ----
        static RouteStep[] Lake1() => new[] {
            Hold(Right), Until(XAtLeast(L.StartX1 - .7f)), Jump(), Until(Airborne()), Until(Over(L.Ferry1)), Release(), Until(Grounded()),
            Until(When("Ferry_1 at its end", t => L.Ferry1.Offset(t).x >= 8.5f)) }
            .Concat(HopTo(L.Sink2))
            .Concat(new[] { Until(When("Sink_2 near its end", t => L.Sink2.Offset(t).x >= 6.3f)) }).Concat(HopTo(L.Glitch3))
            .Concat(new[] { Until(WarnStarts(L.Glitch3)) }).Concat(Leap(L.Glitch3, 1))
            .Concat(new[] { Until(WarnStarts(L.Glitch3)) }).Concat(Leap(L.Glitch3, 2))
            .Concat(new[] { Until(When("Ferry_4 at home", t => L.Ferry4.Since(t) < 0 || L.Ferry4.Since(t) > L.Ferry4.Period - 30)) }).Concat(HopTo(L.Ferry4))
            .Concat(new[] { Until(When("Ferry_4 at its end", t => L.Ferry4.Offset(t).x >= 8.8f)), Hold(Right), Jump().Timed(TimedMode.Shift), Until(Airborne()),
                Until(XAtLeast(L.MidX0 + .6f)), Release(), Until(Grounded()), Hold(Right), Until(XAtLeast(L.Checkpoint1X - .3f)) })
            .ToArray();

        // Leapfrog a glitch platform: jump as it flickers, toward its next spot (`index`), and drop there.
        static RouteStep[] Leap(L.M m, int index)
        {
            float x = m.X0 + TrapPath.Point(m.Points, index).x + .6f;
            return new[] { Hold(Right), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(XAtLeast(x)), Release(), Until(Grounded()) };
        }

        // ---- Section 1 ----
        static RouteStep[] Lake2() => new[] {
            Release(), Until(Still()),
            Until(When("Lift_5 about to rise", t => L.Lift5.Since(t) >= L.Lift5.Period - 25)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.MidX1 - .6f)), Jump(),
            Until(Airborne()), Until(Over(L.Lift5)), Release(), Until(Grounded()),
            Until(When("Lift_5 at the top", t => L.Lift5.Since(t) >= L.Lift5.Move + 4 && L.Lift5.Since(t) < L.Lift5.Move + L.Lift5.Hold)) }
            .Concat(HopTo(L.Ferry6))
            .Concat(new[] { Until(When("Ferry_6 at its end", t => L.Ferry6.Offset(t).x >= 7.5f)) }).Concat(HopTo(L.Slide7))
            .Concat(new[] { Until(When("Slide_7 at the bottom", t => L.Slide7.Offset(t).y <= -2.4f)) }).Concat(HopTo(L.Leap8))
            .Concat(new[] { Until(WarnStarts(L.Leap8)) }).Concat(Leap(L.Leap8, 1))
            .Concat(new[] { Until(WarnStarts(L.Leap8)) }).Concat(Leap(L.Leap8, 2))
            .Concat(new[] { Until(WarnStarts(L.Leap8)) }).Concat(Leap(L.Leap8, 3))
            .Concat(new[] { Hold(Right), Jump(), Until(RoomComplete()) })
            .ToArray();

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L037Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Lake1(), Lake2() };
            var solution = new Route("L037 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int P1 = 0, P2 = 1;
            RouteStep[] p1 = parts[P1], p2 = parts[P2];
            return new RoomRoutes(solution,
                new Betrayal("Sink_2 [NW]: a cat that rides Sink_2 to its end goes down with it into the laser", "Lake_Laser", DeathCause.Hazard,
                    B("ride on", P1, After(p1, "Until(Grounded)", 2), Until(Dead())), revealedBy: "Sink_2"),
                new Betrayal("Glitch_3 [NW]: a cat that stays on Glitch_3 falls as it jumps away", "Lake_Laser", DeathCause.Hazard,
                    B("stay", P1, After(p1, "Until(Grounded)", 3), Until(Dead())), revealedBy: "Glitch_3"),
                new Betrayal("Leap_8 [NW]: a cat that stays on Leap_8 falls as it jumps away", "Lake_Laser", DeathCause.Hazard,
                    B("stay", P2, After(p2, "Until(Grounded)", 5), Until(Dead())), revealedBy: "Leap_8"));
        }
    }
}
