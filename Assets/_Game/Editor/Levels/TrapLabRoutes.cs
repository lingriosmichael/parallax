using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): Trap Lab room 3 (the arrows, D-078). Not a level, so not in LevelRoutes.
    // PAX-080 (D-080): Trap Lab room 4, the troll-route room.
    public static class TrapLabRoutes
    {
        public static RoomRoutes Room3()
        {
            var solution = new Route("Trap Lab room 3 solution",
                Hold(Right), Until(XAtLeast(6.2f)), Release(), Until(Still()),
                Until(Stopped("ArrowA")),
                // D-056 (1): the crossing after ArrowA has stopped, measured from rest.
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(8.5f))),
                Until(XAtLeast(13.2f)), Jump(), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(14.6f))),
                Until(XAtLeast(16.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(17.6f))),
                Until(XAtLeast(21f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("ArrowB fires at the shins of a cat that runs past x 17", "ArrowB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor) && X>=14.6)", "run past ArrowB's trigger", Until(Dead()))),
                new Betrayal("ArrowC fires at the head of a cat that jumps into its band", "ArrowC", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor) && X>=14.6)", "jump into ArrowC's band", Until(XAtLeast(17.4f)), Jump(), Until(Dead()))));
        }

        public static RoomRoutes Room4()
        {
            // Jump before Stone_A's touch skin, clear Thin_Collapse, and leave Up_2 before Ledge_End's skin.
            var solution = new Route("Trap Lab room 4 solution",
                Hold(Right), Until(XAtLeast(4f)), Jump(), Until(GroundedOn("Up_1")),
                Until(XAtLeast(9.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Up_2")),
                Until(XAtLeast(14.8f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Stone_A drops a cat that walks on off the start floor", "Pit_Hazard", DeathCause.Hazard,
                    new Route("walk onto Stone_A", Hold(Right), Until(Dead())), revealedBy: "Stone_A"),
                new Betrayal("Thin_Collapse drops a cat that stops on it", "Pit_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_1))", "hop onto Thin_Collapse",
                        Until(XAtLeast(9.4f)), Jump(), Until(XAtLeast(10.9f)), Release(), Until(GroundedOn("Thin_Collapse")), Until(Dead())),
                    revealedBy: "Thin_Collapse"),
                Betrayal.Recovers("Ledge_End drops a cat that walks on off Up_2 into the Gutter, which climbs back to Up_2", "Ledge_End",
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_2))", "step off Up_2 onto Ledge_End, then climb back",
                        Until(GroundedOn("Gutter")), Release(), Until(Still()),
                        Hold(Left), Until(XAtMost(17.9f)), Jump(), Until(XAtMost(15.5f)), Release(), Until(GroundedOn("Up_2")), Until(Still()),
                        Hold(Right), Jump(), Until(RoomComplete()))),
                new Betrayal("The Bridge goes after ArrowD fires over a cat that runs on from the Gutter", "Pit_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_2))", "run on over the Bridge", Until(GroundedOn("Gutter")), Until(Dead())),
                    revealedBy: "ArrowD"));
        }
    }
}
