using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): Trap Lab room 3 (the arrows, D-078). Not a level, so not in LevelRoutes.
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
    }
}
