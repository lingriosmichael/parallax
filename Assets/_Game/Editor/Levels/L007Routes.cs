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
        // PAX-104: Ride_A is phased to a cat that climbs at once: it's home as that cat comes down, and leaves ~20 ticks after
        // it boards. A cat that comes later (one that rode Tread_RC down and up) waits up here, out of the storm, for Ride_A to
        // come home again.
        static RouteStep[] AfterRC(bool waitForRideA) => new[] {
            Hold(Right), Until(XAtLeast(28.9f)), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_RD")), Release(), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("Tread_RE")), Release(), Until(Still()) }
            .Concat(waitForRideA ? new[] { Until(Moving("Ride_A")), Until(Home("Ride_A")) } : new RouteStep[0])
            .Concat(new[] { Hold(Left), Jump(), Until(GroundedOn("S2_East")) }).Concat(AlongS2()).ToArray();

        // PAX-102: S2 to the door: onto Ride_A (PAX-104: as it comes home, under the storm from here on), ride it over gap A,
        // walk on off the sinking section, onto Ride_B, ride it over gap B, then wait out Arrow_S before the door.
        static RouteStep[] AlongS2() => new[] {
            Until(GroundedOn("Ride_A").And(XAtMost(22f))), Release(), Until(Still()), Until(Stopped("Ride_A")),
            Hold(Left), Until(XAtMost(13.3f)), Release(), Until(Still()), Until(Home("Ride_B")),
            Hold(Left), Until(GroundedOn("Ride_B").And(XAtMost(12f))), Release(), Until(Still()), Until(Stopped("Ride_B")),
            Hold(Left), Until(XAtMost(6.6f)), Release(), Until(Still()), Until(Moving("Arrow_S")), Until(Stopped("Arrow_S")),
            Hold(Left), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L007 solution", new Route("L007 solution",
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
                Hold(Left), Jump(), Until(GroundedOn("Tread_RC")), Release(), Until(Still()))
                .Steps.Concat(AfterRC(waitForRideA: false)).ToArray());

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
                new Betrayal("T10: a cat that comes down before Ride_A is home falls into gap A", "Spikes_GapA", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_RE))", "come down at once", Release(), Until(Still()), Until(Home("Ride_A")), For(10), Until(Moving("Ride_A")), For(10),
                        Hold(Left), Jump(), Until(GroundedOn("S2_East")), Until(Dead())), revealedBy: "Ride_A"),
                new Betrayal("T11: Sink_S gives way under a cat that stops where Ride_A set it down", "Spikes_GapO", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Ride_A))", "stop on the section", Hold(Left), Until(XAtMost(17.3f)), Release(), Until(Dead())), revealedBy: "Sink_S"),
                new Betrayal("T12: a cat that runs on off Ride_B before it leaves falls into gap B", "Spikes_GapB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Ride_A))", "run on", Hold(Left), Until(Dead())), revealedBy: "Ride_B"),
                // PAX-103: the storm. Waiting is what it punishes.
                new Betrayal("T14: the storm strikes a cat that stops on S2_Mid instead of going on to Ride_B", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Ride_A))", "stop on S2_Mid", Hold(Left), Until(XAtMost(14.2f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T13: Arrow_S's next shot hits a cat that stops in its lane before the door", "Arrow_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Ride_B))", "stop in the lane", Hold(Left), Until(XAtMost(4f)), Release(), Until(Dead()))),
                // PAX-100 (D-106): the sinking tread. A cat that waits on it rides it down and back up, then goes on.
                // PAX-107: with the storm 20 % faster and striking 10 % more often, a cat that waits on Tread_RC to ride it down and
                // back up is struck before it comes back (it recovered before).
                new Betrayal("T7: a cat that waits on Tread_RC as it sinks is struck by the storm", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_RC))", "wait on the tread", Release(), Until(Dead())), revealedBy: "Cloud"));
        }
    }
}
