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
                // D-116: Arrow_A's first shot comes as the cat sets off along S2: jump it. Then stop short of the corbel's new
                // lane, let its shot land, and walk under the corbel before the drip.
                Hold(Left), Until(XAtMost(26.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(XAtMost(20.6f)), Release(), Until(Still()),
                Until(Home("Arrow_X")), For(2), Until(Moving("Arrow_X")), For(8), Until(Stopped("Arrow_X")), Hold(Left),
                // Off S2's end, down to the low ledge, back left, and down to S1 off its left end.
                Until(XAtMost(5.6f)), Until(Airborne()), Until(GroundedOn("Ledge_Lo")), Release(), Until(Still()),
                Hold(Left), Until(GroundedOn("S1_A")),
                // Into the nook, and wait for both arrows.
                Hold(Right), Until(GroundedOn("Nook")), Release(), Until(Still()), Until(Moving("Arrow_C")), For(8), Until(Stopped("Arrow_C")), Until(Moving("Arrow_D")), For(8), Until(Stopped("Arrow_D")),
                Hold(Right), Jump(), Until(GroundedOn("S1_B1")),
                // D-119: S1's holes, each on its rider: wait for it at the hole, step on as it comes home, ride it out, step off.
                // Arrow_C's next shot comes up behind as the cat reaches the first hole, its rider home: jump onto the rider over it.
                Until(XAtLeast(8.6f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Ride_S1")), Release(), Until(Stopped("Ride_S1")),
                // (walk-ons are positions: which collider a cat on a seam stands on varies between replays)
                Hold(Right), Until(XAtLeast(16.4f)), Release(), Until(Stopped("Ride_S2")),
                Hold(Right), Until(GroundedOn("S1_B3")),
                Until(XAtLeast(26.3f)), Jump(), Until(GroundedOn("Ground_2c")), Release(), Until(Still()),
                // D-116: onto the crush ledge, left along it, and over its spikes off its end.
                Hold(Left), Until(XAtMost(30.4f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Ledge_D")), Until(XAtMost(25.9f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Ground_2c")),
                // D-119: the ground's pits, each on its rider.
                Until(XAtMost(19.1f)), Release(), Until(Stopped("Ride_G2")),
                Hold(Left), Until(XAtMost(13.1f)), Release(), Until(Stopped("Ride_G1")),
                Hold(Left), Until(GroundedOn("Ground_2a")),
                Until(XAtMost(6.5f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Arrow_A hits a cat that runs along S2", "Arrow_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead()))),
                new Betrayal("T1b: the corbel's next arrow hits a cat that stops under it", "Arrow_Drip", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_X))", "stop under the corbel", Hold(Left), Until(XAtMost(14.8f)), Release(), Until(Dead()))),
                // D-116: the corbel's second launcher.
                new Betrayal("T1c: the corbel's 45-degree arrow hits a cat that stops in its lane", "Arrow_X", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stop in the lane", Hold(Left), Until(XAtMost(18.2f)), Release(), Until(Dead()))),
                new Betrayal("T2: Arrow_B hits a cat that jumps the gap to the high ledge", "Arrow_B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_X))", "jump the gap", Hold(Left), Until(XAtMost(6f)), Jump(), Until(Dead()))),
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
                // D-116: the crush ledge.
                new Betrayal("T7: spikes come up on the ledge's end under a cat that runs on along it", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ledge_D))", "run on", Until(Dead()))),
                new Betrayal("Dead end: the ledge comes down on a cat that walks under it", "Ledge_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2c))", "under the ledge", Hold(Left), Until(Dead()))));
        }
    }
}
