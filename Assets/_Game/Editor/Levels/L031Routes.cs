using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEngine;
using static Parallax.Editor.Routes.R;
using L = Parallax.Editor.Levels.L031Layout;

namespace Parallax.Editor.Levels
{
    // D-262: level 31's routes ("Firewall"). Every gate, stone and eye is periodic from the room's start, so a wait is on the
    // room tick: Clear(...) is true when a run from where the cat stands crosses the named gates while none of them is lethal (a
    // few ticks to spare each side). Betrayals branch off the solution's own steps.
    static class L031Routes
    {
        const float Run = .12f, Half = .5f;
        const int Spare = 4;

        // A pulse gate's timing: x, phase, period, lethal ticks (after its GateWarn).
        readonly struct G
        {
            public readonly float X; public readonly int Phase, Period, On;
            public G(float x, int phase, int period, int on) { X = x; Phase = phase; Period = period; On = on; }
            public bool Lethal(int t) { if (t < Phase) return false; int k = (t - Phase) % Period; return k >= L.GateWarn && k < L.GateWarn + On; }
        }
        static readonly G Gate1 = new(L.Gate1X, L.Gate1Phase, L.Beat1, L.Gate1On), Gate2 = new(L.Gate2X, L.Gate2Phase, L.Beat1, L.Gate2On);
        static readonly G Gate3 = new(L.Gate3X, L.Gate3Phase, L.PocketBeat, L.PocketOn), Gate4 = new(L.Gate4X, L.Gate4Phase, L.PocketBeat, L.PocketOn);
        static readonly G Gate5 = new(L.Gate5X, L.Gate5Phase, L.Beat5, L.Gate5On), Gate6 = new(L.Gate6X, L.Gate6Phase, L.Beat2, L.Gate6On);
        static readonly G Gate7 = new(L.Gate7X, L.Gate7Phase, L.Gate7Beat, L.Gate7On);

        // A run from the cat's x now, at run speed, crosses every gate given while it's harmless (Spare ticks each side).
        static RouteCondition Clear(string label, params G[] gates) => new($"clear {label}", v =>
        {
            int now = v.Last.RoomLifeTick; float x = v.Last.X;
            foreach (G g in gates)
            {
                int a = Mathf.FloorToInt((g.X - L.GateW * .5f - Half - x) / Run) - Spare, b = Mathf.CeilToInt((g.X + L.GateW * .5f + Half - x) / Run) + 3 + Spare;
                for (int t = now + 1 + Math.Max(a, 0); t <= now + 1 + b; t++) if (g.Lethal(t)) return false;
            }
            return true;
        });
        // The opposite: a run from here meets the gate while it's lethal (a betrayal's go).
        static RouteCondition Blocked(G g) => new($"blocked {g.X}", v =>
        {
            int now = v.Last.RoomLifeTick; float x = v.Last.X;
            int a = Mathf.FloorToInt((g.X - L.GateW * .5f - Half - x) / Run), b = Mathf.CeilToInt((g.X + L.GateW * .5f + Half - x) / Run);
            for (int t = now + 1 + Math.Max(a, 0); t <= now + 1 + b; t++) if (g.Lethal(t)) return true;
            return false;
        });
        // v2: Trident_1 fires at room tick t; a run from the cat's x now crosses its fan (where its beams meet a cat on the floor,
        // x FanX0-FanX1) while it's quiet.
        const float FanX0 = L.TridentX - 3.2f, FanX1 = L.TridentX + 3.2f;
        static bool FanLethal(int t) { if (t < L.TridentPhase) return false; int k = (t - L.TridentPhase) % L.Beat5; return k >= L.TridentWarn && k < L.TridentWarn + L.TridentFire; }
        static bool FanQuiet(RouteView v)
        {
            int now = v.Last.RoomLifeTick; float x = v.Last.X;
            int a = Mathf.FloorToInt((FanX0 - x) / Run) - Spare, b = Mathf.CeilToInt((FanX1 - x) / Run) + 3 + Spare;
            for (int t = now + 1 + Math.Max(a, 0); t <= now + 1 + b; t++) if (FanLethal(t)) return false;
            return true;
        }
        static RouteCondition And(string label, RouteCondition a, RouteCondition b) => new(label, v => a.Test(v) && b.Test(v));
        static RouteCondition FanClear() => new("Trident_1 quiet", FanQuiet);

        // D-276 amendment 2: the chaser's state, read from the room (its pose depends on where the cat has been).
        static Parallax.Gameplay.Rooms.ChaserTrap Chaser() => UnityEngine.Object.FindFirstObjectByType<Parallax.Gameplay.Rooms.ChaserTrap>();
        static RouteCondition ChaserPast(float x) => new($"Purge_1 past {x}", v => Chaser() is { } c && c.X >= x);
        // Its tilt off upright (degrees; + its top forward, its lower beam back).
        static RouteCondition Tilt(string label, Func<float, bool> test) => new($"Purge_1 {label}", v => Chaser() is { } c && test(90f - c.Angle));

        // A phase stone is lit at room tick t.
        static bool Lit(int t, int on, int period, int phase) => t < phase || (t - phase) % period < on - L.StoneWarn;
        // The room tick is `tick` past a multiple of `period`.
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        // Stone_1 is lit from `from` to `to` ticks from now.
        static RouteCondition StoneLit(int from, int to) => new($"Stone_1 lit +{from}..{to}", v =>
        {
            for (int t = v.Last.RoomLifeTick + from; t <= v.Last.RoomLifeTick + to; t++) if (!Lit(t, L.Stone1On, L.StonePeriod, L.Stone1Phase)) return false;
            return true;
        });

        // ---- Hall 1 ----
        static RouteStep[] Hall1() => new[] {
            // Gate_1 is on as a running cat gets there: stop short and go as it's off.
            Hold(Right), Until(XAtLeast(L.Gate1X - 2.4f)), Release(), Until(Still()),
            Until(Clear("Gate_1", Gate1)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Gate1X + .75f)), Release(), Until(Still()),
            // Wait in the gap past Gate_1, out of Eye_1's sight, and run through to the pit's edge.
            Until(Clear("Gate_2", Gate2)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Gate2X + 1.3f)), Release(), Until(Still()),
            // Onto Stone_1 while it's lit, and off it at once.
            Until(StoneLit(10, 40)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Pit1X0 - .55f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast((L.Pit1X0 + L.Pit1X1) * .5f + .2f)), Jump(), Until(Airborne()), Until(Grounded()),
            // The pocket: in while Gate_3 is off, hold still, out while Gate_4 is off.
            Until(XAtLeast(L.Gate3X - 2f)), Release(), Until(Still()),
            // v2: stop near Gate_3 (by Gate_4 the floor is a trapdoor).
            Until(Clear("Gate_3", Gate3)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.PocketStopX - .3f)), Release(), Until(Still()),
            // v2: out over the trapdoor (it gives way only after the cat is off it) to WaitX5, clear of Trident_1's fan.
            Until(Clear("Gate_4", Gate4)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.WaitX5 - .4f)), Release(), Until(Still()),
            // The glass bridge: run Trident_1's fan and Gate_5 together, and the bridge without a stop.
            Until(And("clear Gate_5 and Trident_1", Clear("Gate_5", Gate5), FanClear())), Hold(Right).Timed(TimedMode.Shift),
            Until(XAtLeast(L.Checkpoint1X - .3f)),
        };

        // ---- Hall 2 ----
        static RouteStep[] Hall2() => new[] {
            // Wait for the running light under Table_0, out of the swing.
            Until(XAtLeast(L.Pit3X0 - 1.3f)), Release(), Until(Still()),
            // The running light: onto Turn_1 as it has turned back, and hop on at once from each to the next.
            Until(Beat(L.Beat2, L.TurnPhaseOf(0) + 60)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Pit3X0 - .4f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(L.TurnX[0] + .3f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(L.TurnX[1] + .3f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(L.TurnX[2] + .3f)), Jump(), Until(Airborne()), Until(XAtLeast(L.Pit3X1 + .3f)), Release(), Until(Grounded()), Until(Still()),
            // Under Table_1 for Gate_6, then on to the next checkpoint.
            Hold(Right), Until(XAtLeast(L.Table1[0] + .9f)), Release(), Until(Still()),
            Until(Clear("Gate_6", Gate6)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(L.Checkpoint2X - .3f)),
        };

        // ---- Hall 3: the overtake ----
        static RouteStep[] Hall3() => new[] {
            // Into the Tunnel; Purge_1 surges past overhead and parks, swinging, over Table_P.
            Until(XAtLeast(L.Tunnel[0] + 4f)), Release(), Until(Still()),
            Until(ChaserPast(L.ParkX)), Hold(Right), Until(XAtLeast(L.ParkX - 5.5f)), Release(), Until(Still()),
            // Under Table_P as its lower beam swings forward (away), out the far side as it swings back.
            Until(Tilt("forward", t => t <= -40f)), Hold(Right), Until(XAtLeast(L.ParkX - .1f)), Release(), Until(Still()),
            Until(Tilt("back", t => t >= 40f)), Hold(Right), Until(XAtLeast(L.RetreatX + .4f)), Release(), Until(Still()),
            // The door slides back past Gate_7: time it (from under Table_D).
            Until(Clear("Gate_7", Gate7)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()),
        };

        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        // The index of the step after the `n`th (1-based) step of `part` whose label starts with `label`.
        static int After(RouteStep[] part, string label, int n = 1)
        {
            for (int i = 0, seen = 0; i < part.Length; i++)
                if (part[i].Label.StartsWith(label, StringComparison.Ordinal) && ++seen == n) return i + 1;
            throw new InvalidOperationException($"L031Routes: no step {n} labelled '{label}'.");
        }

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Hall1(), Hall2(), Hall3() };
            var solution = new Route("L031 solution", parts.SelectMany(p => p).ToArray());
            Route B(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));
            const int H1 = 0, H2 = 1, H3 = 2;
            RouteStep[] h1 = parts[H1], h2 = parts[H2], h3 = parts[H3];
            int Step(RouteStep[] part, string label, int n = 1) => After(part, label, n) - 1;
            return new RoomRoutes(solution,
                // ---- Hall 1 ----
                new Betrayal("Gate_1 [W]: a cat that runs straight at Gate_1 is caught in it", "Gate_1", DeathCause.Hazard,
                    B("run at it", H1, 0, Hold(Right), Until(Dead()))),
                new Betrayal("Gate_2 [NW]: a cat that runs on from Gate_1 without a stop meets Gate_2 on", "Gate_2", DeathCause.Hazard,
                    B("run on", H1, After(h1, "Hold(Right)", 2), Until(Dead()))),
                new Betrayal("Eye_1 [OL]: a cat that waits for Gate_2 right in front of it is shot by Eye_1", "Eye_1", DeathCause.Hazard,
                    B("wait at Gate_2", H1, After(h1, "Until(Still)", 2), Hold(Right), Until(XAtLeast(L.Gate2X - 2f)), Release(), Until(Dead()))),
                new Betrayal("Stone_1 [NW]: a cat that stays on Stone_1 falls as it goes out", "Pit_1_Laser", DeathCause.Hazard,
                    B("stay on it", H1, After(h1, "Until(Grounded)", 1), Release(), Until(Dead())), revealedBy: "Stone_1"),
                new Betrayal("Gate_3 [W]: a cat that steps into the pocket while Gate_3 is on is caught", "Gate_3", DeathCause.Hazard,
                    B("go now", H1, Step(h1, "Until(clear Gate_3)"), Until(Blocked(Gate3)), Hold(Right), Until(Dead()))),
                new Betrayal("Gate_4 [SS]: a cat that runs straight through the pocket meets Gate_4 on", "Gate_4", DeathCause.Hazard,
                    B("run through", H1, After(h1, "Until(clear Gate_3)"), Hold(Right), Until(Dead()))),
                // v2's trolls.
                new Betrayal("Trap_P [T]: a cat that waits in the pocket by Gate_4 drops through the trapdoor", "Pit_P_Laser", DeathCause.Hazard,
                    B("stop by Gate_4", H1, After(h1, "Until(clear Gate_3)"), Hold(Right), Until(XAtLeast(L.TrapX0 + .5f)), Release(), Until(Dead())), revealedBy: "Trap_P"),
                new Betrayal("Trident_1 [OL]: a cat that waits for Gate_5 at the bridge's foot is shot by Trident_1", "Trident_1", DeathCause.Hazard,
                    B("wait at the foot", H1, After(h1, "Until(clear Gate_4)"), Hold(Right), Until(XAtLeast(L.TridentX - .3f)), Release(), Until(Dead()))),
                new Betrayal("Glass [NW]: a cat that stops on the glass falls through it", "Pit_2_Laser", DeathCause.Hazard,
                    B("stop on it", H1, After(h1, "Until(clear Gate_5") + 1, Until(XAtLeast(L.Glass0 + .6f)), Release(), Until(Dead())), revealedBy: "Glass_1"),
                new Betrayal("Purge_1 [NW]: a cat that waits too long before the bridge is purged", "Purge_1", DeathCause.Hazard,
                    B("wait", H1, After(h1, "Until(Still)", 6), Until(Dead()))),
                // ---- Hall 2 ----
                new Betrayal("Turn_1 [W]: a cat that jumps for Turn_1 late lands as it turns over", "Turn_1", DeathCause.Hazard,
                    B("jump late", H2, Step(h2, "Until(beat"), Until(Beat(L.Beat2, L.TurnPhaseOf(0) + 100)), Hold(Right), Until(XAtLeast(L.Pit3X0 - .4f)), Jump(), Until(Airborne()),
                        Until(XAtLeast(L.TurnX[0] - .2f)), Release(), Until(Dead()))),
                new Betrayal("Turn_2 [NW]: a cat that waits on Turn_1 is spiked as it turns over", "Turn_1", DeathCause.Hazard,
                    B("wait on it", H2, After(h2, "Until(Grounded)", 1), Release(), Until(Dead()))),
                new Betrayal("Gate_6 [W]: a cat that runs on from the landing meets Gate_6 on", "Gate_6", DeathCause.Hazard,
                    B("run on", H2, Step(h2, "Until(clear Gate_6)"), Hold(Right), Until(Dead()))),
                // ---- Hall 3 ----
                new Betrayal("Purge_1 [W]: a cat that runs on out of the Tunnel before the laser has passed is caught", "Purge_1", DeathCause.Hazard,
                    B("run on", H3, After(h3, "Until(Still)", 1), Hold(Right), Until(Dead()))),
                new Betrayal("Purge_1 [NW]: a cat that goes for Table_P as the laser swings back is swept", "Purge_1", DeathCause.Hazard,
                    B("go now", H3, After(h3, "Until(Still)", 2), Until(Tilt("back", t => t >= 40f)), Hold(Right), Until(Dead()))),
                new Betrayal("Retreat [T]: a cat that runs on as the door slides back meets Gate_7", "Gate_7", DeathCause.Hazard,
                    B("run on", H3, After(h3, "Until(Still)", 3), Until(And("back, Gate_7 on", Tilt("back", t => t >= 40f), Blocked(Gate7))), Hold(Right), Until(Dead()))));
        }
    }
}
