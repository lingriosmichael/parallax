using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 20's routes. Every wait is on the machine's chain (an element's fire tick) except the last,
    // G_9's eruption, which runs on the room's clock (a geyser can't be chained).
    static class L020Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);

        // Out, the mid storey: hop Post_P west to the lever; back east, jumping Arrow_1 as it catches up; hop Post_P again
        // (short, so as not to land past the gate's cut); stop short of Collapse_2 as it goes; jump the gap.
        static RouteStep[] Mid() => new[] {
            Hold(Left), Until(XAtMost(15.9f)), Jump(), Until(Grounded()), Until(XAtMost(2.2f)), Release(), Until(Still()),
            Hold(Right), Until(Since("Arrow_1", 17)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(13.6f)), Jump().Timed(TimedMode.Shift), Until(XAtLeast(15.4f)), Release(), Until(Grounded()),
            Hold(Right), Until(XAtLeast(18.1f)), Release(), Until(Still()), Until(Fired("Collapse_2")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(18.4f)), Jump(), Until(GroundedOn("Mid_E")),
        };

        // Out, the chimney: into its mouth and stop; when Spear_4a has stuck, hop onto it; up the stair, each step once its
        // spear has stuck, to the Perch.
        static RouteStep[] Chimney() => new[] {
            Until(XAtLeast(27.3f)), Release(), Until(Still()), Until(Since("Spear_4a", 12)),
            Hold(Right).Timed(TimedMode.Shift), Jump(), Until(XAtLeast(29.8f)), Release(), Until(GroundedOn("Spear_4a_Shaft")), Until(Still()),
            Until(Since("Spear_4b", 12)), Jump().Timed(TimedMode.Shift), Until(YAtLeast(StepY(1) + .6f)), Hold(Left), Until(XAtMost(28f)), Release(), Until(GroundedOn("Spear_4b_Shaft")), Until(Still()),
            Until(Since("Spear_4c", 12)), Jump().Timed(TimedMode.Shift), Until(YAtLeast(StepY(2) + .6f)), Hold(Right), Until(XAtLeast(29.8f)), Release(), Until(GroundedOn("Spear_4c_Shaft")), Until(Still()),
            Jump().Timed(TimedMode.Shift), Until(YAtLeast(StepY(3) + .6f)), Hold(Left), Until(XAtMost(28f)), Release(), Until(GroundedOn("Perch")), Until(Still()),
        };
        // A cat standing on step k: its centre's height.
        static float StepY(int k) => L020Layout.StepTop(k) + .285f;

        // Back: onto V_5 and up; leap west onto the Top before it snaps (the snap flips the controls: hold right to keep
        // going west); mirrored, west out of the cloud's reach; wait out the flip; on west over Shrink_8 and the vent to V_L;
        // climb; leap onto the Loft.
        static RouteStep[] Back() => new[] {
            Hold(Right), Until(XAtLeast(28.2f)), Hold(Up), Until(On("V_5", 28.8f)), Until(YAtLeast(16.4f)),
            Hold(Left), Jump(), Until(Landed()), ReleaseClimb(), Until(Fired("Inv_6")), Hold(Right).Timed(TimedMode.Shift),
            Until(XAtMost(18.3f)), Release(), Until(Still()), Until(Since("Inv_6", 151)),
            Hold(Left), Until(XAtMost(2.2f)), Hold(Up), Until(On("V_L", 1.2f)), Until(YAtLeast(23.1f)),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(Landed()), ReleaseClimb(),
        };

        // Last: east along the Loft; the door drops through it onto Door_Ledge; stop; walk off the Loft's end, onto the vent;
        // ride the next eruption and steer west onto the ledge, into the door (PAX-099, D-106).
        static RouteStep[] Last() => new[] {
            Until(XAtLeast(5f)), Release(), Until(Still()),
            Hold(Right), Until(XAtLeast(10.1f)), Release(), Until(GroundedOn("Top_W")), Hold(Right), Until(XAtLeast(11.4f)), Release(),
            Until(Still()), Until(Airborne()), Hold(Left).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Mid(), Chimney(), Back(), Last() };
            const int MidPart = 0, ChimneyPart = 1, BackPart = 2, LastPart = 3;
            var solution = new Route("L020 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            // A detour off part `part` at step `index`, rejoining the solution at the same part's step `rejoin`.
            Route Detour(string name, int part, int index, int rejoin, params RouteStep[] then) =>
                new(name, Branch(parts, part, index, then).Concat(parts[part].Skip(rejoin)).Concat(parts.Skip(part + 1).SelectMany(p => p)).ToArray());

            return new RoomRoutes(solution,
                // Out.
                new Betrayal("T1 [J]: a cat that walks on when Arrow_1 catches it up is struck", "Arrow_1", DeathCause.Hazard,
                    B("walk on", MidPart, 9, Until(Dead()))),
                new Betrayal("T2 [NW]: a cat that stops in front of Post_P is crushed into it by Push_3", "Push_3", DeathCause.Hazard,
                    B("stop at the post", MidPart, 12, Until(XAtLeast(13.3f)), Release(), Until(Dead()))),
                new Betrayal("T3 [W]: a cat that runs on past the gate falls where Collapse_2 was", "Spikes_2", DeathCause.Hazard,
                    B("run on", MidPart, 19, Until(Dead())), revealedBy: "Collapse_2"),
                new Betrayal("T4 [W]: a cat that hops at the stair before Spear_4a has stuck is run through", "Spear_4a", DeathCause.Hazard,
                    B("hop early", ChimneyPart, 1, Jump(), Until(Dead()))),
                new Betrayal("T4b [W]: a cat that hops on for step 2 as it lands on step 1, before Spear_4b has stuck, is run through", "Spear_4b", DeathCause.Hazard,
                    B("hop on", ChimneyPart, 9, Jump(), Until(Dead()))),
                // Back.
                new Betrayal("T5 [J]: a cat that climbs V_5 to the top is on it when it snaps and falls onto Thorns_5", "Thorns_5", DeathCause.Hazard,
                    B("climb on", BackPart, 4, Release(), Until(YAtLeast(17.6f)), Until(Dead())), revealedBy: "V_5"),
                new Betrayal("T6 [B]: a cat that keeps holding left when the controls flip runs back east into Block_7a", "Block_7a", DeathCause.Hazard,
                    B("hold left", BackPart, 10, Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T7 [NW]: a cat that stops under the cloud to wait out the flip is struck", "Cloud", DeathCause.Hazard,
                    B("stop under it", BackPart, 11, Until(XAtMost(24f)), Release(), Until(Dead())), revealedBy: "Cloud"),
                Betrayal.Recovers("Dead end D2 [LW]: a cat that shelters under Roof_N waits out a strike there, then goes on", "Cloud",
                    Detour("the shelter", BackPart, 11, 10, Until(XAtMost(21.6f)), Release(), Until(Still()), Until(Since("Cloud", 82)))),
                new Betrayal("T8 [NW]: a cat that stops on Shrink_8 falls into its well onto Spikes_8", "Spikes_8", DeathCause.Hazard,
                    B("stop on it", BackPart, 16, Until(XAtMost(14.5f)), Release(), Until(Dead())), revealedBy: "Shrink_8"),
                Betrayal.Recovers("Dead end D1 [LW]: a cat that tries G_9 on the way west is thrown up short of the Loft, comes down and goes on", "G_9",
                    Detour("the geyser", BackPart, 16, 15, Until(XAtMost(11.7f)), Release(), Until(Still()), Until(Airborne()), Until(Grounded()))),
                // Last.
                new Betrayal("T9 [BAIT]: a cat that chases the door runs off the Loft into Shrink_8's well", "Spikes_8", DeathCause.Hazard,
                    B("chase it", LastPart, 1, Until(Dead())), revealedBy: "Door"),
                // PAX-099 (D-106): a cat that walks off the Loft's end and steers back west at once passes east of Door_Ledge and the
                // door, lands on Top_W, and goes on (the room auditor's open margin, about 0.5 u).
                Betrayal.Recovers("Dead end D3 [LW]: a cat that drops off the Loft's end and steers back west misses Door_Ledge, lands on Top_W and goes on", "Door",
                    Detour("steer back", LastPart, 5, 6, Hold(Left))));
        }
    }
}
