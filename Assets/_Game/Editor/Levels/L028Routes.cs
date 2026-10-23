using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-173: level 28's routes ("Mirror Lanes"). On the ceiling (gravity up) a jump leaps toward the floor and Grounded means
    // standing on the ceiling. Betrayals branch off the solution's own steps.
    static class L028Routes
    {
        // Arrow_M2's shot in flight (from its last fire: the tell, then 0.3 u a tick from the jamb's face), its tip within `d` of
        // the cat's west side.
        static RouteCondition ArrowWithin(float d) => new($"Arrow_M2 within {d}", v =>
        {
            int i = v.Result.Elements.IndexOf("Arrow_M2");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int flying = v.Last.RoomLifeTick - v.Last.FireTick[i] - L028Layout.ArrowTell;
            float tip = L028Layout.JambFace + .8f + .3f * flying, back = v.Last.X - .5f;
            return flying >= 0 && tip < back && back - tip <= d;
        });

        // ---- Mirror, corridor 1, walked east ----
        // Jump Pop_F1 as it springs; stop as Slam_F1 comes down, hop onto it and off; jump Collapse_F1; into Flip_U1 at a run. On
        // the ceiling: jump CC_C1, stop as Piston_C1 rises, hop onto it and off; jump Pop_C1 and fall up the hole into corridor 2.
        static RouteStep[] Mirror() => new[] {
            Hold(Right), Until(Fired("Pop_F1")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(7.5f)), Release(), Until(Still()), Until(Fired("Slam_F1")), Until(Stopped("Slam_F1")),
            Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(10.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityUp()), Until(Grounded()),
            Until(XAtLeast(19.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(24f)), Release(), Until(Still()), Until(Fired("Piston_C1")), Until(Stopped("Piston_C1")),
            Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(Fired("Pop_C1")), Jump().Timed(TimedMode.Shift), Until(Airborne()),
            Until(YAtLeast(L028Layout.F2 + .5f)), Release(), Until(Grounded()), Until(Still()),
        };

        // ---- Arrows, corridor 2, walked west ----
        // Over Kerb_C2; jump the shot along the ceiling as it comes; jump Pop_C2 as it springs, into Flip_D2 as it lands, letting go
        // so the cat drops straight; jump Pop_F2 as it springs; jump the shot along the floor; into Flip_U3, letting go so the cat falls
        // straight up the shaft.
        static RouteStep[] Arrows() => new[] {
            Hold(Left), Until(XAtMost(30.4f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(ArrowWithin(2.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(Fired("Pop_C2")), Jump().Timed(TimedMode.Shift), Until(Airborne()),
            Until(GravityDown()), Release(), Until(Grounded()),
            Hold(Left), Until(Fired("Pop_F2")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(ArrowWithin(2.5f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(GravityUp()), Release(), Until(Grounded()), Until(Still()),
        };

        // ---- Crown, corridor 3, walked east ----
        // Jump Pop_C3; stop as Slam_C3 rises, hop onto it and off; jump CC_C3, into Flip_D3, letting go; jump Collapse_F3; stop as
        // Slam_F3 comes down, hop onto it and off; jump Pop_F3, into the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(Fired("Pop_C3")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(8.3f)), Release(), Until(Still()), Until(Fired("Slam_C3")), Until(Stopped("Slam_C3")),
            Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(11.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()),
            Until(GravityDown()), Release(), Until(Grounded()),
            Hold(Right), Until(XAtLeast(16.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(21.5f)), Release(), Until(Still()), Until(Fired("Slam_F3")), Until(Stopped("Slam_F3")),
            Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
            Until(Fired("Pop_F3")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(RoomComplete()),
        };

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L028Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Mirror(), Arrows(), Crown() };
            var solution = new Route("L028 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int M = 0, A = 1, C = 2;
            RouteStep[] mirror = parts[M], arrows = parts[A], crown = parts[C];

            return new RoomRoutes(solution,
                // Mirror.
                new Betrayal("T1 [J]: a cat that runs on is caught by Pop_F1", "Pop_F1", DeathCause.Hazard,
                    B("run on", M, After(mirror, "Until(Fired(Pop_F1))"), Until(Dead()))),
                new Betrayal("T2 [SS]: a cat that runs on under Slam_F1 is crushed", "Slam_F1", DeathCause.Hazard,
                    B("run on", M, After(mirror, "Until(X>=7.5)"), Until(Dead()))),
                new Betrayal("T3 [J]: a cat that walks onto Collapse_F1 falls into Pit_C1", "Pit_C1", DeathCause.Hazard,
                    B("walk on", M, After(mirror, "Until(X>=10.6)"), Until(Dead())), revealedBy: "Collapse_F1"),
                new Betrayal("T4 [J]: a cat that walks on along the ceiling falls up through CC_C1 onto Spikes_CC1", "Spikes_CC1", DeathCause.Hazard,
                    B("walk on", M, After(mirror, "Until(X>=19.6)"), Until(Dead())), revealedBy: "CC_C1"),
                new Betrayal("T5 [SS]: a cat that runs on as Piston_C1 rises is crushed into the ceiling", "Piston_C1", DeathCause.Hazard,
                    B("run on", M, After(mirror, "Until(X>=24)"), Until(Dead()))),
                new Betrayal("T6 [J]: a cat that runs on is caught by Pop_C1", "Pop_C1", DeathCause.Hazard,
                    B("run on", M, After(mirror, "Until(Fired(Pop_C1))"), Until(Dead()))),
                // Arrows.
                new Betrayal("T7 [J]: a cat that stands on the ceiling is shot by Arrow_M2", "Arrow_M2", DeathCause.Hazard,
                    B("stand", A, After(arrows, "Until(Grounded)"), Release(), Until(Dead()))),
                new Betrayal("T8 [J]: a cat that runs on along the ceiling is caught by Pop_C2", "Pop_C2", DeathCause.Hazard,
                    B("run on", A, After(arrows, "Until(Fired(Pop_C2))"), Until(Dead()))),
                new Betrayal("T9 [J]: a cat that runs on along the floor is caught by Pop_F2", "Pop_F2", DeathCause.Hazard,
                    B("run on", A, After(arrows, "Until(Fired(Pop_F2))"), Until(Dead()))),
                // Crown.
                new Betrayal("T10 [J]: a cat that runs on is caught by Pop_C3", "Pop_C3", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(Fired(Pop_C3))"), Until(Dead()))),
                new Betrayal("T11 [SS]: a cat that runs on as Slam_C3 rises is crushed into the roof", "Slam_C3", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(X>=8.3)"), Until(Dead()))),
                new Betrayal("T12 [J]: a cat that walks on along the ceiling falls up through CC_C3 onto Spikes_CC3", "Spikes_CC3", DeathCause.Hazard,
                    B("walk on", C, After(crown, "Until(X>=11.4)"), Until(Dead())), revealedBy: "CC_C3"),
                new Betrayal("T13 [J]: a cat that walks onto Collapse_F3 falls into Pit_F3", "Pit_F3", DeathCause.Hazard,
                    B("walk on", C, After(crown, "Until(X>=16.6)"), Until(Dead())), revealedBy: "Collapse_F3"),
                new Betrayal("T14 [SS]: a cat that runs on under Slam_F3 is crushed", "Slam_F3", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(X>=21.5)"), Until(Dead()))),
                new Betrayal("T15 [J]: a cat that runs on is caught by Pop_F3", "Pop_F3", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(Fired(Pop_F3))"), Until(Dead()))));
        }
    }
}
