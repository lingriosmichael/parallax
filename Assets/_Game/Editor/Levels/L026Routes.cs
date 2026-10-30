using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Levels
{
    // D-220: level 26's routes ("Wrong Way", the plate floors). Written with the layout and not yet replayed (the developer asked
    // for the changes without test runs): re-record before relying on them.
    static class L026Routes
    {
        static RouteCondition Landed() => new("landed", v => v.Last.Grounded);
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static RouteStep[] To(int dir, float x) => new[] { Hold(dir), dir > 0 ? Until(XAtLeast(x)) : Until(XAtMost(x)), Release(), Until(Still()) };

        static RouteStep[] Solution() => new RouteStep[][] {
            // F1: east, away from Plate_S; onto Plate_1; on over Hatch_1 as it drops, down onto F2.
            To(Right, 26.6f), new[] { Until(Fired("Hatch_1")), Hold(Right), Until(XAtLeast(34f)), Release(), Until(Landed()), Until(Still()) },
            // F2: west onto Plate_2A; through Gate_2 while it's up; onto Plate_2B; to Hatch_2 while it's open, down onto F3.
            To(Left, 29f), new[] { Until(Fired("Gate_2")), Until(Since("Gate_2", 15)) }, To(Left, 18.6f),
            // D-221: over Plate_2T (it would open Trap_2 under the way on).
            new[] { Hold(Left), Jump(), Until(Airborne()), Until(Landed()), Until(Fired("Hatch_2")), Hold(Left), Until(XAtMost(9f)), Release(), Until(Landed()), Until(Still()) },
            // F3: east into Flip_U, onto the ceiling; along it over Plate_C1, up through Hatch_3 onto F2's ceiling.
            new[] { Hold(Right), Until(GravityUp()), Until(Landed()), Until(XAtLeast(26f)), Release(), Until(Landed()), Until(Still()) },
            // F2 upside down: east over Plate_C2; on, up through Hatch_4 onto F1's ceiling.
            new[] { Hold(Right), Until(XAtLeast(30f)), Until(XAtLeast(32f)), Release(), Until(Landed()), Until(Still()) },
            // F1 upside down: west over Plate_D, past Gate_D as it drops, into Flip_D; the door.
            new[] { Hold(Left), Until(GravityDown()), Until(Landed()), Hold(Left), Until(RoomComplete()) },
        }.SelectMany(p => p).ToArray();

        public static RoomRoutes Build() => new(new Route("L026 solution", Solution()));
    }
}
