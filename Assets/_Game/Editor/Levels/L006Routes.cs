using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 6's anatomy. T1 (spikes on a rhythm) is waited out; T2 punishes waiting a cycle too long (go
    // on the first gap); T4 punishes stopping on the floor past the lift; T5 and T6 are floors that aren't there. Leaving
    // S1 by its right end, toward the door, is the dead end. PAX-107 (D-124): the way down from S1's west end is the climb
    // (the chimney, the shelf, the plinth, the slot) onto Lift_G, which waits 2 s and carries the cat over Spikes_G.
    static class L006Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L006 solution",
                // Left to the spikes, wait for them to go down, and go.
                Hold(Left), Until(XAtMost(13f)), Release(), Until(Still()), Until(Moving("Spikes_1")),
                // PAX-107 (D-124): off S1's west end onto Pillar_Moss, wall-jump to Hang_Moss and back over the pillar, down onto
                // the shelf, off its end onto the plinth, west under the shelf and down the slot onto Lift_G; wait for it to rise and carry the cat over
                // Spikes_G, then off its end over Floor_4.
                Hold(Left).Timed(TimedMode.Hesitate), Until(XAtMost(7f)), Jump(), Until(ClingingLeft()),
                Release(), Jump(), For(2), Hold(Right), Until(ClingingRight()),
                Release(), Jump(), For(2), Hold(Left), Until(GroundedOn("Shelf")),
                Hold(Right), Until(GroundedOn("Plinth")), Hold(Left), Until(GroundedOn("Lift_G")), Release(), Until(Still()),
                Until(Moving("Lift_G")), Until(Stopped("Lift_G")),
                Hold(Right),
                // D-119: on past the landing to Pit_7's edge, wait for Ride_7 to come home, ride it across, and from its far end
                // jump the hinge floor (it swings up behind the cat and pushes): straight on over the floor that isn't there.
                Until(XAtLeast(9.6f)), Release(), Until(Still()), Until(Moving("Ride_7")), Until(Home("Ride_7")),
                Hold(Right), Until(XAtLeast(11.4f)), Release(), Until(Stopped("Ride_7")),
                Hold(Right), Until(XAtLeast(14.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Then (PAX-100, D-106) wait for Ride_6 to come home, board it, ride it across Pit_6 and step off onto the door's floor.
                Until(XAtLeast(19.1f)), Jump(), Until(Airborne()), Until(Grounded()),
                Release(), Until(Still()), Until(Moving("Ride_6")), Until(Home("Ride_6")),
                Hold(Right), Until(GroundedOn("Ride_6").And(XAtLeast(25.5f))), Release(), Until(Still()),
                Until(XAtLeast(28.6f)), Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Spikes_1 are up under a cat that runs on", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead()))),
                new Betrayal("T2: Block_2 comes down on a cat that waits a cycle too long", "Block_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Still)", "keep waiting", Until(Dead()))),
                // PAX-107 (D-124): Floor_4 is where the lift's ride ends.
                new Betrayal("T4: the floor past the lift gives way under a cat that stops on it", "Pit4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Lift_G))", "step off and stop", Hold(Right), Until(Airborne()), Release(), Until(Dead())), revealedBy: "Floor_4"),
                // D-119: the rider and the hinge floor.
                new Betrayal("T7: a cat that walks on without waiting for Ride_7 falls into Pit_7", "Pit7_Hazard", DeathCause.Hazard,
                    // PAX-107 (D-124): the lift brings the cat here as Ride_7 comes home; walk off while it's away.
                    Route.PrefixOf(solution, "Until(Moving(Ride_7))", "walk off while it's away", For(20), Hold(Right), Until(Airborne()), Release(), Until(Dead())), revealedBy: "Ride_7"),
                new Betrayal("T8: the hinge floor's wall pushes a cat that stops past it onto Floor_5", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=14.6)", "stop past it", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Flip_8"),
                new Betrayal("T5: the next floor gives way under a cat that runs on", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=19.1)", "run on", Until(Dead())), revealedBy: "Floor_5"),
                new Betrayal("T6: a cat that runs on without waiting for Ride_6 falls into Pit_6", "Pit6_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=19.1)", "run on", Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Ride_6"),
                new Betrayal("Dead end: the jump off S1's end toward the door lands on the lid", "Spikes_L", DeathCause.Hazard,
                    new Route("toward the door", Hold(Right), Until(XAtLeast(19.3f)), Jump(), Until(Dead()))),
                // D-126: the shortcut down off S1's west end. Invert_Drop swaps the cat's left and right below S1's top: pressing right
                // carries it onto Spikes_G; steering right anyway (holding left) still lands it on Spikes_G's end (S1_Lip).
                new Betrayal("Dead end: dropping off S1's west end inverts the cat; pressing right carries it onto Spikes_G", "Spikes_G", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Spikes_1))", "drop off the west end", Hold(Left), Until(Airborne()), Hold(Right), Until(Dead())), revealedBy: "Invert_Drop"),
                new Betrayal("Dead end: an inverted cat that steers right in the drop still lands on Spikes_G", "Spikes_G", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Spikes_1))", "drop and steer right", Hold(Left), Until(Airborne()), Hold(Right), Until(Fired("Invert_Drop")), Hold(Left), Until(Dead())), revealedBy: "Invert_Drop"),
                // PAX-107 (D-124): a cat stepping off S1's end drops through the gap before the lid onto Floor_5, which gives way.
                new Betrayal("Dead end: stepping off S1's end drops the cat through the gap onto Floor_5", "Pit5_Hazard", DeathCause.Hazard,
                    new Route("step off", Hold(Right), Until(Airborne()), Release(), Until(Dead())), revealedBy: "Floor_5"),
                new Betrayal("Dead end: running off S1's end carries the cat onto the lid", "Spikes_L", DeathCause.Hazard,
                    new Route("run off", Hold(Right), Until(Dead()))));
        }
    }
}
