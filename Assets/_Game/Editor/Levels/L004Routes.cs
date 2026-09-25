using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 4's anatomy. T1 is jumped; T2, the flip everyone jumps into, is walked under; T3 (spikes on
    // the roof) are jumped upside down; T4 punishes that jump (the landing gives way: jump straight off it); T5 punishes
    // keeping on along the roof (the door backs away over a section that gives way: go back down, along the slab's top,
    // and up under the door). The flip toward the door from the start is the dead end.
    static class L004Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L004 solution",
                Hold(Right), Until(XAtLeast(14.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Under Flip_A, into Flip_R, up through the gap to the roof.
                Until(GravityUp()), Until(GroundedOn("Roof_R")),
                Hold(Left), Until(XAtMost(22.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Roof_4")), Jump(), Until(GroundedOn("Roof_M")),
                // Down through Flip_D to the slab's top, along it, up through Flip_E to the door.
                Jump(), Until(GravityDown()), Until(GroundedOn("Slab")),
                Until(GravityUp()), Until(GroundedOn("Roof_L")), Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Spikes_1 rise under a cat that runs on", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run on", Until(Dead()))),
                new Betrayal("T2: Flip_A sends a cat that jumps into it onto spikes under the slab", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump into the flip", Jump(), Until(Dead()))),
                new Betrayal("T3: spikes on the roof under a cat walking it upside down", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_R))", "walk on", Hold(Left), Until(Dead()))),
                new Betrayal("T4: the roof where the jump lands gives way under a cat that stops", "Recess4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_4))", "stop where it lands", Release(), Until(Dead())), revealedBy: "Roof_4"),
                new Betrayal("T5: a cat that follows the door along the roof falls into the recess", "Recess5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_M))", "follow the door", Until(Dead())), revealedBy: "Roof_5"),
                new Betrayal("Dead end: the flip toward the door drops a cat onto spikes under the slab", "Spikes_L", DeathCause.Hazard,
                    new Route("toward the door", Hold(Left), Until(Dead()))));
        }
    }
}
