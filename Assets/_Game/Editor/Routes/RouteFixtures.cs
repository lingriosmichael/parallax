using System;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
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

        // PAX-078 R3: L002's Lift trigger, x centre as given (shipped 19.80).
        public static SoloRoomDefinition L002WithLiftTriggerX(float x) =>
            Replace(Layout("L002"), "Lift", e => new SoloRoomElement(e.Kind, e.Name, e.Position, e.Size, new Vector2(x, e.SecondaryPosition.y), e.SecondarySize, e.Settings, e.HazardRole));

        public static RouteCase L002LiftApproach(float triggerX) => new($"L002 lift approach {triggerX}", L002WithLiftTriggerX(triggerX), new Route("lift approach",
            Hold(Right), Until(XAtLeast(4.4f)), Jump(), Until(GroundedOn("Platform_B")),
            Until(XAtLeast(9.4f)), Jump(), Until(GroundedOn("Floor_C")),
            Until(GroundedOn("Lift")), Until(Fired("Lift")), For(1)));

        // L004 with its checkpoint on FalseLanding's left end, so replays start at the SourceSpikes stretch.
        public static SoloRoomDefinition L004FromFalseLanding() =>
            Replace(Layout("L004"), "Checkpoint_3", e => new SoloRoomElement(e.Kind, e.Name, new Vector2(19.6f, 0f), e.Size, e.SecondaryPosition, e.SecondarySize, e.Settings, e.HazardRole));

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

        static SoloRoomDefinition WithCheckpointAt(this SoloRoomDefinition room, float x, float y) =>
            new(room.Id, room.Origin.x, room.Width, room.Elements.Select(e => e.Kind == SoloRoomElementKind.Checkpoint ? new SoloRoomElement(e.Kind, e.Name, new Vector2(x, y), e.Size) : e).ToArray(),
                room.Openings, room.RequiredJumps, room.RequiredSteps);

        static SoloRoomDefinition Layout(string id) =>
            LevelLayouts.TryGet(id, out SoloRoomDefinition room) ? room : throw new ArgumentException("no layout " + id);

        static SoloRoomDefinition Replace(SoloRoomDefinition room, string name, Func<SoloRoomElement, SoloRoomElement> change)
        {
            if (!room.Elements.Any(e => e.Name == name)) throw new ArgumentException($"room {room.Id} has no element '{name}'.");
            SoloRoomElement[] elements = room.Elements.Select(e => e.Name == name ? change(e) : e).ToArray();
            return new SoloRoomDefinition(room.Id, room.Origin.x, room.Width, elements, room.Openings, room.RequiredJumps, room.RequiredSteps);
        }
    }
}
