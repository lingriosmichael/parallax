using System.Linq;
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
        // From Tread_RC (it sinks 30 ticks after a landing: PAX-100, D-106) up the wall steps and left along S2 to the door.
        static RouteStep[] AfterRC() => new[] {
            Hold(Right), Until(XAtLeast(28.9f)), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_RD")), Release(), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("Tread_RE")), Release(), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("S2_East")) }.Concat(AlongS2()).ToArray();

        // PAX-102: S2 to the door: wait for Ride_A, ride it over gap A, walk on off the sinking section, wait for Ride_B, ride
        // it over gap B, then wait out Arrow_S before the door.
        static RouteStep[] AlongS2() => new[] {
            Until(XAtMost(23.3f)), Release(), Until(Still()), Until(Home("Ride_A")),
            Hold(Left), Until(GroundedOn("Ride_A").And(XAtMost(22f))), Release(), Until(Still()), Until(XAtMost(18.9f)),
            Hold(Left), Until(XAtMost(13.3f)), Release(), Until(Still()), Until(Home("Ride_B")),
            Hold(Left), Until(GroundedOn("Ride_B").And(XAtMost(12f))), Release(), Until(Still()), Until(XAtMost(8.9f)),
            Hold(Left), Until(XAtMost(6.6f)), Release(), Until(Still()), Until(Moving("Arrow_S")), Until(Stopped("Arrow_S")),
            Hold(Left), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L007 solution", new Route("L007 solution",
                // Left along the ground: jump the floor that drops, and run on off the one that lands you.
                Hold(Left), Until(Fired("Spikes_1")), Until(XAtMost(27.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                // PAX-102: wait out Arrow_G; then stop at once as Arrow_O fires, let it land, and walk on through it.
                Until(XAtMost(19.2f)), Release(), Until(Still()), Until(Moving("Arrow_G")), Until(Stopped("Arrow_G")),
                Hold(Left), Until(Fired("Arrow_O")), Release(), Until(Still()), Until(Stopped("Arrow_O")), Hold(Left),
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
                Hold(Left), Jump(), Until(GroundedOn("Tread_RC")), Release(), Until(Still()))
                .Steps.Concat(AfterRC()).ToArray());

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
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_RE))", "keep climbing", Release(), Until(Still()), Hold(Right), Jump(), Until(Dead())), revealedBy: "Tread_RF"),
                // PAX-102: the ground's arrows, the riders, the sinking section and the door's arrow.
                new Betrayal("T8: Arrow_G's next shot hits a cat that stops in its lane on the ground", "Arrow_G", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stop in the lane", Until(XAtMost(15.9f)), Release(), Until(Dead()))),
                new Betrayal("T9: Arrow_O comes down on a cat that walks on", "Arrow_O", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_G))", "walk on", Hold(Left), Until(Dead()))),
                new Betrayal("T10: a cat that runs on without waiting for Ride_A falls into gap A", "GapA_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2_East))", "run on", Hold(Left), Until(Dead())), revealedBy: "Ride_A"),
                new Betrayal("T11: Sink_S sinks into gap A with a cat that stops where Ride_A set it down", "GapA_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=18.9)", "stop on the section", Hold(Left), Until(XAtMost(17.3f)), Release(), Until(Dead())), revealedBy: "Sink_S"),
                new Betrayal("T12: a cat that runs on without waiting for Ride_B falls into gap B", "GapB_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=18.9)", "run on", Hold(Left), Until(Dead())), revealedBy: "Ride_B"),
                new Betrayal("T13: Arrow_S's next shot hits a cat that stops in its lane before the door", "Arrow_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=8.9)", "stop in the lane", Hold(Left), Until(XAtMost(4f)), Release(), Until(Dead()))),
                // PAX-100 (D-106): the sinking tread. A cat that waits on it rides it down and back up, then goes on.
                Betrayal.Recovers("T7: a cat that waits on Tread_RC sinks with it, rides it back up and goes on", "Tread_RC",
                    new Route("wait on the tread", Route.PrefixOf(solution, "Until(GroundedOn(Tread_RC))", "wait on the tread",
                        Release(), Until(Still()), Until(Moving("Tread_RC")), Until(Home("Tread_RC"))).Steps.Concat(AfterRC()).ToArray())));
        }
    }
}
