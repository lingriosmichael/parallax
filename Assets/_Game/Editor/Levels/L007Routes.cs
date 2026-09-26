using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 7's anatomy. T1 (the floor one step ahead drops) is jumped; T2 punishes stopping where that
    // jump lands; T4 (an S1 section that gives way) is jumped, as L3 taught; T5 punishes jumping the next stretch too
    // (walk under the overhang); T6 is T1 again, escalated: the jump T1 taught lands on spikes that come up (wait for them
    // to go, then jump); T3 reverses L3: the step onto S2's end gives way, the wall step is real. Each tower's step at the
    // wall past its storey is a dead end.
    static class L007Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L007 solution",
                // Left along the ground: jump the floor that drops, and run on off the one that lands you.
                Hold(Left), Until(Fired("Spikes_1")), Until(XAtMost(27.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                // Up the left tower to S1.
                Until(XAtMost(6.4f)), Jump(), Until(GroundedOn("Tread_LA")), Release(), Until(Still()),
                Hold(Left), Jump(), Until(GroundedOn("Tread_LB")), Release(), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("Tread_LC")), Release(), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("S1_A")),
                // Right along S1: jump T4, walk under the overhang, wait for the spikes past T6's gap to come and go, and jump it.
                Until(XAtLeast(9.1f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(18f)), Release(), Until(Still()), Until(Moving("Spikes_6b")), Until(Home("Spikes_6b")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(18.9f)), Jump(), Until(Airborne()), Until(GroundedOn("S1_D")),
                Until(XAtLeast(26.55f)), Release().Timed(TimedMode.Shift), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("Tread_RA")), Release(), Until(Still()),
                // Up the right tower (L3's left tower, mirrored) and on up the wall steps, and left along S2 to the door.
                Hold(Left), Until(XAtMost(29.2f)), Release().Timed(TimedMode.Shift), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("Tread_RB")), Release(), Until(Still()),
                Hold(Left), Jump(), Until(GroundedOn("Tread_RC")), Release(), Until(Still()),
                Hold(Right), Until(XAtLeast(28.9f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("Tread_RD")), Release(), Until(Still()),
                Hold(Left), Jump(), Until(GroundedOn("Tread_RE")), Release(), Until(Still()),
                Hold(Left), Jump(), Until(GroundedOn("S2_Main")), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: the floor one step ahead drops a cat that runs on", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead())), revealedBy: "Floor_1"),
                new Betrayal("T2: the floor where T1's jump lands gives way under a cat that stops", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stop where it lands", Release(), Until(Dead())), revealedBy: "Floor_2"),
                new Betrayal("T3: S2's end, the step L3 taught, gives way onto spikes on S1", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_RC))", "step onto S2's end", Release(), Until(Still()), Hold(Left), Jump(), Until(Dead())), revealedBy: "S2_End"),
                new Betrayal("T4: an S1 section gives way under a cat that walks onto it", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_A))", "walk on", Until(Dead())), revealedBy: "S1_T4"),
                new Betrayal("T5: spikes under the overhang meet a cat that jumps the next stretch too", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.1)", "jump the next stretch", Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(13.9f)), Jump(), Until(Dead()))),
                new Betrayal("T6: the floor ahead drops again under a cat that runs on, into T2's open pit below", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=18)", "run on", Until(Dead())), revealedBy: "S1_T6"),
                new Betrayal("T6: spikes come up where a cat lands that jumps the gap at once, as T1 taught", "Spikes_6b", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=18)", "jump the gap at once", Until(XAtLeast(18.9f)), Jump(), Until(Dead()))),
                new Betrayal("Dead end: the left tower's rhythm goes on at the wall", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_LC))", "keep the rhythm", Release(), Until(Still()), Hold(Left), Jump(), Until(Dead())), revealedBy: "Tread_LD"),
                new Betrayal("Dead end: the tower's rhythm goes on past S2", "Spikes_F", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_RE))", "keep climbing", Release(), Until(Still()), Hold(Right), Jump(), Until(Dead())), revealedBy: "Tread_RF"));
        }
    }
}
