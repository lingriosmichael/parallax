using Parallax.Core;
using Parallax.Editor.Setup;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-111 (D-177): a small synthetic room for the laser turret, used only by the EditMode tests (the fixtures convention; not
    // a level, not the Trap Lab). A corridor 32 wide, floor top 0, ceiling underside 4; Turret sits in the ceiling at x 16 and
    // fires straight down to the floor every Period from room tick 0: Tell ticks of warning ray, then Beam ticks lethal. Two
    // checkpoint sections, so the route validator's rewind check covers the beam's restore: Start (x 2) and Corridor (gate x
    // 6-6.4, checkpoint x 8).
    public static class BeamFixtures
    {
        public const int Period = 120, Tell = 12, Beam = 40;
        public const float LaneX = 16f, Top = 4f;

        public static SoloRoomDefinition Room() => Room(Tell, Beam, Period, Period - 1, false);

        public static SoloRoomDefinition Room(int tell, int beam, int period, int cooldown, bool disguised) => new(0, 0f, 32f, new[] {
                E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Floor, "Floor", (16f, -1f), (32f, 2f)),
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, Top + 1f), (32f, 2f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Door, "Door", (30.5f, .75f), (.6f, 1.5f)),
                E(SoloRoomElementKind.Arrow, "Turret", (LaneX, Top + .2f), (.6f, .4f), settings: new SoloRoomTrapSettings(
                    new ArrowLane(new ArrowLane(ArrowDirection.Right, Top, LaneX, tellTicks: tell, thickness: .3f, disguised: disguised, angleDegrees: -90f, laneEndY: 0f), true, beam, true),
                    new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, cooldownTicks: cooldown))),
            }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, new[] {
                CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
                new CheckpointSection("Corridor", new Vector2(8f, 0f), new Rect(6f, 0f, .4f, Top), new[] { "Turret" }),
            });

        // The room tick is `tick` past a multiple of the period.
        static RouteCondition Beat(int tick) => new($"beat {tick}/{Period}", v => ((v.Last.RoomLifeTick - tick) % Period + Period) % Period == 0);

        static RouteStep[] ToLane() => new[] { Hold(Right), Until(XAtLeast(LaneX - 2.5f)), Release(), Until(Still()) };

        public static RoomRoutes Routes() => new(
            new Route("cross when it's dark", Concat(ToLane(), Until(Beat(Tell + Beam)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
            new Betrayal("T1 [W]: a cat that goes as the ray shows runs into the beam", "Turret", DeathCause.Hazard,
                new Route("go on the ray", Concat(ToLane(), Until(Beat(0)), Hold(Right), Until(Dead())))),
            new Betrayal("T2 [W]: a cat that stands under the turret is caught when the beam fires", "Turret", DeathCause.Hazard,
                new Route("stand under it", Concat(ToLane(), Until(Beat(Tell + Beam)), Hold(Right), Until(XAtLeast(LaneX)), Release(), Until(Dead())))));

        static RouteStep[] Concat(RouteStep[] head, params RouteStep[] tail)
        {
            var all = new RouteStep[head.Length + tail.Length];
            head.CopyTo(all, 0); tail.CopyTo(all, head.Length);
            return all;
        }
    }
}
