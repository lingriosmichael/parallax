using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-200: level 21, "Vent Tower" (the developer, 2026-10-25, after D-198: "Fully redo level 21. It loks awfull"; concept picked
    // from three). A tower climbed on thermal vents: four storeys 5.5 apart (D-201: "the gap between the two levels needs to be
    // bigger"; the vents throw harder), hung alternately from the west and the east wall, each ending in a vent that throws the cat
    // up through the open column at its end onto the next. No glitch walls (D-201: levels 21-30 have none).
    //  - Ground (section 0): the cat starts on a ledge over a spike floor, beside V1. V1 erupts at once: ride it.
    //  - Storey A (walked east): the Gap over the spike floor; Swing_A, a pendulum hung from storey B, sweeps the walk (pass
    //    under it while it's swung out); V2 at the east end.
    //  - Storey B (walked west): Swing_B sweeps the walk just before Spikes_B (pass under, jump them); Spin_B turns one arm across
    //    the walk (pass under while it's up); V3 at the west end.
    //  - Storey C (walked east): Stone_C, a phase platform (D-201) in the middle of Spikes_C, appears, flickers and goes in one
    //    spot (hop on as it appears, off before it goes); then V4 on a pad in a spike pit. The lie: V4 looks like every vent, but
    //    GD in the roof above blows down on every other eruption, and a cat hugging the top storey's face falls past the pad onto
    //    Spikes_V4; and the turret, Eye, sees the pad and the column, so wait out of its sight under the top storey for the
    //    eruption GD leaves alone.
    //  - Top (walked west): the floor falls away into the trench behind the cat tile by tile from its first step; Ferry carries it
    //    over the last pit to the door while Eye keeps shooting (step aside while it locks).
    //  - D-202 (the developer: "now the floors dont have any good challeneges. So put these laser things on each floor"): storeys A,
    //    B and C each have an aiming turret, Eye_A, Eye_B and Eye_C, hung under the storey above at the end the cat walks away from,
    //    so its beam runs low along the walk: a cat standing in its sight when it fires dies, and jumping as it locks clears it.
    static class L021Layout
    {
        // Walk lines: the ground, the storeys, the top; the storeys' undersides (the top storey is thicker, over its trenches); the roof.
        internal const float G = 0f, A = 5.5f, B = 11f, C = 16.5f, T = 22f, AU = 4.5f, BU = 10f, CU = 15.5f, TU = 20.3f, R = 25.8f, RoofTop = 27.8f;
        internal const float PitFloor = -1f, TrenchFloor = TU + .5f, PadW = 26.9f, PadE = 28.3f, TopE = 25.8f;
        // The vents: x of their centres (about 4.4 from the walls for their launch envelopes, R2), their launch (D-201: about 6 up,
        // for storeys 5.5 apart), period and first eruption; GD, the lie, every other V4 eruption.
        internal const float V1X = 4.5f, V2X = 27.5f, V3X = 4.5f, V4X = 27.6f, VLaunch = 16.5f, GDX = 26.55f;
        internal const int VPeriod = 120, V1Erupt = 40, V2Erupt = 100, V3Erupt = 60, V4Erupt = 20, GDPeriod = 2 * VPeriod, GDErupt = V4Erupt + 4;
        // Stone_C: there StoneOn ticks of every StonePeriod, flickering its last StoneWarn.
        internal const int StonePeriod = 140, StoneOn = 80, StoneWarn = 12, StonePhase = 0;
        // The pendulums.
        internal const int SwingAPeriod = 180, SwingBPeriod = 170;
        // The turrets (D-202, D-203): each aims EyeAim, locks EyeLock (its tell), fires EyeFire and rests EyeRest (D-203: "make it
        // recharge faster"; 33 is the least D-056's periodic slack allows), every EyePeriod. Each hangs from a ceiling and tracks only a cat below it, in its sight and inside
        // its zone (D-203: "it should only aim below", "the range of the turrets needs to be the whole standable area of the current
        // floor"): its storey from wall or column to column, floor to the underside above. Storey C's ends at V4's spike pit, so a
        // cat on the vent's pad can't be shot.
        internal const int EyeAim = 50, EyeLock = 12, EyeFire = 10, EyeRest = 33, EyePeriod = EyeAim + EyeLock + EyeFire + EyeRest;
        // Where each hangs, and its phase: its first fire (tuned against the solution; each starts after the cat has left its
        // storey's checkpoint, and never fires as the cat stands on or rides a vent in its zone).
        internal const float EyeAX = 15f, EyeBX = 11f, EyeCX = 11f, EyeX = 14f;
        internal const int EyeAPhase = 130, EyeBPhase = 556, EyeCPhase = 999, EyePhase = 13;
        // Each turret's first lethal tick in its period.
        internal const int EyeShot = EyeAim + EyeLock;
        // The top's falling floor: TileCount tiles from the trench's east end, each TileStep after the one before (slower than a run).
        internal const int TileCount = 5, TileFirstDelay = 10, TileStep = 20;
        // The trench ends clear of GD's launch envelope (R2).
        internal const float TileWidth = 1.75f, TrenchE = 21f, TrenchW = TrenchE - TileCount * TileWidth, PitW = 4.5f, PitE = 11f;
        internal const int FerryDelay = 10, FerryMove = 150;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(-4f + RoofTop) * .5f),(1f,RoofTop + 4f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(-4f + RoofTop) * .5f),(1f,RoofTop + 4f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.9f,G),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(1.5f,T + .75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, 32f, R, RoofTop);

            // ---- Ground: the start ledge and the spike floor ----
            Block(elements, SoloRoomElementKind.Floor, "Start", 0f, 5.8f, -4f, G);
            Block(elements, SoloRoomElementKind.Floor, "Ground", 5.8f, 32f, -4f, PitFloor);
            Spikes(elements, "Spikes_G", 5.8f, 32f, PitFloor);
            elements.Add(E(SoloRoomElementKind.Geyser,"V1",(V1X,G - .15f),(1f,.3f),settings:Vent(V1Erupt)));

            // ---- Storey A (hung from the east wall; the Gap at x 17-20) ----
            Block(elements, SoloRoomElementKind.Floor, "A_W", 5.8f, 17f, AU, A);
            Block(elements, SoloRoomElementKind.Floor, "A_E", 20f, 32f, AU, A);
            elements.Add(E(SoloRoomElementKind.Spinner,"Swing_A",(23.5f,BU - .4f),(.5f,.5f),settings:new SoloRoomTrapSettings(SpinnerSettings.Pendulum(3.85f, 60f, SwingAPeriod),new SoloRoomTrapSettings())));
            elements.Add(E(SoloRoomElementKind.Geyser,"V2",(V2X,A - .15f),(1f,.3f),settings:Vent(V2Erupt)));
            Turret(elements, "Eye_A", EyeAX, BU, new Rect(5.8f, A, 32f - 5.8f, BU - A), EyeAPhase);

            // ---- Storey B (hung from the west wall) ----
            Block(elements, SoloRoomElementKind.Floor, "B_Floor", 0f, 26.2f, BU, B);
            elements.Add(E(SoloRoomElementKind.Spinner,"Swing_B",(17f,CU - .4f),(.5f,.5f),settings:new SoloRoomTrapSettings(SpinnerSettings.Pendulum(3.85f, 60f, SwingBPeriod),new SoloRoomTrapSettings())));
            Spikes(elements, "Spikes_B", 11.5f, 13f, B);
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_B",(7.5f,(B + CU) * .5f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,2f,3f),new SoloRoomTrapSettings())));
            elements.Add(E(SoloRoomElementKind.Geyser,"V3",(V3X,B - .15f),(1f,.3f),settings:Vent(V3Erupt)));
            Turret(elements, "Eye_B", EyeBX, CU, new Rect(0f, B, 26.2f, CU - B), EyeBPhase);

            // ---- Storey C (hung from the east wall) ----
            Block(elements, SoloRoomElementKind.Floor, "C_Floor", 5.8f, 24.7f, CU, C);
            Turret(elements, "Eye_C", EyeCX, TU, new Rect(5.8f, C, 24.7f - 5.8f, TU - C), EyeCPhase);
            Spikes(elements, "Spikes_C", 17f, 21f, C);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Stone_C",(19f,C + 1.1f),(1.5f,.3f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, phaseOnTicks: StoneOn, phaseWarnTicks: StoneWarn),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: StonePeriod, phaseTicks: StonePhase, cooldownTicks: StoneOn, movingKind: MovingTrapKind.Solid))));
            // The spike pit at the storey's east end (its floor 0.5 under the walk line) and V4's pad standing in it.
            Block(elements, SoloRoomElementKind.Floor, "PitC_W", 24.7f, PadW, CU, C - .5f);
            Block(elements, SoloRoomElementKind.Floor, "PitC_E", PadE, 32f, CU, C - .5f);
            Spikes(elements, "Spikes_V4", 24.7f, PadW, C - .5f);
            Spikes(elements, "Spikes_V4E", PadE, 32f, C - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Pad", PadW, PadE, CU, C);
            elements.Add(E(SoloRoomElementKind.Geyser,"V4",(V4X,C - .15f),(1f,.3f),settings:Vent(V4Erupt)));
            // GD, in the roof between V4's column and the top storey's face, 1.4 wide, down to y 21.5: it blows a cat riding V4 up
            // the face down past the pad onto Spikes_V4.
            elements.Add(E(SoloRoomElementKind.Geyser,"GD",(GDX,R + .15f),(1.4f,.3f),settings:new SoloRoomTrapSettings(
                new GeyserSettings(GeyserDirection.Down, columnWidth: 1.4f, columnHeight: R - 21.5f),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: GDPeriod, phaseTicks: Mod(GDErupt - GeyserMath.DefaultTellTicks, GDPeriod)))));
            // Eye, in the roof over the top storey's middle (D-203: off the Summit checkpoint, and out of sight of storey C).
            Turret(elements, "Eye", EyeX, R, new Rect(0f, T, TopE, R - T), EyePhase);

            // ---- Top (hung from the west wall): T_W holds the door; the Pit; T_M; the Trench under the bridge; T_E ----
            Block(elements, SoloRoomElementKind.Floor, "T_W", 0f, PitW, TU, T);
            Pit(elements, "Pit", PitW, PitE);
            Block(elements, SoloRoomElementKind.Floor, "T_M", PitE, TrenchW, TU, T);
            Pit(elements, "Trench", TrenchW, TrenchE);
            Block(elements, SoloRoomElementKind.Floor, "T_E", TrenchE, TopE, TU, T);
            // The bridge: the cat's first step off T_E (Bridge_1's trigger, over the trench from its floor to the roof) sets
            // Bridge_1 off; each next goes TileStep later, a little slower than a run, and lands with its top on the trench floor,
            // under the spikes' tips. Five tiles, and the trigger clear of T_M: an overlap query returns at most eight colliders,
            // and with more under the trigger the cat went unseen.
            for (int i = 0; i < TileCount; i++)
            {
                float x1 = TrenchE - i * TileWidth, cx = x1 - TileWidth * .5f;
                const float travel = T - TrenchFloor;
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.FallingBlock, "Bridge_1", (cx, T - .2f), (TileWidth, .4f), ((TrenchW + .02f + TrenchE) * .5f, (TrenchFloor + R) * .5f), (TrenchE - TrenchW - .02f, R - TrenchFloor),
                        new SoloRoomTrapSettings(delayTicks:TileFirstDelay,unitsPerTick:.25f,travelDistance:travel))
                    : E(SoloRoomElementKind.FallingBlock, "Bridge_" + (i + 1), (cx, T - .2f), (TileWidth, .4f),
                        settings:new SoloRoomTrapSettings(delayTicks:TileStep,unitsPerTick:.25f,travelDistance:travel,triggerSource:TrapTriggerSource.Chain,chainSource:"Bridge_" + i)));
            }
            // Ferry: home at the Pit's east end, flush with T_M; FerryDelay after a cat steps on, west 4 to T_W, over FerryMove.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry",(9.75f,T - .25f),(2.5f,.5f),(9.75f,(T + R) * .5f),(2.5f,R - T),new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(-(PitE - 2.5f - PitW), 0f) }),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(delayTicks:FerryDelay,moveTicks:FerryMove,holdTicks:600,movingKind:MovingTrapKind.Solid)))));

            var sections = new[] {
                CheckpointSection.Start("Base", new Vector2(2.9f, G), new[] { "V1" }),
                new CheckpointSection("Storey_A", new Vector2(6.7f, A), new Rect(.1f, AU + .3f, 5.6f, .2f), new[] { "Swing_A", "V2", "Eye_A" }),
                new CheckpointSection("Storey_B", new Vector2(25.4f, B), new Rect(24.4f, B, .2f, CU - B), new[] { "Swing_B", "Spin_B", "V3", "Eye_B" }),
                new CheckpointSection("Storey_C", new Vector2(6.6f, C), new Rect(7.1f, C, .2f, TU - C), new[] { "Stone_C", "V4", "GD", "Eye_C" }),
                new CheckpointSection("Summit", new Vector2(25.05f, T), new Rect(24.4f, T, .2f, R - T), Bridge(new[] { "Ferry", "Eye" })),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static string[] Bridge(string[] also)
        {
            var names = new List<string>();
            for (int i = 1; i <= TileCount; i++) names.Add("Bridge_" + i);
            names.AddRange(also);
            return names.ToArray();
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A recess in the top storey: its floor (TU to TrenchFloor, a floor the trigger coverage stands on) and the spikes on it.
        static void Pit(List<SoloRoomElement> e, string id, float x0, float x1)
        {
            e.Add(E(SoloRoomElementKind.Floor, id + "_Floor", ((x0 + x1) * .5f, (TU + TrenchFloor) * .5f), (x1 - x0, TrenchFloor - TU)));
            Spikes(e, id + "_Spikes", x0, x1, TrenchFloor);
        }

        // D-202/D-203: a turret hung from the underside `under` of the storey above (its muzzle just below it), ceiling-mounted, aiming
        // over `zone` (room coordinates).
        static void Turret(List<SoloRoomElement> e, string name, float x, float under, Rect zone, int phase) =>
            e.Add(E(SoloRoomElementKind.Turret, name, (x, under - .15f), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, EyeLock, EyeFire, ceilingMount: true, aimZone: new Rect(zone.x - x, zone.y - (under - .15f), zone.width, zone.height)),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: EyePeriod, phaseTicks: phase, cooldownTicks: EyeAim + EyeLock + EyeFire))));

        // A vent (tell 25, erupt 40, column 1.5, launch VLaunch) whose eruption starts at room tick `erupt`, and every VPeriod.
        static SoloRoomTrapSettings Vent(int erupt) =>
            new(new GeyserSettings(GeyserDirection.Up, launchSpeed: VLaunch), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: VPeriod,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, VPeriod)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
