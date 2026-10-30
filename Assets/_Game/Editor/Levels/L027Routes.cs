using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-232: level 27's routes ("Updraft", the canyon). A vine is caught in the air by holding Down (D-089); a leap off it is a jump
    // while climbing with a direction held. Written with the layout and not yet replayed (the developer asked for the changes without
    // test runs): re-record before relying on them.
    static class L027Routes
    {
        static RouteCondition OnVine(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        static RouteStep[] Steps(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();

        // Catch `vine` (at x) by holding Down, then let go of the stick.
        static RouteStep[] Catch(string vine, float x) => new[] { Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };
        // Leap off the vine east and catch `vine` (at x) once past catchAt.
        static RouteStep[] Leap(string vine, float x, float catchAt) => new[] {
            ReleaseClimb(), Hold(Right), Jump().Timed(TimedMode.Shift), Until(XAtLeast(catchAt)), Hold(R.Down), Until(OnVine(vine, x)), Release(), ReleaseClimb() };
        // Leap off the vine east, landing on what's under.
        static RouteStep[] LeapOff() => new[] { ReleaseClimb(), Hold(Right), Jump(), Until(Landed()), Release(), Until(Still()) };
        // A running jump east from x, landing where it falls.
        static RouteStep[] JumpFrom(float x) => new[] { Hold(Right), Until(XAtLeast(x)), Jump(), Until(Airborne()), Until(Landed()), Release(), Until(Still()) };

        // ---- Swing: V1, V2 past Spin_1, onto Pad_3 ----
        static RouteStep[] Swing() => Steps(
            new[] { Hold(Right), Until(XAtLeast(4.4f)), Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V1", L027Layout.V1X),
            Leap("V2", L027Layout.V2X, 10.4f), LeapOff());

        // ---- Crumbles: over Crumb_1 and Crumb_2 between GD_5's blasts, to V3, V4, onto Mid_Ledge ----
        static RouteStep[] Crumbles() => Steps(
            new[] { Until(Beat(L027Layout.Beat, L027Layout.GD5Erupt + 45)) }, JumpFrom(16.3f),
            new[] { Hold(Right), Jump(), Until(Landed()), Jump(), Until(XAtLeast(L027Layout.V3X - .6f)) }, Catch("V3", L027Layout.V3X),
            Leap("V4", L027Layout.V4X, 27.4f), LeapOff());

        // ---- Crushers: under Slam_1 and Slam_2 between their drops; V5, V6 past Swing_1; onto Ledge_3 ----
        static RouteStep[] Crushers() => Steps(
            new[] { Until(Beat(L027Layout.SlamPeriod, L027Layout.SlamMove + L027Layout.SlamHold + L027Layout.SlamBack)), Hold(Right), Until(XAtLeast(39.6f)),
                Jump().Timed(TimedMode.Shift), Until(Falling()) }, Catch("V5", L027Layout.V5X),
            Leap("V6", L027Layout.V6X, 45.4f), LeapOff());

        // ---- Last: over Pit_3 (Inv_1 swaps the stick), V7, V8, onto Ledge_E; Slam_D; the door ----
        static RouteStep[] Last() => Steps(
            new[] { Hold(Right), Until(XAtLeast(52.2f)), Hold(Left), Until(XAtLeast(52.7f)), Jump(), Until(Landed()), Release(), Until(Still()) },
            JumpFrom(59.6f).Take(3).ToArray(), new[] { Until(Falling()) }, Catch("V7", L027Layout.V7X),
            Leap("V8", L027Layout.V8X, 65.4f), LeapOff(),
            new[] { Hold(Right), Until(XAtLeast(71.2f)), Release(), Until(Still()), For(80), Hold(Right), Until(RoomComplete()) });

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Swing(), Crumbles(), Crushers(), Last() };
            return new RoomRoutes(new Route("L027 solution", parts.SelectMany(p => p).ToArray()));
        }
    }
}
