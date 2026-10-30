using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-114/115/116 (D-187): small synthetic rooms for the new movers, used only by the EditMode tests (the fixtures convention;
    // not a level, not the Trap Lab). Each is a corridor 32 wide, floor top 0, ceiling underside 6, with two checkpoint sections
    // (Start x 2; the trap's, gate x 6-6.4, checkpoint x 8), so the route validator's rewind check covers each restore.
    public static class NewMoverFixtures
    {
        public const float Top = 6f;
        // Glitch: Bridge spans the pit (x 10-14) and teleports 3.5 u up for GlitchStep ticks of every GlitchPeriod (from
        // GlitchPhase + GlitchStep); Gate, a block over the walkway at x 20, teleports down onto the floor the same way, a beat later.
        public const int GlitchPeriod = 120, GlitchPhase = 30, GlitchStep = 40, GlitchWarn = 10, GatePhase = 90;
        // Flip: Flip spans the pit (x 11.6-14.4 here, its turning disc's width), turning FlipDelay after a cat comes onto it; Flip_C
        // hangs over the walkway at x 22.
        public const int FlipDelay = 30, FlipTurn = 10, FlipHold = 40;
        // Spinner: a bar (two arms of SpinLength) about (16, SpinY), SpinRate degrees a tick from 0, from the room's first tick.
        public const float SpinLength = 1.9f, SpinY = 2.2f, SpinRate = 3f;

        static SoloRoomElement[] Shell(float pitFrom, float pitTo) => new[] {
            E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 2f), (1f, 10f)),
            E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 2f), (1f, 10f)),
            E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, Top + 1f), (32f, 2f)),
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
            E(SoloRoomElementKind.Door, "Door", (30.5f, .75f), (.6f, 1.5f)),
            E(SoloRoomElementKind.Floor, "Floor_W", (pitFrom * .5f, -1f), (pitFrom, 2f)),
            E(SoloRoomElementKind.Floor, "Floor_E", ((pitTo + 32f) * .5f, -1f), (32f - pitTo, 2f)),
            E(SoloRoomElementKind.PitBottom, "Pit_Bottom", ((pitFrom + pitTo) * .5f, -2.5f), (pitTo - pitFrom, 1f)),
            E(SoloRoomElementKind.Hazard, "Pit_Spikes", ((pitFrom + pitTo) * .5f, -1.85f), (pitTo - pitFrom, .3f)),
        };

        static CheckpointSection[] Sections(params string[] trap) => new[] {
            CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
            new CheckpointSection("Trap", new Vector2(8f, 0f), new Rect(6f, 0f, .4f, Top), trap),
        };

        static SoloRoomDefinition Room(SoloRoomElement[] shell, CheckpointSection[] sections, params SoloRoomElement[] traps)
        {
            var all = new SoloRoomElement[shell.Length + traps.Length];
            shell.CopyTo(all, 0); traps.CopyTo(all, shell.Length);
            return new SoloRoomDefinition(0, 0f, 32f, all, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);

        // ---------- glitch walls ----------

        public static SoloRoomTrapSettings Glitch(Vector2 jump, int phase, int warn = GlitchWarn, int step = GlitchStep) =>
            new(new PathSettings(new[] { jump, Vector2.zero }, glitch: true, warnTicks: warn),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks: 2 * step, repeatMode: TrapRepeatMode.Periodic, periodTicks: GlitchPeriod, phaseTicks: phase,
                        cooldownTicks: 2 * step, movingKind: MovingTrapKind.Solid)));

        public static SoloRoomDefinition GlitchRoom() => GlitchRoom(GlitchWarn);
        public static SoloRoomDefinition GlitchRoom(int warn) => Room(Shell(10f, 14f), Sections("Bridge", "Gate"),
            E(SoloRoomElementKind.MovingTrap, "Bridge", (12f, -.25f), (4f, .5f), settings: Glitch(new Vector2(0f, 3.5f), GlitchPhase, warn)),
            E(SoloRoomElementKind.MovingTrap, "Gate", (20f, 4.25f), (1f, 2.5f), settings: Glitch(new Vector2(0f, -3f), GatePhase, warn)));

        // Wait at the pit's edge for Bridge to come back, cross; pass under Gate while it's up.
        public static RoomRoutes GlitchRoutes()
        {
            RouteStep[] edge = { Hold(Right), Until(XAtLeast(9f)), Release(), Until(Still()), Until(Fired("Bridge")) };
            return new RoomRoutes(
                new Route("cross when it's back", Concat(edge, Until(Beat(GlitchPeriod, GlitchPhase + 2 * GlitchStep + 1)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
                new Betrayal("T1 [NW]: a cat that waits on Bridge falls when it jumps away", "Pit_Spikes", DeathCause.Hazard,
                    new Route("wait on it", Concat(edge, Until(Beat(GlitchPeriod, GlitchPhase + 2 * GlitchStep + 1)), Hold(Right), Until(XAtLeast(12f)), Release(), Until(Dead()))), revealedBy: "Bridge"),
                new Betrayal("T2 [NW]: a cat that stops under Gate is crushed as it appears", "Gate", DeathCause.Hazard,
                    new Route("stop under it", Concat(edge, Until(Beat(GlitchPeriod, GlitchPhase + 2 * GlitchStep + 1)), Hold(Right), Until(XAtLeast(20f)), Release(), Until(Dead())))));
        }

        // ---------- flip platforms ----------

        public static SoloRoomDefinition FlipRoom() => FlipRoom(FlipTurn);
        public static SoloRoomDefinition FlipRoom(int turn) => Room(Shell(11.6f, 14.4f), Sections("Flip", "Flip_C"),
            E(SoloRoomElementKind.MovingTrap, "Flip", (13f, -.25f), (2.5f, .5f), (13f, .5f), (2.5f, 1f), new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, flipTicks: turn),
                new SoloRoomTrapSettings(delayTicks: FlipDelay, holdTicks: FlipHold, returnTicks: turn, repeatMode: TrapRepeatMode.Rearm,
                    cooldownTicks: 8 + 2 * turn + FlipHold, movingKind: MovingTrapKind.Solid))),
            E(SoloRoomElementKind.MovingTrap, "Flip_C", (22f, 1.45f), (2f, .5f), settings: new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, flipTicks: turn),
                new SoloRoomTrapSettings(holdTicks: FlipHold, returnTicks: turn, repeatMode: TrapRepeatMode.Periodic, periodTicks: 5000, phaseTicks: 4999,
                    cooldownTicks: 8 + 2 * turn + FlipHold, movingKind: MovingTrapKind.Solid))));

        // Across Flip before it turns; walk under Flip_C.
        public static RoomRoutes FlipRoutes() => new(
            new Route("walk across", new[] { Hold(Right), Until(RoomComplete()) }),
            new Betrayal("T1 [NW]: a cat that stays on Flip is impaled as it turns over", "Flip", DeathCause.Hazard,
                new Route("stay on", new[] { Hold(Right), Until(XAtLeast(12.6f)), Release(), Until(Dead()) }), revealedBy: "Flip"));

        // Jump under Flip_C into its spiked underside (a test route: an honest static killer has no reveal, so it isn't a betrayal).
        public static Route FlipJumpUnder() => new("jump under", new[] { Hold(Right), Until(XAtLeast(21.2f)), Jump(), Until(Dead()) });

        // ---------- spinners ----------

        public static SoloRoomDefinition SpinnerRoom() => SpinnerRoom(SpinRate);
        public static SoloRoomDefinition SpinnerRoom(float rate) => Room(Shell(31f, 31.5f), Sections("Spin"),
            E(SoloRoomElementKind.Spinner, "Spin", (16f, SpinY), (.5f, .5f), settings: new SoloRoomTrapSettings(new SpinnerSettings(2, SpinLength, rate), new SoloRoomTrapSettings())));

        // Wait short of the bar and go on its beat (its low arm is clear of the walkway from tick 41 to 79 of every 60).
        public static RoomRoutes SpinnerRoutes()
        {
            RouteStep[] wait = { Hold(Right), Until(XAtLeast(12f)), Release(), Until(Still()) };
            return new RoomRoutes(
                new Route("go on the beat", Concat(wait, Until(Beat(60, 22)), Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete()))),
                new Betrayal("T1 [W]: a cat that goes off the beat is struck by the bar", "Spin", DeathCause.Hazard,
                    new Route("go now", Concat(wait, Until(Beat(60, 52)), Hold(Right), Until(Dead())))));
        }

        static RouteStep[] Concat(RouteStep[] head, params RouteStep[] tail)
        {
            var all = new RouteStep[head.Length + tail.Length];
            head.CopyTo(all, 0); tail.CopyTo(all, head.Length);
            return all;
        }
    }
}
