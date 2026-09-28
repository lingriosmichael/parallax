using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 15's routes. The cat waits under cover (a fixed roof), or dances out from under the cloud's
    // locks where there is none (the Run's dance); everywhere else it keeps moving. Waits are on the layout's own clocks: an
    // element's fire tick (the cloud's locks, the spears, Arrow_3, Arrow_9) or its pose (Collapse_2, D3 and Mover_M home or away).
    static class L015Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // Open: jump the gap (landing sets off Arrow_3 and Collapse_2); shelter under Overhang_B; jump Arrow_3 there; when
        // Collapse_2 is back, cross it and hop Post_C.
        static RouteStep[] Open() => new[] {
            Hold(Right), Until(XAtLeast(6.3f)), Jump(), Until(GroundedOn("Floor_B")),
            Until(XAtLeast(10.4f)), Release(), Until(Still()),
            Until(Since("Arrow_3", 18)), Jump().Timed(TimedMode.Shift), Until(Grounded()),
            Until(Home("Collapse_2")), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(15.9f)), Jump(), Until(GroundedOn("Floor_C")),
        };

        // Shelter: past Fake_4 to Overhang_4; wait for Mover_M at home, ride it standing, step off onto Floor_D. Walk on to set
        // off Spear_P, stand on it under Roof_S (setting off Spear_B), wait for the bridge, jump onto it and up onto Floor_E.
        static RouteStep[] Shelter() => new[] {
            Until(XAtLeast(24.2f)), Release(), Until(Still()),
            Until(Home("Mover_M")), Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("Mover_M")), Release(),
            Until(Moving("Mover_M")), Until(Stopped("Mover_M")), Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("Floor_D")),
            Until(XAtLeast(32.4f)), Release(), Until(Still()), Until(Since("Spear_P", 16)),
            Hold(Right), Until(XAtLeast(34.3f)), Release(), Until(Still()), Until(Since("Spear_B", 16)),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(35.3f)), Jump(), Until(GroundedOn("Spear_B_Shaft")), Until(XAtLeast(38.6f)), Jump(), Until(XAtLeast(41f)),
        };

        // The cloud's charges (its locks) come at its wake + 50 + 100 k.
        static RouteCondition Lock(int k) => Since("Cloud", 50 + 100 * k);

        // The solution's dance: the lock at 850 comes over D1 (hop on to D2), the one at 950 over D2 with D3 still gone (hop
        // back to D1); D3 is back before the next.
        static readonly int[] DanceLocks = { 8, 9 };
        // The dead ends reach the dance about 150 ticks later, so D3 comes back later too: a third hop, at the lock at 1050.
        static readonly int[] LateDanceLocks = { 8, 9, 10 };

        // Run: through the cave-in (Block_3 knocks out D3) and onto D1. The dance: at each of `locks`, the cloud has caught up
        // and locks over the cat's platform; hop out from under it to the other one (D1 down to D2, or D2 back up to D1). When
        // D3 is back, on over D2 and D3 onto P1 under Roof_9; when Arrow_9 has stopped, over Stop_9 to P2 and up onto Floor_F1;
        // stop dead there while Spear_10 tells and sticks below it; then down over the shaft, over Curb_F, to the door.
        static RouteStep[] Run(int[] locks)
        {
            var steps = new List<RouteStep> { Until(XAtLeast(45.8f)), Jump(), Until(XAtLeast(47.3f)), Release(), Until(GroundedOn("D1")), Until(Still()) };
            bool onD1 = true;
            foreach (int k in locks)
            {
                steps.Add(Until(Lock(k)));
                steps.AddRange(onD1
                    ? new[] { Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(48.9f)), Release(), Until(GroundedOn("D2")), Until(Still()) }
                    : new[] { Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(48f)), Release(), Until(GroundedOn("D1")), Until(Still()) });
                onD1 = !onD1;
            }
            steps.Add(Until(Home("D3")));
            steps.AddRange(onD1
                ? new[] { Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("D2")), Jump() }
                : new[] { Hold(Right).Timed(TimedMode.Shift), Jump() });
            steps.AddRange(new[] {
                Until(XAtLeast(51.7f)), Release(), Until(GroundedOn("D3")),
                Hold(Right), Jump(), Until(XAtLeast(54.2f)), Release(), Until(GroundedOn("P1")), Until(Still()),
                Until(Since("Arrow_9", 62)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(54.4f)), Jump(), Until(XAtLeast(55.8f)), Release(), Until(GroundedOn("P2")),
                Hold(Right), Until(XAtLeast(56.8f)), Jump(), Until(XAtLeast(58.7f)), Release(), Until(GroundedOn("Floor_F1")),
                Until(Still()), Until(Since("Spear_10", 42)),
                Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(61.6f)), Jump(), Until(RoomComplete()) });
            return steps.ToArray();
        }

        static RouteStep[] Untimed(RouteStep[] steps) => steps.Select(s => s.Timing == TimedMode.None ? s : s.Timed(TimedMode.None)).ToArray();

        public static RoomRoutes Build()
        {
            var solution = new Route("L015 solution", Open().Concat(Shelter()).Concat(Run(DanceLocks)).ToArray());
            Route FromShelter(string name, params RouteStep[] then) =>
                Route.FromSection(solution, "Shelter", name, new[] { Release(), Until(Grounded()), For(5), Hold(Right) }.Concat(then).ToArray());
            Route FromRun(string name, params RouteStep[] then) =>
                Route.FromSection(solution, "Run", name, new[] { Release(), Until(Grounded()), For(5), Hold(Right) }.Concat(then).ToArray());

            return new RoomRoutes(solution,
                new Betrayal("T1 [NW]: a cat that stops at the gap is struck", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=6.3)", "stop at the gap", Release(), Until(Dead())), revealedBy: "Cloud"),
                Betrayal.Recovers("Dead end D1 [LW]: a cat that turns back to the start alcove waits out a strike there, then goes on", "Cloud",
                    new Route("the start alcove", new RouteStep[] { Hold(Right), Until(XAtLeast(5.6f)), Hold(Left), Until(XAtMost(.9f)), Release(), Until(Still()),
                        Until(Since("Cloud", 130)) }.Concat(Untimed(Open())).Concat(Untimed(Shelter())).Concat(Untimed(Run(LateDanceLocks))).ToArray())),
                new Betrayal("T2 [B]: a cat that waits at the edge where the floor went, out from under the overhang, is struck", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "wait at the edge", Hold(Right), Until(XAtLeast(12.4f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T2 [B]: a cat that jumps the arrow and walks on falls where the floor went", "Pit_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "walk on", Hold(Right), Until(Dead())), revealedBy: "Collapse_2"),
                new Betrayal("T3 [J]: a cat that stays down under the overhang is struck by Arrow_3", "Arrow_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Still)", "stay down", Until(Dead()))),
                new Betrayal("T4 [LW]: a cat that shelters under Fake_4 is struck through it", "Cloud", DeathCause.Hazard,
                    FromShelter("the fake overhang", Until(XAtLeast(20.3f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T4b [NJ]: a cat that jumps the mover's pit falls into it", "Pit_M", DeathCause.Hazard,
                    FromShelter("jump the pit", Until(XAtLeast(24.2f)), Release(), Until(Still()), Until(Home("Mover_M")), Hold(Right), Until(XAtLeast(24.6f)), Jump(), Until(Dead())),
                    revealedBy: "Mover_M"),
                Betrayal.Recovers("Dead end D2 [LW]: a cat that stays on Mover_M rides it back and out again, then steps off", "Mover_M",
                    new Route("ride it back", Untimed(Open()).Concat(new RouteStep[] {
                        Until(XAtLeast(24.2f)), Release(), Until(Still()), Until(Home("Mover_M")), Hold(Right), Until(GroundedOn("Mover_M")), Release(),
                        Until(Moving("Mover_M")), Until(Stopped("Mover_M")), Until(Home("Mover_M")), Until(Moving("Mover_M")), Until(Stopped("Mover_M")),
                        Hold(Right), Until(GroundedOn("Floor_D")) }).Concat(Untimed(Shelter().SkipWhile(s => s.Label != "Until(GroundedOn(Floor_D))").Skip(1).ToArray()))
                        .Concat(Untimed(Run(LateDanceLocks))).ToArray())),
                new Betrayal("T5 [NJ]: a cat that jumps for the far side before the bridge falls into Pit_S", "Pit_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Spear_P +16)", "jump early", Hold(Right), Until(XAtLeast(35.3f)), Jump(), Until(Dead())), revealedBy: "Spear_P"),
                new Betrayal("T5b [OL]: a cat that waits on Floor_D's edge instead of the planted spear is struck", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Spear_P +16)", "wait at the edge", Hold(Right), Until(XAtLeast(32.5f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T7 [NW]: a cat that stops under the cave-in is crushed by Block_1", "Block_1", DeathCause.Hazard,
                    FromRun("stop early", Until(XAtLeast(42.9f)), Release(), Until(Dead()))),
                new Betrayal("T7 [NW]: a cat that stops at the cave-in's end is crushed by Block_3", "Block_3", DeathCause.Hazard,
                    FromRun("stop late", Until(XAtLeast(45.3f)), Release(), Until(Dead()))),
                new Betrayal("T11 [NW]: a cat that stays on D2 through the lock is struck", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Cloud +950)", "stay on D2", Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T12 [B]: a cat that dodges the lock forward, where D3 was, falls into Pit_9", "Pit_9", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Cloud +950)", "dodge forward", Hold(Right), Jump(), Until(Dead())), revealedBy: "D3"),
                new Betrayal("T13 [NJ]: a cat that jumps for P1 from D2 while D3 is gone falls into Pit_9", "Pit_9", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(D2))", "jump for P1", Hold(Right), Jump(), Until(Dead())), revealedBy: "D3"),
                new Betrayal("T9 [W]: a cat that goes on from P1 without waiting meets Arrow_9", "Arrow_9", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(P1))", "go on", Hold(Right), Until(XAtLeast(54.4f)), Jump(), Until(XAtLeast(55.8f)), Release(), Until(Dead()))),
                new Betrayal("T10 [SS]: a cat that runs on past the cloud's range, off Floor_F1, meets Spear_10", "Spear_10", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=58.7)", "run on", Until(Dead()))));
        }
    }
}
