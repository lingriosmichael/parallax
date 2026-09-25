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

        // S1 from the hole to the left tower, the left tower, and S2 with the bait.
        static RouteStep[] Rest() => new[] {
            Until(XAtMost(17.5f)), Jump(), Until(GroundedOn("S1_A")),
            Until(XAtMost(5.45f)), Release().Timed(TimedMode.Shift), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("Tread_A")), Release(), Until(Still()),
            Hold(Right), Until(XAtLeast(2.8f)), Release().Timed(TimedMode.Shift), Until(Still()),
            Hold(Left), Jump(), Until(GroundedOn("Tread_B")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Tread_C")), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("S2")),
            // The bait: into Block_5's trigger, back out, and over the block once it has landed.
            Until(XAtLeast(16.9f)), Hold(Left).Timed(TimedMode.Hesitate), Until(XAtMost(15.5f)), Release(), Until(Still()), Until(Stopped("Block_5")),
            Hold(Right), Until(XAtLeast(18.4f)), Jump(), Until(Airborne()), Until(GroundedOn("S2").And(XAtLeast(21f))), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L003 solution", Ground().Concat(RightTower()).Concat(Rest()).ToArray());
            var roundAgain = new Route("L003 round again", Ground().Concat(RightTower())
                .Concat(new[] { Until(XAtMost(16.5f)), Until(GroundedOn("Ground_3")), Hold(Right) })
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
                new Betrayal("T5: Block_5 comes down on a cat that runs on along S2", "Block_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2))", "run on", Until(Dead()))),
                new Betrayal("Dead end: the right tower's step past S1 gives way onto spikes", "Spikes_R", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tread_R3))", "keep climbing",
                        Until(Still()), Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
