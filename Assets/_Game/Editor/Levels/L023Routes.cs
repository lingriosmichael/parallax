using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-206: level 23's routes ("Ferryman"). Each platform the cat stands on sets off under it, so most waits are "until it has
    // arrived"; the presses keep their own beat. Hops land where they're aimed by letting go of the stick in the air. Betrayals
    // branch off the solution's own steps.
    static class L023Routes
    {
        const int D = L023Layout.TriggerDelay, PP = L023Layout.PressPeriod;
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // `ticks` after the element's latest fire (a triggered platform's fire is its trigger plus TriggerDelay, when it starts to move).
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteStep[] Hop(float at) => new[] { Hold(Right), Jump(), Until(XAtLeast(at)), Release(), Until(Grounded()) };
        // Walk east to `x`, stop, and wait to stand still there.
        static RouteStep[] WalkTo(float x) => new[] { Hold(Right), Until(XAtLeast(x)), Release(), Until(Still()) };
        // Stand on `platform` until it has run its `move`.
        static RouteStep Ride(string platform, UnityEngine.Vector2[] path) => Until(Since(platform, L023Layout.Ticks(path) + 2));

        static RouteStep[] Lake() => new RouteStep[][] {
            // Over Climb (resting flush with the shore) down onto Lift; up; onto Glide; down.
            new[] { Hold(Right), Until(XAtLeast(3.4f)), Jump(), Until(XAtLeast(8.3f)), Release(), Until(Grounded()), Until(Still()), Ride("Lift", L023Layout.LiftPath) },
            WalkTo(11.5f), new[] { Ride("Glide", L023Layout.GlidePath) },
            // Onto Ferry so it carries the cat under Press_1 while the press is up; along to V.
            new[] { Until(Beat(PP, L023Layout.Press1Phase + FerryBoard)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(22f)), Release(), Until(Still()), Ride("Ferry", L023Layout.FerryPath) },
            WalkTo(35.5f),
            // Down V; walk off onto the Rock at its bottom.
            new[] { Until(Since("V", L023Layout.Ticks(new[] { L023Layout.VPath[0] }) - 9)), Hold(Right), Until(XAtLeast(42f)), Release(), Until(Still()) },
            // Elevator up; Arc down; Stair down; Rocket up.
            WalkTo(44.5f), new[] { Ride("Elevator", L023Layout.ElevatorPath) }, WalkTo(47f), new[] { Ride("Arc", L023Layout.ArcPath) },
            WalkTo(59.5f), new[] { Ride("Stair", L023Layout.StairPath) }, WalkTo(68f), new[] { Ride("Rocket", L023Layout.RocketPath) },
            // Onto Glide_2 so it carries the cat under Press_2 while the press is up; down to Zig.
            new[] { Until(Beat(PP, L023Layout.Press2Phase + Glide2Board)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(70.5f)), Release(), Until(Still()), Ride("Glide_2", L023Layout.Glide2Path) },
            WalkTo(89f), new[] { Ride("Zig", L023Layout.ZigPath) },
            // Onto Loop; east and up; off onto the east shore as it tops its rise.
            WalkTo(100.5f), new[] { Until(Since("Loop", LoopTop - 6)), Hold(Right), Until(XAtLeast(107.2f)), Release(), Until(Still()) },
        }.SelectMany(p => p).ToArray();

        // Ticks into Press_1's / Press_2's beat that the cat sets off for Ferry / Glide_2.
        const int FerryBoard = 25, Glide2Board = 40;
        // Loop tops its rise (east 3, then up 2.5) this long after it sets off.
        static readonly int LoopTop = L023Layout.Ticks(new[] { L023Layout.LoopPath[0], L023Layout.LoopPath[1] });

        static RouteStep[] ShoreSteps() => new[] { Hold(Right), Until(RoomComplete()) };

        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L023Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Lake(), ShoreSteps() };
            var solution = new Route("L023 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int L = 0;
            RouteStep[] lake = parts[L];
            RouteStep[] Then(RouteStep[] steps, params RouteStep[] more) => steps.Concat(more).ToArray();
            return new RoomRoutes(solution,
                new Betrayal("T1 [J]: a cat that walks onto Climb is carried up into Spikes_C", "Spikes_C", DeathCause.Hazard,
                    B("onto Climb", L, 0, Then(WalkTo(5f), Until(Dead()))), revealedBy: "Climb"),
                new Betrayal("T2 [J]: a cat that jumps up onto Skyway is carried under Spikes_H1", "Spikes_H1", DeathCause.Hazard,
                    B("onto Skyway", L, After(lake, "Until(Lift +"), Until(Beat(L023Layout.Period(L023Layout.SkyPath, L023Layout.SkyHold), 125)), Hold(Right), Until(XAtLeast(9.2f)), Jump(), Until(XAtLeast(11.2f)), Release(), Until(Grounded()), Until(Dead())), revealedBy: "Skyway"),
                new Betrayal("T3 [W]: a cat that boards Ferry on Press_1's slam is crushed", "Press_1", DeathCause.Hazard,
                    B("board now", L, After(lake, "Until(Glide +"), Until(Beat(PP, L023Layout.Press1Phase + FerryBoard - 45)), Hold(Right), Until(XAtLeast(22f)), Release(), Until(Dead()))),
                new Betrayal("T4 [J]: a cat that jumps up onto Hopper is carried up into Spikes_P", "Spikes_P", DeathCause.Hazard,
                    B("onto Hopper", L, After(lake, "Until(Ferry +"), Hold(Right), Jump(), Until(XAtLeast(35f)), Release(), Until(Grounded()), Until(Dead())), revealedBy: "Hopper"),
                new Betrayal("T5 [J]: a cat that jumps up onto Lambda is carried up into Spikes_L", "Spikes_L", DeathCause.Hazard,
                    B("onto Lambda", L, After(lake, "Until(Glide_2 +"), Hold(Right), Jump(), Until(XAtLeast(88.6f)), Release(), Until(Grounded()), Until(Dead())), revealedBy: "Lambda"),
                new Betrayal("T6 [W]: a cat that boards Glide_2 on Press_2's slam is crushed", "Press_2", DeathCause.Hazard,
                    B("board now", L, After(lake, "Until(Rocket +"), Until(Beat(PP, L023Layout.Press2Phase + Glide2Board - 50)), Hold(Right), Until(XAtLeast(70.5f)), Release(), Until(Dead()))));
        }
    }
}
