using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-148): level 18's routes. While an inverter's swap is on, a held direction moves the cat the other way: the
    // steps say the stick, not the way the cat goes. Waits are on the layout's clocks: the cloud's (it wakes on the first
    // step, charges 50 ticks later and every 90 after that, 25 ticks before each strike), the geysers' eruptions, an element's
    // fire tick or pose. Betrayals branch off the solution's own steps.
    static class L018Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // A room tick at or after `tick` (the finale starts on the cloud's clock).
        static RouteCondition At(int tick) => new($"t>={tick}", v => v.Last.RoomLifeTick >= tick);
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        // No inverter's swap is shown (Inv_3's ends in its box on the dais).
        static RouteCondition Normal() => new("not inverted", v =>
        {
            int i = v.Result.Elements.IndexOf(R.CatInverted);
            return i < 0 || v.Last.Signature[i] == v.Result.Records[0].Signature[i];
        });

        // The cloud's clock (D-090): woken at W, it charges at W + 50 + 90k for 25 ticks, then strikes for 6.
        static int CloudPhase(RouteView v)
        {
            int i = v.Result.Elements.IndexOf("Cloud");
            int w = i >= 0 ? v.Last.FireTick[i] : -1;
            int since = v.Last.RoomLifeTick - w - 50;
            return w < 0 || since < 0 ? -1 : since % 90;
        }
        // Charging: the cloud has locked over where it is.
        static RouteCondition Charging() => new("cloud charging", v => { int p = CloudPhase(v); return p >= 0 && p < 25; });
        // The strike is over.
        static RouteCondition Calm() => new("cloud calm", v => { int p = CloudPhase(v); return p < 0 || p >= 31; });

        // ---- Ground (S1), walked east ----
        // Off at once (Hinge_0 stands up behind), over Collapse_1; stop short of Slam_1.
        static RouteStep[] Start() => new[] {
            Hold(Right), Until(XAtLeast(4.2f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(8.6f)), Release(), Until(Still()),
        };
        // Hop Slam_1 once it has landed; stop short of Lift_1, which rises as the cat lands.
        static RouteStep[] SlamHop() => new[] {
            Until(Fired("Slam_1")), Until(Stopped("Slam_1")), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
        };
        // The cloud catches up while Lift_1 is up: step aside from its charge, cross once the lift is down, hop Kerb_1.
        static RouteStep[] LiftWait() => new[] {
            Until(Charging()), Hold(Right), Until(XAtLeast(13.75f)), Release(), Until(Still()), Until(Moving("Lift_1")), Until(Home("Lift_1")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(16.4f)), Jump(), Until(Airborne()), Until(Grounded()),
        };
        // Jump Arrow_1's shot through Inv_1 (from here Left goes east), onto the Plinth, back onto G_1's vent.
        static RouteStep[] Mirror() => new[] {
            Until(XAtLeast(20.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Fired("Inv_1")), Hold(Left), Until(Grounded()),
            Until(XAtLeast(24.7f)), Jump(), Until(Airborne()), Until(Grounded()), Hold(Right), Until(XAtMost(27.6f)), Release(), Until(Still()),
        };
        // The cloud charges over the vent before it erupts: step east off it, back on after the strike, ride the eruption; the
        // swap ends in the hole, so Left goes west again: onto S2.
        static RouteStep[] Launch() => new[] {
            Until(Charging()), Hold(Left), Until(XAtLeast(28.05f)), Release(), Until(Still()), Until(Calm()),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtMost(27.6f)), Release(), Until(YAtLeast(4.7f)), Hold(Left), Until(Grounded()),
        };

        // ---- Storm (S2), walked west ----
        // Jump Spikes_2 ahead of Hinge_2; the eye swaps the stick (Right goes west); stop short of Slam_2.
        static RouteStep[] Eye() => new[] {
            Until(XAtMost(20.9f)), Jump(), Until(Airborne()), Until(Grounded()), Until(Fired("Inv_E")), Hold(Right),
            Until(XAtMost(15.9f)), Release(), Until(Still()),
        };
        // Hop onto Slam_2; step to its west end, out of the cloud's charge; wait out G_D's eruption; jump Spikes_G.
        static RouteStep[] Downdraft() => new[] {
            Until(Fired("Slam_2")), Until(Stopped("Slam_2")), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(XAtMost(14.6f)), Release(), Until(Grounded()), Until(Still()),
            Hold(Right), Until(XAtMost(13.5f)), Release(), Until(Still()), Until(Since("G_D", 66)), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()),
        };
        // Hop Kerb_2 between Arrow_2's shots and leap straight on for V_6; climb above the cloud; leap east onto S3.
        static RouteStep[] Climb() => new[] {
            Until(XAtMost(8.6f)), Jump(), Until(Airborne()), Until(Grounded()), Jump(), Until(Falling()), Hold(R.Down), Until(On("V_6", 1.3f)), Release(), ReleaseClimb(),
            Hold(Up), Until(YAtLeast(10.2f)), Until(Since("Inv_E", 202)), ReleaseClimb(), Hold(Right), Jump(), For(14), Until(Landed()),
        };

        // ---- Crown (S3), walked east ----
        // Over the crumbles; Inv_3 swaps the stick (Left goes east); stop short of Piston_3, which rises as the cat comes.
        static RouteStep[] Crumbles() => new[] {
            Until(Fired("Inv_3")), Hold(Left), Until(XAtLeast(17.1f)), Release(), Until(Still()), Until(Moving("Piston_3")), Until(Home("Piston_3")), Until(At(898)),
        };
        // The finale: stop short of Slam_3, hop onto it ahead of Hinge_3, step out from under the cloud's charge, onto Slam_3b
        // once it's down over Spikes_7, onto the dais (the swap ends there) and into the door.
        static RouteStep[] Finale() => new[] {
            Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(21.9f)), Release(), Until(Fired("Slam_3")), Until(Stopped("Slam_3")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(XAtLeast(23.6f)), Release(), Until(Grounded()), Until(Still()),
            Until(Calm()), Until(Charging()), Hold(Left), Until(XAtLeast(24.7f)), Release(), Until(Still()), Until(Stopped("Slam_3b")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(XAtLeast(26.8f)), Release(), Until(Grounded()),
            Hold(Left), Until(XAtLeast(27.5f)), Jump(), Until(Airborne()), Until(Normal()), Hold(Right), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Start(), SlamHop(), LiftWait(), Mirror(), Launch(), Eye(), Downdraft(), Climb(), Crumbles(), Finale() };
            const int Off = 0, Hop = 1, Lift = 2, Mirrored = 3, Vent = 4, Spikes = 5, Down = 6, Vine = 7, Crumble = 8, End = 9;
            var solution = new Route("L018 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Ground.
                new Betrayal("T1 [NW]: a cat that waits at the start is pushed onto Collapse_1 by Hinge_0 and falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("wait at the start", Off, 0, Until(Dead())), revealedBy: "Hinge_0"),
                new Betrayal("T2 [J]: a cat that walks onto Collapse_1 falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("walk on", Off, 1, Until(Dead())), revealedBy: "Collapse_1"),
                new Betrayal("T3 [W]: a cat that runs on under Slam_1 is crushed", "Slam_1", DeathCause.Hazard,
                    B("run on", Off, 6, Until(Dead()))),
                new Betrayal("T4 [B]: a cat that waits for Lift_1 where the cloud stops over it is struck", "Cloud", DeathCause.Hazard,
                    B("stand under it", Lift, 0, Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T5 [W]: a cat that runs on over Lift_1 is carried up into slab A", "Lift_1", DeathCause.Hazard,
                    B("run on", Hop, 6, Until(Dead()))),
                new Betrayal("T6 [J]: a cat that walks on along the ground meets Arrow_1", "Arrow_1", DeathCause.Hazard,
                    B("walk on", Mirrored, 1, Until(Dead()))),
                new Betrayal("T4 [B]: a cat that waits on G_1's vent through the charge is struck", "Cloud", DeathCause.Hazard,
                    B("stay on the vent", Vent, 0, Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("Dead end D1 [OL]: a cat that still steers mirrored as the swap ends drifts east onto Ledge_E: Spikes_E come up", "Spikes_E", DeathCause.Hazard,
                    B("steer east", Vent, 10, Hold(Right), Until(Dead()))),
                // Storm.
                new Betrayal("T1 [NW]: a cat that stops on S2's landing is pushed onto Spikes_2 by Hinge_2", "Spikes_2", DeathCause.Hazard,
                    B("stop", Spikes, 0, Until(XAtMost(21.6f)), Release(), Until(Dead())), revealedBy: "Hinge_2"),
                new Betrayal("T7 [W]: a cat that runs on under Slam_2 is crushed", "Slam_2", DeathCause.Hazard,
                    B("run on", Spikes, 7, Until(Dead()))),
                new Betrayal("T8 [NJ]: a cat that jumps Spikes_G while G_D erupts is blown down onto them", "Spikes_G", DeathCause.Hazard,
                    B("jump at once", Down, 13, Hold(Right), Jump(), Until(Dead())), revealedBy: "G_D"),
                new Betrayal("T9 [J]: a cat that stops past Kerb_2 meets Arrow_2", "Arrow_2", DeathCause.Hazard,
                    B("stop", Vine, 4, Release(), Until(Dead()))),
                new Betrayal("T10 [NW]: a cat that rests at the top of V_6, above the cloud, falls onto Spikes_V when it snaps", "Spikes_V", DeathCause.Hazard,
                    B("rest", Vine, 12, Until(Dead())), revealedBy: "V_6"),
                // Crown.
                new Betrayal("T11 [NW]: a cat that stops on the crumbles falls into the Briar", "Briar", DeathCause.Hazard,
                    B("stop", Vine, 17, Until(XAtLeast(6.5f)), Release(), Until(Dead())), revealedBy: "TL_2"),
                new Betrayal("T12 [SS]: a cat that runs on over Piston_3 is carried up into the roof", "Piston_3", DeathCause.Hazard,
                    B("run on", Crumble, 3, Until(Dead()))),
                new Betrayal("T13 [SS]: a cat that runs on under Slam_3 is crushed", "Slam_3", DeathCause.Hazard,
                    B("run on", End, 2, Until(Dead()))),
                new Betrayal("T14 [OL]: a cat that waits in front of Slam_3 is caught by Hinge_3", "Hinge_3", DeathCause.Hazard,
                    B("wait", End, 5, Until(Dead()))),
                new Betrayal("T4 [B]: a cat that stands still on Slam_3 through the charge is struck", "Cloud", DeathCause.Hazard,
                    B("stand still", End, 14, Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T15 [NJ]: a cat that jumps east off Slam_3 before Slam_3b comes down lands on Spikes_7", "Spikes_7", DeathCause.Hazard,
                    B("jump early", End, 13, Hold(Left), Jump(), Until(Dead()))),
                new Betrayal("Dead end D2 [B]: a cat that turns back west off S3's landing drops down the shaft onto Spikes_V", "Spikes_V", DeathCause.Hazard,
                    B("back west", Vine, 17, Hold(Left), Until(Dead())), revealedBy: "V_6"));
        }
    }
}
