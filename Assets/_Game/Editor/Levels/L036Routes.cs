using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L036Layout;

namespace Parallax.Editor.Levels
{
    // D-267: level 36's routes ("Overwatch"). Every eye is periodic from the room's start, so each wait is on the room tick:
    // Beat(period, tick) is true on the tick an eye's shot ends. Betrayals branch off the solution's own steps.
    static class L036Routes
    {
        const int Shot = L.EyeAim + L.EyeLock + L.EyeFire;
        // The tick (mod its period) each eye's shot ends.
        static readonly int Eye1End = (L.Eye1Phase + Shot) % L.Eye1Period, Eye2End = (L.Eye2Phase + Shot) % L.Eye2Period;
        static readonly int EyeWEnd = (L.EyeWPhase + Shot) % L.CrossPeriod, EyeEEnd = (L.EyeEPhase + Shot) % L.CrossPeriod;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // Hop the block whose west face is x0 from where the cat stands (pressed on it or short of it).
        static RouteStep[] Hop(float x0) => new[] { Until(XAtLeast(x0 - 1.5f)), Jump(), Until(Airborne()), Until(Grounded()) };
        // Run on into the block at x0 and stand pressed on it.
        static RouteStep[] Press(float x0) => new[] { Until(XAtLeast(x0 - 1f)), Until(Still()), Release() };

        // ---- Hall 1 ----
        static RouteStep[] Hall1() => new RouteStep[] { Hold(Right) }
            .Concat(Press(L.Block1X))
            .Concat(new[] { Until(Beat(L.Eye1Period, Eye1End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block1X)).Concat(Press(L.Block3X))
            .Concat(new[] { Until(Beat(L.Eye1Period, Eye1End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block3X))
            // Short of Glass_4: a cat that touches it breaks it.
            .Concat(new[] { Until(XAtLeast(L.Glass4X - 1.9f)), Release(), Until(Still()) })
            .Concat(new[] { Until(Beat(L.Eye1Period, Eye1End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Glass4X)).Concat(Press(L.Block5X))
            .Concat(new[] { Until(Beat(L.Eye1Period, Eye1End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block5X)).Concat(Press(L.Block6X))
            .Concat(new[] { Until(Beat(L.Eye1Period, Eye1End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block6X))
            .Concat(new[] { Until(XAtLeast(L.Pit1X0 - .6f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(L.Checkpoint1X - .3f)) })
            .ToArray();

        // ---- Hall 2 ----
        static RouteStep[] Hall2() => new RouteStep[] { Release(), Until(Still()),
                // Walk in Shield_1's shadow (pressed on it) until it has sunk and the cat is at Block_7.
                Until(Beat(L.Eye2Period, L.EscortBeat)), Hold(Right).Timed(TimedMode.Shift) }
            .Concat(Press(L.Block7X))
            .Concat(new[] { Until(Beat(L.Eye2Period, Eye2End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block7X)).Concat(Press(L.Block8X))
            .Concat(new[] { Until(Beat(L.Eye2Period, Eye2End)), Hold(Right).Timed(TimedMode.Shift) }).Concat(Hop(L.Block8X))
            .Concat(new[] { Until(XAtLeast(L.Tower2X0 + .5f)), Release(), Until(Still()) })
            .Concat(Cross())
            .ToArray();

        // ---- The crossfire: hop each block as Eye_E's shot ends, cross each gap as Eye_W's ends ----
        static RouteStep[] Cross()
        {
            var s = new System.Collections.Generic.List<RouteStep>();
            for (int k = 0; k < L.CrossX.Length; k++)
            {
                s.AddRange(new[] { Until(Beat(L.CrossPeriod, EyeEEnd)), Hold(Right).Timed(TimedMode.Shift) });
                s.AddRange(Hop(L.CrossX[k]));
                s.AddRange(new[] { Until(XAtLeast(L.CrossX[k] + L.CoverW + 1f)), Release(), Until(Still()) });
                s.AddRange(new[] { Until(Beat(L.CrossPeriod, EyeWEnd)), Hold(Right).Timed(TimedMode.Shift) });
                if (k + 1 < L.CrossX.Length) s.AddRange(Press(L.CrossX[k + 1]));
            }
            s.Add(Until(RoomComplete()));
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
            throw new InvalidOperationException($"L036Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Hall1(), Hall2() };
            var solution = new Route("L036 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int H1 = 0, H2 = 1;
            RouteStep[] h1 = parts[H1], h2 = parts[H2];
            return new RoomRoutes(solution,
                // ---- Hall 1 ----
                new Betrayal("Eye_1 [OL]: a cat that stops in the open is shot", "Eye_1", DeathCause.Hazard,
                    B("stop", H1, After(h1, "Until(Grounded)", 2), Until(XAtLeast(21f)), Release(), Until(Dead()))),
                new Betrayal("Watch_1 [NW]: a cat that stays behind Block_1 for a second shot is shot from above", "Watch_1", DeathCause.Hazard,
                    B("stay", H1, After(h1, "Release", 1), Until(Dead()))),
                new Betrayal("Holo_2 [H]: a cat that takes cover behind Holo_2 is shot through it", "Eye_1", DeathCause.Hazard,
                    B("hide", H1, After(h1, "Until(Grounded)", 1), Until(XAtLeast(L.Holo2X - .8f)), Release(), Until(Dead()))),
                new Betrayal("Glass_4 [T]: a cat that leans on Glass_4 is shot as it breaks", "Eye_1", DeathCause.Hazard,
                    B("lean", H1, After(h1, "Until(Grounded)", 2), Until(Dead()))),
                // ---- Hall 2 ----
                new Betrayal("Shield_1 [NW]: a cat that stops while Shield_1 goes on is shot", "Eye_2", DeathCause.Hazard,
                    B("stop", H2, After(h2, "Hold(Right)", 1), Until(XAtLeast(60f)), Release(), Until(Dead()))),
                new Betrayal("Shield_1 [S]: a cat that stays where Shield_1 sank is shot", "Eye_2", DeathCause.Hazard,
                    B("stay", H2, After(h2, "Hold(Right)", 1), Until(XAtLeast(L.SlotX0 - .7f)), Release(), Until(Dead()))),
                new Betrayal("Eye_W [NW]: a cat that waits in front of a block for a second shot is shot by Eye_W", "Eye_W", DeathCause.Hazard,
                    B("wait", H2, After(h2, "Release", 6), Until(Dead()))),
                new Betrayal("Eye_E [NW]: a cat that waits by the door for Eye_E's shot is shot", "Eye_E", DeathCause.Hazard,
                    B("wait", H2, After(h2, "Release", 11), Until(Dead()))));
        }
    }
}
