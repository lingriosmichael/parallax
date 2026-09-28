using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 16's routes. Waits are on the chains the cat sets off (an element's fire tick).
    static class L016Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);

        // Bed: east along the top; jump from before Lip_2 into the gap; stand in the Dip while Arrow_3 passes over; hop out west
        // onto the ground, over Post_E, and west along the floor to V_B; jump up onto it and climb it into its flip, up onto
        // the underside.
        static RouteStep[] Bed() => new[] {
            Hold(Right), Until(XAtLeast(25.9f)), Jump(), Until(XAtLeast(30f)), Release(), Until(Grounded()), Until(Since("Arrow_3", 27)),
            Hold(Left), Jump(), Until(Airborne()), Until(Grounded()), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtMost(3.3f)), Release(), Until(Still()),
            Hold(Up), Jump().Timed(TimedMode.Shift), Until(Climbing()), Until(GravityUp()), ReleaseClimb(), Until(Grounded()),
        };

        // Underside: east to V_7; grab it (push screen-down), climb down, leap east under Flip_H7 onto Bed_B; to V_8, grab it,
        // climb down and leap across Recess_8; east to the gap and up it onto the ceiling.
        static RouteStep[] Underside() => new[] {
            Hold(Right), Until(XAtLeast(9.2f)), Release(), Until(Still()), Hold(Down), Until(Climbing()), Until(Below(6f)),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(12.6f)), Release(), ReleaseClimb(), Until(Grounded()),
            Hold(Right), Until(XAtLeast(14.2f)), Release(), Until(Still()), Hold(Down), Until(Climbing()), Until(Below(5.4f)),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(20.6f)), ReleaseClimb(), Until(Grounded()),
            Until(XAtLeast(30f)), Until(YAtLeast(18f)), Until(Grounded()),
        };

        // Sky: west along the ceiling, jumping Spikes_C, into Flip_S0; let go, and fall straight onto the Bed's top; step west
        // off the landing (Block_S3 comes down on it) and wait while Spikes_S1 are up; west over them into Flip_S2 and let go,
        // back up onto the ceiling; jump Spikes_S5; west to the door.
        static RouteStep[] Sky() => new[] {
            Hold(Left), Until(XAtMost(25.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityDown()), Release(), Until(Grounded()), Hold(Left), Until(XAtMost(17.2f)), Release(), Until(Still()),
            Until(Since("Spikes_S1", 62)), Hold(Left).Timed(TimedMode.Shift), Until(GravityUp()), Release(), Until(Grounded()), Hold(Left),
            Until(XAtMost(9.9f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Bed(), Underside(), Sky() };
            const int BedPart = 0, UnderPart = 1, SkyPart = 2;
            var solution = new Route("L016 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Bed.
                new Betrayal("T2 [J]: a cat that walks on to the Bed's end drops with Lip_2 onto Spikes_2", "Spikes_2", DeathCause.Hazard,
                    B("walk on", BedPart, 1, Until(Dead())), revealedBy: "Lip_2"),
                new Betrayal("T3 [SS]: a cat that jumps out of the Dip at once meets Arrow_3", "Arrow_3", DeathCause.Hazard,
                    B("out at once", BedPart, 6, Hold(Left), Jump(), Until(Dead()))),
                new Betrayal("Dead end D1 [LW]: a cat that climbs V_D, straight up at the door, meets Spikes_X under the Bed", "Spikes_X", DeathCause.Hazard,
                    B("V_D", BedPart, 13, Until(XAtMost(1.3f)), Release(), Until(Still()), Hold(Up), Jump(), Until(Climbing()), Until(Dead()))),
                new Betrayal("T6 [OL]: a cat that climbs V_A is flipped onto Tile_6, which gives way onto Thorns_6", "Thorns_6", DeathCause.Hazard,
                    B("V_A", BedPart, 13, Until(XAtMost(8.3f)), Release(), Until(Still()), Hold(Up), Jump(), Until(Climbing()), Until(GravityUp()), ReleaseClimb(), Until(Dead())), revealedBy: "Tile_6"),
                // Underside.
                new Betrayal("Dead end D2 [LW]: a cat that goes west under the Bed from V_B's top meets Spikes_X", "Spikes_X", DeathCause.Hazard,
                    B("west", UnderPart, 0, Hold(Left), Until(Dead()))),
                new Betrayal("T7 [LW]: a cat that jumps Recess_7 meets the hidden Flip_H7 and falls onto Spikes_7", "Spikes_7", DeathCause.Hazard,
                    B("jump it", UnderPart, 1, Until(XAtLeast(9.4f)), Jump(), Until(Dead()))),
                new Betrayal("T8 [LW]: a cat that jumps Recess_8 falls short into it, onto Spikes_8", "Spikes_8", DeathCause.Hazard,
                    B("jump it", UnderPart, 13, Hold(Right), Until(XAtLeast(14.4f)), Jump(), Until(Dead()))),
                // Sky.
                new Betrayal("T9 [NW]: a cat that stays where it lands is crushed by Block_S3", "Block_S3", DeathCause.Hazard,
                    B("stay", SkyPart, 8, Until(Dead()))),
                new Betrayal("T10 [W]: a cat that walks on at once runs onto Spikes_S1", "Spikes_S1", DeathCause.Hazard,
                    B("walk on", SkyPart, 8, Hold(Left), Until(Dead()))),
                new Betrayal("T11 [NJ]: a cat that backs off from the storm to the Hedge meets Spikes_H", "Spikes_H", DeathCause.Hazard,
                    B("back off", SkyPart, 8, Hold(Right), Until(Dead()))),
                new Betrayal("T12 [J]: a cat that walks on along the ceiling runs onto Spikes_S5", "Spikes_S5", DeathCause.Hazard,
                    B("walk on", SkyPart, 18, Until(Dead()))),
                new Betrayal("T13 [J]: a cat that walks on from the gap along the ceiling runs onto Spikes_C", "Spikes_C", DeathCause.Hazard,
                    B("walk on", SkyPart, 2, Until(Dead()))));
        }
    }
}
