using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): L001's anatomy. Retreat is non-lethal (KIT-3b); Block_A only catches a cat that
    // hesitates at the spikes (R9): measured, a 32-34 tick hesitation is killed, 0-30 clears it and 36+ lands on it.
    static class L001Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L001 solution",
                Hold(Right), Until(XAtLeast(5.3f)), Jump(), Until(GroundedOn("Platform_B")),
                Until(XAtLeast(11.3f)), Jump(), Until(GroundedOn("Floor_C")),
                Until(XAtLeast(17.3f)), Jump(), Until(GroundedOn("Floor_D")),
                // D-056 (1): Spikes_A starts the race against Block_A; a hesitation before the spike jump is measured from rest.
                Jump().Timed(TimedMode.Hesitate), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Collapse_C drops a cat that walks onto it", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_C))", "walk onto Collapse_C", Until(XAtLeast(19f)), Release(), Until(Dead())),
                    revealedBy: "Collapse_C"),
                new Betrayal("Spikes_A rise under a cat that runs on", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_D))", "run into Spikes_A", Until(Dead()))),
                new Betrayal("Block_A falls on a cat that hesitates at the spikes", "Block_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_D))", "hesitate before the spikes", Release(), For(33), Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
