using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-206 (replaces D-193/D-205; the developer, 2026-10-27: "the platforms are only moving sideways. They dont move in diagonals.
    // I wanted it to start with multiple platforms that make it confusing what is the right path and where to go while avoiding
    // traps. So make it just one very very long lake. At least 10 diffrent moving platforms moving in different directions.
    // Redesign it completely."): level 23, "Ferryman". One very long spike lake, crossed in one go (its section's cap is raised,
    // D-206), on fifteen platforms that each move their own way. Most wait for the cat and set off once it stands on them, so
    // what they do is the surprise; the wrong ones betray it.
    //  - The start: four platforms. Climb, flush with the shore, is the obvious one: on its beat it shoots up and away diagonally
    //    and carries its rider into Spikes_C in the roof. Skyway shuttles high and fast, and
    //    carries a rider under Spikes_H1. Jump down past Climb onto Lift instead; up, onto Glide.
    //  - Glide slides down a diagonal to Ferry. Ferry runs east under Press_1, a spiked press slamming on its own beat. At its far
    //    end, Hopper (a jump up) shoots up into Spikes_P; V, level with Ferry, is the way.
    //  - V dips down and climbs steeply back up: off at the bottom onto the Rock.
    //  - Elevator, from the Rock, up high.
    //  - Arc curves down east; Stair steps down east; Rocket shoots up; Glide_2 slides down a long diagonal through the beat of
    //    Press_2; Zig runs a square wave (Lambda, a jump up over it, peaks into Spikes_L); Loop goes east and up to the east shore.
    static class L023Layout
    {
        internal const float Shore = 6.5f, Lake = 0f, Roof = 14f, Base = -1f, Width = 112f, ShoreE = 6.5f, RockTop = 1.5f;
        // Each triggered platform sets off TriggerDelay after a cat stands on it.
        internal const int TriggerDelay = 6;
        // D-208 (the developer: the cat "is shaking when it stands on fast moving platforms… Reduce the speed of the faster ones keep
        // them consistent"): every platform moves at PlatformSpeed (u a tick; 5 u/s), out and back.
        internal const float PlatformSpeed = .1f;
        // The periodic platforms: Climb and Skyway rest RestTicks at home (D-056's slack); their phases. The presses (periodic,
        // hazards): a fast slam, a hold, back up, rest.
        internal const int RestTicks = 45, ClimbHold = 30, ClimbPhase = 140, SkyHold = 20, SkyPhase = 160;
        internal const int PressPeriod = 130, PressSlam = 20, PressHold = 25, PressBack = 40, Press1Phase = 0, Press2Phase = 0;
        // Each platform's path (offsets from home), for the layout and the routes' waits.
        internal static readonly Vector2[] ClimbPath = { new(6f, 6f) }, LiftPath = { new(0f, 5f) }, GlidePath = { new(8f, -4f) }, SkyPath = { new(14f, 0f) },
            FerryPath = { new(11f, 0f) }, VPath = { new(4f, -2.5f), new(2.5f, 0f) }, HopperPath = { new(6f, 5f) }, ElevatorPath = { new(0f, 9f - RockTop) },
            ArcPath = { new(3f, -.5f), new(6f, -1.6f), new(8.5f, -3.2f), new(10f, -5f) },
            StairPath = { new(2f, 0f), new(2f, -1f), new(4f, -1f), new(4f, -2f), new(6f, -2f) }, RocketPath = { new(0f, 8f) }, Glide2Path = { new(16f, -6f) },
            LambdaPath = { new(3f, 4f), new(6f, 0f) }, ZigPath = { new(3f, 0f), new(3f, 3f), new(6f, 3f), new(6f, 0f), new(9f, 0f) },
            LoopPath = { new(3f, 0f), new(3f, ShoreE - 4f), new(0f, ShoreE - 4f), Vector2.zero };
        // The five dead ends (D-208): each carries a cat away to nowhere, waits DeadEndHold, and brings it back.
        internal static readonly Vector2[] Bait1Path = { new(0f, 2.5f) }, Bait2Path = { new(-2f, 2f) }, Bait3Path = { new(0f, 2.5f) }, Bait4Path = { new(-3f, 0f) },
            Bait5Path = { new(3f, -2f) };
        internal const int DeadEndHold = 100;

        /// <summary>D-208: the ticks a platform takes along `points` at PlatformSpeed.</summary>
        internal static int Ticks(Vector2[] points) => Mathf.Max(1, Mathf.RoundToInt(TrapPath.Length(points) / PlatformSpeed));

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,Shore),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(110f,ShoreE + .75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, Roof, Roof + 1f);

            // ---- The west shore (x 0-4), the lake (x 4-104.5, spiked) with the Rock (x 41-43), the east shore (x 104.5-112) ----
            // D-208 (the developer: "Make the lake look consistent… no shades"): both shores stand 6.5 high. A spike strip between
            // floors at most 6.3 above it is dressed as a pit (a recessed back wall, EnvironmentStackSetup.PitVoids); at 6 the west
            // shore made the lake's west half one, so the two halves differed. Now neither is: sky behind the whole lake.
            Block(elements, SoloRoomElementKind.Floor, "Shore_W", 0f, 4f, Base, Shore);
            Block(elements, SoloRoomElementKind.Floor, "Lake_Floor", 4f, 104.5f, Base, Lake);
            Spikes(elements, "Lake_A", 4f, 41f, Lake);
            Block(elements, SoloRoomElementKind.Floor, "Rock", 41f, 43f, Lake, RockTop);
            Spikes(elements, "Lake_B", 43f, 104.5f, Lake);
            Block(elements, SoloRoomElementKind.Floor, "Shore_E", 104.5f, Width, Base, ShoreE);
            // Hang_1, a stone hanging from the roof with Spikes_H1 under it, over Skyway's way east (a rider's head reaches them).
            Block(elements, SoloRoomElementKind.Wall, "Hang_1", 21.5f, 23.5f, 10f, Roof);
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_H1",(22.5f,9.85f),(2f,.3f)));

            // ---------- The start ----------
            // Climb (periodic), flush with the shore: it rests there, then rises up and east, and back. At the top of its climb a
            // rider's head meets Spikes_C, under Hang_C in the roof.
            Periodic(elements, "Climb", 5f, Shore, ClimbPath, ClimbHold, ClimbPhase);
            Hanger(elements, "C", 11f, 12.9f);
            // Lift, below and beyond Climb: up to Glide's height.
            Mover(elements, "Lift", 8.5f, 3f, LiftPath, 80);
            // Glide: down a diagonal east to Ferry's height.
            Mover(elements, "Glide", 11.5f, 8f, GlidePath, 140);
            // Skyway (periodic), over Glide's home: east under Hang_1, and back.
            Periodic(elements, "Skyway", 11.5f, 9.2f, SkyPath, SkyHold, SkyPhase);
            // Bait_4, under Glide's slope (a drop from Glide on its way down): west, back toward Lift's foot.
            Mover(elements, "Bait_4", 16f, 3f, Bait4Path, DeadEndHold);

            // ---------- Ferry and V ----------
            Mover(elements, "Ferry", 22f, 4f, FerryPath, 60);
            // Press_1 slams from its home (bottom 9.5) until its spikes (the 0.3 strip under it, D-207) are 0.45 over Ferry's top.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Press_1",(27.5f,9.8f),(2f,.6f),settings:Press(new Vector2(0f, 4.75f - 9.5f), Press1Phase)));
            // Bait_1, a jump up from Ferry past the press: straight up, to nowhere.
            Mover(elements, "Bait_1", 30.5f, 5.5f, Bait1Path, DeadEndHold);
            // V: down east to the Rock's height, then back up west (off at the bottom onto the Rock).
            Mover(elements, "V", 35.5f, 4f, VPath, 40);

            // Hopper, over V's home (a jump up from Ferry's far end): it rises up and east, its rider into Spikes_P.
            Mover(elements, "Hopper", 35.5f, 5.4f, HopperPath, 40);
            Hanger(elements, "P", 41.5f, 10.8f);

            // ---------- The Rock and Elevator ----------
            Mover(elements, "Elevator", 44.5f, RockTop, ElevatorPath, 80);
            // Bait_5, a jump east off Elevator on its way up: down east, to nowhere.
            Mover(elements, "Bait_5", 47.5f, 4.5f, Bait5Path, DeadEndHold);

            // ---------- High and down again ----------
            // Arc: a curve down east from Elevator's top.
            Mover(elements, "Arc", 47f, 9f, ArcPath, 60);
            // Stair: steps down east.
            Mover(elements, "Stair", 59.5f, 4f, StairPath, 60);
            // Bait_2, a hop up from Stair's first step: up west, to nowhere.
            Mover(elements, "Bait_2", 62.5f, 5f, Bait2Path, DeadEndHold);
            // Rocket: straight up.
            Mover(elements, "Rocket", 68f, 2f, RocketPath, 250);
            // Glide_2: a long diagonal down east from Rocket's top, through Press_2's beat.
            Mover(elements, "Glide_2", 70.5f, 10f, Glide2Path, 60);
            // Press_2 slams from its home (bottom 12.45) until its spikes are just over a rider's head where Glide_2 passes under it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Press_2",(73f,12.75f),(2f,.6f),settings:Press(new Vector2(0f, -2.7f), Press2Phase)));

            // ---------- To the shore ----------
            // Lambda, over Zig's home (a jump up from Glide_2's end): up east, then down east; at its peak its rider's head meets
            // Spikes_L, under Hang_L.
            Mover(elements, "Lambda", 89f, 5.4f, LambdaPath, 40);
            Hanger(elements, "L", 92f, 9.8f);
            // Zig: east, up, east, down, east.
            Mover(elements, "Zig", 89f, 4f, ZigPath, 60);
            // Bait_3, a hop up from Zig's high run: straight up, to nowhere.
            Mover(elements, "Bait_3", 96f, 8.4f, Bait3Path, DeadEndHold);
            // Loop: east, then up to the east shore's top, then back west and down.
            Mover(elements, "Loop", 100.5f, 4f, LoopPath, 0, closed: true);

            var sections = new[] {
                CheckpointSection.Start("Lake", new Vector2(2f, Shore), new[] { "Climb", "Lift", "Glide", "Skyway", "Bait_4", "Ferry", "Press_1", "Bait_1", "V", "Hopper", "Elevator", "Bait_5", "Arc", "Stair", "Bait_2", "Rocket", "Glide_2", "Press_2", "Lambda", "Zig", "Bait_3", "Loop" }),
                new CheckpointSection("Shore", new Vector2(107f, ShoreE), new Rect(106f, ShoreE, .3f, 1.5f), new string[0]),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A platform (2 u wide, top at `top`, centred on x) that sets off TriggerDelay after a cat stands on it (its trigger is its top
        // strip), along `points` at PlatformSpeed, holds, and comes back the same way (a closed path is home again at its end); it can
        // be set off again once home.
        static void Mover(List<SoloRoomElement> e, string name, float x, float top, Vector2[] points, int hold, bool closed = false)
        {
            int move = Ticks(points), back = closed ? 0 : move;
            e.Add(E(SoloRoomElementKind.MovingTrap, name, (x, top - .25f), (2f, .5f), (x, top + .3f), (2f, .6f), new SoloRoomTrapSettings(
                new PathSettings(points),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(delayTicks: TriggerDelay, moveTicks: move, holdTicks: hold, returnTicks: back, repeatMode: TrapRepeatMode.Rearm,
                        cooldownTicks: move + hold + back, movingKind: MovingTrapKind.Solid)))));
        }

        /// <summary>A periodic platform's beat: out and back at PlatformSpeed, its hold, and RestTicks at home.</summary>
        internal static int Period(Vector2[] points, int hold) => 2 * Ticks(points) + hold + RestTicks;

        // A platform that moves on its own beat (Period).
        static void Periodic(List<SoloRoomElement> e, string name, float x, float top, Vector2[] points, int hold, int phase)
        {
            int move = Ticks(points);
            e.Add(E(SoloRoomElementKind.MovingTrap, name, (x, top - .25f), (2f, .5f), settings: new SoloRoomTrapSettings(
                new PathSettings(points),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(moveTicks: move, holdTicks: hold, returnTicks: move, repeatMode: TrapRepeatMode.Periodic, periodTicks: Period(points, hold),
                        phaseTicks: phase, cooldownTicks: 2 * move + hold, movingKind: MovingTrapKind.Solid)))));
        }

        // A stone hanging from the roof over x, 2 wide, with a spike strip under it whose tips are at `tips`.
        static void Hanger(List<SoloRoomElement> e, string id, float x, float tips)
        {
            Block(e, SoloRoomElementKind.Wall, "Hang_" + id, x - 1f, x + 1f, tips + .3f, Roof);
            e.Add(E(SoloRoomElementKind.Hazard, "Spikes_" + id, (x, tips + .15f), (2f, .3f)));
        }

        // A spiked press slamming `offset` (down) and back on PressPeriod.
        static SoloRoomTrapSettings Press(Vector2 offset, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: true, spiked: true),
                new SoloRoomTrapSettings(offset: offset, moveTicks: PressSlam, holdTicks: PressHold, returnTicks: PressBack, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: PressPeriod, phaseTicks: phase, cooldownTicks: PressSlam + PressHold + PressBack, movingKind: MovingTrapKind.Solid));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));
    }
}
