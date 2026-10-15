using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-142): level 16's routes ("Flipside").
    static class L016Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // Arrow_M's shot in flight (from its last fire: the 10-tick tell, then 0.3 u a tick from the jamb's face), its tip
        // within `d` of the cat's west side.
        static RouteCondition ArrowWithin(float d) => new($"Arrow_M within {d}", v =>
        {
            int i = v.Result.Elements.IndexOf("Arrow_M");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - 10;
            float tip = 1.5f + .8f + .3f * flying, back = v.Last.X - .5f;
            return flying >= 0 && tip < back && back - tip <= d;
        });

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // Floor: set Slam_1 off, back off, hop it; into Flip_U1 at a run; on the ceiling, jump CC_1 and stop where it lands
        // while Lift_1 goes up and down; on through Flip_D1; jump Spikes_F2 ahead of Hinge_1; climb the chimney.
        static RouteStep[] Floor() => new[] {
            Hold(Right), Until(Fired("Slam_1")), Release(), Until(Still()), Until(Since("Slam_1", 16)),
            Hold(Right), Until(XAtLeast(2.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityUp()), Until(Grounded()), Until(XAtLeast(12.3f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Release(), Until(Still()), Until(Fired("Lift_1")), Until(Since("Lift_1", 52)),
            Hold(Right), Until(XAtLeast(19.2f)), Release(), Until(Still()), Until(Since("Lift_1b", 46)),
            Hold(Right), Until(GravityDown()), Until(Grounded()), Until(XAtLeast(26.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Jump(), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(GroundedOn("SA_3")),
        };

        // Arrows: west along the floor; stop as Slam_2 comes down and stand still until it's back up; into Flip_U2; west along
        // the ceiling, jumping each shot as it comes, over CC_2 without stopping, and up the shaft.
        static RouteStep[] Arrows() => new[] {
            Until(XAtMost(28.4f)), Release(), Until(Still()), Until(Beat(L016Layout.PistonPeriod, L016Layout.PistonPhase + 42)),
            Hold(Left), Until(Fired("Slam_2")), Release(), Until(Still()), Until(Since("Slam_2", 50)),
            Hold(Left), Until(GravityUp()), Until(Grounded()),
            Until(ArrowWithin(2.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(ArrowWithin(2.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(3f)), Release(), Until(YAtLeast(16f)), Until(Grounded()),
        };

        // Top: east along the ceiling ahead of Push_3, over Spikes_C3, into Flip_D3; jump Flip_H as it lands; on to the cut,
        // stop as Slam_3a comes down, hop onto it, wait for Slam_3b, onto it, over Spikes_3b to the door.
        static RouteStep[] Top() => new[] {
            Hold(Right), Until(XAtLeast(7.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityDown()), Until(Grounded()), Until(XAtLeast(15.2f)), Release(), Until(Still()), Until(Beat(L016Layout.RhythmPeriod, 2)),
            Hold(Right).Timed(TimedMode.Shift), Until(Fired("Hinge_3")), Release(), Until(Still()), Until(Since("Hinge_3", 16)),
            Hold(Right), Jump(), Until(GroundedOn("Slam_3a")), Release(), Until(Since("Hinge_3", 92)),
            Hold(Right), Until(GroundedOn("Slam_3b")), Until(XAtLeast(28.6f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Floor(), Arrows(), Top() };
            const int FloorPart = 0, ArrowsPart = 1, TopPart = 2;
            var solution = new Route("L016 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Floor.
                new Betrayal("T1 [BAIT]: a cat that runs on as it sets off is crushed by Slam_1", "Slam_1", DeathCause.Hazard,
                    B("run on", FloorPart, 1, Until(Dead()))),
                new Betrayal("Dead end D1 [LW]: a cat that jumps into Flip_U1 and turns back west on the ceiling meets Spikes_CW", "Spikes_CW", DeathCause.Hazard,
                    B("back west", FloorPart, 10, Until(XAtLeast(7.2f)), Jump(), Until(GravityUp()), Until(Grounded()), Hold(Left), Until(Dead()))),
                new Betrayal("T2 [J]: a cat that walks on along the ceiling falls up through CC_1 onto Spikes_CC1", "Spikes_CC1", DeathCause.Hazard,
                    B("walk on", FloorPart, 10, Until(GravityUp()), Until(Grounded()), Until(Dead())), revealedBy: "CC_1"),
                new Betrayal("T3 [W]: a cat that walks on where the jump over CC_1 lands is crushed by Lift_1", "Lift_1", DeathCause.Hazard,
                    B("walk on", FloorPart, 16, Until(Dead()))),
                new Betrayal("T4 [SS]: a cat that walks on once Lift_1 is down is crushed by Lift_1b", "Lift_1b", DeathCause.Hazard,
                    B("walk on", FloorPart, 21, Until(Dead()))),
                new Betrayal("T5 [NW]: a cat that stops where Flip_D1 drops it is pushed onto Spikes_F2 by Hinge_1", "Spikes_F2", DeathCause.Hazard,
                    B("stop", FloorPart, 28, Release(), Until(Dead())), revealedBy: "Hinge_1"),
                // Arrows.
                new Betrayal("T6 [W]: a cat that walks straight on over Piston_2 is crushed by it", "Piston_2", DeathCause.Hazard,
                    B("walk on", ArrowsPart, 0, Until(Dead()))),
                new Betrayal("T7 [SS]: a cat that walks on as Slam_2 comes down is crushed", "Slam_2", DeathCause.Hazard,
                    B("walk on", ArrowsPart, 6, Until(Dead()))),
                new Betrayal("T8 [J]: a cat that jumps into Flip_U2, out of the arrows, meets the next shot on the ceiling", "Arrow_M", DeathCause.Hazard,
                    B("jump in", ArrowsPart, 10, Until(XAtMost(18.4f)), Jump(), Until(GravityUp()), Until(Grounded()), Until(Dead()))),
                new Betrayal("T9 [NW]: a cat that stops where Flip_U2 lands it falls up through CC_2 onto Spikes_CC2", "Spikes_CC2", DeathCause.Hazard,
                    B("stop", ArrowsPart, 12, Release(), Until(Dead())), revealedBy: "CC_2"),
                // Top.
                new Betrayal("T10 [NW]: a cat that stops short of Spikes_C3 is shoved onto them by Push_3", "Spikes_C3", DeathCause.Hazard,
                    B("stop", TopPart, 1, Until(XAtLeast(6.4f)), Release(), Until(Dead())), revealedBy: "Push_3"),
                new Betrayal("Dead end D2 [B]: a cat that goes back west along corridor 3's floor meets Spikes_D2", "Spikes_D2", DeathCause.Hazard,
                    B("back west", TopPart, 7, Hold(Left), Until(Dead()))),
                new Betrayal("T11 [NJ]: a cat that jumps Spikes_R meets the hidden Flip_H and falls up onto Spikes_CT", "Spikes_CT", DeathCause.Hazard,
                    B("jump them", TopPart, 7, Until(XAtLeast(15.6f)), Jump(), Until(Dead())), revealedBy: "Cat.Gravity"),
                new Betrayal("T12 [OL]: a cat that stays on the floor is caught by Hinge_3's spikes", "Hinge_3", DeathCause.Hazard,
                    B("stay", TopPart, 16, Until(Dead()))),
                new Betrayal("T13 [SS]: a cat that walks on as Slam_3a comes down is crushed", "Slam_3a", DeathCause.Hazard,
                    B("walk on", TopPart, 12, Until(Dead()))),
                new Betrayal("T14 [W]: a cat that steps on before Slam_3b is down drops onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("step on", TopPart, 19, Hold(Right), Until(Dead()))),
                new Betrayal("T15 [LW]: a cat that jumps from Slam_3a straight for the door, over Slam_3b, lands on Spikes_3b", "Spikes_3b", DeathCause.Hazard,
                    B("jump for the door", TopPart, 21, Hold(Right), Jump(), Until(Dead()))));
        }
    }
}
