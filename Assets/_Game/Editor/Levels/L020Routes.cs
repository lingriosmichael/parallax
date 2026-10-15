using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-150): level 20's routes ("The Machine"). Route moves are stick input: while an inverter holds, Hold(Right)
    // walks the cat west (D-087 (7)).
    static class L020Routes
    {
        const float S3 = L020Layout.S3;

        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // A launcher's latest shot in flight toward -x (after its tell, `speed` u a tick from `mouth`), its tip within `d` of
        // the cat's east side.
        static RouteCondition ShotWithin(string arrow, float mouth, int tell, float speed, float d) => new($"{arrow} within {d}", v =>
        {
            int i = v.Result.Elements.IndexOf(arrow);
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - tell;
            float tip = mouth - speed * flying, front = v.Last.X + .5f;
            return flying >= 0 && tip > front && tip - front <= d;
        });
        // Arrow_Hi's latest shot (flying west from Hang_C's face after its tell) has gone past the cat, tail and all.
        static RouteCondition HiPassed() => new("Arrow_Hi passed", v =>
        {
            int i = v.Result.Elements.IndexOf("Arrow_Hi");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - 10;
            return flying >= 0 && 29.25f - .5f * flying + .8f < v.Last.X - .5f;
        });
        // Arrow_Hi's latest shot is in flight and its tip within `d` of the cat's east side.
        static RouteCondition HiNear(float d) => ShotWithin("Arrow_Hi", 29.25f, 10, .5f, d);
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        // The inversion cue is on (or off).
        static RouteCondition Inverted(bool on) => new(on ? "inverted" : "not inverted", v => v.Last.Signature[v.Result.Elements.IndexOf(R.CatInverted)] == (on ? 1 : 0));

        // Works (S1, then S2): jump Spikes_0 ahead of Hinge_0; stop short of Lift_1 and let it go up and down; hop Kerb_W onto
        // the bridge and run it, jumping Arrow_Lo's shot; hop Kerb_E and onto the Plinth; wait on the vent for G_1. The launch:
        // steer west, and when Inv_2 swaps the controls, hold right to keep going west onto S2.
        static RouteStep[] Works() => new[] {
            Hold(Right), Until(XAtLeast(4.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Release(), Until(Still()), Until(Moving("Lift_1")), Until(Home("Lift_1")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(12.2f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(ShotWithin("Arrow_Lo", 21.5f, 10, .3f, 2.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(20.6f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(25.6f)), Jump(), Until(XAtLeast(27.2f)), Release(), Until(GroundedOn("Plinth")), Until(Still()),
            Until(Airborne()), For(4), Hold(Left), Until(Fired("Inv_2")), Hold(Right), Until(Grounded()),
        };

        // Mirror (inverted: Hold(Right) walks west): stop as Slam_2 comes down, hop it; into Flip_U2; on the ceiling, jump CC_2;
        // into Flip_D2, which drops the cat at the turn (the inversion's end); west to V_3 and up; leap east onto S3.
        static RouteStep[] Mirror() => new[] {
            Release(), Until(Still()), Hold(Right), Until(Fired("Slam_2")), Release(), Until(Still()), Until(Stopped("Slam_2")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtMost(20.9f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(GravityUp()), Release(), Until(Grounded()), Hold(Right), Until(XAtMost(14.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityDown()), Until(Inverted(false)), Hold(Left), Until(Grounded()), Until(XAtMost(4.2f)), Jump(), Until(Falling()), Hold(R.Down), Until(On("V_3", 1.6f)),
            Release(), Hold(R.Up), Until(YAtLeast(S3 + 1.1f)),
            ReleaseClimb(), Hold(Right), Jump(), Until(Landed()),
        };

        // Storm and the finale: the snap swaps the controls (hold left to go east) and wakes the Cloud and Arrow_Hi; let
        // Arrow_Hi's shot pass over, then jump Spikes_3; the controls come back; under Hang_C into the chimney and up it; west
        // along S4: stop as Slam_Fa comes down, hop onto it before Hinge_F arrives, wait for Slam_Fb, onto it, off it west onto
        // Door_Step as the door retreats; jump from the step onto Door_Ledge, short of Spikes_D.
        static RouteStep[] Storm() => new[] {
            Until(Fired("Inv_3")), Hold(Left), Until(XAtLeast(13.6f)), Release(), Until(Still()), Until(HiPassed()),
            Hold(Left).Timed(TimedMode.Shift), Until(XAtLeast(14.3f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(Since("Inv_3", 151)), Hold(Right), Until(XAtLeast(30.6f)), Release(), Until(Still()),
            Jump(), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(6), Hold(Left), Until(GroundedOn("S4_E")),
            Until(XAtMost(20.9f)), Release(), Until(Still()), Until(Stopped("Slam_Fa")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(GroundedOn("Slam_Fa")), Until(XAtMost(18.6f)), Release(), Until(Stopped("Slam_Fb")),
            Hold(Left).Timed(TimedMode.Shift), Jump(), Until(GroundedOn("Slam_Fb")), Until(XAtMost(14.1f)), Jump(), Until(Grounded()),
            Until(XAtMost(7.6f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Works(), Mirror(), Storm() };
            const int WorksPart = 0, MirrorPart = 1, StormPart = 2;
            var solution = new Route("L020 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Works.
                new Betrayal("T1 [J]: a cat that waits at the start is caught by Hinge_0's spikes", "Hinge_0", DeathCause.Hazard,
                    new Route("wait at the start", Until(Dead()))),
                new Betrayal("T2 [W]: a cat that runs on after the jump is on Lift_1 as it shoots up, and is crushed", "Lift_1", DeathCause.Hazard,
                    B("run on", WorksPart, 5, Until(Dead()))),
                new Betrayal("T3 [NW]: a cat that hops onto the bridge and stops falls with it onto Pit_B", "Pit_B", DeathCause.Hazard,
                    B("stop on the bridge", WorksPart, 12, Until(Airborne()), For(14), Release(), Until(Grounded()), Until(Dead())), revealedBy: "Bridge_1"),
                new Betrayal("T4 [J]: a cat that runs the bridge without jumping is struck by Arrow_Lo", "Arrow_Lo", DeathCause.Hazard,
                    B("don't jump", WorksPart, 14, Until(Dead()))),
                new Betrayal("Dead end D1 [OL]: a cat that walks past the vent into the Alcove meets Spikes_A", "Spikes_A", DeathCause.Hazard,
                    B("the alcove", WorksPart, 27, Hold(Right), Until(Dead()))),
                new Betrayal("T5 [B]: a cat that keeps steering left when Inv_2 swaps the controls drifts east into the nook onto Spikes_N", "Spikes_N", DeathCause.Hazard,
                    B("keep left", WorksPart, 31, Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("Dead end D2 [LW]: a cat that steers east off the launch lands in the nook on Spikes_N", "Spikes_N", DeathCause.Hazard,
                    B("steer east", WorksPart, 29, For(12), Hold(Right), Until(Fired("Inv_2")), Hold(Left), Until(Dead()))),
                // Mirror.
                new Betrayal("T6 [SS]: a cat that walks on as Slam_2 comes down is crushed", "Slam_2", DeathCause.Hazard,
                    B("walk on", MirrorPart, 4, Until(Dead()))),
                new Betrayal("T7 [J]: a cat that walks on along the ceiling falls up through CC_2 onto Spikes_CC2", "Spikes_CC2", DeathCause.Hazard,
                    B("walk on", MirrorPart, 16, Until(Dead())), revealedBy: "CC_2"),
                new Betrayal("T8 [B]: a cat that keeps holding right when the controls come back at the turn walks east onto Spikes_F2", "Spikes_F2", DeathCause.Hazard,
                    B("keep right", MirrorPart, 22, Until(Dead())), revealedBy: R.CatInverted),
                // Storm.
                new Betrayal("T9 [NW]: a cat that hangs on V_3 falls onto Thorns_V when it snaps", "Thorns_V", DeathCause.Hazard,
                    B("hang on", MirrorPart, 32, Until(Dead())), revealedBy: "V_3"),
                new Betrayal("T10 [B]: a cat that keeps holding right when the snap swaps the controls runs back west into V_3's gap onto Thorns_V", "Thorns_V", DeathCause.Hazard,
                    B("keep right", StormPart, 1, Until(Dead())), revealedBy: R.CatInverted),
                new Betrayal("T11 [NW]: a cat that stops on S3 is struck by the Cloud", "Cloud", DeathCause.Hazard,
                    B("stop", StormPart, 2, Release(), Until(Dead())), revealedBy: "Cloud"),
                new Betrayal("T12 [NJ]: a cat that jumps as Arrow_Hi comes over it is struck", "Arrow_Hi", DeathCause.Hazard,
                    B("jump into it", StormPart, 2, Until(HiNear(4.5f)), Jump(), Until(Dead()))),
                // Finale.
                new Betrayal("T13 [OL]: a cat that stays on the floor is caught by Hinge_F's spikes", "Hinge_F", DeathCause.Hazard,
                    B("stay", StormPart, 33, Until(Dead()))),
                new Betrayal("T14 [SS]: a cat that walks on as Slam_Fa comes down is crushed", "Slam_Fa", DeathCause.Hazard,
                    B("walk on", StormPart, 30, Until(Dead()))),
                new Betrayal("T15 [W]: a cat that steps off Slam_Fa before Slam_Fb is down drops onto Spikes_F", "Spikes_F", DeathCause.Hazard,
                    B("step off", StormPart, 37, Hold(Left), Until(Dead()))),
                new Betrayal("T16 [BAIT]: a cat that chases the door off Door_Step meets Spikes_D", "Spikes_D", DeathCause.Hazard,
                    B("chase it", StormPart, 45, Until(Dead()))));
        }
    }
}
