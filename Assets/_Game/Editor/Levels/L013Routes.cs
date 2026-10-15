using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using static Parallax.Editor.Levels.L013Layout;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 13's routes. The cat waits on a vent until the eruption launches it (Until(Airborne)), and
    // waits for a rhythm on the room tick (the layout's timeline), which a section rewind restores with the room.
    static class L013Routes
    {
        // True on the ticks `tick` + k × period.
        static RouteCondition Beat(string label, int period, int tick) => new(label, v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);
        // Rising fast above y: a launch that started above it.
        static RouteCondition LaunchedAbove(float y) => new($"launched above {y}", v => v.Last.Y > y && v.Last.Vy > 13f);

        static readonly RouteCondition S1Sinks = Beat("Spikes_S sink", 100, S1Down);
        static readonly RouteCondition GAStopped = Beat("G_A stopped", 100, GAErupt + 40);
        static readonly RouteCondition SyncBeat = Beat("the sync", 300, SyncTick - 5);
        static readonly RouteCondition G_S1Beat = Beat("G_S1's next", 100, SyncTick - 5);
        // PAX-107: G_S1's eruption (40 ticks) is over: the walk west past its vent, after Lift_4.
        static readonly RouteCondition G_S1Stopped = Beat("G_S1 stopped", 100, SyncTick + 40);
        static readonly RouteCondition C1Sinks = Beat("Spikes_C1 sink", 100, C1Down);
        static readonly RouteCondition C2Sinks = Beat("Spikes_C2 sink", 150, C2Down);

        // From the sync to the door: the stack (holding right through the three launches), then the corridor.
        static RouteStep[] Stack() => new[] {
            Until(SyncBeat), Hold(Right).Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("S5_A")),
            // Top: (still holding right) through the gate; let go over the notch and drop into the corridor.
            Until(XAtLeast(18.9f)), Release(), Until(GroundedOn("Corr_Floor")), Until(Still()),
            Until(C1Sinks), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(23.6f)), Release(), Until(Still()),
            Until(C2Sinks), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(27.9f)), Release(), Until(Still()),
            Until(Airborne()), Until(YAtLeast(21.1f)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        public static RoomRoutes Build()
        {
            var solution = new Route("L013 solution", new RouteStep[] {
                // Rhythm (PAX-107): left and right are swapped until G_1's launch (D-141: Invert at the start); route moves are
                // stick input, so every Hold(Left) here walks the cat east (D-087 (7)). Jump Collapse_1; let Slam_1 come down and
                // hop it; wait short of Lift_1, count Spikes_S down and cross (over Lift_1) onto Ferry_1; ride it over Pit_M; onto
                // G_1's vent; steer left onto S2 once above it (the launch has ended the swap).
                Hold(Left), Until(XAtLeast(4.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Ground")),
                Until(XAtLeast(12f)), Release(), Until(Still()), Until(Stopped("Slam_1")),
                Hold(Left), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
                Until(Home("Lift_1")), Until(S1Sinks), Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(22.1f)), Release(), Until(GroundedOn("Ferry_1")), Until(Still()),
                Until(XAtLeast(25.4f)), Hold(Left), Until(XAtLeast(27.9f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(4.1f)), Hold(Left).Timed(TimedMode.Shift), Until(GroundedOn("S2")),
                // S2: off the landing at once (Slam_2); stop on S2 short of Collapse_2 and Lift_2; once Lift_2 is back down,
                // cross both and jump Slam_1's hole.
                Until(XAtMost(23.8f)), Release(), Until(Still()), Until(Moving("Lift_2")), Until(Home("Lift_2")),
                Hold(Left), Until(XAtMost(15.5f)), Jump(), Until(Airborne()), Until(Grounded()),
                // G_A erupts as the cat arrives: stop short of it, pass it once it stops, and wait on G_B's vent.
                Until(XAtMost(11.8f)), Release(), Until(Still()),
                Until(GAStopped), Hold(Left), Until(XAtMost(6.1f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(8.1f)), Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("S3")),
                // PAX-107: Hinge_3 stands up behind: keep going, jump Spikes_3.
                Until(XAtLeast(12.9f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("S3_B")),
                // Sync: on to G_C's vent and wait there (Block_C drops beside it); G_C lifts the cat to S4; west past G_S1.
                Until(XAtLeast(19.9f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(12.1f)), Hold(Left).Timed(TimedMode.Shift), Until(Grounded().And(YAtLeast(12f))),
                // PAX-107 round fifteen: Hinge_S4 stands up behind: keep going, jump Spikes_4.
                Until(XAtMost(14.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("S4_W")),
                // PAX-107: Lift_4 rises ahead: stop short, go once it's back down.
                Until(XAtMost(9.6f)), Release(), Until(Still()), Until(Moving("Lift_4")), Until(Home("Lift_4")),
                // Then short of G_S1's vent until its eruption is over, and past it.
                Hold(Left), Until(XAtMost(5.3f)), Release(), Until(Still()), Until(G_S1Stopped),
                Hold(Left), Until(XAtMost(2.3f)), Release(), Until(Still()),
            }.Concat(Stack()).ToArray());

            RouteStep[] tail = Stack().Select(s => s.Timing == TimedMode.None ? s : s.Timed(TimedMode.None)).ToArray();
            return new RoomRoutes(solution,
                new Betrayal("T12 [J]: a cat that walks on from the start drops through Collapse_1 into Pit_1", "Pit_1", DeathCause.Hazard,
                    new Route("walk on", Hold(Left), Until(Dead())), revealedBy: "Collapse_1"),
                new Betrayal("T11 [SS]: a cat that walks on along the ground is crushed by Slam_1, the floor above coming down", "Slam_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground))", "walk on", Until(Dead()))),
                new Betrayal("T1 [B]: a cat that waits for Spikes_S at their edge is lifted into S2 by Lift_1", "Lift_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Slam_1))", "wait at the edge", Hold(Left), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(18.1f)), Release(), Until(Dead()))),
                new Betrayal("T13 [NJ]: a cat that jumps for the far side of Pit_M instead of riding Ferry_1 falls in", "Pit_M", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Spikes_S sink)", "jump across", Hold(Left), Until(XAtLeast(21.3f)), Jump(), Until(Dead())), revealedBy: "Ferry_1"),
                new Betrayal("T14 [NW]: a cat that stays where G_1's launch lands it is crushed by Slam_2", "Slam_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2))", "stay", Release(), Until(Dead()))),
                new Betrayal("T14 [NW]: a cat that waits for Lift_2 on Collapse_2 drops with it into Pit_M", "Pit_M", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2))", "wait on it", Until(XAtMost(22.4f)), Release(), Until(Dead())), revealedBy: "Collapse_2"),
                new Betrayal("T11 [SS]: a cat that walks on along S2 is lifted into S3 by Lift_2", "Lift_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S2))", "walk on", Until(Dead()))),
                new Betrayal("T3 [SS]: a cat that waits beside G_B instead of on its vent is skewered by Spike_2", "Spike_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(G_A stopped)", "wait beside G_B", Hold(Left), Until(XAtMost(7.35f)), Release(), Until(Dead()))),
                new Betrayal("T2 [W]: a cat that walks on into G_A while it erupts is thrown into Spikes_A", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=11.8)", "walk on", Until(Dead()))),
                new Betrayal("T10 [NW]: a cat that stops short of the jump at the start is pushed into Pit_1 by Hinge_1", "Pit_1", DeathCause.Hazard,
                    new Route("stop at the edge", Hold(Left), Until(XAtLeast(4.2f)), Release(), Until(Dead())), revealedBy: "Hinge_1"),
                new Betrayal("T10 [NW]: a cat that stops on S4 is pushed onto Spikes_4 by Hinge_S4", "Spikes_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=14.3)", "stop on S4", Release(), Until(Dead())), revealedBy: "Hinge_S4"),
                new Betrayal("T10 [NW]: a cat that stops on S3 is pushed onto Spikes_3 by Hinge_3", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=12.9)", "stop", Release(), Until(Dead())), revealedBy: "Hinge_3"),
                new Betrayal("T3 [SS]: a cat that waits beside G_C instead of on its vent is crushed by Block_C", "Block_C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S3_B))", "wait beside G_C", Until(XAtLeast(18f)), Release(), Until(Dead()))),
                new Betrayal("T11 [SS]: a cat that walks on along S4 is lifted into Pad_2 by Lift_4", "Lift_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=9.6)", "walk on", Hold(Left), Until(Dead()))),
                new Betrayal("T4 [W]: a cat that rides G_S1 off the beat lands on Spikes_P2", "Spikes_P2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=2.3)", "ride off the beat", Release(), Until(Still()), Until(G_S1Beat), Hold(Right), Until(Dead()))),
                new Betrayal("T5 [NW]: a cat that rides G_S3 straight up is blown back down by G_D onto Spikes_P3", "Spikes_P3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(the sync)", "straight up", Hold(Right), Until(LaunchedAbove(18.2f)), Release(), Until(Dead())), revealedBy: "G_D"),
                new Betrayal("T6 [OL]: a cat that lands on the nearer ledge past G_S3 falls through Ledge_5 into Pit_F", "Pit_F", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(the sync)", "the nearer ledge", Hold(Right), Until(LaunchedAbove(18.2f)), For(12), Release(), Until(Dead())), revealedBy: "Ledge_5"),
                new Betrayal("T7 [LW]: a cat that walks straight on over the notch is thrown by G_G into Spikes_G", "Spikes_G", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S5_A))", "straight on", Until(XAtLeast(17.9f)), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                new Betrayal("T8 [W]: a cat that runs into the corridor without counting meets Spikes_C1", "Spikes_C1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Corr_Floor))", "run in", Hold(Right), Until(Dead()))),
                new Betrayal("T9 [SS]: a cat that runs on over the safe tile meets Spikes_C2", "Spikes_C2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=23.6)", "run on", Until(Dead()))),
                Betrayal.Recovers("Dead end D1 [B]: an off-beat ride steered left lands on the Alcove, which gives way and drops the cat back to S4", "Alcove",
                    Route.PrefixOf(solution, "Until(X<=2.3)", "the Alcove", new RouteStep[] {
                        Release(), Until(Still()), Until(G_S1Beat), Hold(Right), Until(Airborne()), Hold(Left), Until(GroundedOn("Alcove")),
                        Until(GroundedOn("S4_A")), Hold(Right), Until(XAtLeast(2.2f)), Release(), Until(Still()) }.Concat(tail).ToArray())),
                new Betrayal("Dead end D2 [OL]: G_B's launch steered left lands on S3's west ledge, over Spikes_D2", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Y>=8.1)", "steer left", Hold(Left), Until(Dead()))));
        }
    }
}
