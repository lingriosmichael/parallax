using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): L004's anatomy. The solution is PAX-078's Flip_A route. FalseLanding and Block_1 are
    // PAX-080's (§8, §11 R19 (c)); Block_2 never kills and the floor run is gone (R9).
    static class L004Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L004 solution",
                Hold(Right), Until(XAtLeast(3.4f)), Jump(), Until(GroundedOn("Platform_B")),
                Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("Platform_C")),
                Until(XAtLeast(15.4f)), Release(), Until(Still()),
                // The learned braked landing: from rest, onto FalseLanding's left end, below its trigger.
                Hold(Right), Jump(), Until(XAtLeast(18.9f)), Release(), Until(GroundedOn("FalseLanding")),
                Hold(Right), Until(XAtLeast(22.5f)), Jump().Timed(TimedMode.Shift), Until(GravityUp()), Until(GroundedOn("Ceiling")),
                Until(XAtLeast(27.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Ceiling").And(XAtLeast(29.8f))),
                Until(XAtLeast(30.9f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("SourceSpikes rise under a cat that runs on", "SourceSpikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(FalseLanding))", "run into SourceSpikes", Hold(Right), Until(Dead()))),
                new Betrayal("CeilingHiddenSpikes drop on a cat that walks the ceiling", "CeilingHiddenSpikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ceiling))", "walk into CeilingHiddenSpikes", Until(Dead()))));
        }
    }
}
