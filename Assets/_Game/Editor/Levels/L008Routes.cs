using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 8's anatomy. T1 (a roof block) is stood out; T2 is the chain lesson: the block's landing
    // brings spikes up further on, so a cat that hops the block at once meets them coming up (watch the chain finish, then
    // jump them); T3 (a lift into roof spikes) is jumped; T4 punishes stopping where that jump lands; T5 is the long chain:
    // T4's collapse brought spikes up on S1, in view from above (jump them); T6 (a block in S1's underside) is stood out.
    // The hole behind the start and the ledge down toward the door are the dead ends.
    static class L008Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L008 solution",
                // S2: stop for the block, watch the spikes it set off come up, hop the block and jump the spikes.
                Hold(Right), Until(XAtLeast(4.5f)), Release(), Until(Still()), Until(Stopped("Block_1")), Until(Fired("Spikes_2")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(9.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                // Over the lift, run on off the floor that lands you, and off S2's end onto S1.
                Until(XAtLeast(13.4f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Airborne()), Until(GroundedOn("S1")),
                // S1: back left over the spikes the collapse brought up, and off its end into the column.
                // PAX-102: wait where it lands for Arrow_S's shot, then go.
                Release(), Until(Still()), Until(Moving("Arrow_S")), Until(Stopped("Arrow_S")),
                Hold(Left), Until(XAtMost(15.6f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Airborne()), Until(GroundedOn("Ground")),
                // The ground: stop for the block, hop it, and on to the door.
                // PAX-102: stop at once as Arrow_O fires, let it land, walk on.
                Release(), Until(Still()), Hold(Right), Until(Fired("Arrow_O")), Release(), Until(Still()), Until(Stopped("Arrow_O")),
                Hold(Right), Until(XAtLeast(8.8f)), Release(), Until(Still()), Until(Stopped("Block_6")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                // PAX-102: wait for Ride_A, ride it over the first pit, wait for Ride_B, ride it over the second, on to the door.
                Until(XAtLeast(13.9f)), Release(), Until(Still()), Until(Home("Ride_A")),
                Hold(Right), Until(GroundedOn("Ride_A").And(XAtLeast(15.5f))), Release(), Until(Still()), Until(XAtLeast(18.4f)),
                Hold(Right), Until(XAtLeast(20.4f)), Release(), Until(Still()), Until(Home("Ride_B")),
                Hold(Right), Until(GroundedOn("Ride_B").And(XAtLeast(22f))), Release(), Until(Still()), Until(XAtLeast(24.9f)),
                Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Block_1 comes down on a cat that runs on", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run on", Until(Dead()))),
                new Betrayal("T2: spikes the block's landing set off come up under a cat that hops it at once", "Spikes_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_1))", "hop the block at once", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                new Betrayal("T3: the lift carries a cat that walks onto it into the roof's spikes", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=13.4)", "walk onto the lift", Until(Dead())), revealedBy: "Lift_3"),
                new Betrayal("T4: the floor where the jump over the lift lands gives way under a cat that stops", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=13.4)", "stop where it lands", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Floor_4"),
                new Betrayal("T5: spikes the collapse brought up on S1 meet a cat that runs on", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_S))", "run on along S1", Hold(Left), Until(Dead()))),
                new Betrayal("T6: Block_6 comes down on a cat that runs on along the ground", "Block_6", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_O))", "run on", Hold(Right), Until(Dead()))),
                // PAX-102: the arrows and the riders.
                new Betrayal("T7: Arrow_S's next shot hits a cat that stops in its lane on S1", "Arrow_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "stop in the lane", Release(), Until(Still()), Hold(Left), Until(XAtMost(18.6f)), Release(), Until(Dead()))),
                new Betrayal("T8: Arrow_O comes down on a cat that walks on from the column", "Arrow_O", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground))", "walk on", Release(), Until(Still()), Hold(Right), Until(Dead()))),
                new Betrayal("T9: a cat that runs on without waiting for Ride_A falls into the first pit", "Pit10_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_6))", "run on", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Ride_A"),
                new Betrayal("T10: a cat that runs on without waiting for Ride_B falls into the second pit", "Pit11_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=18.4)", "run on", Hold(Right), Until(Dead())), revealedBy: "Ride_B"),
                new Betrayal("Dead end: the ledge down toward the door", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "down toward the door", Hold(Right), Until(Dead())), revealedBy: "Ledge_D"),
                new Betrayal("Dead end: stepping off S1's end toward the door", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "step off", Release(), Until(Still()), Hold(Right), Until(XAtLeast(27.4f)), Release(), Until(Still()),
                        Hold(Right), Until(Airborne()), Release(), Until(Dead())), revealedBy: "Ledge_D"),
                new Betrayal("Dead end: the hole behind the start", "Pit9_Hazard", DeathCause.Hazard,
                    new Route("the way down", Hold(Left), Until(Dead())), revealedBy: "Floor_9"));
        }
    }
}
