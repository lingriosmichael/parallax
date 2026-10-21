using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-160: level 2's routes ("Backwards"). Route moves are stick input (D-087 (7)): from Invert on, Hold(Right) walks the cat
    // west and Hold(Left) east. The solution runs west into Invert, presses the other way at once and jumps Spikes_1 ahead of
    // Hinge_1, walks into Flip_U1, jumps CC_1 on the ceiling, stops while Lift_1 goes up and down, jumps Pop_C over the shaft and
    // falls up it; east along the upper ceiling ahead of Push_2, over Spikes_C2, into Flip_D2; then east ahead of Hinge_W, over
    // Pop_2 and Crumble, to the door. Every betrayal is a way a run meets a trap: stopping, walking on, or pressing the way the
    // cat went before Invert.
    static class L002Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        public static RoomRoutes Build()
        {
            var solution = new Route("L002 solution",
                // West into Invert; from here the stick is backwards. Jump Spikes_1 ahead of Hinge_1, into Flip_U1.
                Hold(Left), Until(Fired("Invert")), Hold(Right), Until(XAtMost(24.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // On the ceiling: jump CC_1, stop where the jump lands while Lift_1 goes up and down, jump Pop_C and fall up the shaft.
                Until(GravityUp()), Until(GroundedOn("SA_2")), Until(XAtMost(15.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(XAtMost(12f)), Until(Grounded()),
                Release(), Until(Still()), Until(Fired("Lift_1")), Until(Since("Lift_1", 52)),
                Hold(Right), Until(Fired("Pop_C")), Jump().Timed(TimedMode.Shift), Until(YAtLeast(10f)), Until(GroundedOn("Roof_A")),
                // The upper ceiling, east: ahead of Push_2, over Spikes_C2, into Flip_D2.
                Hold(Left), Until(XAtLeast(7.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Roof_B")),
                Until(GravityDown()), Until(GroundedOn("SA_2")),
                // Ahead of Hinge_W: jump Pop_2 as it springs, jump Crumble, to the door.
                Until(Fired("Pop_2")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(24f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: Hinge_1 shoves a cat that stops past Invert onto Spikes_1", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Invert))", "stop", Release(), Until(Dead())), revealedBy: "Hinge_1"),
                new Betrayal("T2: a cat that keeps pressing the way it went turns back into Hinge_1, which shoves it onto Spikes_1", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Invert))", "keep pressing", Until(Dead())), revealedBy: "Hinge_1"),
                new Betrayal("T3: CC_1 gives way under a cat that walks on along the ceiling", "Spikes_CC1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(SA_2))", "walk on", Until(Dead())), revealedBy: "CC_1"),
                new Betrayal("T4: Lift_1 crushes a cat that walks on where the jump over CC_1 lands", "Lift_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=12)", "walk on", Until(Grounded()), Until(Dead()))),
                new Betrayal("T5: Pop_C springs out of the ceiling under a cat that walks on", "Pop_C", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Pop_C))", "walk on into Pop_C", Until(Dead()))),
                new Betrayal("T6: Push_2 shoves a cat that stops short of Spikes_C2 onto them", "Spikes_C2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_A))", "stop", Hold(Left), Until(XAtLeast(6.4f)), Release(), Until(Dead())), revealedBy: "Push_2"),
                new Betrayal("T7: Hinge_W catches a cat that stops on the upper floor", "Hinge_W", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GravityDown)", "stop", Until(Grounded()), Release(), Until(Dead()))),
                new Betrayal("T8: Pop_2 springs up under a cat that runs on", "Pop_2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Pop_2))", "run on into Pop_2", Until(Dead()))),
                new Betrayal("T9: Crumble gives way under a cat that runs on", "Spikes_Cr", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Pop_2))", "run on onto Crumble", Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())), revealedBy: "Crumble"));
        }
    }
}
