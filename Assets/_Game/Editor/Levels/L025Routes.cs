using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-213: level 25's routes ("Rhythm"). A vine is caught in the air by holding Down (D-089); a leap off a vine is a jump while
    // climbing with a direction held; the chimney is climbed by wall jumps (D-110). Written with the layout and not yet replayed
    // (the developer asked for the changes without test runs): re-record before relying on them.
    static class L025Routes
    {
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        static RouteStep[] Steps(params object[] parts)
        {
            var list = new System.Collections.Generic.List<RouteStep>();
            foreach (object p in parts) { if (p is RouteStep s) list.Add(s); else list.AddRange((RouteStep[])p); }
            return list.ToArray();
        }

        // A running jump east from x, landing where it falls.
        static RouteStep[] JumpFrom(float x) => new[] { Hold(Right), Until(XAtLeast(x)), Jump(), Until(Airborne()), Until(Landed()) };
        // Leap east off the vine the cat is on, and catch `vine` (at x) late in the fall.
        static RouteStep[] Leap(string vine, float x) => new[] {
            ReleaseClimb(), Hold(Right), Jump().Timed(TimedMode.Shift), For(26), Hold(R.Down), Until(On(vine, x)), Release(), ReleaseClimb() };
        // Hanging low enough that a leap clears the next vine's reach.
        const float Low = 7f;
        static RouteStep[] Down(float y) => new[] { Hold(R.Down), Until(Below(y)), ReleaseClimb() };

        // ---- Descent ----
        // Off the start onto Step_1 (it crumbles: on at once); off onto Step_2 and straight on onto Step_3; a long running jump past
        // Fake_4 onto Step_4; a jump onto Step_5's east half (Eye_D shoots at a cat that waits).
        static RouteStep[] Descent() => Steps(
            Hold(Right), Until(XAtLeast(9f)), Until(Landed()),
            JumpFrom(11.4f), JumpFrom(18.8f), JumpFrom(26.6f), Release(), Until(Still()));

        // ---- Valley ----
        // Onto V1; vine to vine ahead of the snapping wave, moving out of Eye_V's locked line; off V6 onto the east floor.
        static RouteStep[] Valley() => Steps(
            Hold(Right), Jump(), Until(Falling()), Hold(R.Down), Until(On("V1", L025Layout.VineX[0])), Release(), ReleaseClimb(),
            Down(Low), Leap("V2", L025Layout.VineX[1]),
            Down(Low), Leap("V3", L025Layout.VineX[2]),
            Down(Low), Leap("V4", L025Layout.VineX[3]),
            Down(Low), Leap("V5", L025Layout.VineX[4]),
            Down(Low), Leap("V6", L025Layout.VineX[5]),
            Down(Low), ReleaseClimb(), Hold(Right), Jump(), Until(Landed()), Release(), Until(Still()));

        // ---- Climb ----
        // East into the chimney; up it by wall jumps; over Wall_E's top; after the door.
        static RouteStep[] Climb() => Steps(
            Hold(Right), Until(XAtLeast(67f)), Release(), Until(Still()),
            Jump(), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(Landed()), Hold(Right), Until(RoomComplete()));

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Descent(), Valley(), Climb() };
            return new RoomRoutes(new Route("L025 solution", parts.SelectMany(p => p).ToArray()));
        }
    }
}
