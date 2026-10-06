using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 1's anatomy. T1 (the first floor) teaches jumping; T2 punishes running on to it (stop as it
    // goes); T3 punishes backing away from T2 (stay put: both land as grip walls, climbed wall to wall, PAX-105); T4 is
    // passed on the shelf (the other ledge); T5 is T1 again before the door. The door backing away is a Recovers betrayal on
    // the solution itself. D-111 (the developer, 2026-10-04): the floor between the walls shrinks away (stay and fall), the
    // ledge falls on a cat that runs under it, and the last falling floor is 4 u long, so the solution below doesn't
    // complete yet: the developer's solution comes later.
    static class L001Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L001 solution",
                Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Ground_2")),
                // Stop as Block_1 goes: it lands ahead of the cat (and Block_2 behind it), walling it in. PAX-105 (D-110 amendment 3):
                // jump at Block_1 (it latches by itself, PAX-106) and wall-jump from wall to wall until the kick off Block_2 lands the cat on
                // Block_1's top; then run off it onto the shelf.
                Until(Fired("Block_1")), Release(), Until(Still()), Until(Stopped("Block_1")), Until(Stopped("Block_2")),
                Hold(Right), Jump(), Until(Falling()), Until(ClingingRight()), Release(),
                Jump(), Hold(Left), Until(ClingingLeft()), Release(),
                Jump(), Hold(Right), Until(GroundedOn("Block_1")), Until(GroundedOn("Shelf")),
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
                    Route.PrefixOf(solution, "Until(GroundedOn(Block_1))", "step down to the ground and run on under the shelf",
                        Release(), Until(Still()), Hold(Right), Until(XAtLeast(13.9f)), Release(), Until(GroundedOn("Ground_2b")), Hold(Right), Until(Dead()))),
                // D-111: the floor between the walls shrinks away under a cat that stays on it.
                new Betrayal("Squeeze: the floor between the walls shrinks away under a cat that stays", "Pit2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_2))", "stay between the walls", Until(Dead())), revealedBy: "Squeeze"),
                // D-111: the ledge falls on a cat that runs through the gap under it.
                new Betrayal("The ledge falls on a cat that runs under it", "Ledge", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Shelf))", "run on under the ledge", Until(Dead()))),
                new Betrayal("T5: the last floor before the door drops a cat that runs on", "Pit5_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=27.3)", "run on", Until(Dead())), revealedBy: "Floor_7"),
                Betrayal.Recovers("The door backs away from a cat on the shelf, which follows it", "Door", solution));
        }
    }
}
