using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 5's anatomy. T1 (an arrow along S2) is jumped as it comes; T2 (an arrow across the gap to
    // the high ledge) is avoided by dropping to S1 instead (T3, the low ledge, removed 2026-10-18); T4 (an arrow along S1)
    // is waited out standing in the nook; T5 punishes leaving the nook once T4 has passed (wait for the second arrow); T6
    // is level 1's first floor again. The high ledge near the door is the dead end.
    static class L005Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L005 solution",
                // D-116: Arrow_A's first shot comes as the cat sets off along S2: jump it. D-156: then stop right of the roof's
                // vertical lanes, let a volley land (seen in flight first: an arrow's tell holds still), and walk under all three.
                // (Not re-validated: the developer tests this one by hand.)
                Hold(Left), Until(XAtMost(26.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(XAtMost(21.2f)), Release(), Until(Still()),
                Until(Moving("Arrow_V1")), Until(Stopped("Arrow_V1")), Hold(Left),
                // Off S2's end, down past the high ledge to S1 (2026-10-18: the low ledge is gone).
                Until(XAtMost(5.6f)), Until(Airborne()), Until(GroundedOn("S1_A")),
                // Into the nook, and wait for both arrows.
                Hold(Right), Until(GroundedOn("Nook")), Release(), Until(Still()), Until(Moving("Arrow_C")), For(8), Until(Stopped("Arrow_C")), Until(Moving("Arrow_D")), For(8), Until(Stopped("Arrow_D")),
                Hold(Right), Jump(), Until(GroundedOn("S1_B1")),
                // D-119: S1's holes, each on its rider: wait for it at the hole, step on as it comes home, ride it out, step off.
                // Arrow_C's next shot comes up behind as the cat reaches the first hole, its rider home: jump onto the rider over it.
                Until(XAtLeast(8.6f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Ride_S1")), Release(), Until(Stopped("Ride_S1")),
                // (walk-ons are positions: which collider a cat on a seam stands on varies between replays)
                Hold(Right), Until(XAtLeast(16.4f)), Release(), Until(Stopped("Ride_S2")),
                Hold(Right), Until(GroundedOn("S1_B3")),
                Until(XAtLeast(26.3f)), Jump(), Until(GroundedOn("Ground_2d")), Release(), Until(Still()),
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
                // D-156: the roof's vertical arrows.
                new Betrayal("T1b: a vertical arrow hits a cat that stops under it", "Arrow_V2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_V1))", "stop under the middle lane", Hold(Left), Until(XAtMost(17.1f)), Release(), Until(Dead()))),
                new Betrayal("T2: Arrow_B hits a cat that jumps the gap to the high ledge", "Arrow_B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_V1))", "jump the gap", Hold(Left), Until(XAtMost(6f)), Jump(), Until(Dead()))),
                new Betrayal("T4: Arrow_C hits a cat that jumps over the nook", "Arrow_C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_A))", "go on along S1", Hold(Right), Until(XAtLeast(4.2f)), Jump(), Until(Dead()))),
                new Betrayal("T5: Arrow_D hits a cat that leaves the nook once Arrow_C has passed", "Arrow_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_C))", "leave the nook", Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T6: the last floor before the door drops a cat that runs on", "Pit6_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=6.5)", "run on", Until(Dead())), revealedBy: "Floor_6"),
                // D-116: the crush ledge.
                new Betrayal("T7: spikes come up on the ledge's end under a cat that runs on along it", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ledge_D))", "run on", Until(Dead()))),
                new Betrayal("Dead end: the floor under the ledge rises into a cat that walks under it", "Rise_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2d))", "under the ledge", Hold(Left), Until(Dead()))));
        }
    }
}
