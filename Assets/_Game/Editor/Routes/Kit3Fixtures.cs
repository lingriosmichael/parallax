using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-123/124 (D-276): small synthetic rooms for the trident and the rotating chaser, used only by the EditMode tests (the fixtures
    // convention, as Kit2Fixtures). Each is a corridor 32 wide, floor top 0, ceiling underside 4, with two checkpoint sections (Start
    // x 2; the trap's, gate x 4-4.3, checkpoint x 5), so the route validator's rewind check covers each restore.
    public static class Kit3Fixtures
    {
        const float Ceiling = 4f;

        static SoloRoomDefinition Room(float doorX, SoloRoomElement[] traps, params string[] owned)
        {
            var all = new System.Collections.Generic.List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall, "Wall_L", (-.5f, 1f), (1f, 8f)),
                E(SoloRoomElementKind.Wall, "Wall_R", (32.5f, 1f), (1f, 8f)),
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, Ceiling + 1f), (32f, 2f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Door, "Door", (doorX, .75f), (.6f, 1.5f)),
            };
            all.AddRange(traps);
            return new SoloRoomDefinition(0, 0f, 32f, all.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, new[] {
                CheckpointSection.Start("Start", new Vector2(2f, 0f), System.Array.Empty<string>()),
                new CheckpointSection("Trap", new Vector2(5f, 0f), new Rect(4f, 0f, .3f, Ceiling), owned),
            });
        }

        static SoloRoomElement Floor(string name, float x0, float x1, float top = 0f, float bottom = -3f) =>
            E(SoloRoomElementKind.Floor, name, ((x0 + x1) * .5f, (top + bottom) * .5f), (x1 - x0, top - bottom));
        static RouteCondition Beat(int period, int tick) => new($"beat {tick}/{period}", v => ((v.Last.RoomLifeTick - tick) % period + period) % period == 0);
        static Route R0(string name, params RouteStep[] steps) => new(name, steps);

        // ---------- trident ----------
        // Fork, in the ceiling at x 16, warns ForkWarn and fires ForkFire every ForkPeriod from ForkPhase: one beam down to x 16, two at
        // ± ForkSpread to the floor about 2.7 u either side.
        public const int ForkWarn = 12, ForkFire = 20, ForkPeriod = 120, ForkPhase = 30;
        public const float ForkSpread = 35f;
        public static SoloRoomDefinition TridentRoom() => TridentRoom(ForkWarn);
        public static SoloRoomDefinition TridentRoom(int warnTicks) => Room(30.5f, new[] {
            Floor("Floor", 0f, 32f),
            E(SoloRoomElementKind.Trident, "Fork", (16f, Ceiling - .2f), (.6f, .6f), settings: new SoloRoomTrapSettings(new TridentSettings(warnTicks, ForkFire, spreadDegrees: ForkSpread),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: ForkPeriod, phaseTicks: ForkPhase, cooldownTicks: warnTicks + ForkFire))),
        }, "Fork");
        // Wait clear of the fan; run as a fire ends (the next warning comes after the cat is through).
        public static RoomRoutes TridentRoutes() => new(
            R0("run as it goes out", Hold(Right), Until(XAtLeast(10f)), Release(), Until(Still()), Until(Beat(ForkPeriod, ForkPhase + ForkWarn + ForkFire)),
                Hold(Right).Timed(TimedMode.Shift), Until(RoomComplete())),
            new Betrayal("T1 [W]: a cat that stops under the trident is shot", "Fork", DeathCause.Hazard,
                R0("stop under it", Hold(Right), Until(XAtLeast(15.8f)), Release(), Until(Dead()))));

        // ---------- rotating chaser ----------
        // Wave: a bar 2 x WaveArm through its pivot (home x .5, y 2), turning WaveTurn a tick; every WavePeriod from WavePhase it warns
        // WaveWarn at home, then sweeps the corridor to x 31.9 over WaveMove ticks (faster than a run). Den, a recess 1.2 deep at x 14-16,
        // is out of its reach: hide there as it passes, then follow it to the door before the next wave.
        public const int WaveWarn = 30, WaveMove = 170, WavePeriod = 300, WavePhase = 20;
        public const float WaveArm = 2.3f, WaveTurn = 6f, DenX0 = 14f, DenX1 = 16f, DenDepth = 1.2f;
        public static SoloRoomDefinition SweeperRoom() => SweeperRoom(WaveTurn);
        public static SoloRoomDefinition SweeperRoom(float degreesPerTick) => Room(27.5f, new[] {
            Floor("Floor_W", 0f, DenX0),
            Floor("Den", DenX0, DenX1, -DenDepth),
            Floor("Floor_E", DenX1, 32f),
            E(SoloRoomElementKind.Sweeper, "Wave", (.5f, Ceiling * .5f), (.5f, .5f), settings: new SoloRoomTrapSettings(new SweeperSettings(WaveArm, degreesPerTick, WaveWarn),
                new SoloRoomTrapSettings(offset: new Vector2(31.4f, 0f), moveTicks: WaveMove, repeatMode: TrapRepeatMode.Periodic, periodTicks: WavePeriod, phaseTicks: WavePhase,
                    cooldownTicks: WaveWarn + WaveMove))),
        }, "Wave");
        public static RoomRoutes SweeperRoutes() => new(
            R0("hide in the den as it passes", Hold(Right), Until(XAtLeast(DenX0 + .8f)), Release(), Until(Grounded()), Until(Still()),
                Until(new RouteCondition("Wave past the den", v => v.Last.RoomLifeTick >= WavePhase + WaveWarn + WaveMove * 21 / 31)),
                Hold(Right), Jump(), Until(Airborne()), Until(Grounded()), Until(RoomComplete())),
            new Betrayal("T1 [W]: a cat that waits on the open floor is swept", "Wave", DeathCause.Hazard,
                R0("wait in the open", Hold(Right), Until(XAtLeast(11f)), Release(), Until(Dead()))));
    }
}
