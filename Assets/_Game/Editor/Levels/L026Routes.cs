using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-170: level 26's routes. The solution jumps every fake, goes through the slams in their gaps, follows the door up on G_1,
    // walks straight off the shrine, and stays ahead of the cloud. Betrayals branch off the solution's own steps.
    static class L026Routes
    {
        const int Beat = L026Layout.Beat;
        const float S2 = L026Layout.S2;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition On(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // ---- Hall, storey 1, walked east ----
        // Jump Fake_0; long-jump onto Stone_a and off it, over both small stones; jump Pop_1 as it springs; stop short of Slam_1 and
        // go through as it goes back up; stop on G_1 and ride its eruption up the hole, steering west onto storey 2.
        static RouteStep[] Hall() => new[] {
            Hold(Right), Until(XAtLeast(3.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Ground_1")),
            Until(XAtLeast(8.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Stone_a")),
            Until(XAtLeast(14f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Ground_2")),
            Until(Fired("Pop_1")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(On(Beat, L026Layout.Slam1 + 33)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(27.3f)), Release(), Until(Still()),
            Until(YAtLeast(S2 + .3f)), Hold(Left), Until(Grounded()),
        };

        // ---- Shrine, storey 2, walked west ----
        // Straight off the shrine; jump Fake_2b ahead of Hinge_2 and Fake_2a; stop short of Slam_2 and go through as it goes back
        // up; wait for Lift_2 at home, step on, ride it up.
        static RouteStep[] Shrine() => new[] {
            Until(XAtMost(17.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(12f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(On(Beat, L026Layout.Slam2 + 33)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(3.4f)), Release(), Until(Still()),
            Until(On(L026Layout.LiftPeriod, L026Layout.LiftPhase + 2 * L026Layout.LiftMove + L026Layout.LiftHold + 3)), Hold(Left), Until(XAtMost(1.9f)), Release(), Until(Still()),
            Until(Moving("Lift_2")), Until(Stopped("Lift_2")),
        };

        // ---- Crown, storey 3, walked east ----
        // Ahead of the cloud: jump Fake_3a and Fake_3b, jump Pop_3 as it springs, cross Slam_D's cut and stop short, hop onto it,
        // into the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(XAtLeast(8.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(14f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(Fired("Pop_3")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(25.9f)), Release(), Until(Still()), Until(Stopped("Slam_D")),
            Hold(Right), Jump(), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Hall(), Shrine(), Crown() };
            var solution = new Route("L026 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            const int H = 0, S = 1, C = 2;

            return new RoomRoutes(solution,
                // Hall.
                new Betrayal("T1 [W]: a cat that runs on from the start falls through Fake_0 onto Pit_0's spikes", "Pit_0", DeathCause.Hazard,
                    B("run on", H, 1, Until(Dead())), revealedBy: "Fake_0"),
                new Betrayal("T2 [J]: a cat that hops onto the near small stone falls through Fake_a", "Pit_1", DeathCause.Hazard,
                    B("hop short", H, 5, Until(XAtLeast(8.2f)), Jump(), For(12), Release(), Until(Dead())), revealedBy: "Fake_a"),
                new Betrayal("T3 [J]: a cat that hops off Stone_a onto the far small stone falls through Fake_b", "Pit_2", DeathCause.Hazard,
                    B("hop short", H, 9, Until(XAtLeast(14f)), Jump(), For(12), Release(), Until(Dead())), revealedBy: "Fake_b"),
                new Betrayal("T4 [W]: a cat that runs on is caught by Pop_1", "Pop_1", DeathCause.Hazard,
                    B("run on", H, 14, Until(Dead()))),
                new Betrayal("T5 [W]: a cat that goes before Slam_1 is back up is crushed", "Slam_1", DeathCause.Hazard,
                    B("go now", H, 19, Until(On(Beat, L026Layout.Slam1 - 12)), Hold(Right), Until(Dead()))),
                Betrayal.Recovers("The door rides its lift up the shaft away from a cat that comes to it, which follows it up", "Door", solution),
                // Shrine.
                new Betrayal("T6 [W]: a cat that rests at the checkpoint is crushed by Slam_S", "Slam_S", DeathCause.Hazard,
                    B("rest", S, 0, Until(XAtMost(24.5f)), Release(), Until(Dead()))),
                new Betrayal("T7 [W]: a cat that stops past Hinge_2 is shoved through Fake_2b into the lake", "Pit_2", DeathCause.Hazard,
                    B("stop", S, 0, Until(XAtMost(17.6f)), Release(), Until(Dead())), revealedBy: "Hinge_2"),
                new Betrayal("T8 [W]: a cat that walks on falls through Fake_2b into the lake", "Pit_2", DeathCause.Hazard,
                    B("walk on", S, 0, Until(Airborne()), Release(), Until(Dead())), revealedBy: "Fake_2b"),
                new Betrayal("T9 [W]: a cat that walks on falls through Fake_2a into the lake", "Pit_1", DeathCause.Hazard,
                    B("walk on", S, 4, Until(Airborne()), Release(), Until(Dead())), revealedBy: "Fake_2a"),
                new Betrayal("T10 [W]: a cat that goes before Slam_2 is back up is crushed", "Slam_2", DeathCause.Hazard,
                    B("go now", S, 10, Until(On(Beat, L026Layout.Slam2 - 12)), Hold(Left), Until(Dead()))),
                new Betrayal("T11 [W]: a cat that steps into Lift_2's well while it's up is crushed when it comes down", "Lift_2", DeathCause.Hazard,
                    B("step in", S, 15, Until(On(L026Layout.LiftPeriod, L026Layout.LiftPhase + L026Layout.LiftMove + 5)), Hold(Left), Until(Dead()))),
                // Crown.
                new Betrayal("T12 [W]: a cat that walks on falls through Fake_3a, and the fakes under it, into the lake", "Pit_1", DeathCause.Hazard,
                    B("walk on", C, 1, Until(XAtLeast(L026Layout.ColA0)), Until(Airborne()), Release(), Until(Dead())), revealedBy: "Fake_3a"),
                new Betrayal("T13 [W]: a cat that stops is struck by the cloud", "Cloud", DeathCause.Hazard,
                    B("stop", C, 5, Release(), Until(Dead()))),
                new Betrayal("T14 [W]: a cat that walks on falls through Fake_3b, and the fakes under it, into the lake", "Pit_2", DeathCause.Hazard,
                    B("walk on", C, 5, Until(Airborne()), Release(), Until(Dead())), revealedBy: "Fake_3b"),
                new Betrayal("T15 [W]: a cat that runs on is caught by Pop_3", "Pop_3", DeathCause.Hazard,
                    B("run on", C, 10, Until(Dead()))),
                new Betrayal("T16 [W]: a cat that runs on is crushed by Slam_D", "Slam_D", DeathCause.Hazard,
                    B("run on", C, 14, Until(Dead()))));
        }
    }
}
