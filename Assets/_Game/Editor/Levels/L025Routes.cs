using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-169: level 25's routes. Almost every wait is on the beat (room ticks); Arrow_3 is jumped as each shot comes up behind.
    // Betrayals branch off the solution's own steps.
    static class L025Routes
    {
        const int Beat = L025Layout.Beat;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition On(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // Arrow_3's latest shot in flight (after its tell, from Jamb_W's face), its tip within `d` behind the cat's west side.
        static RouteCondition ShotBehind(float d) => new($"Arrow_3 within {d}", v =>
        {
            int i = v.Result.Elements.IndexOf("Arrow_3");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - L025Layout.ArrowTell;
            float tip = L025Layout.ArrowMouth + .8f + L025Layout.ArrowSpeed * flying, back = v.Last.X - .5f;
            return flying >= 0 && tip < back && back - tip <= d;
        });

        // ---- Pistons, storey 1, walked east ----
        // Set off as Piston_A comes down and run the wave; hop onto the Plinth and stand on G_1 until it launches the cat; steer
        // west onto storey 2.
        static RouteStep[] Pistons() => new[] {
            Until(On(Beat, L025Layout.PistonA + 35)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(16.6f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(18.2f)), Release(), Until(YAtLeast(L025Layout.S2 + .3f)), Hold(Left), Until(Grounded()),
        };

        // ---- Slams, storey 2, walked west ----
        // Stop short of Slam_2a; go as it goes back up, through both slams' gaps; wait for Lift_2 at home, step on, ride it up.
        static RouteStep[] Slams() => new[] {
            Until(XAtMost(15.6f)), Release(), Until(Still()),
            Until(On(Beat, L025Layout.Slam2a + 40)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(3.4f)), Release(), Until(Still()),
            Until(On(L025Layout.LiftPeriod, L025Layout.LiftPhase + 2 * L025Layout.LiftMove + L025Layout.LiftHold + 3)), Hold(Left), Until(XAtMost(1.9f)), Release(), Until(Still()),
            Until(Moving("Lift_2")), Until(Stopped("Lift_2")),
        };

        // ---- Drums, storey 3, walked east ----
        // Stop short of Spikes_3; jump them once G_3 has stopped blowing; jump Arrow_3's shots as they come up behind; over Kerb_3;
        // stop, and through as Slam_D goes back up, into the door.
        static RouteStep[] Drums() => new[] {
            Hold(Right), Until(XAtLeast(3.4f)), Release(), Until(Still()),
            Until(On(Beat, L025Layout.G3Erupt + 42)), Hold(Right), Until(XAtLeast(4.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(ShotBehind(2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(21.6f)), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(On(Beat, L025Layout.SlamD + 34)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Pistons(), Slams(), Drums() };
            const int P = 0, S = 1, D = 2;
            var solution = new Route("L025 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Pistons.
                new Betrayal("T1 [W]: a cat that sets off late is under Piston_A as it rises", "Piston_A", DeathCause.Hazard,
                    B("late", P, 0, Until(On(Beat, L025Layout.PistonA + 60)), Hold(Right), Until(Dead()))),
                new Betrayal("T2 [NW]: a cat that stops under Piston_B is crushed", "Piston_B", DeathCause.Hazard,
                    B("stop", P, 2, Until(XAtLeast(9.6f)), Release(), Until(Dead()))),
                new Betrayal("T3 [NW]: a cat that stops under Piston_C is crushed", "Piston_C", DeathCause.Hazard,
                    B("stop", P, 2, Until(XAtLeast(13.6f)), Release(), Until(Dead()))),
                // Slams.
                new Betrayal("T4 [W]: a cat that goes before Slam_2a is back up is crushed", "Slam_2a", DeathCause.Hazard,
                    B("go now", S, 3, Until(On(Beat, L025Layout.Slam2a - 15)), Hold(Left), Until(Dead()))),
                new Betrayal("T5 [NW]: a cat that stops under Slam_2b is crushed", "Slam_2b", DeathCause.Hazard,
                    B("stop", S, 5, Until(XAtMost(7.9f)), Release(), Until(Dead()))),
                new Betrayal("T6 [W]: a cat that steps into Lift_2's well while it's up is crushed when it comes down", "Lift_2", DeathCause.Hazard,
                    B("step in", S, 8, Until(On(L025Layout.LiftPeriod, L025Layout.LiftPhase + L025Layout.LiftMove + 5)), Hold(Left), Until(Dead()))),
                // Drums.
                new Betrayal("T7 [W]: a cat that jumps Spikes_3 while G_3 blows is pushed onto them", "Spikes_3", DeathCause.Hazard,
                    B("jump now", D, 4, Until(On(Beat, L025Layout.G3Erupt + 5)), Hold(Right), Jump(), Until(Dead())), revealedBy: "G_3"),
                new Betrayal("T8 [J]: a cat that doesn't jump Arrow_3's shot is hit", "Arrow_3", DeathCause.Hazard,
                    B("walk on", D, 11, Until(Dead()))),
                new Betrayal("T9 [W]: a cat that goes as Slam_D comes down is crushed", "Slam_D", DeathCause.Hazard,
                    B("go now", D, 20, Until(On(Beat, L025Layout.SlamD - 5)), Hold(Right), Until(Dead()))));
        }
    }
}
