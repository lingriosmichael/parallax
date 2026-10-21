using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-159: level 1's routes ("Run!"). The solution runs the bridge as it falls, stops for Slam_1 and passes under it once it's
    // back up, jumps Spikes_H ahead of Hinge_1, stops for Block_1 and hops it, drops to the lower storey, and runs west ahead of
    // the cloud, jumping Crumble and Pop, to leap after the door as its lift carries it over the pit. Every betrayal is a way a
    // normal run meets a trap: stopping where the room says run, running where it says stop.
    static class L001Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        public static RoomRoutes Build()
        {
            var solution = new Route("L001 solution",
                // The bridge: run.
                Hold(Right), Until(GroundedOn("Pier")),
                // Slam_1: stop as it starts down, let it go back up, pass under.
                Until(Fired("Slam_1")), Release(), Until(Still()), Until(Since("Slam_1", 63)),
                // Hinge_1 stands up behind: keep going and jump Spikes_H.
                Hold(Right), Until(XAtLeast(19.8f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                // Block_1: stop as it drops, hop onto it, and run off the slab's end to the lower storey.
                Until(Fired("Block_1")), Release(), Until(Still()), Until(Stopped("Block_1")),
                Hold(Right), Jump(), Until(GroundedOn("Block_1")), Until(GroundedOn("Ground_2")),
                // The lower storey, ahead of the cloud: jump Crumble, jump Pop as it springs, leap after the door.
                Hold(Left), Until(XAtMost(26.8f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(GroundedOn("Ground_1")),
                Until(Fired("Pop")), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()),
                Until(XAtMost(14.8f)), Jump().Timed(TimedMode.Shift), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("T1: the bridge falls under a cat that stops on it", "PitW_Hazard", DeathCause.Hazard,
                    new Route("stop on the bridge", Hold(Right), Until(XAtLeast(3.3f)), Release(), Until(Dead())), revealedBy: "Bridge_2"),
                new Betrayal("T2: Slam_1 comes down on a cat that runs on", "Slam_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Slam_1))", "run on under Slam_1", Until(Dead()))),
                new Betrayal("T3: Hinge_1 shoves a cat that stops short onto Spikes_H", "Spikes_H", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Slam_1 +63)", "stop past the hinge", Hold(Right), Until(Fired("Hinge_1")), Release(), Until(Dead())), revealedBy: "Hinge_1"),
                new Betrayal("T4: Block_1 drops on a cat that runs on", "Block_1", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Block_1))", "run on under Block_1", Until(Dead()))),
                new Betrayal("T5: Crumble gives way under a cat that runs on", "PitC_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2))", "run on west", Hold(Left), Until(Dead())), revealedBy: "Crumble"),
                new Betrayal("T6: Pop springs up under a cat that runs on", "Pop", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(Fired(Pop))", "run on into Pop", Until(Dead()))),
                new Betrayal("T7: the cloud strikes a cat that stops on the lower storey", "Cloud", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Ground_2))", "stay where it lands", Release(), Until(Dead()))),
                new Betrayal("T8: a cat that runs off the edge after the door falls into the pit", "PitD_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X<=14.8)", "run off the edge", Until(Dead())), revealedBy: "Lift_D"),
                Betrayal.Recovers("The door rides its lift away from a cat that comes to it, which leaps after it", "Door", solution));
        }
    }
}
