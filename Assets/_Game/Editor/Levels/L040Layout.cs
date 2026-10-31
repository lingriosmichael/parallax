using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-271 (Docs/Design/L031-L035_concepts.md §7): level 40, "Freefall": the first descent. Five storeys 4 apart, each a run
    // to its hole at the far end, and the Beam, a flat laser wall to wall, coming down the whole tower after the cat from the
    // start (a lethal mover, like L032's floods: scanners sweep sideways only). The lie: "go down the hole". A hole is a hatch,
    // and it opens when it opens.
    //  - S0: Gate_0 on the walk; Hatch_0 over the hole pulls back as the cat comes to it.
    //  - S1: the glass run over Pit_1 (a laser in the slab): don't stop on it. The hole at the west end is open.
    //  - S2: Hatch_2 opens late, only as the beam comes down close over S2: wait at it, under the beam, and drop as it goes.
    //  - S3 (section 1): the Beam is close behind from here on. Inv_3, hidden, turns the stick round; Gate_3 on the walk; Hatch_3
    //    pulls back as the cat comes to it.
    //  - S4: run the floor east to the door, in a notch in the tower's wall the Beam doesn't reach.
    static class L040Layout
    {
        // The tower's inside is x 0-TowerW; at S4 the floor runs on into the door's notch (TowerW-Width, NotchH tall).
        internal const float TowerW = 24f, Width = 25.5f, NotchH = 2f, Slab = 1f, StoreyH = 3f, HoleW = 2.4f, BeamH = .3f, PitDepth = .8f;
        internal static readonly float[] F = { 0f, -4f, -8f, -12f, -16f };
        // S0 is taller (the Beam starts high over it).
        internal const float RoofY = 5f, Top = RoofY + 1.5f, Base = -18f;
        // The holes: S0 and S2 east, S1 and S3 west.
        internal const float HoleE0 = 20f, HoleW0 = 0f;
        internal static float HoleX0(int k) => k % 2 == 0 ? HoleE0 : HoleW0;
        // ---- Section 0 ----
        // The Beam: from BeamY0 down to BeamY1 (on S4's floor) at BeamSpeed, from the start (its trigger is the spawn's storey).
        internal const float BeamY0 = RoofY - .2f, BeamY1 = -16f + .15f, BeamSpeed = .016f;
        internal static readonly int BeamTicks = Mathf.RoundToInt((BeamY0 - BeamY1) / BeamSpeed);
        internal const float Gate0X = 10f;
        internal const int GateWarn = 12, GateOn = 36, GatePeriod = 90, Gate0Phase = 30;
        internal const float Hatch0TrigX = 17.5f;
        internal const int HatchShrink = 20;
        internal const float Glass1X0 = 8f, GlassW = 2f;
        internal const int GlassCount = 3, GlassDelay = 15;
        // Hatch_2 opens Hatch2Delay after the Beam sets off: as it comes down close over S2's floor.
        internal static readonly int Hatch2Delay = Mathf.RoundToInt((BeamY0 - (F[2] + 2f)) / BeamSpeed);
        internal const float Checkpoint1X = 21f;
        // ---- Section 1 ----
        internal const float Inv3X = 15f, Gate3X = 9f;
        internal const int InvTicks = 30, Gate3Phase = 0;
        internal const float Hatch3TrigX = 5f, DoorX = 24.75f;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,F[0]),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,F[4] + .75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, TowerW, RoofY, Top);
            Block(e, SoloRoomElementKind.Wall, "Tower_R", TowerW, Width, F[4] + NotchH, Top);

            // ---- The slabs, each cut by its hole (S1's also by Pit_1, under its glass) ----
            for (int k = 0; k < 4; k++)
            {
                float h0 = HoleX0(k), y0 = F[k] - Slab, y1 = F[k];
                if (k == 1)
                {
                    float g1 = Glass1X0 + GlassCount * GlassW;
                    Block(e, SoloRoomElementKind.Floor, "Slab_1a", h0 + HoleW, Glass1X0, y0, y1);
                    e.Add(E(SoloRoomElementKind.PitBottom, "Pit_1", ((Glass1X0 + g1) * .5f, (y0 + y1 - PitDepth) * .5f), (g1 - Glass1X0, Slab - PitDepth)));
                    e.Add(E(SoloRoomElementKind.Hazard, "Pit_1_Laser", ((Glass1X0 + g1) * .5f, y1 - PitDepth + .15f), (g1 - Glass1X0, .3f)));
                    for (int i = 0; i < GlassCount; i++)
                    {
                        float x0 = Glass1X0 + i * GlassW;
                        e.Add(E(SoloRoomElementKind.CollapsingFloor, $"Glass_{i + 1}", (x0 + GlassW * .5f, y1 - PitDepth * .5f), (GlassW, PitDepth), settings: new SoloRoomTrapSettings(delayTicks: GlassDelay)));
                    }
                    Block(e, SoloRoomElementKind.Floor, "Slab_1b", g1, TowerW, y0, y1);
                    continue;
                }
                if (h0 > 0f) Block(e, SoloRoomElementKind.Floor, $"Slab_{k}a", 0f, h0, y0, y1);
                Block(e, SoloRoomElementKind.Floor, $"Slab_{k}b", h0 + HoleW, TowerW, y0, y1);
            }
            Block(e, SoloRoomElementKind.Floor, "Slab_4", 0f, Width, Base, F[4]);

            // ---------- Section 0 ----------
            // The Beam: from the start (its trigger is S0's storey), down the tower.
            e.Add(E(SoloRoomElementKind.MovingTrap, "Beam", (TowerW * .5f, BeamY0), (TowerW, BeamH), (TowerW * .5f, (F[0] + RoofY) * .5f), (TowerW, RoofY - F[0]), new SoloRoomTrapSettings(
                offset: new Vector2(0f, BeamY1 - BeamY0), moveTicks: BeamTicks, holdTicks: 9999, movingKind: MovingTrapKind.Hazard)));
            e.Add(Gate("Gate_0", Gate0X, F[0], Gate0Phase, RoofY - F[0]));
            e.Add(Hatch("Hatch_0", HoleX0(0), F[0], Hatch0TrigX, ShrinkFrom.Left, RoofY - F[0]));
            // Hatch_2: chained from the Beam (the start), opening Hatch2Delay later.
            e.Add(E(SoloRoomElementKind.ShrinkingFloor, "Hatch_2", (HoleX0(2) + HoleW * .5f, F[2] - Slab * .5f), (HoleW, Slab), settings: new SoloRoomTrapSettings(
                new ShrinkSettings(HatchShrink, 0f, ShrinkFrom.Left),
                new SoloRoomTrapSettings(delayTicks: Hatch2Delay, triggerSource: TrapTriggerSource.Chain, chainSource: "Beam"))));

            // ---------- Section 1 ----------
            e.Add(E(SoloRoomElementKind.Inverter, "Inv_3", (Inv3X, F[3] + 1f), (.4f, 2f), settings: new SoloRoomTrapSettings(new InverterSettings(InvTicks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));
            e.Add(Gate("Gate_3", Gate3X, F[3], Gate3Phase));
            e.Add(Hatch("Hatch_3", HoleX0(3), F[3], Hatch3TrigX, ShrinkFrom.Right));

            var sections = new[] {
                CheckpointSection.Start("Upper", new Vector2(2f, F[0]), new[] { "Beam", "Gate_0", "Hatch_0", "Glass_1", "Glass_2", "Glass_3", "Hatch_2" }),
                new CheckpointSection("Lower", new Vector2(Checkpoint1X, F[3]), new Rect(HoleE0, F[3] + 1.6f, HoleW, 1.2f), new[] { "Inv_3", "Gate_3", "Hatch_3" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A pulse gate across a storey (floor y to its ceiling, `h` over it) at x.
        static SoloRoomElement Gate(string name, float x, float y, int phase, float h = StoreyH) =>
            E(SoloRoomElementKind.PulseGate, name, (x, y + h * .5f), (.3f, h), settings: new SoloRoomTrapSettings(
                new PulseGateSettings(GateWarn, GateOn), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: GatePeriod, phaseTicks: phase)));

        // A hatch over a storey's hole: it pulls back over HatchShrink once a cat comes to trigX (its trigger the storey's height, `h`).
        static SoloRoomElement Hatch(string name, float x0, float y, float trigX, ShrinkFrom from, float h = StoreyH) =>
            E(SoloRoomElementKind.ShrinkingFloor, name, (x0 + HoleW * .5f, y - Slab * .5f), (HoleW, Slab), (trigX, y + h * .5f), (.4f, h),
                new SoloRoomTrapSettings(new ShrinkSettings(HatchShrink, 0f, from), new SoloRoomTrapSettings(delayTicks: 0)));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
