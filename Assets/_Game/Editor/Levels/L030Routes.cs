using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-183 + D-189: level 30's routes ("The Elevator"). Waits on a ride are on its departure (each chained trap fires a fixed number
    // of ticks after its lift leaves) or on a beat (a function of the room tick; D-257: the lake's platforms, slams and arrows).
    // Betrayals branch off the solution's own steps.
    static class L030Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // The room tick is `tick` past a multiple of `period` (a spinner's beat).
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // D-257: platform k's (Lift_H, then the rafts) west edge on this tick, from its latest fire (on the beat).
        static float RaftX(RouteView v, int k)
        {
            int i = v.Result.Elements.IndexOf(L030Layout.RaftNames[k]);
            int fire = i >= 0 ? v.Last.FireTick[i] : -1;
            return L030Layout.RaftHome(k) + (fire < 0 ? 0f
                : L030Layout.RaftTravel * TrapPath.Progress(v.Last.RoomLifeTick - fire, L030Layout.RaftTicks, L030Layout.RaftHold, L030Layout.RaftTicks));
        }
        // Platform k at its east end (its neighbour at home beyond).
        static RouteCondition AtEnd(int k) => new($"{L030Layout.RaftNames[k]} at its end", v => RaftX(v, k) >= L030Layout.RaftHome(k) + L030Layout.RaftTravel - 1e-3f);
        // Platform k at its east end: jump, steer onto the next in the air (a run before the jump would walk off the end if it
        // came late), letting go `release` past the next's west edge, and land.
        static RouteStep[] Hop(int k, float release) => new[] {
            Until(AtEnd(k)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Hold(Right),
            Until(XAtLeast(L030Layout.RaftHome(k + 1) + release)), Release(), Until(Grounded()), Until(Still()),
        };
        // ---- Ride (D-259) ----
        // Onto Lift_H before it leaves; then from each platform at its east end on to the next at once (a laser locks over five of
        // those ends as the cat gets there); off the last onto the landing.
        static RouteStep[] RideSteps() => new[] {
            new[] { Hold(Right), Until(XAtLeast(L030Layout.LiftHome + .33f)), Release(), Until(Still()), Until(Fired("Lift_H")) },
        }.Concat(Enumerable.Range(0, L030Layout.RaftNames.Length - 1).Select(k => Hop(k, .33f))).Concat(new[] {
            new[] { Until(AtEnd(L030Layout.RaftNames.Length - 1)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Hold(Right), Until(Grounded()),
                Until(XAtLeast(L030Layout.LandingE + 1.2f)), Release(), Until(Still()) },
        }).SelectMany(x => x).ToArray();

        // ---- Shaft ----
        // Ride Lift_V1 up (D-258: no thorns to shuffle past); jump across onto Lift_V2 before it stops (D-261); and from it,
        // before it stops,
        // step west onto Lift_P before Spikes_D comes down; ride it up and over; under Arrow_D's lane just after a shot, into Summit.
        static RouteStep[] ShaftSteps() => new[] {
            Hold(Right), Until(XAtLeast(37.1f + L030Layout.East)), Release(), Until(Fired("Lift_V1")),
            // D-261: Spikes_V comes down as Lift_V1 arrives: jump from it on the way up, onto Lift_V2's west end (over Pillar_B's face).
            Until(Since("Lift_V1", L030Layout.V1Ticks - 20)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Hold(Right),
            Until(XAtLeast(38.4f + L030Layout.East)), Release(), Until(Grounded()), Until(Still()),
            // Spikes_D as Lift_V2 arrives: jump from it on the way up, west onto Lift_P.
            Until(Fired("Lift_V2")), Until(Since("Lift_V2", L030Layout.V2Up - 18)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Hold(Left),
            Until(XAtMost(36.8f + L030Layout.East)), Release(), Until(Grounded()),
            Until(Fired("Lift_P")), Until(Since("Lift_P", L030Layout.PTicks + 2)),
            Until(Beat(L030Layout.DPeriod, 40)), Hold(Right), Until(XAtLeast(42.6f + L030Layout.East)), Release(), Until(Still()),
        };

        // ---- Summit (D-254) ----
        // Run: Hinge_S stands up behind; jump Pit_S1 at its edge onto Crumble_S and run on under Block_1-3; leap Pit_S2 and, as Inv_S
        // turns the stick round mid-air, press back (the way on); keep pressing back till it wears off, then on, after the door.
        static RouteStep[] SummitSteps() => new[] {
            Hold(Right), Until(XAtLeast(L030Layout.HingeTrigX + .5f)),
            Until(XAtLeast(L030Layout.Pit1X0 - .35f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(L030Layout.BlockX[0] + .3f)), Until(XAtLeast(L030Layout.BlockX[2] + L030Layout.BlockW + .6f)),
            Until(XAtLeast(L030Layout.Pit2X0 - .35f)), Jump().Timed(TimedMode.Shift), Until(Airborne()),
            Until(Fired("Inv_S")), Hold(Left), Until(Since("Inv_S", L030Layout.InvTicks)),
            Hold(Right), Until(RoomComplete()),
        };
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L030Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { RideSteps(), ShaftSteps(), SummitSteps() };
            var solution = new Route("L030 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int R0 = 0, S = 1, T = 2;
            RouteStep[] ride = parts[R0], shaft = parts[S], top = parts[T];
            return new RoomRoutes(solution,
                // Ride (D-259).
                new Betrayal("R1 [W]: a cat that walks on after Lift_H has left falls into the lake", "Chasm", DeathCause.Hazard,
                    B("walk on late", R0, 0, Until(Since("Lift_H", 50)), Hold(Right), Until(Dead())), revealedBy: "Lift_H"),
                new Betrayal("R2 [NW]: a cat that waits at Raft_1's end as Laser_1 locks is shot", "Laser_1", DeathCause.Hazard,
                    B("wait", R0, After(ride, "Until(Raft_1 at its end"), Until(Dead()))),
                new Betrayal("R3 [NW]: a cat that waits at Raft_3's end as Laser_3 locks is shot", "Laser_3", DeathCause.Hazard,
                    B("wait", R0, After(ride, "Until(Raft_3 at its end"), Until(Dead()))),
                new Betrayal("R4 [NW]: a cat that waits at Raft_6's end as Laser_6 locks is shot", "Laser_6", DeathCause.Hazard,
                    B("wait", R0, After(ride, "Until(Raft_6 at its end"), Until(Dead()))),
                new Betrayal("R5 [NW]: a cat that waits at Raft_9's end as Laser_9 locks is shot", "Laser_9", DeathCause.Hazard,
                    B("wait", R0, After(ride, "Until(Raft_9 at its end"), Until(Dead()))),
                new Betrayal("R6 [NW]: a cat that waits at Raft_12's end as Laser_12 locks is shot", "Laser_12", DeathCause.Hazard,
                    B("wait", R0, After(ride, "Until(Raft_12 at its end"), Until(Dead()))),
                // Shaft.
                new Betrayal("T11 [NW]: a cat that stays on Lift_V1 at its stop meets Spikes_V", "Spikes_V", DeathCause.Hazard,
                    B("stay", S, After(shaft, "Until(Lift_V1"), Until(Dead()))),
                new Betrayal("T12 [BAIT]: a cat that steps into the alcove falls through Fake_V onto its spikes", "Alcove_Spikes", DeathCause.Hazard,
                    // D-261: jumping west off Lift_V1 on its way up, as the solution jumps east.
                    B("into the alcove", S, 0, Hold(Right), Until(XAtLeast(35.5f + L030Layout.East)), Release(),
                        Until(Since("Lift_V1", L030Layout.V1Ticks - 20)), Jump(), Until(Airborne()), Hold(Left), Until(Dead())), revealedBy: "Fake_V"),
                new Betrayal("T13 [NW]: a cat that stays on Lift_V2 meets Spikes_D", "Spikes_D", DeathCause.Hazard,
                    B("stay", S, After(shaft, "Until(Lift_V2"), Hold(Right), Until(XAtLeast(40.3f + L030Layout.East)), Release(), Until(Dead()))),
                new Betrayal("T14 [W]: a cat that goes under Arrow_D as it shoots is hit", "Arrow_D", DeathCause.Hazard,
                    B("go now", S, After(shaft, "Until(Lift_P"), Until(Beat(L030Layout.DPeriod, 97)), Hold(Right), Until(Dead()))),
                // Summit.
                new Betrayal("T15 [NW]: a cat that stops as Hinge_S stands up is caught", "Hinge_S", DeathCause.Hazard,
                    B("stop", T, After(top, "Until(X>=76.5"), Release(), Until(Dead()))),
                new Betrayal("T16 [NW]: Crumble_S gives way under a cat that stops where it lands", "Pit_S1_Spikes", DeathCause.Hazard,
                    B("stop on it", T, After(top, "Until(Grounded"), Release(), Until(Dead())), revealedBy: "Crumble_S"),
                new Betrayal("T17 [NW]: Block_1 drops on a cat that stops under it", "Block_1", DeathCause.Hazard,
                    B("stop", T, After(top, "Until(X>=85.3"), Release(), Until(Dead()))),
                new Betrayal("T18 [W]: Block_2 drops on a cat that walks", "Block_2", DeathCause.Hazard,
                    B("stop under Block_2", T, After(top, "Until(X>=85.3"), Until(XAtLeast(L030Layout.BlockX[1] + .3f)), Release(), Until(Dead()))),
                new Betrayal("T19 [J]: a cat that keeps pressing on as Inv_S turns the stick round falls short into Pit_S2", "Pit_S2_Spikes", DeathCause.Hazard,
                    B("keep pressing on", T, After(top, "Until(Fired(Inv_S))"), Until(Dead())), revealedBy: "Inv_S"),
                new Betrayal("T20 [NW]: a cat that waits where the door was is crushed by Ceiling_F", "Ceiling_F", DeathCause.Hazard,
                    B("wait at the door's place", T, top.Length - 2, Hold(Right), Until(XAtLeast(L030Layout.DoorX - .8f)), Release(), Until(Dead()))));
        }
    }
}
