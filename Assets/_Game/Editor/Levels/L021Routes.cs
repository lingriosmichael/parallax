using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using static Parallax.Editor.Routes.R;
using static Parallax.Editor.Levels.L021Layout;

namespace Parallax.Editor.Levels
{
    // D-200: level 21's routes, on the layout's clocks: the pendulums' and Eye's beats, Stone_C's appearances, the vents' eruptions.
    // Betrayals branch off the solution's own steps.
    static class L021Routes
    {
        // Ticks since the element last fired between `from` and `to`.
        static RouteCondition Window(string element, int from, int to) => new($"{element} +{from}..{to}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            if (i < 0 || v.Last.FireTick[i] < 0) return false;
            int since = v.Last.RoomLifeTick - v.Last.FireTick[i];
            return since >= from && since <= to;
        });
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // The beats the solution goes on (tuned against the dumps).
        // A pendulum's rod is high (over a walking cat) from 0.083 to 0.417 of its period and back; the solution goes as it rises.
        const int SwingAGo = 25, SwingBGo = 15, SpinGo = 0, SafeGo = 55, LieGo = 205;
        // D-203: each turret watches the whole of its floor, on its own beat (EyePeriod). Under a steep beam the cat steps aside as
        // the turret locks and back once it has fired; under a low one it jumps 8 ticks before it fires (a later jump hasn't risen
        // out of the beam); or the solution is already moving, or in the air, as it fires.
        static RouteCondition Locks(int phase) => Beat(EyePeriod, phase + EyeAim);
        static RouteCondition Shot(int phase) => Beat(EyePeriod, phase + EyeShot - 8);
        static RouteCondition Fired(int phase) => Beat(EyePeriod, phase + EyeShot + EyeFire);
        static RouteStep[] Dodge(int phase) => new[] { Until(Shot(phase)), Jump(), Until(Airborne()), Until(Grounded()), Until(Still()) };
        // Aside `away` (Left -1 / Right +1) to `outX` as the turret locks; back to `backX` once it has fired.
        static RouteStep[] Aside(int phase, int away, float outX, float backX) => new[] {
            Until(Locks(phase)), Hold(away), Until(away < 0 ? XAtMost(outX) : XAtLeast(outX)), Release(),
            Until(Fired(phase)), Hold(-away), Until(away < 0 ? XAtLeast(backX) : XAtMost(backX)), Release(), Until(Still()),
        };

        // ---- Ground ----
        // Onto V1 and up; steer east onto storey A.
        static RouteStep[] Ground() => new[] {
            Hold(Right), Until(XAtLeast(V1X)), Release(), Until(YAtLeast(A + .3f)), Hold(Right).Timed(TimedMode.Shift), Until(Grounded()),
        };
        // ---- Storey A, walked east ----
        // Stop short of the Gap, under Eye_A, and step aside from it twice; over the Gap and on under Swing_A while it's swung out
        // (Eye_A fires as the cat runs, and misses); onto V2 and up, steering west onto storey B.
        static readonly RouteStep[] AWait = { Until(XAtLeast(15.8f)), Release(), Until(Still()) };
        static RouteStep[] AAside() => Aside(EyeAPhase, Left, 15.2f, 15.8f);
        static readonly RouteStep[] AGo = {
            Until(Beat(SwingAPeriod, SwingAGo - 45)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(16.6f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtLeast(V2X - .3f)), Release(), Until(Still()), Until(YAtLeast(B + .3f)), Hold(Left).Timed(TimedMode.Shift), Until(Grounded()),
        };
        static RouteStep[] StoreyA() => Cat(AWait, AAside(), AAside(), AGo);
        // ---- Storey B, walked west ----
        // Over Eye_B's beam; under Swing_B while it's swung out (Eye_B fires as the cat walks, and misses) and over Spikes_B; aside
        // from Eye_B, nearly overhead, where the cat lands; from there under Spin_B's arm while it's up; onto V3 and up, steering east.
        static readonly RouteStep[] BWait = { Until(XAtMost(20.6f)), Release(), Until(Still()) };
        static readonly RouteStep[] BGo = {
            Until(Beat(SwingBPeriod, SwingBGo)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(13.7f)), Jump(), Until(Airborne()), Until(Grounded()),
            Until(XAtMost(9.9f)), Release(), Until(Still()),
        };
        // Aside east as Eye_B locks; it goes on from there once Eye_B has fired.
        static readonly RouteStep[] BAside = { Until(Locks(EyeBPhase)), Hold(Right), Until(XAtLeast(10.4f)), Release(), Until(Fired(EyeBPhase)) };
        static readonly RouteStep[] BSpin = {
            Until(Beat(120, SpinGo)), Hold(Left).Timed(TimedMode.Shift), Until(XAtMost(V3X + .3f)), Release(), Until(Still()),
            Until(YAtLeast(C + .3f)), Hold(Right).Timed(TimedMode.Shift), Until(Grounded()),
        };
        static RouteStep[] StoreyB() => Cat(BWait, Dodge(EyeBPhase), BGo, BAside, BSpin);
        // ---- Storey C, walked east ----
        // Over Eye_C's beam; onto Stone_C as it appears and off it before it goes (Eye_C fires as the cat jumps off it); under the
        // top storey until the eruption GD leaves alone, going before Eye_C fires; over Spikes_V4 onto V4 and up, steering west.
        static readonly RouteStep[] CWait = { Until(XAtLeast(15.8f)), Release(), Until(Still()) };
        static readonly RouteStep[] CStone = {
            Until(Window("Stone_C", 0, 2)), Hold(Right), Jump(), Until(XAtLeast(18.4f)), Release(), Until(Grounded()),
        };
        static readonly RouteStep[] COff = {
            Hold(Right), Until(XAtLeast(19.4f)), Jump(), Until(Airborne()), Until(XAtLeast(21.6f)), Release(), Until(Grounded()),
            Hold(Right), Until(XAtLeast(23.4f)), Release(), Until(Still()),
        };
        static readonly RouteStep[] CGo = {
            Until(Beat(GDPeriod, SafeGo)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(24.4f)), Jump(), Until(XAtLeast(V4X - .3f)), Release(), Until(Grounded()),
            Until(YAtLeast(C + .3f)), Hold(Left).Timed(TimedMode.Shift), Until(Grounded()),
        };
        static RouteStep[] StoreyC() => Cat(CWait, Dodge(EyeCPhase), CStone, COff, CGo);
        // ---- Top, walked west ----
        // Ahead of the falling floor onto Ferry (Eye fires as the cat runs under it, and misses); jumping Eye's beam twice on Ferry;
        // off it onto T_W, into the door.
        static readonly RouteStep[] TFerry = { Until(XAtMost(10.3f)), Release(), Until(Still()) };
        static RouteStep[] Top() => Cat(
            TFerry,
            new[] { Until(Shot(EyePhase)), Jump(), Until(Airborne()), Until(Grounded()) },
            new[] { Until(Shot(EyePhase)), Jump(), Until(Airborne()), Until(Grounded()) },
            new[] { Until(Stopped("Ferry")), Hold(Left), Until(RoomComplete()) });

        static RouteStep[] Cat(params RouteStep[][] parts) => parts.SelectMany(p => p).ToArray();
        // The solution's steps before part `part`'s step `index`, then these.
        static RouteStep[] Branch(RouteStep[][] parts, int part, int index, params RouteStep[] then) =>
            parts.Take(part).SelectMany(p => p).Concat(parts[part].Take(index)).Concat(then).ToArray();

        public static RoomRoutes Build()
        {
            RouteStep[][] parts = { Ground(), StoreyA(), StoreyB(), StoreyC(), Top() };
            const int SA = 1, SB = 2, SC = 3, Tp = 4;
            var solution = new Route("L021 solution", Cat(parts));
            Route Br(string name, int part, int index, params RouteStep[] then) => new(name, Branch(parts, part, index, then));

            int dodge = Dodge(0).Length;
            return new RoomRoutes(solution,
                // Storey A.
                new Betrayal("T1 [NW]: a cat that stands in Eye_A's sight as it fires is shot", "Eye_A", DeathCause.Hazard,
                    Br("stand", SA, AWait.Length, Until(Dead()))),
                new Betrayal("T2 [W]: a cat that walks on under Swing_A is struck", "Swing_A", DeathCause.Hazard,
                    Br("go late", SA, AWait.Length + 2 * AAside().Length, Until(Beat(SwingAPeriod, SwingAGo + 6)), Hold(Right), Until(XAtLeast(16.6f)), Jump(), Until(Dead()))),
                // Storey B.
                new Betrayal("T3 [NW]: a cat that stands in Eye_B's sight as it fires is shot", "Eye_B", DeathCause.Hazard,
                    Br("stand", SB, BWait.Length, Until(Dead()))),
                new Betrayal("T4 [W]: a cat that walks on under Swing_B is struck", "Swing_B", DeathCause.Hazard,
                    Br("go late", SB, BWait.Length + dodge, Until(Beat(SwingBPeriod, SwingBGo + 125)), Hold(Left), Until(XAtMost(13.7f)), Jump(), Until(Dead()))),
                new Betrayal("T5 [W]: a cat that walks on under Spin_B is struck", "Spin_B", DeathCause.Hazard,
                    Br("go now", SB, BWait.Length + dodge + BGo.Length, Hold(Left), Until(Dead()))),
                // Storey C.
                new Betrayal("T6 [NW]: a cat that stays on Stone_C falls onto Spikes_C as it goes", "Spikes_C", DeathCause.Hazard,
                    Br("stay", SC, CWait.Length + dodge + CStone.Length, Until(Shot(EyeCPhase)), Jump(), Until(Airborne()), Until(Grounded()), Until(Dead())),
                    revealedBy: "Stone_C"),
                new Betrayal("T7 [LIE]: a cat that rides the V4 eruption GD blows on is blown down onto Spikes_V4", "Spikes_V4", DeathCause.Hazard,
                    Br("ride it", SC, CWait.Length + dodge + CStone.Length + COff.Length, Cat(Dodge(EyeCPhase), Dodge(EyeCPhase), new[] {
                        Until(Beat(GDPeriod, LieGo)), Hold(Right), Until(XAtLeast(24.4f)), Jump(), Until(XAtLeast(V4X - .3f)), Release(), Until(Grounded()),
                        Until(YAtLeast(C + .3f)), Hold(Left), Until(Dead()) })), revealedBy: "GD"),
                new Betrayal("T8 [NW]: a cat that stops on the walk under Eye_C is shot", "Eye_C", DeathCause.Hazard,
                    Br("stop", SC, 0, Until(XAtLeast(10f)), Release(), Until(Dead()))),
                // Top.
                new Betrayal("T9 [NW]: a cat that stops on the falling floor goes down with it into the trench", "Trench_Spikes", DeathCause.Hazard,
                    Br("stop", Tp, 0, Until(XAtMost(20f)), Release(), Until(Dead())), revealedBy: "Bridge_1"),
                new Betrayal("T10 [NW]: a cat that stands still on Ferry is shot by Eye", "Eye", DeathCause.Hazard,
                    Br("stand", Tp, TFerry.Length, Until(Dead()))));
        }
    }
}
