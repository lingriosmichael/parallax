using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Setup
{
    // PAX-093 (D-095): small synthetic rooms for the floors that move, used only by the EditMode tests (next to SpearFixtures,
    // the existing convention). Not used by any menu, not part of LevelLayouts or the Trap Lab. Every room is 32 wide with a
    // floor top at 0 and a ceiling underside at 7; the pit rooms have a pit x 6-26 between Floor_L and Floor_R.
    public static class MovingFloorFixtures
    {
        // ---------- harness cases: the cat spawns standing on the pattern ----------

        // Mover x 6-10 (top 0) over the pit; its trigger holds its top, so the cat standing on it sets it off (+10 ticks), and
        // it moves 14 right over 140 ticks (0.1 u/tick = 5 u/s), then holds.
        public static RouteCase Ride(SurfaceMotion motion) => new($"ride a {motion} mover", PitRoom(8f, Mover(motion)),
            new Route("stand on the mover", For(170)));

        // The same mover; 16 ticks in (moving since tick 11, so a Legacy cat is still on it), the cat jumps straight up with
        // no input.
        public static RouteCase JumpOffMover(SurfaceMotion motion) => new($"jump off a {motion} mover", PitRoom(8f, Mover(motion)),
            new Route("jump off the mover", For(16), Jump(), For(30)));

        // Lift x 14-18 (top 4) over the floor, set off by the cat standing on it (+10); it moves 3 down over 30 ticks (5 u/s).
        public static RouteCase RideDown(SurfaceMotion motion) => new($"ride a {motion} lift down", FloorRoom(new Vector2(16f, 4f),
            E(SoloRoomElementKind.MovingTrap, "Lift", (16f, 3.75f), (4f, .5f), (16f, 4.5f), (4f, 1f), Floor(motion, new SoloRoomTrapSettings(delayTicks: 10,
                offset: new Vector2(0f, -3f), moveTicks: 30, holdTicks: 600, movingKind: MovingTrapKind.Solid)))),
            new Route("stand on the lift", For(60)));

        // Lift x 14-18 (top 0.5) rises 3 over 30 ticks (5 u/s) and stops: D-056 (3)'s launch, whatever its SurfaceMotion.
        public static RouteCase RideUp(SurfaceMotion motion) => new($"ride a {motion} lift up", FloorRoom(new Vector2(16f, .5f),
            E(SoloRoomElementKind.MovingTrap, "Lift", (16f, .25f), (4f, .5f), (16f, 1f), (4f, 1f), Floor(motion, new SoloRoomTrapSettings(delayTicks: 10,
                offset: new Vector2(0f, 3f), moveTicks: 30, holdTicks: 600, movingKind: MovingTrapKind.Solid)))),
            new Route("stand on the lift", For(90)));

        // Slider x 6-10 (top 0), set off by the cat standing on it (+10), slides 6 right over 20 ticks (0.3 u/tick).
        public static RouteCase SlideAway(SurfaceMotion motion) => new($"stand on a {motion} slide-away", PitRoom(8f,
            E(SoloRoomElementKind.MovingTrap, "Slider", (8f, -.25f), (4f, .5f), (8f, .5f), (4f, 1f), Floor(motion, new SoloRoomTrapSettings(delayTicks: 10,
                offset: new Vector2(6f, 0f), moveTicks: 20, holdTicks: 600, movingKind: MovingTrapKind.Solid)))),
            new Route("stand on the slider", For(120)));

        // Drop x 6-10 (top 0), set off by the cat standing on it (+15): it drops 2 over 10 ticks, holds 30 and rises back over
        // 40 (Rearm, cooldown 90 >= 10 + 30 + 40).
        public static RouteCase DropAndReturn(SurfaceMotion motion) => new($"stand on a {motion} drop-and-return floor", PitRoom(8f, DropFloor(motion, 90)),
            new Route("stand on the drop", For(130)));

        // Shrink x 6-10 (top 0), its own top its trigger: +10 after a touch it shrinks from the right over 40 ticks to nothing.
        // The cat stands at x 9.5, near the edge that moves in.
        public static RouteCase StandOnShrinker(ShrinkFrom from) => new($"stand on a shrinker ({from})", PitRoom(9.5f, Shrinker(from, 40, 0f)),
            new Route("stand on the shrinker", For(120)));

        // Pusher x 2.5-3.5 (y 0-2) on the floor moves 8 right over 80 ticks (0.1 u/tick) once the cat, standing at x 6, is in
        // its trigger. With a partner, Anvil (x 12-13, y 0-2) ends the push path: the cat is crushed against it. Without, the
        // path ends in open space and the cat is left flush against the stopped wall.
        public static RouteCase Pushed(bool partner) => new($"pushed {(partner ? "into Anvil" : "into open space")}",
            FloorRoom(new Vector2(6f, 0f), PushRoomElements(partner)), new Route("stand in front of the pusher", For(130)));

        // D-091 rewind: two sections split by the gate at x 10 (checkpoint x 11). Walking right through the floor trigger at x 6
        // sets off Shrink (x 12-16, top 4.25, out of reach; +10 ticks, from its right edge over 300 ticks), so it is mid-shrink at
        // the gate. Mover (Carry, Periodic from tick 0: x 21-23 out to 27-29 over 60, hold 20, back over 60, every 200; top 3.25,
        // out of reach) runs on the room clock. Neither is touched after the gate, so both depend only on room ticks. The
        // reference walks through the gate and stands; RewindAfter dies `ticks` past the gate and stands after the rewind.
        public static RouteCase RewindReference() => new("walk through the gate and stand", RewindRoom(), RewindSolution());
        public static Route RewindAfter(int ticks) => Route.FromSection(RewindSolution(), "Two", ticks, "die past the gate, then stand", For(260));

        static Route RewindSolution() => new("walk through the gate and stand", Hold(Right), Until(XAtLeast(10.6f)), Release(), For(600));

        static SoloRoomDefinition RewindRoom() => FloorRoom(new Vector2(2f, 0f),
            E(SoloRoomElementKind.ShrinkingFloor, "Shrink", (14f, 4f), (4f, .5f), (6f, .5f), (1f, 1f),
                new SoloRoomTrapSettings(new ShrinkSettings(300, 0f, ShrinkFrom.Right), new SoloRoomTrapSettings(delayTicks: 10))),
            E(SoloRoomElementKind.MovingTrap, "Mover", (22f, 3f), (2f, .5f), settings: Floor(SurfaceMotion.Carry, new SoloRoomTrapSettings(offset: new Vector2(6f, 0f),
                moveTicks: 60, holdTicks: 20, returnTicks: 60, repeatMode: TrapRepeatMode.Periodic, periodTicks: 200, cooldownTicks: 150, movingKind: MovingTrapKind.Solid))))
            .WithCheckpointSections(new[] {
                CheckpointSection.Start("One", new Vector2(2f, 0f), new[] { "Shrink" }),
                new CheckpointSection("Two", new Vector2(11f, 0f), new Rect(9.9f, -1f, .2f, 8f), new[] { "Mover" }),
            });

        // ---------- validator rooms ----------

        // A sideways mover (Periodic) with this SurfaceMotion (Q1: Legacy is an error in levels 14+ and Trap Lab room 12).
        public static SoloRoomDefinition SidewaysMover(SurfaceMotion motion) => PitRoom(2f,
            E(SoloRoomElementKind.MovingTrap, "Mover", (8f, -.25f), (4f, .5f), settings: Floor(motion, new SoloRoomTrapSettings(offset: new Vector2(14f, 0f),
                moveTicks: 140, holdTicks: 30, returnTicks: 140, repeatMode: TrapRepeatMode.Periodic, periodTicks: 400, cooldownTicks: 320, movingKind: MovingTrapKind.Solid))));

        // A drop-and-return floor with this cooldown (Q4: at least move + hold + return = 80).
        public static SoloRoomDefinition DropCooldown(int cooldown) => PitRoom(2f, DropFloor(SurfaceMotion.Carry, cooldown));

        // Q6: Post (x 7.5-8.5, y 0-1) stands in Pusher's path, and isn't its crush partner.
        public static SoloRoomDefinition PushPathBlocked() => FloorRoom(new Vector2(20f, 0f), Pusher("Anvil"), Anvil(), E(SoloRoomElementKind.Wall, "Post", (8f, .5f), (1f, 1f)));

        // Q6: the named partner is far past the path's end (x 20-21), so the path ends in open space short of it.
        public static SoloRoomDefinition PushPartnerNotAtTheEnd() => FloorRoom(new Vector2(28f, 0f), Pusher("Far_Anvil"), E(SoloRoomElementKind.Wall, "Far_Anvil", (20.5f, 1f), (1f, 2f)));

        // Q6: the push path ends in open space, no partner named.
        public static SoloRoomDefinition PushIntoOpenSpace() => FloorRoom(new Vector2(20f, 0f), Pusher(null));

        public static SoloRoomDefinition PushIntoAnvil() => FloorRoom(new Vector2(20f, 0f), PushRoomElements(true));

        // A shrinker with these settings (unconfigured when shrinkTicks is 0).
        public static SoloRoomDefinition ShrinkerSettings(int shrinkTicks, float minWidth) => PitRoom(2f, Shrinker(ShrinkFrom.Both, shrinkTicks, minWidth));

        // Q5: a shrinker whose settings are configured with this tick count (ShrinkerSettings leaves 0 unconfigured).
        public static SoloRoomDefinition ShrinkerOverTicks(int shrinkTicks) => PitRoom(2f,
            E(SoloRoomElementKind.ShrinkingFloor, "Shrink", (8f, -.25f), (4f, .5f), settings: new SoloRoomTrapSettings(new ShrinkSettings(shrinkTicks, 0f, ShrinkFrom.Both), new SoloRoomTrapSettings(delayTicks: 10))));

        // Q6: the named partner, Spikes, stands at the path's end but is a hazard, not a fixed solid.
        public static SoloRoomDefinition PushPartnerNotASolid() => FloorRoom(new Vector2(20f, 0f), Pusher("Spikes"), E(SoloRoomElementKind.Hazard, "Spikes", (12.5f, .25f), (1f, .5f)));

        // D-056 (3): Mover's swept path (x 6-24 at its top) runs into Block (x 15-16, y -1 to 1).
        public static SoloRoomDefinition MoverIntoAWall() => PitRoom(2f,
            E(SoloRoomElementKind.MovingTrap, "Mover", (8f, -.25f), (4f, .5f), (8f, .5f), (4f, 1f), Floor(SurfaceMotion.Carry, new SoloRoomTrapSettings(delayTicks: 10,
                offset: new Vector2(14f, 0f), moveTicks: 140, holdTicks: 600, movingKind: MovingTrapKind.Solid))),
            E(SoloRoomElementKind.Wall, "Block", (15.5f, 0f), (1f, 2f)));

        // Band (D-065): one pattern each, for a band-1 level id.
        public static SoloRoomDefinition BandMover() => SidewaysMover(SurfaceMotion.Carry);
        public static SoloRoomDefinition BandShrinker() => ShrinkerSettings(40, 0f);
        public static SoloRoomDefinition BandPushWall() => PushIntoOpenSpace();
        public static SoloRoomDefinition BandSlideAway() => SlideAwayRoom(SurfaceMotion.Slip);
        public static SoloRoomDefinition BandDropAndReturn() => DropCooldown(90);

        public static SoloRoomDefinition SlideAwayRoom(SurfaceMotion motion) => (SoloRoomDefinition)SlideAway(motion).Room;

        // ---------- parts ----------

        static SoloRoomTrapSettings Floor(SurfaceMotion motion, SoloRoomTrapSettings timing) => new(new MovingFloorSettings(motion), timing);

        static SoloRoomElement Mover(SurfaceMotion motion) =>
            E(SoloRoomElementKind.MovingTrap, "Mover", (8f, -.25f), (4f, .5f), (8f, .5f), (4f, 1f), Floor(motion, new SoloRoomTrapSettings(delayTicks: 10,
                offset: new Vector2(14f, 0f), moveTicks: 140, holdTicks: 600, movingKind: MovingTrapKind.Solid)));

        static SoloRoomElement DropFloor(SurfaceMotion motion, int cooldown) =>
            E(SoloRoomElementKind.MovingTrap, "Drop", (8f, -.25f), (4f, .5f), (8f, .5f), (4f, 1f), Floor(motion, new SoloRoomTrapSettings(delayTicks: 15,
                offset: new Vector2(0f, -2f), moveTicks: 10, holdTicks: 30, returnTicks: 40, repeatMode: TrapRepeatMode.Rearm, cooldownTicks: cooldown, movingKind: MovingTrapKind.Solid)));

        static SoloRoomElement Shrinker(ShrinkFrom from, int shrinkTicks, float minWidth) =>
            E(SoloRoomElementKind.ShrinkingFloor, "Shrink", (8f, -.25f), (4f, .5f), settings: shrinkTicks > 0
                ? new SoloRoomTrapSettings(new ShrinkSettings(shrinkTicks, minWidth, from), new SoloRoomTrapSettings(delayTicks: 10))
                : new SoloRoomTrapSettings(delayTicks: 10));

        static SoloRoomElement Pusher(string partner) =>
            E(SoloRoomElementKind.MovingTrap, "Pusher", (3f, 1f), (1f, 2f), (6f, 1f), (2f, 2f), new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip, pushes: true, crushPartner: partner),
                new SoloRoomTrapSettings(offset: new Vector2(8f, 0f), moveTicks: 80, holdTicks: 600, movingKind: MovingTrapKind.Solid)));

        static SoloRoomElement Anvil() => E(SoloRoomElementKind.Wall, "Anvil", (12.5f, 1f), (1f, 2f));

        static SoloRoomElement[] PushRoomElements(bool partner) => partner ? new[] { Pusher("Anvil"), Anvil() } : new[] { Pusher(null) };

        // A full floor, the cat standing at `checkpoint`.
        static SoloRoomDefinition FloorRoom(Vector2 checkpoint, params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (checkpoint.x, checkpoint.y), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor", (16f, -.5f), (32f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        // Floor_L x 0-6 and Floor_R x 26-32 (top 0), the pit between them (its hazard at y -2.85); the cat at (checkpointX, 0).
        static SoloRoomDefinition PitRoom(float checkpointX, params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 7.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (checkpointX, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor_L", (3f, -.5f), (6f, 1f)),
                E(SoloRoomElementKind.Floor, "Floor_R", (29f, -.5f), (6f, 1f)),
                E(SoloRoomElementKind.Wall, "Pit_L", (5.5f, -2.5f), (1f, 3f)),
                E(SoloRoomElementKind.Wall, "Pit_R", (26.5f, -2.5f), (1f, 3f)),
                E(SoloRoomElementKind.PitBottom, "Pit_Bottom", (16f, -3.5f), (20f, 1f)),
                E(SoloRoomElementKind.Hazard, "Pit_Hazard", (16f, -2.85f), (20f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), new[] { O(SoloRoomOpeningKind.Pit, 6f, 26f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") }, System.Array.Empty<RequiredJump>());
        }
    }
}
