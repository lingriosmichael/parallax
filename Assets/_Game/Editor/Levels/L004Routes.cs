using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 4's anatomy. T1 is jumped; T2, the flip everyone jumps into, is walked under; T3 (spikes on
    // the roof) are jumped upside down; T4 punishes that jump (the landing gives way: jump straight off it); T5 punishes
    // keeping on along the roof (the door backs away over a section that gives way: go back down, along the slab's top,
    // and up under the door). The flip toward the door from the start is the dead end.
    // PAX-103: the end is the climb (L004Layout): down Flip_D to the slab's top, onto Step_1, ride Ride_2 left, Step_3, ride
    // Ride_4 left, Step_5, and the door step. Each hop onto a 1 u platform releases mid-air (a full jump carries 3.9 u),
    // as a timed step; each ride is boarded as it comes home (it waits there 50 ticks). T5 and the door pads' landings went
    // with the door's retreat.
    // D-117: the cat is inverted from the first tick to the end (L004Layout's Invert); route moves are stick input, so every
    // Hold(Right) walks it left and every Hold(Left) right (D-087 (7)).
    static class L004Routes
    {
        // PAX-103: from the slab's top under Flip_D, up the climb to the door.
        static RouteStep[] Climb() => new RouteStep[] {
            Release(), Until(Still()),
            Hold(Right), Until(XAtMost(12f)), Jump(), Until(XAtMost(11.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Step_1")), Until(Still()),
            Until(Moving("Ride_2")), Until(Home("Ride_2")), Hold(Right), Jump(), Until(XAtMost(9.7f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ride_2")), Until(Stopped("Ride_2")),
            Hold(Right), Jump(), Until(XAtMost(6.7f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Step_3")), Until(Still()),
            Until(Moving("Ride_4")), Until(Home("Ride_4")), Hold(Right), Jump(), Until(XAtMost(5.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Ride_4")), Until(Stopped("Ride_4")),
            Hold(Right), Jump(), Until(XAtMost(2.2f)), Release().Timed(TimedMode.Shift), Until(GroundedOn("Step_5")), Until(Still()),
            Hold(Right), Jump(), Until(RoomComplete()) };

        public static RoomRoutes Build()
        {
            var solution = new Route("L004 solution", new RouteStep[] {
                // PAX-102: the opening arrow; stop at once, let it land, walk on through it.
                Hold(Left), Until(XAtLeast(10.1f)), Release(), Until(Still()), Until(Stopped("Arrow_O")), Hold(Left) }.Concat(FromSpikes()).ToArray());

            // D-118: the cat that flips up at Flip_L, turns back as it rises and lands under the slab past Spikes_L walks over
            // Left_Post into the gap, climbs down it upside down (Left_Moss, then the corbel's moss), comes up under the corbel's
            // foot, walks right into Flip_A, lands right side up past Spikes_1, and goes on as the solution does. Route moves are
            // inverted stick input: Hold(Left) moves the cat right.
            var escape = new Route("stuck under the slab", new RouteStep[] {
                Hold(Right), Until(GravityUp()), Hold(Left), Until(Grounded()), Until(XAtLeast(13.3f)), Release(), Until(Still()),
                Hold(Right), Jump(), Until(Clinging()), Release(),
                Jump(), For(2), Hold(Left), Until(Clinging()), Release(),
                Jump(), For(10), Hold(Left), Until(GravityDown()), Until(GroundedOn("Ground")) }
                .Concat(PastSpikes()).ToArray());

            return Routes(solution, escape);
        }

        // Over Spikes_1 and on to the door.
        static RouteStep[] FromSpikes() => new RouteStep[] {
                Until(XAtLeast(14.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // D-119: the first Arrow_Run catches up past Flip_A: hop it.
                Until(XAtLeast(21.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()) }.Concat(PastSpikes()).ToArray();

        // From the ground past Spikes_1, moving right, to the door.
        static RouteStep[] PastSpikes() => new RouteStep[] {
                // Under Flip_A, into Flip_R, up through the gap to the roof.
                Until(GravityUp()), Until(GroundedOn("Roof_R2")),
                // PAX-102: wait on Roof_R2 for Arrow_7's shot, then over Sink_R and the lane together.
                Release(), Until(Still()), Until(Moving("Arrow_7")), Until(Stopped("Arrow_7")),
                Hold(Right), Until(XAtMost(22.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Roof_4")), Jump(), Until(GroundedOn("Roof_M")),
                // Down through Flip_D to the slab's top, then the climb.
                Jump(), Until(GravityDown()), Release(), Until(GroundedOn("Slab")) }.Concat(Climb()).ToArray();

        static RoomRoutes Routes(Route solution, Route escape)
        {
            return new RoomRoutes(solution,
                new Betrayal("T1: Spikes_1 rise under a cat that runs on", "Spikes_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_O))", "run on", Hold(Left), Until(Dead()))),
                // PAX-102: the opening arrow, the rising roof section and the roof walk's repeating arrow.
                new Betrayal("T6: Arrow_O comes down on a cat that runs on from the start", "Arrow_O", DeathCause.Hazard,
                    new Route("run on", Hold(Left), Until(Dead()))),
                new Betrayal("T7: Sink_R rises into the recess with a cat that stops under it", "RecessC_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", "stop under the section", Hold(Right), Until(XAtMost(27.5f)), Release(), Until(Dead())), revealedBy: "Sink_R"),
                new Betrayal("T8: Arrow_7's shot hits a cat that stops in its lane on the roof", "Arrow_7", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", "stop in the lane", Hold(Right), Until(XAtMost(25.6f)), Release(), Until(Dead())),
                    escape: d => Route.PrefixOf(solution, "Until(GroundedOn(Roof_R2))", $"step out {d} ticks after the shot",
                        new RouteStep[] { Hold(Right), Until(XAtMost(25.6f)), Release(), Until(Revealed("Arrow_7")) }
                        .Concat(d > 0 ? new[] { For(d) } : System.Array.Empty<RouteStep>())
                        .Concat(new[] { Hold(Right), Until(XAtMost(22.4f)), Jump(), Until(Airborne()), Until(GroundedOn("Roof_4")), Jump(), Until(GroundedOn("Roof_M")),
                            Jump(), Until(GravityDown()), Release(), Until(GroundedOn("Slab")) }).Concat(Climb()).ToArray())),
                new Betrayal("T2: Flip_A sends a cat that jumps into it onto spikes under the slab", "Spikes_A", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Grounded)", "jump into the flip", Jump(), Until(Dead()))),
                new Betrayal("T3: spikes on the roof under a cat walking it upside down", "Spikes_3", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_7))", "walk on", Hold(Right), Until(Dead()))),
                new Betrayal("T4: the roof where the jump lands gives way under a cat that stops", "Recess4_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Roof_4))", "stop where it lands", Release(), Until(Dead())), revealedBy: "Roof_4"),
                // PAX-103: the climb's two rides. A cat that hops for a ride's waiting place after it has left falls onto the spikes.
                new Betrayal("T9: a cat that hops for Ride_2 after it has left falls onto the climb's spikes", "Climb_Spikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Step_1))", "hop after the ride left",
                        Until(Still()), Until(Home("Ride_2")), For(10), Until(Moving("Ride_2")), For(35), Hold(Right), Jump(), Until(XAtMost(9.7f)), Release(), Until(Dead())), revealedBy: "Ride_2"),
                new Betrayal("T10: a cat that hops for Ride_4 after it has left falls onto the climb's spikes", "Climb_Spikes", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Step_3))", "hop after the ride left",
                        Until(Still()), Until(Home("Ride_4")), For(10), Until(Moving("Ride_4")), For(35), Hold(Right), Jump(), Until(XAtMost(5.2f)), Release(), Until(Dead())), revealedBy: "Ride_4"),
                // D-119: the left wall's launchers.
                new Betrayal("T11: Arrow_Run hits a cat that walks on past Flip_A without hopping it", "Arrow_Run", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=21.6)", "walk on", Until(Dead()))),
                new Betrayal("T12: Arrow_Jump hits a cat that stops past Flip_A and jumps as it comes", "Arrow_Jump", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=21.6)", "stop and jump", Jump(), Until(Airborne()), Until(Grounded()), Release(), Until(Still()), For(20), Jump(), Until(Dead()))),
                new Betrayal("Dead end: the flip toward the door drops a cat onto spikes under the slab", "Spikes_L", DeathCause.Hazard,
                    new Route("toward the door", Hold(Right), Until(Dead()))),
                Betrayal.Recovers("The way out from under the slab: a cat that lands past Spikes_L climbs down the mossy gap", R.CatGravity, escape));   // a flip shows nothing; the cat's gravity does
        }
    }
}
