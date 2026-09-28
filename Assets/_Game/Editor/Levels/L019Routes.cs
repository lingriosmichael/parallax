using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 19's routes. The bridge and the maps are timed on the chains the cat sets off (each element's
    // fire tick); the movers are on the room's clock, so the route waits for each at home (the read).
    static class L019Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);

        // Bridge: to the cliff's edge; when Spear_1 has stuck, off the edge onto it; down steps 1 and 2; stop on step 3
        // while Sweeper_4 crosses step 4, then jump Sweeper_3 in place; on step 4, stay down while Sweeper_H passes over;
        // down step 5 onto Landing A.
        static RouteStep[] Off(int k) => new[] { Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(L019Layout.Face(k) - 1.55f)), Release() };
        static RouteStep[] Bridge() => new[] {
            Hold(Left), Until(XAtMost(60.8f)), Release(), Until(Still()), Until(Since("Spear_1", 17)),
            Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(59.9f)), Release(), Until(GroundedOn("Spear_1_Shaft")) }
            .Concat(Off(1)).Concat(new[] { Until(GroundedOn("Spear_2_Shaft")) })
            .Concat(Off(2)).Concat(new[] { Until(GroundedOn("Spear_3_Shaft")), Until(Still()),
                Until(Since("Sweeper_3", 10)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()) })
            .Concat(Off(3)).Concat(new[] { Until(GroundedOn("Spear_4_Shaft")), Until(Still()), Until(Since("Sweeper_H", 30)) })
            .Concat(Off(4)).Concat(new[] { Until(GroundedOn("Spear_5_Shaft")) })
            .Concat(new[] { Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(L019Layout.Face(5) - 1.5f)), Release(), Until(GroundedOn("Landing_A")) }).ToArray();

        // Map A: hop Thorns_5 into Orb_A; mirrored, hop to P1, P2, P3 and P4, switching back when the inversion ends in the
        // last jump; on P4, watch Mover_1 leave and board it when it's back (the read), ride it west, step onto Ledge_M.
        static RouteStep[] MapA() => new[] {
            Hold(Left), Until(XAtMost(50.8f)), Jump(), Until(Grounded()), Until(Fired("Orb_A")),
            Hold(Right), Until(XAtMost(44.4f)), Jump(), Until(GroundedOn("P1")), Until(XAtMost(40f)), Jump(), Until(GroundedOn("P2")),
            Until(XAtMost(35.6f)), Jump(), Until(GroundedOn("P3")), Until(XAtMost(31.3f)), Jump(), Until(Since("Orb_A", 151)), Hold(Left),
            Until(GroundedOn("P4")), Release(), Until(Still()),
            Until(Moving("Mover_1")), Until(Home("Mover_1")), Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(25f)), Release(), Until(GroundedOn("Mover_1")),
            Until(Moving("Mover_1")), Until(Stopped("Mover_1")), Hold(Left), Until(GroundedOn("Ledge_M")),
        };

        // Map B: on to the edge and jump; Orb_B inverts the cat in the air, so switch to Right onto Q1; watch Mover_2 leave
        // and board it when it's back; ride it down; walk steadily west under Spear_9 and over Floor_C to the door.
        static RouteStep[] MapB() => new[] {
            Until(XAtMost(16.9f)), Jump(), Until(Fired("Orb_B")), Hold(Right).Timed(TimedMode.Shift), Until(XAtMost(13f)), Release(), Until(GroundedOn("Q1")),
            Until(Still()), Until(Since("Orb_B", 71)), Until(Moving("Mover_2")), Until(Home("Mover_2")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(10f)), Release(), Until(GroundedOn("Mover_2")),
            Until(Moving("Mover_2")), Until(Stopped("Mover_2")), Hold(Left), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Bridge(), MapA(), MapB() };
            const int BridgePart = 0, MapAPart = 1, MapBPart = 2;
            var solution = new Route("L019 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // The bridge.
                new Betrayal("T1 [W]: a cat that steps off the cliff before Spear_1 has stuck falls through the volley", "Spear_4", DeathCause.Hazard,
                    B("step off at once", BridgePart, 4, Hold(Left), Until(Dead()))),
                new Betrayal("T2 [NW]: a cat that stops on step 2 meets Sweeper_2", "Sweeper_2", DeathCause.Hazard,
                    B("stop on step 2", BridgePart, 13, Until(Dead()))),
                new Betrayal("T3 [J]: a cat that stands on step 3 meets Sweeper_3", "Sweeper_3", DeathCause.Hazard,
                    B("stand", BridgePart, 19, Until(Dead()))),
                new Betrayal("T4 [W]: a cat that steps straight down from step 3 meets Sweeper_4", "Sweeper_4", DeathCause.Hazard,
                    B("step down at once", BridgePart, 17, Hold(Left), Until(XAtMost(L019Layout.Face(3) - 1.55f)), Release(), Until(Dead()))),
                new Betrayal("T5 [NJ]: a cat that jumps the arrow on step 4 as it did on step 3 meets Sweeper_H", "Sweeper_H", DeathCause.Hazard,
                    B("jump it", BridgePart, 27, Until(Since("Sweeper_H", 10)), Jump(), Until(Dead()))),
                // Map A.
                new Betrayal("T6 [B]: a cat that keeps holding left when Orb_A flips it runs back onto Thorns_5", "Thorns_5", DeathCause.Hazard,
                    B("hold left", MapAPart, 5, Hold(Left), Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T7 [NW]: a cat that stops on P2 falls into the pit", "Pit_M1", DeathCause.Hazard,
                    B("stop on P2", MapAPart, 12, Release(), Until(Dead())), revealedBy: "P2"),
                new Betrayal("T8 [SS]: a cat that keeps holding right as the inversion ends falls short of P4", "Pit_M1", DeathCause.Hazard,
                    B("keep right", MapAPart, 18, Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T9 [W]: a cat that jumps for Mover_1 as it leaves falls into the pit", "Pit_M1", DeathCause.Hazard,
                    B("jump as it leaves", MapAPart, 23, Hold(Left), Jump(), Until(Dead())), revealedBy: "Mover_1"),
                Betrayal.Recovers("Dead end D1 [LW]: a cat that stays on Mover_1 rides it back to P4 and out again, then steps off", "Mover_1",
                    new Route("the ride back", Branch(parts, MapAPart, 31, Until(Moving("Mover_1")), Until(Home("Mover_1")), Until(Moving("Mover_1")), Until(Stopped("Mover_1")))
                        .Concat(parts[MapAPart].Skip(31)).Concat(parts[MapBPart]).ToArray())),
                // Map B.
                new Betrayal("T10 [B]: a cat that heads for the lift while still flipped walks off Q1 onto Thorns_M2", "Thorns_M2", DeathCause.Hazard,
                    B("head for the lift", MapBPart, 7, Hold(Left), Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T11 [SS]: a cat that steps off the lift before it stops lands on Thorns_W", "Thorns_W", DeathCause.Hazard,
                    B("step off early", MapBPart, 17, Until(Below(4.2f)), Hold(Left), Until(Dead())), revealedBy: "Mover_2"),
                new Betrayal("Dead end D2 [B]: a cat that turns back east once the lift has gone falls into its shaft", "Pit_L", DeathCause.Hazard,
                    B("back east", MapBPart, 19, Until(XAtMost(7.3f)), Release(), Until(Still()), Until(Moving("Mover_2")), Hold(Right), Until(Dead())), revealedBy: "Mover_2"),
                new Betrayal("T12 [NJ]: a cat that hops Floor_C meets Spear_9", "Spear_9", DeathCause.Hazard,
                    B("hop the floor", MapBPart, 19, Until(XAtMost(6.9f)), Jump(), Until(Dead()))),
                new Betrayal("T13 [NW]: a cat that stops on Floor_C falls onto Spikes_C", "Spikes_C", DeathCause.Hazard,
                    B("stop on it", MapBPart, 19, Until(XAtMost(5.6f)), Release(), Until(Dead())), revealedBy: "Floor_C"));
        }
    }
}
