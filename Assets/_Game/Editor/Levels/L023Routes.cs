using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-167: level 23's routes. A ride is boarded by its beat (a few ticks after it gets home) and left by its pose (moving, stopped).
    // Betrayals branch off the solution's own steps.
    static class L023Routes
    {
        // A shuttle (or lift) has been home `ticks` ticks: it leaves at `phase`, out over `move`, waits `hold`, back over `move`.
        static RouteCondition HomeFor(int period, int phase, int move, int hold, int ticks) => Beat(period, phase + 2 * move + hold + ticks);
        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // ---- Docks, storey 1, walked east ----
        // Off Pier_0 onto Ferry_1 at once; ride it out and jump to Isle_1 at its far end. Onto Ferry_2 and stand; once it has
        // stalled, jump to Isle_2. Wait for Lift_U at home, step on, ride it up.
        static RouteStep[] Docks() => new[] {
            Hold(Right), Until(XAtLeast(4.3f)), Release(), Until(Still()),
            Until(Beat(L023Layout.Ferry1Period, L023Layout.Ferry1Phase + L023Layout.Ferry1Move)), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(16.5f)), Release(), Until(Still()),
            Until(Fired("Ferry_2")), Until(Moving("Ferry_2")), Until(Stopped("Ferry_2")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(27.8f)), Release(), Until(Still()),
            Until(HomeFor(L023Layout.LiftPeriod, L023Layout.LiftUPhase, L023Layout.LiftMove, L023Layout.LiftHold, 3)), Hold(Right), Until(XAtLeast(29.2f)), Release(), Until(Still()),
            Until(Moving("Lift_U")), Until(Stopped("Lift_U")),
        };

        // ---- Gallery, storey 2, walked west ----
        // Off the lift and along Dock_2; jump onto Sink_3 and straight on to Isle_3. Wait for Ride_4 at home, step on, ride it out
        // and step off onto Dock_3. Wait for Lift_U2 at home, step on, ride it up.
        static RouteStep[] Gallery() => new[] {
            Hold(Left), Until(XAtMost(20.4f)), Jump(), Until(Airborne()), Until(Grounded()), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(10f)), Release(), Until(Still()),
            Until(HomeFor(L023Layout.Ride4Period, L023Layout.Ride4Phase, L023Layout.RideMove, 20, 3)), Hold(Left), Until(XAtMost(8.4f)), Release(), Until(Still()),
            Until(Moving("Ride_4")), Until(Stopped("Ride_4")), Hold(Left), Until(XAtMost(2.6f)), Release(), Until(Still()),
            Until(HomeFor(L023Layout.LiftPeriod, L023Layout.LiftU2Phase, L023Layout.LiftMove, L023Layout.LiftHold, 3)), Hold(Left), Until(XAtMost(.9f)), Release(), Until(Still()),
            Until(Moving("Lift_U2")), Until(Stopped("Lift_U2")),
        };

        // ---- Crown, storey 3, walked east ----
        // Wait for Ride_5a at home, step on, ride it out; hop onto Ride_5b and ride it home. Jump Spikes_5 ahead of Hinge_F; stop
        // short of Slam_F and hop onto it; the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(XAtLeast(5.5f)), Release(), Until(Still()),
            Until(HomeFor(L023Layout.RidePeriod, L023Layout.Ride5aPhase, L023Layout.RideMove, 30, 3)), Hold(Right), Until(XAtLeast(7.6f)), Release(), Until(Still()),
            Until(Moving("Ride_5a")), Until(Stopped("Ride_5a")), Hold(Right), Jump(), Until(Airborne()), For(20), Release(), Until(Grounded()), Until(Still()),
            Until(HomeFor(L023Layout.RidePeriod, L023Layout.Ride5bPhase, L023Layout.RideMove, 40, 2)),
            Hold(Right), Until(XAtLeast(20.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(26f)), Release(), Until(Still()),
            Until(Fired("Slam_F")), Until(Stopped("Slam_F")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Docks(), Gallery(), Crown() };
            const int D = 0, G = 1, C = 2;
            var solution = new Route("L023 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Docks.
                new Betrayal("T1 [NW]: a cat that waits at the start goes down with Pier_0 into Lake_1", "Lake_1", DeathCause.Hazard,
                    B("wait", D, 0, Until(Dead())), revealedBy: "Pier_0"),
                new Betrayal("T2 [W]: a cat that jumps for Isle_1 before Ferry_1 gets there falls into Lake_1", "Lake_1", DeathCause.Hazard,
                    B("jump early", D, 4, Until(Moving("Ferry_1")), For(10), Hold(Right), Jump(), Until(Dead())), revealedBy: "Ferry_1"),
                new Betrayal("T3 [W]: a cat that jumps for Isle_2 while Ferry_2 is still on its way falls into Lake_2", "Lake_2", DeathCause.Hazard,
                    B("jump early", D, 14, Hold(Right), Jump(), Until(Dead())), revealedBy: "Ferry_2"),
                new Betrayal("T4 [NW]: a cat that waits on the stalled Ferry_2 is crushed by Slam_2", "Slam_2", DeathCause.Hazard,
                    B("wait", D, 15, Until(Dead()))),
                new Betrayal("T5 [W]: a cat that steps into Lift_U's well while it's up is crushed when it comes down", "Lift_U", DeathCause.Hazard,
                    B("step in", D, 22, Until(Beat(L023Layout.LiftPeriod, L023Layout.LiftUPhase + L023Layout.LiftMove + 10)), Hold(Right), Until(Dead()))),
                // Gallery.
                new Betrayal("T6 [NW]: a cat that stops on Sink_3 sinks with it into Lake_3", "Lake_3", DeathCause.Hazard,
                    B("stop", G, 5, Release(), Until(Dead())), revealedBy: "Sink_3"),
                new Betrayal("T7 [W]: a cat that walks off Isle_3 while Ride_4 is away falls into Lake_4", "Lake_4", DeathCause.Hazard,
                    B("walk on", G, 11, Until(Beat(L023Layout.Ride4Period, L023Layout.Ride4Phase + 10)), Hold(Left), Until(Dead())), revealedBy: "Ride_4"),
                // Crown.
                new Betrayal("T8 [W]: a cat that hops for Ride_5b before Ride_5a gets there falls into Lake_5", "Lake_5", DeathCause.Hazard,
                    B("hop early", C, 9, Until(Moving("Ride_5a")), For(12), Hold(Right), Jump(), Until(Dead())), revealedBy: "Ride_5a"),
                new Betrayal("T9 [NW]: a cat that stops short of Spikes_5 is caught by Hinge_F", "Hinge_F", DeathCause.Hazard,
                    B("stop", C, 20, Until(XAtLeast(21f)), Release(), Until(Dead())), revealedBy: "Hinge_F"),
                new Betrayal("T10 [SS]: a cat that runs on under Slam_F is crushed", "Slam_F", DeathCause.Hazard,
                    B("run on", C, 25, Until(Dead()))));
        }
    }
}
