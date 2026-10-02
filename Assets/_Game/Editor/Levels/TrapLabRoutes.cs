using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): Trap Lab room 3 (the arrows, D-078). Not a level, so not in LevelRoutes.
    // PAX-080 (D-080): Trap Lab room 4, the troll-route room.
    // PAX-084 (D-086): Trap Lab room 6, the spear room.
    // PAX-085 (D-087): Trap Lab room 7, the inverter room.
    // PAX-086 (D-088): Trap Lab room 8, the geyser room.
    // PAX-087 (D-089): Trap Lab room 9, the vine room.
    // PAX-088 (D-090): Trap Lab room 10, the storm cloud room.
    // PAX-090 (D-091): Trap Lab room 11, the checkpoint-section room.
    // PAX-099 (D-106): Trap Lab room 13, the angled-arrow room.
    public static class TrapLabRoutes
    {
        public static RoomRoutes Room3()
        {
            var solution = new Route("Trap Lab room 3 solution",
                Hold(Right), Until(XAtLeast(6.2f)), Release(), Until(Still()),
                Until(Stopped("ArrowA")),
                // D-056 (1): the crossing after ArrowA has stopped, measured from rest.
                Hold(Right).Timed(TimedMode.Hesitate), Jump(), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(8.5f))),
                Until(XAtLeast(13.2f)), Jump(), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(14.6f))),
                Until(XAtLeast(16.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Floor").And(XAtLeast(17.6f))),
                // PAX-082 (D-082): the 0.6 PillarB is cleared by a take-off at x 20.6 with the lower jump.
                Until(XAtLeast(20.6f)), Jump(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("ArrowB fires at the shins of a cat that runs past x 17", "ArrowB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor) && X>=14.6)", "run past ArrowB's trigger", Until(Dead()))),
                new Betrayal("ArrowC fires at the head of a cat that jumps into its band", "ArrowC", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor) && X>=14.6)", "jump into ArrowC's band", Until(XAtLeast(17.4f)), Jump(), Until(Dead()))));
        }

        public static RoomRoutes Room4()
        {
            // Jump before Stone_A's touch skin, clear Thin_Collapse, and leave Up_2 before Ledge_End's skin.
            var solution = new Route("Trap Lab room 4 solution",
                // PAX-082 (D-082): take-offs for the rescaled room and the lower jump.
                Hold(Right), Until(XAtLeast(4f)), Jump(), Until(GroundedOn("Up_1")),
                Until(XAtLeast(8.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Up_2")),
                Until(XAtLeast(13.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Stone_A drops a cat that walks on off the start floor", "Pit_Hazard", DeathCause.Hazard,
                    new Route("walk onto Stone_A", Hold(Right), Until(Dead())), revealedBy: "Stone_A"),
                new Betrayal("Thin_Collapse drops a cat that stops on it", "Pit_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_1))", "hop onto Thin_Collapse",
                        Until(XAtLeast(8.4f)), Jump(), Until(XAtLeast(9.8f)), Release(), Until(GroundedOn("Thin_Collapse")), Until(Dead())),
                    revealedBy: "Thin_Collapse"),
                Betrayal.Recovers("Ledge_End drops a cat that walks on off Up_2 into the Gutter, which climbs back to Up_2", "Ledge_End",
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_2))", "step off Up_2 onto Ledge_End, then climb back",
                        Until(GroundedOn("Gutter")), Release(), Until(Still()),
                        // PAX-082 (D-082): the climb back jumps straight up from under the gap left of the perch, then steers onto Up_2.
                        Hold(Left), Until(XAtMost(14.8f)), Release(), Until(Still()), Jump(), For(8), Hold(Left), Until(GroundedOn("Up_2")), Release(), Until(Still()),
                        Hold(Right), Until(XAtLeast(13.3f)), Jump(), Until(RoomComplete()))),
                new Betrayal("The Bridge goes after ArrowD fires over a cat that runs on from the Gutter", "Pit_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Up_2))", "run on over the Bridge", Until(GroundedOn("Gutter")), Until(Dead())),
                    revealedBy: "ArrowD"));
        }

        // PAX-076 (D-083): Trap Lab room 5, the precision room. Full speed along P1-P8 (each jump once the cat is a
        // platform-width-minus-one short of the edge), a walk off P8 onto the Step, and the precision jump up to the Exit.
        public static RoomRoutes Room5()
        {
            var steps = new List<RouteStep> { Hold(Right), Until(XAtLeast(3.8f)), Jump(), Until(Airborne()), Until(GroundedOn("P1")) };
            float[] right = { 8.9f, 13.1f, 17f, 21.2f, 25.1f, 29.3f, 33.2f };
            for (int i = 0; i < right.Length; i++)
            {
                RouteStep jump = i == 2 ? Jump().Timed(TimedMode.Shift) : Jump();
                steps.AddRange(new[] { Until(XAtLeast(right[i] - 1f)), jump, Until(Airborne()), Until(GroundedOn("P" + (i + 2))) });
            }
            steps.AddRange(new[] { Until(GroundedOn("Step")), Until(XAtLeast(41f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(RoomComplete()) });
            var solution = new Route("Trap Lab room 5 solution", steps.ToArray());

            return new RoomRoutes(solution,
                new Betrayal("Block_P5 falls on a cat that stops on P5", "Block_P5", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(P5))", "stop on P5", Release(), Until(Dead()))));
        }

        // PAX-084 (D-086): Trap Lab room 6, the spear room. Cross the cut and run on: the cat drops into the Dip (under every
        // lane) and waits there until the whole volley has stuck (a spear sits still at the mouth for its 8-tick tell, so each
        // wait is for its fire, 9 ticks into the flight, then its stop). Then climb: out of the Dip, Spear_1, Spear_2, Spear_3, FarWall. Each climb jumps straight up
        // first, clearing the spear above, then steers onto the next step and stops short of the one after.
        public static RoomRoutes Room6()
        {
            var solution = new Route("Trap Lab room 6 solution",
                Hold(Right), Until(GroundedOn("Dip")), Release(), Until(Still()), Until(Fired("Spear_3")), For(9), Until(Stopped("Spear_3")),
                // The climb starts once the volley has stuck; the window measures how early it could.
                Jump().Timed(TimedMode.Shift), For(4), Hold(Right), Until(XAtLeast(6.6f)), Release(), Until(GroundedOn("Floor_B")), Until(Still()),
                Hold(Right), Until(XAtLeast(7.6f)), Release(), Until(Still()),
                Jump(), For(5), Hold(Right), Until(XAtLeast(8.6f)), Release(), Until(GroundedOn("Spear_1_Shaft")), Until(Still()),
                Jump(), For(8), Hold(Right), Until(XAtLeast(10f)), Release(), Until(GroundedOn("Spear_2_Shaft")), Until(Still()),
                Jump(), For(8), Hold(Right), Until(XAtLeast(11f)), Release(), Until(GroundedOn("Spear_3_Shaft")), Until(Still()),
                Jump(), For(8), Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                // "Standing in a lane": hop the Dip like a gap and Spear_1, at shin height over the floor, runs through you.
                new Betrayal("Spear_1 runs through a cat that hops over the Dip", "Spear_1", DeathCause.Hazard,
                    new Route("hop over the Dip", Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(XAtLeast(6.8f)), Release(), Until(GroundedOn("Floor_B")), Until(Dead()))),
                new Betrayal("Spear_2 hits a cat that climbs onto Spear_1 as soon as it sticks", "Spear_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Still)", "climb as soon as Spear_1 sticks",
                        Until(Fired("Spear_1")), For(9), Until(Stopped("Spear_1")),
                        Jump(), For(4), Hold(Right), Until(XAtLeast(6.6f)), Release(), Until(GroundedOn("Floor_B")), Until(Still()),
                        Hold(Right), Until(XAtLeast(7.6f)), Release(), Until(Still()),
                        Jump(), For(5), Hold(Right), Until(XAtLeast(8.6f)), Release(), Until(Dead()))));
        }

        // PAX-085 (D-087): Trap Lab room 7, the inverter room. Hop Spikes_Back and run into the Inverter; then stop and wait
        // out the 150 inverted steps (the step after Until(Fired) is the first inverted one), go right again, and jump the pit.
        // The window measures how early the run can restart while still inverted.
        public static RoomRoutes Room7()
        {
            var solution = new Route("Trap Lab room 7 solution",
                Hold(Right), Until(XAtLeast(4f)), Jump(), Until(Airborne()), Until(Fired("Inverter")),
                Release(), For(150),
                Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(10.4f)), Jump(), Until(Airborne()), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("A cat that keeps holding right after the Inverter runs back into Spikes_Back", "Spikes_Back", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Inverter))", "keep holding right", Until(Dead())), revealedBy: CatInverted),
                Betrayal.Recovers("A cat that plays it inverted (holds left to go right) jumps the pit and reaches the door", CatInverted,
                    Route.PrefixOf(solution, "Until(Fired(Inverter))", "play it inverted",
                        Hold(Left), Until(XAtLeast(10.4f)), Jump(), Until(Airborne()), Until(RoomComplete()))));
        }

        // PAX-086 (D-088): Trap Lab room 8, the geyser room. Walk onto the vent and wait: the eruption launches the cat. Ten
        // ticks into the flight, steer left over the Ledge's edge, land on it and run on to the door. The window measures how
        // early and late the steer can start.
        public static RoomRoutes Room8()
        {
            var solution = new Route("Trap Lab room 8 solution",
                Hold(Right), Until(XAtLeast(8.4f)), Release(), Until(Still()), Until(Airborne()),
                For(10), Hold(Left).Timed(TimedMode.Shift), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("A launch steered right rises into the Ceiling_Spikes", "Ceiling_Spikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Airborne)", "steer the launch right", Hold(Right), Until(Dead()))),
                Betrayal.Recovers("A cat that steps on the vent during the tell and walks off is left under the Ledge, and rides the next eruption", "Geyser",
                    Route.PrefixOf(solution, "Until(Still)", "walk off the vent during the tell, then come back",
                        Until(Fired("Geyser")), Hold(Left), Until(XAtMost(6.5f)), Release(), Until(Still()), For(50),
                        Hold(Right), Until(XAtLeast(8.4f)), Release(), Until(Still()), Until(Airborne()),
                        For(10), Hold(Left), Until(RoomComplete()))));
        }

        // PAX-087 (D-089): Trap Lab room 9, the vine room. Walk right pushing up: the cat grabs Vine_Real from the ground,
        // climbs to its top (collider top at the vine's 5.1, its bottom at 4.54, just above the Cliff) and leaps right onto the Cliff. The leap is the
        // timed step: how much earlier (lower on the vine) or later it can go. ReleaseClimb() after the leap, so the cat in
        // the air never grabs Vine_Obvious. The betrayals push up only once airborne: up on the jump tick would grab from
        // the ground, and a jump press on a grab tick leaps (D-089).
        public static RoomRoutes Room9()
        {
            var solution = new Route("Trap Lab room 9 solution",
                Hold(Right), Hold(Up), Until(Climbing()), Until(YAtLeast(4.8f)),
                Jump().Timed(TimedMode.Shift), ReleaseClimb(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("The obvious vine next to the Cliff snaps halfway up and drops the cat onto the PitHazard", "PitHazard", DeathCause.Hazard,
                    new Route("climb the obvious vine",
                        Hold(Right), Until(XAtLeast(8.6f)), Jump(), Until(Airborne()), Hold(Up), Until(Climbing()), Until(Dead())),
                    revealedBy: "Vine_Obvious"),
                Betrayal.Recovers("A cat that leaps back off the obvious vine once it has touched the snap sees it go, and takes the real vine", "Vine_Obvious",
                    new Route("touch the snap, leap back, take the real vine",
                        Hold(Right), Until(XAtLeast(8.6f)), Jump(), Until(Airborne()), Hold(Up), Until(Climbing()), Until(YAtLeast(1.8f)),
                        Hold(Left), Jump(), ReleaseClimb(), Until(Grounded()), Until(Fired("Vine_Obvious")),
                        Hold(Right), Hold(Up), Until(Climbing()), Until(YAtLeast(4.8f)), Jump(), ReleaseClimb(), Until(RoomComplete()))));
        }

        // PAX-088 (D-090): Trap Lab room 10, the storm cloud room. The solution never stops: the cloud (0.08 u a tick) can't
        // keep up with a running cat (0.12), so every strike lands behind it. The jump onto the Rise is the timed step. Both
        // betrayals stop after waking the cloud; it settles over the cat and the first strike (wake + 75) kills it. The
        // revealing element is the cloud itself: its first visible change is its first follow step (wake + 1).
        public static RoomRoutes Room10()
        {
            var solution = new Route("Trap Lab room 10 solution",
                Hold(Right), Until(XAtLeast(10f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Rise")), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("A cat that stops once the cloud wakes is struck where it stands", "StormCloud", DeathCause.Hazard,
                    new Route("stand still after waking the cloud", Hold(Right), Until(Fired("StormCloud")), Release(), Until(Dead()))),
                new Betrayal("A cat that shelters under Fake_Overhang is struck through it", "StormCloud", DeathCause.Hazard,
                    new Route("wait under the fake overhang", Hold(Right), Until(XAtLeast(7.2f)), Release(), Until(Dead()))));
        }

        // PAX-090 (D-091): Trap Lab room 11, three checkpoint sections. The solution: jump the collapse; stop under the spear
        // lane and wait for Spear_1 to stick; climb it onto the Step (waking the cloud) and run through the Act2 gate to the
        // Overhang; wait there for the Geyser's eruption to end, then run under Vent_Roof and through the Act3 gate; hop
        // Spikes_Back, touch the Inverter and wait out its 150 steps; climb Vine_Real and leap onto the Cliff. One betrayal
        // per act; Act2's and Act3's start from their gate (Route.FromSection: replay to it, die, rewind).
        public static RoomRoutes Room11()
        {
            var solution = new Route("Trap Lab room 11 solution",
                Hold(Right), Until(XAtLeast(7.2f)), Jump(), Until(Airborne()), Until(GroundedOn("Floor_B")),
                Until(XAtLeast(15.1f)), Release(), Until(Still()), Until(Fired("Spear_1")), For(9), Until(Stopped("Spear_1")),
                Jump(), For(5), Hold(Right), Until(XAtLeast(16.9f)), Release(), Until(GroundedOn("Spear_1_Shaft")), Until(Still()),
                Jump(), For(8), Hold(Right), Until(GroundedOn("Step")),
                Until(XAtLeast(27.8f)), Release(), Until(Still()),
                Until(Moving("Geyser").And(Home("Geyser"))),
                Hold(Right), Until(XAtLeast(44f)), Jump(), Until(Airborne()), Until(GroundedOn("Floor_C").And(XAtLeast(46.5f))),
                Until(Fired("Inverter")), Release(), For(150),
                Hold(Right), Hold(Up), Until(Climbing()), Until(YAtLeast(4.8f)), Jump(), ReleaseClimb(), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Collapse_1 drops a cat that walks on over the pit", "Pit_Hazard", DeathCause.Hazard,
                    new Route("walk onto Collapse_1", Hold(Right), Until(Dead())), revealedBy: "Collapse_1"),
                new Betrayal("Spear_1 runs through a cat that jumps at the Step as it flies in", "Spear_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_B))", "jump at the Step's face",
                        Until(XAtLeast(17f)), Release(), Until(Fired("Spear_1")), For(8), Jump(), Until(Dead()))),
                new Betrayal("From the Act2 checkpoint: a cat that waits on the vent is launched into Vent_Spikes", "Vent_Spikes", DeathCause.Hazard,
                    Route.FromSection(solution, "Act2", "wait on the vent", Hold(Right), Until(XAtLeast(31.6f)), Release(), Until(Dead())), revealedBy: "Geyser"),
                new Betrayal("From the Act3 checkpoint: a cat that keeps holding right after the Inverter runs back into Spikes_Back", "Spikes_Back", DeathCause.Hazard,
                    Route.FromSection(solution, "Act3", "keep holding right",
                        Hold(Right), Until(XAtLeast(44f)), Jump(), Until(Airborne()), Until(GroundedOn("Floor_C").And(XAtLeast(46.5f))),
                        Until(Fired("Inverter")), Until(Dead())),
                    revealedBy: CatInverted));
        }

        // PAX-093 (D-095): Trap Lab room 12, one of each moving floor. The solution: board Mover at home and ride it over P1;
        // land on Slider and jump again at once (it slides away the next tick); hop on and off Drop before its 20 ticks run out;
        // run the Shrink bridge without stopping (its edge chases at 5 u/s); cross the cut at x 58, which sends Pusher back at
        // you, jump up onto Ledge_P and let it pass under; then on to the door. One betrayal per pattern; the second and third
        // sections' start from their gate (Route.FromSection).
        public static RoomRoutes Room13()
        {
            var solution = new Route("Trap Lab room 13 solution",
                // Past ArrowD45's cut, stop, and let it land; then walk on through it (a stopped arrow is harmless).
                Hold(Right), Until(XAtLeast(7.3f)), Release(), Until(Still()), Until(Moving("ArrowD45")), For(8), Until(Stopped("ArrowD45")),
                // Wait short of the Stump for a shot of ArrowU30 to stop in the ceiling, then hop the Stump.
                Hold(Right), Until(XAtLeast(17.6f)), Release(), Until(Still()), Until(Moving("ArrowU30")), For(8), Until(Stopped("ArrowU30")),
                Hold(Right).Timed(TimedMode.Hesitate), Until(XAtLeast(18.1f)), Jump(), Until(Airborne()), Until(Grounded()),
                // Past ArrowD60's cut, stop, and let it land; then on to the door.
                Until(XAtLeast(23.8f)), Release(), Until(Still()), Until(Moving("ArrowD60")), For(8), Until(Stopped("ArrowD60")),
                Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("ArrowD45 comes down on a cat that runs on past its cut", "ArrowD45", DeathCause.Hazard,
                    new Route("run on", Hold(Right), Until(Dead()))),
                new Betrayal("ArrowU30's next shot strikes a cat that walks up to the Stump", "ArrowU30", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(ArrowD45))", "walk up to the Stump", Hold(Right), Until(Dead()))),
                new Betrayal("ArrowD60 comes down on a cat that runs on past its cut", "ArrowD60", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "run on", Until(Dead()))));
        }

        public static RoomRoutes Room12()
        {
            var solution = new Route("Trap Lab room 12 solution",
                Hold(Right), Until(XAtLeast(7f)), Release(), Until(Still()),
                Until(XAtLeast(16.3f)),
                Hold(Right), Until(GroundedOn("F1")), Until(XAtLeast(23f)), Jump(), Until(GroundedOn("Slider")), Jump().Timed(TimedMode.Shift), Until(GroundedOn("F2")),
                Until(XAtLeast(36.6f)), Jump(), Until(GroundedOn("Drop")), Jump().Timed(TimedMode.Shift), Until(GroundedOn("F3")),
                Until(GroundedOn("F5")),
                Until(XAtLeast(57.9f)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Ledge_P")), Release(), Until(Still()),
                For(70),
                Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("Mover: a cat that walks on once the Mover has left falls into P1", "P1_Hazard", DeathCause.Hazard,
                    new Route("walk on after the mover", Until(Moving("Mover")), Hold(Right), Until(Dead())), revealedBy: "Mover"),
                new Betrayal("Slider: a cat that stops on the Slider drops as it slides away", "P2_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Slider))", "stop on the slider", Release(), Until(Dead())), revealedBy: "Slider"),
                new Betrayal("From the Sink checkpoint: a cat that stops on the Drop falls with it", "P3_Hazard", DeathCause.Hazard,
                    Route.FromSection(solution, "Sink", "stop on the drop", Hold(Right), Until(XAtLeast(36.6f)), Jump(), Until(GroundedOn("Drop")), Release(), Until(Dead())),
                    revealedBy: "Drop"),
                new Betrayal("From the Sink checkpoint: a cat that stops on the Shrink bridge falls as its edge catches up", "P4_Hazard", DeathCause.Hazard,
                    Route.FromSection(solution, "Sink", "stop on the bridge", Hold(Right), Until(XAtLeast(36.6f)), Jump(), Until(GroundedOn("Drop")), Jump(), Until(GroundedOn("F3")),
                        Until(XAtLeast(50f)), Release(), Until(Dead())),
                    revealedBy: "Shrink"),
                new Betrayal("From the Shove checkpoint: a cat that stays on the floor is shoved back into P4", "P4_Hazard", DeathCause.Hazard,
                    Route.FromSection(solution, "Shove", "stay on the floor", Hold(Right), Until(XAtLeast(58.5f)), Release(), Until(Dead())),
                    revealedBy: "Pusher"));
        }

        // PAX-076 (D-083) §2.5: the bait gap attempted from its best take-off: full speed off P8's edge, the jump in the
        // coyote window. It dies in the pit. Not a Betrayal: nothing reveals (the gap is visible all along), so
        // RouteValidator's lead has nothing to measure; TrapLabRoom5Tests replays it on its own.
        public static Route Room5BaitAttempt() =>
            Route.PrefixOf(Room5().Solution, "Until(GroundedOn(P8))", "Trap Lab room 5: jump the bait gap from P8's edge",
                Until(Airborne()), For(3), Jump(), Until(Dead()));
    }
}
