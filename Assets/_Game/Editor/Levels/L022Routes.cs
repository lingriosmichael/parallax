using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-192, D-204: level 22's routes ("Clockwork Ridge"). Every wait is on the room tick's beat: the spinners', the stair's, the
    // turret's, the flips' and pendulums', the cogs' and Hand's. Hops land where they're aimed by letting go of the stick in the air.
    // Betrayals branch off the solution's own steps.
    static class L022Routes
    {
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // A hop east that lets go of the stick past `at`, landing on what's under.
        static RouteStep[] Hop(float at) => new[] { Hold(Right), Jump(), Until(XAtLeast(at)), Release(), Until(Grounded()) };

        // ---- Gears ----
        // Short of Spin_1; under it while it lies flat; wait between the bars; under Spin_2 while it lies flat.
        static RouteStep[] Gears() => new[] {
            Hold(Right), Until(XAtLeast(5f)), Release(), Until(Still()),
            Until(Beat(90, 70)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(8.9f)), Release(), Until(Still()),
            Until(Beat(90, 20)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(15.3f)), Release(), Until(Still()),
        };

        // ---- Stair ----
        // Up the stair as each step appears: Step_1, Step_2, Step_3, Peak.
        static RouteStep[] Stair() => new RouteStep[][] {
            new[] { Until(Beat(L022Layout.StepPeriod, L022Layout.StepPhase + 2)) }, Hop(17.8f),
            new[] { Until(Beat(L022Layout.StepPeriod, L022Layout.StepPhase + 32)) }, Hop(21.3f),
            new[] { Until(Beat(L022Layout.StepPeriod, L022Layout.StepPhase + 62)) }, Hop(24.8f),
            Hop(28.2f), new[] { Hold(Right), Until(XAtLeast(30.5f)), Release(), Until(Still()) },
        }.SelectMany(p => p).ToArray();

        // ---- Descent ----
        // Down Flip_1, Flip_2, Flip_3 while each is right side up and its pendulum is away; into the valley.
        static RouteStep[] Descent() => new RouteStep[][] {
            new[] { Until(Beat(L022Layout.FlipPeriod, L022Layout.DescentPhase + 47)) }, Hop(33.3f), Hop(36.8f), Hop(40.3f), Hop(43.6f),
            new[] { Hold(Right), Until(XAtLeast(44.8f)), Release(), Until(Still()) },
        }.SelectMany(p => p).ToArray();

        // ---- Valley ----
        // Past Spin_V onto Plate_V; wait off the slot while Lift_P comes down; on, up the cliff; under Swing_C to the tower.
        static RouteStep[] ValleySteps() => new[] {
            Until(Beat(90, 40)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(50.4f)), Release(), Until(Still()), Until(Fired("Lift_P")),
            Until(Since("Lift_P", L022Layout.LiftMove + 4)), Hold(Right), Until(XAtLeast(53.8f)), Release(), Until(Still()),
            Until(Since("Lift_P", 2 * L022Layout.LiftMove + L022Layout.LiftHold + 2)),
            Until(Beat(L022Layout.SwingCPeriod, 49)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(66.9f)), Release(), Until(Still()),
        };

        // ---- Tower ----
        // Onto Cog_1 at home; up and east; off onto the pillar; across it while Hand is up; onto Cog_2 at home; up and east; off
        // onto the door's ledge, to the door.
        static RouteStep[] Tower() => new[] {
            Until(Beat(L022Layout.CogPeriod, L022Layout.Cog1Phase + L022Layout.CogMove + 4)), Hold(Right), Until(XAtLeast(68.9f)), Release(), Until(Still()),
            Until(Beat(L022Layout.CogPeriod, L022Layout.Cog1Phase + CogCorner - 10)), Hold(Right), Until(XAtLeast(77.3f)), Release(), Until(Still()),
            Until(Beat(L022Layout.CogPeriod, L022Layout.Cog2Phase + CogCorner - 10)), Hold(Right), Until(RoomComplete()),
        };

        // A cog reaches its top east corner (up CogRise, then east CogRun: half its loop) this long after it leaves home.
        const int CogCorner = L022Layout.CogMove / 2;

        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L022Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Gears(), Stair(), Descent(), ValleySteps(), Tower() };
            var solution = new Route("L022 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int G = 0, S = 1, D = 2, V = 3, T = 4;
            RouteStep[] gears = parts[G], stair = parts[S], descent = parts[D], valley = parts[V], tower = parts[T];
            return new RoomRoutes(solution,
                new Betrayal("T1 [W]: a cat that goes under Spin_1 as it turns down is struck", "Spin_1", DeathCause.Hazard,
                    B("go now", G, After(gears, "Until(Still)"), Until(Beat(90, 30)), Hold(Right), Until(Dead()))),
                new Betrayal("T2 [W]: a cat that goes under Spin_2 as it turns down is struck", "Spin_2", DeathCause.Hazard,
                    B("go now", G, After(gears, "Until(Still)", 2), Until(Beat(90, 60)), Hold(Right), Until(Dead()))),
                new Betrayal("T3 [NW]: a cat that waits at the stair's foot for a better wave is shot by Eye_S", "Eye_S", DeathCause.Hazard,
                    B("wait", S, 0, Until(Dead()))),
                new Betrayal("T4 [NW]: a cat that waits on Step_1 falls as it fades", "Pit_1", DeathCause.Hazard,
                    B("wait", S, After(stair, "Until(Grounded)"), Until(Dead())), revealedBy: "Step_1"),
                new Betrayal("T5 [W]: a cat that sets off as soon as Flip_1 rights itself meets Swing_2", "Swing_2", DeathCause.Hazard,
                    B("go now", D, 0, new[] { Until(Beat(L022Layout.FlipPeriod, L022Layout.DescentPhase + 36)) }.Concat(Hop(33.3f)).Concat(Hop(36.8f)).Append(Until(Dead())).ToArray())),
                new Betrayal("T6 [NW]: a cat that waits on Flip_1 goes down as it turns over", "Flip_1", DeathCause.Hazard,
                    B("wait", D, After(descent, "Until(Grounded)"), Until(Dead())), revealedBy: "Flip_1"),
                new Betrayal("T7 [W]: a cat that walks into Spin_V's arm is struck", "Spin_V", DeathCause.Hazard,
                    B("go now", V, 0, Until(Beat(90, 2)), Hold(Right), Until(Dead()))),
                new Betrayal("T8 [W]: a cat that walks on from Plate_V into the slot is crushed as Lift_P comes down", "Lift_P", DeathCause.Hazard,
                    B("walk on", V, After(valley, "Until(Still)"), Hold(Right), Until(XAtLeast(54f)), Release(), Until(Dead()))),
                new Betrayal("T9 [W]: a cat that steps off Lift_P at once meets Swing_C", "Swing_C", DeathCause.Hazard,
                    B("step off now", V, After(valley, "Until(Lift_P +", 2), Until(Beat(L022Layout.SwingCPeriod, 30)), Hold(Right), Until(Dead()))),
                new Betrayal("T10 [W]: a cat that walks off Cog_1 before it reaches the pillar falls", "Pit_T1", DeathCause.Hazard,
                    B("off now", T, After(tower, "Until(Still)"), Until(Beat(L022Layout.CogPeriod, L022Layout.Cog1Phase + 60)), Hold(Right), Until(Dead())), revealedBy: "Cog_1"),
                new Betrayal("T11 [NW]: a cat that stops on the pillar is struck by Hand", "Hand", DeathCause.Hazard,
                    B("stop", T, After(tower, "Until(Still)"), Until(Beat(L022Layout.CogPeriod, L022Layout.Cog1Phase + CogCorner - 10)), Hold(Right), Until(XAtLeast(75f)), Release(), Until(Dead()))),
                new Betrayal("T12 [W]: a cat that lets Cog_1 go round once more meets Hand on the next lap", "Hand", DeathCause.Hazard,
                    B("next lap", T, 1, new[] { For(L022Layout.CogPeriod) }.Concat(tower.Skip(1).Take(tower.Length - 2)).Append(Until(Dead())).ToArray())));
        }
    }
}
