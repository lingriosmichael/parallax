using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-149): level 19's routes. Hold(Left) and Hold(Right) are the keys pressed: while an inverter's window is
    // open (Invert, Orb_2, Orb_L) Left moves the cat east and Right west.
    static class L019Routes
    {
        const float F2 = L019Layout.F2, F3 = L019Layout.F3;

        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // Standing on this element.
        static RouteCondition On(string element) => GroundedOn(element);
        // The arrow's latest shot was fired between `from` and `to` ticks ago.
        static RouteCondition ShotAge(string arrow, int from, int to) => new($"{arrow} shot {from}-{to} ago", v =>
        {
            int i = v.Result.Elements.IndexOf(arrow);
            int age = v.Last.RoomLifeTick - v.Last.FireTick[i];
            return i >= 0 && v.Last.FireTick[i] >= 0 && age >= from && age <= to;
        });

        // No inverter's window is open (the cue is off: Invert's and Orb_L's end in their end boxes).
        static RouteCondition Uninverted() => new("uninverted", v => v.Last.Signature[v.Result.Elements.Count - 1] == 0);

        static RouteStep[] Steps(params object[] parts)
        {
            var list = new List<RouteStep>();
            foreach (object p in parts) { if (p is RouteStep s) list.Add(s); else list.AddRange((RouteStep[])p); }
            return list.ToArray();
        }

        // Mirror: inverted, Left is east. Jump Pit_1 at once; stop short of Slam_1, hop it; onto Ferry_1; off it onto
        // Crumb_1 and at once on, switching to Right as the jump crosses Invert's end box; the spear stair.
        static RouteStep[] Mirror() => Steps(
            Hold(Left), Until(XAtLeast(3.9f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(8.5f)), Release(), Until(Still()), Until(Fired("Lift_1")), Until(Moving("Lift_1")), Until(Home("Lift_1")),
            Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(12.3f)), Release(), Until(Still()), Until(Stopped("Slam_1")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(17.1f)), Release(), Until(On("Ferry_1")), Until(Still()),
            Until(Moving("Ferry_1")), Until(Stopped("Ferry_1")),
            Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(20.6f)), Jump(), Until(XAtLeast(21.9f)), Release(), Until(On("Crumb_1")),
            Hold(Left), Jump(), Until(Uninverted()), Hold(Right), Until(Grounded()),
            // The stair.
            Until(XAtLeast(28.3f)), Release(), Until(Still()), Until(Since("Spear_1", 22)),
            Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(XAtLeast(30.6f)), Release(), Until(On("Spear_1_Shaft")), Until(Still()),
            Until(Since("Spear_2", 12)), Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(28.6f)), Release(), Until(On("Spear_2_Shaft")), Until(Still()),
            Until(Since("Spear_3", 12)), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(XAtLeast(30.6f)), Release(), Until(On("Spear_3_Shaft")), Until(Still()),
            Until(Since("Spear_4", 12)), Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(28.6f)), Release(), Until(On("Spear_4_Shaft")), Until(Still()),
            Hold(Left), Jump(), Until(XAtMost(27.3f)), Release(), Until(Grounded()), Hold(Left), Until(XAtMost(26.5f)));

        // Orb: jump Thorns_2 into Orb_2 (Right is west from then); stop short of Slam_2, hop it (its landing passes Lift_2's
        // cut); stop short of Lift_2, cross once it's home; jump onto the bridge as Orb_2 runs out (Left is west again); run
        // the bridge onto Lift_U.
        static RouteStep[] Orb() => Steps(
            Until(XAtMost(26.2f)), Jump(), Until(Fired("Orb_2")), Hold(Right), Until(Grounded()),
            Until(XAtMost(21.9f)), Release(), Until(Still()), Until(Stopped("Slam_2")),
            Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()),
            Until(Fired("Lift_2")), Until(Moving("Lift_2")), Until(Home("Lift_2")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtMost(12.8f)), Jump(), Until(Since("Orb_2", L019Layout.Orb2Ticks + 1)), Hold(Left), Until(Grounded()),
            Until(XAtMost(1.3f)), Release(), Until(On("Lift_U")), Until(Moving("Lift_U")), Until(Stopped("Lift_U")));

        // Run: mirrored by Orb_L (Left is east). Stop short of Slam_3, hop it between Arrow_3Hi's shots, run over Collapse_3;
        // the finale: stop short of Slam_3a, hop onto it, wait for Slam_3b, onto it, and the last jump, switching to Right
        // as it crosses Orb_L's end box.
        static RouteStep[] Run() => Steps(
            Hold(Left), Until(XAtLeast(4.5f)), Release(), Until(Still()), Until(ShotAge("Arrow_3Lo", 20, 200)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Hold(Left), Until(XAtLeast(10.1f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(18.4f)), Release(), Until(Still()), Until(Stopped("Slam_3a")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtLeast(20.5f)), Release(), Until(On("Slam_3a")), Until(Still()),
            Until(Stopped("Slam_3b")), Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtLeast(24.4f)), Release(), Until(On("Slam_3b")), Until(Still()),
            Hold(Left), Jump(), Until(Uninverted()), Hold(Right), Until(RoomComplete()));

        static Route Prefix(Route solution, string label, string name, params RouteStep[] then) => Route.PrefixOf(solution, label, name, then);

        public static RoomRoutes Build()
        {
            var solution = new Route("L019 solution", Steps(Mirror(), Orb(), Run()));
            Route P(string label, string name, params RouteStep[] then) => Prefix(solution, label, name, then);
            return new RoomRoutes(solution,
                // Mirror.
                new Betrayal("T1 [NW]: a cat that waits at the start is pushed into Pit_1 by Hinge_0", "Thorns_1", DeathCause.Hazard,
                    new Route("wait at the start", Until(Dead())), revealedBy: "Hinge_0"),
                new Betrayal("T2 [B]: a cat that presses right to go east walks back onto Hinge_0 and drops onto Thorns_0 as it stands up", "Thorns_0", DeathCause.Hazard,
                    new Route("press right", Hold(Right), Until(Dead())), revealedBy: "Hinge_0"),
                new Betrayal("T3 [W]: a cat that runs on over Lift_1 is crushed into the trench's bed above", "Lift_1", DeathCause.Hazard,
                    P("Until(X>=8.5)", "run on", Until(Dead()))),
                new Betrayal("T4 [BAIT]: a cat that runs on is slammed by Slam_1", "Slam_1", DeathCause.Hazard,
                    P("Until(X>=12.3)", "run on", Until(Dead()))),
                new Betrayal("Crumble [NW]: a cat that stops on Crumb_1 falls onto Thorns_F", "Thorns_F", DeathCause.Hazard,
                    P("Until(GroundedOn(Crumb_1))", "stop on Crumb_1", Release(), Until(Dead())), revealedBy: "Crumb_1"),
                new Betrayal("Switch [SS]: a cat that keeps pressing left as Invert runs out in the jump falls back onto Thorns_F", "Thorns_F", DeathCause.Hazard,
                    P("Until(GroundedOn(Crumb_1))", "keep pressing left", Hold(Left), Jump(), Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("Dead end D1 [OL]: a cat that hops short off Crumb_1 onto the near ledge falls through Fake_1", "Thorns_F", DeathCause.Hazard,
                    P("Until(GroundedOn(Crumb_1))", "the near ledge", Hold(Left), Jump(), For(10), Release(), Until(Dead())), revealedBy: "Crumb_1"),
                new Betrayal("T8 [NJ]: a cat that climbs on from step 2 at once meets Spear_3", "Spear_3", DeathCause.Hazard,
                    P("Until(GroundedOn(Spear_2_Shaft))", "climb on at once", Hold(Right), Jump(), Until(Dead()))),
                // Orb.
                new Betrayal("T9 [B]: a cat that keeps pressing left as Orb_2 swaps its hands runs back onto Thorns_2", "Thorns_2", DeathCause.Hazard,
                    P("Until(Fired(Orb_2))", "keep pressing left", Hold(Left), Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T10 [SS]: a cat that walks on is slammed by Slam_2", "Slam_2", DeathCause.Hazard,
                    P("Until(X<=21.9)", "walk on", Until(Dead()))),
                new Betrayal("T11 [SS]: a cat that walks on over Lift_2 is crushed into slab B", "Lift_2", DeathCause.Hazard,
                    P("Until(Stopped(Slam_2))", "walk on", Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))),
                new Betrayal("Switch [SS]: a cat that keeps pressing right as Orb_2 runs out in the jump falls back into the trench", "Spikes_B", DeathCause.Hazard,
                    P("Until(X<=12.8)", "keep pressing right", Jump(), Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("Crumble [NW]: a cat that stops on the bridge falls onto Spikes_B", "Spikes_B", DeathCause.Hazard,
                    P("Until(Orb_2 +188)", "stop on the bridge", Hold(Left), Until(Grounded()), Until(XAtMost(7.1f)), Release(), Until(Dead())), revealedBy: "Bridge_3"),
                // Run.
                new Betrayal("T14 [NJ]: a cat that jumps the shot coming at it meets Arrow_3Hi passing overhead", "Arrow_3Hi", DeathCause.Hazard,
                    P("Until(X>=4.5)", "jump the shot", Until(XAtLeast(6.2f)), Jump(), Until(Dead()))),
                new Betrayal("T15 [W]: a cat that walks on meets Arrow_3Lo", "Arrow_3Lo", DeathCause.Hazard,
                    P("Until(X>=4.5)", "walk on", Until(Dead()))),
                new Betrayal("Crumble [NW]: a cat that stops on Collapse_3 falls into Pit_3", "Pit_3", DeathCause.Hazard,
                    P("Until(Arrow_3Lo shot 20-200 ago)", "stop on Collapse_3", Jump(), Until(Airborne()), Until(Grounded()), Hold(Left), Until(XAtLeast(6.8f)), Release(), Until(Dead())), revealedBy: "Collapse_3"),
                new Betrayal("T17 [J]: a cat that walks up to the kerb meets Arrow_3Lo's next shot", "Arrow_3Lo", DeathCause.Hazard,
                    P("Until(Arrow_3Lo shot 20-200 ago)", "walk to the kerb", Jump(), Until(Airborne()), Until(Grounded()), Hold(Left), Until(Dead()))),
                new Betrayal("T18 [W]: a cat that runs on is slammed by Slam_3a", "Slam_3a", DeathCause.Hazard,
                    P("Until(X>=18.4)", "run on", Until(Dead()))),
                new Betrayal("T19 [J]: a cat that stays on the floor is caught by Hinge_3's spikes", "Hinge_3", DeathCause.Hazard,
                    P("Until(Stopped(Slam_3a))", "stay down", Until(Dead()))),
                new Betrayal("Dead end D2 [W]: a cat that steps down off Slam_3a to walk on meets Spikes_3", "Spikes_3", DeathCause.Hazard,
                    P("Until(GroundedOn(Slam_3a))", "step down", Hold(Left), Until(Dead()))),
                new Betrayal("Switch [SS]: a cat that keeps pressing left as Orb_L runs out in the last jump falls onto Spikes_3b", "Spikes_3b", DeathCause.Hazard,
                    P("Until(GroundedOn(Slam_3b))", "keep pressing left", Hold(Left), Jump(), Until(Dead())), revealedBy: R.CatInverted));
        }
    }
}
