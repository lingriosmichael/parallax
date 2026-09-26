using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 9's anatomy. T1 (L4's lure, a floating flip) is not jumped into; T2 punishes L4's answer,
    // walking under it: an arrow comes along S1 from behind (wait for it in the nook); T3 (spikes on S1) is jumped; T4 is
    // the real flip, which fires an arrow along the roof behind the cat walking back (jump it, upside down); T5 punishes
    // stopping where the hop over the stub lands. The flip in front of the start and the ledge behind it, "straight up to the door", are the dead ends.
    static class L009Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L009 solution",
                // S1: hop the post, walk under the floating flip, wait in the nook for the arrow it sent, and jump the spikes.
                Hold(Right), Until(XAtLeast(6.3f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(GroundedOn("Nook")), Release(), Until(Still()), Until(Moving("Arrow_2")), For(8), Until(Stopped("Arrow_2")),
                Hold(Right), Jump(), Until(GroundedOn("S1_B")),
                Until(XAtLeast(20.3f)), Jump(), Until(Airborne()), Until(Grounded()),
                // Up the floor flip to the roof by the wall, and back left upside down: jump the arrow it sent, hop the stub
                // where it stopped, and on to the door.
                Until(GravityUp()), Until(Grounded()),
                Hold(Left), Until(XAtMost(29f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(24.2f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: the floating flip sends a cat that jumps into it onto spikes under the slab", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump into the flip", Jump(), Until(Dead()))),
                new Betrayal("T1: the floating flip sends a cat that jumps into it at its far edge onto spikes under the slab", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump into the flip's far edge", Until(XAtLeast(10.6f)), Jump(), Until(Dead()))),
                new Betrayal("T2: Arrow_2 catches a cat that walked under the floating flip and goes on", "Arrow_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Nook))", "hop out at once", Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T3: Spikes_3 come up under a cat that runs on", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_B))", "run on", Until(Dead()))),
                new Betrayal("T4: the arrow the real flip fired meets a cat that walks the roof back without jumping it", "Arrow_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=29)", "walk on", Until(Dead()))),
                new Betrayal("T5: the roof where the hop over the stub lands gives way under a cat that stops", "Recess5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=24.2)", "stop where it lands", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Roof_5"),
                new Betrayal("Dead end: the flip floating in front of the start, straight up to the door", "Spikes_D1", DeathCause.Hazard,
                    new Route("straight up", Hold(Right), Until(XAtLeast(4f)), Jump(), Until(Dead()))),
                new Betrayal("Dead end: the ledge behind the start, the first step up to the door", "Spikes_D2", DeathCause.Hazard,
                    new Route("up the ledge", Hold(Left), Jump(), Until(Fired("Spikes_D2")), Release(), Until(Dead())), revealedBy: "Ledge_D2"),
                new Betrayal("Dead end: the flip floating in front of the start, jumped into at its far edge", "Spikes_D1", DeathCause.Hazard,
                    new Route("straight up", Hold(Right), Until(XAtLeast(4.6f)), Jump(), Until(Dead()))));
        }
    }
}
