using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 8's anatomy. T1 (a roof block) is stood out; T2 is the chain lesson: the block's landing
    // brings spikes up further on, so a cat that hops the block at once meets them coming up (watch the chain finish, then
    // jump them). PAX-107 (L008Layout): T3, S2's hinge floor, stands up behind the cat and pushes it toward the waterfall
    // shaft: let the wave pass and drop in behind it before the wall arrives (T4 punishes dropping into the wave). On S1,
    // T5 (spikes under the hinge's hole) is jumped and T6, S1's hinge floor, pushes a cat that stops onto Spikes_5 (jumped);
    // T7 (a block in S1's underside) is stood out, T9's rider crosses the ground's first pit, and the chimney under the
    // wall is climbed on its two mossy faces to the door. The hole behind the start and the run east along S1 toward the door
    // are the dead ends.
    static class L008Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L008 solution",
                // S2: stop for the block, watch the spikes it set off come up, hop the block and jump the spikes.
                Hold(Right), Until(XAtLeast(4.5f)), Release(), Until(Still()), Until(Stopped("Block_1")), Until(Fired("Spikes_2")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(9.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                // PAX-107: over the hinge floor (it stands up behind the cat), to the shaft's edge; let the first wave go by and drop
                // in behind it, before the wall arrives.
                Until(XAtLeast(20.3f)), Release(), Until(Still()), Until(Moving("Fall_W0")), For(2), Until(Moving("Fall_W0")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(Airborne()), Release(), Until(GroundedOn("S1")),
                // S1: back left over Spikes_4, past the hinge floor as it stands up, over Spikes_5, and off its end into the column.
                Hold(Left), Until(XAtMost(15.7f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(10.7f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Airborne()), Until(GroundedOn("Ground")),
                // The ground: stop for the block, hop it, and on.
                Hold(Right), Until(XAtLeast(8.8f)), Release(), Until(Still()), Until(Stopped("Block_6")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                // PAX-102: wait for Ride_A to come home (PAX-107: a fresh return, so it waits its whole 50 ticks) and ride it over
                // the first pit.
                Until(XAtLeast(13.9f)), Release(), Until(Still()), Until(Moving("Ride_A")), For(2), Until(Home("Ride_A")),
                Hold(Right), Until(GroundedOn("Ride_A").And(XAtLeast(15.5f))), Release(), Until(Still()), Until(XAtLeast(18.4f)),
                // PAX-107: down into the chimney, under the wall, and up its two mossy faces to the door.
                Hold(Right), Until(GroundedOn("Sump_Floor")), Until(XAtLeast(24.9f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(ClingingRight()), Release(),
                Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
                Jump(), For(2), Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Block_1 comes down on a cat that runs on", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run on", Until(Dead()))),
                new Betrayal("T2: spikes the block's landing set off come up under a cat that hops it at once", "Spikes_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_1))", "hop the block at once", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                new Betrayal("T3: S2's hinge floor pushes a cat that waits at the shaft into the waterfall", "Fall_W0", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=20.3)", "wait for the wall", Release(), Until(Dead())), revealedBy: "Hinge_S2"),
                new Betrayal("T4: the waterfall's first wave meets a cat that runs straight into the shaft", "Fall_E0", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=20.3)", "run straight in", Until(Dead()))),
                new Betrayal("T5: Spikes_4, up under the hinge floor's hole, meet a cat that runs on along S1", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "run on", Hold(Left), Until(Dead()))),
                new Betrayal("T6: S1's hinge floor pushes a cat that stops past Spikes_4 onto Spikes_5", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=15.7)", "stop where it lands", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Hinge_S1"),
                new Betrayal("T7: Block_6 comes down on a cat that runs on along the ground", "Block_6", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground))", "run on", Hold(Right), Until(Dead()))),
                new Betrayal("T9: a cat that runs on without waiting for Ride_A falls into the first pit", "Pit10_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_6))", "run on", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Ride_A"),
                new Betrayal("Dead end: east along S1 toward the door", "Spikes_E", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "east toward the door", Hold(Right), Until(Dead()))),
                new Betrayal("Dead end: the hole behind the start", "Pit9_Hazard", DeathCause.Hazard,
                    new Route("the way down", Hold(Left), Until(Dead())), revealedBy: "Floor_9"));
        }
    }
}
