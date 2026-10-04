using System.Linq;
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
                // PAX-102: the opening arrow; stop at once, let it land, walk on through it.
                Hold(Right), Until(XAtLeast(10.1f)), Release(), Until(Still()), Until(Stopped("Arrow_O")), Hold(Right),
                Until(XAtLeast(14.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Under Flip_A, into Flip_R, up through the gap to the roof.
                Until(GravityUp()), Until(GroundedOn("Roof_R2")),
                // PAX-102: wait on Roof_R2 for Arrow_7's shot, then over Sink_R and the lane together.
                Release(), Until(Still()), Until(Moving("Arrow_7")), Until(Stopped("Arrow_7")),
                Hold(Left), Until(XAtMost(22.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Roof_4")), Jump(), Until(GroundedOn("Roof_M")),
                // Down through Flip_D to the slab's top, along it, up through Flip_E to the door.
                Jump(), Until(GravityDown()), Until(GroundedOn("Slab")),
                Until(GravityUp()), Until(GroundedOn("Roof_L")), Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Spikes_1 rise under a cat that runs on", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_O))", "run on", Hold(Right), Until(Dead()))),
                // PAX-102: the opening arrow, the rising roof section and the roof walk's repeating arrow.
                new Betrayal("T6: Arrow_O comes down on a cat that runs on from the start", "Arrow_O", DeathCause.Hazard,
                    new Route("run on", Hold(Right), Until(Dead()))),
                new Betrayal("T7: Sink_R rises into the recess with a cat that stops under it", "RecessC_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", "stop under the section", Hold(Left), Until(XAtMost(27.5f)), Release(), Until(Dead())), revealedBy: "Sink_R"),
                new Betrayal("T8: Arrow_7's shot hits a cat that stops in its lane on the roof", "Arrow_7", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", "stop in the lane", Hold(Left), Until(XAtMost(25.6f)), Release(), Until(Dead())),
                    escape: d => Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", $"step out {d} ticks after the shot",
                        new RouteStep[] { Hold(Left), Until(XAtMost(25.6f)), Release(), Until(Revealed("Arrow_7")) }
                        .Concat(d > 0 ? new[] { For(d) } : System.Array.Empty<RouteStep>())
                        .Concat(new[] { Hold(Left), Until(XAtMost(22.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Roof_4")), Jump(), Until(GroundedOn("Roof_M")),
                            Jump(), Until(GravityDown()), Until(GroundedOn("Slab")), Until(GravityUp()), Until(GroundedOn("Roof_L")), Hold(Right), Until(RoomComplete()) }).ToArray())),
                new Betrayal("T2: Flip_A sends a cat that jumps into it onto spikes under the slab", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump into the flip", Jump(), Until(Dead()))),
                new Betrayal("T3: spikes on the roof under a cat walking it upside down", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_7))", "walk on", Hold(Left), Until(Dead()))),
                new Betrayal("T4: the roof where the jump lands gives way under a cat that stops", "Recess4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_4))", "stop where it lands", Release(), Until(Dead())), revealedBy: "Roof_4"),
                new Betrayal("T5: a cat that follows the door along the roof falls into the recess", "Recess5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_M))", "follow the door", Until(Dead())), revealedBy: "Roof_5"),
                new Betrayal("Dead end: the flip toward the door drops a cat onto spikes under the slab", "Spikes_L", DeathCause.Hazard,
                    new Route("toward the door", Hold(Left), Until(Dead()))),
                // PAX-099 (D-106): the pad the retreated door stands on. A cat that waits in Flip_E comes to rest under it, and
                // walks off either end: left, up to Roof_L and the door; right, up to Roof_5, which starts to give way as the cat
                // runs on over it, along Roof_M to where Roof_4 gave way.
                Betrayal.Recovers("Landing: a cat that waits in Flip_E rests under Door_Pad_L and walks off its left end to the door", "Door",
                    Route.PrefixOf(solution, "Until(GroundedOn(Slab))", "wait under the pad, then left",
                        Until(GravityUp()), Release(), Until(GroundedOn("Door_Pad_L")), Until(Still()), Hold(Left), Until(GroundedOn("Roof_L")), Hold(Right), Until(RoomComplete()))),
                new Betrayal("Landing: a cat that waits in Flip_E rests under Door_Pad_L and walks off its right end along the roof", "Recess4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Slab))", "wait under the pad, then right",
                        Until(GravityUp()), Release(), Until(GroundedOn("Door_Pad_L")), Until(Still()), Hold(Right), Until(Dead())), revealedBy: "Roof_4"));
        }
    }
}
