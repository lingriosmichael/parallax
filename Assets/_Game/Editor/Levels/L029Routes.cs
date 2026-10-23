using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-174: level 29's routes ("Stormfront"). Waits are on the layout's clocks: the cloud's strikes (from its wake: the first
    // charge FirstStrike ticks after, then one every StrikePeriod, each a 25-tick tell and a 6-tick strike), Slam_2's beat, the
    // lifts' poses and elements' fire ticks. Betrayals branch off the solution's own steps.
    static class L029Routes
    {
        const int Tell = StormCloudMath.DefaultTellTicks, Strike = StormCloudMath.DefaultStrikeTicks;

        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition On(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // The cloud's latest strike ended at least `ticks` ago (and the next charge hasn't begun).
        static RouteCondition StruckAgo(int ticks) => new($"Cloud struck {ticks} ago", v =>
        {
            int i = v.Result.Elements.IndexOf("Cloud");
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int since = v.Last.RoomLifeTick - v.Last.FireTick[i] - L029Layout.FirstStrike;
            if (since < 0) return false;
            int inCycle = since % L029Layout.StrikePeriod;
            return inCycle >= Tell + Strike + ticks;
        });

        // ---- Bridge, storey 1, walked east ----
        // Over the bridge at a run onto the far bank; stop short of the strike ahead and let it go; jump Pop_1 as it springs; onto
        // Lift_1 once it's home, and up.
        static RouteStep[] Bridge() => new[] {
            Hold(Right), Until(XAtLeast(16.8f)), Release(), Until(Still()), Until(StruckAgo(0)),
            Hold(Right).Timed(TimedMode.Shift), Until(Fired("Pop_1")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(29.6f)), Release(), Until(Still()), Until(Home("Lift_1")), Hold(Right), Until(XAtLeast(31.1f)), Release(), Until(Still()),
            Until(Moving("Lift_1")), Until(Stopped("Lift_1")),
        };

        // ---- Gallery, storey 2, walked west ----
        // The storm follows from behind: straight off the lift and over Bridge 2's planks without stopping; jump Pop_2 as it
        // springs; stop short of Slam_2 and go through as it goes back up; onto Lift_2 once it's home, and up.
        static RouteStep[] Gallery() => new[] {
            Hold(Left), Until(Fired("Pop_2")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(6.6f)), Release(), Until(Still()), Until(On(L029Layout.Beat, L029Layout.Slam2 + 33)), Hold(Left).Timed(TimedMode.Shift),
            Until(XAtMost(3.4f)), Release(), Until(Still()), Until(Home("Lift_2")), Hold(Left), Until(XAtMost(1.9f)), Release(), Until(Still()),
            Until(Moving("Lift_2")), Until(Stopped("Lift_2")),
        };

        // ---- Crown, storey 3, walked east ----
        // Straight off the lift and over Bridge 3's planks ahead of the storm; cross Slam_D's cut and stop short, hop onto it, into
        // the door.
        static RouteStep[] Crown() => new[] {
            Hold(Right), Until(XAtLeast(25.4f)), Release(), Until(Still()), Until(Stopped("Slam_D")),
            Hold(Right), Jump(), Until(RoomComplete()),
        };

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L029Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Bridge(), Gallery(), Crown() };
            var solution = new Route("L029 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int Br = 0, G = 1, C = 2;
            RouteStep[] bridge = parts[Br], gallery = parts[G], crown = parts[C];

            return new RoomRoutes(solution,
                // Bridge.
                new Betrayal("T1 [NW]: a cat that stops on the bridge goes down with it into the lake", "Pit_1", DeathCause.Hazard,
                    B("stop on the bridge", Br, 1, Until(XAtLeast(11.5f)), Release(), Until(Dead())), revealedBy: "Bridge1_1"),
                new Betrayal("T2 [W]: a cat that runs on off the bridge is struck by the cloud", "Cloud", DeathCause.Hazard,
                    B("run on", Br, After(bridge, "Until(X>=16.8)") - 1, Until(Dead()))),
                new Betrayal("T3 [J]: a cat that runs on is caught by Pop_1", "Pop_1", DeathCause.Hazard,
                    B("run on", Br, After(bridge, "Until(Fired(Pop_1))"), Until(Dead()))),
                // Gallery.
                new Betrayal("T4 [NW]: a cat that waits where the lift leaves it is struck by the storm", "Cloud", DeathCause.Hazard,
                    B("wait", G, 0, Until(Dead()))),
                new Betrayal("T5 [NW]: a cat that stops on Bridge 2 goes down with it onto Spikes_2", "Spikes_2", DeathCause.Hazard,
                    B("stop on the bridge", G, 1, Until(XAtMost(21.25f)), Release(), Until(Dead())), revealedBy: "Plank2_3"),
                new Betrayal("T6 [J]: a cat that runs on is caught by Pop_2", "Pop_2", DeathCause.Hazard,
                    B("run on", G, After(gallery, "Until(Fired(Pop_2))"), Until(Dead()))),
                new Betrayal("T7 [W]: a cat that goes before Slam_2 is back up is crushed", "Slam_2", DeathCause.Hazard,
                    B("go now", G, After(gallery, "Until(Still)"), Until(On(L029Layout.Beat, L029Layout.Slam2 - 12)), Hold(Left), Until(Dead()))),
                // Crown.
                new Betrayal("T8 [NW]: a cat that waits where the lift leaves it is struck by the storm", "Cloud", DeathCause.Hazard,
                    B("wait", C, 0, Until(Dead()))),
                new Betrayal("T9 [NW]: a cat that stops on Bridge 3 goes down with it onto Spikes_3", "Spikes_3", DeathCause.Hazard,
                    B("stop on the bridge", C, 1, Until(XAtLeast(10.25f)), Release(), Until(Dead())), revealedBy: "Plank3_3"),
                new Betrayal("T10 [W]: a cat that runs on is crushed by Slam_D", "Slam_D", DeathCause.Hazard,
                    B("run on", C, After(crown, "Until(X>=25.4)") - 1, Until(Dead()))));
        }
    }
}
