using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-143): level 14's routes. A vine is caught in the air by holding Down (|Climb| >= 0.5, D-089); a leap off a
    // vine is a jump while climbing with a direction held.
    static class L014Routes
    {
        // Climbing, snapped to this vine's centre.
        static RouteCondition On(string vine, float x) => new($"on {vine}", v => v.Last.IsClimbing && Math.Abs(v.Last.X - x) < .05f);
        static RouteCondition Below(float y) => new("Y<" + y.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Last.Y < y);
        // Standing, off the vine.
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded && !v.Last.IsClimbing);

        static RouteStep[] Steps(params object[] parts)
        {
            var list = new System.Collections.Generic.List<RouteStep>();
            foreach (object p in parts) { if (p is RouteStep s) list.Add(s); else list.AddRange((RouteStep[])p); }
            return list.ToArray();
        }

        // Hanging low enough (centre y) that a leap stays under the high lane.
        const float Low1 = .55f, Low2 = S2 + .55f, Low3 = S3 + .55f;
        const float S2 = L014Layout.S2, S3 = L014Layout.S3;

        // Leap off the vine the cat is on toward `dir`, and catch `vine` (at x) late in the fall.
        static RouteStep[] Leap(int dir, string vine, float x) => new[] {
            ReleaseClimb(), Hold(dir), Jump().Timed(TimedMode.Shift), For(28), Hold(R.Down), Until(On(vine, x)), Release(), ReleaseClimb() };

        // The launcher's latest shot was fired between `from` and `to` ticks ago (it has gone by, the next one isn't near).
        static RouteCondition ShotAge(string arrow, int from, int to) => new($"{arrow} shot {from}-{to} ago", v =>
        {
            int i = v.Result.Elements.IndexOf(arrow);
            int age = v.Last.RoomLifeTick - v.Last.FireTick[i];
            return i >= 0 && v.Last.FireTick[i] >= 0 && age >= from && age <= to;
        });

        static RouteStep[] Down(float y) => new[] { Hold(R.Down), Until(Below(y)), ReleaseClimb() };

        static RouteStep[] Field() => Steps(
            // The start: Hinge_0 stands up behind: run and leap onto V1.
            Hold(Right), Until(XAtLeast(3.6f)), Jump(), Until(Falling()), Hold(R.Down), Until(On("V1", 5.6f)), Release(), ReleaseClimb(),
            Down(Low1), Leap(Right, "V2", 8.4f),
            Down(Low1), Leap(Right, "V3", 11.2f),
            Down(Low1), Leap(Right, "V4", 14.2f),
            // Off V4 low, under Pier_1, through Arrow_1Lo's lane once its shot has gone by.
            Down(.35f), Until(ShotAge("Arrow_1Lo", 22, 70)), Down(-.3f), Hold(Right), Jump().Timed(TimedMode.Shift), Until(Landed()),
            // Lift_1: wait short of it until it's on its way down, then cross.
            Until(XAtLeast(20.4f)), Release(), Until(Still()), Until(Moving("Lift_1")), Until(Home("Lift_1")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(25.6f)), Jump(), Until(Landed()),
            // V_Up1 up the east shaft; leap west onto Landing_2.
            Until(XAtLeast(30.6f)), Release(), Hold(Up), Until(On("V_Up1", 30.75f)), Until(YAtLeast(5.8f)),
            ReleaseClimb(), Hold(Left), Jump(), For(14), Release(), Until(Landed()), Until(Still()));

        static RouteStep[] Ledges() => Steps(
            Hold(Left), Until(XAtMost(27.1f)), Jump(), Until(Falling()), Hold(R.Down), Until(On("V5", 25.4f)), Release(), ReleaseClimb(),
            Down(Low2), Leap(Left, "V6", 22.6f),
            Down(Low2), Leap(Left, "V7", 19.8f),
            Down(S2 + .35f), Until(ShotAge("Arrow_2Lo", 18, 60)), Down(S2 + .1f), Hold(Left), Jump().Timed(TimedMode.Shift), Until(Landed()),
            // Lift_2: stop short as it goes, cross once it's down.
            Until(XAtMost(15.2f)), Release(), Until(Still()), Until(Fired("Lift_2")), Until(Moving("Lift_2")), Until(Home("Lift_2")),
            Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(5.6f)), Jump(), Until(Landed()),
            // V_Up2 up the west shaft; leap east onto S3_Land.
            Until(XAtMost(1.4f)), Release(), Hold(Up), Until(On("V_Up2", 1.25f)), Until(YAtLeast(11.3f)),
            ReleaseClimb(), Hold(Right), Jump(), For(14), Release(), Until(Landed()), Until(Still()));

        static RouteStep[] Roof() => Steps(
            Hold(Right), Until(XAtLeast(4.0f)), Jump(), Until(Falling()), Hold(R.Down), Until(On("V8", 5.6f)), Release(), ReleaseClimb(),
            Down(Low3), Leap(Right, "V9", 8.4f),
            // Slam_10 comes down V10's column: let it go back up.
            Down(Low3), Until(Fired("Slam_10")), Until(Moving("Slam_10")), Until(Home("Slam_10")), Leap(Right, "V10", 11.2f),
            Down(Low3), Leap(Right, "V11", 14f),
            Down(Low3), Hold(Right), Jump().Timed(TimedMode.Shift), Until(Landed()),
            // Slam_3: stop short, let it land, hop it.
            Until(XAtLeast(18.5f)), Release(), Until(Still()), Until(Fired("Slam_3")), Until(Stopped("Slam_3")),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(19.2f)), Jump(), Until(Landed()),
            // Hinge_5 stands up behind: jump Spikes_5 at once, into the chimney under Hang_C.
            Until(XAtLeast(25.3f)), Jump(), Until(Airborne()), Until(Landed()), Release(), Until(Still()),
            // The chimney: Wall_C's moss, Hang_C's, then over Wall_C's top to the door.
            Jump(), Hold(Right), Until(ClingingRight()), Release(),
            Jump(), For(2), Hold(Left), Until(ClingingLeft()), Release(),
            Jump(), For(2), Hold(Right), Until(RoomComplete()));

        static Route Prefix(Route solution, string label, string name, params RouteStep[] then) => Route.PrefixOf(solution, label, name, then);

        // A cat that stays on `vine` falls into the thorns when the wave snaps it.
        static Betrayal Hang(Route solution, string vine, string killer) =>
            new($"Canopy [NW]: a cat that hangs on {vine} falls when it snaps", killer, DeathCause.Hazard,
                Prefix(solution, $"Until(on {vine})", "hang on " + vine, Release(), ReleaseClimb(), Until(Dead())), revealedBy: vine);

        public static RoomRoutes Build()
        {
            var solution = new Route("L014 solution", Steps(Field(), Ledges(), Roof()));
            return new RoomRoutes(solution,
                // Field.
                new Betrayal("T1 [J]: a cat that waits at the start is pushed into the trench by Hinge_0", "Thorns_1", DeathCause.Hazard,
                    new Route("wait at the start", Until(Dead())), revealedBy: "Hinge_0"),
                Hang(solution, "V1", "Thorns_1"), Hang(solution, "V2", "Thorns_1"), Hang(solution, "V3", "Thorns_1"), Hang(solution, "V4", "Thorns_1"),
                new Betrayal("T2 [LW]: a cat that leaps on from high on V1 meets Arrow_1Hi", "Arrow_1Hi", DeathCause.Hazard,
                    Prefix(solution, "Until(on V1)", "leap from high", Release(), ReleaseClimb(), Hold(Right), Jump(), For(28), Hold(R.Down), Until(Dead()))),
                new Betrayal("T3 [SS]: a cat that drops low on V4 as Arrow_1Lo fires is struck", "Arrow_1Lo", DeathCause.Hazard,
                    Prefix(solution, "Until(on V4)", "drop at once", Hold(R.Down), Until(Below(-.3f)), ReleaseClimb(), Until(Dead()))),
                new Betrayal("T4 [W]: a cat that stands on Lift_1 is crushed into the trench above", "Lift_1", DeathCause.Hazard,
                    Prefix(solution, "Until(X>=20.4)", "stand on the lift", Until(XAtLeast(23f)), Release(), Until(Dead())), revealedBy: "Lift_1"),
                new Betrayal("T5 [J]: a cat that walks onto Collapse_1 falls into Pit_1", "Pit_1", DeathCause.Hazard,
                    Prefix(solution, "Until(Home(Lift_1))", "walk on", Hold(Right), Until(XAtLeast(27.2f)), Release(), Until(Dead())), revealedBy: "Collapse_1"),
                // Ledges.
                Hang(solution, "V5", "Thorns_2"), Hang(solution, "V6", "Thorns_2"), Hang(solution, "V7", "Thorns_2"),
                new Betrayal("T6 [LW]: a cat that leaps on from high on V5 meets Arrow_2Hi", "Arrow_2Hi", DeathCause.Hazard,
                    Prefix(solution, "Until(on V5)", "leap from high", Release(), ReleaseClimb(), Hold(Left), Jump(), For(28), Hold(R.Down), Until(Dead()))),
                new Betrayal("T7 [SS]: a cat that climbs down low on V5 is struck by Arrow_2Lo", "Arrow_2Lo", DeathCause.Hazard,
                    Prefix(solution, "Until(on V5)", "hang low", Hold(R.Down), Until(Below(S2 + .1f)), ReleaseClimb(), Until(Dead()))),
                new Betrayal("T8 [W]: a cat that runs on as Lift_2 shoots up is crushed into trench 3's bed", "Lift_2", DeathCause.Hazard,
                    Prefix(solution, "Until(X<=15.2)", "run on", Until(Dead())), revealedBy: "Lift_2"),
                new Betrayal("T9 [J]: a cat that stops past Hinge_3 is pushed onto Spikes_2", "Spikes_2", DeathCause.Hazard,
                    Prefix(solution, "Until(Home(Lift_2))", "stop", Hold(Left), Until(XAtMost(5.5f)), Release(), Until(Dead())), revealedBy: "Hinge_3"),
                // Roof.
                Hang(solution, "V8", "Thorns_3"), Hang(solution, "V9", "Thorns_3"), Hang(solution, "V10", "Thorns_3b"), Hang(solution, "V11", "Thorns_3b"),
                new Betrayal("T10 [W]: a cat that leaps to V10 at once meets Slam_10 coming down", "Slam_10", DeathCause.Hazard,
                    Prefix(solution, "Until(on V9)", "leap at once", Release(), ReleaseClimb(), Hold(Right), Jump(), For(28), Hold(R.Down), Until(Dead())), revealedBy: "Slam_10"),
                new Betrayal("T11 [BAIT]: a cat that walks on under the roof is slammed by Slam_3", "Slam_3", DeathCause.Hazard,
                    Prefix(solution, "Until(X>=18.5)", "walk on", Until(Dead())), revealedBy: "Slam_3"),
                new Betrayal("T12 [NW]: a cat that stops at Spikes_5 meets Hinge_5's spikes", "Hinge_5", DeathCause.Hazard,
                    Prefix(solution, "Until(X>=25.3)", "stop", Release(), Until(Dead())), revealedBy: "Hinge_5"),
                // Dead ends.
                new Betrayal("Dead end D1 [OL]: a cat that rests on the Perch between V9 and V10 meets Spikes_P", "Spikes_P", DeathCause.Hazard,
                    Prefix(solution, "Until(on V9)", "the perch", Release(), ReleaseClimb(), Hold(Right), Jump(), For(10), Release(), Until(Dead()))),
                new Betrayal("Dead end D2 [LW]: a cat that climbs V_Lure over Spikes_5 falls onto them when it snaps", "Spikes_5", DeathCause.Hazard,
                    Prefix(solution, "Until(X>=25.3)", "the vine over the spikes", Jump(), Hold(R.Up), Until(Climbing()), Release(), Until(Dead())), revealedBy: "V_Lure"));
        }
    }
}
