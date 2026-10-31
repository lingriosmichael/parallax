using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L040Layout;

namespace Parallax.Editor.Levels
{
    // D-271: level 40's routes ("Freefall"). Run each storey to its hole and drop; wait for each gate on the room tick (Clear(...)),
    // for Hatch_2 to open, and hold the stick back through Inv_3. Betrayals branch off the solution's own steps.
    static class L040Routes
    {
        const float Run = .12f, Half = .5f;
        const int Spare = 4;

        static RouteCondition Below(float y) => new($"Y<={y}", v => v.Last.Y <= y);
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        static bool Lethal(int t, int phase) { if (t < phase) return false; int k = (t - phase) % L.GatePeriod; return k >= L.GateWarn && k < L.GateWarn + L.GateOn; }
        // A run from the cat's x now crosses the gate at gateX while it's harmless (Spare ticks each side).
        static RouteCondition Clear(string label, float gateX, int phase) => new($"clear {label}", v =>
        {
            int now = v.Last.RoomLifeTick; float d = Mathf.Abs(gateX - v.Last.X);
            int a = Mathf.FloorToInt((d - .15f - Half) / Run) - Spare, b = Mathf.CeilToInt((d + .15f + Half) / Run) + 3 + Spare;
            for (int t = now + 1 + Math.Max(a, 0); t <= now + 1 + b; t++) if (Lethal(t, phase)) return false;
            return true;
        });

        // ---- Section 0 ----
        static RouteStep[] Upper() => new[] {
            Hold(Right), Until(XAtLeast(L.Gate0X - 2.4f)), Release(), Until(Still()),
            Until(Clear("Gate_0", L.Gate0X, L.Gate0Phase)), Hold(Right).Timed(TimedMode.Shift), Until(Below(L.F[1] + .5f)), Until(Grounded()),
            // S1: west over the glass and down the open hole.
            Hold(Left), Until(Below(L.F[2] + .5f)), Until(Grounded()),
            // S2: east to Hatch_2, and wait for it under the beam.
            Hold(Right), Until(XAtLeast(L.HoleX0(2) - 1.2f)), Release(), Until(Still()),
            Until(Since("Hatch_2", L.HatchShrink)), Hold(Right).Timed(TimedMode.Shift), Until(Below(L.F[3] + .5f)), Until(Grounded()),
        };

        // ---- Section 1 ----
        static RouteStep[] Lower() => new[] {
            Release(), Until(Still()),
            Hold(Left).Timed(TimedMode.Shift), Until(Fired("Inv_3")), Hold(Right), Until(Since("Inv_3", L.InvTicks)), Hold(Left),
            Until(XAtMost(L.Gate3X + 2.4f)), Release(), Until(Still()),
            Until(Clear("Gate_3", L.Gate3X, L.Gate3Phase)), Hold(Left).Timed(TimedMode.Shift), Until(Below(L.F[4] + .5f)), Until(Grounded()),
            Hold(Right), Until(RoomComplete()),
        };

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L040Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Upper(), Lower() };
            var solution = new Route("L040 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int S0 = 0, S1 = 1;
            RouteStep[] u = parts[S0], w = parts[S1];
            return new RoomRoutes(solution,
                new Betrayal("Gate_0 [W]: a cat that runs straight at Gate_0 is caught in it", "Gate_0", DeathCause.Hazard,
                    B("run at it", S0, 0, Hold(Right), Until(Dead()))),
                new Betrayal("Beam [NW]: a cat that waits on S0 is caught by the Beam", "Beam", DeathCause.Hazard,
                    B("wait", S0, After(u, "Until(Still)", 1), Until(Dead()))),
                new Betrayal("Glass [NW]: a cat that stops on the glass falls through it", "Pit_1_Laser", DeathCause.Hazard,
                    B("stop", S0, After(u, "Hold(Left)", 1), Until(XAtMost(L.Glass1X0 + 3f)), Release(), Until(Dead())), revealedBy: "Glass_2"),
                new Betrayal("Hatch_2 [NW]: a cat that stays by Hatch_2 once it has opened is caught by the Beam", "Beam", DeathCause.Hazard,
                    B("stay", S0, After(u, "Until(Hatch_2"), Until(Dead()))),
                new Betrayal("Gate_3 [W]: a cat that runs on into Gate_3 is caught in it", "Gate_3", DeathCause.Hazard,
                    B("run on", S1, After(w, "Hold(Left)", 2), Until(Dead()))),
                new Betrayal("Beam [NW3]: a cat that waits on S3 is caught by the Beam", "Beam", DeathCause.Hazard,
                    B("wait", S1, 2, Until(Dead()))));
        }
    }
}
