using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-168: level 24's routes. Route moves are stick input: while Inv_0 holds (corridor 1), Hold(Left) walks the cat east
    // (D-087 (7)); it ends as the cat rises through the shaft, so the same Hold(Left) then walks corridor 2 west.
    static class L024Routes
    {
        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // Arrow_1's latest shot in flight toward -x (after its tell, from its mouth), its tip within `d` of the cat's east side.
        static RouteCondition ShotWithin(float d) => new($"Arrow_1 within {d}", v =>
        {
            int i = v.Result.Elements.IndexOf("Arrow_1");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - L024Layout.ArrowTell;
            float tip = L024Layout.ArrowMouth - L024Layout.ArrowSpeed * flying, front = v.Last.X + .5f;
            return flying >= 0 && tip > front && tip - front <= d;
        });

        // ---- Town, corridor 1's ceiling, walked east (inverted) ----
        // Up onto the ceiling; stop short of Spikes_R1 and walk through while they're gone; jump CC_1; stop short of Lift_1, let
        // it go up and down; over Kerb_C1; jump Arrow_1's shots; into the shaft and up it onto corridor 2's ceiling.
        static RouteStep[] Town() => new[] {
            Until(GravityUp()), Until(Grounded()),
            Hold(Left), Until(XAtLeast(3.9f)), Release(), Until(Still()),
            Until(Beat(L024Layout.RhythmPeriod, L024Layout.RhythmFrom + 2)), Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(11.2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(15.6f)), Release(), Until(Still()), Until(Moving("Lift_1")), Until(Home("Lift_1")),
            Hold(Left), Until(XAtLeast(18.9f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(ShotWithin(2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(ShotWithin(2f)), Jump(), Until(Airborne()),
            Until(YAtLeast(L024Layout.F2 + 1f)), Until(Grounded()),
        };

        // ---- Gallery, corridor 2's ceiling, walked west ----
        // Wait out Piston_2 and cross under it; jump Pop_C2; over CC_2 without stopping; into the shaft and up onto the roof.
        static RouteStep[] Gallery() => new[] {
            Hold(Left), Until(XAtMost(25.5f)), Release(), Until(Still()),
            Until(Beat(L024Layout.PistonPeriod, L024Layout.PistonPhase + 42)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(19.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(3f)), Release(), Until(YAtLeast(16f)), Until(Grounded()),
        };

        // ---- Crown, the roof's underside and corridor 3's floor, walked east ----
        // Ahead of Push_3, over Spikes_C3, into Flip_D3; on the floor jump Spikes_5 ahead of Hinge_F; stop short of Slam_F and hop
        // onto it; the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(XAtLeast(7.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityDown()), Until(Grounded()),
            Until(XAtLeast(16.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(22f)), Release(), Until(Still()),
            Until(Fired("Slam_F")), Until(Stopped("Slam_F")), Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Town(), Gallery(), Crown() };
            const int T = 0, G = 1, C = 2;
            var solution = new Route("L024 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Town.
                new Betrayal("T1 [NJ]: a cat that jumps Spikes_R1 meets the hidden Flip_H1 and drops onto Street_1a", "Street_1a", DeathCause.Hazard,
                    B("jump them", T, 6, Hold(Left), Until(XAtLeast(4.2f)), Jump(), Until(Dead())), revealedBy: "Cat.Gravity"),
                new Betrayal("T2 [W]: a cat that walks on while Spikes_R1 are out meets them", "Spikes_R1", DeathCause.Hazard,
                    B("walk on", T, 6, Until(Beat(L024Layout.RhythmPeriod, L024Layout.RhythmFrom + L024Layout.RhythmDown + 2)), Hold(Left), Until(Dead())), revealedBy: "Spikes_R1"),
                new Betrayal("T3 [J]: a cat that walks onto CC_1 falls up onto Spikes_CC1", "Spikes_CC1", DeathCause.Hazard,
                    B("walk on", T, 9, Until(Dead())), revealedBy: "CC_1"),
                new Betrayal("T4 [SS]: a cat that walks on over Lift_1 is crushed against SA_L", "Lift_1", DeathCause.Hazard,
                    B("walk on", T, 13, Until(Dead()))),
                new Betrayal("T5 [J]: a cat that doesn't jump Arrow_1's shot is hit", "Arrow_1", DeathCause.Hazard,
                    B("walk on", T, 23, Until(Dead()))),
                // Gallery.
                new Betrayal("T6 [W]: a cat that crosses as Piston_2 rises is crushed against SB_P", "Piston_2", DeathCause.Hazard,
                    B("cross now", G, 4, Until(Beat(L024Layout.PistonPeriod, L024Layout.PistonPhase - 12)), Hold(Left), Until(Dead()))),
                new Betrayal("T7 [J]: a cat that walks on as Pop_C2 springs out of the ceiling meets it", "Pop_C2", DeathCause.Hazard,
                    B("walk on", G, 7, Until(Dead()))),
                new Betrayal("T8 [NW]: a cat that stops on CC_2 falls up onto Spikes_CC2", "Spikes_CC2", DeathCause.Hazard,
                    B("stop", G, 10, Until(XAtMost(10f)), Release(), Until(Dead())), revealedBy: "CC_2"),
                // Crown.
                new Betrayal("T9 [NW]: a cat that stops short of Spikes_C3 is shoved onto them by Push_3", "Spikes_C3", DeathCause.Hazard,
                    B("stop", C, 1, Until(XAtLeast(6.4f)), Release(), Until(Dead())), revealedBy: "Push_3"),
                new Betrayal("T10 [NW]: a cat that stops short of Spikes_5 is caught by Hinge_F", "Hinge_F", DeathCause.Hazard,
                    B("stop", C, 7, Until(XAtLeast(17f)), Release(), Until(Dead())), revealedBy: "Hinge_F"),
                new Betrayal("T11 [SS]: a cat that runs on under Slam_F is crushed", "Slam_F", DeathCause.Hazard,
                    B("run on", C, 12, Until(Dead()))));
        }
    }
}
