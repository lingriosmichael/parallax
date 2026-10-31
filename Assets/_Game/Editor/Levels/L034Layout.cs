using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-265 (Docs/Design/L031-L035_concepts.md): level 34, "Phase Shift", the first level built on phase platforms (PAX-122): a
    // void crossed on hard-light stones that blink, with a laser floor far below and a laser roof far above. The lie: "stay on
    // the stone that's lit"; the lit one is the one about to go.
    //  - Blink (section 0): eight floor stones, every other one lit in turn, half a beat each. Hop on the beat: leave a stone
    //    before it goes, land on the next as it lights.
    //  - Flip (section 1): stones on the floor and on the ceiling in turn, each lit on its own beat, with honest gravity orbs
    //    in the gaps between: jump into an orb and fall up (or down) onto the next stone as it lights. Inv_C3, hidden on a
    //    ceiling stone, turns the stick round, so a cat still holding on walks back off it into the laser roof.
    //  - Blackout: a run of glass tiles that shatter one after another behind the cat from its first step, a little slower than
    //    a run, then up onto the ceiling's tiles through an orb (the shattering follows it there) and down by the door.
    static class L034Layout
    {
        // The stones' faces: floor stones' tops at 0, ceiling stones' undersides at Ceil. The lasers: VoidY below, RoofY above.
        internal const float Ceil = 5f, VoidY = -3f, RoofY = 8f, Base = -4f, Top = 10f, Width = 104f, StoneW = 2f, StoneT = .3f;
        internal const int Warn = 12;
        // ---- Blink ----
        internal const float StartX1 = 4f, BlinkGap = 1.6f;
        internal const int BlinkCount = 8, BlinkPeriod = 100, BlinkOn = 50;
        internal static float BlinkX0(int k) => StartX1 + BlinkGap + k * (StoneW + BlinkGap);
        // Odd stones (k even, 0-based) fire at 0, even ones at BlinkOn.
        internal static int BlinkPhase(int k) => k % 2 == 0 ? 0 : BlinkOn;
        internal static readonly float MidX0 = BlinkX0(BlinkCount - 1) + StoneW + BlinkGap;
        internal static readonly float MidX1 = MidX0 + 3f;
        internal static readonly float GateX = MidX0 + .3f, Checkpoint1X = MidX0 + 1.6f;
        // ---- Flip ----
        // The stones (x0, on the ceiling?), each lit FlipOn of every FlipPeriod from its phase; the orbs between them (x, up?).
        // Each orb is OrbIn past the platform before it, and the next stone starts OrbOut past the orb: a cat that jumps off one
        // edge into the orb flips there and comes to the next face about OrbOut + 0.7 on, over the stone.
        internal const float OrbIn = 1.5f, OrbOut = 1f, Pitch = StoneW + OrbIn + OrbOut;
        internal static readonly (float x0, bool ceiling)[] FlipStones = {
            (MidX1 + OrbIn + OrbOut, true), (MidX1 + OrbIn + OrbOut + Pitch, false), (MidX1 + OrbIn + OrbOut + 2f * Pitch, true),
            (MidX1 + OrbIn + OrbOut + 3f * Pitch, false), (MidX1 + OrbIn + OrbOut + 4f * Pitch, true) };
        internal const int FlipPeriod = 120, FlipOn = 70, FlipStep = 30, FlipPhase0 = 0;
        internal static int FlipPhase(int k) => FlipPhase0 + k * FlipStep;
        internal static readonly float[] OrbX = { MidX1 + OrbIn, MidX1 + OrbIn + Pitch, MidX1 + OrbIn + 2f * Pitch, MidX1 + OrbIn + 3f * Pitch, MidX1 + OrbIn + 4f * Pitch, MidX1 + OrbIn + 5f * Pitch };
        internal const float OrbY0 = 1.5f, OrbY1 = 3.5f;
        internal static readonly float LandX0 = MidX1 + 5f * Pitch + OrbIn + OrbOut;
        // Inv_C3: hidden on the third stone (the second ceiling stone).
        internal const int InvTicks = 40;
        // ---- Blackout ----
        // Glass tiles from LandX0 + LandW (floor), up through Orb_B1 onto the ceiling tiles, down through Orb_B2 by the door ledge.
        internal const float LandW = 3f, TileW = 2f;
        internal const int FloorTiles = 8, CeilTiles = 6, TileFirst = 30, TileStep = 19;
        internal static readonly float Tiles0 = LandX0 + LandW;
        internal static readonly float Tiles1 = Tiles0 + FloorTiles * TileW;
        internal static readonly float CeilTiles0 = Tiles1 + OrbIn + OrbOut;
        internal static readonly float CeilTiles1 = CeilTiles0 + CeilTiles * TileW;
        internal static readonly float LedgeX0 = CeilTiles1 + OrbIn + OrbOut;
        internal static readonly float DoorX = LedgeX0 + 2.5f;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            // The void: a laser floor at VoidY and a laser roof at RoofY, wall to wall.
            e.Add(E(SoloRoomElementKind.PitBottom,"Void",(Width * .5f,(Base + VoidY) * .5f),(Width,VoidY - Base)));
            e.Add(E(SoloRoomElementKind.Hazard,"Void_Laser",(Width * .5f,VoidY + .15f),(Width,.3f)));
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, RoofY, Top);
            e.Add(E(SoloRoomElementKind.Hazard,"Roof_Laser",(Width * .5f,RoofY - .15f),(Width,.3f)));

            // ---------- Blink ----------
            Block(e, SoloRoomElementKind.Floor, "Start", 0f, StartX1, VoidY, 0f);
            for (int k = 0; k < BlinkCount; k++)
                e.Add(Stone($"Blink_{k + 1}", BlinkX0(k), false, BlinkOn, BlinkPeriod, BlinkPhase(k)));
            Block(e, SoloRoomElementKind.Floor, "Mid", MidX0, MidX1, VoidY, 0f);

            // ---------- Flip ----------
            for (int k = 0; k < FlipStones.Length; k++)
                e.Add(Stone(FlipStones[k].ceiling ? $"Stone_C{k + 1}" : $"Stone_F{k + 1}", FlipStones[k].x0, FlipStones[k].ceiling, FlipOn, FlipPeriod, FlipPhase(k)));
            var up = new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceUp, rearmOnExit: true, rendererEnabled: true);
            var down = new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceDown, rearmOnExit: true, rendererEnabled: true);
            for (int k = 0; k < OrbX.Length; k++)
                e.Add(E(SoloRoomElementKind.GravityFlip, k % 2 == 0 ? $"Orb_U{k + 1}" : $"Orb_D{k + 1}", (OrbX[k], (OrbY0 + OrbY1) * .5f), (1f, OrbY1 - OrbY0), settings: k % 2 == 0 ? up : down));
            // Inv_C3, hidden under the second ceiling stone at a walking cat's height.
            float c3 = FlipStones[2].x0;
            e.Add(E(SoloRoomElementKind.Inverter,"Inv_C3",(c3 + 1f,Ceil - 1f),(.4f,2f),settings:new SoloRoomTrapSettings(new InverterSettings(InvTicks, disguised: true), new SoloRoomTrapSettings(delayTicks:0))));

            // ---------- Blackout ----------
            Block(e, SoloRoomElementKind.Floor, "Land", LandX0, LandX0 + LandW, VoidY, 0f);
            // The floor tiles: the first gives way TileFirst after a cat steps on it, each next TileStep after the one before.
            for (int k = 0; k < FloorTiles; k++)
                e.Add(Tile($"Tile_{k + 1}", Tiles0 + k * TileW, false, k == 0 ? null : $"Tile_{k}"));
            e.Add(E(SoloRoomElementKind.GravityFlip, "Orb_B1", (Tiles1 + OrbIn, (OrbY0 + OrbY1) * .5f), (1f, OrbY1 - OrbY0), settings: up));
            for (int k = 0; k < CeilTiles; k++)
                e.Add(Tile($"Tile_{FloorTiles + k + 1}", CeilTiles0 + k * TileW, true, $"Tile_{FloorTiles + k}"));
            e.Add(E(SoloRoomElementKind.GravityFlip, "Orb_B2", (CeilTiles1 + OrbIn, (OrbY0 + OrbY1) * .5f), (1f, OrbY1 - OrbY0), settings: down));
            Block(e, SoloRoomElementKind.Floor, "Ledge", LedgeX0, Width, VoidY, 0f);

            var blink = new string[BlinkCount];
            for (int k = 0; k < BlinkCount; k++) blink[k] = $"Blink_{k + 1}";
            var rest = new List<string> { "Inv_C3" };
            for (int k = 0; k < FlipStones.Length; k++) rest.Add(FlipStones[k].ceiling ? $"Stone_C{k + 1}" : $"Stone_F{k + 1}");
            for (int k = 0; k < OrbX.Length; k++) rest.Add(k % 2 == 0 ? $"Orb_U{k + 1}" : $"Orb_D{k + 1}");
            for (int k = 0; k < FloorTiles + CeilTiles; k++) rest.Add($"Tile_{k + 1}");
            rest.Add("Orb_B1"); rest.Add("Orb_B2");
            var sections = new[] {
                CheckpointSection.Start("Blink", new Vector2(2f, 0f), blink),
                new CheckpointSection("Flip", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), rest.ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A hard-light stone (a phase platform), on the floor (its top at 0) or the ceiling (its underside at Ceil): lit `on` of every
        // `period` from `phase`, flickering its last Warn, then gone until the next.
        static SoloRoomElement Stone(string name, float x0, bool ceiling, int on, int period, int phase) =>
            E(SoloRoomElementKind.MovingTrap, name, (x0 + StoneW * .5f, ceiling ? Ceil + StoneT * .5f : -StoneT * .5f), (StoneW, StoneT), settings: new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, phaseOnTicks: on, phaseWarnTicks: Warn),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: on, movingKind: MovingTrapKind.Solid)));

        // A glass tile on the floor (its top at 0) or the ceiling (its underside at Ceil): the first gives way TileFirst after a cat
        // steps on it (its trigger is the band over it); each next, TileStep after the one before (a chain).
        static SoloRoomElement Tile(string name, float x0, bool ceiling, string after)
        {
            var at = (x0 + TileW * .5f, ceiling ? Ceil + StoneT * .5f : -StoneT * .5f);
            var size = (TileW, StoneT);
            if (after == null)
                return E(SoloRoomElementKind.CollapsingFloor, name, at, size, (x0 + TileW * .5f, RoofY * .5f), (TileW, RoofY), new SoloRoomTrapSettings(delayTicks: TileFirst));
            return E(SoloRoomElementKind.CollapsingFloor, name, at, size, settings: new SoloRoomTrapSettings(delayTicks: TileStep, triggerSource: TrapTriggerSource.Chain, chainSource: after));
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
