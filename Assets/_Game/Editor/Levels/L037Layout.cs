using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-268 (Docs/Design/L031-L035_concepts.md §7): level 37, "Transit". One long laser lake crossed on cat-sized platforms that
    // move every way: ferries, a sinker, a lift, a slide, and glitch platforms (PAX-114) that jump from spot to spot and never
    // carry a rider. The lie: "ride the one going your way". It turns back, it sinks, it jumps away from under you.
    //  - Section 0: Ferry_1 leaves as the level starts: on, and across. Sink_2 looks like the next ferry, but at its far end it
    //    goes down through the laser into the Well: off before it sinks. Glitch_3 jumps east from spot to spot: leapfrog it
    //    (jump as it flickers, land where it appears). Ferry_4 runs to the middle ledge and straight back: off at the turn.
    //  - Section 1: Lift_5 rises to Ferry_6's height (no jumping while it rises: wait for the top). Ferry_6 runs east; Slide_7
    //    comes down the slope from its end to the lake's height.
    //  - The finale: Leap_8, a glitch platform, jumps four times toward the door ledge, faster than Glitch_3. Leapfrog it to the
    //    door.
    static class L037Layout
    {
        // The lake: its laser at VoidY across the whole width, its floor at VoidY but for the Well under Sink_2's end (Base deep).
        internal const float C = 4.8f, VoidY = -3f, Base = -5f, Top = 6.8f, Width = 100f, PW = 1.5f, PT = .5f;
        internal const int Warn = 12;

        // A mover's schedule: left edge X0 and top Y at home; path points (offsets); out Move, hold Hold, back Ret, every Period
        // from Phase; a glitch platform jumps between the points instead.
        internal readonly struct M
        {
            public readonly string Name; public readonly float X0, Y; public readonly Vector2[] Points;
            public readonly int Move, Hold, Ret, Period, Phase, FirstLeg; public readonly bool Glitch;
            public M(string name, float x0, float y, Vector2[] points, int move, int hold, int ret, int period, int phase, bool glitch = false, int firstLeg = 0)
            { Name = name; X0 = x0; Y = y; Points = points; Move = move; Hold = hold; Ret = ret; Period = period; Phase = phase; Glitch = glitch; FirstLeg = firstLeg; }
            public int Since(int t) => t < Phase ? -1 : (t - Phase) % Period;
            public Vector2 Offset(int t) { int s = Since(t); return s < 0 ? Vector2.zero : TrapPath.Offset(Points, Glitch, s, Move, Hold, Ret, FirstLeg); }
            public float Left(int t) => X0 + Offset(t).x;
            public float TopAt(int t) => Y + Offset(t).y;
        }

        // ---- Section 0 ----
        internal const float StartX1 = 4f;
        internal static readonly M Ferry1 = new("Ferry_1", 4.5f, 0f, new[] { new Vector2(9f, 0f) }, 100, 20, 100, 260, 40);
        internal static readonly M Sink2 = new("Sink_2", 17f, 0f, new[] { new Vector2(7f, 0f), new Vector2(7f, -3.6f) }, 110, 30, 110, 300, 180, firstLeg: 70);
        internal const float WellX0 = 23.8f, WellX1 = 25.7f;
        internal static readonly M Glitch3 = new("Glitch_3", 27.5f, 0f, new[] { new Vector2(3.5f, 0f), new Vector2(7f, 0f) }, 120, 50, 120, 340, 240, glitch: true);
        internal static readonly M Ferry4 = new("Ferry_4", 38.5f, 0f, new[] { new Vector2(9f, 0f) }, 90, 12, 90, 260, 430);
        internal const float MidX0 = 49.5f, MidX1 = 54f, GateX = 50.2f, Checkpoint1X = 52f;
        // ---- Section 1 ----
        internal static readonly M Lift5 = new("Lift_5", 55f, 0f, new[] { new Vector2(0f, 2.5f) }, 60, 50, 60, 270, 70);
        internal static readonly M Ferry6 = new("Ferry_6", 58f, 2.5f, new[] { new Vector2(8f, 0f) }, 80, 20, 80, 270, 180);
        internal static readonly M Slide7 = new("Slide_7", 69f, 2.5f, new[] { new Vector2(5f, -2.5f) }, 70, 20, 70, 270, 270);
        internal static readonly M Leap8 = new("Leap_8", 77f, 0f, new[] { new Vector2(3.5f, 0f), new Vector2(7f, 0f), new Vector2(10.5f, 0f) }, 135, 45, 45, 270, 360, glitch: true);
        internal const float LedgeX0 = 89.5f, DoorX = 96f;

        internal static readonly M[] Section0 = { Ferry1, Sink2, Glitch3, Ferry4 }, Section1 = { Lift5, Ferry6, Slide7, Leap8 };

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, C, Top);
            // The lake's floor (the Well cut out under Sink_2's end) and its laser, wall to wall.
            Block(e, SoloRoomElementKind.PitBottom, "Lake_W", 0f, WellX0, Base, VoidY);
            Block(e, SoloRoomElementKind.PitBottom, "Well", WellX0, WellX1, Base, -4.2f);
            Block(e, SoloRoomElementKind.PitBottom, "Lake_E", WellX1, Width, Base, VoidY);
            e.Add(E(SoloRoomElementKind.Hazard,"Lake_Laser",(Width * .5f,VoidY + .15f),(Width,.3f)));

            // The ledges: start, middle, door.
            Block(e, SoloRoomElementKind.Floor, "Start", 0f, StartX1, VoidY, 0f);
            Block(e, SoloRoomElementKind.Floor, "Mid", MidX0, MidX1, VoidY, 0f);
            Block(e, SoloRoomElementKind.Floor, "Ledge", LedgeX0, Width, VoidY, 0f);

            foreach (M m in Section0) e.Add(Mover(m));
            foreach (M m in Section1) e.Add(Mover(m));

            var s0 = new List<string>(); foreach (M m in Section0) s0.Add(m.Name);
            var s1 = new List<string>(); foreach (M m in Section1) s1.Add(m.Name);
            var sections = new[] {
                CheckpointSection.Start("Lake1", new Vector2(2f, 0f), s0.ToArray()),
                new CheckpointSection("Lake2", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), s1.ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A cat-sized platform on its path, Periodic from the room's start.
        static SoloRoomElement Mover(M m) =>
            E(SoloRoomElementKind.MovingTrap, m.Name, (m.X0 + PW * .5f, m.Y - PT * .5f), (PW, PT), settings: new SoloRoomTrapSettings(
                new PathSettings(m.Points, glitch: m.Glitch, warnTicks: m.Glitch ? Warn : 0, firstLegTicks: m.FirstLeg),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks: m.Move, holdTicks: m.Hold, returnTicks: m.Ret, repeatMode: TrapRepeatMode.Periodic, periodTicks: m.Period,
                        phaseTicks: m.Phase, cooldownTicks: m.Move + m.Hold + m.Ret, movingKind: MovingTrapKind.Solid))));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
