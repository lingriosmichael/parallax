using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Setup
{
    // PAX-073 (D-074): small synthetic rooms used only by the EditMode trigger-coverage and
    // platform-size tests. Not used by any menu and not part of LevelLayouts (not real levels).
    // Every room is 32 wide with a floor top at 0 and a ceiling underside at 7 unless noted.
    public static class TriggerCoverageFixtures
    {
        // A falling ceiling block at x 20 whose trigger sits at floor height only (y 0-1).
        public static SoloRoomDefinition FloorHeightTrigger() => Room(2f, Block("Ceiling_Block", 20f, (18f, .5f), (1f, 1f)));

        // The same block with a full floor-to-ceiling curtain (y 0-7) before it.
        public static SoloRoomDefinition FullCurtain() => Room(2f, Block("Ceiling_Block", 20f, (18f, 3.5f), (1f, 7f)));

        // Approach from the left: the checkpoint is at x 2 and the block at x 10 lies before the curtain at x 14.
        public static SoloRoomDefinition CheckpointLeftDangerBeforeCurtain() => Room(2f, Block("Ceiling_Block", 10f, (14f, 3.5f), (.5f, 7f)));

        // Approach from the right: the checkpoint is at x 30 and the block at x 12 lies beyond the curtain at x 14.
        public static SoloRoomDefinition CheckpointRightDangerBeyondCurtain() => Room(30f, Block("Ceiling_Block", 12f, (14f, 3.5f), (.5f, 7f)));

        // Approach from the right: the checkpoint is at x 30 and the block at x 20 lies before the curtain at x 14.
        public static SoloRoomDefinition CheckpointRightDangerBeforeCurtain() => Room(30f, Block("Ceiling_Block", 20f, (14f, 3.5f), (.5f, 7f)));

        // A room with a gravity flip, whose trigger covers only the gravity-down jump band (y 0-3.76)
        // and leaves the ceiling-side band a gravity-up cat occupies (y 3.76-7) uncovered.
        public static SoloRoomDefinition GravityUpBandUncovered() => Room(2f,
            E(SoloRoomElementKind.GravityFlip, "Flip_A", (8f, 3f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            Block("Ceiling_Block", 20f, (18f, 1.88f), (1f, 3.76f)));

        // The gravity-down jump band is uncovered: the trigger spans only y 3.24-7 (the ceiling side).
        public static SoloRoomDefinition CeilingSideBandOnly() => Room(2f, Block("Ceiling_Block", 20f, (18f, 5.12f), (1f, 3.76f)));

        public static SoloRoomDefinition LearnedBypass(string reason) => Room(2f, Block("Ceiling_Block", 20f, (18f, .5f), (1f, 1f), reason));

        // A chained block whose danger (x 9.5-10.5) lies before its root's curtain (x 15.75-16.25).
        public static SoloRoomDefinition ChainDescendantBeforeRootCurtain() => Room(2f,
            E(SoloRoomElementKind.HiddenSpikes, "Spikes_Root", (24f, .15f), (1f, .3f), (16f, 3.5f), (.5f, 7f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
            E(SoloRoomElementKind.FallingBlock, "Block_Early", (10f, 6f), (1f, 1f), settings: new SoloRoomTrapSettings(delayTicks: 12, unitsPerTick: .3f, travelDistance: 5.5f, triggerSource: TrapTriggerSource.Chain, chainSource: "Spikes_Root")));

        // The floor right of an unjumpable moat (x 17-32) is entered only through Flip_B. The drop
        // spikes' trigger either equals Flip_B (passes) or sits elsewhere (rejected).
        public static SoloRoomDefinition FlipEntry(bool triggerContainsFlip) => Room(2f,
            E(SoloRoomElementKind.GravityFlip, "Flip_A", (8f, 3f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.Hazard, "Moat", (14f, .15f), (6f, .3f), hazardRole: SoloRoomHazardRole.UnjumpableFloor),
            E(SoloRoomElementKind.GravityFlip, "Flip_B", (26f, 4f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.HiddenSpikes, "Drop_Spikes", (25f, .15f), (1f, .3f), triggerContainsFlip ? (26f, 4f) : (29f, 4f), (1f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)));

        // The FlipEntry(true) room, but the danger hangs from the ceiling, within reach of a
        // gravity-up cat walking the ceiling over the moat without touching any flip.
        public static SoloRoomDefinition FlipEntryCeilingDanger() => Room(2f,
            E(SoloRoomElementKind.GravityFlip, "Flip_A", (8f, 3f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.Hazard, "Moat", (14f, .15f), (6f, .3f), hazardRole: SoloRoomHazardRole.UnjumpableFloor),
            E(SoloRoomElementKind.GravityFlip, "Flip_B", (26f, 4f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.HiddenSpikes, "Ceiling_Drop", (25f, 6.85f), (1f, .3f), (26f, 4f), (1f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)));

        // A pit x 16-22 with a standable Floor sunk inside it (top -1): a curtain from y 0 misses a
        // cat standing on it.
        public static SoloRoomDefinition SunkPlatformInPit() => new(0, 0f, 32f, new[] {
            E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
            E(SoloRoomElementKind.Floor, "Floor_L", (8f, -.5f), (16f, 1f)),
            E(SoloRoomElementKind.Floor, "Floor_R", (27f, -.5f), (10f, 1f)),
            E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            E(SoloRoomElementKind.Wall, "Pit_L", (15.5f, -2.5f), (1f, 3f)),
            E(SoloRoomElementKind.Wall, "Pit_R", (22.5f, -2.5f), (1f, 3f)),
            E(SoloRoomElementKind.PitBottom, "Pit_Bottom", (19f, -3.5f), (6f, 1f)),
            E(SoloRoomElementKind.Hazard, "Pit_Hazard", (19f, -2.85f), (6f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
            E(SoloRoomElementKind.Floor, "Sunk", (19f, -1.25f), (2f, .5f)),
            Block("Ceiling_Block", 26f, (19f, 3.5f), (.5f, 7f)),
        }, new[] { O(SoloRoomOpeningKind.Pit, 16f, 22f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") }, System.Array.Empty<RequiredJump>());

        public static SoloRoomDefinition NoCeiling() => new(0, 0f, 32f, new[] {
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
            E(SoloRoomElementKind.Floor, "Floor_A", (16f, -.5f), (32f, 1f)),
            E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            Block("Ceiling_Block", 20f, (18f, 3.5f), (1f, 7f)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        // One floating Floor named "Thin" of the given size, above a full-width ground floor.
        public static SoloRoomDefinition WithPlatform(float width, float thickness) => Room(2f,
            E(SoloRoomElementKind.Floor, "Thin", (10f, 2f), (width, thickness)));

        // TrapLabLayout room 0 reduced to its "ThinPlatform" Floor (R10), for the build test.
        // No elements when the lab has no such platform.
        public static SoloRoomDefinition TrapLabThinPlatformOnly()
        {
            SoloRoomDefinition lab = TrapLabLayout.Rooms[0];
            var elements = new List<SoloRoomElement>();
            foreach (SoloRoomElement e in lab.Elements) if (e.Name == "ThinPlatform") elements.Add(e);
            return new SoloRoomDefinition(lab.Id, lab.Origin.x, lab.Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        static SoloRoomElement Block(string name, float x, (float x, float y) triggerCentre, (float x, float y) triggerSize, string learnedBypassReason = null) =>
            E(SoloRoomElementKind.FallingBlock, name, (x, 6f), (1f, 1f), triggerCentre, triggerSize,
                new SoloRoomTrapSettings(delayTicks: 6, unitsPerTick: .3f, travelDistance: 5.5f, learnedBypassReason: learnedBypassReason));

        static SoloRoomDefinition Room(float checkpointX, params SoloRoomElement[] extra)
        {
            float doorX = checkpointX < 16f ? 30f : 2f;
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (checkpointX, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor_A", (16f, -.5f), (32f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (doorX, .75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }
    }
}
