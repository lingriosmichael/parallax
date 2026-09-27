using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 12's routes. Route moves are stick input, so while the cat is inverted Hold(Right) walks it
    // left; revealedBy Cat.Inverted measures a death from the flip itself.
    static class L012Routes
    {
        public static RoomRoutes Build()
        {
            // Route moves are stick input: while the cat is inverted, Hold(Right) moves it left (D-087 (7)).
            var solution = new Route("L012 solution",
                // Mirror: hop Spikes_Back onto Floor_1; Inv_1 flips the hands and Floor_1 is going: play it mirrored at once (hold
                // right to go left), jump P1, run over Floor_2, jump P3; switch hands as the flip ends, and on to the drop.
                Hold(Left), Until(XAtMost(26.8f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Fired("Inv_1")), Hold(Right), Until(XAtMost(22.1f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(18.4f)), Until(XAtMost(16.45f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(Moving(CatInverted)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(6.6f)), Until(Grounded().And(YAtLeast(14f).And(new RouteCondition("Y<16", v => v.Last.Y < 16f)))),
                // Halls: through the gate to Orb_A; mirrored from here: hold left to go right. Hop Collapse_B, run on, drop.
                Release(), Until(Still()), Hold(Right), Until(Fired("Orb_A")), Hold(Left), Until(XAtLeast(9.55f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Orb_A's flip ends on the run east: switch hands, on to the hole.
                Until(Moving(CatInverted)), Hold(Right).Timed(TimedMode.Shift),
                Until(XAtLeast(29.3f)), Until(Grounded().And(new RouteCondition("Y<11", v => v.Last.Y < 11f))),
                // Hall 3: west under the Shelf to Orb_B; Collapse_F opens ahead: hands flipped, the same stick walks the cat back
                // out; onto the Shelf, across, and down the hole.
                Hold(Left), Until(Fired("Orb_B")), Until(XAtLeast(20.6f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(Airborne()), Until(GroundedOn("Shelf")),
                Until(XAtMost(8.5f)), Until(Grounded().And(new RouteCondition("Y<6", v => v.Last.Y < 6f))),
                // Handoff: still flipped by Orb_B, run east (hold left) through Orb_G, jump Pit_H, switch hands as it ends; down
                // to hall 1 and west: jump PK, play Inv_H's flip mirrored, switch back, and jump to the door.
                Hold(Left), Until(Fired("Orb_G")), Until(XAtLeast(16.4f)), Jump(), Until(Airborne()), For(18), Hold(Right).Timed(TimedMode.Shift), Until(Grounded()),
                Until(XAtLeast(29.3f)), Until(Grounded().And(new RouteCondition("Y<1", v => v.Last.Y < 1f))),
                Hold(Left), Until(XAtMost(18.1f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(Fired("Inv_H")), Hold(Right), For(3), Until(Moving(CatInverted)), Hold(Left).Timed(TimedMode.Shift),
                Until(XAtMost(7.6f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()));
            return new RoomRoutes(solution,
                new Betrayal("T1 [SS]: a cat that keeps holding left once Inv_1 has flipped its hands runs back into Spikes_Back", "Spikes_Back", DeathCause.Hazard,
                    new Route("keep holding left", Hold(Left), Until(XAtMost(26.8f)), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: CatInverted),
                new Betrayal("T2 [NW]: a cat that stops to wait out the flip drops with Floor_1", "P1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Inv_1))", "wait it out", Release(), Until(Dead())), revealedBy: "Floor_1"),
                new Betrayal("T2 [NW]: a cat that stops on Floor_2 drops with it", "P2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=18.4)", "stop on Floor_2", Release(), Until(Dead())), revealedBy: "Floor_2"),
                new Betrayal("T3 [J]: a cat that runs on past Orb_A falls where Collapse_B opens", "PB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_A))", "run on", Hold(Left), Until(Dead())), revealedBy: "Collapse_B"),
                new Betrayal("T5 [NW]: a cat that stops where the hop lands is caught by Spikes_D", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.55)", "stop after the hop", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Orb_A"),
                // Orb_A's trigger box runs to x 14.4 (the layout says why): turning back re-enters it from the east after the
                // room has gone off, and the way back leads into Spikes_D.
                new Betrayal("T5 [NW]: a cat that turns back after the hop walks into Spikes_D", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.55)", "turn back", Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(16f)), Hold(Right), Until(Dead()))),
                new Betrayal("T6 [SS]: a cat that backs off by instinct (holding right, which now walks it back) is crushed by Block_E", "Block_E", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_A))", "back off", Hold(Right), Until(Dead()))),
                new Betrayal("T7 [B]: a cat that plays Orb_B's flip mirrored and runs on falls where Collapse_F opens", "PF", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_B))", "run on", Hold(Right), Until(Dead())), revealedBy: "Collapse_F"),
                new Betrayal("T8 [J]: a cat that keeps its mirrored hands after the inversion ends turns back over Pit_H", "PH", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=16.4)", "keep holding left", Jump(), Until(Airborne()), Until(Dead())), revealedBy: CatInverted),
                new Betrayal("T9 [J]: a cat that follows the door walks onto FakeFloor_D", "PD", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=7.6)", "follow the door", Until(Dead())), revealedBy: "FakeFloor_D"),
                new Betrayal("T10 [SS]: a cat that keeps holding left after Inv_H's unseen flip runs back into PK", "PK", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Inv_H))", "keep holding left", Until(Dead())), revealedBy: CatInverted),
                new Betrayal("Dead end D1 [OL]: the Slot, a shaft straight down from hall 5", "Pit_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Cat.Inverted))", "down the Slot", Hold(Left), Until(XAtMost(7.1f)), Jump(), Until(Dead())), revealedBy: "Slot_Floor"),
                new Betrayal("Dead end D2 [LW]: the Nook at hall 3's west end", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Shelf))", "into the Nook", Until(XAtMost(9.3f)), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))));
        }
    }
}
