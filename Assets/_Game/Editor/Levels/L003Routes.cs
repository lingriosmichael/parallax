using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 3's anatomy. T1 (a floor that isn't there) teaches jumping; T2 punishes stopping where that
    // jump lands (keep going); T3 drops the cat from S1 back to the ground (a Recovers betrayal: it walks round again);
    // T4 is the left tower's rhythm step at the wall (take the step off the rhythm, onto S2); T5 punishes keeping going
    // (the one bait). The right tower's rhythm step past S1 is the dead end. Every release that sets up a landing or a
    // take-off spot is a timed step (its window is measured).
    // PAX-103: S2 was the arrow floor. D-113: S1 is the arrow floor now (jump the arrows from both ends; Lift_1 and Shrink_1
    // in it), and S2 is two spike trenches crossed on shuttling floors; T3 (S1_Mid) is gone.
    static class L003Routes
    {
        // The ground as far as the right tower.
        static RouteStep[] Ground() => new[] {
            Hold(Right), Until(XAtLeast(7.4f)), Jump(), Until(Airborne()), Until(Grounded()),
            // D-113: Pit 5, under Lift_1's gap.
            Until(XAtLeast(16f)), Jump(), Until(Airborne()), Until(GroundedOn("Ground_4")) };

        // The right tower, from the ground to S1.
        static RouteStep[] RightTower() => new[] {
            Until(XAtLeast(23.9f)), Jump(), Until(GroundedOn("Tread_R1")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_R2")), Release(), Until(Still()),
            Hold(Left), Until(XAtMost(29f)), Release().Timed(TimedMode.Shift), Until(Still()),
            Hold(Left), Jump(), Until(XAtMost(27f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_R3")), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("S1_B")) };

        // D-113: S1, the arrow floor, from S1_B to the left tower: up onto Post_R (1.2 u tall, the perch over both lanes); 28
        // ticks after Lift_1 comes up (the arrows fire on its rhythm: Arrow_L at 0 every 200, Arrow_R at 50 every 100), walk
        // off onto the lift and run without a stop: jump its gap to S1_C, jump onto
        // Shrink_1 and run off it as it shrinks behind, and jump up onto raised S1_A. Each jump clears an arrow (planned against
        // both lanes; every jump's window is measured).
        static RouteStep[] ArrowFloor() => new[] {
            // Up onto Post_R, the perch over both lanes: straight up beside it, then over and down onto its top.
            Release(), Until(Still()), Jump(), Until(YAtLeast(6.6f)), Hold(Left), Until(XAtMost(22.9f)), Release(), Until(GroundedOn("Post_R")), Until(Still()),
            // 28 ticks after Lift_1 comes up, walk off the perch onto it and run the floor.
            Until(Moving("Lift_1")), Until(Home("Lift_1")), For(28),
            Hold(Left).Timed(TimedMode.Hesitate), Until(GroundedOn("Lift_1")),
            Until(XAtMost(19.81f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("S1_C")),
            Until(XAtMost(15.61f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Shrink_1")),
            Until(XAtMost(10.69f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("S1_A")), Release(), Until(Still()) };

        // The left tower, and S2 (D-113 amendment): across the spike bed on its platforms to S2_C and the door.
        static RouteStep[] Rest() => new[] {
            Hold(Left), Jump(), Until(XAtMost(3.6f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_A")), Until(Still()),
            Hold(Right), Until(XAtLeast(2.8f)), Release().Timed(TimedMode.Shift), Until(Still()),
            // PAX-102: released over Tread_B, so the cat lands without striking the wall (that contact's solver order varies
            // with the arrows' bodies and made the replays differ by 1e-4).
            Hold(Left), Jump(), Until(XAtMost(1.7f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_B")), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_C")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("S2_A")), Release(), Until(Still()),
            // D-113 amendment: over False_1 onto Hop_1; onto Ride_1 at home and out; up onto Lift_2 while it's up; over False_2
            // down onto Hop_2; onto Ride_2 when it comes to fetch the cat, and off onto S2_C.
            Hold(Right), Until(XAtLeast(8.4f)), Jump(), Until(XAtLeast(11.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Hop_1")), Until(Still()),
            Until(Moving("Ride_1")), Until(Home("Ride_1")),
            Hold(Right), Until(XAtLeast(12.6f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ride_1")), Until(Stopped("Ride_1")),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(17.9f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Lift_2")),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(21.3f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Hop_2")), Until(Still()),
            Until(Moving("Ride_2")), Until(Stopped("Ride_2")),
            Hold(Right), Until(XAtLeast(23.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ride_2")), Until(Home("Ride_2")),
            Hold(Right), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L003 solution", Ground().Concat(RightTower()).Concat(ArrowFloor()).Concat(Rest()).ToArray());

            return new RoomRoutes(solution,
                new Betrayal("T1: the first floor drops a cat that runs on", "Pit1_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run onto Floor_1", Until(Dead())), revealedBy: "Floor_1"),
                new Betrayal("T2: the floor where the jump lands gives way under a cat that stops", "Pit3_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stop where it lands", Release(), Until(Dead())), revealedBy: "Floor_3"),
                new Betrayal("T4: the left tower's step at the wall gives way onto spikes", "Spikes_B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_C))", "keep the rhythm",
                        Release(), Until(Still()), Hold(Left), Until(XAtMost(3.1f)), Release(), Until(Still()), Hold(Left), Jump(), Until(Dead()))),
                // D-113: S1, the arrow floor.
                new Betrayal("T6: a cat that waits on S1_C is shot", "Arrow_L", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_C))", "wait", Release(), Until(Dead()))),
                new Betrayal("T7: a cat that runs off Lift_1 into its gap drops to the ground and runs on into Pit 3", "Pit3_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Lift_1))", "run off the lift", Hold(Left), Until(Dead())), revealedBy: "Lift_1"),
                new Betrayal("T8: Shrink_1 shrinks out from under a cat that lands short on it and stops", "Pit3_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=15.61)", "land short and stop", Jump(), Until(XAtMost(12f)), Release(), Until(Dead())), revealedBy: "Shrink_1"),
                // D-113 amendment: S2, the spike bed.
                new Betrayal("T9: False_1, the first step, is a fake; a cat that lands on it falls onto the spikes", "Trench_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2_A))", "hop onto the first step",
                        Release(), Until(Still()), Hold(Right), Until(XAtLeast(8.4f)), Jump(), Until(XAtLeast(9.6f)), Release(), Until(Dead())), revealedBy: "False_1"),
                new Betrayal("T10: a cat that steps off Hop_1 while Ride_1 is away falls onto the spikes", "Trench_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Ride_1))", "step off while it's away", Until(Moving("Ride_1")), For(15), Hold(Right), Until(Dead())), revealedBy: "Ride_1"),
                new Betrayal("T11: False_2, the near step after the lift, is a fake; a cat that lands on it falls onto the spikes", "Trench_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Lift_2))", "hop onto the near step", Hold(Right), Jump(), Until(XAtLeast(19.9f)), Release(), Until(Dead())), revealedBy: "False_2"),
                new Betrayal("T12: a cat that lets Ride_2 go back without it and then steps off Hop_2 falls onto the spikes", "Trench_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Ride_2))", "step off after it's gone", Until(Moving("Ride_2")), Until(Home("Ride_2")), Until(Home("Hop_2")), Hold(Right), Until(Dead())), revealedBy: "Ride_2"),
                new Betrayal("Dead end: the right tower's step past S1 gives way onto spikes", "Spikes_R", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_R3))", "keep climbing",
                        Until(Still()), Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
