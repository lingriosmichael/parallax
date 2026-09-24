using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): L001's anatomy. Retreat is non-lethal (KIT-3b); Block_A only catches a cat that
    // hesitates at the spikes (R9). PAX-082 (D-082), from x 23 with the lower jump: 0-28 clears it, 30-38 is killed,
    // 40 dies on the spikes, 42+ clears.
    static class L001Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L001 solution",
                Hold(Right), Until(XAtLeast(5.3f)), Jump(), Until(GroundedOn("Platform_B")),
                Until(XAtLeast(11.3f)), Jump(), Until(GroundedOn("Floor_C")),
                Until(XAtLeast(17.3f)), Jump(), Until(GroundedOn("Floor_D")),
                // PAX-082 (D-082): the lower jump lands at x ~21.4, short of the spikes; run on to x 23 before the spike jump.
                Until(XAtLeast(23f)),
                // D-056 (1): Spikes_A starts the race against Block_A; a hesitation before the spike jump is measured from rest.
                Jump().Timed(TimedMode.Hesitate), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Collapse_C drops a cat that walks onto it", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_C))", "walk onto Collapse_C", Until(XAtLeast(19f)), Release(), Until(Dead())),
                    revealedBy: "Collapse_C"),
                new Betrayal("Spikes_A rise under a cat that runs on", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_D))", "run into Spikes_A", Until(Dead()))),
                new Betrayal("Block_A falls on a cat that hesitates at the spikes", "Block_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=23)", "hesitate before the spikes", Release(), For(34), Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
