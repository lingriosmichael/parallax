using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 6's anatomy. T1 (spikes on a rhythm) is waited out; T2 punishes waiting a cycle too long (go
    // on the first gap); T3 (the sweep) is stood out by the wall; T4 punishes stopping after the post, as T3 taught; T5 and
    // T6 are floors that aren't there. Leaving S1 by its right end, toward the door, is the dead end.
    static class L006Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L006 solution",
                // Left to the spikes, wait for them to go down, and go.
                Hold(Left), Until(XAtMost(13f)), Release(), Until(Still()), Until(Moving("Spikes_1")),
                Hold(Left).Timed(TimedMode.Hesitate), Until(XAtMost(6.4f)), Until(Airborne()), Until(Grounded()),
                // Down by the wall: landing sets off the sweep; stand still while it comes out and goes home, then over the post.
                Release(), Until(Still()), Until(Moving("Sweep_3")), Until(Home("Sweep_3")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(3.4f)), Jump(), Until(Airborne()), Until(Grounded()),
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
                new Betrayal("T3: Sweep_3 hits a cat that goes on as it lands", "Sweep_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "go on", Hold(Right), Until(Dead()))),
                new Betrayal("T4: the landing past the post gives way under a cat that stops", "Pit4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=3.4)", "stop past the post", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Floor_4"),
                // D-119: the rider and the hinge floor.
                new Betrayal("T7: a cat that walks on without waiting for Ride_7 falls into Pit_7", "Pit7_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.6)", "walk on", Until(Dead())), revealedBy: "Ride_7"),
                new Betrayal("T8: the hinge floor's wall pushes a cat that stops past it onto Floor_5", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=14.6)", "stop past it", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Flip_8"),
                new Betrayal("T5: the next floor gives way under a cat that runs on", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=19.1)", "run on", Until(Dead())), revealedBy: "Floor_5"),
                new Betrayal("T6: a cat that runs on without waiting for Ride_6 falls into Pit_6", "Pit6_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=19.1)", "run on", Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Ride_6"),
                new Betrayal("Dead end: the jump off S1's end toward the door lands on the lid", "Spikes_L", DeathCause.Hazard,
                    new Route("toward the door", Hold(Right), Until(XAtLeast(19.3f)), Jump(), Until(Dead()))),
                // PAX-107: the lid now reaches x 20.5, so a cat stepping off S1's end comes down on its spikes.
                new Betrayal("Dead end: stepping off S1's end drops the cat onto the lid's spikes", "Spikes_L", DeathCause.Hazard,
                    new Route("step off", Hold(Right), Until(Airborne()), Release(), Until(Dead()))),
                new Betrayal("Dead end: running off S1's end carries the cat onto the lid", "Spikes_L", DeathCause.Hazard,
                    new Route("run off", Hold(Right), Until(Dead()))));
        }
    }
}
