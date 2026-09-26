using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Setup
{
    // PAX-084 (D-086): small synthetic rooms used only by the EditMode spear tests (R3: next to ArrowFixtures, the
    // existing convention). Not used by any menu and not part of LevelLayouts or the Trap Lab.
    public static class SpearFixtures
    {
        static readonly (float, float) LauncherSize = (.5f, .4f);

        // ---------- ValidateSpear ----------
        // 16 wide, floor top 0, ceiling underside 7, checkpoint x 2. Launcher_Wall x[0,1] y[0,7]; FarWall x[12,16]
        // y[0,4.4] with the door on its top. Spear_A: lane y 0.9 (band 0.7-1.1) from the mouth x 1 to FarWall's face x 12;
        // stuck, its shaft is x[8.6,12] y[0.7,1.1]. Its trigger is a cut at x[4,4.5].

        public static SoloRoomDefinition AllRulesPass() => Room(SpearA());

        public static SoloRoomDefinition Rearm() => Room(SpearA(timing: new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 60)));

        public static SoloRoomDefinition Periodic() => Room(SpearA(timing: new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: 200, cooldownTicks: 60)));

        // 0.8 long: below PlatformSizeConfig's 1.0 width (speed 0.6 keeps it under the cap, so only the length fails).
        public static SoloRoomDefinition ShortShaft() => Room(SpearA(ArrowLane.SpearLane(ArrowDirection.Right, .9f, 12f, length: .8f, unitsPerTick: .6f)));

        // 0.3 thick: below the spear minimum 0.4.
        public static SoloRoomDefinition ThinShaft() => Room(SpearA(ArrowLane.SpearLane(ArrowDirection.Right, .9f, 12f, length: 3.4f, thickness: .3f)));

        // The lane ends at x 9, at the face of a CollapsingFloor x[9,11] y[0.5,1.5].
        public static SoloRoomDefinition EndsInCollapsingFloor() => Room(SpearA(ArrowLane.SpearLane(ArrowDirection.Right, .9f, 9f, length: 3.4f)),
            E(SoloRoomElementKind.CollapsingFloor, "Crumble", (10f, 1f), (2f, 1f), settings: new SoloRoomTrapSettings(delayTicks: 12)));

        // The lane ends at x 9, at the face of a moving Solid x[9,11] y[0.5,1.5].
        public static SoloRoomDefinition EndsInMovingSolid() => Room(SpearA(ArrowLane.SpearLane(ArrowDirection.Right, .9f, 9f, length: 3.4f)),
            E(SoloRoomElementKind.MovingTrap, "Slider", (10f, 1f), (2f, 1f), (6f, 3.5f), (.5f, 7f),
                new SoloRoomTrapSettings(offset: new UnityEngine.Vector2(0f, 2f), moveTicks: 20, holdTicks: 10, returnTicks: 20, cooldownTicks: 60, movingKind: MovingTrapKind.Solid)));

        // The lane ends at x 9, at the face of a fake platform x[9,11] y[0.5,1.5].
        public static SoloRoomDefinition EndsInFakePlatform() => Room(SpearA(ArrowLane.SpearLane(ArrowDirection.Right, .9f, 9f, length: 3.4f)),
            E(SoloRoomElementKind.FakePlatform, "Fake", (10f, 1f), (2f, 1f)));

        // Spikes x[9,10] y[1.0,1.3] overlap the stuck shaft x[8.6,12] y[0.7,1.1].
        public static SoloRoomDefinition ShaftOverlapsHazard() => Room(SpearA(), E(SoloRoomElementKind.Hazard, "Spikes", (9.5f, 1.15f), (1f, .3f)));

        // Spear_B's lane y 1.2 (band 1.0-1.4) overlaps Spear_A's stuck shaft (band 0.7-1.1).
        public static SoloRoomDefinition ShaftOverlapsAnotherLane() => Room(SpearA(),
            Spear("Spear_B", (.75f, 1.2f), ArrowLane.SpearLane(ArrowDirection.Right, 1.2f, 12f, length: 1.4f), Chain("Spear_A")));

        static SoloRoomElement SpearA(ArrowLane? lane = null, SoloRoomTrapSettings? timing = null) =>
            Spear("Spear_A", (.75f, .9f), lane ?? ArrowLane.SpearLane(ArrowDirection.Right, .9f, 12f, length: 3.4f), timing ?? Once(), (4.25f, 3.5f), (.5f, 7f));

        static SoloRoomDefinition Room(params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (8f, 7.5f), (16f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor", (8f, -.5f), (16f, 1f)),
                E(SoloRoomElementKind.Wall, "Launcher_Wall", (.5f, 3.5f), (1f, 7f)),
                E(SoloRoomElementKind.Wall, "FarWall", (14f, 2.2f), (4f, 4.4f)),
                E(SoloRoomElementKind.Door, "Door", (14.5f, 5.15f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 16f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        // ---------- harness cases ----------

        // The drop room: the cat spawns on High_Ledge (top 9, x[0,4]); Spear_Drop (8 long, lane y 1.3) fires on the spawn
        // and sticks in Stop_Wall's face at x 15, so its shaft x[7,15] y[1.1,1.5] is solid long before the cat walks off
        // the ledge. Falling 7.5 u, the cat reaches the 20 u/s cap (6.67 u) before it meets the 0.4 shaft. The door
        // floats out of reach.
        const string DropShaft = "Spear_Drop_Shaft";

        public static RouteCase DropOntoStuckSpear() => new("drop at max fall onto a stuck spear", DropRoom(false),
            new Route("drop onto Spear_Drop", GroundedOn(DropShaft), Hold(Right), Until(Stopped("Spear_Drop")), Until(Airborne()), Until(GroundedOn(DropShaft))));

        public static RouteCase JumpOffStuckSpear() => new("jump off a stuck spear", DropRoom(false),
            new Route("jump off Spear_Drop", Hold(Right), Until(Stopped("Spear_Drop")), Until(Airborne()), Until(GroundedOn(DropShaft)), Release(), Until(Still()),
                Jump(), Until(Airborne()), Until(GroundedOn(DropShaft)), For(5)));

        // R3: a cat standing on the stuck Spear_Drop is hit by Spear_High (lane y 1.8, band 1.6-2.0), fired when the cat
        // enters the box over the shaft. The kill must name Spear_High alone.
        public static RouteCase SecondSpearHitsCatOnStuckSpear() => new("a higher spear hits a cat on a stuck spear", DropRoom(true),
            new Route("stand on Spear_Drop under Spear_High", Hold(Right), Until(Stopped("Spear_Drop")), Until(Airborne()), Until(GroundedOn(DropShaft)), Release(), Until(Dead())));

        static SoloRoomDefinition DropRoom(bool withHighSpear)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (8f, 13.5f), (16f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (2f, 9f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "High_Ledge", (2f, 8.5f), (4f, 1f)),
                E(SoloRoomElementKind.Floor, "Floor", (8f, -.5f), (16f, 1f)),
                E(SoloRoomElementKind.Wall, "Launcher_Wall", (.5f, 4f), (1f, 8f)),
                E(SoloRoomElementKind.Wall, "Stop_Wall", (15.5f, 6.5f), (1f, 13f)),
                E(SoloRoomElementKind.Door, "Door", (8f, 12.25f), (.6f, 1.5f)),
                Spear("Spear_Drop", (.75f, 1.3f), ArrowLane.SpearLane(ArrowDirection.Right, 1.3f, 15f, length: 8f), Once(), (2f, 10f), (4f, 2f)),
            };
            if (withHighSpear)
                elements.Add(Spear("Spear_High", (.75f, 1.8f), ArrowLane.SpearLane(ArrowDirection.Right, 1.8f, 15f), Once(), (11f, 2.5f), (8f, 1f)));
            return new SoloRoomDefinition(0, 0f, 16f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        // R2: Spear_Pin (3 long, lane y 0.9, band 0.7-1.1, over a walking cat's head) sticks in Stop_Wall's face at x 15;
        // its shaft is x[12,15]. The cat crosses the cut at x[11.5,12] and runs on under the shaft; `ticks` after the
        // fire it jumps. Swept over `ticks`, one jump leaves the ground on the stop tick and enters the shaft during that
        // tick's physics step: the stop + 1 check kills it.
        public static RouteCase JumpIntoTheShaftAfterTheFire(int ticks) => new($"jump {ticks} ticks after Spear_Pin fires", PinRoom(),
            new Route($"jump {ticks} ticks after Spear_Pin fires", Hold(Right), Until(Fired("Spear_Pin")), For(ticks), Jump(), For(30)));

        static SoloRoomDefinition PinRoom() => new(0, 0f, 16f, new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (8f, 7.5f), (16f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor", (8f, -.5f), (16f, 1f)),
                E(SoloRoomElementKind.Wall, "Launcher_Wall", (.5f, 3.5f), (1f, 7f)),
                E(SoloRoomElementKind.Wall, "Stop_Wall", (15.5f, 3.5f), (1f, 7f)),
                E(SoloRoomElementKind.Door, "Door", (8f, 6f), (.6f, 1.5f)),
                Spear("Spear_Pin", (.75f, .9f), ArrowLane.SpearLane(ArrowDirection.Right, .9f, 15f, length: 3f), Once(), (11.75f, 3.5f), (.5f, 7f)),
            }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        // ---------- helpers ----------

        static SoloRoomTrapSettings Once() => new SoloRoomTrapSettings(delayTicks: 0);
        static SoloRoomTrapSettings Chain(string source) => new SoloRoomTrapSettings(triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, (float x, float y) launcher, ArrowLane lane, SoloRoomTrapSettings timing, (float x, float y) trigger = default, (float x, float y) triggerSize = default) =>
            E(SoloRoomElementKind.Arrow, name, launcher, LauncherSize, trigger, triggerSize, new SoloRoomTrapSettings(lane, timing));
    }
}
