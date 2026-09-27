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
                // Rhythm: count Spikes_S down and cross; onto G_1's vent; steer left onto S2 once above it (not right onto Ledge_2).
                Hold(Right), Until(XAtLeast(18.1f)), Release(), Until(Still()), Until(S1Sinks), Hold(Right).Timed(TimedMode.Shift),
                Until(XAtLeast(27.9f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(4.1f)), Hold(Left).Timed(TimedMode.Shift), Until(GroundedOn("S2")),
                // G_A erupts as the cat arrives: stop short of it, pass it once it stops, and wait on G_B's vent.
                Until(XAtMost(11.8f)), Release(), Until(Still()),
                Until(GAStopped), Hold(Left), Until(XAtMost(6.1f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(8.1f)), Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("S3")),
                // Sync: on to G_C's vent and wait there (Block_C drops beside it); G_C lifts the cat to S4; west past G_S1.
                Until(XAtLeast(19.9f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(12.1f)), Hold(Left).Timed(TimedMode.Shift), Until(GroundedOn("S4_W")),
                Until(XAtMost(2.3f)), Release(), Until(Still()),
            }.Concat(Stack()).ToArray());

            RouteStep[] tail = Stack().Select(s => s.Timing == TimedMode.None ? s : s.Timed(TimedMode.None)).ToArray();
            return new RoomRoutes(solution,
                new Betrayal("T1 [OL]: a cat that steers G_1's launch right, towards the door, lands on Ledge_2, which gives way over Spikes_1", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Y>=4.1)", "steer right", Hold(Right), Until(Dead())), revealedBy: "Ledge_2"),
                new Betrayal("T2 [W]: a cat that walks on into G_A while it erupts is thrown into Spikes_A", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=11.8)", "walk on", Until(Dead()))),
                new Betrayal("T3 [SS]: a cat that waits beside G_C instead of on its vent is crushed by Block_C", "Block_C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(S3))", "wait beside G_C", Until(XAtLeast(18f)), Release(), Until(Dead()))),
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
                        Until(GroundedOn("S4_W")), Hold(Right), Until(XAtLeast(2.2f)), Release(), Until(Still()) }.Concat(tail).ToArray())),
                new Betrayal("Dead end D2 [OL]: G_B's launch steered left lands on S3's west ledge, over Spikes_D2", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Y>=8.1)", "steer left", Hold(Left), Until(Dead()))));
        }
    }
}
