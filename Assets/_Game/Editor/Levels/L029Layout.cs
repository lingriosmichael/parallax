using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-174: level 29, "Stormfront" (D-163's ninth; Golden Ruins, D-172). The verb is READ THE SKY: one storm cloud follows the
    // cat through the whole room, storey to storey, and every storey has a bridge that won't hold a cat that stops. On storey 1
    // the cloud starts ahead and strikes ahead, so the cat is herded between a strike in front and a bridge falling behind; on
    // the upper storeys it comes up after the cat and drives it over crumbling planks from behind (a room has one cloud, D-090).
    // Three storeys 5.5 u apart on 1 u slabs, walked east, west, east.
    //  - Bridge (section 0, storey 1, walked east): the cloud starts ahead and wakes as the cat sets off. The bridge over the
    //    chasm falls tile by tile from the cat's first step on it, and the cloud strikes the far bank as the cat gets there: stop
    //    on the bank, not on the bridge, let it strike, go on. Pop_1 springs out of the ground (jump it); Lift_1 carries the cat
    //    up.
    //  - Gallery (section 1, storey 2, walked west): the cloud comes up after the cat and chases it from behind, onto Bridge 2,
    //    crumbling planks over a spike bed (don't stop on them). Pop_2 springs out of the floor (jump it), Slam_2 pounds on the
    //    beat (wait for the gap with the storm coming); Lift_2 carries the cat up.
    //  - Crown (section 2, storey 3, walked east): the storm chases over Bridge 3's planks, then Slam_D comes down in front of the
    //    door and stays (stop short, hop onto it, into the door).
    static class L029Layout
    {
        // The storeys' walk lines (room-local y); each slab under the next storey is 1 u of stone, the roof 2 over 5 u of air.
        internal const float S1 = 0f, S2 = 5.5f, S3 = 11f, RoofU = 16f, RoofTop = 18f, Base = -4f, LakeBed = -3f;
        // The bridges: five tiles each, the first BridgeDelay ticks after the cat steps on, each next one BridgeStep later.
        internal const int BridgeCount = 5, BridgeDelay = 10, BridgeStep = 15, PlankDelay = 14;
        internal const float TileWidth = 1.3f, B1X = 9.5f, B2X = 18f, B3X = 7f;
        // The cloud: wake, first charge after the wake, then one every period (tell 25, strike 6).
        internal const int FirstStrike = 100, StrikePeriod = 100;
        // The lifts and Slam_2's beat (room ticks).
        internal const int LiftPeriod = 135, Lift1Phase = 10, Lift2Phase = 45, LiftMove = 30, LiftHold = 15, Beat = 90, Slam2 = 47;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.7f,S3 + .75f),(.6f,1.5f)),
            };
            float b1End = B1X + BridgeCount * TileWidth, b2End = B2X + BridgeCount * TileWidth, b3End = B3X + BridgeCount * TileWidth;

            // ---- Storey 1's floor (y -4..0) ----
            // Ground_0 to the chasm (x 9.5-16, the lake 3 u down); Ground_1 to Lift_1's seat (x 30.5-32).
            Block(elements, SoloRoomElementKind.Floor, "Ground_0", 0f, B1X, Base, S1);
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_1_Bottom",((B1X + b1End) * .5f,(Base + LakeBed) * .5f),(b1End - B1X,LakeBed - Base)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Pit_1",((B1X + b1End) * .5f,LakeBed + .15f),(b1End - B1X,.3f)));
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", b1End, 30.5f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 30.5f, 32f, Base, S1 - .5f);

            // ---- Slab A (y 4.5-5.5): storey 1's ceiling, storey 2's floor ----
            // Lift_2's seat (x 1-2.5); Bed_2 (x 18-24.5), half the slab, under Bridge 2; the hole for Lift_1 (x 30.5-32).
            Block(elements, SoloRoomElementKind.Floor, "SA_W", 0f, 1f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 1f, 2.5f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 2.5f, B2X, S2 - 1f, S2);
            Bed(elements, "Bed_2", "Spikes_2", B2X, b2End, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", b2End, 30.5f, S2 - 1f, S2);

            // ---- Slab B (y 10-11): storey 2's ceiling, storey 3's floor ----
            // The hole for Lift_2 (x 1-2.5); Cap_2 over Slam_2 (x 4-5.5); Bed_3 (x 7-13.5) under Bridge 3.
            Block(elements, SoloRoomElementKind.Floor, "SB_W", 0f, 1f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 2.5f, 4f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2", 4f, 5.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_0b", 5.5f, B3X, S3 - 1f, S3);
            Bed(elements, "Bed_3", "Spikes_3", B3X, b3End, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", b3End, 32f, S3 - 1f, S3);

            // ---- The roof (y 16-18): Slam_D is its bottom unit under Cap_D ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 26.5f, RoofU, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_D", 26.5f, 28f, RoofU + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 28f, 32f, RoofU, RoofTop);

            // ---------- The storm ----------
            // The cloud starts over storey 1's far end and wakes as the cat sets off (its cut, x 2.8-3.2); it follows the cat at a
            // little over half a run, rising and sinking with it from storey to storey (on storey 3 it stays clear of the door,
            // west of x 28.5), and strikes FirstStrike ticks after the wake, then every StrikePeriod.
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(26.4f,S2 - 1.9f),(2f,.8f),(3f,(S1 + S2 - 1f) * .5f),(.4f,S2 - 1f - S1),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] {
                        new StormFloor(-100f, S2 - 1.9f, 1.5f, 30.5f),
                        new StormFloor(S2, S3 - 1.9f, 1.5f, 30.5f),
                        new StormFloor(S3, RoofU - 1.9f, 1.5f, 28.5f) },
                    followSpeed:.08f, firstStrikeDelay:FirstStrike, strikePeriod:StrikePeriod), new SoloRoomTrapSettings(delayTicks:0))));

            // ---------- Bridge (storey 1) ----------
            // Bridge 1 over the chasm, from the cat's first step on it (its trigger is the chasm, from the lake to slab A, L021's):
            // each tile falls into the lake.
            Bridge(elements, "Bridge1_", B1X, S1, S1 - LakeBed + .25f, ((B1X - .05f + b1End) * .5f, (LakeBed + S2 - 1f) * .5f), (b1End - B1X + .05f, S2 - 1f - LakeBed));
            // Pop_1: spikes in the ground at x 26.2-27.4 spring up as the cat comes within 1.6 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_1",(26.8f,S1 + .15f),(1.2f,.3f),(26f,(S1 + S2 - 1f) * .5f),(2.8f,S2 - 1f - S1),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Lift_1, on its seat by the east wall, up to storey 2 and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(31.25f,S1 - .25f),(1.5f,.5f),settings:Lift(S2 - S1, Lift1Phase)));

            // ---------- Gallery (storey 2) ----------
            // Bridge 2: crumbling planks over Bed_2, each giving way PlankDelay ticks after the cat first touches it.
            Planks(elements, "Plank2_", B2X, S2);
            // Pop_2: spikes in the floor at x 14-15.2 spring up as a cat walking west comes within 1.6 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_2",(14.6f,S2 + .15f),(1.2f,.3f),(15.4f,(S2 + S3 - 1f) * .5f),(2.8f,S3 - 1f - S2),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Slam_2, slab B's bottom unit at x 4-5.5, pounds the floor on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(4.75f,S3 - .75f),(1.5f,.5f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: "Cap_2", slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, S2 - S3 + 1f), moveTicks: 8, holdTicks: 10, returnTicks: 15, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Beat, phaseTicks: Slam2, cooldownTicks: 33, movingKind: MovingTrapKind.Solid))));
            // Lift_2, on its seat by the west wall (x 1-2.5), up to storey 3 and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(1.75f,S2 - .25f),(1.5f,.5f),settings:Lift(S3 - S2, Lift2Phase)));

            // ---------- Crown (storey 3) ----------
            // Bridge 3: crumbling planks over Bed_3.
            Planks(elements, "Plank3_", B3X, S3);
            // Slam_D, the roof's bottom unit at x 26.5-28, slams 5 u onto the floor 9 ticks after a cat walking east comes within x
            // 25.1, and stays: stop short, hop onto it, into the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_D",(27.25f,RoofU + .375f),(1.5f,.75f),(27.5f,(S3 + RoofU + .75f) * .5f),(4.8f,RoofU + .75f - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,S3 - RoofU),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            var sections = new[] {
                CheckpointSection.Start("Bridge", new Vector2(1.5f, S1), Names("Bridge1_").Concat(new[] { "Cloud", "Pop_1", "Lift_1" }).ToArray()),
                new CheckpointSection("Gallery", new Vector2(28.5f, S2), new Rect(30.5f, S2 - .6f, 1.5f, .2f), Names("Plank2_").Concat(new[] { "Pop_2", "Slam_2", "Lift_2" }).ToArray()),
                new CheckpointSection("Crown", new Vector2(4f, S3), new Rect(1f, S3 - .6f, 1.5f, .2f), Names("Plank3_").Concat(new[] { "Slam_D" }).ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static string[] Names(string prefix) => Enumerable.Range(1, BridgeCount).Select(i => prefix + i).ToArray();

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // A spike bed in a slab under a bridge (x0-x1): the slab's lower half (a floor, so the storeys stay apart) and the spikes on it.
        static void Bed(List<SoloRoomElement> e, string bed, string spikes, float x0, float x1, float walk)
        {
            e.Add(E(SoloRoomElementKind.Floor, bed, ((x0 + x1) * .5f, walk - .75f), (x1 - x0, .5f)));
            e.Add(E(SoloRoomElementKind.Hazard, spikes, ((x0 + x1) * .5f, walk - .35f), (x1 - x0, .3f)));
        }

        // A bridge of BridgeCount tiles from x0 at the walk line (from its east end when fromEast): the first falls BridgeDelay ticks
        // after the cat steps into its trigger, each next BridgeStep after the one before, `travel` down at 0.25 u a tick (each ends with
        // its top under the spikes' tips), so the bridge falls away behind a cat that keeps moving (L021's).
        static void Bridge(List<SoloRoomElement> e, string prefix, float x0, float walk, float travel, (float, float) cut, (float, float) cutSize)
        {
            for (int k = 0; k < BridgeCount; k++)
            {
                float cx = x0 + (k + .5f) * TileWidth;
                e.Add(k == 0
                    ? E(SoloRoomElementKind.FallingBlock, prefix + (k + 1), (cx, walk - .25f), (TileWidth, .5f), cut, cutSize,
                        new SoloRoomTrapSettings(delayTicks: BridgeDelay, unitsPerTick: .25f, travelDistance: travel))
                    : E(SoloRoomElementKind.FallingBlock, prefix + (k + 1), (cx, walk - .25f), (TileWidth, .5f),
                        settings: new SoloRoomTrapSettings(delayTicks: BridgeStep, unitsPerTick: .25f, travelDistance: travel, triggerSource: TrapTriggerSource.Chain, chainSource: prefix + k)));
            }
        }

        // A bridge of BridgeCount crumbling planks from x0 at the walk line, over a spike bed: each gives way PlankDelay ticks after
        // the cat first touches it, so a cat that stops on the bridge goes down onto the spikes.
        static void Planks(List<SoloRoomElement> e, string prefix, float x0, float walk)
        {
            for (int k = 0; k < BridgeCount; k++)
                e.Add(E(SoloRoomElementKind.CollapsingFloor, prefix + (k + 1), (x0 + (k + .5f) * TileWidth, walk - .25f), (TileWidth, .5f),
                    settings: new SoloRoomTrapSettings(delayTicks: PlankDelay)));
        }

        // A lift rising `rise` and back every LiftPeriod from its phase (over LiftMove each way, LiftHold at the top).
        static SoloRoomTrapSettings Lift(float rise, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset: new Vector2(0f, rise), moveTicks: LiftMove, holdTicks: LiftHold, returnTicks: LiftMove, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: LiftPeriod, phaseTicks: phase, cooldownTicks: 2 * LiftMove + LiftHold, movingKind: MovingTrapKind.Solid));
    }
}
