using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-113 (D-178): a small synthetic room for the scanner, used only by the EditMode tests (the fixtures convention; not a
    // level, not the Trap Lab). A corridor 32 wide, floor top 0, ceiling underside 4; Scan is a vertical beam (0.3 wide, floor to
    // ceiling) resting at x 22. Every Period from room tick Phase it warns Warn ticks at home, sweeps 10 u west over Move, holds,
    // and comes back over Return. Two checkpoint sections (Start x 2; Corridor, gate x 6-6.4, checkpoint x 8), so the route
    // validator's rewind check covers the scanner's restore.
    public static class ScannerFixtures
    {
        public const int Period = 300, Phase = 80, Warn = 12, Move = 40, Hold = 10, Return = 40;
        public const float HomeX = 22f, Sweep = -10f, Top = 4f;

        public static SoloRoomDefinition Room() => Room(Warn, Top, 0f);

        // `beamTop`: where the beam ends (Top reaches the ceiling); `rise`: an up-down part of the offset (none for a scanner).
        public static SoloRoomDefinition Room(int warn, float beamTop, float rise) => new(0, 0f, 32f, new[] {
                E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Floor, "Floor", (16f, -1f), (32f, 2f)),
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, Top + 1f), (32f, 2f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Door, "Door", (30.5f, .75f), (.6f, 1.5f)),
                E(SoloRoomElementKind.MovingTrap, "Scan", (HomeX, beamTop * .5f), (.3f, beamTop), settings: Scanner(warn, rise)),
            }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, new[] {
                CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
                new CheckpointSection("Corridor", new Vector2(8f, 0f), new Rect(6f, 0f, .4f, Top), new[] { "Scan" }),
            });

        public static SoloRoomTrapSettings Scanner(int warn, float rise = 0f) =>
            new(new ScanSettings(warn), new SoloRoomTrapSettings(offset: new Vector2(Sweep, rise), moveTicks: Move, holdTicks: Hold, returnTicks: Return,
                repeatMode: TrapRepeatMode.Periodic, periodTicks: Period, phaseTicks: Phase, cooldownTicks: warn + Move + Hold + Return, movingKind: MovingTrapKind.Hazard));

        // Walk to x 10, short of the sweep's far end (x 11.85), and stop.
        static RouteStep[] ToWait() => new[] { Hold(Right), Until(XAtLeast(10f)), Release(), Until(Still()) };

        public static RoomRoutes Routes() => new(
            new Route("wait for it to come and go", Concat(ToWait(), Until(Home("Scan")), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
            new Betrayal("T1 [W]: a cat that walks on meets the scanner coming", "Scan", DeathCause.Hazard,
                new Route("walk on", Concat(ToWait(), Hold(Right), Until(Dead())))),
            new Betrayal("T2 [W]: a cat that stands where the scanner rests is caught as it sets off", "Scan", DeathCause.Hazard,
                new Route("stand at its home", Concat(ToWait(), Until(Home("Scan")), Hold(Right), Until(XAtLeast(HomeX)), Release(), Until(Dead())))));

        static RouteStep[] Concat(RouteStep[] head, params RouteStep[] tail)
        {
            var all = new RouteStep[head.Length + tail.Length];
            head.CopyTo(all, 0); tail.CopyTo(all, head.Length);
            return all;
        }
    }
}
