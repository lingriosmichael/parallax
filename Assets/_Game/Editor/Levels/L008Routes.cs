using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 8's anatomy. T1 (a roof block) is stood out; T2 is the chain lesson: the block's landing
    // brings spikes up further on, so a cat that hops the block at once meets them coming up (watch the chain finish, then
    // jump them). PAX-107 (L008Layout): T3, S2's hinge floor, stands up behind the cat and comes after it, spiked (D-124),
    // toward the waterfall shaft: let the wave pass and drop in behind it before the wall arrives (T4 punishes dropping into the wave). On S1,
    // T5 (spikes under the hinge's hole) is jumped and T6, S1's hinge floor, pushes a cat that stops onto Spikes_5 (jumped);
    // T7 (a block in S1's underside) is stood out, T9's rider crosses the ground's first pit, and the chimney under the
    // wall is climbed on its two mossy faces to the door. The hole behind the start and the run east along S1 toward the door
    // are the dead ends. PAX-107 round three: P1 (Press_S1) comes down where the jump over Spikes_5 lands and P2 (Piston_G)
    // rises out of the ground under S1_W: wait each out. The climb out of the shaft's top into the maze east of it is the
    // other way down (it recovers: M1 the press, M2 the spikes, M3 the piston, M4 Spikes_E under the drop).
    static class L008Routes
    {
        static RouteStep[] Concat(params RouteStep[][] parts) => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(parts, p => p));

        // From landing on S1 (by the shaft) to the door: the way both routes share.
        static RouteStep[] FromS1() => new[] {
            // S1: back left over Spikes_4, past the hinge floor as it stands up, over Spikes_5; then wait out Press_S1 (round
            // three) where the jump lands, and off S1_W's end into the column.
            Hold(Left), Until(XAtMost(15.7f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(10.7f)), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(Moving("Press_S1")), For(2), Until(Home("Press_S1")),
            Hold(Left).Timed(TimedMode.Hesitate), Until(Airborne()), Until(GroundedOn("Ground")), Release(), Until(Still()),
            // The ground: wait out Piston_G by the wall, then on to the block, stop for it, hop it, and on.
            Until(Moving("Piston_G")), For(2), Until(Home("Piston_G")),
            Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(8.8f)), Release(), Until(Still()), Until(Stopped("Block_6")),
            Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
            // PAX-102: wait for Ride_A to come home (PAX-107: a fresh return, so it waits its whole 50 ticks) and ride it over
            // the first pit.
            Until(XAtLeast(13.9f)), Release(), Until(Still()), Until(Moving("Ride_A")), For(2), Until(Home("Ride_A")),
            Hold(Right), Until(GroundedOn("Ride_A").And(XAtLeast(15.5f))), Release(), Until(Still()), Until(XAtLeast(18.4f)),
            // PAX-107: down into the chimney, under the wall, and up its two mossy faces to the door.
            Hold(Right), Until(GroundedOn("Sump_Floor")), Until(XAtLeast(24.9f)), Release(), Until(Still()),
            Hold(Right), Jump(), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L008 solution", Concat(new[] {
                // S2: stop for the block, watch the spikes it set off come up, hop the block and jump the spikes.
                Hold(Right), Until(XAtLeast(4.5f)), Release(), Until(Still()), Until(Stopped("Block_1")), Until(Fired("Spikes_2")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(9.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                // PAX-107: over the hinge floor (it stands up behind the cat), to the shaft's edge; let the first wave go by and drop
                // in behind it, before the wall arrives.
                Until(XAtLeast(20.3f)), Release(), Until(Still()), Until(Moving("Fall_W0")), For(2), Until(Moving("Fall_W0")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(Airborne()), Release(), Until(GroundedOn("S1")) }, FromS1()));

            // PAX-107 round three: the other way down. From the shaft's edge, jump onto Shaft_Moss, wall-jump to Column_Moss and over
            // Shaft_E onto Maze_1; wait out Press_1, drop to Maze_2, wait out Spikes_M2, drop to Maze_3, wait out Piston_3, and drop
            // onto S1 short of Spikes_E; then the way on, as the solution's.
            var maze = new Route("the climb and the maze", Concat(new[] {
                Hold(Right), Until(XAtLeast(4.5f)), Release(), Until(Still()), Until(Stopped("Block_1")), Until(Fired("Spikes_2")),
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(9.2f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(20.3f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(ClingingRight()), Release(),
                Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
                Jump(), For(2), Hold(Right), Until(GroundedOn("Maze_1")), Release(), Until(Still()),
                Until(Moving("Press_1")), For(2), Until(Home("Press_1")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(GroundedOn("Maze_2")), Release(), Until(Still()),
                Until(Moving("Spikes_M2")), For(2), Until(Home("Spikes_M2")),
                Hold(Left).Timed(TimedMode.Hesitate), Until(GroundedOn("Maze_3a")), Release(), Until(Still()),
                Until(Moving("Piston_3")), For(2), Until(Home("Piston_3")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(28.6f)), Until(Airborne()), Release(), Until(GroundedOn("S1")), Until(Still()) }, FromS1()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Block_1 comes down on a cat that runs on", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Hold(Right)", "run on", Until(Dead()))),
                new Betrayal("T2: spikes the block's landing set off come up under a cat that hops it at once", "Spikes_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_1))", "hop the block at once", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                // PAX-107 (D-124): the wall is spiked; it kills a cat that waits at the shaft.
                new Betrayal("T3: S2's spiked hinge wall reaches a cat that waits at the shaft", "Hinge_S2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=20.3)", "wait for the wall", Release(), Until(Dead()))),
                new Betrayal("T4: the waterfall's first wave meets a cat that runs straight into the shaft", "Fall_E0", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=20.3)", "run straight in", Until(Dead()))),
                new Betrayal("T5: Spikes_4, up under the hinge floor's hole, meet a cat that runs on along S1", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "run on", Hold(Left), Until(Dead()))),
                new Betrayal("T6: S1's hinge floor pushes a cat that stops past Spikes_4 onto Spikes_5", "Spikes_5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=15.7)", "stop where it lands", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Hinge_S1"),
                // PAX-107 round three: the crushers.
                new Betrayal("P1: Press_S1 comes down on a cat that runs under it as it starts down", "Press_S1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Press_S1))", "run under it", Hold(Left), Until(Dead()))),
                new Betrayal("P2: Piston_G carries a cat that stops on it up into S1_W", "Piston_G", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Piston_G))", "stop on it", Hold(Right), Until(XAtLeast(5.75f)), Release(), Until(Dead()))),
                new Betrayal("T7: Block_6 comes down on a cat that runs on along the ground", "Block_6", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Piston_G))", "run on", Hold(Right), Until(Dead()))),
                new Betrayal("T9: a cat that runs on without waiting for Ride_A falls into the first pit", "Pit10_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Block_6))", "run on", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Ride_A"),
                // PAX-107 round three: the climb and the maze.
                Betrayal.Recovers("The climb out of the shaft and the maze: the other way down", "Hinge_S2", maze),
                new Betrayal("M1: Press_1 comes down on a cat that stops under it", "Press_1", DeathCause.Hazard,
                    Route.PrefixOf(maze, "Until(Home(Press_1))", "stop under it", Hold(Right), Until(XAtLeast(26.6f)), Release(), Until(Dead()))),
                new Betrayal("M2: Spikes_M2 are up under a cat that runs on along Maze_2", "Spikes_M2", DeathCause.Hazard,
                    Route.PrefixOf(maze, "Until(GroundedOn(Maze_2))", "run on", Hold(Left), Until(Dead()))),
                new Betrayal("M3: Piston_3 carries a cat that stops on it up into Maze_2", "Piston_3", DeathCause.Hazard,
                    Route.PrefixOf(maze, "Until(Home(Piston_3))", "stop on it", Hold(Right), Until(XAtLeast(26.25f)), Release(), Until(Dead()))),
                new Betrayal("M4: a cat that runs on off Maze_3 lands on Spikes_E", "Spikes_E", DeathCause.Hazard,
                    Route.PrefixOf(maze, "Until(Home(Piston_3))", "run on", Hold(Right), Until(Dead()))),
                new Betrayal("Dead end: east along S1 toward the door", "Spikes_E", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S1))", "east toward the door", Hold(Right), Until(Dead()))),
                new Betrayal("Dead end: the hole behind the start", "Pit9_Hazard", DeathCause.Hazard,
                    new Route("the way down", Hold(Left), Until(Dead())), revealedBy: "Floor_9"));
        }
    }
}
