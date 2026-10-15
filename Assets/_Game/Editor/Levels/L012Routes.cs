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
                // PAX-107: Hinge_4 stands up behind: keep running (mirrored) and jump P4; Drop_4 comes down ahead: stop, let it
                // land; Orb_A's flip ends as it lands: switch hands, hop it and down the hole.
                // PAX-107 round nineteen: Orb_A's flip ends over Hinge_4's notch: switch hands; Orb_P flips them again just before P4:
                // switch back at once and jump P4 mirrored.
                Until(XAtLeast(18.8f)), Hold(Right).Timed(TimedMode.Shift),
                Until(Fired("Hinge_4")), Until(Fired("Orb_P")), Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(21.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("F4_B3")),
                Release(), Until(Still()), Until(Stopped("Drop_4")),
                Hold(Right), Jump(), Until(Airborne()), Until(Grounded().And(new RouteCondition("Y<11", v => v.Last.Y < 11f))),
                // Hall 3: west under the Shelf to Orb_B; Collapse_F opens ahead: hands flipped, the same stick walks the cat back
                // out; onto the Shelf, across, and down the hole.
                // PAX-107: Drop_3 comes down ahead: stop, let it land, hop it.
                Hold(Left), Until(XAtMost(26.4f)), Release(), Until(Still()), Until(Stopped("Drop_3")),
                Hold(Left), Until(XAtMost(25.9f)), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Fired("Orb_B")), Until(XAtLeast(20.6f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(Airborne()), Until(GroundedOn("Shelf")),
                // PAX-107: Shelf_W rises into hall 4's slab: wait on the Shelf for it to go back down.
                Release(), Until(Still()), Until(Moving("Shelf_W")), Until(Home("Shelf_W")), Hold(Right),
                Until(XAtMost(8.5f)), Until(Grounded().And(new RouteCondition("Y<6", v => v.Last.Y < 6f))),
                // Handoff: still flipped by Orb_B, run east (hold left) through Orb_G, jump Pit_H, switch hands as it ends; down
                // to hall 1 and west: jump PK, play Inv_H's flip mirrored, switch back, and jump to the door.
                Hold(Left), Until(Fired("Orb_G")), Until(XAtLeast(16.4f)), Jump(), Until(Airborne()), For(18), Hold(Right).Timed(TimedMode.Shift), Until(Grounded()),
                // PAX-107 round thirteen: Slam_H2 comes down ahead: stop, let it slam and go back up; then Lift_H2 shoots up
                // ahead: stop, let it go up, walk under it through its socket and hop out.
                Until(XAtLeast(21.3f)), Release(), Until(Still()), Until(Moving("Slam_H2")), Until(Home("Slam_H2")),
                Hold(Right), Until(XAtLeast(24.9f)), Release(), Until(Still()), Until(Moving("Lift_H2")), Until(Stopped("Lift_H2")),
                Hold(Right), Until(GroundedOn("F2_Seat")), Jump(), Until(Airborne()),
                Until(XAtLeast(29.3f)), Until(Grounded().And(new RouteCondition("Y<1", v => v.Last.Y < 1f))),
                // PAX-107: Piston_1 shoots up ahead: wait for it to go back down.
                Hold(Left), Until(XAtMost(26.6f)), Release(), Until(Still()), Until(Home("Piston_1")),
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
                new Betrayal("T3 [J]: a cat that stops at Orb_A is pushed into PB by Block_E, sliding on after it lands", "PB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_A))", "stop", Release(), Until(Dead())), revealedBy: "Block_E"),
                new Betrayal("T3 [J]: a cat that keeps running forward as Block_E pushes it falls into PB", "PB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_A))", "pushed, run forward", Release(), Until(Moving("Block_E")), Hold(Left), Until(Dead())), revealedBy: "Block_E"),
                new Betrayal("T5 [NW]: a cat that stops where the hop lands is caught by Spikes_D", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.55)", "stop after the hop", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Dead())), revealedBy: "Orb_A"),
                // Orb_A's trigger box runs to x 14.4 (the layout says why): turning back re-enters it from the east after the
                // room has gone off, and the way back leads into Spikes_D.
                new Betrayal("T5 [NW]: a cat that turns back after the hop walks into Spikes_D", "Spikes_D", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=9.55)", "turn back", Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(16.5f)), Hold(Right), Until(Dead()))),
                new Betrayal("T6 [SS]: a cat that backs off by instinct (holding right, which now walks it back) meets Block_E as it lands and is pushed into PB", "PB", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_A))", "back off", Hold(Right), Until(Dead())), revealedBy: "Block_E"),
                new Betrayal("T12 [NW]: a cat that stops on hall 4's run is pushed into P4 by Hinge_4", "P4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Hinge_4))", "stop", Release(), Until(Dead())), revealedBy: "Hinge_4"),
                new Betrayal("T10 [SS]: a cat that keeps holding right after Orb_P flips its hands runs back into Hinge_4, which shoves it into P4", "P4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_P))", "keep holding right", Until(Dead())), revealedBy: "Orb_P"),
                new Betrayal("T11 [B]: a cat that runs on past P4 is crushed by Drop_4, falling from the ceiling", "Drop_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(F4_B3))", "run on", Until(Dead()))),
                new Betrayal("T7 [B]: a cat that plays Orb_B's flip mirrored and runs on falls where Collapse_F opens", "PF", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_B))", "run on", Hold(Right), Until(Dead())), revealedBy: "Collapse_F"),
                new Betrayal("T13 [W]: a cat that runs on across the Shelf is pressed into hall 4's slab by Shelf_W", "Shelf_W", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Shelf))", "run on", Until(Dead()))),
                new Betrayal("T11 [B]: a cat that runs on in hall 3 is crushed by Drop_3, falling from the ceiling", "Drop_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=26.4)", "run on", Until(Dead()))),
                new Betrayal("T12 [NW]: a cat that stops as Orb_G flips its hands is pushed into Pit_H by Hinge_2", "PH", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Orb_G))", "stop", Release(), Until(Dead())), revealedBy: "Hinge_2"),
                new Betrayal("T8 [J]: a cat that keeps its mirrored hands after the inversion ends turns back over Pit_H", "PH", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=16.4)", "keep holding left", Jump(), Until(Airborne()), Until(Dead())), revealedBy: CatInverted),
                new Betrayal("T14 [W]: a cat that runs on in hall 2 is crushed by Slam_H2, coming down from the ceiling", "Slam_H2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=21.3)", "run on", Until(Dead()))),
                new Betrayal("T13 [W]: a cat that runs on onto Lift_H2 is carried up into hall 3's slab", "Lift_H2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=24.9)", "run on", Until(Dead()))),
                new Betrayal("T13 [W]: a cat that runs on in hall 1 rides Piston_1 up into hall 2's slab", "Piston_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=26.6)", "run on", Until(Dead()))),
                new Betrayal("T9 [J]: a cat that follows the door walks onto FakeFloor_D", "PD", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=7.6)", "follow the door", Until(Dead())), revealedBy: "FakeFloor_D"),
                new Betrayal("T10 [SS]: a cat that keeps holding left after Inv_H's unseen flip runs back into PK", "PK", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Inv_H))", "keep holding left", Until(Dead())), revealedBy: CatInverted),
                new Betrayal("Dead end D1 [OL]: the Slot, a shaft straight down from hall 5", "Pit_S", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Moving(Cat.Inverted))", "down the Slot", Hold(Left), Until(XAtMost(7.1f)), Jump(), Until(Dead())), revealedBy: "Slot_Floor"),
                new Betrayal("Dead end D2 [LW]: the Nook at hall 3's west end", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Home(Shelf_W))", "into the Nook", Hold(Right), Until(XAtMost(9.3f)), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead()))));
        }
    }
}
