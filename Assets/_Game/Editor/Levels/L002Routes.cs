using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 2's anatomy. T1 is the bait (touch its trigger, step back, let Block_1 land); T2 punishes
    // going on straight after it (wait for Block_2); T3 punishes waiting (don't stop on the ledge); T4 is jumped on S1;
    // T5 is the door backing away over a section of S1 that isn't there (jump it). The stair behind the start and the
    // high ledge on S1 are the dead ends. Every release that sets up a landing or a take-off spot is a timed step, so its
    // window of release ticks is measured (>= 12, PAX-059: a release is mid-air control, and no landing may need precision).
    static class L002Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L002 solution",
                // The bait: into Block_1's trigger and straight back out.
                Hold(Left), Until(XAtMost(23.4f)),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(24f)), Release(), Until(Still()), Until(Stopped("Block_1")),
                // PAX-102: Block_1 is 1.6 u wide now; the hop lands on it, and a second hop comes down short of Block_2's place.
                Hold(Left), Until(XAtMost(22.5f)), Jump(), Until(Airborne()), Until(Grounded()),
                Jump(), Until(Airborne()), Until(XAtMost(19.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ground_2")),
                // Wait for Block_2, then hop it with a run-up.
                Until(Still()), Until(Stopped("Block_2")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(19.1f)), Hold(Left), Until(XAtMost(18.6f)), Jump(), Until(GroundedOn("Ground_2")),
                Until(XAtMost(14.1f)), Jump(), Until(XAtMost(11.3f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ledge")),
                // Straight on off the ledge.
                Hold(Left), Jump(), Until(GroundedOn("Ground_1")),
                // The tower.
                Until(XAtMost(5.4f)), Jump(), Until(XAtMost(3.4f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Tread_A")), Until(Still()),
                Hold(Left), Until(XAtMost(2.6f)), Release().Timed(TimedMode.Shift), Until(Still()), Hold(Left), Jump(), Until(GroundedOn("Tread_B")), Release(), Until(Still()),
                Hold(Right), Jump(), Until(GroundedOn("Tread_C")), Release(), Until(Still()),
                Hold(Left), Until(XAtMost(2.7f)), Release().Timed(TimedMode.Shift), Until(Still()), Hold(Right), Jump(), Until(GroundedOn("S1_A")),
                // PAX-102: stop short of Spikes_4, hop them onto the strip before Block_3's hole, hop the three holes, and jump the
                // section that isn't there to the door.
                Release(), Until(Still()), Hold(Right), Jump(), Until(XAtLeast(9.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("S1_A")),
                Until(Still()), Hold(Right), Jump(), Until(XAtLeast(12.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("S1_A2")), Hold(Right),
                Until(XAtLeast(15.6f)), Jump(), Until(XAtLeast(18.1f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("S1_A3")),
                Until(Still()), Hold(Right), Until(XAtLeast(19.4f)), Jump(), Until(XAtLeast(22f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("S1_A4")),
                Hold(Right), Until(XAtLeast(23.2f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Block_1 comes down on a cat that runs on from the start", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Left)", "run on", Until(Dead()))),
                new Betrayal("T2: Block_2 comes down on a cat that hopped Block_1 and ran on", "Block_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "run on after the hop", Until(Dead()))),
                new Betrayal("T3: Block_3 comes down on a cat that stops on the ledge", "Block_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ledge))", "stop on the ledge", Release(), Until(Dead()))),
                new Betrayal("T4: Spikes_4 rise under a cat that runs along S1", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_A))", "run on", Hold(Right), Until(Dead()))),
                new Betrayal("T5: a cat that follows the door walks off S1 onto Spikes_5", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=23.2)", "follow the door", Until(Dead())), revealedBy: "Floor_9"),
                new Betrayal("Dead end: the stair up to the door", "Pit9_Hazard", DeathCause.Hazard,
                    new Route("stair", Hold(Right), Until(XAtLeast(27.2f)), Jump(), Until(GroundedOn("Tread_1")), Jump(), Until(Dead())), revealedBy: "Tread_2"),
                new Betrayal("Dead end: the high ledge on S1 gives way onto spikes", "Spikes_6", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1_A2))", "the high ledge",
                        Release(), Until(Still()), Hold(Left), Until(XAtMost(12.6f)), Release(), Until(Still()),
                        Hold(Right), Jump(), Until(XAtLeast(13.6f)), Release(), Until(GroundedOn("Step_1")), Until(Still()),
                        Hold(Right), Jump(), Until(GroundedOn("Ledge_H")), Until(XAtLeast(19f)), Release(), Until(Dead()))));
        }
    }
}
