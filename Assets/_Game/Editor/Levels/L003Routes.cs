using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): L003's anatomy. PeriodicUp is honest (a timed pass, not a betrayal) and Retreat is
    // non-lethal (KIT-3b), so neither has a betrayal route (R9).
    static class L003Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L003 solution",
                Hold(Right), Until(XAtLeast(3.9f)), Jump(), Until(GroundedOn("Platform_B")),
                Until(XAtLeast(9f)), Jump(), Until(GroundedOn("Floor_Pre")),
                Until(XAtLeast(15.4f)), Jump(), Until(GravityUp()), Until(GroundedOn("Ceiling")),
                Release(), Until(Still()), Until(Moving("PeriodicUp")), Until(Home("PeriodicUp")),
                // D-056 (1): the pass under PeriodicUp, measured from rest.
                // A braked jump over CeilingSpikes lands on the ceiling short of Flip_B; the drop back down enters
                // Flip_B's left side moving left, so the cat lands past ExitSpikes at the retreated door (D-060).
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(26.1f)), Jump(), Until(XAtLeast(28.9f)), Release(), Until(GroundedOn("Ceiling")),
                Hold(Right), Until(XAtLeast(30.3f)), Release(), Until(Still()), Hold(Left), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Collapse_C drops a cat that walks onto it", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_Pre))", "walk onto Collapse_C", Until(XAtLeast(16.5f)), Release(), Until(Dead())),
                    revealedBy: "Collapse_C"),
                new Betrayal("CeilingSpikes drop on a cat that walks on", "CeilingSpikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(PeriodicUp))", "walk into CeilingSpikes", Hold(Right), Until(Dead()))),
                new Betrayal("ExitSpikes rise under a cat that drops straight down", "ExitSpikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=26.1)", "straight drop", Jump(), Until(GravityDown()), Release(), Until(Dead()))));
        }
    }
}
