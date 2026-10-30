using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-213 (replaces D-169/D-195; the developer, 2026-10-28: of the spinners, "Lets remove these traps from level 25. Its been
    // too many levels in a row wiht the same mechanics. Lets make the level longer, and use different traps, and mechanics that
    // can kill you", then "follow the recommendation"): level 25, "Rhythm", rebuilt with none of L021-L024's movers (no spinners,
    // pistons, pendulums or turrets). Down a broken slope, across a valley on vines, up a chimney to a door that won't wait.
    //  - Descent (section 0): floating steps drop 13 u to a spike pit far below. Step_1 and Step_3 crumble soon after a cat lands;
    //    Eye_D, a laser turret in the descent's roof (D-215: it tracks the cat, locks, fires), watches the whole slope; the obvious
    //    hop from Step_3 is Fake_4, a step that isn't there; the real Step_4 is a long running jump further on.
    //  - Valley (section 1): V1-V6 hang from the valley's roof over its spikes. V1 snaps VineFirst after a cat takes it, each next
    //    VineWave after the one before, so a cat must keep leaping vine to vine; Eye_V, in the valley's roof, shoots at it on the
    //    vines.
    //  - Climb (section 2): along the valley's east floor to the chimney (D-214: no cloud); up the chimney by wall jumps between its
    //    two mossy faces (D-216: no laser there); onto the ledge, where the door runs (D-219: the chase, below) (Retreat;
    //    D-217: no laser there). D-215 (the developer: "Arrow and laser at time? No remove the arrows … not following the cat"):
    //    no arrows, and every laser is a turret that follows the cat.
    static class L025Layout
    {
        internal const float Pit = 1f, Start = 20f, Base = -1f, RoofTop = 25f, Width = 134f;
        // The steps: (name, x0, x1, top, crumbles).
        internal static readonly (string name, float x0, float x1, float top, bool crumbles)[] Steps = {
            ("Step_1", 6f, 9f, 17.5f, true), ("Step_2", 11f, 14f, 15f, false), ("Step_3", 16f, 19f, 12.5f, true),
            ("Step_4", 24f, 27f, 9.5f, false), ("Step_5", 29f, 32f, 7f, false),
        };
        internal const int CrumbleDelay = 14;
        // D-215: the laser turrets (as L021's, D-202/D-203): each aims EyeAim, locks EyeLock (its warning), fires EyeFire and rests
        // EyeRest, every EyePeriod, over its own zone only.
        internal const int EyeAim = 50, EyeLock = 12, EyeFire = 10, EyeRest = 33, EyePeriod = EyeAim + EyeLock + EyeFire + EyeRest;
        // The valley: its roof's underside, the vines' x and their bottom, the snap wave, the rain's beat.
        internal const float ValleyRoof = 12f, VineBottom = 5f, ValleyE = 51f, EastFloor = 4f, EastRoof = 9.5f;
        internal static readonly float[] VineX = { 34.5f, 37.3f, 40.1f, 42.9f, 45.7f, 48.5f };
        internal const int VineFirst = 70, VineWave = 45;
        // The chimney: its west face (Pillar_W's moss) and east face (Wall_E's), its bottom under Pillar_W, the ledge on top.
        internal const float ChimneyW = 66f, ChimneyE = 68f, PillarFoot = 6f, Ledge = 15f, ChimneyRoof = 18.5f;
        // D-219 (the developer: "make the last strip super long, and have the player chase the door … the floor starts moving up
        // towards the ceiling to kill the cat and there is a specific combination of running, jumping stopping and running and then
        // jumping again to get to the exit"): the door runs DoorSlide east over DoorMove once a cat passes x RetreatX, a little
        // faster than a cat runs, and stops at the strip's end behind the last rising section. A door slides in one straight line.
        internal const float DoorX = 75f, RetreatX = 73.5f, DoorSlide = 53f;
        internal const int DoorMove = 380;
        // The strip's rising floor sections (D-219), each 0.5 thick and flush with the ledge, rising into Roof_Ledge (their crush
        // partner) until less than a cat is left: (name, x0, x1, delay after the trigger, ticks to rise, its trigger's x0-x1 or the
        // section itself, periodic beat (0: once a cat sets it off) and phase). A cat running flat out leaves each long one about 15
        // ticks before it would be crushed.
        internal static readonly (string name, float x0, float x1, int delay, int rise, float t0, float t1, int period, int phase)[] Risers = {
            ("Rise_Run1", 78f, 86f, 12, 80, 78f, 86f, 0, 0),        // run: it rises under a cat, slowly; only a cat that keeps running is off it in time
            ("Rise_Jump1", 90f, 92.5f, 2, 10, 90f, 92.5f, 0, 0),    // jump: touched, it slams up at once; jump clean over it
            ("Rise_Wait", 96f, 98f, 0, 15, 0f, 0f, 120, 0),        // stop: it rises and falls on its beat; wait short of it, cross while it's down
            ("Rise_Run2", 100f, 110f, 10, 95, 100f, 110f, 0, 0),     // run again, a longer stretch
            ("Rise_Jump2", 125f, 127f, 0, 8, 123f, 125f, 0, 0),     // jump: it slams up as a cat comes near it; over it, into the door
        };

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop + 1f) * .5f),(1f,RoofTop + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + RoofTop + 1f) * .5f),(1f,RoofTop + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,Start),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,Ledge + .75f),(.6f,1.5f)),
            };

            // ---- The roofs: high over the descent, the valley's lower (the vines hang from it), the east floor's lower still (the
            // cloud under it), the chimney's and the ledge's ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Descent", 0f, 32f, RoofTop - 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Valley", 32f, ValleyE, ValleyRoof, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_East", ValleyE, 64.75f, EastRoof, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Ledge", 64.75f, Width, ChimneyRoof, RoofTop);

            // ---- The ground: the start ledge, the spike pit under the slope and the valley, the east floor and the ledge ----
            Block(elements, SoloRoomElementKind.Floor, "Start", 0f, 4f, Base, Start);
            Block(elements, SoloRoomElementKind.Floor, "Pit_Floor", 4f, ValleyE, Base, Pit);
            Spikes(elements, "Pit", 4f, ValleyE, Pit);
            Block(elements, SoloRoomElementKind.Floor, "East_Floor", ValleyE, ChimneyE, Base, EastFloor);
            Block(elements, SoloRoomElementKind.Floor, "Wall_E", ChimneyE + .25f, 70f, EastFloor, Ledge);
            // The ledge (D-219: the strip, x 70 to the room's end), cut where each rising section sits (a seat under it).
            float cursor = 70f;
            foreach (var r in Risers)
            {
                Block(elements, SoloRoomElementKind.Floor, "Ledge_" + r.name, cursor, r.x0, Base, Ledge);
                Block(elements, SoloRoomElementKind.Floor, r.name + "_Seat", r.x0, r.x1, Base, Ledge - .5f);
                cursor = r.x1;
            }
            Block(elements, SoloRoomElementKind.Floor, "Ledge_End", cursor, Width, Base, Ledge);
            Block(elements, SoloRoomElementKind.Floor, "Wall_E_Foot", ChimneyE, 70f, Base, EastFloor);

            // ---------- Descent ----------
            foreach (var (name, x0, x1, top, crumbles) in Steps)
            {
                if (crumbles) elements.Add(E(SoloRoomElementKind.CollapsingFloor, name, ((x0 + x1) * .5f, top - .5f), (x1 - x0, 1f), settings: new SoloRoomTrapSettings(delayTicks: CrumbleDelay)));
                else Block(elements, SoloRoomElementKind.Floor, name, x0, x1, top - 1f, top);
            }
            // Fake_4: the step at the obvious hop from Step_3, which isn't there.
            elements.Add(E(SoloRoomElementKind.FakePlatform, "Fake_4", (21.5f, 9.5f), (2f, 1f)));
            // Eye_D (D-215, was Laser_2 and Laser_5): in the descent's roof, over the whole slope.
            Turret(elements, "Eye_D", 18f, RoofTop - 1f, new Rect(4f, Pit, 28f, RoofTop - 1f - Pit), 0);

            // ---------- Valley ----------
            for (int i = 0; i < VineX.Length; i++)
            {
                var snap = i == 0 ? new SoloRoomTrapSettings(delayTicks: VineFirst) : new SoloRoomTrapSettings(delayTicks: VineWave, revealDelayTicks: VineWave, triggerSource: TrapTriggerSource.Chain, chainSource: "V" + i);
                elements.Add(E(SoloRoomElementKind.Vine, "V" + (i + 1), (VineX[i], (ValleyRoof + VineBottom) * .5f), (.6f, ValleyRoof - VineBottom), settings: snap));
            }
            // Eye_V (D-215, was Rain_1-3, arrows): in the valley's roof, over the vines.
            Turret(elements, "Eye_V", 41.5f, ValleyRoof, new Rect(32f, Pit, ValleyE - 32f, ValleyRoof - Pit), 35);

            // ---------- Climb ----------
            // The chimney (x 66-68): Pillar_W hangs from the roof down to PillarFoot (the cat walks in under it), mossy on its east face;
            // Wall_E rises from the floor to the ledge, mossy on its west face (its top band plain, to climb over).
            Block(elements, SoloRoomElementKind.Wall, "Pillar_W", 64.75f, ChimneyW - .25f, PillarFoot, ChimneyRoof);
            elements.Add(E(SoloRoomElementKind.GripWall, "Pillar_Moss", (ChimneyW - .125f, (PillarFoot + ChimneyRoof) * .5f), (.25f, ChimneyRoof - PillarFoot)));
            elements.Add(E(SoloRoomElementKind.GripWall, "Wall_Moss", (ChimneyE + .125f, (EastFloor + Ledge - .5f) * .5f), (.25f, Ledge - .5f - EastFloor)));
            elements.Add(E(SoloRoomElementKind.Wall, "Wall_E_Hi", (ChimneyE + .125f, Ledge - .25f), (.25f, .5f)));
            // D-216 (the developer: "Remove the laser in this section it is impossible to pass"): no turret over the chimney.
            // The door runs DoorSlide east along the strip as a cat comes within RetreatX (D-217: no laser over the ledge; D-219: the
            // chase).
            elements.Add(E(SoloRoomElementKind.DoorRetreat, "Retreat", (RetreatX, Ledge + 1.5f), (.4f, 3f), settings: new SoloRoomTrapSettings(moveTicks: DoorMove, offset: new Vector2(DoorSlide, 0f))));
            // The rising floor sections (D-219).
            float crush = ChimneyRoof - Ledge - .4f;   // up until 0.4 is left under the roof: less than a cat
            foreach (var r in Risers)
            {
                float x = (r.x0 + r.x1) * .5f;
                var floor = new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: "Roof_Ledge");
                var timing = r.period > 0
                    ? new SoloRoomTrapSettings(offset: new Vector2(0f, crush), moveTicks: r.rise, holdTicks: 30, returnTicks: r.rise, repeatMode: TrapRepeatMode.Periodic,
                        periodTicks: r.period, phaseTicks: r.phase, cooldownTicks: 2 * r.rise + 30, movingKind: MovingTrapKind.Solid)
                    : new SoloRoomTrapSettings(delayTicks: r.delay, offset: new Vector2(0f, crush), moveTicks: r.rise, holdTicks: 60, returnTicks: 60, repeatMode: TrapRepeatMode.Rearm,
                        cooldownTicks: r.rise + 60 + 60, movingKind: MovingTrapKind.Solid);
                elements.Add(r.period > 0
                    ? E(SoloRoomElementKind.MovingTrap, r.name, (x, Ledge - .25f), (r.x1 - r.x0, .5f), settings: new SoloRoomTrapSettings(floor, timing))
                    : E(SoloRoomElementKind.MovingTrap, r.name, (x, Ledge - .25f), (r.x1 - r.x0, .5f), ((r.t0 + r.t1) * .5f, Ledge + .3f), (r.t1 - r.t0, .6f), new SoloRoomTrapSettings(floor, timing)));
            }

            var sections = new[] {
                CheckpointSection.Start("Descent", new Vector2(2f, Start), new[] { "Step_1", "Step_3", "Fake_4", "Eye_D" }),
                new CheckpointSection("Valley", new Vector2(30.5f, 7f), new Rect(29.3f, 7f, .3f, 1.5f), new[] { "V1", "V2", "V3", "V4", "V5", "V6", "Eye_V" }),
                new CheckpointSection("Climb", new Vector2(53f, EastFloor), new Rect(52.2f, EastFloor, .3f, EastRoof - EastFloor), new[] { "Retreat", "Rise_Run1", "Rise_Jump1", "Rise_Wait", "Rise_Run2", "Rise_Jump2" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // D-215: a laser turret hung from a roof's underside `under` at x (its muzzle just below), ceiling-mounted: it tracks a cat
        // below it, in sight and inside `zone` (room coordinates), locks, and fires along the locked line (as L021's).
        static void Turret(List<SoloRoomElement> e, string name, float x, float under, Rect zone, int phase) =>
            e.Add(E(SoloRoomElementKind.Turret, name, (x, under - .15f), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, EyeLock, EyeFire, ceilingMount: true, aimZone: new Rect(zone.x - x, zone.y - (under - .15f), zone.width, zone.height)),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: EyePeriod, phaseTicks: phase, cooldownTicks: EyeAim + EyeLock + EyeFire))));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));
    }
}
