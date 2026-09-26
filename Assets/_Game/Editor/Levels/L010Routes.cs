using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 10's anatomy, the exam. T1 (L2, a block) is stood out; T2 (L3) punishes stopping where the
    // hop over it lands; T3 (L6, spikes on a rhythm) is waited out; T4 (L9, L5) is the real flip, which fires an arrow along
    // the roof (wait for it in the alcove); T5 (L8) is the chain T1's landing set off: spikes on the roof, in view since
    // then (jump them); T6 (L2, L1) is the door backing away over a roof section that gives way (jump it). The stair
    // "straight up to the door" and the floating flip (L4's lure) are the dead ends.
    static class L010Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L010 solution",
                // S1: stop for the block, hop it and run on, and wait for the spikes to go down.
                Hold(Left), Until(XAtMost(17.1f)), Release(), Until(Still()), Until(Stopped("Block_1")),
                Hold(Left), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(9.9f)), Release(), Until(Still()), Until(Moving("Spikes_3")),
                // Into the floor flip, up to the roof, and into the alcove until the arrow the flip fired has passed.
                Hold(Left).Timed(TimedMode.Hesitate), Until(GravityUp()), Until(Grounded()),
                Hold(Right), Until(GroundedOn("Alcove_Top")), Release(), Until(Still()), Until(Moving("Arrow_4")), For(8), Until(Stopped("Arrow_4")),
                // Out of the alcove, over the stub and the chain's spikes, and after the door, over the section that gives way.
                Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(7.8f)), Jump(), Until(Airborne()), Until(Grounded()),
                Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(18.3f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Block_1 comes down on a cat that runs on", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead()))),
                new Betrayal("T2: the floor where the hop over the block lands gives way under a cat that stops", "Spikes_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_1))", "stop where it lands", Hold(Left), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "S1_2"),
                new Betrayal("T3: Spikes_3 are up under a cat that runs on", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "run on", Until(Dead()))),
                new Betrayal("T4: the arrow the real flip fired catches a cat that hops out of the alcove at once", "Arrow_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Alcove_Top))", "hop out at once", Jump(), Until(Dead()))),
                new Betrayal("T5: the spikes the block's landing brought up meet a cat that walks on past the stub", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=7.8)", "walk on", Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                new Betrayal("T6: a cat that follows the door onto the roof section falls into the recess", "Recess6_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=18.3)", "follow the door", Until(Dead())), revealedBy: "Roof_6"),
                new Betrayal("Dead end: the stair straight up to the door", "Spikes_D1", DeathCause.Hazard,
                    new Route("up the stair", Hold(Right), Jump(), Until(GroundedOn("Step_A")), Release(), Until(Still()),
                        Hold(Right), Jump(), Until(GroundedOn("Step_B")), Release(), Until(Still()), Hold(Left), Jump(), Until(Fired("Spikes_D1")), Release(), Until(Dead())), revealedBy: "Step_C"),
                new Betrayal("Dead end: the flip floating over S1 sends a cat that jumps into it onto spikes under the slab", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Spikes_3))", "jump into the flip", Hold(Left), Until(XAtMost(7f)), Jump(), Until(Dead()))),
                new Betrayal("Dead end: the floating flip, jumped into at its far edge", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Spikes_3))", "jump into the flip's far edge", Hold(Left), Until(XAtMost(5.9f)), Jump(), Until(Dead()))));
        }
    }
}
