using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-269 (Docs/Design/L031-L035_concepts.md §7): level 38, "Compactor": claustrophobia (the developer liked L024's squeezes and
    // jaws). A low corridor whose roof comes down, all of it set off by the cat, none of it on a beat. The lie: "the danger is the
    // gap closing ahead". The roof caving in behind is what kills a cat that hesitates.
    //  - Section 0, the cave-in: from the start the roof comes down behind the cat, column by column, a little slower than a run.
    //    Ahead, presses come down as the cat comes near: Press_1 and Press_2 close slowly (run under, don't stop; jump Pit_0a
    //    between them, and hold the stick back through Inv_0, hidden past Press_2), Press_3 looks
    //    the same but stops half way (a cat that stops for it is caught by the cave-in), Press_4 shuts fast and opens again: stop
    //    at its face, and go as it lifts, on the lead the cave-in left you. Then Press_5 (slow again) and Pit_0c to the checkpoint.
    //  - Section 1, the closing hall: the Lid, the whole hall's ceiling, comes down as the cat leaves the checkpoint (a slam: a cat
    //    that jumps into it as it comes down is crushed too). Jump Pit_A at once, run the glass over Pit_C, hold the stick back
    //    through Inv_D and Inv_F (hidden), and out from under the Lid's end with a little head room left. The door is just past it.
    static class L038Layout
    {
        // The corridor: floor top 0, roof underside C. The roof columns (ColW wide, C tall) sit in the roof, between C and 2C.
        internal const float C = 3.2f, Base = -2f, Top = 2f * C + 1f, Width = 120f, ColW = 2.5f, PitDepth = 1.5f;
        internal const float SpawnX = 6f;
        // ---- Section 0 ----
        // The cave-in: Cols columns from x 0, the first CaveFirst after the spawn, each CaveStep after the one before (chained), down
        // C over CaveDrop and staying. Presses at PressCols; each comes down once a cat is PressTrig[k] before it.
        internal const int Cols = 26, CaveFirst = 30, CaveStep = 23, CaveDrop = 8;
        internal static readonly int[] PressCols = { 4, 8, 12, 17, 22 };
        internal static readonly float[] PressTrig = { 3f, 4f, 4f, 5f, 4f };
        // Press_1/2/5: down C over PressSlow (closing on a cat that doesn't keep running). Press_3: down to Press3Gap over Press3Ticks
        // (a cat fits under), then on down, slowly, to shut by Press3Close (no hiding under it). Press_4:
        // down over Press4Drop, held Press4Hold, up over Press4Rise, and again for the next cat (rearmed).
        internal static readonly int[] PressSlow = { 80, 90, 0, 0, 85 };
        internal const float Press3Gap = 1f;
        internal const int Press3Ticks = 60, Press3Close = 150, Press4Drop = 30, Press4Hold = 40, Press4Rise = 25;
        // Two laser pits to jump on the way (Pit_0a, Pit_0b), and Inv_0, hidden, between Press_2 and Press_3.
        internal const float Pit0aX0 = 16.5f, Pit0aX1 = 18.5f, Pit0bX0 = 34f, Pit0bX1 = 36f, Pit0cX0 = 61.5f, Pit0cX1 = 63.5f, Inv0X = 26f;
        internal static float ColX(int k) => k * ColW;
        internal static readonly float CaveEnd = ColX(Cols);
        internal const float GateX = 66.5f, Checkpoint1X = 68f;
        // ---- Section 1 ----
        // The Lid (x LidX0-LidX1, 1 thick) from C down to LidStop over LidTicks, once a cat passes LidTrigX.
        internal const float LidX0 = 69.5f, LidX1 = 115f, LidStop = .3f, LidTrigX = 69.5f;
        internal const int LidTicks = 480;
        internal const float PitA0 = 74f, PitA1 = 76.5f, PitC0 = 84f, PitC1 = 90f, GlassW = 2f, InvDX = 96f, InvFX = 107f, DoorX = 117f;
        internal const int GlassDelay = 15, InvTicks = 30, Inv0Ticks = 45;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(SpawnX,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };

            // ---------- Section 0: the roof is columns over the cave-in, solid past it ----------
            Block(e, SoloRoomElementKind.Ceiling, "Roof_0", 0f, CaveEnd, 2f * C, Top);
            Block(e, SoloRoomElementKind.Floor, "Floor_0", 0f, Pit0aX0, Base, 0f);
            Pit(e, "Pit_0a", Pit0aX0, Pit0aX1);
            Block(e, SoloRoomElementKind.Floor, "Floor_0b", Pit0aX1, Pit0bX0, Base, 0f);
            Pit(e, "Pit_0b", Pit0bX0, Pit0bX1);
            Block(e, SoloRoomElementKind.Floor, "Floor_0c", Pit0bX1, Pit0cX0, Base, 0f);
            Pit(e, "Pit_0c", Pit0cX0, Pit0cX1);
            Block(e, SoloRoomElementKind.Floor, "Floor_0d", Pit0cX1, PitA0, Base, 0f);
            e.Add(E(SoloRoomElementKind.Inverter, "Inv_0", (Inv0X, 1f), (.4f, 2f), settings: new SoloRoomTrapSettings(new InverterSettings(Inv0Ticks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));
            string last = null; int lastCol = -1;
            for (int k = 0; k < Cols; k++)
            {
                int p = System.Array.IndexOf(PressCols, k);
                if (p >= 0) { e.Add(Press(p)); continue; }
                string name = $"Cave_{k + 1}";
                var at = (ColX(k) + ColW * .5f, C * 1.5f);
                var slam = new MovingFloorSettings(SurfaceMotion.Carry, slams: true);
                if (last == null)
                    // Its trigger is the whole cave-in (the cat is in it from the spawn): the chain's every column comes after its near edge.
                    e.Add(E(SoloRoomElementKind.MovingTrap, name, at, (ColW, C), (CaveEnd * .5f, C - PitDepth * .5f), (CaveEnd, 2f * C + PitDepth), new SoloRoomTrapSettings(slam,
                        new SoloRoomTrapSettings(delayTicks: CaveFirst, offset: new Vector2(0f, -C), moveTicks: CaveDrop, holdTicks: 9999, movingKind: MovingTrapKind.Solid))));
                else
                    e.Add(E(SoloRoomElementKind.MovingTrap, name, at, (ColW, C), settings: new SoloRoomTrapSettings(slam,
                        new SoloRoomTrapSettings(delayTicks: CaveStep * (k - lastCol), offset: new Vector2(0f, -C), moveTicks: CaveDrop, holdTicks: 9999,
                            triggerSource: TrapTriggerSource.Chain, chainSource: last, movingKind: MovingTrapKind.Solid))));
                last = name; lastCol = k;
            }

            // ---------- Section 1 ----------
            Block(e, SoloRoomElementKind.Ceiling, "Roof_1", CaveEnd, Width, C + 1f, Top);
            Block(e, SoloRoomElementKind.Ceiling, "Lintel", CaveEnd, LidX0, C, C + 1f);
            Block(e, SoloRoomElementKind.Ceiling, "Lintel_E", LidX1, Width, C, C + 1f);
            e.Add(E(SoloRoomElementKind.MovingTrap, "Lid", ((LidX0 + LidX1) * .5f, C + .5f), (LidX1 - LidX0, 1f), (LidTrigX + .2f, (C + 1f) * .5f), (.4f, C + 1f), new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, LidStop - C), moveTicks: LidTicks, holdTicks: 9999, movingKind: MovingTrapKind.Solid))));
            Pit(e, "Pit_A", PitA0, PitA1);
            Block(e, SoloRoomElementKind.Floor, "Floor_1", PitA1, PitC0, Base, 0f);
            Pit(e, "Pit_C", PitC0, PitC1);
            for (int i = 0; i < 3; i++)
            {
                float x0 = PitC0 + i * GlassW;
                e.Add(E(SoloRoomElementKind.CollapsingFloor, $"Glass_{i + 1}", (x0 + GlassW * .5f, -PitDepth * .5f), (GlassW, PitDepth), settings: new SoloRoomTrapSettings(delayTicks: GlassDelay)));
            }
            Block(e, SoloRoomElementKind.Floor, "Floor_2", PitC1, Width, Base, 0f);
            e.Add(E(SoloRoomElementKind.Inverter, "Inv_D", (InvDX, 1f), (.4f, 2f), settings: new SoloRoomTrapSettings(new InverterSettings(InvTicks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));
            e.Add(E(SoloRoomElementKind.Inverter, "Inv_F", (InvFX, .6f), (.4f, 1.2f), settings: new SoloRoomTrapSettings(new InverterSettings(InvTicks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));

            var s0 = new List<string> { "Inv_0" };
            for (int k = 0; k < Cols; k++) { int p = System.Array.IndexOf(PressCols, k); s0.Add(p >= 0 ? $"Press_{p + 1}" : $"Cave_{k + 1}"); }
            var sections = new[] {
                CheckpointSection.Start("CaveIn", new Vector2(SpawnX, 0f), s0.ToArray()),
                new CheckpointSection("Closing", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), new[] { "Lid", "Glass_1", "Glass_2", "Glass_3", "Inv_D", "Inv_F" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // Press_{p+1}, the roof column PressCols[p]: down once a cat comes within PressTrig[p] of it.
        static SoloRoomElement Press(int p)
        {
            int k = PressCols[p];
            float x0 = ColX(k), trig = x0 - PressTrig[p];
            var at = (x0 + ColW * .5f, C * 1.5f);
            var trigAt = (trig + .2f, C);
            var slam = new MovingFloorSettings(SurfaceMotion.Carry, slams: true);
            SoloRoomTrapSettings timing = p switch
            {
                0 or 1 or 4 => new SoloRoomTrapSettings(offset: new Vector2(0f, -C), moveTicks: PressSlow[p], holdTicks: 9999, movingKind: MovingTrapKind.Solid),
                2 => new SoloRoomTrapSettings(new PathSettings(new[] { new Vector2(0f, Press3Gap - C), new Vector2(0f, -C) }, firstLegTicks: Press3Ticks),
                    new SoloRoomTrapSettings(moveTicks: Press3Close, holdTicks: 9999, movingKind: MovingTrapKind.Solid)),
                _ => new SoloRoomTrapSettings(offset: new Vector2(0f, -C), moveTicks: Press4Drop, holdTicks: Press4Hold, returnTicks: Press4Rise,
                    repeatMode: TrapRepeatMode.Rearm, cooldownTicks: Press4Drop + Press4Hold + Press4Rise, movingKind: MovingTrapKind.Solid),
            };
            return E(SoloRoomElementKind.MovingTrap, $"Press_{p + 1}", at, (ColW, C), trigAt, (.4f, 2f * C), new SoloRoomTrapSettings(slam, timing));
        }

        // A laser pit: its floor PitDepth down, lethal along the bottom.
        static void Pit(List<SoloRoomElement> e, string name, float x0, float x1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, name, ((x0 + x1) * .5f, (Base - PitDepth) * .5f), (x1 - x0, -PitDepth - Base)));
            e.Add(E(SoloRoomElementKind.Hazard, name + "_Laser", ((x0 + x1) * .5f, -PitDepth + .15f), (x1 - x0, .3f)));
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
