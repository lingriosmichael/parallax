using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-166: level 22's routes. Waits are on the layout's clocks: the punches' beat, an element's fire tick or pose.
    // Betrayals branch off the solution's own steps.
    static class L022Routes
    {
        const int Period = L022Layout.PunchPeriod;
        // A punch is busy (out, held, back) for this many ticks from its beat; the climb sets off as it's back.
        const int Busy = L022Layout.PunchOut + L022Layout.PunchHold + L022Layout.PunchBack;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // A chimney climbed from its foot: off the near wall (`first`, +1 east), across to the far one, back, and over the far
        // wall's top onto the floor above.
        static RouteStep[] Climb(int first) => new[] {
            Jump(), Until(first > 0 ? ClingingRight() : ClingingLeft()), Release(),
            Jump(), For(2), Hold(-first), Until(first > 0 ? ClingingLeft() : ClingingRight()), Release(),
            Jump(), For(2), Hold(first), Until(first > 0 ? ClingingRight() : ClingingLeft()), Release(),
            Jump(), For(2), Hold(-first), Until(Grounded()),
        };

        // ---- Works, corridor 1, walked east ----
        // Jump Spikes_0 ahead of Hinge_0; stop short of Slam_1 and hop it once it's down; jump Collapse_1; stop short of Lift_2
        // and let it go up and down; into chimney 1, and up it as Punch_1 pulls back.
        static RouteStep[] Works() => new[] {
            Hold(Right), Until(XAtLeast(4.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(10f)), Release(), Until(Still()),
            Until(Fired("Slam_1")), Until(Stopped("Slam_1")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(15.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(20.7f)), Release(), Until(Still()), Until(Moving("Lift_2")), Until(Home("Lift_2")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(29.1f)), Until(Beat(Period, L022Layout.Punch1Phase + Busy)),
        }.Concat(Climb(Right)).ToArray();

        // ---- Gallery, corridor 2, walked west ----
        // Jump Spikes_2 clear of Hinge_2, then Collapse_2; stop short of Slam_2 and hop it; into chimney 2 and up it as
        // Punch_2 pulls back.
        static RouteStep[] Gallery() => new[] {
            Hold(Left), Until(XAtMost(22.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(16.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(10.9f)), Release(), Until(Still()),
            Until(Fired("Slam_2")), Until(Stopped("Slam_2")), Hold(Left), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(2.8f)), Until(Beat(Period, L022Layout.Punch2Phase + Busy)),
        }.Concat(Climb(Left)).ToArray();

        // ---- Crown, corridor 3, walked east ----
        // Jump Spikes_3 ahead of Hinge_F; stop short of Slam_F and hop onto it; jump Collapse_3 and Pop_F, into the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(XAtLeast(8.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(14f)), Release(), Until(Still()),
            Until(Fired("Slam_F")), Until(Stopped("Slam_F")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(19f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(25.7f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Works(), Gallery(), Crown() };
            const int W = 0, G = 1, C = 2;
            var solution = new Route("L022 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Works.
                new Betrayal("T1 [NW]: a cat that waits at the start is caught by Hinge_0", "Hinge_0", DeathCause.Hazard,
                    B("wait", W, 0, Until(Dead())), revealedBy: "Hinge_0"),
                new Betrayal("T2 [SS]: a cat that runs on under Slam_1 is crushed", "Slam_1", DeathCause.Hazard,
                    B("run on", W, 5, Until(Dead()))),
                new Betrayal("T3 [J]: a cat that walks onto Collapse_1 falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("walk on", W, 14, Until(Dead())), revealedBy: "Collapse_1"),
                new Betrayal("T4 [SS]: a cat that runs on onto Lift_2 is crushed against SA_L", "Lift_2", DeathCause.Hazard,
                    B("run on", W, 18, Until(Dead()))),
                new Betrayal("T5 [W]: a cat that climbs chimney 1 into Punch_1's beat is crushed against Anvil_1", "Punch_1", DeathCause.Hazard,
                    B("climb now", W, 25, Until(Beat(Period, L022Layout.Punch1Phase + Period - 20)), Jump(), Until(ClingingRight()), Release(), Jump(), For(2), Hold(Left), Until(Dead()))),
                // Gallery.
                new Betrayal("T6 [NW]: a cat that stops short of Spikes_2 is shoved onto them by Hinge_2", "Spikes_2", DeathCause.Hazard,
                    B("stop", G, 1, Until(XAtMost(22.6f)), Release(), Until(Dead())), revealedBy: "Hinge_2"),
                new Betrayal("T7 [J]: a cat that walks onto Collapse_2 falls onto Briar_2", "Briar_2", DeathCause.Hazard,
                    B("walk on", G, 5, Until(Dead())), revealedBy: "Collapse_2"),
                new Betrayal("T8 [SS]: a cat that runs on under Slam_2 is crushed", "Slam_2", DeathCause.Hazard,
                    B("run on", G, 9, Until(Dead()))),
                new Betrayal("T10 [W]: a cat that climbs chimney 2 into Punch_2's beat is crushed against Anvil_2", "Punch_2", DeathCause.Hazard,
                    B("climb now", G, 19, Until(Beat(Period, L022Layout.Punch2Phase + Period - 20)), Jump(), Until(ClingingLeft()), Release(), Jump(), For(2), Hold(Right), Until(Dead()))),
                // Crown.
                new Betrayal("T11 [NW]: a cat that stops short of Spikes_3 is caught by Hinge_F", "Hinge_F", DeathCause.Hazard,
                    B("stop", C, 1, Until(XAtLeast(8.6f)), Release(), Until(Dead())), revealedBy: "Hinge_F"),
                new Betrayal("T12 [SS]: a cat that runs on under Slam_F is crushed", "Slam_F", DeathCause.Hazard,
                    B("run on", C, 5, Until(Dead()))),
                new Betrayal("T13 [J]: a cat that walks onto Collapse_3 falls onto Briar_3", "Briar_3", DeathCause.Hazard,
                    B("walk on", C, 14, Until(Dead())), revealedBy: "Collapse_3"),
                new Betrayal("T14 [J]: a cat that walks on by the door meets Pop_F", "Pop_F", DeathCause.Hazard,
                    B("walk on", C, 18, Until(Dead()))));
        }
    }
}
