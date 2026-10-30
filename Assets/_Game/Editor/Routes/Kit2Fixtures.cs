using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-118-121 (D-197), PAX-122 (D-201): small synthetic rooms for the phase platform, the pendulum, the plate and gate, the portal and the turret, used only
    // by the EditMode tests (the fixtures convention). Each is a corridor 32 wide, floor top 0, with two checkpoint sections (Start x 2;
    // the trap's, gate x 4-4.3, checkpoint x 5), so the route validator's rewind check covers each restore.
    public static class Kit2Fixtures
    {
        static SoloRoomDefinition Room(float ceiling, SoloRoomElement[] traps, params string[] owned)
        {
            var all = new System.Collections.Generic.List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 3f), (1f, 12f)),
                E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 3f), (1f, 12f)),
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, ceiling + 1f), (32f, 2f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Door, "Door", (30.5f, .75f), (.6f, 1.5f)),
            };
            all.AddRange(traps);
            return new SoloRoomDefinition(0, 0f, 32f, all.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, new[] {
                CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
                new CheckpointSection("Trap", new Vector2(5f, 0f), new Rect(4f, 0f, .3f, ceiling), owned),
            });
        }

        static SoloRoomElement Floor(string name, float x0, float x1, float top = 0f) => E(SoloRoomElementKind.Floor, name, ((x0 + x1) * .5f, (top - 2f) * .5f), (x1 - x0, top + 2f));
        static SoloRoomElement Spikes(string name, float x0, float x1, float face) => E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f));
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        static Route R0(string name, params RouteStep[] steps) => new(name, steps);
        static RouteCondition Since(string element, int ticks) => new($"{element} +{ticks}", v =>
        {
            int i = v.Result.Elements.IndexOf(element);
            return i >= 0 && v.Last.FireTick[i] >= 0 && v.Last.RoomLifeTick - v.Last.FireTick[i] >= ticks;
        });

        // ---------- phase platform (PAX-122, D-201) ----------
        // Stone, over the middle of Pit (x 12-20), is there StoneOn ticks of every StonePeriod (flickering its last `warn`), then gone.
        public const int StonePeriod = 140, StoneOn = 80;
        public static SoloRoomDefinition PhaseRoom() => PhaseRoom(12);
        public static SoloRoomDefinition PhaseRoom(int warn) => Room(6f, new[] {
            Floor("Floor_W", 0f, 12f), Floor("Pit_Floor", 12f, 20f, -1.5f), Spikes("Pit", 12f, 20f, -1.5f), Floor("Floor_E", 20f, 32f),
            E(SoloRoomElementKind.MovingTrap, "Stone", (16f, .35f), (1.5f, .3f), settings: new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, phaseOnTicks: StoneOn, phaseWarnTicks: warn),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: StonePeriod, phaseTicks: 0, cooldownTicks: StoneOn, movingKind: MovingTrapKind.Solid))),
        }, "Stone");
        public static RoomRoutes PhaseRoutes()
        {
            RouteStep[] onto = { Hold(Right), Until(XAtLeast(10.5f)), Release(), Until(Still()), Until(Beat(StonePeriod, 0)),
                Hold(Right), Until(XAtLeast(11.8f)), Jump(), Until(XAtLeast(15.6f)), Release(), Until(Grounded()) };
            return new RoomRoutes(
                R0("on as it appears, off before it goes", Concat(onto, Hold(Right), Until(XAtLeast(16.5f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()))),
                new Betrayal("T1 [NW]: a cat that stays on Stone falls into Pit as it goes", "Pit", DeathCause.Hazard,
                    R0("stay", Concat(onto, Until(Dead()))), revealedBy: "Stone"));
        }

        // ---------- pendulum ----------
        public const int SwingPeriod = 120;
        public static SoloRoomDefinition PendulumRoom() => PendulumRoom(50f);
        public static SoloRoomDefinition PendulumRoom(float amplitude) => Room(6f, new[] {
            Floor("Floor", 0f, 32f),
            E(SoloRoomElementKind.Spinner, "Blade", (16f, 5.6f), (.5f, .5f), settings: new SoloRoomTrapSettings(SpinnerSettings.Pendulum(5.1f, amplitude, SwingPeriod), new SoloRoomTrapSettings())),
        }, "Blade");
        public static RoomRoutes PendulumRoutes()
        {
            RouteStep[] wait = { Hold(Right), Until(XAtLeast(11f)), Release(), Until(Still()) };
            return new RoomRoutes(
                R0("pass behind it", Concat(wait, Until(Beat(SwingPeriod, 35)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
                new Betrayal("T1 [W]: a cat that walks into the blade's swing is struck", "Blade", DeathCause.Hazard,
                    R0("go now", Concat(wait, Until(Beat(SwingPeriod, 80)), Hold(Right), Until(Dead())))));
        }

        // ---------- plate and gate ----------
        // Plate (x 8-9.2) opens Gate (x 15-16, the corridor's height) 1 tick after a cat steps on it, for GateHold ticks.
        public const int GateHold = 70;
        public static SoloRoomDefinition PlateRoom() => PlateRoom(1.2f);
        public static SoloRoomDefinition PlateRoom(float plateWidth) => Room(3f, new[] {
            Floor("Floor", 0f, 32f),
            E(SoloRoomElementKind.Ceiling, "Recess_Cap", (15.5f, 6.5f), (1f, 1f)),
            E(SoloRoomElementKind.Plate, "Plate", (8.6f, .05f), (plateWidth, .1f), settings: new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 20)),
            E(SoloRoomElementKind.MovingTrap, "Gate", (15.5f, 1.5f), (1f, 3f), settings: new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(delayTicks: 1, offset: new Vector2(0f, 3f), moveTicks: 8, holdTicks: GateHold, returnTicks: 8,
                    triggerSource: TrapTriggerSource.Chain, chainSource: "Plate", repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 8 + GateHold + 8, movingKind: MovingTrapKind.Solid))),
        }, "Plate", "Gate");
        public static RoomRoutes PlateRoutes() => new(
            R0("press and run", Hold(Right), Until(XAtLeast(8.6f)), Until(Fired("Gate")), Until(RoomComplete())),
            new Betrayal("T1 [W]: a cat that dawdles is crushed as Gate shuts on it", "Gate", DeathCause.Hazard,
                R0("dawdle", Hold(Right), Until(XAtLeast(8.6f)), Release(), Until(Still()), Until(Fired("Gate")), Until(Since("Gate", 20)), Hold(Right), Until(XAtLeast(14.4f)), Release(), Until(Still()), Until(Moving("Gate")), Hold(Right), Until(XAtLeast(15.3f)), Release(), Until(Dead()))));

        // ---------- portal ----------
        // A wall across the corridor at x 14-15; Warp's entry at x 11-12, its exit at x 18-19, which glitches over Pit (x 21.5-23.5)
        // for a while of every ExitPeriod.
        public const int ExitPeriod = 160;
        public static SoloRoomDefinition PortalRoom() => PortalRoom(.6f);
        public static SoloRoomDefinition PortalRoom(float entryWidth) => Room(6f, new[] {
            Floor("Floor_W", 0f, 21.5f), Floor("Pit_Floor", 21.5f, 23.5f, -1.5f), Spikes("Pit", 21.5f, 23.5f, -1.5f), Floor("Floor_E", 23.5f, 32f),
            E(SoloRoomElementKind.Wall, "Block", (14.5f, 3f), (1f, 6f)),
            E(SoloRoomElementKind.Portal, "Warp", (11.5f, .75f), (entryWidth, 1.5f), (18.5f, .75f), (.8f, 1.5f), new SoloRoomTrapSettings(
                new PortalSettings(),
                new(new PathSettings(new[] { new Vector2(4f, 0f), Vector2.zero }, glitch: true, warnTicks: 12),
                    new SoloRoomTrapSettings(moveTicks: 119, repeatMode: TrapRepeatMode.Periodic, periodTicks: ExitPeriod, phaseTicks: 0, cooldownTicks: 119)))),
        }, "Warp");
        public static RoomRoutes PortalRoutes()
        {
            RouteStep[] wait = { Hold(Right), Until(XAtLeast(10f)), Release(), Until(Still()) };
            return new RoomRoutes(
                R0("in while the exit is home", Concat(wait, Until(Beat(ExitPeriod, 125)), Hold(Right).Timed(TimedMode.Shift), Until(XAtLeast(20.9f)), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete()))),
                new Betrayal("T1 [W]: a cat that goes in while the exit is over Pit falls into it", "Pit", DeathCause.Hazard,
                    R0("in now", Concat(wait, Until(Beat(ExitPeriod, 62)), Hold(Right), Until(XAtLeast(11.6f)), Release(), Until(Dead()))), revealedBy: "Warp"));
        }

        // ---------- turret ----------
        // Gun, in the ceiling at x 16, aims at the cat AimTicks, locks LockTicks, fires FireTicks, every GunPeriod.
        public const int AimTicks = 50, LockTicks = 12, FireTicks = 10, GunPeriod = 120;
        public static SoloRoomDefinition TurretRoom() => TurretRoom(LockTicks);
        public static SoloRoomDefinition TurretRoom(int lockTicks) => Room(6f, new[] {
            Floor("Floor", 0f, 32f),
            E(SoloRoomElementKind.Turret, "Gun", (16f, 6.1f), (.6f, .6f), settings: new SoloRoomTrapSettings(new TurretSettings(AimTicks, lockTicks, FireTicks),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: GunPeriod, phaseTicks: 30, cooldownTicks: AimTicks + lockTicks + FireTicks))),
        }, "Gun");
        // D-203: Gun ceiling-mounted, aiming over the floor's standable area, its muzzle at `y` (just under the ceiling at 6, or inside it).
        public static SoloRoomDefinition CeilingTurretRoom() => CeilingTurretRoom(5.85f);
        public static SoloRoomDefinition CeilingTurretRoom(float y) => Room(6f, new[] {
            Floor("Floor", 0f, 32f),
            E(SoloRoomElementKind.Turret, "Gun", (16f, y), (.6f, .6f), settings: new SoloRoomTrapSettings(new TurretSettings(AimTicks, LockTicks, FireTicks, ceilingMount: true, aimZone: new Rect(-16f, -y, 32f, 6f)),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: GunPeriod, phaseTicks: 30, cooldownTicks: AimTicks + LockTicks + FireTicks))),
        }, "Gun");
        public static RoomRoutes TurretRoutes() => new(
            R0("keep moving", Hold(Right), Until(RoomComplete())),
            new Betrayal("T1 [W]: a cat that stops under the turret is shot", "Gun", DeathCause.Hazard,
                R0("stop", Hold(Right), Until(XAtLeast(14f)), Release(), Until(Dead()))));

        static RouteStep[] Concat(RouteStep[] head, params RouteStep[] tail)
        {
            var all = new RouteStep[head.Length + tail.Length];
            head.CopyTo(all, 0); tail.CopyTo(all, head.Length);
            return all;
        }
    }
}
