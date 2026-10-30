using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-192 (replaces D-179; the developer, 2026-10-24: "I played 22 and 23 and they are waaaay too easy. Absolutely no challenge,
    // the traps were lame, no creativity"): level 22, "Clockwork Ridge". Crossed west to east over a ridge, never up a stack.
    // D-204 (the developer, 2026-10-27: the stair's platforms parked up out of reach are "misleading"; missing the lift is a dead
    // end; "I dont like the disppearing walls"; the level is "too easy and too quickly over"): longer, under low roofs, and with no
    // glitch movers. Everything runs on its own clock, and the clocks don't agree.
    //  - Gears (section 0): Spin_1 and Spin_2 turn opposite ways over the plain: pass under each while it lies flat.
    //  - Stair (section 1): Step_1-3, phase platforms (PAX-122), appear in a wave over the spiked pit, each in its own spot, and
    //    flicker out: hop up as each appears, onto Peak. Eye_S, a turret in the stair's roof, punishes the wait the stair teaches.
    //  - Descent (section 2): Flip_1-3 step down into the valley, turning over on a 120-tick beat; Swing_1 and Swing_2 swing
    //    through the hops on the same beat from their own phases: setting off as soon as Flip_1 rights itself meets Swing_2.
    //  - Valley (section 3): Spin_V sweeps the valley. Lift_P waits up at the cliff's edge; Plate_V marks its trigger, which calls it
    //    down into its slot hard enough to crush a cat already waiting there. It goes back up after its hold, and the plate calls
    //    it again (no dead end). On the cliff, Swing_C swings across the walk.
    //  - Tower (section 4): Cog_1 and Cog_2 run clockwise loops (up, east, down, west) on one 240-tick beat, up the clock tower;
    //    Hand turns over the pillar between them on a 160-tick beat, so only every other lap is clear. Ride Cog_1 up and east
    //    onto the pillar, cross it while Hand is up, ride Cog_2 up and east onto the door's ledge.
    static class L022Layout
    {
        internal const float Plain = 2f, Pit = 0f, Peak = 7.6f, Valley = 2f, Cliff = 10f, Base = -2f, Top = 24.5f;
        // The roofs' undersides: over the plain and the stair; over Peak and the descent; over the valley; over the cliff; the tower's.
        internal const float StairRoof = 9.4f, DescentRoof = 10.5f, ValleyRoof = 7.5f, CliffRoof = 14.4f, TowerRoof = 23.5f;
        internal const float SpinRate = 2f;
        // The stair: each step there StepOn ticks of every StepPeriod (flickering its last StepWarn), StepGap later than the one before.
        internal const int StepOn = 60, StepWarn = 12, StepPeriod = 150, StepGap = 30, StepPhase = 20;
        // The descent's clock: the flips turn over FlipGap apart, and Swing_1 and Swing_2 swing on the same beat from their own phases
        // (Swing_1 crosses the first hop just as Flip_1 rights itself), all DescentPhase into the room's beat.
        internal const int FlipPeriod = 120, FlipGap = 25, DescentPhase = 0, Swing1Phase = 75, Swing2Phase = 115, SwingCPeriod = 140;
        // Eye_S (as L021's turrets): aim, lock (its tell), fire, rest, and its phase.
        internal const int EyeAim = 50, EyeLock = 12, EyeFire = 10, EyeRest = 33, EyePeriod = EyeAim + EyeLock + EyeFire + EyeRest, EyePhase = 75;
        // Lift_P: down LiftMove ticks LiftDelay after Plate_V, holds LiftHold in its slot, back up over LiftMove.
        internal const int LiftDelay = 10, LiftMove = 60, LiftHold = 60;
        // The tower: the cogs' loop and beat, their phases, Hand's turn (degrees a tick; 160 ticks a turn).
        internal const int CogMove = 190, CogPeriod = 240, Cog1Phase = 0, Cog2Phase = 140;
        // Hand starts at HandStart degrees: on the cogs' lap the cat takes it points up and away as the cat crosses the pillar; a lap
        // earlier or later (half a turn on) it points down at the pillar.
        internal const float HandRate = 2.25f, HandStart = 30f, CogRise = 4.5f, CogRun = 3.5f;
        internal const float Mid = Cliff + CogRise, Ledge = Mid + CogRise, PitT = 8.5f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + Top) * .5f),(1f,Top - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(86.5f,(Base - 1f + Top) * .5f),(1f,Top - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,Plain),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(84.5f,Ledge + .75f),(.6f,1.5f)),
            };
            // ---- The roofs ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Stair", 0f, 27f, StairRoof, Top);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Descent", 27f, 44.5f, DescentRoof, Top);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Valley", 44.5f, 53f, ValleyRoof, Top);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Cliff", 53f, 68f, CliffRoof, Top);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_Tower", 68f, 86f, TowerRoof, Top);

            // ---- The ground: the plain (x 0-16), the stair's pit (16-27), Peak (27-31), the descent's pit (31-43), the valley
            // (43-53), Lift_P's slot (53-55), the cliff (55-68), the tower's pits either side of the pillar (68-82), the door's ledge ----
            Block(elements, SoloRoomElementKind.Floor, "Plain", 0f, 16f, Base, Plain);
            Block(elements, SoloRoomElementKind.Floor, "Pit_1_Floor", 16f, 27f, Base, Pit);
            Spikes(elements, "Pit_1", 16f, 27f, Pit);
            Block(elements, SoloRoomElementKind.Floor, "Peak", 27f, 31f, Base, Peak);
            Block(elements, SoloRoomElementKind.Floor, "Pit_2_Floor", 31f, 43f, Base, Pit);
            Spikes(elements, "Pit_2", 31f, 43f, Pit);
            Block(elements, SoloRoomElementKind.Floor, "Valley_Floor", 43f, 53f, Base, Valley);
            Block(elements, SoloRoomElementKind.Floor, "Lift_Slot", 53f, 55f, Base, Valley - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Cliff", 55f, 68f, Base, Cliff);
            Block(elements, SoloRoomElementKind.Floor, "Pit_T1_Floor", 68f, 73.5f, Base, PitT);
            Spikes(elements, "Pit_T1", 68f, 73.5f, PitT);
            Block(elements, SoloRoomElementKind.Floor, "Pillar", 73.5f, 76.5f, Base, Mid);
            Block(elements, SoloRoomElementKind.Floor, "Pit_T2_Floor", 76.5f, 82f, Base, PitT);
            Spikes(elements, "Pit_T2", 76.5f, 82f, PitT);
            Block(elements, SoloRoomElementKind.Floor, "Door_Ledge", 82f, 86f, Base, Ledge);

            // ---------- Gears ----------
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_1",(7f,Plain + 1.5f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(2,1.2f,SpinRate),new SoloRoomTrapSettings())));
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_2",(11f,Plain + 1.5f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(2,1.2f,-SpinRate,67f),new SoloRoomTrapSettings())));

            // ---------- Stair ----------
            // Each step is there StepOn ticks of every StepPeriod, from i * StepGap, in its own place (D-204: nothing parked out of reach).
            foreach ((string name, float x0, float top, int i) in new[] { ("Step_1", 17f, Plain + 1.4f, 0), ("Step_2", 20.5f, Plain + 2.8f, 1), ("Step_3", 24f, Plain + 4.2f, 2) })
                elements.Add(E(SoloRoomElementKind.MovingTrap,name,(x0 + 1f,top - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry, phaseOnTicks: StepOn, phaseWarnTicks: StepWarn),
                    new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: StepPeriod, phaseTicks: StepPhase + i * StepGap, cooldownTicks: StepOn, movingKind: MovingTrapKind.Solid))));
            // Eye_S, in the stair's roof over Step_3, aims over the stair and the plain's last 4 u, where the stair is waited for.
            Turret(elements, "Eye_S", 23f, StairRoof, new Rect(12f, Pit, 15f, StairRoof - Pit), EyePhase);

            // ---------- Descent ----------
            foreach ((string name, float x0, float top, int i) in new[] { ("Flip_1", 32.5f, Peak - 1.2f, 0), ("Flip_2", 36f, Peak - 2.4f, 1), ("Flip_3", 39.5f, Peak - 3.6f, 2) })
                elements.Add(E(SoloRoomElementKind.MovingTrap,name,(x0 + 1f,top - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry, flipTicks: 10),
                    new SoloRoomTrapSettings(holdTicks:40,returnTicks:10,repeatMode:TrapRepeatMode.Periodic,periodTicks:FlipPeriod,phaseTicks:DescentPhase + i * FlipGap,
                        cooldownTicks:8 + 10 + 40 + 10,movingKind:MovingTrapKind.Solid))));
            // Swing_1 and Swing_2 hang from the descent's roof over the gaps between the flips, their tips through the hops' arcs.
            Pendulum(elements, "Swing_1", 35.25f, DescentRoof, 3.3f, 60f, Swing1Phase);
            Pendulum(elements, "Swing_2", 38.75f, DescentRoof, 4.4f, 50f, Swing2Phase);

            // ---------- Valley ----------
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_V",(47f,Valley + 1.6f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.4f,-4f),new SoloRoomTrapSettings())));
            elements.Add(E(SoloRoomElementKind.Plate,"Plate_V",(50.6f,Valley + .05f),(1.2f,.1f),settings:new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Rearm,cooldownTicks:20)));
            // Lift_P: home up in the cliff's edge, flush with its top. A cat over Plate_V (its trigger is the plate's column, floor to
            // roof, so the plate is what calls it) brings it down 8 into its slot LiftDelay later: a slam, it crushes a cat waiting
            // there. It holds, goes back up with whoever boarded, and the plate calls it again once it's home.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_P",(54f,Cliff - .25f),(2f,.5f),(50.6f,(Valley + ValleyRoof) * .5f),(1.2f,ValleyRoof - Valley),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(delayTicks:LiftDelay,offset:new Vector2(0f,Valley - Cliff),moveTicks:LiftMove,holdTicks:LiftHold,returnTicks:LiftMove,
                    repeatMode:TrapRepeatMode.Rearm,cooldownTicks:LiftMove + LiftHold + LiftMove,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Spinner,"Swing_C",(61f,CliffRoof - .25f),(.5f,.5f),settings:new SoloRoomTrapSettings(SpinnerSettings.Pendulum(3.9f, 60f, SwingCPeriod),new SoloRoomTrapSettings())));

            // ---------- Tower ----------
            // Each cog: home flush with the floor it leaves from, up CogRise, east CogRun (flush with the floor it reaches), down, west.
            Cog(elements, "Cog_1", 69f, Cliff, Cog1Phase);
            Cog(elements, "Cog_2", 77.5f, Mid, Cog2Phase);
            elements.Add(E(SoloRoomElementKind.Spinner,"Hand",(75f,Mid + 3.2f),(.6f,.6f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,3f,HandRate,HandStart,.35f),new SoloRoomTrapSettings())));

            var sections = new[] {
                CheckpointSection.Start("Gears", new Vector2(2f, Plain), new[] { "Spin_1", "Spin_2" }),
                new CheckpointSection("Stair", new Vector2(14.7f, Plain), new Rect(13.6f, Plain, .3f, 1.5f), new[] { "Step_1", "Step_2", "Step_3", "Eye_S" }),
                new CheckpointSection("Descent", new Vector2(29f, Peak), new Rect(27.5f, Peak, .3f, 1.5f), new[] { "Flip_1", "Flip_2", "Flip_3", "Swing_1", "Swing_2" }),
                new CheckpointSection("Valley", new Vector2(44.2f, Valley), new Rect(43.2f, Valley, .3f, 1.5f), new[] { "Spin_V", "Plate_V", "Lift_P", "Swing_C" }),
                new CheckpointSection("Tower", new Vector2(66f, Cliff), new Rect(65.2f, Cliff, .3f, 1.5f), new[] { "Cog_1", "Cog_2", "Hand" }),
            };
            return new SoloRoomDefinition(0, 0f, 86f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A cog: a 2 u platform centred on `x`, its top at `top`, looping up, east, down and west over CogMove of every CogPeriod.
        static void Cog(List<SoloRoomElement> e, string name, float x, float top, int phase) =>
            e.Add(E(SoloRoomElementKind.MovingTrap, name, (x, top - .25f), (2f, .5f), settings: new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(0f, CogRise), new Vector2(CogRun, CogRise), new Vector2(CogRun, 0f), Vector2.zero }),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks: CogMove, repeatMode: TrapRepeatMode.Periodic, periodTicks: CogPeriod, phaseTicks: phase, cooldownTicks: CogMove, movingKind: MovingTrapKind.Solid)))));

        // A pendulum hung .25 under a roof's underside `under`, swinging `amplitude` either side of straight down once every
        // FlipPeriod: set off `delay` after each of Flip_1's turns, so it keeps the flips' beat from its own phase.
        static void Pendulum(List<SoloRoomElement> e, string name, float x, float under, float length, float amplitude, int delay) =>
            e.Add(E(SoloRoomElementKind.Spinner, name, (x, under - .25f), (.5f, .5f), settings: new SoloRoomTrapSettings(
                new SpinnerSettings(1, length, 0f, 270f, .4f, alwaysOn: false, swingAmplitude: amplitude, swingPeriod: FlipPeriod),
                new SoloRoomTrapSettings(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: "Flip_1", repeatMode: TrapRepeatMode.Rearm, cooldownTicks: FlipPeriod - 20))));

        // A turret hung from a roof's underside `under` (its muzzle just below), ceiling-mounted, aiming over `zone` (as L021's).
        static void Turret(List<SoloRoomElement> e, string name, float x, float under, Rect zone, int phase) =>
            e.Add(E(SoloRoomElementKind.Turret, name, (x, under - .15f), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(EyeAim, EyeLock, EyeFire, ceilingMount: true, aimZone: new Rect(zone.x - x, zone.y - (under - .15f), zone.width, zone.height)),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: EyePeriod, phaseTicks: phase, cooldownTicks: EyeAim + EyeLock + EyeFire))));
    }
}
