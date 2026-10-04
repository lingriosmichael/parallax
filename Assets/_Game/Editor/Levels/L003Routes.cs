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
    // PAX-103: S2 is the arrow floor (L003Layout): wait at the safe spot before each of its four lanes for a shot to land,
    // then go; hop the low block and the plinth around the level arrow's lane. The ground's and S1's arrows, the corbel's
    // arrow on S1 and S2's falling block (T5) are gone.
    static class L003Routes
    {
        // The ground as far as the right tower.
        static RouteStep[] Ground() => new[] {
            Hold(Right), Until(XAtLeast(7.4f)), Jump(), Until(Airborne()), Until(Grounded()) };

        // The right tower, from the ground to S1.
        static RouteStep[] RightTower() => new[] {
            Until(XAtLeast(23.9f)), Jump(), Until(GroundedOn("Tread_R1")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_R2")), Release(), Until(Still()),
            Hold(Left), Until(XAtMost(29f)), Release().Timed(TimedMode.Shift), Until(Still()),
            Hold(Left), Jump(), Until(XAtMost(27f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_R3")), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("S1_B")) };

        // PAX-102: wait short of a repeating arrow's lane for a shot to land, then go on (it fires again a beat later).
        static RouteStep[] WaitFor(string arrow) => new[] {
            // PAX-103: from its home (hidden after its cooldown; two ticks on, as vanishing is a change too), so the next change
            // is a shot; past its tell (it waits at its mouth); until it lands.
            Release(), Until(Still()), Until(Home(arrow)), For(2), Until(Moving(arrow)), For(10), Until(Stopped(arrow)) };

        // S1 from the hole to the left tower, the left tower, and S2 with the bait.
        static RouteStep[] Rest() => new[] {
            Until(XAtMost(17.5f)), Jump(), Until(GroundedOn("S1_A")),
            Until(XAtMost(5.45f)), Release().Timed(TimedMode.Shift), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("Tread_A")), Release(), Until(Still()),
            Hold(Right), Until(XAtLeast(2.8f)), Release().Timed(TimedMode.Shift), Until(Still()),
            // PAX-102: released over Tread_B, so the cat lands without striking the wall (that contact's solver order varies
            // with the arrows' bodies and made the replays differ by 1e-4).
            Hold(Left), Jump(), Until(XAtMost(1.7f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_B")), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_C")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("S2")),
            // PAX-103: the arrow floor. Arrow_2A's safe spot, then its lane.
            Until(XAtLeast(7.6f)) }.Concat(WaitFor("Arrow_2A")).Concat(new[] { Hold(Right),
            // Arrow_2C's: short of the low block; then over it, along the lane and over the plinth before the next shot.
            Until(XAtLeast(11.6f)) }).Concat(WaitFor("Arrow_2C")).Concat(new[] { Hold(Right),
            Until(XAtLeast(12f)), Jump(), Until(Airborne()), Until(GroundedOn("S2")),
            Until(XAtLeast(15.6f)), Jump(), Until(Airborne()), Until(GroundedOn("S2").And(XAtLeast(18f))),
            // Arrow_2D's: past the plinth; Arrow_2B's: midway.
            Release(), Until(Still()), Until(Home("Arrow_2D")), For(2), Until(Moving("Arrow_2D")), For(10), Until(Stopped("Arrow_2D")), Hold(Right),
            Until(XAtLeast(22.6f)) }).Concat(WaitFor("Arrow_2B")).Concat(new[] { Hold(Right),
            Until(RoomComplete()) }).ToArray();

        public static RoomRoutes Build()
        {
            var solution = new Route("L003 solution", Ground().Concat(RightTower()).Concat(Rest()).ToArray());
            var roundAgain = new Route("L003 round again", Ground().Concat(RightTower())
                .Concat(new[] { Hold(Left), Until(XAtMost(16.5f)), Until(GroundedOn("Ground_3")), Hold(Right) })
                .Concat(RightTower()).Concat(Rest()).ToArray());

            return new RoomRoutes(solution,
                new Betrayal("T1: the first floor drops a cat that runs on", "Pit1_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run onto Floor_1", Until(Dead())), revealedBy: "Floor_1"),
                new Betrayal("T2: the floor where the jump lands gives way under a cat that stops", "Pit3_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stop where it lands", Release(), Until(Dead())), revealedBy: "Floor_3"),
                Betrayal.Recovers("T3: a section of S1 drops the cat to the ground, and it walks round again", "S1_Mid", roundAgain),
                new Betrayal("T4: the left tower's step at the wall gives way onto spikes", "Spikes_B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_C))", "keep the rhythm",
                        Release(), Until(Still()), Hold(Left), Until(XAtMost(3.1f)), Release(), Until(Still()), Hold(Left), Jump(), Until(Dead()))),
                // PAX-103: the arrow floor. A cat that stops in a lane is hit by the next shot.
                new Betrayal("T8: Arrow_2A's next shot hits a cat that stops in its lane on S2", "Arrow_2A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2))", "stop in the lane", Hold(Right), Until(XAtLeast(10.2f)), Release(), Until(Dead()))),
                new Betrayal("T9: Arrow_2C's next shot hits a cat that stops in its lane between the block and the plinth", "Arrow_2C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_2C))", "stop in the lane",
                        Hold(Right), Until(XAtLeast(12f)), Jump(), Until(Airborne()), Until(GroundedOn("S2")), Until(XAtLeast(14.6f)), Release(), Until(Dead()))),
                new Betrayal("T10: Arrow_2D's next shot hits a cat that stops in its lane past the plinth", "Arrow_2D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2) && X>=18)", "stop in the lane", Hold(Right), Until(XAtLeast(20.9f)), Release(), Until(Dead()))),
                new Betrayal("T11: Arrow_2B's next shot hits a cat that stops where it comes in low", "Arrow_2B", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_2D))", "stop in the lane", Hold(Right), Until(XAtLeast(24.6f)), Release(), Until(Dead()))),
                new Betrayal("Dead end: the right tower's step past S1 gives way onto spikes", "Spikes_R", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_R3))", "keep climbing",
                        Until(Still()), Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
