using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-267 (Docs/Design/L031-L035_concepts.md §7): level 36, "Overwatch", the first level built on aiming turrets (PAX-121) and
    // cover. A wall turret tracks the cat anywhere and its beam stops at the first solid, so a low block between it and the cat is
    // cover. The lie: "get behind cover". Some cover is a hologram the beam goes through, some is glass that breaks once leant on,
    // some moves off or sinks, and an eye in the ceiling watches every place you'd hide twice.
    //  - Hall 1 (section 0): Eye_1, in Tower_1 at the hall's end, fires down the hall on its beat. Hop from cover to cover between
    //    shots: Block_1, then past Holo_2 (a hologram, no cover) to Block_3, then Glass_4 (don't touch it), Block_5, Block_6
    //    (taller: this close, the beam comes down steeper), and over Pit_1 under Tower_1. Watch_1/3/5, ceiling eyes, shoot a cat
    //    that stays behind the same block for a second shot. Far from an eye a low block shades a long way; close to it, little.
    //  - Hall 2 (section 1): Shield_1, a moving block, sets off east toward Eye_2 as the cat leaves the checkpoint: walk in its
    //    shadow. It sinks into Slot_S by Block_7: get behind Block_7 as it goes down, then Block_8, and under Tower_2.
    //  - The finale, the crossfire: Eye_W (low on Tower_2's east face) and Eye_E (high over the door) fire in turn down the last
    //    hall. Each block covers one side only: hop it as Eye_E's shot ends and stand in its lee for Eye_W's; cross each gap as
    //    Eye_W's ends and press on the next block's face for Eye_E's.
    static class L036Layout
    {
        internal const float C = 4.5f, Base = -2f, Top = 6.5f, Width = 108f, PitDepth = 1.5f, CoverH = .8f, CoverW = 1f;
        // Turrets: aim EyeAim (tracking), lock EyeLock (the tell), fire EyeFire. Wall muzzles sit 0.05 off their host's face.
        internal const int EyeAim = 30, EyeLock = 12, EyeFire = 8;
        internal const float Skin = .05f, TowerFoot = 1.8f;
        // ---- Hall 1 ----
        internal const float Tower1X0 = 47f, Eye1Y = 4f, Eye1Range = 50f;
        internal const int Eye1Period = 90, Eye1Phase = 30;
        // The cover, west faces (x0): Block_1, Holo_2, Block_3, Glass_4, Block_5, Block_6; then Pit_1 before Tower_1.
        internal const float Block1X = 9f, Holo2X = 12.5f, Block3X = 17f, Glass4X = 25f, Block5X = 32f, Block6X = 38f, Pit1X0 = 41.5f, Pit1X1 = 44f;
        internal const int GlassDelay = 10;
        // Block_6 is taller: this close to Eye_1 its beam comes down steeper.
        internal const float Block6H = 1.1f;
        // Watch_1/3/5: ceiling eyes over the floor in front of Block_1, Block_3, Block_5 (WatchZone wide), WatchPhase on Eye_1's beat.
        internal const float WatchZone = 2.5f;
        internal const int WatchPeriod = 90, WatchPhase = 75;
        internal const float GateX = 49.5f, Checkpoint1X = 51f;
        // ---- Hall 2 ----
        // Shield_1 (ShieldW x ShieldH) from ShieldX0 east ShieldRun over ShieldTicks, then down ShieldH into Slot_S over SinkTicks,
        // set off as a cat passes ShieldTrigX.
        internal const float ShieldX0 = 53f, ShieldW = 1.2f, ShieldH = 1f, ShieldRun = 17.3f, ShieldTrigX = 52f;
        internal const int ShieldTicks = 220, SinkTicks = 50;
        internal static readonly float SlotX0 = ShieldX0 + ShieldRun, SlotX1 = ShieldX0 + ShieldRun + ShieldW;
        internal const float Block7X = 72f, Block8X = 76f, Tower2X0 = 80f, Eye2Y = 4f, Eye2Range = 27f;
        internal const int Eye2Period = 85, Eye2Phase = 0, EscortBeat = 0;
        // ---- The crossfire ----
        internal static readonly float[] CrossX = { 84f, 89f, 94f, 99f };
        internal const float CrossH = 1f, EyeWY = 2f, EyeEY = 4f, EyeWRange = 28f, EyeERange = 30f, DoorX = 104.5f;
        internal const int CrossPeriod = 90, EyeWPhase = 0, EyeEPhase = 45;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, C, Top);

            // ---- The floor, cut by Pit_1 and Slot_S ----
            Block(e, SoloRoomElementKind.Floor, "Floor_A", 0f, Pit1X0, Base, 0f);
            e.Add(E(SoloRoomElementKind.PitBottom,"Pit_1",((Pit1X0 + Pit1X1) * .5f,(Base - PitDepth) * .5f),(Pit1X1 - Pit1X0,-PitDepth - Base)));
            e.Add(E(SoloRoomElementKind.Hazard,"Pit_1_Laser",((Pit1X0 + Pit1X1) * .5f,-PitDepth + .15f),(Pit1X1 - Pit1X0,.3f)));
            Block(e, SoloRoomElementKind.Floor, "Floor_B", Pit1X1, SlotX0, Base, 0f);
            Block(e, SoloRoomElementKind.Floor, "Slot_S", SlotX0, SlotX1, Base, -ShieldH);
            Block(e, SoloRoomElementKind.Floor, "Floor_C", SlotX1, Width, Base, 0f);

            // ---------- Hall 1 ----------
            Block(e, SoloRoomElementKind.Floor, "Block_1", Block1X, Block1X + CoverW, 0f, CoverH);
            e.Add(E(SoloRoomElementKind.FakePlatform,"Holo_2",(Holo2X + CoverW * .5f,CoverH * .5f),(CoverW,CoverH)));
            Block(e, SoloRoomElementKind.Floor, "Block_3", Block3X, Block3X + CoverW, 0f, CoverH);
            e.Add(E(SoloRoomElementKind.CollapsingFloor,"Glass_4",(Glass4X + CoverW * .5f,CoverH * .5f),(CoverW,CoverH),settings:new SoloRoomTrapSettings(delayTicks:GlassDelay)));
            Block(e, SoloRoomElementKind.Floor, "Block_5", Block5X, Block5X + CoverW, 0f, CoverH);
            Block(e, SoloRoomElementKind.Floor, "Block_6", Block6X, Block6X + CoverW, 0f, Block6H);
            Block(e, SoloRoomElementKind.Ceiling, "Tower_1", Tower1X0, Tower1X0 + 1f, TowerFoot, C);
            e.Add(WallEye("Eye_1", Tower1X0 - Skin, Eye1Y, Eye1Range, Eye1Period, Eye1Phase));
            e.Add(Watch("Watch_1", Block1X, WatchPhase));
            e.Add(Watch("Watch_3", Block3X, WatchPhase));
            e.Add(Watch("Watch_5", Block5X, WatchPhase));

            // ---------- Hall 2 ----------
            // Shield_1: east along the floor, then down into Slot_S, once a cat passes ShieldTrigX.
            e.Add(E(SoloRoomElementKind.MovingTrap,"Shield_1",(ShieldX0 + ShieldW * .5f,ShieldH * .5f),(ShieldW,ShieldH),(ShieldTrigX + .2f,C * .5f),(.4f,C),new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(ShieldRun, 0f), new Vector2(ShieldRun, -ShieldH) }, firstLegTicks: ShieldTicks),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks:ShieldTicks + SinkTicks,holdTicks:9999,movingKind:MovingTrapKind.Solid)))));
            Block(e, SoloRoomElementKind.Floor, "Block_7", Block7X, Block7X + CoverW, 0f, CoverH);
            Block(e, SoloRoomElementKind.Floor, "Block_8", Block8X, Block8X + CoverW, 0f, CoverH);
            Block(e, SoloRoomElementKind.Ceiling, "Tower_2", Tower2X0, Tower2X0 + 1f, TowerFoot, C);
            e.Add(WallEye("Eye_2", Tower2X0 - Skin, Eye2Y, Eye2Range, Eye2Period, Eye2Phase));

            // ---------- The crossfire ----------
            for (int k = 0; k < CrossX.Length; k++) Block(e, SoloRoomElementKind.Floor, $"Cross_{k + 1}", CrossX[k], CrossX[k] + CoverW, 0f, CrossH);
            e.Add(WallEye("Eye_W", Tower2X0 + 1f + Skin, EyeWY, EyeWRange, CrossPeriod, EyeWPhase));
            e.Add(WallEye("Eye_E", Width - Skin, EyeEY, EyeERange, CrossPeriod, EyeEPhase));

            var sections = new[] {
                CheckpointSection.Start("Hall1", new Vector2(2f, 0f), new[] { "Eye_1", "Watch_1", "Watch_3", "Watch_5", "Glass_4", "Holo_2" }),
                new CheckpointSection("Hall2", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), new[] { "Shield_1", "Eye_2", "Eye_W", "Eye_E" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A wall turret with its muzzle at (x, y), just off its host's face: it tracks the cat anywhere, its beam reaching `range`,
        // every `period` from `phase`.
        static SoloRoomElement WallEye(string name, float x, float y, float range, int period, int phase) =>
            E(SoloRoomElementKind.Turret, name, (x, y), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, EyeLock, EyeFire, range: range),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: EyeAim + EyeLock + EyeFire)));

        // A ceiling eye watching only the floor just west of the cover block at blockX (WatchZone wide).
        static SoloRoomElement Watch(string name, float blockX, int phase)
        {
            float muzzle = C - .15f, x = blockX - WatchZone * .5f;
            var zone = new Rect(-WatchZone * .5f, -muzzle, WatchZone, 1.2f);
            return E(SoloRoomElementKind.Turret, name, (x, muzzle), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, EyeLock, EyeFire, ceilingMount: true, aimZone: zone),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: WatchPeriod, phaseTicks: phase, cooldownTicks: EyeAim + EyeLock + EyeFire)));
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
