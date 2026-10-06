using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Setup
{
    // PAX-105 (D-110 and its amendments): small synthetic rooms for the wall cling, used only by the EditMode tests (next to
    // MovingFloorFixtures and SpearFixtures). Not used by any menu, not part of LevelLayouts or the Trap Lab. Every room is 32
    // wide with a floor top at 0 and a ceiling underside at 12 (except the static rules' cases at the end). The cat stands
    // at x 12 unless a room says otherwise. Only grip walls (GripWall) can be grabbed (amendment 2).
    public static class WallClingFixtures
    {
        public const float Face = 14f;   // the wall rooms' face (a wall x 14-15, on the cat's right)

        // ---------- one grip wall on the right, x 14-15, y 0-6 ----------

        // A running jump at the wall (it touches the face while rising, so it rises against it; PAX-106, D-110 amendment 4: it
        // latches by itself at the top of the rise), then nothing until the cat lands at the wall's foot.
        public static RouteCase SlideLand() => new("jump at the wall, slide, land", WallRoom(Wall()),
            new Route("latch and slide", Hold(Right), Jump(), Until(Falling()), Release(), Until(Grounded()), For(10)));

        // The same, then a wall jump with the stick held back toward the wall (the move lock, then Move toward it): the
        // single wall can't be latched again before the cat lands.
        public static RouteCase SingleWall() => new("one wall can't be climbed", WallRoom(Wall()),
            new Route("latch, wall jump, come back", Hold(Right), Jump(), Until(Falling()), Until(Clinging()), Release(),
                Jump(), Hold(Right), Until(Falling()), For(4), Until(Grounded()), For(10)));

        // Clinging, then the stick pushed away (left): the cat lets go and falls.
        public static RouteCase PushAway() => new("latch, push away", WallRoom(Wall()),
            new Route("latch and push away", Hold(Right), Jump(), Until(Falling()), Until(Clinging()), For(5), Hold(Left), For(2), Release(), Until(Grounded())));

        // Jump pressed near the top of the rise, while still rising against the face: airborne, so it waits in the jump buffer,
        // and the step the cat latches (the top of the rise) wall-jumps at once.
        public static RouteCase BufferedWallJump() => new("a jump buffered onto the latch", WallRoom(Wall()),
            new Route("press jump just before the latch", Hold(Right), Jump(), Until(Airborne()), For(13), Jump(), Release(), Until(Grounded())));

        // A hazard where the wall was (a trigger: never clung to) kills.
        public static RouteCase HazardWall() => new("jump at a hazard wall", WallRoom(E(SoloRoomElementKind.Hazard, "Spikes", (14.5f, 3f), (1f, 6f))),
            new Route("into the hazard", Hold(Right), Jump(), Until(Falling()), Release(), For(60)));

        // A grip block hanging at y 1.5 (height 0.5 or 1.0), x 14-18: the running jump meets its face near the top of the rise.
        public static RouteCase HangingBlock(float height) => new($"grab a {height:0.0} u face", WallRoom(
                E(SoloRoomElementKind.GripWall, "Block", (16f, 1.5f + height * .5f), (4f, height))),
            new Route("at the block's face", Hold(Right), Jump(), Until(Falling()), Release(), For(30)));

        // The same running jump at a plain Wall where the grip wall was: no grip, the cat drops (amendments 2 and 4: touching a
        // plain wall never latches).
        public static RouteCase PlainWall() => new("jump at a plain wall", WallRoom(E(SoloRoomElementKind.Wall, "Wall", (14.5f, 3f), (1f, 6f))),
            new Route("at the plain wall", Hold(Right), Jump(), Until(Falling()), Release(), Until(Grounded()), For(10)));

        // ---------- two facing walls: a shaft ----------

        // The shaft's faces `gap` apart: Left_Wall x 12-13 (grip, face 13, 11 tall) and Right_Grip x 13+gap to 14+gap (grip, top 8),
        // the face of the plateau beyond it (to x 20, top 8). The cat
        // stands in the middle, jumps at the left wall, latches on and wall-jumps from side to side (every face latches by itself)
        // until it lands on the plateau; `hops` is even, so the last jump leaves the left wall toward the plateau.
        public static RouteCase Shaft(float gap, int hops) => new($"climb a {gap:0.00} u shaft", ShaftRoom(gap), ShaftRoute(hops));

        public static SoloRoomDefinition ShaftRoom(float gap) => Room(13f + gap * .5f,
            E(SoloRoomElementKind.GripWall, "Left_Wall", (12.5f, 5.5f), (1f, 11f)),
            E(SoloRoomElementKind.GripWall, "Right_Grip", (13.5f + gap, 4f), (1f, 8f)),
            E(SoloRoomElementKind.Floor, "Plateau", ((14f + gap + 20f) * .5f, 4f), (20f - 14f - gap, 8f)));

        static Route ShaftRoute(int hops)
        {
            var steps = new List<RouteStep> { Hold(Left), Jump(), Until(Falling()), Until(ClingingLeft()), Release() };
            for (int i = 0; i < hops; i++)
            {
                bool fromLeft = i % 2 == 0;
                steps.Add(Jump());
                steps.Add(Hold(fromLeft ? Right : Left));
                steps.Add(Until(fromLeft ? ClingingRight() : ClingingLeft()));
                steps.Add(Release());
            }
            steps.Add(Jump());
            steps.Add(Hold(hops % 2 == 0 ? Right : Left));
            steps.Add(For(60));
            return new Route($"climb the shaft in {hops} hops", GroundedOn("Plateau"), steps.ToArray());
        }

        // A wall jump off a left wall with the stick held right, across open floor: where it comes down measures the crossing
        // reach (no far wall).
        public static RouteCase KickAcross() => new("kick off a left wall across the room", Room(13.6f, E(SoloRoomElementKind.GripWall, "Left_Wall", (12.5f, 5.5f), (1f, 11f))),
            new Route("latch and kick across", Hold(Left), Jump(), Until(Falling()), Until(ClingingLeft()), Release(), For(10), Jump(), Hold(Right), Until(Grounded()), For(2)));

        // A wall x 14-15 standing on the floor, `top` high: latch at the top of a running jump, kick off (left) and hold the
        // stick back toward it: past the move lock the cat comes back over the wall's top when it's low enough.
        public static RouteCase KickBack(float top) => new($"kick back onto a {top:0.00} u wall", WallRoom(E(SoloRoomElementKind.GripWall, "Wall", (14.5f, top * .5f), (1f, top))),
            new Route("latch, kick off, hold back", Hold(Right), Jump(), Until(Falling()), Until(Clinging()), Release(), Jump(), Hold(Right), Until(Grounded()), For(5)));

        // ---------- the static rules' grip-wall cases (amendment 2: only grip faces add reach) ----------

        // TriggerCoverageFixtures.ClimbOnlyLedge with a grip shaft instead of the vine: Ledge x 6-20 (top 4), out of a jump's
        // reach, is reached only by climbing Grip_A (x 2-3, y 0-6) and Grip_B (x 5-6, hung at y 1-3.5, under the Ledge's west
        // end): faces 2 u apart. Ledge_Spikes (cut at x 14 above the Ledge) lie beyond the cut seen from the shaft (x 18) or
        // before it (x 9).
        public static SoloRoomDefinition GripOnlyLedge(bool dangerBeyond) => CoverageRoom(30f,
            E(SoloRoomElementKind.Floor, "Ledge", (13f, 3.75f), (14f, .5f)),
            E(SoloRoomElementKind.GripWall, "Grip_A", (2.5f, 3f), (1f, 6f)),
            E(SoloRoomElementKind.GripWall, "Grip_B", (5.5f, 2.25f), (1f, 2.5f)),
            E(SoloRoomElementKind.HiddenSpikes, "Ledge_Spikes", (dangerBeyond ? 18f : 9f, 4.15f), (1f, .3f), (14f, 5.5f), (.5f, 3f), new SoloRoomTrapSettings(revealDelayTicks: 6)));

        // TriggerCoverageFixtures.FlipEntry(true) (the stretch right of the unjumpable Moat, x 11-17, entered only through Flip_B,
        // which the drop spikes' trigger contains) with a grip wall hung over the Moat (Grip_Post x 12.5-13.5, y 1.5-4.5): a
        // kick off it clears the rest of the moat, so the flip-entry clause no longer covers Drop_Spikes.
        public static SoloRoomDefinition GripFlipEntry() => CoverageRoom(2f,
            E(SoloRoomElementKind.GravityFlip, "Flip_A", (8f, 3f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.Hazard, "Moat", (14f, .15f), (6f, .3f), hazardRole: SoloRoomHazardRole.UnjumpableFloor),
            E(SoloRoomElementKind.GravityFlip, "Flip_B", (26f, 4f), (1f, 2f), settings: new SoloRoomTrapSettings(rearmOnExit: true, rendererEnabled: true)),
            E(SoloRoomElementKind.HiddenSpikes, "Drop_Spikes", (25f, .15f), (1f, .3f), (26f, 4f), (1f, 2f), new SoloRoomTrapSettings(revealDelayTicks: 6)),
            E(SoloRoomElementKind.GripWall, "Grip_Post", (13f, 3f), (1f, 3f)));

        // PrecisionFixtures.BaitRoom(gap) with its far pit wall a grip wall up to the floor's top (Pit_R x right to right+1, y -4
        // to 0; Floor_R from right+1): a cat that falls short grabs it and kicks back onto its top, the bait's target.
        public static SoloRoomDefinition GripBaitRoom(float gap)
        {
            float right = 5f + gap;
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (10f, 7.5f), (20f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (2f, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor_L", (2.5f, -.5f), (5f, 1f)),
                E(SoloRoomElementKind.Floor, "Floor_R", ((right + 1f + 20f) * .5f, -.5f), (20f - right - 1f, 1f)),
                E(SoloRoomElementKind.Wall, "Pit_L", (4.5f, -2.5f), (1f, 3f)),
                E(SoloRoomElementKind.GripWall, "Pit_R", (right + .5f, -2f), (1f, 4f)),
                E(SoloRoomElementKind.PitBottom, "Pit_Bottom", ((4f + right + 1f) * .5f, -3.5f), (right + 1f - 4f, 1f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard, "Pit_Hazard", new Vector2((5f + right) * .5f, -2.85f), new Vector2(gap, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Door, "Door", (18f, .75f), (.6f, 1.5f)),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 5f, right, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            var baits = new[] { new BaitGap("Gap", 5f, 0f, right, 0f) };
            return new SoloRoomDefinition(0, 0f, 20f, elements, openings, System.Array.Empty<RequiredJump>(), null, null, baits);
        }

        // A fake platform x 10-14 (underside 3.5) over the floor: a straight jump's top (2.26) stays clear of it, but a kick off
        // Grip_Under (x 8-9, hung at y 0.5-3, its east face under the platform's west end) reaches it from below.
        public static SoloRoomDefinition GripUnderTrapFloor() => CoverageRoom(2f,
            E(SoloRoomElementKind.FakePlatform, "Trap_Ledge", (12f, 3.75f), (4f, .5f)),
            E(SoloRoomElementKind.GripWall, "Grip_Under", (8.5f, 1.75f), (1f, 2.5f)));

        // ValidateGripWall's cases: one grip wall, x 10-11, fine ("ok") or broken one way each.
        public static SoloRoomDefinition GripWallCase(string what) => CoverageRoom(2f, what switch
        {
            "short" => new[] { E(SoloRoomElementKind.GripWall, "Grip", (10.5f, 2.45f), (1f, .9f)) },
            "outside" => new[] { E(SoloRoomElementKind.GripWall, "Grip", (31.8f, 3f), (1f, 2f)) },
            "overlaps" => new[] { E(SoloRoomElementKind.GripWall, "Grip", (10.5f, 2f), (1f, 2f)), E(SoloRoomElementKind.Floor, "Slab", (12f, 2.5f), (3f, .5f)) },
            "hazard" => new[] { E(SoloRoomElementKind.GripWall, "Grip", (10.5f, 3f), (1f, 2f)), E(SoloRoomElementKind.Hazard, "Spikes", (11.5f, .15f), (1f, .3f)) },
            "ceiling" => new[] { E(SoloRoomElementKind.GripWall, "Grip", (10.5f, 6f), (1f, 3f)) },
            _ => new[] { E(SoloRoomElementKind.GripWall, "Grip", (10.5f, 3f), (1f, 2f)) },
        });

        static SoloRoomDefinition CoverageRoom(float checkpointX, params SoloRoomElement[] extra)
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

        // ---------- rooms ----------

        static SoloRoomElement Wall() => E(SoloRoomElementKind.GripWall, "Wall", (14.5f, 3f), (1f, 6f));

        static SoloRoomDefinition WallRoom(params SoloRoomElement[] extra) => Room(12f, extra);

        static SoloRoomDefinition Room(float checkpointX, params SoloRoomElement[] extra)
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling, "Ceiling", (16f, 12.5f), (32f, 1f)),
                E(SoloRoomElementKind.Checkpoint, "Checkpoint", (checkpointX, 0f), (0f, 0f)),
                E(SoloRoomElementKind.Floor, "Floor", (16f, -.5f), (32f, 1f)),
                E(SoloRoomElementKind.Door, "Door", (30f, .75f), (.6f, 1.5f)),
            };
            elements.AddRange(extra);
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }
    }
}
