using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Setup
{
    // PAX-076 (D-083): layouts for the precision, band, bait-gap and camera tell tests (PrecisionSectionTests,
    // DifficultyBandTests, BaitGapTests, CameraTellTests). The EditMode test assembly doesn't reference
    // Parallax.Editor, so these are built here and reached by reflection, like ArrowFixtures.
    public static class PrecisionFixtures
    {
        // A 2.2 gap between two floors: the required jump is 3.2 u, over D-056's 0.75 of the 3.92 u reach and within
        // the provisional 0.85. section: "none", "both" (both ends inside) or "takeoff" (straddles the region's edge).
        public static SoloRoomDefinition JumpRoom(string section)
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 7.5f, 20f, 1f),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                E(SoloRoomElementKind.Floor, "Floor_L", 2.5f, -.5f, 5f, 1f),
                E(SoloRoomElementKind.Floor, "Floor_R", 13.6f, -.5f, 12.8f, 1f),
                E(SoloRoomElementKind.Wall, "Pit_L", 4.5f, -2.5f, 1f, 3f),
                E(SoloRoomElementKind.Wall, "Pit_R", 7.7f, -2.5f, 1f, 3f),
                E(SoloRoomElementKind.PitBottom, "Pit_Bottom", 6.1f, -3.5f, 4.2f, 1f),
                new SoloRoomElement(SoloRoomElementKind.Hazard, "Pit_Hazard", new Vector2(6.1f, -2.85f), new Vector2(2.2f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 5f, 7.2f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            var jumps = new[] { new RequiredJump("Pit_Bottom", RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, 4.5f, 7.7f, 0f, 0f, 2f, sourceName: "Floor_L", destinationName: "Floor_R") };
            PrecisionSection[] sections = section switch
            {
                "both" => new[] { new PrecisionSection("Gap", Rect.MinMaxRect(3f, -1f, 9f, 2f)) },
                "takeoff" => new[] { new PrecisionSection("Gap", Rect.MinMaxRect(3f, -1f, 6f, 2f)) },
                _ => null,
            };
            return new SoloRoomDefinition(0, 0f, 20f, elements, openings, jumps, null, sections);
        }

        // A periodic spike patch whose safe window (period 96 - cooldown 54 = 42) clears its from-rest crossing
        // (31.7 ticks) by 10: enough at the provisional 8, not at D-056's 12.
        public static SoloRoomDefinition PeriodicRoom(bool marked)
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 7.5f, 20f, 1f),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                E(SoloRoomElementKind.Floor, "Floor", 10f, -.5f, 20f, 1f),
                new SoloRoomElement(SoloRoomElementKind.HiddenSpikes, "Spikes", new Vector2(10f, .15f), new Vector2(2f, .3f),
                    settings: new SoloRoomTrapSettings(revealDelayTicks: 6, repeatMode: TrapRepeatMode.Periodic, periodTicks: 96, cooldownTicks: 54)),
                E(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f),
            };
            PrecisionSection[] sections = marked ? new[] { new PrecisionSection("Spikes", Rect.MinMaxRect(7f, -1f, 13f, 3f)) } : null;
            return new SoloRoomDefinition(0, 0f, 20f, elements, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, sections);
        }

        // Two floors at the same height with a gap between their edges; the gap is declared as bait. At full reach
        // (1.0), with the edge take-off and the 5 coyote ticks, the cat covers 5.52 u edge to edge (+ one run tick
        // 0.12 of margin): a 5 u gap is crossable, a 6.5 u gap isn't.
        public static SoloRoomDefinition BaitRoom(float gap)
        {
            float right = 5f + gap;
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 7.5f, 20f, 1f),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                E(SoloRoomElementKind.Floor, "Floor_L", 2.5f, -.5f, 5f, 1f),
                E(SoloRoomElementKind.Floor, "Floor_R", (right + 20f) * .5f, -.5f, 20f - right, 1f),
                E(SoloRoomElementKind.Wall, "Pit_L", 4.5f, -2.5f, 1f, 3f),
                E(SoloRoomElementKind.Wall, "Pit_R", right + .5f, -2.5f, 1f, 3f),
                E(SoloRoomElementKind.PitBottom, "Pit_Bottom", (4f + right + 1f) * .5f, -3.5f, right + 1f - 4f, 1f),
                new SoloRoomElement(SoloRoomElementKind.Hazard, "Pit_Hazard", new Vector2((5f + right) * .5f, -2.85f), new Vector2(gap, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 5f, right, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            var baits = new[] { new BaitGap("Gap", 5f, 0f, right, 0f) };
            return new SoloRoomDefinition(0, 0f, 20f, elements, openings, System.Array.Empty<RequiredJump>(), null, null, baits);
        }

        // A flat room with one arrow firing left at the cat. The arrow fires when the cat crosses triggerX; its
        // launcher sits at launcherX. Wide (60) with the launcher far ahead of the trigger, the reveal is off screen
        // in follow mode; with the trigger moved near the launcher, it's on screen; narrow (18), the room is fit mode.
        public static SoloRoomDefinition CameraTellRoom(float width, float launcherX, float triggerX)
        {
            var arrow = new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left, .3f, 1f, unitsPerTick: .36f), new SoloRoomTrapSettings(delayTicks: 0));
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", width * .5f, 7.5f, width, 1f),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                E(SoloRoomElementKind.Floor, "Floor", width * .5f, -.5f, width, 1f),
                new SoloRoomElement(SoloRoomElementKind.Arrow, "ArrowX", new Vector2(launcherX, .3f), new Vector2(.5f, .4f), new Vector2(triggerX, 3.5f), new Vector2(.5f, 7f), arrow),
                E(SoloRoomElementKind.Door, "Door", width - 1f, .75f, .6f, 1.5f),
            };
            return new SoloRoomDefinition(0, 0f, width, elements, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        public static RoomRoutes CameraTellRoutes()
        {
            var run = new Route("run right", Hold(Right), Until(RoomComplete()));
            return new RoomRoutes(run, new Betrayal("ArrowX shoots a cat that runs on", "ArrowX", DeathCause.Hazard, new Route("run into ArrowX", Hold(Right), Until(Dead()))));
        }

        static SoloRoomElement E(SoloRoomElementKind kind, string name, float x, float y, float w, float h) => new(kind, name, new Vector2(x, y), new Vector2(w, h));
    }
}
