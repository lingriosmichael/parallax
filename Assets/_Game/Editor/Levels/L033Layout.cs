using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-264 (Docs/Design/L031-L035_concepts.md): level 33, "Wormhole", the first level with portals (PAX-120). Cells walled off
    // from each other, each crossed by a portal whose exit glitches between places on a beat (its warning flicker first): over
    // the floor, over a laser pit, high in the air. A portal keeps the cat's speed. The lie: the portal that was safe a moment ago
    // is the one that kills you now. Read the exit, not the portal.
    //  - Cell A: Portal_A's exit sits over Pit_A most of its beat; wait until it's home, go, and jump the pit at once (the cat
    //    comes out still running).
    //  - Cell B: Portal_B stands on glass that shatters under a waiting cat: time the run-up from the solid floor before it.
    //  - Cell C (section 1): the way on is down. Pit_C's floor is Portal_C between two laser strips: jump into it, and fall out
    //    of its exit high over Cell C's far floor, or over Pit_C2 when it's off beat.
    //  - Cell X: Portal_X's exit hangs over a laser lake, and Ferry_X shuttles across it underneath: go in as the ferry comes
    //    under the exit, drop onto it, ride it across.
    //  - Cell Y: Portal_Y's exit sits high over Pit_Y. Run in and the cat falls short; jump in and it keeps its rise and carries
    //    across (the portal keeps the cat's speed).
    //  - The finale: Portal_D's exit hops across the chasm to the door in four jumps, and sits over the door's ledge only at the
    //    end of its beat. Eye_D watches the floor by the entry: wait out of its sight, run in as the exit reaches the ledge.
    static class L033Layout
    {
        internal const float C = 5f, Base = -4f, Top = 7f, Width = 134f, PitDepth = 1.5f, PortalW = .8f, PortalH = 1.5f;
        internal const int Warn = 12;
        // ---- Cell A ----
        internal const float WallA0 = 9f, PitA0 = 12.5f, PitA1 = 15.5f, EntryAX = 7.5f, ExitAX = 11.2f, SpotAX = 14.7f;
        // Portal_A: from each fire (every APeriod from APhase) home AMove, over Pit_A AHold, home again the rest (and before its first
        // fire). A cat that runs straight in comes out over Pit_A.
        internal const int AMove = 30, AHold = 49, APeriod = 120, APhase = 0;
        // ---- Cell B ----
        internal const float GlassB0 = 24f, GlassB1 = 30f, GlassW = 2f, EntryBX = 29f, WallB1 = 31f, PitB0 = 35f, PitB1 = 38.5f, ExitBX = 33f, SpotBX = 36.75f;
        internal const int GlassDelay = 20, BMove = 40, BHold = 60, BPeriod = 140, BPhase = 60;
        internal const float GateX = 40f, Checkpoint1X = 41.5f;
        // ---- Cell C ----
        // Pit_C (x PitC0-PitC1, PitCDepth deep), lethal along the bottom but for Portal_C's entry (PortalCW wide) in its middle.
        internal const float PitC0 = 46f, PitC1 = 54f, PitCDepth = 3f, PortalCW = 2.4f, WallC1 = 55f, PitC20 = 60f, PitC21 = 62.8f;
        internal const float ExitCX = 56f, ExitCY = 3.5f, SpotCX = 61.4f;
        internal const int CMove = 40, CHold = 45, CPeriod = 140, CPhase = 20;
        // ---- Cell X ----
        // The lake (x Lake0-Lake1); Portal_X's entry at EntryXX, its exit fixed over the lake at (ExitXX, ExitXY); Ferry_X (FerryW
        // wide) from FerryHome east FerryRun and back, on its beat (out FerryMove, hold FerryHold, back FerryMove, rest the rest).
        internal const float WallX0 = 72f, Lake0 = 73f, Lake1 = 85f, EntryXX = 70f, ExitXX = 78f, ExitXY = 1.75f, FerryW = 2f, FerryHome = 74.5f, FerryRun = 8f;
        internal const int FerryMove = 80, FerryHold = 40, FerryPeriod = 250, FerryPhase = 0;
        // ---- Cell Y ----
        // Portal_Y's exit sits high over Pit_Y, past Wall_Y: a cat that runs in falls short into the pit; one that jumps in keeps
        // its rise and carries across.
        internal const float WallY0 = 90f, EntryYX = 88.5f, ExitYX = 92.5f, ExitYY = 2.5f, PitY1 = 95.5f;
        // ---- The finale ----
        internal const float EntryDX = 105f, Chasm0 = 106f, Chasm1 = 126f, ExitDX = 109f, ExitDY = 2f, LedgeSpotX = 127.5f, DoorX = 130.5f;
        // Portal_D's exit: home, then every DStep through three spots over the chasm, then over the ledge DHold, then home (over
        // the chasm too) to the end of DPeriod.
        internal const int DStep = 32, DHold = 40, DPeriod = 212, DPhase = 0;
        internal static readonly float[] DSpots = { 114f, 119f, 124f, LedgeSpotX };
        // Eye_D, under the ceiling over the floor by the entry (EyeDX0 to the chasm).
        internal const float EyeDX = 104f, EyeDX0 = 102.2f;
        internal const int EyeAim = 40, EyeLock = 12, EyeFire = 10, EyePeriod = 100, EyeDPhase = 0;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, C, Top);

            // ---------- Cell A ----------
            Block(e, SoloRoomElementKind.Floor, "Floor_A1", 0f, WallA0, Base, 0f);
            Block(e, SoloRoomElementKind.Wall, "Wall_A", WallA0, WallA0 + 1f, Base, C);
            Block(e, SoloRoomElementKind.Floor, "Floor_A2", WallA0 + 1f, PitA0, Base, 0f);
            Pit(e, "Pit_A", PitA0, PitA1, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_A3", PitA1, GlassB0, Base, 0f);
            e.Add(Portal("Portal_A", (EntryAX, PortalH * .5f), (PortalW, PortalH), (ExitAX, PortalH * .5f), (PortalW, PortalH),
                new[] { new Vector2(SpotAX - ExitAX, 0f) }, AMove, AHold, APeriod, APhase));

            // ---------- Cell B ----------
            Pit(e, "Pit_B0", GlassB0, GlassB1, PitDepth);
            for (int i = 0; i < 3; i++)
            {
                float x0 = GlassB0 + i * GlassW;
                e.Add(E(SoloRoomElementKind.CollapsingFloor,$"Glass_B{i + 1}",(x0 + GlassW * .5f,-PitDepth * .5f),(GlassW,PitDepth),settings:new SoloRoomTrapSettings(delayTicks:GlassDelay)));
            }
            Block(e, SoloRoomElementKind.Wall, "Wall_B", GlassB1, WallB1, Base, C);
            Block(e, SoloRoomElementKind.Floor, "Floor_B2", WallB1, PitB0, Base, 0f);
            Pit(e, "Pit_B", PitB0, PitB1, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_B3", PitB1, PitC0, Base, 0f);
            e.Add(Portal("Portal_B", (EntryBX, PortalH * .5f), (PortalW, PortalH), (ExitBX, PortalH * .5f), (PortalW, PortalH),
                new[] { new Vector2(SpotBX - ExitBX, 0f) }, BMove, BHold, BPeriod, BPhase));

            // ---------- Cell C ----------
            // Pit_C: its floor PitCDepth down; laser strips along it either side of Portal_C's entry.
            float floorC = -PitCDepth, mid = (PitC0 + PitC1) * .5f;
            e.Add(E(SoloRoomElementKind.PitBottom,"Pit_C",(mid,(Base + floorC) * .5f),(PitC1 - PitC0,floorC - Base)));
            e.Add(E(SoloRoomElementKind.Hazard,"Pit_C_LaserW",((PitC0 + mid - PortalCW * .5f - .2f) * .5f,floorC + .15f),(mid - PortalCW * .5f - .2f - PitC0,.3f)));
            e.Add(E(SoloRoomElementKind.Hazard,"Pit_C_LaserE",((mid + PortalCW * .5f + .2f + PitC1) * .5f,floorC + .15f),(PitC1 - mid - PortalCW * .5f - .2f,.3f)));
            Block(e, SoloRoomElementKind.Wall, "Wall_C", PitC1, WallC1, Base, C);
            Block(e, SoloRoomElementKind.Floor, "Floor_C2", WallC1, PitC20, Base, 0f);
            Pit(e, "Pit_C2", PitC20, PitC21, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_X1", PitC21, WallX0, Base, 0f);
            e.Add(Portal("Portal_C", (mid, floorC + .2f + PortalH * .5f), (PortalCW, PortalH), (ExitCX, ExitCY), (PortalW, PortalH),
                new[] { new Vector2(SpotCX - ExitCX, 0f) }, CMove, CHold, CPeriod, CPhase));

            // ---------- Cell X ----------
            Block(e, SoloRoomElementKind.Wall, "Wall_X", WallX0, Lake0, Base, C);
            Pit(e, "Lake_X", Lake0, Lake1, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_Y1", Lake1, WallY0, Base, 0f);
            e.Add(E(SoloRoomElementKind.Portal, "Portal_X", (EntryXX, PortalH * .5f), (PortalW, PortalH), (ExitXX, ExitXY), (PortalW, PortalH),
                settings: new SoloRoomTrapSettings(new PortalSettings(), new SoloRoomTrapSettings(delayTicks: 0))));
            e.Add(E(SoloRoomElementKind.MovingTrap,"Ferry_X",(FerryHome,-.25f),(FerryW,.5f),settings:new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(FerryRun, 0f) }),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks:FerryMove,holdTicks:FerryHold,returnTicks:FerryMove,repeatMode:TrapRepeatMode.Periodic,periodTicks:FerryPeriod,
                        phaseTicks:FerryPhase,cooldownTicks:2 * FerryMove + FerryHold,movingKind:MovingTrapKind.Solid)))));

            // ---------- Cell Y ----------
            Block(e, SoloRoomElementKind.Wall, "Wall_Y", WallY0, WallY0 + 1f, Base, C);
            Pit(e, "Pit_Y", WallY0 + 1f, PitY1, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_D1", PitY1, Chasm0, Base, 0f);
            // Its entry is tall, so a jumping cat goes in as well as a running one.
            e.Add(E(SoloRoomElementKind.Portal, "Portal_Y", (EntryYX, 1.3f), (PortalW, 2.6f), (ExitYX, ExitYY), (PortalW, 2.6f),
                settings: new SoloRoomTrapSettings(new PortalSettings(), new SoloRoomTrapSettings(delayTicks: 0))));

            // ---------- The finale ----------
            Pit(e, "Chasm", Chasm0, Chasm1, PitDepth);
            Block(e, SoloRoomElementKind.Floor, "Floor_D2", Chasm1, Width, Base, 0f);
            var spots = new Vector2[DSpots.Length];
            for (int i = 0; i < spots.Length; i++) spots[i] = new Vector2(DSpots[i] - ExitDX, 0f);
            e.Add(Portal("Portal_D", (EntryDX, PortalH * .5f), (PortalW, PortalH), (ExitDX, ExitDY), (PortalW, PortalH), spots, DStep * DSpots.Length, DHold, DPeriod, DPhase));
            {
                float muzzle = C - .15f;
                var zone = new Rect(EyeDX0 - EyeDX, -muzzle, Chasm0 - EyeDX0, 1.2f);
                e.Add(E(SoloRoomElementKind.Turret,"Eye_D",(EyeDX,muzzle),(.6f,.6f),settings:new SoloRoomTrapSettings(
                    new TurretSettings(EyeAim, EyeLock, EyeFire, ceilingMount: true, aimZone: zone),
                    new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:EyePeriod,phaseTicks:EyeDPhase,cooldownTicks:EyeAim + EyeLock + EyeFire))));
            }

            var sections = new[] {
                CheckpointSection.Start("Near", new Vector2(2f, 0f), new[] { "Portal_A", "Glass_B1", "Glass_B2", "Glass_B3", "Portal_B" }),
                new CheckpointSection("Deep", new Vector2(Checkpoint1X, 0f), new Rect(GateX, 0f, .4f, 1.5f), new[] { "Portal_C", "Portal_X", "Ferry_X", "Portal_Y", "Portal_D", "Eye_D" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A portal: its entry, its exit's home, and the exit glitching through `spots` (offsets from home): from each fire (every
        // period from `phase`) home for move / spots.Length, then a spot each move / spots.Length, the last held `hold`, then home.
        static SoloRoomElement Portal(string name, (float x, float y) entry, (float x, float y) entrySize, (float x, float y) exit, (float x, float y) exitSize,
            Vector2[] spots, int move, int hold, int period, int phase) =>
            E(SoloRoomElementKind.Portal, name, entry, entrySize, exit, exitSize, new SoloRoomTrapSettings(new PortalSettings(),
                new(new PathSettings(spots, glitch: true, warnTicks: Warn),
                    new SoloRoomTrapSettings(moveTicks: move, holdTicks: hold, repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: move + hold))));

        // A laser pit: its floor `depth` down, lethal along the bottom.
        static void Pit(List<SoloRoomElement> e, string name, float x0, float x1, float depth)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, name, ((x0 + x1) * .5f, (Base - depth) * .5f), (x1 - x0, -depth - Base)));
            e.Add(E(SoloRoomElementKind.Hazard, name + "_Laser", ((x0 + x1) * .5f, -depth + .15f), (x1 - x0, .3f)));
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
