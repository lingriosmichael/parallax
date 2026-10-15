using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-147): level 17's routes ("Deja Vu"). Every wait is on a chain the cat set off (an element's fire tick) or on a
    // moving trap's own motion. On lane 2 left and right are swapped: Hold(Right) walks the cat west.
    static class L017Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // An arrow's shot as drawn has just passed the cat (its trailing end within 2 u beyond the cat's far side; the cat is 1 u
        // wide). `dir` is the shot's direction (+1 east, -1 west). A shot stuck in its kerb is never "just past".
        static RouteCondition ShotPast(string arrow, int dir) => new($"{arrow} past", v =>
        {
            int i = v.Result.Elements.IndexOf(arrow);
            if (i < 0 || !v.Last.Rendered[i]) return false;
            // The drawn bounds hold the launcher and the shot: the shot's tail is 0.8 behind its tip, the bounds' far end.
            UnityEngine.Rect b = v.Last.RenderBounds[i];
            float gap = dir > 0 ? b.xMax - .8f - (v.Last.X + .5f) : (v.Last.X - .5f) - (b.xMin + .8f);
            return gap > 0f && gap <= 2f;
        });

        // Out: run the crumbling bridge; stop as Slam_1 comes down, hop onto it; over Kerb_1 once a shot has hit it; wait short of
        // Hinge_1 for the next shot to pass over, then run and jump Spikes_1 ahead of the hinge; under the Spine into well 1; up
        // V1-V4 as each sticks; onto lane 2.
        static RouteStep[] Out() => new[] {
            Hold(Right), Until(Fired("Slam_1")), Release(), Until(Still()), Until(Stopped("Slam_1")),
            Hold(Right), Jump(), Until(XAtLeast(13.4f)), Release(), Until(GroundedOn("Slam_1")),
            Until(Since("Arrow_1", 47)), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(20.5f)), Release(), Until(Still()), Until(ShotPast("Arrow_1", -1)),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(22.4f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(28.2f)), Release(), Until(Still()), Until(Since("V1", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Right), Until(GroundedOn("V1_Shaft")), Release(), Until(Since("V2", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Left), Until(GroundedOn("V2_Shaft")), Release(), Until(Since("V3", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Right), Until(GroundedOn("V3_Shaft")), Release(), Until(Since("V4", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Left), Until(Fired("Inv_2")),
        };

        // Back, swapped: west over the crumbling bridge 2; stop as Slam_2 comes down, hop onto it and over Kerb_2; wait short of
        // Hinge_2 for a shot to pass over, then run and jump Spikes_2; under Spine_2 into well 2; up W1-W4 with the hands swapped.
        static RouteStep[] Back() => new[] {
            Hold(Right), Until(Fired("Slam_2")), Release(), Until(Still()), Until(Stopped("Slam_2")),
            Hold(Right), Jump(), Until(XAtMost(18.6f)), Release(), Until(GroundedOn("Slam_2")),
            Until(Still()), Hold(Right).Timed(TimedMode.Shift), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(11.5f)), Release(), Until(Still()), Until(ShotPast("Arrow_2", 1)),
            Hold(Right).Timed(TimedMode.Shift), Until(XAtMost(9.6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(3.7f)), Release(), Until(Still()), Until(Since("W1", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Right), Until(GroundedOn("W1_Shaft")), Release(), Until(Since("W2", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Left), Until(GroundedOn("W2_Shaft")), Release(), Until(Since("W3", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Right), Until(GroundedOn("W3_Shaft")), Release(), Until(Since("W4", 13)),
            Jump().Timed(TimedMode.Shift), Hold(Left), Until(XAtLeast(3.55f)),
        };

        // Last: Inv_3 swaps the hands (hold left to go on east); jump Pit_3 and swap back as the swap ends in the air; stop as
        // Slam_3 comes down, hop onto it ahead of Push_3; when Spear_D has stuck, onto its shaft and into the door.
        static RouteStep[] Last() => new[] {
            Hold(Right), Until(Fired("Inv_3")), Hold(Left), Until(XAtLeast(10.1f)), Jump().Timed(TimedMode.Shift), Until(Since("Inv_3", 36)), Hold(Right), Until(Grounded()),
            Until(Fired("Slam_3")), Release(), Until(Still()), Until(Stopped("Slam_3")),
            Hold(Right), Jump(), Until(XAtLeast(21.9f)), Release(), Until(GroundedOn("Slam_3")),
            Hold(Right), Until(XAtLeast(23.2f)), Release(), Until(Since("Spear_D", 13)),
            Hold(Right), Jump().Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Out(), Back(), Last() };
            const int OutPart = 0, BackPart = 1, LastPart = 2;
            var solution = new Route("L017 solution", Cat(parts));
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            return new RoomRoutes(solution,
                // Out.
                new Betrayal("T1 [NW]: a cat that stops on the bridge falls with it into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("stop", OutPart, 1, Until(XAtLeast(6.5f)), Release(), Until(Dead())), revealedBy: "Br_2"),
                new Betrayal("Dead end D1 [B]: a cat that turns back west over the falling bridge drops into Pit_1", "Pit_1", DeathCause.Hazard,
                    B("back west", OutPart, 1, Until(XAtLeast(8f)), Hold(Left), Until(Dead())), revealedBy: "Br_2"),
                new Betrayal("T2 [BAIT]: a cat that runs on off the bridge is crushed by Slam_1", "Slam_1", DeathCause.Hazard,
                    B("run on", OutPart, 1, Until(Dead()))),
                new Betrayal("T3 [W]: a cat that runs straight for Spikes_1 meets a shot from the Spine in its jump", "Arrow_1", DeathCause.Hazard,
                    B("run on", OutPart, 15, Until(XAtLeast(22.4f)), Jump(), Until(Dead()))),
                new Betrayal("T4 [NW]: a cat that stops short of Spikes_1 is caught by Hinge_1's spikes", "Hinge_1", DeathCause.Hazard,
                    B("stop", OutPart, 20, Until(XAtLeast(22.8f)), Release(), Until(Dead()))),
                new Betrayal("T5 [NJ]: a cat that jumps for the next step before V1 has stuck is run through", "V1", DeathCause.Hazard,
                    B("jump at once", OutPart, 27, Until(Since("V1", 4)), Jump(), Hold(Right), Until(Dead()))),
                // Back.
                new Betrayal("Dead end D2 [B]: a cat that keeps its old hand on bridge 2 walks back east and drops into Pit_2", "Pit_2", DeathCause.Hazard,
                    B("old hand", BackPart, 1, Until(XAtMost(23f)), Hold(Left), Until(Dead())), revealedBy: "Br2_1"),
                new Betrayal("T6 [SS]: a cat that runs on off bridge 2 is crushed by Slam_2", "Slam_2", DeathCause.Hazard,
                    B("run on", BackPart, 1, Until(Dead()))),
                new Betrayal("T7 [W]: a cat that runs straight for Spikes_2 meets a shot from Spine_2 in its jump", "Arrow_2", DeathCause.Hazard,
                    B("run on", BackPart, 15, Until(XAtMost(9.6f)), Jump(), Until(Dead()))),
                new Betrayal("T8 [NW]: a cat that stops short of Spikes_2 is caught by Hinge_2's spikes", "Hinge_2", DeathCause.Hazard,
                    B("stop", BackPart, 20, Until(XAtMost(9.2f)), Release(), Until(Dead()))),
                new Betrayal("T9 [NJ]: a cat that jumps for the next step before W1 has stuck is run through", "W1", DeathCause.Hazard,
                    B("jump at once", BackPart, 27, Until(Since("W1", 4)), Jump(), Hold(Right), Until(Dead()))),
                // Last.
                new Betrayal("T10 [J]: a cat that keeps the swapped hand when Inv_3 ends in the air falls back into Pit_3", "Pit_3", DeathCause.Hazard,
                    B("keep left", LastPart, 5, Until(Dead())), revealedBy: "Inv_3"),
                new Betrayal("T11 [SS]: a cat that runs on is crushed by Slam_3", "Slam_3", DeathCause.Hazard,
                    B("run on", LastPart, 8, Until(Dead()))),
                new Betrayal("T12 [OL]: a cat that stays on the floor is caught by Push_3's spikes", "Push_3", DeathCause.Hazard,
                    B("stay", LastPart, 12, Until(Dead()))));
        }
    }
}
