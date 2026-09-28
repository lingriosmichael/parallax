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

        // ---------- PAX-091: the ways up the approach search didn't see ----------
        // Each room is entered from the checkpoint at x 30, so the fallback (the checkpoint's side) is Right; the upper
        // storey is reached only from its west end, by the one way up. Ledge_Spikes' trigger is a cut on the upper storey:
        // dangerBeyond puts the spikes beyond it as seen from the west (passes), otherwise before it (rejected).

        // Ledge x 6-20 (top 4), out of a jump's reach; Vent (x 4) launches the cat onto its west end.
        public static SoloRoomDefinition LaunchOnlyLedge(bool dangerBeyond) => Room(30f,
            E(SoloRoomElementKind.Floor, "Ledge", (13f, 3.75f), (14f, .5f)),
            E(SoloRoomElementKind.Geyser, "Vent", (4f, -.15f), (1f, .3f), settings: new SoloRoomTrapSettings(new GeyserSettings(),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: 150))),
            LedgeSpikes(dangerBeyond ? 18f : 9f, 4f, 14f));

        // The same ledge; Vine (x 5, y 0-5.5) is the way up, and a leap off its top stop lands on the ledge's west end.
        public static SoloRoomDefinition ClimbOnlyLedge(bool dangerBeyond) => Room(30f,
            E(SoloRoomElementKind.Floor, "Ledge", (13f, 3.75f), (14f, .5f)),
            E(SoloRoomElementKind.Vine, "Vine", (5f, 2.75f), (LevelLayoutValidator.VineWidth, 5.5f)),
            LedgeSpikes(dangerBeyond ? 18f : 9f, 4f, 14f));

        // Ledge x 10-25 (top 2.4) on Pillar x 10-11, out of a jump's reach from the floor. Spear_Step (fired by a cut at
        // x 28 as the cat sets off) sticks in Pillar's face: its shaft x 6.6-10 (top 1.2) is the step up.
        public static SoloRoomDefinition SpearOnlyLedge(bool dangerBeyond) => Room(30f,
            E(SoloRoomElementKind.Floor, "Ledge", (17.5f, 2.15f), (15f, .5f)),
            E(SoloRoomElementKind.Wall, "Pillar", (10.5f, .95f), (1f, 1.9f)),
            E(SoloRoomElementKind.Arrow, "Spear_Step", (1.25f, 1f), (.5f, .4f), (28f, 3.5f), (.5f, 7f),
                new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right, 1f, 10f, length: 3.4f), new SoloRoomTrapSettings(delayTicks: 0))),
            LedgeSpikes(dangerBeyond ? 20f : 13f, 2.4f, 16f));

        // Surface coverage on a second storey: the cat starts on the upper slab (top 4, x 0-32); Upper_Collapse, chained
        // from Upper_Spikes, whose trigger is a cut of the upper storey only (x 12, y 4-7), lies beyond it (x 17-19) or
        // before it (x 8-10).
        public static SoloRoomDefinition SecondStoreySurface(bool stripBeyond)
        {
            float cx = stripBeyond ? 18f : 9f;
            return Storeys(2f,
                E(SoloRoomElementKind.Floor, "Slab_L", ((cx - 1f) * .5f, 3f), (cx - 1f, 2f)),
                E(SoloRoomElementKind.CollapsingFloor, "Upper_Collapse", (cx, 3.5f), (2f, 1f),
                    settings: new SoloRoomTrapSettings(delayTicks: 10, triggerSource: TrapTriggerSource.Chain, chainSource: "Upper_Spikes")),
                E(SoloRoomElementKind.Floor, "Slab_R", ((cx + 1f + 32f) * .5f, 3f), (31f - cx, 2f)),
                E(SoloRoomElementKind.HiddenSpikes, "Upper_Spikes", (26f, 4.15f), (1f, .3f), (12f, 5.5f), (.5f, 3f), new SoloRoomTrapSettings(revealDelayTicks: 6)));
        }

        // A drop through a closed pit: the upper slab (top 4) has a pit at x 14-16, closed by Pit_Bottom, and an open hole
        // at x 28-30. The lower storey (floor 0, under the slab's underside 2) is entered only through the hole, from the
        // east, so Low_Spikes (x 18) lie beyond their cut (x 22). With a CollapsingFloor for the pit's floor, it may be gone
        // in a real run, so the drop through the pit counts and the spikes are reached from the west too (rejected).
        public static SoloRoomDefinition ClosedPitDrop(bool fixedFloor) => new(0, 0f, 32f, StoreyElements(2f,
            E(SoloRoomElementKind.Floor, "Slab_A", (7f, 3f), (14f, 2f)),
            fixedFloor ? E(SoloRoomElementKind.PitBottom, "Pit_Bottom", (15f, 2.25f), (2f, .5f))
                : E(SoloRoomElementKind.CollapsingFloor, "Pit_Bottom", (15f, 2.25f), (2f, .5f), settings: new SoloRoomTrapSettings(delayTicks: 20)),
            E(SoloRoomElementKind.Hazard, "Pit_Hazard", (15f, 2.65f), (2f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
            E(SoloRoomElementKind.Floor, "Slab_B", (22f, 3f), (12f, 2f)),
            E(SoloRoomElementKind.Floor, "Slab_C", (31f, 3f), (2f, 2f)),
            E(SoloRoomElementKind.HiddenSpikes, "Low_Spikes", (18f, .15f), (1f, .3f), (22f, 1f), (.5f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6))),
            new[] { O(SoloRoomOpeningKind.Pit, 14f, 16f, "Slab_A", "Slab_B", "Pit_Bottom", "Pit_Hazard") }, System.Array.Empty<RequiredJump>());

        // PAX-093 (D-095): the same ledge; Lift (x 3-5, standing on the floor, top 0.5), a Carry mover, rides up to the ledge's
        // height and back (Periodic), so a cat steps off it onto the ledge's west end.
        public static SoloRoomDefinition MoverOnlyLedge(bool dangerBeyond) => Room(30f,
            E(SoloRoomElementKind.Floor, "Ledge", (13f, 3.75f), (14f, .5f)),
            E(SoloRoomElementKind.MovingTrap, "Lift", (4f, .25f), (2f, .5f), settings: new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset: new UnityEngine.Vector2(0f, 3.5f), moveTicks: 40, holdTicks: 40, returnTicks: 40, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: 200, cooldownTicks: 120, movingKind: MovingTrapKind.Solid))),
            LedgeSpikes(dangerBeyond ? 18f : 9f, 4f, 14f));

        // PAX-093 (D-095): the vertical wall (PAX-091's L012 Orb_B finding). The upper slab (top 4, x 3.2-28) has an open hole at
        // x 28-30; west of it the Slot (x 1.4-3.2) drops into a pit. Col (x 3.2-3.7, y 0-2) under the slab's west end makes, with
        // the slab, a wall the full height of the lower hall, so a cat dropping down the Slot can't reach the hall (floor 0).
        // The hall is entered only through the hole, from the east: Low_Spikes (x 8) lie beyond their cut (x 12).
        public static SoloRoomDefinition VerticalWallDrop() => new(0, 0f, 32f, new[] {
            E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (10f, 4f), (0f, 0f)),
            E(SoloRoomElementKind.Wall, "Slot_W", (.7f, 1.5f), (1.4f, 11f)),
            E(SoloRoomElementKind.Floor, "Slab_A", (15.6f, 3f), (24.8f, 2f)),
            E(SoloRoomElementKind.Floor, "Slab_C", (31f, 3f), (2f, 2f)),
            E(SoloRoomElementKind.Wall, "Col", (3.45f, 1f), (.5f, 2f)),
            E(SoloRoomElementKind.Floor, "Hall", (17.6f, -.5f), (28.8f, 1f)),
            E(SoloRoomElementKind.PitBottom, "Slot_Bottom", (2.3f, -3.5f), (1.8f, 1f)),
            E(SoloRoomElementKind.Hazard, "Slot_Hazard", (2.3f, -2.85f), (1.8f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
            E(SoloRoomElementKind.Door, "Door", (31f, 4.75f), (.6f, 1.5f)),
            E(SoloRoomElementKind.HiddenSpikes, "Low_Spikes", (8f, .15f), (1f, .3f), (12f, 1f), (.5f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
        }, new[] { O(SoloRoomOpeningKind.Pit, 1.4f, 3.2f, "Slot_W", "Col", "Slot_Bottom", "Slot_Hazard") }, System.Array.Empty<RequiredJump>());

        // PAX-060 (L014 finding): a grab through a slab. The cat starts on the upper slab (top 4, x 3-28); the hall under it
        // (floor 0) is entered only through the open hole at x 28-30, from the east, so Low_Spikes (x 8) lie beyond their cut
        // (x 12). Vine (x 6, y 0.05-1.9) hangs in the hall under the slab: no way down to it passes the slab, so it isn't a way
        // into the hall from the west. With upperVine, Vine_Up (x 7.5) stands on the slab: a leap off it onto Vine is blocked too.
        public static SoloRoomDefinition GrabThroughSlab(bool upperVine) => new(0, 0f, 32f, new[] {
            E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (10f, 4f), (0f, 0f)),
            E(SoloRoomElementKind.Wall, "Wall_W", (1.5f, 3f), (3f, 8f)),
            E(SoloRoomElementKind.Floor, "Slab_A", (15.5f, 3f), (25f, 2f)),
            E(SoloRoomElementKind.Floor, "Slab_C", (31f, 3f), (2f, 2f)),
            E(SoloRoomElementKind.Floor, "Hall", (17.5f, -.5f), (29f, 1f)),
            E(SoloRoomElementKind.Vine, "Vine", (6f, .975f), (LevelLayoutValidator.VineWidth, 1.85f)),
            upperVine ? E(SoloRoomElementKind.Vine, "Vine_Up", (7.5f, 5.3f), (LevelLayoutValidator.VineWidth, 2.5f)) : E(SoloRoomElementKind.Floor, "Pad", (20f, 4.25f), (2f, .5f)),
            E(SoloRoomElementKind.Door, "Door", (31f, 4.75f), (.6f, 1.5f)),
            E(SoloRoomElementKind.HiddenSpikes, "Low_Spikes", (8f, .15f), (1f, .3f), (12f, 1f), (.5f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        // PAX-060 (L014 finding): the same room with a trough in the slab (x 5-7, floor 3, walls the slab's faces up to 4) over
        // Vine (x 5.2, reaching under Slab_W beside it): a cat standing in the trough can't pass the slab around it to reach Vine either.
        public static SoloRoomDefinition GrabFromTrough() => new(0, 0f, 32f, new[] {
            E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
            E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (10f, 4f), (0f, 0f)),
            E(SoloRoomElementKind.Wall, "Wall_W", (1.5f, 3f), (3f, 8f)),
            E(SoloRoomElementKind.Floor, "Slab_W", (4f, 3f), (2f, 2f)),
            E(SoloRoomElementKind.Floor, "Trough", (6f, 2.5f), (2f, 1f)),
            E(SoloRoomElementKind.Floor, "Slab_A", (17.5f, 3f), (21f, 2f)),
            E(SoloRoomElementKind.Floor, "Slab_C", (31f, 3f), (2f, 2f)),
            E(SoloRoomElementKind.Floor, "Hall", (17.5f, -.5f), (29f, 1f)),
            E(SoloRoomElementKind.Vine, "Vine", (5.2f, .975f), (LevelLayoutValidator.VineWidth, 1.85f)),
            E(SoloRoomElementKind.Door, "Door", (31f, 4.75f), (.6f, 1.5f)),
            E(SoloRoomElementKind.HiddenSpikes, "Low_Spikes", (8f, .15f), (1f, .3f), (12f, 1f), (.5f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        static SoloRoomElement LedgeSpikes(float x, float ledgeTop, float triggerX) =>
            E(SoloRoomElementKind.HiddenSpikes, "Ledge_Spikes", (x, ledgeTop + .15f), (1f, .3f), (triggerX, (ledgeTop + 7f) * .5f), (.5f, 7f - ledgeTop),
                new SoloRoomTrapSettings(revealDelayTicks: 6));

        // The Room layout with the checkpoint on an upper storey (y 4) and the door at the east end of it.
        static SoloRoomDefinition Storeys(float checkpointX, params SoloRoomElement[] extra) =>
            new(0, 0f, 32f, StoreyElements(checkpointX, extra), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        static SoloRoomElement[] StoreyElements(float checkpointX, params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (checkpointX, 4f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor_A", (16f, -.5f), (32f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (31f, 4.75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return elements.ToArray();
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
