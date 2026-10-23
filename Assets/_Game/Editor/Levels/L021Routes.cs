using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-164: level 21's routes. Waits are on the layout's clocks: the vents' eruptions (tell 25, erupt 40), an element's fire
    // tick or pose. Betrayals branch off the solution's own steps.
    static class L021Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // ---- Ground, walked east ----
        // Off at once over the falling bridge; stop short of G_T.
        static RouteStep[] Start() => new[] {
            Hold(Right), Until(XAtLeast(8.6f)), Release(), Until(Still()),
        };
        // Wait out G_T's eruption; cross under Spikes_T and stop short of Slam_1, which comes down as the cat comes.
        static RouteStep[] Vent() => new[] {
            Until(Fired("G_T")), Until(Since("G_T", 66)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(12.3f)), Release(), Until(Still()),
        };
        // Hop Slam_1 once it has landed and on over Kerb_1 between Arrow_1's shots; jump from the ground's end onto the Plinth.
        static RouteStep[] SlamHop() => new[] {
            Until(Fired("Slam_1")), Until(Stopped("Slam_1")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(15.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(19.4f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(23.1f)), Release(), Until(Still()),
        };

        // ---- Shaft ----
        // Onto V1 as it erupts (its tell starts as the cat lands on the Plinth); ride it, steer east onto Crumb_2 once above Ledge_2, straight on to V2; ride it, steer west
        // onto Crumb_3, straight on to V3; ride it, steer west onto the Lip.
        static RouteStep[] Shaft() => new[] {
            Hold(Right), Until(XAtLeast(24.5f)), Release(),
            Until(YAtLeast(L021Layout.L2Top + .3f)), Hold(Right).Timed(TimedMode.Shift), Until(Grounded()), Until(XAtLeast(28f)), Release(), Until(Still()),
            Until(YAtLeast(L021Layout.L3Top + .3f)), Hold(Left).Timed(TimedMode.Shift), Until(Grounded()), Until(XAtMost(23.5f)), Release(), Until(Still()),
            Until(YAtLeast(L021Layout.ST + .2f)), Hold(Left).Timed(TimedMode.Shift), Until(Grounded()),
        };

        // ---- Crown, walked west ----
        // Hinge_F stands up behind; wait out G_FD's eruption short of Spikes_FD and jump them (landing sets Slam_F off ahead);
        // hop onto Slam_F once it's down and off it, jump the crumbles and Pop_F, into the door.
        static RouteStep[] Crown() => new[] {
            Until(XAtMost(19.1f)), Release(), Until(Still()), Until(Since("G_FD", 66)),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(Fired("Slam_F")), Until(Stopped("Slam_F")), Hold(Left), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(10.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(5.3f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Start(), Vent(), SlamHop(), Shaft(), Crown() };
            const int Off = 0, Geyser = 1, Hop = 2, Climb = 3, End = 4;
            var solution = new Route("L021 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Ground.
                new Betrayal("T1 [NW]: a cat that waits at the start goes down with the bridge onto PitW's spikes", "PitW_Hazard", DeathCause.Hazard,
                    B("wait on the bridge", Off, 0, Hold(Right), Until(XAtLeast(2f)), Release(), Until(Dead())), revealedBy: "Bridge_1"),
                new Betrayal("T2 [W]: a cat that crosses G_T while it erupts is thrown into Spikes_T", "Spikes_T", DeathCause.Hazard,
                    B("cross at once", Geyser, 1, Hold(Right), Until(Dead())), revealedBy: "G_T"),
                new Betrayal("T3 [SS]: a cat that runs on under Slam_1 is crushed", "Slam_1", DeathCause.Hazard,
                    B("run on", Geyser, 3, Until(Dead()))),
                new Betrayal("T4 [J]: a cat that walks on along the ground past Kerb_1 meets Arrow_1", "Arrow_1", DeathCause.Hazard,
                    B("stop in the lane", Hop, 10, Release(), Until(Dead()))),
                new Betrayal("T5 [J]: a cat that walks onto Collapse_P falls into PitP", "PitP", DeathCause.Hazard,
                    B("walk on", Hop, 11, Until(Dead())), revealedBy: "Collapse_P"),
                // Shaft.
                new Betrayal("T6 [NW]: a cat that stops on Crumb_2 falls onto Spikes_Sh", "Spikes_Sh", DeathCause.Hazard,
                    B("stop", Climb, 6, Release(), Until(Dead())), revealedBy: "Crumb_2"),
                new Betrayal("T7 [LW]: a cat that stops on Crumb_3 falls down the shaft onto Spikes_Sh", "Spikes_Sh", DeathCause.Hazard,
                    B("stop", Climb, 12, Release(), Until(Dead())), revealedBy: "Crumb_3"),
                // Crown.
                new Betrayal("T8 [OL]: a cat that keeps waiting short of Spikes_FD is caught by Hinge_F", "Hinge_F", DeathCause.Hazard,
                    B("wait on", End, 3, Until(Dead())), revealedBy: "Hinge_F"),
                new Betrayal("T12 [NJ]: a cat that jumps Spikes_FD while G_FD erupts is blown down onto them", "Spikes_FD", DeathCause.Hazard,
                    B("jump at once", End, 3, Hold(Left), Jump(), Until(Dead())), revealedBy: "G_FD"),
                new Betrayal("T9 [SS]: a cat that runs on under Slam_F is crushed", "Slam_F", DeathCause.Hazard,
                    B("run on", End, 8, Hold(Left), Until(Dead()))),
                new Betrayal("T10 [J]: a cat that walks onto Crumble_F and Crumble_G falls into the Briar", "Briar", DeathCause.Hazard,
                    B("walk on", End, 16, Until(Dead())), revealedBy: "Crumble_F"),
                new Betrayal("T11 [J]: a cat that walks on by the door meets Pop_F", "Pop_F", DeathCause.Hazard,
                    B("walk on", End, 20, Until(Dead()))));
        }
    }
}
