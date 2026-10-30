using Parallax.Core;
using Parallax.Editor.Setup;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-112 (D-176): a small synthetic room for the pulse gate, used only by the EditMode tests (the fixtures convention; not
    // a level, not the Trap Lab). A corridor 32 wide, floor top 0, ceiling underside 4; Gate is a vertical barrier at x 16
    // (0.3 wide) from the floor to the ceiling, warning WarnTicks then on OnTicks of every Period from room tick Phase. Two
    // checkpoint sections, so the route validator's rewind check covers the gate's restore: Start (x 2) and Corridor, whose
    // gate is x 6-6.4 and checkpoint x 8.
    public static class PulseGateFixtures
    {
        public const int Period = 120, Phase = 0, WarnTicks = 12, OnTicks = 48;
        public const float GateX = 16f, Top = 4f;

        public static SoloRoomDefinition Room() => Room(WarnTicks, OnTicks, Period, Top);

        // `gateTop`: where the barrier ends (Top reaches the ceiling; lower leaves a gap over it).
        public static SoloRoomDefinition Room(int warn, int on, int period, float gateTop) => new(0, 0f, 32f, new[] {
                E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 1.5f), (1f, 7f)),
                E(SoloRoomElementKind.Floor, "Floor", (16f, -1f), (32f, 2f)),
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, Top + 1f), (32f, 2f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Door, "Door", (30.5f, .75f), (.6f, 1.5f)),
                E(SoloRoomElementKind.PulseGate, "Gate", (GateX, gateTop * .5f), (.3f, gateTop), settings: Gate(warn, on, period)),
            }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, new[] {
                CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
                new CheckpointSection("Corridor", new Vector2(8f, 0f), new Rect(6f, 0f, .4f, Top), new[] { "Gate" }),
            });

        public static SoloRoomTrapSettings Gate(int warn, int on, int period, int phase = Phase) =>
            new(new PulseGateSettings(warn, on), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase));

        // The room tick is `tick` past a multiple of the period.
        static RouteCondition Beat(int tick) => new($"beat {tick}/{Period}", v => ((v.Last.RoomLifeTick - tick) % Period + Period) % Period == 0);

        // Walk up to the gate and stop short; go as it switches off.
        static RouteStep[] ToGate() => new[] { Hold(Right), Until(XAtLeast(GateX - 2.5f)), Release(), Until(Still()) };

        public static RoomRoutes Routes() => new(
            new Route("cross when it's off", Concat(ToGate(), Until(Beat(Phase + WarnTicks + OnTicks)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
            new Betrayal("T1 [W]: a cat that crosses as the gate fires runs into it", "Gate", DeathCause.Hazard,
                new Route("go as it warns", Concat(ToGate(), Until(Beat(Phase)), Hold(Right), Until(Dead())))),
            new Betrayal("T2 [W]: a cat that stands in the gate while it's off is caught when it comes on", "Gate", DeathCause.Hazard,
                new Route("stand in it", Concat(ToGate(), Until(Beat(Phase + WarnTicks + OnTicks)), Hold(Right), Until(XAtLeast(GateX)), Release(), Until(Dead())))));

        static RouteStep[] Concat(RouteStep[] head, params RouteStep[] tail)
        {
            var all = new RouteStep[head.Length + tail.Length];
            head.CopyTo(all, 0); tail.CopyTo(all, head.Length);
            return all;
        }
    }
}
