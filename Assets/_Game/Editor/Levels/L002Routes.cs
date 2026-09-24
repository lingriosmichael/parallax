using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // PAX-075 (D-079): L002's anatomy. The Lift's slack is a margin (a late landing is still carried);
    // the ReceiverBlock escape is D-056 (1)'s hesitation window (R10 stops below 12). Block_A kills no
    // replayed route (take-offs 25.2-28.4, waits 0-30, no brake), so it has no betrayal route (D-079 limits).
    static class L002Routes
    {
        public static RoomRoutes Build()
        {
            var solution = new Route("L002 solution",
                Margin("Lift landing to Lift fire", GroundedOn("Lift"), Fired("Lift"), 12),
                Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Platform_B")),
                Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("Floor_C")),
                Until(GroundedOn("Lift")), Until(GroundedOn("Receiver")),
                Until(XAtLeast(21.4f)), Jump().Timed(TimedMode.Hesitate), Until(Airborne()), Until(XAtLeast(24.6f)), Release(), Until(GroundedOn("Floor_DLeft")),
                // Past both triggers, clear of the Sweep's reach: wait for Block_A to land and the Sweep to return.
                Hold(Right), Until(XAtLeast(26.8f)), Release(), Until(Still()),
                Until(Stopped("Block_A")), Until(Home("Sweep")),
                // PAX-082 (D-082): the lower jump takes off later (28.5) for the shorter crossing.
                Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(28.5f)), Jump(), Until(XAtLeast(30.6f)), Release(), Until(Grounded()),
                Hold(Right), Until(RoomComplete()));

            return new RoomRoutes(solution,
                new Betrayal("ReceiverBlock crushes a cat that lingers on the Receiver", "ReceiverBlock", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Receiver))", "linger on the Receiver", Release(), Until(Dead()))),
                new Betrayal("Collapse_C drops a cat that steps down onto it", "Pit1_Hazard", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Receiver))", "step onto Collapse_C", Until(XAtLeast(22.8f)), Release(), Until(Dead())),
                    revealedBy: "Collapse_C"),
                new Betrayal("The Sweep hits a cat that runs on", "Sweep", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(GroundedOn(Floor_DLeft))", "run into the Sweep", Hold(Right), Until(Dead()))));
        }
    }
}
