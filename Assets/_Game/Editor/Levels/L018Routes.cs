using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 18's routes. Waits are on the layout's own clocks: the cloud's strikes (its wake + 75 + 100 k),
    // the geysers' eruptions (room ticks, L018Layout), an element's fire tick or pose. Betrayals branch off the solution's
    // own steps, so each one's place in the sequence is where it leaves the solution.
    static class L018Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // A room tick at or after `tick`.
        static RouteCondition At(int tick) => new($"t>={tick}", v => v.Last.RoomLifeTick >= tick);
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);

        // Storm, to the ground under Roof_2: hop Curb_1, stop, jump Arrow_2, on under the roof.
        static RouteStep[] ToRoof() => new[] {
            Hold(Left), Until(XAtMost(26.9f)), Jump(), Until(XAtMost(25f)), Release(), Until(Grounded()), Until(Still()),
            Until(Since("Arrow_2", 30)), Jump().Timed(TimedMode.Shift), Until(Grounded()),
            Hold(Left), Until(XAtMost(22.8f)), Release(), Until(Still()),
        };

        // Storm, the launch: after the strike on Roof_2, onto the Plinth in G_1's tell; ride it, steering hard left onto SL_1.
        static RouteStep[] Launch() => new[] {
            Until(At(205)), Hold(Left).Timed(TimedMode.Shift), Jump(), Until(XAtMost(21.2f)), Release(),
            Until(YAtLeast(3f)), Hold(Left), Until(GroundedOn("SL_1")),
        };

        // Sky, to Drop_3: west over the gate to the cut; stop; jump Spear_S; on over SL_2 and SL_3 onto Drop_3 and stop.
        static RouteStep[] ToDrop() => new[] {
            Until(XAtMost(15.8f)), Release(), Until(Still()), Until(Since("Spear_S", 22)), Jump().Timed(TimedMode.Shift), Until(Grounded()),
            Hold(Left), Until(XAtMost(15.7f)), Jump(), Until(GroundedOn("SL_2")), Until(XAtMost(9.3f)), Release(), Until(GroundedOn("Drop_3")),
        };

        // Sky, down: ride Drop_3 down; step off under Roof_5 (Collapse_V gives way as it lands); when Collapse_V is back, after
        // the strike on the roof, across it to V_6 and up through the cloud's band; leap onto SB_W.
        static RouteStep[] Down() => new[] {
            Until(Below(1f)), Until(Grounded()), Hold(Left), Until(XAtMost(7.3f)), Release(), Until(Still()),
            Until(Fired("Collapse_V")), Until(Home("Collapse_V")), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(2f)), Hold(Up), Until(On("V_6", 1.6f)), Release(),
            Until(YAtLeast(7.7f)), Hold(Right), Jump(), Until(Landed()), ReleaseClimb(),
        };

        // Top, Sky B: wait short of the gap's edge (clear of Block_T's cut) until G_D's eruption is over; jump the gap; stand
        // still on SB_E while Arrow_B passes overhead; hop Step_B onto G_B's vent and ride it, steering right onto the Top.
        static RouteStep[] SkyB() => new[] {
            Until(XAtLeast(7.6f)), Release(), Until(Still()),
            Until(Since("G_D", 66)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("SB_E")), Release(), Until(Still()),
            Until(Since("Arrow_B", 24)), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(XAtLeast(15.6f)), Release(), Until(GroundedOn("SB_E")),
            Until(YAtLeast(9f)), Hold(Right), Until(GroundedOn("TL_2")),
        };

        // Top, the chaos and the Crown: run over the crumbles, jump Spikes_7 onto Plinth_C; ride G_C, steering right onto the
        // Crown; climb V_9 to the Summit; west along it, over the hole Block_7 left, into the door.
        static RouteStep[] Crown() => new[] {
            Until(XAtLeast(24.2f)), Jump(), Until(XAtLeast(27.8f)), Release(), Until(GroundedOn("Plinth_C")),
            Until(YAtLeast(12.5f)), Hold(Right), Until(GroundedOn("Crown")), Until(XAtLeast(31f)), Release(), Hold(Up), Until(On("V_9", 31.4f)),
            Until(YAtLeast(20.3f)), Hold(Left), Jump(), Until(Landed()), ReleaseClimb(), Until(XAtMost(25f)), Jump(), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { ToRoof(), Launch(), ToDrop(), Down(), SkyB(), Crown() };
            const int Roof = 0, Launching = 1, Sky = 2, Drop = 3, SkyBPart = 4, CrownPart = 5;
            var solution = new Route("L018 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Storm.
                new Betrayal("T1 [NW]: a cat that shelters under Fake_1 is struck through it", "Cloud", DeathCause.Hazard,
                    B("the fake cover", Roof, 1, Until(XAtMost(28.1f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T2 [J]: a cat that walks on from the curb meets Arrow_2", "Arrow_2", DeathCause.Hazard,
                    B("walk on", Roof, 6, Hold(Left), Until(Dead()))),
                new Betrayal("T3 [W]: a cat that waits on G_1's vent instead of under Roof_2 is struck", "Cloud", DeathCause.Hazard,
                    B("wait on the vent", Roof, 10, Hold(Left), Until(XAtMost(21.9f)), Jump(), Until(XAtMost(21.1f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T4 [OL]: a cat that steers the launch short lands on Fake_A and falls back into the storm", "Cloud", DeathCause.Hazard,
                    B("steer short", Launching, 7, Until(XAtMost(19.2f)), Release(), Until(Dead())), revealedBy: "Fake_A"),
                // Sky.
                new Betrayal("T5 [J]: a cat that stands on SL_1 when Spear_S comes is struck", "Spear_S", DeathCause.Hazard,
                    B("stand", Sky, 3, Until(Dead()))),
                new Betrayal("T6 [NW]: a cat that stops on SL_2 falls onto Spikes_4", "Spikes_4", DeathCause.Hazard,
                    B("stop on SL_2", Sky, 8, Jump(), Until(XAtMost(13.6f)), Release(), Until(GroundedOn("SL_2")), Until(Dead())), revealedBy: "SL_2"),
                new Betrayal("T6 [NW]: a cat that stops on SL_3 falls onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("stop on SL_3", Sky, 10, Until(XAtMost(10.5f)), Release(), Until(Dead())), revealedBy: "SL_3"),
                new Betrayal("T7 [B]: a cat that stays on the lowered Drop_3 is struck", "Cloud", DeathCause.Hazard,
                    B("stay on the drop", Drop, 1, Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("Dead end D1 [LW]: a cat that steps east off the lowered drop walks onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("east off the drop", Drop, 2, Hold(Right), Until(Dead()))),
                new Betrayal("T8 [W]: a cat that waits at the pit's edge, out from under Roof_5, is struck", "Cloud", DeathCause.Hazard,
                    B("wait at the edge", Drop, 3, Until(XAtMost(6.5f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T8 [W]: a cat that walks on falls where the floor went", "Spikes_V", DeathCause.Hazard,
                    B("walk on", Drop, 3, Until(Dead())), revealedBy: "Collapse_V"),
                Betrayal.Recovers("Dead end D2 [B]: a cat that turns back under Start_Roof waits out a strike there, then goes on", "Cloud",
                    new Route("the start's roof", new RouteStep[] { Hold(Left), Until(XAtMost(29.2f)), Hold(Right), Until(XAtLeast(31.1f)), Release(), Until(Still()),
                        Until(Since("Cloud", 82)) }.Concat(Cat(parts)).ToArray())),
                // Top.
                new Betrayal("T10 [B]: a cat that waits at the gap's edge is crushed by Block_T", "Block_T", DeathCause.Hazard,
                    B("wait at the edge", SkyBPart, 0, Until(XAtLeast(9.3f)), Release(), Until(Dead()))),
                new Betrayal("T9 [W]: a cat that jumps the gap while G_D erupts is blown down onto Spikes_4", "Spikes_4", DeathCause.Hazard,
                    B("jump in the downdraft", SkyBPart, 3, Until(At(750)), Until(Since("G_D", 18)), Hold(Right), Until(XAtLeast(9.4f)), Jump(), Until(Dead())), revealedBy: "G_D"),
                new Betrayal("T11 [SS]: a cat that hops Step_B as it lands meets Arrow_B", "Arrow_B", DeathCause.Hazard,
                    B("hop at once", SkyBPart, 10, Hold(Right), Jump(), Until(Dead()))),
                new Betrayal("T12 [NW]: a cat that stops on the crumbles falls into the Briar", "Briar", DeathCause.Hazard,
                    B("stop on TL_3", CrownPart, 0, Until(XAtLeast(21.3f)), Release(), Until(Dead())), revealedBy: "TL_3"),
                new Betrayal("T12 [NW]: a cat that stops past the crumbles is crushed by Block_7", "Block_7", DeathCause.Hazard,
                    B("stop past them", CrownPart, 0, Until(XAtLeast(23.6f)), Release(), Until(Dead()))),
                new Betrayal("T13 [J]: a cat that runs on meets Spikes_7", "Spikes_7", DeathCause.Hazard,
                    B("run on", CrownPart, 1, Until(Dead()))),
                new Betrayal("T14 [OL]: a cat that steers G_C's launch short misses the Crown and comes down on Thorns_C", "Thorns_C", DeathCause.Hazard,
                    B("steer short", CrownPart, 7, Until(XAtLeast(28.9f)), Release(), Until(Dead())), revealedBy: "G_C"));
        }
    }
}
