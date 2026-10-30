using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-244: level 29's routes ("Trapdoor Tower"). On each floor the cat walks from where it landed to the way on and lets go of the
    // stick at its near edge, so it drops through and lands under it; on the ground, east to the door. Written with the layout and
    // not yet replayed (the developer asked for the changes without test runs): re-record before relying on them.
    static class L029Routes
    {
        static RouteStep[] Floor(float from, float slot)
        {
            float centre = slot + L029Layout.SlotW * .5f;
            return centre > from
                ? new[] { Hold(Right), Until(XAtLeast(slot + .2f)), Release(), Until(Airborne()), Until(Grounded()), Until(Still()) }
                : new[] { Hold(Left), Until(XAtMost(slot + L029Layout.SlotW - .2f)), Release(), Until(Airborne()), Until(Grounded()), Until(Still()) };
        }

        public static RoomRoutes Build()
        {
            var steps = new List<RouteStep>();
            float x = L029Layout.SpawnX;
            foreach (var (slots, way, _) in L029Layout.Floors)
            {
                steps.AddRange(Floor(x, slots[way]));
                x = slots[way] + L029Layout.SlotW * .5f;
            }
            // D-245/D-246: on the ground, west, jumping Door_Trap, to the door.
            steps.AddRange(new[] { Hold(Left), Until(XAtMost(L029Layout.PitX1 + .4f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()) });
            return new RoomRoutes(new Route("L029 solution", steps.ToArray()));
        }
    }
}
