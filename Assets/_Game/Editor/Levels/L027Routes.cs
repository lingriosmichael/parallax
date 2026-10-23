using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-171: level 27's routes. A vine is caught in the air by holding Down or Up (D-089); a leap off it is a jump while
    // climbing with a direction held. Each vent ride waits on the vent, steers once launched, and catches the next vine or
    // ledge. Betrayals branch off the solution's own steps.
    static class L027Routes
    {
        const int Beat = L027Layout.Beat;
        const float S2 = L027Layout.S2, S3 = L027Layout.S3;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition On(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // Climbing, snapped to this vine's centre.
        static RouteCondition OnVine(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // Catch `vine` (at x) by holding Down, then let go of the stick.
        static RouteStep[] Catch(string vine, float x) => new[] { Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };
        // Climb down until the cat's centre is under y.
        static RouteStep[] Down(float y) => new[] { Hold(R.Down), Until(Below(y)), ReleaseClimb() };
        // Leap off the vine toward `dir` and catch `vine` (at x) once over it.
        static RouteStep[] Leap(int dir, string vine, float x, float catchAt) => new[] {
            ReleaseClimb(), Hold(dir), Jump().Timed(TimedMode.Shift), dir > 0 ? Until(XAtLeast(catchAt)) : Until(XAtMost(catchAt)), Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };

        static RouteStep[] Steps(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();

        // ---- Lake, storey 1, walked east ----
        // Onto V1 off the ground, V2, low off V2 onto Pier_G; wait for G_1, steer east on the eruption to V3; low off V3 onto the
        // ground; wait on G_2 and steer west onto storey 2.
        static RouteStep[] Lake() => Steps(
            new[] { Hold(Right), Until(XAtLeast(2.7f)), Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V1", L027Layout.V1X),
            Down(.55f), Leap(Right, "V2", L027Layout.V2X, 7.6f),
            Down(.55f), new[] { ReleaseClimb(), Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(12.2f)), Release(), Until(Landed()), Until(Still()),
                Until(Airborne()), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L027Layout.V3X - .5f)) }, Catch("V3", L027Layout.V3X),
            Down(.55f), new[] { ReleaseClimb(), Hold(Right), Jump().Timed(TimedMode.Shift), Until(Landed()),
                Until(XAtLeast(L027Layout.G2X - .2f)), Release(), Until(Still()),
                Until(Airborne()), Until(YAtLeast(S2 + .3f)), Hold(Left), Until(Grounded()), Release(), Until(Still()) });

        // ---- Gallery, storey 2, walked west ----
        // Stop short of Slam_2 and go through as it goes back up; jump onto V4 and leap off it over Bed_2; wait on G_3, steer west
        // on the eruption, catch V_S high, climb a little and leap east onto storey 3.
        static RouteStep[] Gallery() => Steps(
            new[] { Hold(Left), Until(XAtMost(22.4f)), Release(), Until(Still()),
                Until(On(Beat, L027Layout.Slam2 + 33)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(15.6f)), Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V4", L027Layout.V4X),
            new[] { ReleaseClimb(), Hold(Left), Jump().Timed(TimedMode.Shift), Until(Landed()),
                Until(XAtMost(L027Layout.G3X + .2f)), Release(), Until(Still()),
                Until(Airborne()), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(L027Layout.VSX + .6f)), Hold(Up), Until(OnVine("V_S", L027Layout.VSX)), Until(YAtLeast(S3 - .3f)),
                ReleaseClimb(), Hold(Right), Jump(), Until(XAtLeast(3.6f)), Release(), Until(Landed()), Until(Still()) });

        // ---- Crown, storey 3, walked east ----
        // Onto V5, V6; wait on V6 until GD_C has stopped, leap through its column to V7, V8; low off V8 onto the floor; jump Pop_3;
        // wait on G_D and steer east onto the ledge, into the door.
        static RouteStep[] Crown() => Steps(
            new[] { Hold(Right), Until(XAtLeast(4.3f)), Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V5", L027Layout.V5X),
            Down(S3 + 1.6f), Leap(Right, "V6", L027Layout.V6X, 9.2f),
            Down(S3 + 1.6f), new[] { Until(Fired("GD_C")), Until(Since("GD_C", 66)) }, Leap(Right, "V7", L027Layout.V7X, 12.2f),
            Down(S3 + 1.6f), Leap(Right, "V8", L027Layout.V8X, 15.2f),
            Down(S3 + 1.6f), new[] { ReleaseClimb(), Hold(Right), Jump().Timed(TimedMode.Shift), Until(Landed()),
                Until(Fired("Pop_3")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtLeast(L027Layout.GDoorX - .2f)), Release(), Until(Still()),
                Until(Airborne()), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()) });

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of part `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L027Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Lake(), Gallery(), Crown() };
            var solution = new Route("L027 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int L = 0, G = 1, C = 2;
            RouteStep[] lake = parts[L], gallery = parts[G], crown = parts[C];

            // A cat that stays on `vine` (part, the step after catching it) falls when it snaps.
            Betrayal Hang(string tag, string vine, string killer, int part, int index) =>
                new($"{tag} [NW]: a cat that hangs on {vine} falls when it snaps", killer, DeathCause.Hazard,
                    B("hang on " + vine, part, index, Release(), ReleaseClimb(), Until(Dead())), revealedBy: vine);

            return new RoomRoutes(solution,
                // Lake.
                Hang("T1", "V1", "Pit_1", L, After(lake, "Until(on V1)")),
                Hang("T2", "V2", "Pit_1", L, After(lake, "Until(on V2)")),
                new Betrayal("T3 [W]: a cat that rides G_1 straight up meets Spikes_G", "Spikes_G", DeathCause.Hazard,
                    B("straight up", L, After(lake, "Until(Still)"), Until(Dead())), revealedBy: "G_1"),
                Hang("T4", "V3", "Pit_2", L, After(lake, "Until(on V3)")),
                // Gallery.
                new Betrayal("T5 [W]: a cat that goes before Slam_2 is back up is crushed", "Slam_2", DeathCause.Hazard,
                    B("go now", G, After(gallery, "Until(Still)"), Until(On(Beat, L027Layout.Slam2 - 12)), Hold(Left), Until(Dead()))),
                Hang("T6", "V4", "Spikes_2", G, After(gallery, "Until(on V4)")),
                new Betrayal("T7 [W]: a cat that rides G_3 straight up meets Spikes_C3", "Spikes_C3", DeathCause.Hazard,
                    B("straight up", G, After(gallery, "Until(Still)", 2), Until(Dead())), revealedBy: "G_3"),
                new Betrayal("T8 [J]: a cat that jumps onto V_S and climbs falls onto Spikes_S when it snaps", "Spikes_S", DeathCause.Hazard,
                    B("jump and climb", G, After(gallery, "Until(Still)", 2), Hold(Left), Until(XAtMost(4.3f)), Jump(), Until(Falling()), Hold(Up), Until(Dead())), revealedBy: "V_S"),
                // Crown.
                Hang("T9", "V5", "Spikes_3", C, After(crown, "Until(on V5)")),
                Hang("T10", "V6", "Spikes_3", C, After(crown, "Until(on V6)")),
                new Betrayal("T11 [W]: a cat that leaps from V6 as GD_C blows is blown down onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("leap as it blows", C, After(crown, "Until(GD_C"), Until(On(Beat, L027Layout.GDErupt - 6)), ReleaseClimb(), Hold(Right), Jump(), Until(Dead())), revealedBy: "GD_C"),
                Hang("T12", "V7", "Spikes_3", C, After(crown, "Until(on V7)")),
                Hang("T13", "V8", "Spikes_3", C, After(crown, "Until(on V8)")),
                new Betrayal("T14 [W]: a cat that runs on is caught by Pop_3", "Pop_3", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(landed)"), Until(Dead()))));
        }
    }
}
