using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 1's anatomy. T1 (the first floor) teaches jumping; T2 punishes running on to it (stop as it
    // goes); T3 punishes backing away from T2 (stay put, then hop T2 once it has landed); T4 is passed on the shelf (the
    // other ledge); T5 is T1 again before the door. The high ledge is the dead end; the door backing away is a
    // Recovers betrayal on the solution itself.
    static class L001Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L001 solution",
                Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Ground_2")),
                // Stop as Block_1 goes: it lands ahead of the cat (and Block_2 behind it); then hop it.
                Until(Fired("Block_1")), Release(), Until(Still()), Until(Stopped("Block_1")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(10.9f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(14.9f)), Jump(), Until(GroundedOn("Shelf")),
                // Off the end of the shelf, then over the last floor.
                Until(XAtLeast(25.7f)), Until(Airborne()), Until(Grounded()), Until(XAtLeast(27.3f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: the first floor drops a cat that runs on", "Pit1_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run onto Floor_2", Until(Dead())), revealedBy: "Floor_2"),
                new Betrayal("T2: Block_1 lands on a cat that runs on to it", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2))", "run on after the jump", Until(Dead()))),
                new Betrayal("T3: Block_2 lands on a cat that backs away from Block_1", "Block_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Block_1))", "back away", Hold(Left), Until(Dead()))),
                new Betrayal("T4: Spikes_1 rise under a cat that stays on the ground", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "stay on the ground", Until(Dead()))),
                new Betrayal("T5: the last floor before the door drops a cat that runs on", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=27.3)", "run on", Until(Dead())), revealedBy: "Floor_7"),
                new Betrayal("Dead end: Spikes_2 rise on the high ledge", "Spikes_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Shelf))", "jump up to the ledge",
                        Release(), Until(Still()), Hold(Left), Until(XAtMost(17.7f)), Hold(Right), Jump(), Until(Dead()))),
                Betrayal.Recovers("The door backs away from a cat on the shelf, which follows it", "Door", solution));
        }
    }
}
