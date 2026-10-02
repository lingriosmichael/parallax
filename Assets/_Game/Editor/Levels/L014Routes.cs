using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 14's routes. A vine is caught in the air by holding Down (|Climb| >= 0.5, D-089): Down does
    // nothing on the ground, so the routes hold it through the walk-offs and leaps and let a grab happen on the first overlap.
    static class L014Routes
    {
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        // Standing, off the vine (a grounded climbing cat lets go by pushing into its ground, D-092).
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        // At least `ticks` room ticks since the element fired (Spear_L: its 16-tick tell and its flight, then stuck).
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);

        // Drop: off the lip onto V1, down it, back under the lip onto V2, down to the Trunk.
        static RouteStep[] Drop() => new[] {
            Hold(Left), Until(XAtMost(13.2f)), Hold(Down), Until(On("V1", 11f)), Release(),
            For(44), Hold(Right), Jump().Timed(TimedMode.Shift), Until(On("V2", 14.6f)), Release(),
            Until(Fired("Spear_V2")), ReleaseClimb(), Until(Since("Spear_V2", 44)), Hold(Down).Timed(TimedMode.Shift),
            Until(Landed()), ReleaseClimb(),
        };

        // Trunk, to V_Up (the alcove's vine).
        static RouteStep[] ToAlcove() => new[] { Hold(Right), Until(XAtLeast(20.9f)) };

        // Trunk, from V_Up: set Block_T off and back off; once it has landed, hop it and off the east end onto V3; out of Spear_4's lane when it fires; onto the stuck spear; west into the Trough while Spear_L passes over; out, over the stuck spear and Curb_L to Shrink,
        // stop on it, and step off onto V4 before it has narrowed to the cat; down to the lip.
        static RouteStep[] Trunk() => new[] {
            Until(XAtLeast(22.6f)), Hold(Left), Until(XAtMost(22.2f)), Release(), Until(Stopped("Block_T")),
            Hold(Right).Timed(TimedMode.Shift), Hold(Down), Until(XAtLeast(22.8f)), Jump(), Until(On("V3", 28f)), Release(),
            // PAX-101 (D-106): stop on V3 as Arrow_V fires, let it stick in Shaft_W, climb on down.
            Until(Fired("Arrow_V")), ReleaseClimb(), Until(Moving("Arrow_V")), For(8), Until(Stopped("Arrow_V")), Hold(Down),
            Until(Fired("Spear_4")), Hold(Up), For(8), ReleaseClimb(), Until(Stopped("Spear_4")),
            Hold(Left), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Spear_4_Shaft")),
            Until(GroundedOn("Trough")), Until(Since("Spear_L", 26)), Jump().Timed(TimedMode.Shift), Until(GroundedOn("Low")),
            Until(XAtMost(14f)), Jump(), Until(XAtMost(8.6f)), Release(), Until(Still()),
            Hold(Left).Timed(TimedMode.Shift), Hold(Down), Until(On("V4", 6.8f)), Release(),
            Until(Landed()), ReleaseClimb(),
        };

        // Canopy: jump Spikes_K onto C1 (caught low); vine to vine, each leap rising about 1.6, so the cat leaps at once from C1
        // and C2 and climbs down on C3-C6 first; far onto the exit; the door.
        static readonly string[] Vines = { "C1", "C2", "C3", "C4", "C5", "C6" };
        static readonly float[] VineX = { 11.4f, 14f, 16.6f, 19.2f, 21.8f, 24.4f };
        static readonly int[] Linger = { 2, 2, 20, 20, 20, 10 };

        // Holding Down only once clear of V4, which the cat on the lip stands in.
        static RouteStep[] ToC1() => new[] { Hold(Right), Jump(), Until(XAtLeast(7.6f)), Hold(Down), Until(On("C1", 11.4f)) };

        // On C1 ... C`vines` (untimed); the timed version is the solution's.
        static RouteStep[] Through(int vines, bool timed = false)
        {
            var steps = ToC1().ToList();
            for (int i = 1; i < vines; i++)
                steps.AddRange(new[] { For(Linger[i - 1]), timed ? Jump().Timed(TimedMode.Shift) : Jump(), Until(On(Vines[i], VineX[i])) });
            return steps.ToArray();
        }

        static RouteStep[] Canopy(bool timed = true) => Through(6, timed)
            .Concat(new[] { For(Linger[5]), timed ? Jump().Timed(TimedMode.Shift) : Jump(), Until(Landed()), ReleaseClimb(), Until(RoomComplete()) }).ToArray();

        static RouteStep[] Untimed(RouteStep[] steps) => steps.Select(s => s.Timing == TimedMode.None ? s : s.Timed(TimedMode.None)).ToArray();

        // Standing, and the death hold over.
        static RouteCondition Live() => new("live", v => v.Last.Grounded && !v.Last.Holding);

        // From the Canopy checkpoint: stand, then these steps.
        static Route FromCanopy(Route solution, string name, params RouteStep[] then) =>
            Route.FromSection(solution, "Canopy", name, new[] { Release(), ReleaseClimb(), Until(Live()), For(5) }.Concat(then).ToArray());

        // The steps after the first one labelled `label`.
        static RouteStep[] After(RouteStep[] steps, string label) => steps.SkipWhile(s => s.Label != label).Skip(1).ToArray();

        // T1's declared escape (D-097): a cat that stops on the lip drops with it; `reactionTicks` after it visibly goes, the
        // cat holds Right and Down and catches V2 just below. Caught low (inside Spear_V2's lane), it climbs above the lane
        // first; then it waits out the spear, climbs down to the Trunk and finishes the level.
        public static Route EscapeLip(int reactionTicks) => new($"Lip_1: catch V2 {reactionTicks} ticks after it gives way", new RouteStep[] {
            Hold(Left), Until(XAtMost(13.6f)), Release(), Until(Revealed("Lip_1")) }
            .Concat(reactionTicks > 0 ? new[] { For(reactionTicks) } : Array.Empty<RouteStep>()).Concat(new RouteStep[] {
            Hold(Right), Hold(Down), Until(On("V2", 14.6f)), Release(), Hold(Up), Until(YAtLeast(33.5f)), Hold(Down),
            Until(Fired("Spear_V2")), ReleaseClimb(), Until(Since("Spear_V2", 44)), Hold(Down), Until(Landed()), ReleaseClimb() })
            .Concat(ToAlcove()).Concat(Untimed(Trunk())).Concat(Untimed(Canopy(false))).ToArray());

        public static RoomRoutes Build()
        {
            var solution = new Route("L014 solution", Drop().Concat(ToAlcove()).Concat(Trunk()).Concat(Canopy()).ToArray());

            return new RoomRoutes(solution,
                new Betrayal("T1 [SS]: a cat that walks off the lip as it gives way, without holding on to V1, falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=13.2)", "walk off", Until(Dead())), revealedBy: "Lip_1", escape: EscapeLip),
                new Betrayal("T2 [J]: a cat that stays on V1 falls when it snaps, onto Spikes_V1", "Spikes_V1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(on V1)", "stay on V1", Release(), Until(Dead())), revealedBy: "V1"),
                new Betrayal("Dead end D2 [OL]: a cat that leaps from V2 onto the rest ledge meets Spikes_D2", "Spikes_D2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(on V2)", "the rest ledge", Release(), Until(Below(34.9f)), ReleaseClimb(), Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T2c [W]: a cat that climbs on down V2 when Spear_V2 tells is struck by it", "Spear_V2", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(on V2)", "climb on down", Release(), Until(Dead()))),
                Betrayal.Recovers("Dead end D1 [LW]: a cat that climbs V_Up to the lit alcove is dropped back to the Trunk when it snaps", "V_Up",
                    new Route("the alcove", Untimed(Drop()).Concat(ToAlcove()).Concat(new[] {
                        Hold(Up), Until(On("V_Up", 21f)), Release(), Until(Grounded()), ReleaseClimb(), Hold(Right) }).Concat(Untimed(Trunk())).Concat(Untimed(Canopy(false))).ToArray())),
                new Betrayal("T2b [BAIT]: a cat that walks on under the Beam is crushed by Block_T", "Block_T", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=22.6)", "walk on", Until(Dead()))),
                new Betrayal("T4b [W]: a cat that climbs straight on down V3 meets Arrow_V", "Arrow_V", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(on V3)", "climb straight on", Release(), Until(Dead()))),
                new Betrayal("T4 [B]: a cat that climbs on down V3 into the lane is struck by Spear_4", "Spear_4", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Stopped(Arrow_V))", "stay in the lane", Hold(Down), Until(Dead()))),
                new Betrayal("T4b [NJ]: a cat that jumps the Trough is struck by Spear_L", "Spear_L", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Spear_4_Shaft))", "jump the trough", Until(XAtMost(19.2f)), Jump(), Until(Grounded()), Release(), Until(Dead()))),
                new Betrayal("T5 [NJ]: a cat that leaps from Shrink to the far vine falls into Pit_K when V5 snaps", "Pit_K", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Still)", "the far vine", ReleaseClimb(), Hold(Left), Jump(), Until(XAtMost(5.5f)), Hold(Down), Until(Dead())), revealedBy: "V5"),
                new Betrayal("T5b [NW]: a cat that stands on Shrink falls onto Spikes_K as it narrows", "Spikes_K", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Still)", "wait on Shrink", Until(Dead())), revealedBy: "Shrink"),
                new Betrayal("T7 [NW]: a cat that waits on C2 falls onto the Perch when it snaps, and Block_7 lands on it", "Block_7", DeathCause.Hazard,
                    FromCanopy(solution, "wait on C2", Through(2).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C2"),
                new Betrayal("Canopy [NW]: a cat that hangs on C1 falls into the thorns when it snaps", "Thorns_C1", DeathCause.Hazard,
                    FromCanopy(solution, "hang on C1", Through(1).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C1"),
                new Betrayal("Canopy [NW]: a cat that hangs on C3 falls into the thorns when it snaps", "Thorns", DeathCause.Hazard,
                    FromCanopy(solution, "hang on C3", Through(3).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C3"),
                new Betrayal("Canopy [NW]: a cat that hangs on C4 falls into the thorns when it snaps", "Thorns", DeathCause.Hazard,
                    FromCanopy(solution, "hang on C4", Through(4).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C4"),
                new Betrayal("Canopy [NW]: a cat that hangs on C5 falls into the thorns when it snaps", "Thorns", DeathCause.Hazard,
                    FromCanopy(solution, "hang on C5", Through(5).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C5"),
                new Betrayal("Canopy [NW]: a cat that hangs on C6 falls into the thorns when it snaps", "Thorns", DeathCause.Hazard,
                    FromCanopy(solution, "hang on C6", Through(6).Concat(new[] { Release(), ReleaseClimb(), Until(Dead()) }).ToArray()), revealedBy: "C6"),
                new Betrayal("T8 [OL]: a cat that lands on the near half of the exit meets Spikes_8", "Spikes_8", DeathCause.Hazard,
                    FromCanopy(solution, "the near half", Through(6).Concat(new[] { For(10), Jump(), For(10), Release(), Until(Dead()) }).ToArray())));
        }
    }
}
