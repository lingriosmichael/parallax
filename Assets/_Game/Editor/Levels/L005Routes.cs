using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 5's anatomy. T1 (an arrow along S2) is jumped as it comes; T2 (an arrow across the gap to
    // the high ledge) is avoided by dropping to the low ledge instead; T3 punishes going on from there (its right end
    // gives way: go back and drop off its left end); T4 (an arrow along S1) is waited out standing in the nook; T5 punishes
    // leaving the nook once T4 has passed (wait for the second arrow); T6 is level 1's first floor again. The high ledge
    // near the door is the dead end.
    static class L005Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L005 solution",
                Hold(Left), Until(XAtMost(22.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Off S2's end, down to the low ledge, back left, and down to S1 off its left end.
                Until(XAtMost(5.6f)), Until(Airborne()), Until(GroundedOn("Ledge_Lo")), Release(), Until(Still()),
                Hold(Left), Until(GroundedOn("S1_A")),
                // Into the nook, and wait for both arrows.
                Hold(Right), Until(GroundedOn("Nook")), Release(), Until(Still()), Until(Moving("Arrow_C")), For(8), Until(Stopped("Arrow_C")), Until(Moving("Arrow_D")), For(8), Until(Stopped("Arrow_D")),
                Hold(Right), Jump(), Until(GroundedOn("S1_B")),
                Until(XAtLeast(26.3f)), Jump(), Until(GroundedOn("Ground_2")),
                Hold(Left), Until(XAtMost(6.5f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Arrow_A hits a cat that runs along S2", "Arrow_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead()))),
                new Betrayal("T2: Arrow_B hits a cat that jumps the gap to the high ledge", "Arrow_B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump the gap", Until(XAtMost(6f)), Jump(), Until(Dead()))),
                new Betrayal("T3: the low ledge's right end gives way onto spikes", "Spikes_3b", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ledge_Lo))", "go on to the right", Release(), Until(Still()), Hold(Right), Until(Dead())), revealedBy: "Ledge_Lo2"),
                new Betrayal("T3: the low ledge's right end gives way under a cat that stops on it, onto the nook's spikes", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ledge_Lo))", "stop on the right end", Release(), Until(Still()), Hold(Right), Until(XAtLeast(5.2f)), Release(), Until(Dead())), revealedBy: "Ledge_Lo2"),
                new Betrayal("T4: Arrow_C hits a cat that jumps over the nook", "Arrow_C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_A))", "go on along S1", Hold(Right), Until(XAtLeast(4.2f)), Jump(), Until(Dead()))),
                new Betrayal("T5: Arrow_D hits a cat that leaves the nook once Arrow_C has passed", "Arrow_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_C))", "leave the nook", Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T6: the last floor before the door drops a cat that runs on", "Pit6_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=6.5)", "run on", Until(Dead())), revealedBy: "Floor_6"),
                new Betrayal("Dead end: spikes on the high ledge near the door", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2))", "up to the ledge", Hold(Left), Until(XAtMost(18.5f)), Jump(), Until(Dead()))));
        }
    }
}
