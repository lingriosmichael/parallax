using System.Collections.Generic;
using Parallax.Core;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Setup
{
    // PAX-074 (D-078): small synthetic rooms used only by the EditMode arrow tests. Not used by any
    // menu and not part of LevelLayouts. Every room is 32 wide, floor top 0, ceiling underside 7,
    // checkpoint x 2, door x 30, with the TrapLab room 3 geometry: ShieldA x[7,8] y[0,1], StopA
    // x[14,14.5] y[0,1], PillarB x[22,23] y[0,2.4], Backboard x[15,15.5] y[3,7], Overhang
    // x[27,27.5] y[3,7].
    public static class ArrowFixtures
    {
        static readonly (float, float) LauncherSize = (.5f, .4f);

        // ---------- coverage (R5) ----------

        // F1: ArrowB behind a cut at x[17,17.5]; the lane x[14.5,22] extends 4.5 beyond the near edge.
        public static SoloRoomDefinition CoverageF1_CutWithLaneBeyond() => Room(ArrowB((17.25f, 3.5f), (.5f, 7f)));

        // F2: the cut moved to x[23.5,24]: the whole lane lies before the near edge.
        public static SoloRoomDefinition CoverageF2_CutBeyondTheLane() => Room(ArrowB((23.75f, 3.5f), (.5f, 7f)));

        // F3: a floor-height trigger x[17,17.5] y[0,1]: not a cut and not containing the lane.
        public static SoloRoomDefinition CoverageF3_NotACut() => Room(ArrowB((17.25f, .5f), (.5f, 1f)));

        // F4: ArrowC with a jump-arc trigger x[15.5,27] y[3.1,3.5] containing its lane x[15.5,27] y[3.22,3.38].
        public static SoloRoomDefinition CoverageF4_JumpArcContainsLane() => Room(ArrowC((21.25f, 3.3f), (11.5f, .4f)));

        // F5: ArrowC with a jump-arc trigger x[18,24]: neither a cut nor containing the lane.
        public static SoloRoomDefinition CoverageF5_JumpArcMissesLane() => Room(ArrowC((21f, 3.3f), (6f, .4f)));

        // F6: a cut at x[21.5,22] whose near edge is 0.5 before ArrowB's mouth (x 22): the lane extends
        // only 0.5 beyond it, less than one collider width.
        public static SoloRoomDefinition CoverageF6_CutTooCloseToTheMouth() => Room(ArrowB((21.75f, 3.5f), (.5f, 7f)));

        // F7: F4 mirrored for gravity up: Backboard and Overhang stand on the floor (y[0,4]), PillarB
        // hangs from the ceiling (y[4.6,7]), the lane runs at y 3.7 and the trigger x[15.5,27] y[3.5,3.9]
        // contains it.
        public static SoloRoomDefinition CoverageF7_JumpArcContainsLaneGravityUp() => new(0, 0f, 32f, Frame(
            E(SoloRoomElementKind.Wall, "Backboard", (15.25f, 2f), (.5f, 4f)),
            E(SoloRoomElementKind.Wall, "Overhang", (27.25f, 2f), (.5f, 4f)),
            E(SoloRoomElementKind.Wall, "PillarB", (22.5f, 5.8f), (1f, 2.4f)),
            Arrow("ArrowC", (27.25f, 3.7f), new ArrowLane(ArrowDirection.Left, 3.7f, 15.5f), Once(), (21.25f, 3.7f), (11.5f, .4f))),
            System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        // ---------- the six rules (R6) ----------

        // A well-formed room with all three arrows (the TrapLab room 3 set, rebuilt here so the
        // fixtures don't depend on the lab data).
        public static SoloRoomDefinition AllRulesPass() => Room(ArrowA(), ArrowB((17.25f, 3.5f), (.5f, 7f)), ArrowC((21.25f, 3.3f), (11.5f, .4f)));

        public static SoloRoomDefinition TellBelowSix() => Room(Arrow("ArrowB", (22.25f, .3f), new ArrowLane(ArrowDirection.Left, .3f, 14.5f, tellTicks: 5), Once(), (17.25f, 3.5f), (.5f, 7f)));

        public static SoloRoomDefinition SpeedAt(float unitsPerTick) => Room(Arrow("ArrowB", (22.25f, .3f), new ArrowLane(ArrowDirection.Left, .3f, 14.5f, unitsPerTick: unitsPerTick), Once(), (17.25f, 3.5f), (.5f, 7f)));

        // A Wall x[18,19] y[0,1] stands in ArrowB's lane.
        public static SoloRoomDefinition LaneBlocked() => Room(ArrowB((17.25f, 3.5f), (.5f, 7f)), E(SoloRoomElementKind.Wall, "Crate", (18.5f, .5f), (1f, 1f)));

        // ArrowB's lane ends at x 16, in open air.
        public static SoloRoomDefinition LaneEndInOpenAir() => Room(Arrow("ArrowB", (22.25f, .3f), new ArrowLane(ArrowDirection.Left, .3f, 16f), Once(), (17.25f, 3.5f), (.5f, 7f)));

        // A launcher on a post x[26,26.5] y[0,1] fires right to the room's end (x 32), through the door x[29.7,30.3].
        public static SoloRoomDefinition LaneThroughTheDoor() => Room(
            E(SoloRoomElementKind.Wall, "Post", (26.25f, .5f), (.5f, 1f)),
            Arrow("ArrowD", (26.25f, .3f), new ArrowLane(ArrowDirection.Right, .3f, 32f), Once(), (24f, 3.5f), (.5f, 7f)));

        // ArrowA (tell 6 + 19 lethal ticks = 25) with a Periodic cooldown of 20.
        public static SoloRoomDefinition CooldownBeforeTheArrowStops() => Room(ArrowA(cooldownTicks: 20));

        // ArrowA with period 90: window 90 - 25 = 65 against a from-rest crossing of 65 + 12.
        public static SoloRoomDefinition PeriodicSlackBelowTwelve() => Room(ArrowA(periodTicks: 90));

        // ---------- chain source (R8) ----------

        // ArrowB is the chain source of a falling block 6 ticks later.
        public static SoloRoomDefinition ArrowChainSource() => Room(ArrowB((17.25f, 3.5f), (.5f, 7f)),
            E(SoloRoomElementKind.FallingBlock, "Block_After", (10f, 6f), (1f, 1f), settings: new SoloRoomTrapSettings(delayTicks: 6, unitsPerTick: .3f, travelDistance: 5.5f, triggerSource: TrapTriggerSource.Chain, chainSource: "ArrowB")));

        // ---------- helpers ----------

        static SoloRoomElement ArrowA(int periodTicks = 120, int cooldownTicks = 60) =>
            Arrow("ArrowA", (7.75f, .3f), new ArrowLane(ArrowDirection.Right, .3f, 14f),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: periodTicks, phaseTicks: 30, cooldownTicks: cooldownTicks));

        static SoloRoomElement ArrowB((float x, float y) trigger, (float x, float y) triggerSize) =>
            Arrow("ArrowB", (22.25f, .3f), new ArrowLane(ArrowDirection.Left, .3f, 14.5f, disguised: true), Once(), trigger, triggerSize);

        static SoloRoomElement ArrowC((float x, float y) trigger, (float x, float y) triggerSize) =>
            Arrow("ArrowC", (27.25f, 3.3f), new ArrowLane(ArrowDirection.Left, 3.3f, 15.5f), Once(), trigger, triggerSize);

        static SoloRoomTrapSettings Once() => new SoloRoomTrapSettings(delayTicks: 0);

        static SoloRoomElement Arrow(string name, (float x, float y) launcher, ArrowLane lane, SoloRoomTrapSettings timing, (float x, float y) trigger = default, (float x, float y) triggerSize = default) =>
            E(SoloRoomElementKind.Arrow, name, launcher, LauncherSize, trigger, triggerSize, new SoloRoomTrapSettings(lane, timing));

        static SoloRoomDefinition Room(params SoloRoomElement[] extra) => new(0, 0f, 32f, Frame(extra), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        static SoloRoomElement[] Frame(params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint_0", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor_A", (16f, -.5f), (32f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            };
            bool mirrored = false;
            foreach (SoloRoomElement e in extra) if (e.Name == "Backboard" && e.Position.y < 3f) mirrored = true;
            if (!mirrored)
            {
                elements.Add(E(SoloRoomElementKind.Wall, "ShieldA", (7.5f, .5f), (1f, 1f)));
                elements.Add(E(SoloRoomElementKind.Wall, "StopA", (14.25f, .5f), (.5f, 1f)));
                elements.Add(E(SoloRoomElementKind.Wall, "PillarB", (22.5f, 1.2f), (1f, 2.4f)));
                elements.Add(E(SoloRoomElementKind.Wall, "Backboard", (15.25f, 5f), (.5f, 4f)));
                elements.Add(E(SoloRoomElementKind.Wall, "Overhang", (27.25f, 5f), (.5f, 4f)));
            }
            elements.AddRange(extra);
            return elements.ToArray();
        }
    }
}
