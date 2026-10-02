using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 17's routes. Every wait is on a chain the cat set off (an element's fire tick) or on a
    // moving floor's own motion.
    static class L017Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // A cat standing on step k: its centre's height.
        static float StepY(int k) => L017Layout.StepTop(k) + .285f;

        // Out: east over Crack_1 (walking, under Spear_1); stop short of Block_2's column and let it land; hop it and keep
        // going over Collapse_3; step onto Slide_4 and stand still while it carries the cat to Ground_F.
        static RouteStep[] Out() => new[] {
            Hold(Right), Until(XAtLeast(11.3f)), Release(), Until(Still()), Until(Since("Block_2", 24)),
            Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(19.5f)), Release(), Until(GroundedOn("Slide_4")), Until(Still()),
            Until(Moving("Slide_4")), Until(Stopped("Slide_4")), Hold(Right), Until(XAtLeast(26f)),
        };

        // Far: hop the Sill, on past the far cut; back over the Sill into the nook before Arrow_5a arrives; wait there
        // while the volley builds the stair; out again and up it, V1 to V5, and west onto the upper floor.
        static RouteStep[] Far() => new[] {
            Until(XAtLeast(28f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtLeast(30.3f)), Release(), Until(Still()),
            Hold(Left), Until(XAtMost(29.6f)), Jump(), Until(Airborne()), Until(Grounded()), Until(XAtMost(26.4f)), Release(), Until(Still()),
            Until(Since("V5", 12)),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(28f)), Jump(), Until(XAtLeast(29.4f)), Release(), Until(Grounded()), Until(Still()),
            Jump(), Until(YAtLeast(StepY(0) + .6f)), Hold(Right), Until(XAtLeast(30.6f)), Release(), Until(GroundedOn("V1_Shaft")), Until(Still()),
            Jump(), Until(YAtLeast(StepY(1) + .6f)), Hold(Left), Until(XAtMost(28.6f)), Release(), Until(GroundedOn("V2_Shaft")), Until(Still()),
            Jump(), Until(YAtLeast(StepY(2) + .6f)), Hold(Right), Until(XAtLeast(30.6f)), Release(), Until(GroundedOn("V3_Shaft")), Until(Still()),
            Jump(), Until(YAtLeast(StepY(3) + .6f)), Hold(Left), Until(XAtMost(28.6f)), Release(), Until(GroundedOn("V4_Shaft")), Until(Still()),
            Jump(), Until(YAtLeast(StepY(4) + .6f)), Hold(Right), Until(XAtLeast(30.6f)), Release(), Until(GroundedOn("V5_Shaft")), Until(Still()),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(GroundedOn("UF_E")),
        };

        // Back: west, jumping Spikes_5e; hop Post_8; stop at the hole and let Arrow_8 pass over; jump the hole; west under
        // the Door_Ledge to Post_W; up onto Spear_D's shaft, then onto the ledge and into the door.
        static RouteStep[] Back() => new[] {
            Until(XAtMost(24.9f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(20.3f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(13.7f)), Release(), Until(Still()), Until(Since("Arrow_8", 94)),
            Hold(Left), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(1f)), Release(), Until(Still()),
            Hold(Right), Jump(), Until(GroundedOn("Spear_D_Shaft")), Jump(), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Out(), Far(), Back() };
            const int OutPart = 0, FarPart = 1, BackPart = 2;
            var solution = new Route("L017 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            // A detour off part `part` at step `index`, rejoining the solution at the same part's step `rejoin`.
            Route Detour(string name, int part, int index, int rejoin, params RouteStep[] then) =>
                new(name, Branch(parts, part, index, then).Concat(parts[part].Skip(rejoin)).Concat(parts.Skip(part + 1).SelectMany(p => p)).ToArray());

            return new RoomRoutes(solution,
                // PAX-101 (D-106): the floor past Post_8 sinks under a cat that stops on it; it rides it down and back up.
                Betrayal.Recovers("T9 [NW]: a cat that stops where the hop over Post_8 lands sinks with the floor, rides it back up and goes on", "Sink_9",
                    Detour("stop on the sinking floor", BackPart, 8, 8, Release(), Until(Still()), Until(Moving("Sink_9")), Until(Home("Sink_9")), Hold(Left))),
                // Out.
                new Betrayal("T1 [NJ]: a cat that hops Crack_1 is run through by Spear_1", "Spear_1", DeathCause.Hazard,
                    B("hop the crack", OutPart, 1, Until(XAtLeast(5.9f)), Jump(), Until(Dead()))),
                new Betrayal("Dead end D1 [LW]: a cat that climbs from Spear_1's shaft onto the Shelf over the start is spiked", "Spikes_D1", DeathCause.Hazard,
                    B("the shelf", OutPart, 1, Until(XAtLeast(6f)), Hold(Left), Until(XAtMost(2.6f)), Jump(), Until(GroundedOn("Spear_1_Shaft")),
                        Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T2 [W]: a cat that walks on under Block_2 is crushed", "Block_2", DeathCause.Hazard,
                    B("walk on", OutPart, 1, Until(Dead()))),
                new Betrayal("T3 [NW]: a cat that stops on Collapse_3 falls into Pit_3", "Pit_3", DeathCause.Hazard,
                    B("stop on it", OutPart, 9, Until(GroundedOn("Collapse_3")), Release(), Until(Dead())), revealedBy: "Collapse_3"),
                new Betrayal("T4 [SS]: a cat that walks while Slide_4 carries it walks off into Pit_4", "Pit_4", DeathCause.Hazard,
                    B("walk on it", OutPart, 13, Until(Moving("Slide_4")), Hold(Right), Until(Dead())), revealedBy: "Slide_4"),
                Betrayal.Recovers("Dead end D2 [LW]: a cat that stays on Slide_4 rides it home, rides it out again and steps off", "Slide_4",
                    Detour("ride home", OutPart, 15, 14, Until(Moving("Slide_4")), Until(Home("Slide_4")), Until(Moving("Slide_4")))),
                // Far.
                new Betrayal("T5 [BAIT]: a cat that stays on the far floor to watch the rearrangement meets Arrow_5a", "Arrow_5a", DeathCause.Hazard,
                    B("stay", FarPart, 7, Until(Dead()))),
                new Betrayal("T6 [NW]: a cat that stops where it steps off the stair is crushed by Block_6", "Block_6", DeathCause.Hazard,
                    B("stop at the top", BackPart, 0, Until(XAtMost(26.2f)), Release(), Until(Dead()))),
                // Back.
                new Betrayal("T7 [J]: a cat that walks on into Spikes_5e is spiked", "Spikes_5e", DeathCause.Hazard,
                    B("walk on", BackPart, 1, Until(Dead()))),
                new Betrayal("T8 [W]: a cat that jumps the hole as Arrow_8 comes is struck", "Arrow_8", DeathCause.Hazard,
                    B("jump as it comes", BackPart, 11, Until(Since("Arrow_8", 73)), Hold(Left), Jump(), Until(Dead()))));
        }
    }
}
