using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 11's routes. A spear fires at the start of its tell, and a stuck spear's shaft is ground the
    // route can name ("<spear>_Shaft"), so the solution waits for each one to stick before it climbs it.
    static class L011Routes
    {
        // A spear fires at the start of its tell (8 ticks, when Stopped is already true); For(9) covers it, then Stopped holds once it has stuck.
        static RouteStep[] Stuck(string spear) => new[] { Until(Fired(spear)), For(9), Until(Stopped(spear)) };

        // A hop in place into the next step's lane, then wait for it to stick.
        static RouteStep[] Summon(string spear) => new[] { Jump(), Until(Airborne()), Until(Grounded()), Until(Fired(spear)), For(9), Until(Stopped(spear)) };

        static RouteStep[] Steps(params object[] parts)
        {
            var list = new System.Collections.Generic.List<RouteStep>();
            foreach (object p in parts) { if (p is RouteStep s) list.Add(s); else list.AddRange((RouteStep[])p); }
            return list.ToArray();
        }

        public static RoomRoutes Build()
        {
            var solution = new Route("L011 solution", Steps(
                // Foot: walk west, stop at the gap, let Spear_1 stick, climb it before the floor goes.
                Hold(Left), Until(XAtMost(3.95f)), Release(), Until(Still()), Stuck("Spear_1"),
                Jump().Timed(TimedMode.Shift), For(6), Hold(Left), Until(GroundedOn("Spear_1_Shaft")), Release(), Until(Still()),
                Hold(Right), Until(XAtLeast(1.8f)), Release(), Until(Still()), Stuck("Spear_2"),
                Jump(), Hold(Right), Until(GroundedOn("Spear_2_Shaft")), Release(), Until(Still()),
                Summon("Spear_3"), Hold(Left), Until(XAtMost(4.2f)), Release(), Until(Still()), Jump(), Hold(Left), Until(GroundedOn("Spear_3_Shaft")), Release(), Until(Still()),
                Summon("Spear_4"), Hold(Right), Until(XAtLeast(1.8f)), Release(), Until(Still()), Jump(), Hold(Right), Until(GroundedOn("Spear_4_Shaft")), Release(), Until(Still()),
                Summon("Spear_5"), Hold(Left), Until(XAtMost(4.2f)), Release(), Until(Still()), Jump().Timed(TimedMode.Shift), Hold(Left), Until(GroundedOn("Spear_5_Shaft")), Release(), Until(Still()),
                Summon("Spear_6"), Hold(Right), Until(XAtLeast(1.8f)), Release(), Until(Still()), Jump(), Hold(Right), Until(GroundedOn("Spear_6_Shaft")), Release(), Until(Still()),
                Summon("Spear_7"), Hold(Left), Until(XAtMost(4.2f)), Release(), Until(Still()), Jump(), Hold(Left), Until(GroundedOn("Spear_7_Shaft")), Release(), Until(Still()),
                Summon("Spear_8"), Hold(Right), Until(XAtLeast(1.8f)), Release(), Until(Still()), Jump(), Hold(Right), Until(GroundedOn("Spear_8_Shaft")),
                // Gallery: run east; Spear_G tells behind you: jump it; hop the Curb, through the tunnel, drop.
                Until(Moving("Spear_G")), Jump().Timed(TimedMode.Shift), Until(Grounded()),
                Until(XAtLeast(18.4f)), Jump(), Until(Grounded()),
                Until(XAtLeast(23.9f)), Release(), Until(GroundedOn("Dip")), Until(Still()),
                // The volley: hop out of the Dip, walk on over the cut, and walk straight back down into it.
                Hold(Right), Jump(), Until(XAtLeast(25.9f)), Release(), Until(GroundedOn("Shaft_Floor")), Until(Still()),
                Hold(Left).Timed(TimedMode.Shift), Until(GroundedOn("Dip")), Release(), Until(Still()),
                Stuck("V5"),
                Hold(Right), Until(XAtLeast(25.15f)), Jump(), Until(GroundedOn("Shaft_Floor")), Release(), Until(Still()),
                Hold(Left), Until(XAtMost(26.45f)), Release(), Until(Still()), Jump(), Hold(Right), Until(GroundedOn("V2_Shaft")), Release(), Until(Still()),
                Hold(Left), Until(XAtMost(27.1f)), Release(), Until(Still()), Jump(), Hold(Left), Until(GroundedOn("V3_Shaft")), Release(), Until(Still()),
                Jump(), Hold(Right), Until(GroundedOn("V4_Shaft")), Release(), Until(Still()), Hold(Left), Until(XAtMost(26f)), Release(), Until(Still()),
                Jump(), For(6), Hold(Left), Until(GroundedOn("V5_Shaft")),
                // The jump onto V5 set Spear_Top off: step straight back down onto V4, let it stick, climb back.
                Hold(Right).Timed(TimedMode.Shift), Until(GroundedOn("V4_Shaft")), Release(), Until(Stopped("Spear_Top")),
                Hold(Left), Until(XAtMost(26f)), Release(), Until(Still()),
                Jump(), For(8), Hold(Left), Until(GroundedOn("V5_Shaft")), Release(), Until(Still()),
                Jump(), For(6), Hold(Right), Until(GroundedOn("Spear_Top_Shaft")), Release(), Until(Still()),
                Jump(), For(8), Hold(Right), Until(XAtLeast(28.2f)), Release(), Until(GroundedOn("Tower_E")), Until(Still()),
                Hold(Left), Jump(), Until(GroundedOn("T_East")),
                // Summit: hop Post_G; Spear_Gap tells behind you: jump it; bridge, Curb, T_West; hop Post_D; Spear_Door.
                Until(XAtMost(24.7f)), Jump(), Until(XAtMost(23.4f)), Release(), Until(Grounded()), Until(Still()),
                Until(Moving("Spear_Gap")), For(3), Jump().Timed(TimedMode.Shift), Until(Grounded()), Until(Stopped("Spear_Gap")),
                Hold(Left), Until(XAtMost(23f)), Jump(), Until(Grounded()), Until(GroundedOn("T_West")),
                Until(XAtMost(13.2f)), Jump(), Until(Grounded()),
                Until(XAtMost(9.1f)), Jump(), Until(GroundedOn("Spear_Door_Shaft")), Hold(Left), Until(XAtMost(3.85f)), Release(), Until(Still()),
                Until(Stopped("Block_Door")),
                Hold(Left), Jump(), Until(Grounded()),
                Until(RoomComplete())));
            return new RoomRoutes(solution,
                new Betrayal("T1 [NJ]: a cat that hops the gap as the launcher glows is run through by Spear_1", "Spear_1", DeathCause.Hazard,
                    new Route("hop the gap", Hold(Left), Until(XAtMost(4f)), Jump(), Until(Dead()))),
                new Betrayal("T2 [NW]: a cat that waits at the gap's edge once Spear_1 has stuck drops with the floor", "Pit_Foot", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Spear_1))", "wait at the edge", Until(Dead())), revealedBy: "Foot_C"),
                new Betrayal("T3 [W]: a cat that jumps for the next step as it glows meets Spear_2 in its lane", "Spear_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Spear_1_Shaft))", "jump as it glows", Release(), Until(Still()), Until(Fired("Spear_2")), Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T3b [NW]: a cat that lingers on the fourth step is run through by Spear_L", "Spear_L", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Spear_4_Shaft))", "linger on step 4", Release(), Until(Dead()))),
                new Betrayal("T3c [OL]: a cat that hops onto Ledge_M instead of firing the next step falls to the foot of the shaft", "Pit_Foot", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Spear_7_Shaft))", "hop onto the ledge", Release(), Until(Still()), Hold(Right), Jump(), For(12), Release(), Until(Dead())), revealedBy: "Ledge_M"),
                new Betrayal("T4 [BAIT]: a cat that stays on the shaft floor once the volley starts is swept by V1", "V1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=25.9)", "stay past the Dip", Release(), Until(Dead()))),
                new Betrayal("T4b [NW]: a cat that lingers on V3 is run through by Spear_K", "Spear_K", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(V3_Shaft))", "linger on V3", Release(), Until(Dead()))),
                new Betrayal("T5 [B]: a cat that stands on V5, the top of the stair, is run through by Spear_Top", "Spear_Top", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(V5_Shaft))", "stand on V5", Release(), Until(Dead()))),
                new Betrayal("T6 [J]: a cat that walks on past Post_G is swept by Spear_Gap", "Spear_Gap", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Spear_Gap))", "walk on", Hold(Left), Until(Dead()))),
                new Betrayal("T7 [OL]: a cat that walks onto the Lip in front of the door ledge falls into the pit", "Pit_T", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=13.2)", "walk onto the Lip", Jump(), Until(Grounded()), Hold(Left), Until(Dead())), revealedBy: "Lip"),
                new Betrayal("T8 [W]: a cat that walks straight on to the door is crushed by Block_Door", "Block_Door", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Spear_Door_Shaft))", "walk straight on", Hold(Left), Until(Dead()))),
                new Betrayal("Dead end D1 [OL]: the ledge east of the stairs", "Spikes_D1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Tower_E))", "walk east", Hold(Right), Until(Dead()))),
                new Betrayal("Dead end D2 [LW]: the high ledge above the door ledge", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=13.2)", "climb onto the high ledge", Release(), Until(Still()), Hold(Left), Jump(), Until(XAtMost(12.4f)), Release(), Until(GroundedOn("Post_D")), Until(Still()),
                        Jump(), Hold(Left), Until(GroundedOn("Ledge_Hi")), Release(), Until(Dead()))));
        }
    }
}
