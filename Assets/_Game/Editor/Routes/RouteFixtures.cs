using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    public sealed class RouteCase
    {
        public string Name; public SoloRoomDefinition Room; public Route Route;
        public RouteCase(string name, SoloRoomDefinition room, Route route) { Name = name; Room = room; Route = route; }
    }

    // PAX-075 (D-079): rooms and routes the harness tests replay. Rooms are built from layout data
    // exactly like shipped rooms; copies of shipped rooms change only the named value.
    public static class RouteFixtures
    {
        // ---------- synthetic rooms ----------

        public static SoloRoomDefinition FlatRoom() => Room(60f,
            El(SoloRoomElementKind.Floor, "Floor", 30f, -.5f, 60f, 1f),
            El(SoloRoomElementKind.Door, "Door", 58f, .75f, .6f, 1.5f));

        // A ledge ending at x 10, with a floor 3 u lower beyond it.
        public static SoloRoomDefinition LedgeRoom() => Room(60f,
            El(SoloRoomElementKind.Floor, "Floor_High", 5f, -.5f, 10f, 1f),
            El(SoloRoomElementKind.Floor, "Floor_Low", 35f, -3.5f, 50f, 1f),
            El(SoloRoomElementKind.Door, "Door", 58f, -2.25f, .6f, 1.5f));

        static SoloRoomDefinition Room(float width, params SoloRoomElement[] elements) =>
            new(0, 0f, width, new[] {
                El(SoloRoomElementKind.Ceiling, "Ceiling", width * .5f, 7.5f, width, 1f),
                El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f) }.Concat(elements).ToArray(),
                Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());

        static SoloRoomElement El(SoloRoomElementKind kind, string name, float x, float y, float w, float h) =>
            new(kind, name, new Vector2(x, y), new Vector2(w, h));

        // ---------- motion cases (R11) ----------

        public static RouteCase FlatRun() => new("flat run", FlatRoom(), new Route("run", Hold(Right), For(30), Release(), For(15)));
        public static RouteCase FlatApex() => new("flat apex", FlatRoom(), new Route("apex", For(5), Jump(), For(60)));
        public static RouteCase FlatJump() => new("flat jump", FlatRoom(), new Route("flat jump", Hold(Right), For(20), Jump(), For(70)));
        public static RouteCase Coyote(int airborneStep) => new($"coyote {airborneStep}", LedgeRoom(),
            new Route("coyote", Hold(Right), Until(Airborne()), For(airborneStep - 1), Jump(), For(3)));
        // First press on tick 4; the second on tick 4 + gap (0 = no second press).
        public static RouteCase Buffer(int gap) => new($"buffer {gap}", FlatRoom(), gap <= 0
            ? new Route("buffer base", For(3), Jump(), For(80))
            : new Route("buffer", For(3), Jump(), For(gap), Jump(), For(30)));

        // ---------- route-format cases (§5.5) ----------

        public static RouteCase FormatHoldFor() => new("hold for", FlatRoom(), new Route("hold for", Hold(Right), For(3)));
        public static RouteCase FormatJumpEdge() => new("jump edge", FlatRoom(), new Route("jump edge", Hold(Right), For(2), Jump(), For(3)));
        public static RouteCase FormatUntil(float x) => new("until", FlatRoom(), new Route("until", Hold(Right), Until(XAtLeast(x)), Release(), For(2)));
        public static RouteCase FormatTimed() => new("timed", FlatRoom(), new Route("timed", Hold(Right), For(5), Jump().Timed(TimedMode.Shift), For(5)));
        public static RouteCase FormatMargin() => new("margin", FlatRoom(), new Route("margin",
            Margin("start to x 1", Grounded(), XAtLeast(2.6f), 0), Hold(Right), Until(XAtLeast(2.6f)), For(1)));
        // R17: a Flip zone at the spawn sends the cat to the ceiling; then it holds right.
        public static RouteCase FormatScreenRight() => new("screen right", Room(60f,
                El(SoloRoomElementKind.Floor, "Floor", 30f, -.5f, 60f, 1f),
                new SoloRoomElement(SoloRoomElementKind.GravityFlip, "Flip", new Vector2(2f, 1f), new Vector2(1f, 2f), settings: new SoloRoomTrapSettings(gravityMode: GravityFlipMode.Flip, rendererEnabled: true)),
                El(SoloRoomElementKind.Door, "Door", 58f, .75f, .6f, 1.5f)),
            new Route("screen right", Until(GroundedOn("Ceiling")), Hold(Right), For(20)));
        public static Route FormatPrefix(Route source, string through) => Route.PrefixOf(source, through, "prefix", Release(), For(2));

        // ---------- shipped-room fidelity (R11) ----------

        // PAX-078 R3: L002's Lift trigger, x centre as given (shipped 19.80). PAX-059: the lift room is a frozen copy of
        // the pre-PAX-059 L002 (LiftRoom), since these cases test the harness, not the level.
        public static SoloRoomDefinition L002WithLiftTriggerX(float x) =>
            Replace(LiftRoom(), "Lift", e => new SoloRoomElement(e.Kind, e.Name, e.Position, e.Size, new Vector2(x, e.SecondaryPosition.y), e.SecondarySize, e.Settings, e.HazardRole));

        public static RouteCase L002LiftApproach(float triggerX) => new($"L002 lift approach {triggerX}", L002WithLiftTriggerX(triggerX), new Route("lift approach",
            Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Platform_B")),
            Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("Floor_C")),
            Until(GroundedOn("Lift")), Until(Fired("Lift")), For(1)));

        // PAX-059: the pre-PAX-059 L002 solution as far as the Receiver, with its Lift margin, for LiftRoom.
        public static Route LiftSolution() => new("lift solution",
            Margin("Lift landing to Lift fire", GroundedOn("Lift"), Fired("Lift"), 12),
            Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Platform_B")),
            Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("Floor_C")),
            Until(GroundedOn("Lift")), Until(GroundedOn("Receiver")));

        // L004 with its checkpoint on FalseLanding's left end, so replays start at the SourceSpikes stretch. PAX-059: the
        // pre-PAX-059 L004, frozen as FastFlipRoom.
        public static SoloRoomDefinition L004FromFalseLanding() =>
            Replace(FastFlipRoom(), "Checkpoint_3", e => new SoloRoomElement(e.Kind, e.Name, new Vector2(19.6f, 0f), e.Size, e.SecondaryPosition, e.SecondarySize, e.Settings, e.HazardRole));

        // PAX-078 R4/R10: hold right and hop SourceSpikes into Flip_A; survive = on the ceiling once Block_2 has fired.
        public static RouteCondition OnCeilingAfterBlock2() => Grounded().And(GravityUp()).And(Fired("Block_2"));
        public static RouteCase L004FastFlip(float takeoffX) => new($"L004 fast flip {takeoffX}", L004FromFalseLanding(), new Route("fast flip", OnCeilingAfterBlock2(),
            Hold(Right), Until(XAtLeast(takeoffX)), Jump().Timed(TimedMode.Shift), Until(OnCeilingAfterBlock2())));

        // R18.3: stop before SourceSpikes, then hop into Flip_A from rest.
        public static RouteCase L004StopThenHop(float stopX) => new($"L004 stop at {stopX} then hop", L004FromFalseLanding(), new Route("stop then hop", OnCeilingAfterBlock2(),
            Hold(Right), Until(XAtLeast(stopX)), Release(), Until(Still()), Hold(Right), Jump(), Until(OnCeilingAfterBlock2())));

        // ---------- validator fixtures (§5.3, §5.4) ----------

        public static SoloRoomDefinition TrapLabRoom3WithArrowBTell(int tell) =>
            Replace(TrapLabLayout.Rooms[3], "ArrowB", e =>
            {
                ArrowLane a = e.Settings.Arrow;
                var lane = new ArrowLane(a.Direction, a.LaneY, a.LaneEndX, a.Length, a.Thickness, a.UnitsPerTick, tell, a.Disguised);
                return new SoloRoomElement(e.Kind, e.Name, e.Position, e.Size, e.SecondaryPosition, e.SecondarySize, new SoloRoomTrapSettings(lane, e.Settings), e.HazardRole);
            });

        // ---------- PAX-080 (D-080) ----------

        // A 10 u floor, then a 2 u fake platform at floor height over a pit (hazard at its bottom), then floor again.
        public static SoloRoomDefinition FakeOverPitRoom(bool solid = false) => Room(30f,
            El(SoloRoomElementKind.Floor, "Floor_Left", 5f, -.5f, 10f, 1f),
            El(solid ? SoloRoomElementKind.Floor : SoloRoomElementKind.FakePlatform, "Fake", 11f, -.25f, 2f, .5f),
            El(SoloRoomElementKind.PitBottom, "Pit_Bottom", 15f, -3.5f, 10f, 1f),
            El(SoloRoomElementKind.Hazard, "Pit_Hazard", 15f, -2.85f, 10f, .3f),
            El(SoloRoomElementKind.Floor, "Floor_Right", 25f, -.5f, 10f, 1f),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // A 10 u ledge 2 u up ending in a 2 u fake platform, over a floor that runs to the door: stepping on the
        // fake drops the cat somewhere safe.
        public static SoloRoomDefinition FakeOverFloorRoom(bool solid = false) => Room(30f,
            El(SoloRoomElementKind.Floor, "Ledge", 5f, 1.75f, 10f, .5f),
            El(solid ? SoloRoomElementKind.Floor : SoloRoomElementKind.FakePlatform, "Fake", 11f, 1.75f, 2f, .5f),
            El(SoloRoomElementKind.Floor, "Floor", 15f, -.5f, 30f, 1f),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f)).WithCheckpointAt(2f, 2f);

        public static Route WalkRightUntilDead() => new("walk right until dead", Hold(Right), Until(Dead()));
        public static Route WalkRightToTheDoor() => new("walk right to the door", Hold(Right), Until(RoomComplete()));
        // Runs right, brakes once the collider centre passes x, and waits: from 8.9 its right edge stops short of
        // x 9.95 (the fake's edge minus its touch skin); from 9.6 it stops inside it.
        public static Route BrakeFrom(float x) => new("brake from " + x.ToString(System.Globalization.CultureInfo.InvariantCulture), Hold(Right), Until(XAtLeast(x)), Release(), Until(Still()), For(60));
        // Alive and never done: the 600-tick step cap, and the 1500-tick replay cap.
        public static Route IdleAtTheStepCap() => new("idle at the step cap", Until(XAtLeast(100f)));
        public static Route IdleAtTheTickCap() => new("idle at the tick cap", For(2000));

        // R3: a Solid platform over a pit that slides 3 u left when its trigger fires. The trigger at x 8.5-9 is a
        // full-height cut (y 0-7), or leaves a gap: y 2-5.
        public static SoloRoomDefinition MovingAwaySolidRoom(bool gap) => Room(30f,
            El(SoloRoomElementKind.Floor, "Floor_Left", 5f, -.5f, 10f, 1f),
            new SoloRoomElement(SoloRoomElementKind.MovingTrap, "Slide", new Vector2(11.5f, -.5f), new Vector2(3f, 1f), new Vector2(8.75f, 3.5f), new Vector2(.5f, gap ? 3f : 7f),
                new SoloRoomTrapSettings(offset: new Vector2(-3f, 0f), moveTicks: 24, movingKind: MovingTrapKind.Solid)),
            El(SoloRoomElementKind.PitBottom, "Pit_Bottom", 16.5f, -3.5f, 13f, 1f),
            El(SoloRoomElementKind.Hazard, "Pit_Hazard", 16.5f, -2.85f, 13f, .3f),
            El(SoloRoomElementKind.Floor, "Floor_Right", 26.5f, -.5f, 7f, 1f),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // R3: contact-triggered surfaces only (a fake platform and an Overlap collapsing floor).
        public static SoloRoomDefinition ContactSurfacesRoom() => Room(30f,
            El(SoloRoomElementKind.Floor, "Floor_Left", 5f, -.5f, 10f, 1f),
            El(SoloRoomElementKind.FakePlatform, "Fake", 11f, -.25f, 2f, .5f),
            new SoloRoomElement(SoloRoomElementKind.CollapsingFloor, "Collapse", new Vector2(14f, -.5f), new Vector2(2f, 1f), settings: new SoloRoomTrapSettings(delayTicks: 12)),
            El(SoloRoomElementKind.Floor, "Floor_Right", 25f, -.5f, 10f, 1f),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // R1: a fake platform whose data disagrees with the builder (a 5-tick delay), and one with a trigger box.
        public static SoloRoomDefinition FakeWithDelayRoom() => Room(30f,
            new SoloRoomElement(SoloRoomElementKind.FakePlatform, "Fake", new Vector2(11f, -.25f), new Vector2(2f, .5f), settings: new SoloRoomTrapSettings(delayTicks: 5)),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));
        public static SoloRoomDefinition FakeWithTriggerBoxRoom() => Room(30f,
            new SoloRoomElement(SoloRoomElementKind.FakePlatform, "Fake", new Vector2(11f, -.25f), new Vector2(2f, .5f), new Vector2(11f, 1f), new Vector2(1f, 1f)),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // R1 limit: a FallingBlock chained from a fake platform (rejected: a fake is not a chain source), or, as the
        // control, from an Overlap collapsing floor of the same rect (accepted).
        public static SoloRoomDefinition ChainedFromRoom(bool fromFake) => Room(30f,
            new SoloRoomElement(fromFake ? SoloRoomElementKind.FakePlatform : SoloRoomElementKind.CollapsingFloor, "Source", new Vector2(11f, -.25f), new Vector2(2f, .5f),
                settings: fromFake ? default : new SoloRoomTrapSettings(delayTicks: 12)),
            new SoloRoomElement(SoloRoomElementKind.FallingBlock, "Block", new Vector2(14f, 6f), new Vector2(1f, 1f),
                settings: new SoloRoomTrapSettings(delayTicks: 6, unitsPerTick: .3f, travelDistance: 5.5f, triggerSource: TrapTriggerSource.Chain, chainSource: "Source")),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // R1: a RequiredJump from a floor onto a fake platform (named, or found by its landing point).
        public static SoloRoomDefinition JumpOntoFakeRoom(bool named)
        {
            SoloRoomDefinition room = FakeOverPitRoom();
            var jump = new RequiredJump("Pit_Bottom", RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, 9.5f, 10.5f, 0f, 0f, 3f,
                sourceName: "Floor_Left", destinationName: named ? "Fake" : null);
            return new SoloRoomDefinition(room.Id, room.Origin.x, room.Width, room.Elements, room.Openings, new[] { jump });
        }

        // R5: Trap Lab room 4 with Stone_A made a solid Floor of the same rect.
        public static SoloRoomDefinition TrapLabRoom4WithSolidStoneA() =>
            Replace(TrapLabLayout.Rooms[4], "Stone_A", e => new SoloRoomElement(SoloRoomElementKind.Floor, e.Name, e.Position, e.Size, e.SecondaryPosition, e.SecondarySize, e.Settings, e.HazardRole));

        // ---------- PAX-059: band 1 (C1, C3, ValidateBand1Content, tells) ----------

        // C1: a ledge (top 10) whose door stands above the old frame's top (y 8), under a ceiling at 14, with the room's
        // own side walls; strayHazard adds a hazard above all of the room's geometry.
        public static SoloRoomDefinition TallRoom(bool strayHazard = false)
        {
            var elements = new System.Collections.Generic.List<SoloRoomElement> {
                El(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 14.5f, 20f, 1f),
                El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                El(SoloRoomElementKind.Floor, "Ground", 10f, -.5f, 20f, 1f),
                El(SoloRoomElementKind.Floor, "Ledge", 16f, 9.5f, 8f, 1f),
                El(SoloRoomElementKind.Wall, "Wall_L", -.5f, 7f, 1f, 16f),
                El(SoloRoomElementKind.Wall, "Wall_R", 20.5f, 7f, 1f, 16f),
                El(SoloRoomElementKind.Door, "Door", 18f, 10.75f, .6f, 1.5f) };
            if (strayHazard) elements.Add(El(SoloRoomElementKind.Hazard, "Stray", 10f, 16f, 1f, .3f));
            return new SoloRoomDefinition(0, 0f, 20f, elements.ToArray(), Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());
        }

        // C1 (review): TallRoom with a hazard sunk inside the ground slab (y -0.65 to -0.35), below every floor top.
        public static SoloRoomDefinition SlabHazardRoom() => new(0, 0f, 20f,
            TallRoom().Elements.Append(El(SoloRoomElementKind.Hazard, "Sunk", 10f, -.5f, 1f, .3f)).ToArray(),
            Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());

        // ValidateTrapFloorHeadroom: a flat floor (top 0) with a collapsing ledge (x 9-11, 0.5 thick) whose underside is at
        // the given height. A jump from the floor reaches y 2.16 with the cat's head.
        public static SoloRoomDefinition TrapLedgeRoom(float underside) => Room(20f,
            El(SoloRoomElementKind.Floor, "Floor", 10f, -.5f, 20f, 1f),
            El(SoloRoomElementKind.CollapsingFloor, "Trap_Ledge", 10f, underside + .25f, 2f, .5f));

        // Surface coverage by a trigger over the top: a ledge (top 3.5, x 9-11) that gives way, chained from spikes on the
        // floor under it, whose trigger stands on the ledge's top and is the given height (the cat is 0.56 tall).
        public static SoloRoomDefinition TopTriggerLedgeRoom(float triggerHeight) => Room(20f,
            El(SoloRoomElementKind.Floor, "Floor", 10f, -.5f, 20f, 1f),
            new SoloRoomElement(SoloRoomElementKind.HiddenSpikes, "Spikes", new Vector2(10f, .15f), new Vector2(2f, .3f),
                new Vector2(10f, 3.5f + triggerHeight * .5f), new Vector2(2f, triggerHeight), new SoloRoomTrapSettings(revealDelayTicks: 6)),
            new SoloRoomElement(SoloRoomElementKind.CollapsingFloor, "Ledge", new Vector2(10f, 3.25f), new Vector2(2f, .5f),
                settings: new SoloRoomTrapSettings(triggerSource: TrapTriggerSource.Chain, chainSource: "Spikes", delayTicks: 1)));

        // ValidateTriggerNearTrap: a block in the ceiling at x 14.5-15.5 whose trigger (0.5 wide, full height) is at triggerX.
        public static SoloRoomDefinition BlockTriggerRoom(float triggerX) => Room(20f,
            El(SoloRoomElementKind.Floor, "Floor", 10f, -.5f, 20f, 1f),
            new SoloRoomElement(SoloRoomElementKind.FallingBlock, "Block", new Vector2(15f, 7.5f), Vector2.one,
                new Vector2(triggerX, 3.5f), new Vector2(.5f, 7f), new SoloRoomTrapSettings(unitsPerTick: .36f, travelDistance: 7f)));

        // C3: two storeys under a ceiling at 9. The ground (top 0) runs right to three steps (tops 1.2, 2.4, 3.6) that
        // reach S1 (top 4.8, x 0-16) from its right end. Ground: spikes at x 13 behind a trigger at x 11 that cuts the
        // ground's storey (y 0-3.8, S1's underside). S1: spikes at x 6 behind a trigger at x 9 that cuts S1's storey
        // (y 4.8-9), met from the right. upperMissing: S1's trigger stops at y 7.
        public static SoloRoomDefinition TwoStoreyRoom(bool upperMissing = false) => new(0, 0f, 20f, new[] {
            El(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 9.5f, 20f, 1f),
            El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
            El(SoloRoomElementKind.Floor, "Ground", 10f, -.5f, 20f, 1f),
            El(SoloRoomElementKind.Floor, "Step_1", 19f, .6f, 2f, 1.2f),
            El(SoloRoomElementKind.Floor, "Step_2", 17.2f, 1.2f, 1.6f, 2.4f),
            El(SoloRoomElementKind.Floor, "Step_3", 19f, 3.1f, 2f, 1f),
            El(SoloRoomElementKind.Floor, "S1", 8f, 4.3f, 16f, 1f),
            new SoloRoomElement(SoloRoomElementKind.HiddenSpikes, "Spikes_Ground", new Vector2(13f, .15f), new Vector2(1f, .3f), new Vector2(11f, 1.9f), new Vector2(.5f, 3.8f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
            new SoloRoomElement(SoloRoomElementKind.HiddenSpikes, "Spikes_S1", new Vector2(6f, 4.95f), new Vector2(1f, .3f),
                upperMissing ? new Vector2(9f, 5.9f) : new Vector2(9f, 6.9f), upperMissing ? new Vector2(.5f, 2.2f) : new Vector2(.5f, 4.2f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
            El(SoloRoomElementKind.Door, "Door", 2f, 5.55f, .6f, 1.5f) }, Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());

        // ValidateBand1Content: a flat room, and routes built from a solution of twelve Until(X>=k) steps. The chain
        // betrayals diverge after steps 2..chain+1 (all after step 1 when not sequential); side betrayals are dead ends
        // (named "Dead end: ...") that diverge after step 1; recovers adds Recovers routes.
        public static SoloRoomDefinition Band1Room(float width = 30f, float doorX = 28f) => Room(width,
            El(SoloRoomElementKind.Floor, "Floor", width * .5f, -.5f, width, 1f),
            El(SoloRoomElementKind.Door, "Door", doorX, .75f, .6f, 1.5f));

        public static RoomRoutes Band1Routes(int chain, int side, int recovers, bool sequential)
        {
            var steps = new System.Collections.Generic.List<RouteStep> { Hold(Right) };
            for (int k = 1; k <= 12; k++) steps.Add(Until(XAtLeast(k)));
            steps.Add(Until(RoomComplete()));
            var solution = new Route("band1 solution", steps.ToArray());
            var betrayals = new System.Collections.Generic.List<Betrayal>();
            for (int i = 0; i < chain; i++)
                betrayals.Add(new Betrayal($"chain {i}", $"Killer_{i}", DeathCause.Hazard,
                    Route.PrefixOf(solution, $"Until(X>={(sequential ? i + 2 : 1)})", $"chain {i}", Until(Dead()))));
            for (int i = 0; i < side; i++)
                betrayals.Add(new Betrayal($"Dead end: side {i}", $"Side_{i}", DeathCause.Hazard,
                    Route.PrefixOf(solution, "Until(X>=1)", $"side {i}", Release(), Until(Dead()))));
            for (int i = 0; i < recovers; i++)
                betrayals.Add(Betrayal.Recovers($"dead end {i}", "Door", Route.PrefixOf(solution, "Until(X>=1)", $"dead end {i}", Until(RoomComplete()))));
            return new RoomRoutes(solution, betrayals.ToArray());
        }

        // Tells: S1 (top 4.8) has a fake section at x 10-12 over the ground; singled puts an honest spike strip on the
        // ground exactly under it (a fall-catcher that points at the fake).
        public static SoloRoomDefinition SingledOutHazardRoom(bool singled)
        {
            var elements = new System.Collections.Generic.List<SoloRoomElement> {
                El(SoloRoomElementKind.Ceiling, "Ceiling", 10f, 9.5f, 20f, 1f),
                El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
                El(SoloRoomElementKind.Floor, "Ground", 10f, -.5f, 20f, 1f),
                El(SoloRoomElementKind.Floor, "S1_L", 5f, 4.3f, 10f, 1f),
                El(SoloRoomElementKind.FakePlatform, "S1_Mid", 11f, 4.3f, 2f, 1f),
                El(SoloRoomElementKind.Floor, "S1_R", 14f, 4.3f, 4f, 1f),
                El(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f) };
            if (singled) elements.Add(El(SoloRoomElementKind.Hazard, "Strip", 11f, .15f, 2f, .3f));
            return new SoloRoomDefinition(0, 0f, 20f, elements.ToArray(), Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());
        }

        // Tells, upside down: a roof (underside 9) with a collapsing section at x 10-12 and a recess above it (walls, a
        // hazard under a closing block at 12); filled makes the collapse fill the recess (the Q4 way).
        public static SoloRoomDefinition RoofRecessRoom(bool filled) => new(0, 0f, 20f, new[] {
            El(SoloRoomElementKind.Ceiling, "Roof_L", 5f, 9.5f, 10f, 1f),
            El(SoloRoomElementKind.Ceiling, "Roof_R", 15f, 9.5f, 10f, 1f),
            new SoloRoomElement(SoloRoomElementKind.CollapsingFloor, "Roof_Mid", filled ? new Vector2(11f, 10.5f) : new Vector2(11f, 9.5f), filled ? new Vector2(2f, 3f) : new Vector2(2f, 1f), settings: new SoloRoomTrapSettings(delayTicks: 6)),
            El(SoloRoomElementKind.Wall, "Recess_L", 9.5f, 11f, 1f, 2f),
            El(SoloRoomElementKind.Wall, "Recess_R", 12.5f, 11f, 1f, 2f),
            El(SoloRoomElementKind.Ceiling, "Recess_Top", 11f, 12.5f, 4f, 1f),
            El(SoloRoomElementKind.Hazard, "Recess_Hazard", 11f, 11.85f, 2f, .3f),
            El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
            El(SoloRoomElementKind.Floor, "Ground", 10f, -.5f, 20f, 1f),
            El(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f) }, Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());

        // Tells: a falling block hanging 0.5 under the ceiling (visible), or flush inside it.
        public static SoloRoomDefinition HangingBlockRoom(bool flush) => Room(30f,
            El(SoloRoomElementKind.Floor, "Floor", 15f, -.5f, 30f, 1f),
            new SoloRoomElement(SoloRoomElementKind.FallingBlock, "Block", new Vector2(14f, flush ? 7.5f : 6f), new Vector2(1f, 1f), new Vector2(12f, 3.5f), new Vector2(.5f, 7f),
                new SoloRoomTrapSettings(delayTicks: 6, unitsPerTick: .36f, travelDistance: flush ? 7f : 5.5f)),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // The falling-block landing kill: a block flush in the ceiling with the given travel.
        public static SoloRoomDefinition BlockTravelRoom(float travel) => Room(30f,
            El(SoloRoomElementKind.Floor, "Floor", 15f, -.5f, 30f, 1f),
            new SoloRoomElement(SoloRoomElementKind.FallingBlock, "Block", new Vector2(14f, 7.5f), new Vector2(1f, 1f), new Vector2(12f, 3.5f), new Vector2(.5f, 7f),
                new SoloRoomTrapSettings(delayTicks: 6, unitsPerTick: .36f, travelDistance: travel)),
            El(SoloRoomElementKind.Door, "Door", 28f, .75f, .6f, 1.5f));

        // ---------- PAX-059: frozen copies of the pre-PAX-059 L002 and L004 (harness fidelity, not levels) ----------

        public static SoloRoomDefinition LiftRoom()
        {
            var elements = new System.Collections.Generic.List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_1",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Floor_A",(2.5f,-.5f),(5f,1f)),
                E(SoloRoomElementKind.Floor,"Platform_B",(8.125f,.5f),(3.75f,1f)),
                E(SoloRoomElementKind.Floor,"Floor_C",(14f,0f),(4f,1f)) };
            AddPit(elements, 1, 5f, 24f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift",(18f,-.25f),(4f,.5f),(19.8f,3.375f),(.3f,7.25f),new SoloRoomTrapSettings(offset:new Vector2(0f,1.5f),moveTicks:30,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.Floor,"Receiver",(21f,.25f),(2f,2.5f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"ReceiverBlock",(20.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:50,unitsPerTick:.36f,travelDistance:4f,triggerSource:TrapTriggerSource.Chain,chainSource:"Lift")));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(23f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DLeft",(26f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DMiddle",(29.4f,-.5f),(2.8f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DRight",(31.4f,-.5f),(1.2f,1f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sweep",(30.4f,.45f),(.8f,.3f),(26f,3.5f),(.5f,7f),new SoloRoomTrapSettings(offset:new Vector2(-2f,0f),moveTicks:33,holdTicks:12,returnTicks:33,movingKind:MovingTrapKind.Hazard,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:92)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(29.625f,.15f),(.75f,.3f),(25.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_A",(30.5f,6f),(.75f,.75f),settings:new SoloRoomTrapSettings(delayTicks:26,unitsPerTick:.36f,travelDistance:5.625f,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_A")));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.5f,6.75f,0f,1f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.5f,12.5f,1f,.5f,2.75f,sourceName:"Platform_B",destinationName:"Floor_C"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,24.5f,1.5f,0f,1f,sourceName:"Receiver",destinationName:"Floor_DLeft"),
                J("Spikes_A",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,28.5f,31.3f,0f,0f,3f,1f,sourceName:"Floor_DLeft",destinationName:"Floor_DRight") };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,5f,24f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard") },jumps);
        }

        public static SoloRoomDefinition FastFlipRoom()
        {
            var elements = new System.Collections.Generic.List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_3",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Floor_A",(2f,-.5f),(4f,1f)),
                E(SoloRoomElementKind.Floor,"Platform_B",(7.5f,0f),(4f,1f)),
                E(SoloRoomElementKind.Floor,"Platform_C",(14f,.5f),(6f,1f)) };
            AddPit(elements, 1, 4f, 22f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"FalseLanding",(20.5f,-.5f),(3f,1f),(20.5f,3.5f),(1f,3f),new SoloRoomTrapSettings(offset:new Vector2(-3f,0f),moveTicks:20,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_D",(27f,-.5f),(10f,1f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"SourceSpikes",(24f,.15f),(1f,.3f),(20.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(26.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:28,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"SourceSpikes")));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(29f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:4,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(24.5f,1.8f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"CeilingHiddenSpikes",(28.75f,6.85f),(1f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:8,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_B",(31.5f,4.5f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.5f,6f,0f,.5f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9f,11.5f,.5f,1f,3f,sourceName:"Platform_B",destinationName:"Platform_C"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,16.5f,19.5f,1f,0f,3f,sourceName:"Platform_C",destinationName:"FalseLanding"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,22.5f,0f,0f,1f,sourceName:"FalseLanding",destinationName:"Floor_D"),
                J("SourceSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23f,25f,0f,0f,2f,.3f),
                J("CeilingHiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,27.75f,29.75f,0f,0f,2f,.3f) };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,4f,22f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard") },jumps);
        }

        static void AddPit(System.Collections.Generic.List<SoloRoomElement> elements, int id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_L",(minX - .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_R",(maxX + .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.PitBottom,$"Pit{id}_Bottom",(centre,-3.5f),(width,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"Pit{id}_Hazard",(centre,-2.85f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }

        static SoloRoomDefinition WithCheckpointAt(this SoloRoomDefinition room, float x, float y) =>
            new(room.Id, room.Origin.x, room.Width, room.Elements.Select(e => e.Kind == SoloRoomElementKind.Checkpoint ? new SoloRoomElement(e.Kind, e.Name, new Vector2(x, y), e.Size) : e).ToArray(),
                room.Openings, room.RequiredJumps, room.RequiredSteps);

        static SoloRoomDefinition Replace(SoloRoomDefinition room, string name, Func<SoloRoomElement, SoloRoomElement> change)
        {
            if (!room.Elements.Any(e => e.Name == name)) throw new ArgumentException($"room {room.Id} has no element '{name}'.");
            SoloRoomElement[] elements = room.Elements.Select(e => e.Name == name ? change(e) : e).ToArray();
            return new SoloRoomDefinition(room.Id, room.Origin.x, room.Width, elements, room.Openings, room.RequiredJumps, room.RequiredSteps);
        }
    }
}
