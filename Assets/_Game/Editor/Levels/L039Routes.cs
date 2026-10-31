using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L039Layout;

namespace Parallax.Editor.Levels
{
    // D-270: level 39's routes ("Barrage"). In a shaft: on each step, wait until the next bar has stuck (Stuck(...)), then jump
    // up and across to it. In the corridor: run on, and jump Low as its tip
    // comes within reach of the cat (Coming(...)); never jump for High.
    static class L039Routes
    {
        // At least `ticks` room ticks since the element last fired.
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });
        // A barrage bar's tip is within `reach` behind the cat's back.
        static RouteCondition Coming(string bar, float reach) => new($"{bar} within {reach}", v =>
        {
            int i = v.Result.Elements.IndexOf(bar);
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            float tip = L.ShaftA0 + L.BarLen + (v.Last.RoomLifeTick - v.Last.FireTick[i] - L.BarTell) * L.BarSpeed;
            return tip >= v.Last.X - .5f - reach;
        });
        // A step bar has stuck (its tell and its flight across the shaft over, with ticks to spare).
        static RouteCondition Stuck(string bar) => Since(bar, L.StepTell + Mathf.CeilToInt((L.ShaftW - L.BarLen) / L.BarSpeed) + 3);

        // Up and across a shaft (x0-x1) onto the step on the other side.
        static RouteStep[] Across(bool east, float x0, float x1) => new[] { Hold(east ? Right : Left).Timed(TimedMode.Shift), Jump(), Until(Airborne()),
            Until(east ? XAtLeast(x1 - L.BarLen + .4f) : XAtMost(x0 + L.BarLen - .4f)), Release(), Until(Grounded()) };

        // ---- Section 0 ----
        static RouteStep[] ShaftA()
        {
            var s = new List<RouteStep>();
            for (int k = 1; k <= L.ACount; k++) { s.Add(Until(Stuck($"A{k}"))); s.AddRange(Across(L.AEast(k), L.ShaftA0, L.ShaftA1)); }
            // Up onto the corridor, and the barrage.
            s.AddRange(new[] { Hold(Right), Jump(), Until(Airborne()), Until(Grounded()),
                Until(Coming("Low", 2f)), Jump().Timed(TimedMode.Shift), Until(Airborne()), Until(Grounded()), Until(XAtLeast(L.Checkpoint1X - .3f)) });
            return s.ToArray();
        }

        // ---- Section 1 ----
        static RouteStep[] ShaftB()
        {
            var s = new List<RouteStep> { Release(), Until(Still()),
                Hold(Right), Until(XAtLeast(L.ShaftB1 - L.BarLen - 1.6f)), Jump(), Until(Airborne()), Until(XAtLeast(L.ShaftB1 - L.BarLen + .4f)), Release(), Until(Grounded()) };
            for (int k = 2; k <= L.BCount; k++) { s.Add(Until(Stuck($"B{k}"))); s.AddRange(Across(L.BEast(k), L.ShaftB0, L.ShaftB1)); }
            s.AddRange(new[] { Hold(Left), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()) });
            return s.ToArray();
        }

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L039Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { ShaftA(), ShaftB() };
            var solution = new Route("L039 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int S0 = 0, S1 = 1;
            RouteStep[] s0 = parts[S0], s1 = parts[S1];
            return new RoomRoutes(solution,
                new Betrayal("GlassA [NW]: a cat that waits on Shaft A's floor falls through it", "Laser_A", DeathCause.Hazard,
                    B("wait", S0, 0, Until(Dead())), revealedBy: "GlassA_1"),
                new Betrayal("A2 [J]: a cat that jumps as A2 flies over its head is hit", "A2", DeathCause.Hazard,
                    B("jump", S0, After(s0, "Until(Grounded)", 1), Until(Since("A2", L.StepTell - 10)), Jump(), Until(Dead()))),
                new Betrayal("Low [S]: a cat that runs on without jumping Low is hit", "Low", DeathCause.Hazard,
                    B("run on", S0, After(s0, "Until(Grounded)", L.ACount + 1), Until(Dead()))),
                new Betrayal("High [J]: a cat that jumps High too is hit by it", "High", DeathCause.Hazard,
                    B("jump it", S0, After(s0, "Until(Grounded)", L.ACount + 2), Until(Coming("High", 2f)), Jump(), Until(Dead()))),
                new Betrayal("GlassB [NW]: a cat that waits on Shaft B's floor falls through it", "Laser_B", DeathCause.Hazard,
                    B("wait", S1, 2, Hold(Right), Until(XAtLeast(L.ShaftB0 + 1f)), Release(), Until(Dead())), revealedBy: "GlassB_1"),
                new Betrayal("B3 [J]: a cat that jumps as B3 flies over its head is hit", "B3", DeathCause.Hazard,
                    B("jump", S1, After(s1, "Until(Grounded)", 2), Until(Since("B3", L.StepTell - 10)), Jump(), Until(Dead()))));
        }
    }
}
