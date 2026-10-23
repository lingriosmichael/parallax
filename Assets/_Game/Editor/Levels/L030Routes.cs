using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-175: level 30's routes ("Meltdown"). Waits are on the layout's clocks: the slams' and G_1's beat, Lift_2's pose and
    // elements' fire ticks. A vine is caught in the air by holding Down (D-089); a leap off it is a jump while climbing with a
    // direction held. Betrayals branch off the solution's own steps.
    static class L030Routes
    {
        const int Beat = L030Layout.Beat;
        const float S2 = L030Layout.S2;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition On(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // Climbing, snapped to this vine's centre.
        static RouteCondition OnVine(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);

        // Catch `vine` (at x) by holding Down, then let go of the stick.
        static RouteStep[] Catch(string vine, float x) => new[] { Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };
        // Leap off the vine toward `dir` and catch `vine` (at x) once over it.
        static RouteStep[] Leap(int dir, string vine, float x, float catchAt) => new[] {
            ReleaseClimb(), Hold(dir), Jump().Timed(TimedMode.Shift), dir > 0 ? Until(XAtLeast(catchAt)) : Until(XAtMost(catchAt)), Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };

        static RouteStep[] Steps(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();

        // ---- Cave-in, storey 1, walked east ----
        // Run under the rocks without stopping; jump Pop_1 as it springs; jump Collapse_1; stop short of Slam_1 and go through as it
        // goes back up; stand on G_1 and steer west onto storey 2 once it throws the cat up.
        static RouteStep[] CaveIn() => new[] {
            Hold(Right), Until(Fired("Pop_1")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(16.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(21.9f)), Release(), Until(Still()), Until(On(Beat, L030Layout.Slam1 + 33)), Hold(Right).Timed(TimedMode.Shift),
            Until(XAtLeast(L030Layout.G1X - .2f)), Release(), Until(Still()),
            Until(Airborne()), Until(YAtLeast(S2 + .3f)), Hold(Left), Until(Grounded()), Release(), Until(Still()),
        };

        // ---- Roots, storey 2, walked west ----
        // Stop short of Slam_2 and go through as it goes back up; jump for V1 ahead of Hinge_2, leap V1 to V2 to V3, and off V3 past
        // the spike field; onto Lift_2 once it's home, and up.
        static RouteStep[] Roots() => Steps(
            new[] { Hold(Left), Until(XAtMost(24.3f)), Release(), Until(Still()), Until(On(Beat, L030Layout.Slam2 + 33)), Hold(Left).Timed(TimedMode.Shift),
                Until(XAtMost(19.6f)), Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V1", L030Layout.V1X),
            Leap(Left, "V2", L030Layout.V2X, L030Layout.V2X + .4f),
            Leap(Left, "V3", L030Layout.V3X, L030Layout.V3X + .4f),
            new[] { ReleaseClimb(), Hold(Left), Jump().Timed(TimedMode.Shift), Until(Landed()),
                Until(XAtMost(3.4f)), Release(), Until(Still()), Until(Home("Lift_2")), Hold(Left), Until(XAtMost(1.9f)), Release(), Until(Still()),
                Until(Moving("Lift_2")), Until(Stopped("Lift_2")) });

        // ---- Collapse, storey 3, walked east ----
        // Hinge_3 chases from behind: jump Pop_3 as it springs; jump Collapse_3; stop short of Slam_D, hop onto it once it's down;
        // jump Pop_D as it springs, into the door.
        static RouteStep[] Collapse() => new[] {
            Hold(Right), Until(Fired("Pop_3")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(14.1f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(21.9f)), Release(), Until(Still()), Until(Stopped("Slam_D")),
            Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(Fired("Pop_D")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
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
            RouteStep[][] parts = { CaveIn(), Roots(), Collapse() };
            var solution = new Route("L030 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int Cv = 0, Rt = 1, Cl = 2;
            RouteStep[] cave = parts[Cv], roots = parts[Rt], collapse = parts[Cl];

            return new RoomRoutes(solution,
                // Cave-in.
                new Betrayal("T1 [NW]: a cat that stops under the cave-in is crushed by a rock", "Rock_2", DeathCause.Hazard,
                    B("stop", Cv, 1, Until(XAtLeast(6.4f)), Release(), Until(Dead()))),
                new Betrayal("T2 [J]: a cat that runs on is caught by Pop_1", "Pop_1", DeathCause.Hazard,
                    B("run on", Cv, After(cave, "Until(Fired(Pop_1))"), Until(Dead()))),
                new Betrayal("T3 [J]: a cat that walks onto Collapse_1 falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("walk on", Cv, After(cave, "Until(X>=16.6)"), Until(Dead())), revealedBy: "Collapse_1"),
                new Betrayal("T4 [W]: a cat that goes before Slam_1 is back up is crushed", "Slam_1", DeathCause.Hazard,
                    B("go now", Cv, After(cave, "Until(Still)"), Until(On(Beat, L030Layout.Slam1 - 12)), Hold(Right), Until(Dead()))),
                // Roots.
                new Betrayal("T5 [W]: a cat that goes before Slam_2 is back up is crushed", "Slam_2", DeathCause.Hazard,
                    B("go now", Rt, After(roots, "Until(Still)"), Until(On(Beat, L030Layout.Slam2 - 12)), Hold(Left), Until(Dead()))),
                new Betrayal("T6 [NW]: a cat that stops at the spike field's edge is pushed onto it by Hinge_2", "Spikes_2", DeathCause.Hazard,
                    B("stop", Rt, After(roots, "Until(X<=19.6)") - 1, Until(XAtMost(18.9f)), Release(), Until(Dead())), revealedBy: "Hinge_2"),
                new Betrayal("T7 [NW]: a cat that hangs on V1 goes down with it onto the spikes", "Spikes_2", DeathCause.Hazard,
                    B("hang on", Rt, After(roots, "ReleaseClimb()"), Until(Dead())), revealedBy: "V1"),
                new Betrayal("T8 [NW]: a cat that hangs on V2 goes down with it onto the spikes", "Spikes_2", DeathCause.Hazard,
                    B("hang on", Rt, After(roots, "ReleaseClimb()", 3), Until(Dead())), revealedBy: "V2"),
                new Betrayal("T9 [NW]: a cat that hangs on V3 goes down with it onto the spikes", "Spikes_2", DeathCause.Hazard,
                    B("hang on", Rt, After(roots, "ReleaseClimb()", 5), Until(Dead())), revealedBy: "V3"),
                // Collapse.
                new Betrayal("T10 [NW]: a cat that stops with the wall behind it is caught by Hinge_3", "Hinge_3", DeathCause.Hazard,
                    B("stop", Cl, 1, Until(XAtLeast(7.2f)), Release(), Until(Dead()))),
                new Betrayal("T11 [J]: a cat that runs on is caught by Pop_3", "Pop_3", DeathCause.Hazard,
                    B("run on", Cl, After(collapse, "Until(Fired(Pop_3))"), Until(Dead()))),
                new Betrayal("T12 [J]: a cat that walks onto Collapse_3 drops onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("walk on", Cl, After(collapse, "Until(X>=14.1)"), Until(Dead())), revealedBy: "Collapse_3"),
                new Betrayal("T13 [SS]: a cat that runs on under Slam_D is crushed", "Slam_D", DeathCause.Hazard,
                    B("run on", Cl, After(collapse, "Until(X>=21.9)"), Until(Dead()))),
                new Betrayal("T14 [J]: a cat that runs on to the door is caught by Pop_D", "Pop_D", DeathCause.Hazard,
                    B("run on", Cl, After(collapse, "Until(Fired(Pop_D))"), Until(Dead()))));
        }
    }
}
