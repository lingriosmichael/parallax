using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-262 (the developer, 2026-10-30: "review 21-30 and look at the changes I requested ... then start building the first 5
    // levels for obsidian"): level 31, "Firewall", the first Obsidian Void level.
    // One long data corridor (4 u tall, floor and ceiling always in view) crossed by pulse gates (PAX-112), with a purge beam (a
    // scanner, PAX-113) sweeping it from behind a little slower than a run. The gates teach "wait until it's off"; the purge makes
    // every wait cost, so the level is a budget of waits you have to spend in the right places.
    //  - Hall 1 (section 0): Purge_1 sets off as the level starts. Gate_1 is on as a running cat arrives (wait for it). Eye_1, an
    //    aiming turret, watches the floor in front of Gate_2, the obvious place to wait for it: wait in the gap just past Gate_1
    //    instead, and run through both. Stone_1, a hard-light step over a laser pit, blinks (a phase platform): hop on as it's lit,
    //    off before it goes. Gate_3 and Gate_4 run in opposite phase with a pocket between them: in, hold still, out. The glass
    //    bridge shatters under a cat that stops on it, and Gate_5 stands at its end: time Gate_5 from the solid ground before the
    //    bridge (no inverter on it since the developer's play-test, 2026-10-31: too hard).
    //  - Hall 2 (section 1): Three plates turn over in turn over a long laser pit (flip platforms, PAX-115: a running light), their
    //    spiked undersides coming up: hop from each to the next as the next turns back. Gate_6 stands at the landing.
    //  - The finale, the firewall: five gates in a row switch on one after another from the door toward the cat, faster than it
    //    runs. Run two, stop dead in the pocket, run the last three toward the door.
    // v2 (D-276, the developer, 2026-10-31: "a bit easy", "I dont see troll deaths anywhere"): Level Devil moments and two new traps.
    //  - The pocket troll: the right part of the pocket's floor (Trap_P, by Gate_4, where a cat naturally stops) is a trapdoor over
    //    a laser pit. Stop near Gate_3.
    //  - Trident_1 (PAX-123) in the roof over the bridge's foot, where v1 waited for Gate_5: wait back at x 39 and run its fan and
    //    Gate_5 together.
    //  - One chaser, all level (D-276 amendments 1-3): Purge_1, a pale laser line through a pivot at mid-height, each half to the
    //    first stone (end to end, no gaps). Always near: never more than ChaseLag behind the cat (it speeds up), creeping when closer
    //    than ChaseNear; its speed changes by zone. From SwingFromX (the end of Hall 1) it swings: one way, back past upright, then
    //    further, wider each time. A full-length beam can't be jumped, so stone is the answer: tables (low slabs) where the cat must
    //    wait, under which no swing reaches.
    //  - Hall 3, the overtake: it surges past the cat while the cat shelters in the Tunnel, then parks, swinging, over Table_P. Get
    //    past it under Table_P: in as its lower beam swings forward, out the far side as it swings back.
    //  - The door troll: as the cat comes to the door it slides back (Retreat) past Gate_7, timed for a cat that runs on.
    static class L031Layout
    {
        // The corridor: floor top 0, ceiling underside C, the slabs Base under it and Top over it.
        internal const float C = 4f, Base = -2f, Top = 6f, Width = 156f, PitDepth = 1.5f;
        // Pulse gates: warning WarnTicks, then lethal OnTicks, every period from its phase; 0.3 wide, floor to ceiling.
        internal const int GateWarn = 12;
        internal const float GateW = .3f;
        // ---- Hall 1 ----
        // Purge_1, the one chaser (ChaserTrap): its pivot at mid-height, home Purge1Home; fired at the start (its trigger covers the
        // spawn), it warns at home Purge1Warn, then runs east to Purge1End at ChaseSpeed[i] from ChaseZone[i] on, never more than
        // ChaseLag behind the cat (ChaseCatchUp beyond), creeping (ChaseCreep x) closer than ChaseNear.
        internal const float Purge1Home = .6f, Purge1End = 152f, Purge1Speed = .09f, ChaseLag = 9f, ChaseCatchUp = .16f, ChaseNear = 5f, ChaseCreep = .2f;
        internal const int Purge1Warn = 52;
        // Zones: Hall 1 at v1's speed; a lull over the flip plates; the overtake's surge (OvertakeX) and its park (ParkX).
        internal const float OvertakeX = 106f, ParkX = 128f;
        // It parks at ParkX only once it is ahead of the cat (a cat still in front is caught by the surge); once passed, AfterSpeed.
        internal static readonly float[] ChaseZone = { 0f, 59f, 67f, 84f, OvertakeX }, ChaseSpeed = { Purge1Speed, .07f, .035f, .07f, .25f };
        internal const float AfterSpeed = .05f;
        // The swing: from SwingFromX (the end of Hall 1), to each tilt in turn (degrees off upright; + its top forward) at SwingSpeed.
        internal const float SwingFromX = 45f, SwingSpeed = .8f;
        internal static readonly float[] SwingTilts = { 20f, -30f, 40f, -50f, 60f, -60f };
        // Tables: low slabs (bottom TableBottom, top TableTop) a cat waits under, out of every swing.
        internal const float TableBottom = 1f, TableTop = 1.5f;
        // Gate_1 (on as a running cat arrives), Gate_2 (on as a cat that ran straight on from Gate_1 arrives); both every Beat1.
        internal const int Beat1 = 90, Gate1On = 36, Gate1Phase = 38, Gate2On = 36, Gate2Phase = 72;
        internal const float Gate1X = 10f, Gate2X = 21f;
        // Eye_1, under the ceiling over the floor in front of Gate_2 (EyeZone1X0-X1): it watches only a standing cat there.
        internal const float Eye1X = 16f, EyeZone1X0 = 12f, EyeZone1X1 = 20f;
        // Eye_1 locks and fires as a cat that went straight on from the gap past Gate_1 would stand waiting for Gate_2.
        internal const int EyeAim = 40, EyeLock = 12, EyeFire = 10, EyePeriod = 100, Eye1Phase = 45;
        // Pit_1 (x Pit1X0-Pit1X1) and Stone_1 over its middle, lit Stone1On of every StonePeriod from Stone1Phase.
        internal const float Pit1X0 = 24f, Pit1X1 = 30f, StoneW = 1.5f;
        internal const int StonePeriod = 100, Stone1On = 60, StoneWarn = 12, Stone1Phase = 55;
        // The pocket: Gate_3 and Gate_4, each on PocketOn of every PocketBeat, Gate_4 Gate4Phase behind: Gate_4 is on whenever a cat
        // that crossed Gate_3 at a run gets to it, so the only way through is to stop in the pocket.
        internal const float Gate3X = 34f, Gate4X = 37.5f;
        internal const int PocketBeat = 120, PocketOn = 60, Gate3Phase = 0, Gate4Phase = 66;
        // v2: the pocket's trapdoor (x TrapX0-TrapX1, by Gate_4), giving way TrapDelay after a touch; the safe stop is PocketStopX.
        internal const float TrapX0 = 36.1f, TrapX1 = 37.35f, PocketStopX = 34.9f;
        internal const int TrapDelay = 12;
        // v2: Trident_1, in the roof over the bridge's foot (TridentX): it warns TridentWarn and fires TridentFire every Beat5 from
        // TridentPhase, its beams to the floor about TridentReach either side. Wait for Gate_5 at WaitX5, outside its fan.
        internal const float TridentX = 42.8f, TridentSpread = 35f, WaitX5 = 39f;
        internal const int TridentWarn = 12, TridentFire = 20, TridentPhase = 52;
        // The glass bridge: Glass_1-4 over Pit_2 (x Glass0-Glass1), each GlassDelay after a touch; Gate_5 at its east end.
        internal const float Glass0 = 44f, Glass1 = 52f, GlassW = 2f, Gate5X = 52.3f;
        internal const int GlassDelay = 20, Beat5 = 110, Gate5On = 44, Gate5Phase = 0;
        // The section gate and Checkpoint_1.
        internal const float Gate1SectionX = 60.5f, Checkpoint1X = 63.2f;
        // ---- Hall 2 ----
        // Pit_3 (x Pit3X0-Pit3X1) and the turning plates over it: Turn_1-3 (PAX-115), each every Beat2 from its phase, TurnStep
        // after the one before (a running light): it warns TurnWarn, turns over TurnFlip (not solid), holds spikes up TurnHold,
        // turns back over TurnFlip, and rests the rest of the beat. Hop from each to the next as the next comes back.
        internal const float Pit3X0 = 67f, Pit3X1 = 83f, TurnW = 2f;
        internal static readonly float[] TurnX = { 70.5f, 75f, 79.5f };
        internal const int Beat2 = 120, TurnWarn = 8, TurnFlip = 10, TurnHold = 40, TurnStep = 26, TurnPhase = 0;
        internal static int TurnPhaseOf(int k) => TurnPhase + k * TurnStep;
        // Gate_6 at the landing: on as a cat that hopped straight across lands.
        internal const float Gate6X = 86f;
        internal const int Gate6On = 40, Gate6Phase = 80;
        // Hall 2's tables: Table_0 over the checkpoint and the pit's edge (the wait for the running light); Table_1 before Gate_6.
        internal static readonly float[] Table0 = { 62f, 66.2f }, Table1 = { 84f, 85.7f };
        // ---- Hall 3: the overtake ----
        internal const float Gate2SectionX = 100.5f, Checkpoint2X = 103.2f;
        // The Tunnel (a long table) the cat shelters in as Purge_1 surges past; Table_P, under which Purge_1 parks, swinging.
        // Table_P is narrow (1.4 u) and under ParkX: the widest swings miss its edges and sweep the floor beside it, one side, then
        // the other, so going in and coming out are timed.
        internal static readonly float[] Tunnel = { 112f, 120f }, TableP = { ParkX - .6f, ParkX + .8f };
        // v2: the door (DoorX) slides back DoorSlide over DoorMove as a cat passes RetreatX, just short of Gate_7 (on Gate7On of every
        // Gate7Beat from Gate7Phase), which stands before the door's first place (its slide must cross no hazard). Table_D shelters the
        // wait for Gate_7.
        internal const float DoorX = 149f, DoorSlide = 3.5f, RetreatX = 147f, Gate7X = 148.4f;
        internal const int DoorMove = 25, Gate7On = 40, Gate7Phase = 110, Gate7Beat = 120;
        internal static readonly float[] TableD = { 145.2f, 147.8f };


        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base + Top) * .5f),(1f,Top - Base)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,.75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, C, Top);

            // ---- The floor, cut by the pits, the pocket's trapdoor and Hall 2's dens ----
            Block(elements, SoloRoomElementKind.Floor, "Floor_A", 0f, Pit1X0, Base, 0f);
            Pit(elements, "Pit_1", Pit1X0, Pit1X1);
            Block(elements, SoloRoomElementKind.Floor, "Floor_B", Pit1X1, TrapX0, Base, 0f);
            Pit(elements, "Pit_P", TrapX0, TrapX1);
            Block(elements, SoloRoomElementKind.Floor, "Floor_B2", TrapX1, Glass0, Base, 0f);
            Pit(elements, "Pit_2", Glass0, Glass1);
            Block(elements, SoloRoomElementKind.Floor, "Floor_C", Glass1, Pit3X0, Base, 0f);
            Pit(elements, "Pit_3", Pit3X0, Pit3X1);
            Block(elements, SoloRoomElementKind.Floor, "Floor_D", Pit3X1, Width, Base, 0f);

            // ---------- Hall 1 ----------
            // Purge_1, the one chaser, warns Purge1Warn from the start (its trigger is the spawn) and chases all level (D-276 am. 2).
            elements.Add(E(SoloRoomElementKind.Chaser,"Purge_1",(Purge1Home,C * .5f),(.5f,.5f),(2f,C * .5f),(4f,C),new SoloRoomTrapSettings(
                new ChaserSettings(Purge1Warn, Purge1End, ChaseZone, ChaseSpeed, ChaseLag, ChaseCatchUp, 0f,
                    minLag: ChaseNear, closeScale: ChaseCreep, swingFromX: SwingFromX, swingSpeed: SwingSpeed, swingTilts: SwingTilts,
                    parkFromX: ParkX, parkSpeed: 0f, afterSpeed: AfterSpeed),
                new SoloRoomTrapSettings(delayTicks: 0))));
            elements.Add(Gate("Gate_1", Gate1X, Gate1On, Beat1, Gate1Phase));
            elements.Add(Eye("Eye_1", Eye1X, EyeZone1X0, EyeZone1X1, Eye1Phase));
            elements.Add(Gate("Gate_2", Gate2X, Gate2On, Beat1, Gate2Phase));
            elements.Add(Stone("Stone_1", (Pit1X0 + Pit1X1) * .5f, Stone1On, StonePeriod, Stone1Phase));
            elements.Add(Gate("Gate_3", Gate3X, PocketOn, PocketBeat, Gate3Phase));
            elements.Add(Gate("Gate_4", Gate4X, PocketOn, PocketBeat, Gate4Phase));
            // v2: the pocket's trapdoor, stone like the floor round it, filling Pit_P to its laser.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Trap_P",((TrapX0 + TrapX1) * .5f,-PitDepth * .5f),(TrapX1 - TrapX0,PitDepth),settings:new SoloRoomTrapSettings(delayTicks:TrapDelay)));
            // v2: Trident_1, its muzzle just under the roof over the bridge's foot.
            elements.Add(E(SoloRoomElementKind.Trident,"Trident_1",(TridentX,C - .2f),(.6f,.6f),settings:new SoloRoomTrapSettings(new TridentSettings(TridentWarn, TridentFire, spreadDegrees: TridentSpread),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:Beat5,phaseTicks:TridentPhase,cooldownTicks:TridentWarn + TridentFire))));
            // The glass bridge: Glass_1-4 fill Pit_2 to its spikes (a trap floor draws over what it hides) and give way GlassDelay
            // after a cat lands on each.
            for (int i = 0; i < 4; i++)
            {
                float x0 = Glass0 + i * GlassW;
                elements.Add(E(SoloRoomElementKind.CollapsingFloor,$"Glass_{i + 1}",(x0 + GlassW * .5f,-PitDepth * .5f),(GlassW,PitDepth),settings:new SoloRoomTrapSettings(delayTicks:GlassDelay)));
            }
            elements.Add(Gate("Gate_5", Gate5X, Gate5On, Beat5, Gate5Phase));

            // ---------- Hall 2 ----------
            for (int k = 0; k < TurnX.Length; k++)
                elements.Add(E(SoloRoomElementKind.MovingTrap,$"Turn_{k + 1}",(TurnX[k],-.25f),(TurnW,.5f),settings:new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry, flipTicks: TurnFlip, flipWarnTicks: TurnWarn),
                    new SoloRoomTrapSettings(holdTicks:TurnHold,returnTicks:TurnFlip,repeatMode:TrapRepeatMode.Periodic,periodTicks:Beat2,phaseTicks:TurnPhaseOf(k),
                        cooldownTicks:TurnWarn + TurnFlip + TurnHold + TurnFlip,movingKind:MovingTrapKind.Solid))));
            elements.Add(Gate("Gate_6", Gate6X, Gate6On, Beat2, Gate6Phase));
            // The set piece's shade: a stone step on the floor past the spin.
            Table(elements, "Table_0", Table0); Table(elements, "Table_1", Table1);

            // ---------- Hall 3 ----------
            Table(elements, "Tunnel", Tunnel); Table(elements, "Table_P", TableP); Table(elements, "Table_D", TableD);
            // v2: the door's retreat past Gate_7.
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(RetreatX,C * .5f),(.4f,C),settings:new SoloRoomTrapSettings(moveTicks:DoorMove,offset:new Vector2(DoorSlide,0f))));
            elements.Add(Gate("Gate_7", Gate7X, Gate7On, Gate7Beat, Gate7Phase));

            var hall1 = new[] { "Purge_1", "Gate_1", "Eye_1", "Gate_2", "Stone_1", "Gate_3", "Gate_4", "Trap_P", "Trident_1", "Glass_1", "Glass_2", "Glass_3", "Glass_4", "Gate_5" };
            var hall2 = new[] { "Turn_1", "Turn_2", "Turn_3", "Gate_6" };
            var hall3 = new[] { "Retreat", "Gate_7" };
            var sections = new[] {
                CheckpointSection.Start("Hall1", new Vector2(2f, 0f), hall1),
                new CheckpointSection("Hall2", new Vector2(Checkpoint1X, 0f), new Rect(Gate1SectionX, 0f, .4f, 1.5f), hall2),
                new CheckpointSection("Hall3", new Vector2(Checkpoint2X, 0f), new Rect(Gate2SectionX, 0f, .4f, 1.5f), hall3),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A pulse gate at x, floor to ceiling: warning GateWarn, then lethal `on`, every `period` from `phase`.
        static SoloRoomElement Gate(string name, float x, int on, int period, int phase) =>
            E(SoloRoomElementKind.PulseGate, name, (x, C * .5f), (GateW, C), settings: new SoloRoomTrapSettings(
                new PulseGateSettings(GateWarn, on), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase)));

        // A ceiling turret at x, watching only a cat standing on the floor between zoneX0 and zoneX1.
        static SoloRoomElement Eye(string name, float x, float zoneX0, float zoneX1, int phase, int fire = EyeFire, int lockTicks = EyeLock)
        {
            float muzzle = C - .15f;
            var zone = new Rect(zoneX0 - x, -muzzle, zoneX1 - zoneX0, 1.2f);
            return E(SoloRoomElementKind.Turret, name, (x, muzzle), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, lockTicks, fire, ceilingMount: true, aimZone: zone),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: EyePeriod, phaseTicks: phase, cooldownTicks: EyeAim + lockTicks + fire)));
        }

        // A hard-light stone over a pit, its top level with the floor: lit `on` of every `period` from `phase`, flickering its last
        // StoneWarn, then gone (a phase platform, PAX-122).
        static SoloRoomElement Stone(string name, float x, int on, int period, int phase) =>
            E(SoloRoomElementKind.MovingTrap, name, (x, -.15f), (StoneW, .3f), settings: new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, phaseOnTicks: on, phaseWarnTicks: StoneWarn),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: on, movingKind: MovingTrapKind.Solid)));

        // A laser pit: its floor PitDepth down, lethal along the bottom.
        static void Pit(List<SoloRoomElement> e, string name, float x0, float x1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, name, ((x0 + x1) * .5f, (Base - PitDepth) * .5f), (x1 - x0, Base * -1f - PitDepth)));
            e.Add(E(SoloRoomElementKind.Hazard, name + "_Laser", ((x0 + x1) * .5f, -PitDepth + .15f), (x1 - x0, .3f)));
        }

        // A table: a low stone slab (x span[0]-span[1], TableBottom to TableTop) a cat stands under, out of the chaser's swing.
        static void Table(List<SoloRoomElement> e, string name, float[] span) => Block(e, SoloRoomElementKind.Floor, name, span[0], span[1], TableBottom, TableTop);

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
