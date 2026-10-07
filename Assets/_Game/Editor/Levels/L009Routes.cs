using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 9's anatomy. T1 (L4's lure, a floating flip) is not jumped into; T4 is the real flip; T5
    // punishes stopping on the roof between the lifts. The flip in front of the start and the ledge behind it, "straight up
    // to the door", are the dead ends.
    // PAX-107 (the developer: "The level is too easy … level 9 doesnt need any arrow launcher"): the arrows are gone. The way
    // waits out F1 and F2 under the floating flip, crosses the chasm on Lift_A and Lift_B while each is down, waits out F3 on
    // S1_E1 and goes over the hinge floor and F3 into Flip_4 without stopping; back on the roof it waits out each floor
    // strip's mirror (up while the floor's is down) and the spikes over Lift_B, and crosses the lifts and Roof_5 in one go.
    static class L009Routes
    {
        // A spike strip on the rhythm, waited out: its next change, then down.
        static RouteStep[] WaitOut(string strip) => new[] { Until(Moving(strip)), For(2), Until(Home(strip)) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L009 solution", Concat(
                // S1: F1 is up from the start: stop short of it, cross as it drops; F2 is up by then: wait under the flip.
                new[] { Hold(Right), Until(XAtLeast(7.4f)), Release(), Until(Still()) }, WaitOut("Spikes_F1"),
                new[] { Hold(Right), Until(XAtLeast(11.2f)), Release(), Until(Still()) }, WaitOut("Spikes_F2"),
                // The chasm: to its edge, onto Lift_A as it comes home, to Lift_B as it comes home, off it onto S1_E1.
                new[] { Hold(Right), Until(XAtLeast(14.3f)), Release(), Until(Still()), Until(Home("Lift_A")),
                    Hold(Right), Jump(), Until(XAtLeast(16.3f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Lift_A")), Until(Still()), Until(Home("Lift_B")),
                    Hold(Right), Jump(), Until(GroundedOn("Lift_B")), Jump(), Until(XAtLeast(22.6f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("S1_E1")), Until(Still()) },
                // F3 is up: wait it out at the hinge floor's east end (it only goes for a cat past it), then over F3 into Flip_4
                // and up to the roof, quick enough that R3 is still up there.
                new[] { Hold(Right), Until(XAtLeast(25.7f)), Release(), Until(Still()) }, WaitOut("Spikes_F3"),
                new[] { Hold(Right), Until(GravityUp()), Until(Grounded()), Release(), Until(Still()) },
                // The roof: R3 (F3's mirror) is up: wait it out, then stop short of Lift_B's column for the spikes over it.
                WaitOut("Spikes_R3"),
                new[] { Hold(Left), Until(XAtMost(22.8f)), Release(), Until(Still()) }, WaitOut("Spikes_LB"), new[] {
                    // Over Lift_B, Roof_5, Lift_A and R2 (down by then) in one go, to short of R1.
                    Hold(Left), Until(XAtMost(11f)), Release(), Until(Still()) },
                WaitOut("Spikes_R1"),
                // Over R1 into Flip_E, down onto Slab_D, and to the door standing.
                new[] { Hold(Left), Until(GravityDown()), Until(GroundedOn("Slab_D")), Until(RoomComplete()) }));

            return new RoomRoutes(solution,
                new Betrayal("T1: the floating flip sends a cat that jumps F1 onto spikes under the slab", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=7.4)", "jump F1", Jump(), Until(Dead()))),
                new Betrayal("T1: the floating flip sends a cat that jumps F2 onto spikes under the slab", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=11.2)", "jump F2 from where it waits", Release(), Until(Still()), Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("M1: F1 is up under a cat that runs on from the start", "Spikes_F1", DeathCause.Hazard,
                    new Route("run on", Hold(Right), Until(Dead()))),
                new Betrayal("M2: F2 comes up under a cat that runs on past F1", "Spikes_F2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=11.2)", "run on", Until(Dead()))),
                new Betrayal("L1: Lift_A carries a cat that stays on it up into the roof's spikes", "Spikes_LA", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Lift_A))", "stay on", Release(), Until(Dead()))),
                new Betrayal("L2: Lift_B carries a cat that stays on it up into the roof's spikes", "Spikes_LB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Lift_B))", "stay on", Release(), Until(Dead()))),
                new Betrayal("L3: a cat that jumps for Lift_A before it is down falls into the chasm", "Chasm_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=14.3)", "jump at once", Jump(), Until(Dead())), revealedBy: "Lift_A"),
                new Betrayal("H: the hinge floor pushes a cat that stops past it onto F3", "Spikes_F3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Spikes_F3))", "stop past the hinge floor", Hold(Right), Until(XAtLeast(27.2f)), Release(), Until(Dead())), revealedBy: "Hinge_9"),
                new Betrayal("M3: R3 is up over a cat that walks the roof back at once", "Spikes_R3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GravityUp)", "walk back at once", Until(Grounded()), Hold(Left), Until(Dead()))),
                new Betrayal("L4: the spikes over Lift_B come out under a cat that stops on the roof over it", "Spikes_LB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=22.8)", "stop over the lift", Hold(Left), Until(XAtMost(20.75f)), Release(), Until(Dead())), revealedBy: "Lift_B"),
                new Betrayal("T5: the roof between the lifts gives way under a cat that waits there", "Recess5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Spikes_LB))", "wait between the lifts", Hold(Left), Until(XAtMost(18.75f)), Release(), Until(Dead())), revealedBy: "Roof_5"),
                new Betrayal("M5: R1 is up over a cat that runs on to Flip_E", "Spikes_R1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=11)", "run on", Until(Dead()))),
                new Betrayal("Dead end: the flip floating in front of the start, straight up to the door", "Spikes_D1", DeathCause.Hazard,
                    new Route("straight up", Hold(Right), Until(XAtLeast(4f)), Jump(), Until(Dead()))),
                new Betrayal("Dead end: the ledge behind the start, the first step up to the door", "Spikes_D2", DeathCause.Hazard,
                    new Route("up the ledge", Hold(Left), Jump(), Until(Fired("Spikes_D2")), Release(), Until(Dead())), revealedBy: "Ledge_D2"),
                new Betrayal("Dead end: the flip floating in front of the start, jumped into at its far edge", "Spikes_D1", DeathCause.Hazard,
                    new Route("straight up", Hold(Right), Until(XAtLeast(4.6f)), Jump(), Until(Dead()))),
                // PAX-099 ruling 3 (the room auditor's open question): the same flip entered moving left, from a run-up.
                new Betrayal("Dead end: the flip floating in front of the start, jumped into moving left", "Spikes_D1", DeathCause.Hazard,
                    new Route("straight up, moving left", Hold(Right), Until(XAtLeast(6.4f)), Release(), Until(Still()),
                        Hold(Left), Until(XAtMost(6f)), Jump(), Until(Dead()))));
        }

        static RouteStep[] Concat(params RouteStep[][] parts) => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(parts, p => p));
    }
}
