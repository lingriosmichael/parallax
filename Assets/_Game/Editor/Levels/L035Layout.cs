using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-266 (Docs/Design/L031-L035_concepts.md): level 35, "Scanline", the first level built on scanners (PAX-113). A corridor 4 u
    // tall swept by floor-to-ceiling beams that come at the cat twice as fast as it runs: they can't be jumped or outrun. The only
    // safety is a slot in the floor, below the beam: drop in, let it pass over, climb out, run for the next. The lie: "outrun it".
    //  - Hall 1 (section 0): Scan_A sweeps west on its beat. Slots every 10 u, a run apart between scans; each fills up slowly
    //    once a cat is in it (no idling). Before the third is Shallow_3, a slot too shallow to hide in (the beam's band reaches
    //    into it): jump it, the real one is just past it.
    //  - Hall 2 (section 1): Scan_D sweeps west on its beat. Its slots fill up fast: a cat that drops in is pushed back up into
    //    the beam's band a moment later (Fill_*), so duck late, not early.
    //  - The finale, the double scan: Scan_E follows Scan_D a moment behind over the last slot, which fills as well: drop in just
    //    before the first beam and climb out right after the second.
    static class L035Layout
    {
        internal const float C = 4f, Base = -3f, Top = 6f, Width = 97f, SlotW = 1.4f, SlotDepth = 1.3f, BeamW = .3f;
        internal const int Warn = 12;
        // ---- Hall 1 ----
        // Scan_A: home AHome, sweeping west to AFar at ScanSpeed, then gone home at once; every APeriod from APhase.
        internal const float AHome = 47.5f, AFar = 1f, ScanSpeed = .5f;
        internal static readonly int AMove = Mathf.RoundToInt((AHome - AFar) / ScanSpeed);
        internal const int APeriod = 150, APhase = 40, ScanHold = 2;
        // The slots (x0); Shallow_3 (ShallowDepth deep: a cat in it is still in the beam's band) sits just before the real Slot_3.
        internal static readonly float[] Slots1 = { 6.3f, 16.3f, 26.3f, 36.3f };
        internal const float Fake3X0 = 22.3f, ShallowDepth = .4f;
        internal const float GateX = 48.5f, Checkpoint1X = 50f;
        // ---- Hall 2 ----
        internal const float DHome = 91.5f, DFar = 51f;
        internal static readonly int DMove = Mathf.RoundToInt((DHome - DFar) / ScanSpeed);
        internal const int DPeriod = 150, DPhase = 20, EBehind = 26;
        // The filling slots: each bottom rises SlotDepth over FillTicks, FillDelay after a cat lands in it, holds, and goes back.
        internal static readonly float[] Slots2 = { 57f, 66f, 75f, 84f };
        internal const int FillDelay = 55, FillTicks = 40, FillHold = 60, FillBack = 30;
        // Hall 1's slots fill too, later: a cat may wait for one sweep in them, not two.
        internal const int SlotFillDelay = 90;
        internal const float DoorX = 95f;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, C, Top);

            // ---- The floor, cut by the slots (and Fake_3's pit) ----
            var cuts = new List<(float x0, float x1, string kind)>();
            for (int i = 0; i < Slots1.Length; i++) cuts.Add((Slots1[i], Slots1[i] + SlotW, "slot1"));
            cuts.Add((Fake3X0, Fake3X0 + SlotW, "shallow"));
            for (int i = 0; i < Slots2.Length; i++) cuts.Add((Slots2[i], Slots2[i] + SlotW, "slot2"));
            cuts.Sort((a, b) => a.x0.CompareTo(b.x0));
            float x = 0f; int n = 0, s1 = 0, s2 = 0;
            foreach (var (x0, x1, kind) in cuts)
            {
                Block(e, SoloRoomElementKind.Floor, $"Floor_{++n}", x, x0, Base, 0f);
                if (kind == "shallow")
                    Block(e, SoloRoomElementKind.Floor, "Shallow_3", x0, x1, Base, -ShallowDepth);
                else Fill(e, kind == "slot1" ? $"Slot_{++s1}" : $"Fill_{++s2}", x0, x1, kind == "slot1" ? SlotFillDelay : FillDelay);
                x = x1;
            }
            Block(e, SoloRoomElementKind.Floor, $"Floor_{++n}", x, Width, Base, 0f);

            // ---- The scanners ----
            e.Add(Scanner("Scan_A", AHome, AFar, AMove, APeriod, APhase));
            e.Add(Scanner("Scan_D", DHome, DFar, DMove, DPeriod, DPhase));
            e.Add(Scanner("Scan_E", DHome, DFar, DMove, DPeriod, DPhase + EBehind));

            var hall2 = new List<string> { "Scan_D", "Scan_E" };
            for (int i = 0; i < Slots2.Length; i++) hall2.Add($"Fill_{i + 1}");
            var sections = new[] {
                CheckpointSection.Start("Hall1", new Vector2(2f, 0f), new[] { "Scan_A", "Slot_1", "Slot_2", "Slot_3", "Slot_4" }),
                new CheckpointSection("Hall2", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), hall2.ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A filling slot: its bottom, from Base up to -SlotDepth, rises to the floor `delay` after a cat comes over it.
        static void Fill(List<SoloRoomElement> e, string name, float x0, float x1, int delay)
        {
            Block(e, SoloRoomElementKind.Floor, name + "_Base", x0, x1, Base, -SlotDepth - 1f);
            e.Add(E(SoloRoomElementKind.MovingTrap,name,((x0 + x1) * .5f,-SlotDepth - .5f),(SlotW,1f),((x0 + x1) * .5f,(-SlotDepth + C) * .5f),(SlotW,C + SlotDepth),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:delay,offset:new Vector2(0f,SlotDepth),moveTicks:FillTicks,holdTicks:FillHold,returnTicks:FillBack,
                    repeatMode:TrapRepeatMode.Rearm,cooldownTicks:FillTicks + FillHold + FillBack,movingKind:MovingTrapKind.Solid))));
        }

        // A scanline: a floor-to-ceiling beam resting at `home`, warning Warn, then sweeping west to `far` over `move`, and gone home at
        // once; every `period` from `phase`.
        static SoloRoomElement Scanner(string name, float home, float far, int move, int period, int phase) =>
            E(SoloRoomElementKind.MovingTrap, name, (home, C * .5f), (BeamW, C), settings: new SoloRoomTrapSettings(
                new ScanSettings(Warn),
                new SoloRoomTrapSettings(offset: new Vector2(far - home, 0f), moveTicks: move, holdTicks: ScanHold, returnTicks: 0,
                    repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: Warn + move + ScanHold, movingKind: MovingTrapKind.Hazard)));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
