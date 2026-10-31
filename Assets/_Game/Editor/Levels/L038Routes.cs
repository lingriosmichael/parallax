using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L038Layout;

namespace Parallax.Editor.Levels
{
    // D-269: level 38's routes ("Compactor"). Everything is set off by the cat, so the solution simply keeps going: run, jump the
    // pits, hold the stick back through each inverter, and press on into Press_4 until it lifts. Betrayals branch off it.
    static class L038Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        static RouteStep[] JumpPit(float x0) => new[] { Until(XAtLeast(x0 - .6f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()) };
        static RouteStep[] Invert(string inv, int ticks = L.InvTicks) => new[] { Until(Fired(inv)), Hold(Left), Until(Since(inv, ticks)), Hold(Right) };

        // ---- Section 0: the cave-in ----
        static RouteStep[] CaveIn() => new RouteStep[] { Hold(Right) }
            .Concat(JumpPit(L.Pit0aX0)).Concat(Invert("Inv_0", L.Inv0Ticks)).Concat(JumpPit(L.Pit0bX0)).Concat(JumpPit(L.Pit0cX0))
            .Concat(new[] { Until(XAtLeast(L.Checkpoint1X - .3f)) })
            .ToArray();

        // ---- Section 1: the closing hall ----
        static RouteStep[] Closing() => new RouteStep[] { Release(), Until(Still()), Hold(Right).Timed(TimedMode.Shift) }
            .Concat(JumpPit(L.PitA0)).Concat(Invert("Inv_D")).Concat(Invert("Inv_F"))
            .Concat(new[] { Until(RoomComplete()) })
            .ToArray();

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L038Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { CaveIn(), Closing() };
            var solution = new Route("L038 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int S0 = 0, S1 = 1;
            RouteStep[] s0 = parts[S0], s1 = parts[S1];
            return new RoomRoutes(solution,
                new Betrayal("Cave_3 [NW]: a cat that waits at the start is caught by the cave-in", "Cave_3", DeathCause.Hazard,
                    B("wait", S0, 0, Until(Dead()))),
                new Betrayal("Press_1 [S]: a cat that stops for Press_1 is caught by the cave-in", "Cave_4", DeathCause.Hazard,
                    B("stop", S0, 1, Until(XAtLeast(L.ColX(L.PressCols[0]) - 1.5f)), Release(), Until(Dead()))),
                new Betrayal("Inv_0 [B]: a cat that keeps pressing on as Inv_0 turns the stick round comes to Press_3 as it shuts", "Press_3", DeathCause.Hazard,
                    B("keep pressing", S0, After(s0, "Until(Fired(Inv_0))"), new[] { Until(Since("Inv_0", L.Inv0Ticks)) }.Concat(JumpPit(L.Pit0bX0)).Concat(JumpPit(L.Pit0cX0)).Append(Until(Dead())).ToArray()), revealedBy: "Inv_0"),
                new Betrayal("Press_3 [S]: a cat that stops under Press_3, which seems to have stopped half way, is crushed as it shuts", "Press_3", DeathCause.Hazard,
                    B("stop", S0, After(s0, "Hold(Right)", 2), Until(XAtLeast(L.ColX(L.PressCols[2]) + .8f)), Release(), Until(Dead()))),
                new Betrayal("Lid [NW]: a cat that waits in the closing hall is crushed under the Lid", "Lid", DeathCause.Hazard,
                    B("wait", S1, After(s1, "Until(Grounded)", 1), Release(), Until(Dead()))),
                new Betrayal("Inv_D [B]: a cat that keeps pressing on as Inv_D turns the stick round is crushed under the Lid", "Lid", DeathCause.Hazard,
                    B("keep pressing", S1, After(s1, "Until(Fired(Inv_D))"), Until(Dead())), revealedBy: "Inv_D"),
                new Betrayal("Inv_F [B]: a cat that keeps pressing on as Inv_F turns the stick round is crushed under the Lid", "Lid", DeathCause.Hazard,
                    B("keep pressing", S1, After(s1, "Until(Fired(Inv_F))"), Until(Dead())), revealedBy: "Inv_F"));
        }
    }
}
