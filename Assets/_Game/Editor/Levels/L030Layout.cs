using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-175: level 30, "Meltdown" (D-163's tenth and Golden Ruins' last, D-172). The verb is GET OUT: the temple comes down
    // around the cat, storey by storey, and every storey has something falling in behind it. A checkpointed escape: three
    // storeys 5.5 u apart on 1 u slabs, walked east, west, east, climbing to the door at the top.
    //  - Cave-in (section 0, storey 1, walked east): the ceiling falls in rock by rock behind the running cat (each rock drops as
    //    the cat passes under it: don't stop). Pop_1 springs out of the ground (jump it), Collapse_1 gives way over Pit_1 (jump
    //    it), Slam_1 pounds on the beat (wait for the gap), and G_1 throws the cat up its hole to storey 2.
    //  - Roots (section 1, storey 2, walked west): Slam_2 pounds on the beat; past it Hinge_2 stands up behind the cat and
    //    pushes it onto the spike field (don't stop at the edge). V1-V3 hang over the field, each snapping soon after the cat
    //    takes it; Lift_2 carries the cat up.
    //  - Collapse (section 2, storey 3, walked east): Hinge_3, spiked, stands up behind the cat and slides after it the whole
    //    length of the storey. Pop_3 springs ahead (jump it), Collapse_3 gives way over Bed_3's spikes (jump it), Slam_D comes
    //    down in front of the door and stays (stop short, hop onto it), and Pop_D springs on the last step (jump it).
    static class L030Layout
    {
        // The storeys' walk lines (room-local y); each slab under the next storey is 1 u of stone, the roof 2 over 5 u of air.
        internal const float S1 = 0f, S2 = 5.5f, S3 = 11f, RoofU = 16f, RoofTop = 18f, Base = -4f;
        // The cave-in: RockCount rocks RockWidth wide from RockX in slab A's underside, each falling RockDelay ticks after the cat
        // comes under it, at RockSpeed.
        internal const int RockCount = 5, RockDelay = 14;
        internal const float RockX = 3.5f, RockWidth = 1.5f, RockSpeed = .4f;
        // The beat (room ticks) for the slams and G_1, and each one's place in it (the slam's departure; the eruption's first tick).
        internal const int Beat = 90, Slam1 = 67, Slam2 = 32, G1Erupt = 85;
        // Lift_2 (room ticks), and how long each vine holds after the cat first touches it.
        internal const int LiftPeriod = 135, Lift2Phase = 0, LiftMove = 30, LiftHold = 15, Snap = 45;
        // The vines' x (centres) and G_1's.
        internal const float V1X = 16f, V2X = 13f, V3X = 10f, VineLow = S2 + 1.3f, G1X = 27.2f;
        // Hinge_3's chase: it stands up at x 4 and slides ChaseDistance east over ChaseTicks.
        internal const float ChaseDistance = 17.3f;
        internal const int ChaseTicks = 170;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.7f,S3 + .75f),(.6f,1.5f)),
            };
            float rockEnd = RockX + RockCount * RockWidth;

            // ---- Storey 1's floor (y -4..0) ----
            // Ground_0 to Pit_1 (x 17.5-19.5, 1 u deep under Collapse_1); Ground_1 on to the east wall, G_1 in it under slab A's hole.
            Block(elements, SoloRoomElementKind.Floor, "Ground_0", 0f, 17.5f, Base, S1);
            Pit(elements, "Pit_1", 17.5f, 19.5f, Base, S1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", 19.5f, 32f, Base, S1);

            // ---- Slab A (y 4.5-5.5): storey 1's ceiling, storey 2's floor ----
            // Lift_2's seat (x 1-2.5); SA_R (x 3.5-11) holds the cave-in's rocks in its underside; Hinge_2 is the top unit at x 19.5-21
            // on its seat; Cap_1 over Slam_1 (x 23-24.5); the hole over G_1 (x 26.45-27.95).
            Block(elements, SoloRoomElementKind.Floor, "SA_W", 0f, 1f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 1f, 2.5f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 2.5f, RockX, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_R", RockX, rockEnd, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", rockEnd, 19.5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H2", 19.5f, 21f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 21f, 23f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_1", 23f, 24.5f, S2 - .5f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 24.5f, G1X - .75f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_E", G1X + .75f, 32f, S2 - 1f, S2);

            // ---- Slab B (y 10-11): storey 2's ceiling, storey 3's floor ----
            // The hole for Lift_2 (x 1-2.5); Hinge_3 is the top unit at x 2.5-4 on its seat; Bed_3 under Collapse_3 (x 15-17); Cap_2
            // over Slam_2 (x 22-23.5).
            Block(elements, SoloRoomElementKind.Floor, "SB_W", 0f, 1f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H3", 2.5f, 4f, S3 - 1f, S3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 4f, 15f, S3 - 1f, S3);
            Bed(elements, "Bed_3", "Spikes_3", 15f, 17f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 17f, 22f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2", 22f, 23.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 23.5f, 32f, S3 - 1f, S3);

            // ---- The roof (y 16-18): Slam_D is its bottom unit under Cap_D ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 23f, RoofU, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_D", 23f, 24.5f, RoofU + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 24.5f, 32f, RoofU, RoofTop);

            // Honest spikes: the field on storey 2's floor under the vines.
            Spikes(elements, "Spikes_2", 9f, 18f, S2);

            // ---------- Cave-in (storey 1) ----------
            // The rocks, each its own: it falls RockDelay ticks after the cat comes under it, landing just behind a running cat.
            for (int k = 0; k < RockCount; k++)
            {
                float x0 = RockX + k * RockWidth, cx = x0 + RockWidth * .5f;
                elements.Add(E(SoloRoomElementKind.FallingBlock, "Rock_" + (k + 1), (cx, S2 - .75f), (RockWidth, .5f), (cx, (S1 + S2 - 1f) * .5f), (RockWidth, S2 - 1f - S1),
                    new SoloRoomTrapSettings(delayTicks: RockDelay, unitsPerTick: RockSpeed, travelDistance: S2 - 1f - S1)));
            }
            // Pop_1: spikes in the ground at x 13-14.2 spring up as the cat comes within 1.6 u of them: jump them.
            Pop(elements, "Pop_1", 13f, S1, S2 - 1f);
            // Collapse_1 over Pit_1 (x 17.5-19.5): it gives way under a cat that walks onto it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_1",(18.5f,S1 - .5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // Slam_1, slab A's bottom unit at x 23-24.5, pounds the ground on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(23.75f,S2 - .75f),(1.5f,.5f),settings:Slam("Cap_1", S1 - S2 + 1f, Slam1)));
            // G_1, flush in the ground under slab A's hole, throws the cat up onto storey 2 (a 3 u column).
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(G1X,S1 - .15f),(1f,.3f),settings:Geyser(Beat, G1Erupt, 3f)));

            // ---------- Roots (storey 2) ----------
            // Slam_2, slab B's bottom unit at x 22-23.5, pounds the floor on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(22.75f,S3 - .75f),(1.5f,.5f),settings:Slam("Cap_2", S2 - S3 + 1f, Slam2)));
            // Hinge_2, slab A's top unit at x 19.5-21 (hinged at its west end, so its face turns to the west), stands up behind a cat
            // walking west past x 18.8 (off its flat by then: a swing launches a cat standing on it) and slides 1.5 u west to the spike field's edge: a cat that stops there is pushed onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(20.25f,S2 - .25f),(1.5f,.5f),(18.6f,(S2 + S3 - 1f) * .5f),(.4f,S3 - 1f - S2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(-1.5f,0f),moveTicks:12,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // V1-V3 hang from slab B over the spike field (their feet out of a hanging cat's reach of the spikes), each snapping Snap ticks
            // after the cat first touches it.
            Vine(elements, "V1", V1X, VineLow, S3 - 1.05f, Snap);
            Vine(elements, "V2", V2X, VineLow, S3 - 1.05f, Snap);
            Vine(elements, "V3", V3X, VineLow, S3 - 1.05f, Snap);
            // Lift_2, on its seat by the west wall (x 1-2.5), up to storey 3 and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(1.75f,S2 - .25f),(1.5f,.5f),settings:Lift(S3 - S2, Lift2Phase)));

            // ---------- Collapse (storey 3) ----------
            // Hinge_3, slab B's top unit at x 2.5-4 (hinged at its east end, spiked, so its spikes face the cat), stands up as the cat
            // sets off (its cut, x 6.3-6.7) and slides ChaseDistance east after it, a little slower than a run.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(3.25f,S3 - .25f),(1.5f,.5f),(6.5f,(S3 + RoofU) * .5f),(.4f,RoofU - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Pop_3: spikes in the floor at x 9-10.2 spring up as the cat comes within 1.6 u of them: jump them.
            Pop(elements, "Pop_3", 9f, S3, RoofU);
            // Collapse_3 over Bed_3 (x 15-17): it gives way under a cat that walks onto it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_3",(16f,S3 - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // Slam_D, the roof's bottom unit at x 23-24.5, slams 5 u onto the floor 9 ticks after a cat walking east comes within x
            // 21.6, and stays: stop short, hop onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_D",(23.75f,RoofU + .375f),(1.5f,.75f),(22f,(S3 + RoofU + .75f) * .5f),(.8f,RoofU + .75f - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,S3 - RoofU),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Pop_D: spikes in the floor at x 27.4-28.6, on the last step to the door: jump them.
            Pop(elements, "Pop_D", 27.4f, S3, RoofU);

            var sections = new[] {
                CheckpointSection.Start("Cave-in", new Vector2(1.5f, S1), Names("Rock_", RockCount).Concat(new[] { "Pop_1", "Collapse_1", "Slam_1", "G_1" }).ToArray()),
                new CheckpointSection("Roots", new Vector2(25.6f, S2), new Rect(G1X - .75f, S2 - .6f, 1.5f, .2f), Names("V", 3).Concat(new[] { "Slam_2", "Hinge_2", "Lift_2" }).ToArray()),
                new CheckpointSection("Collapse", new Vector2(5f, S3), new Rect(1f, S3 - .6f, 1.5f, .2f), new[] { "Hinge_3", "Pop_3", "Collapse_3", "Slam_D", "Pop_D" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 17.5f, 19.5f, "Ground_0", "Ground_1", "Pit_1_Bottom", "Pit_1") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static string[] Names(string prefix, int count) => Enumerable.Range(1, count).Select(i => prefix + i).ToArray();

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A spike bed in a slab under a collapse (x0-x1): the slab's lower half (a floor, so the storeys stay apart) and the spikes on it.
        static void Bed(List<SoloRoomElement> e, string bed, string spikes, float x0, float x1, float walk)
        {
            e.Add(E(SoloRoomElementKind.Floor, bed, ((x0 + x1) * .5f, walk - .75f), (x1 - x0, .5f)));
            e.Add(E(SoloRoomElementKind.Hazard, spikes, ((x0 + x1) * .5f, walk - .35f), (x1 - x0, .3f)));
        }

        // A pit in storey 1's floor (x0-x1): its bottom and the spikes on it.
        static void Pit(List<SoloRoomElement> e, string id, float x0, float x1, float y0, float y1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, id + "_Bottom", ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
            e.Add(E(SoloRoomElementKind.Hazard, id, ((x0 + x1) * .5f, y1 + .15f), (x1 - x0, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
        }

        // Pop-up spikes 1.2 u wide from x0 in a floor at `walk`, springing 6 ticks after a cat walking east comes within 1.6 u of
        // them (their cut: the storey's height up to `top`, from 1.6 u short of them).
        static void Pop(List<SoloRoomElement> e, string name, float x0, float walk, float top) =>
            e.Add(E(SoloRoomElementKind.HiddenSpikes, name, (x0 + .6f, walk + .15f), (1.2f, .3f), (x0 - .2f, (walk + top) * .5f), (2.8f, top - walk),
                new SoloRoomTrapSettings(revealDelayTicks: 6)));

        // A snap vine at x from y0 to y1, snapping `snap` ticks after the cat first touches it.
        static void Vine(List<SoloRoomElement> e, string name, float x, float y0, float y1, int snap) =>
            e.Add(E(SoloRoomElementKind.Vine, name, (x, (y0 + y1) * .5f), (.6f, y1 - y0), settings: new SoloRoomTrapSettings(delayTicks: snap)));

        // A slam pounding `dy` down on the beat (over 8 ticks, 10 there) and back up into `cap` (its crush partner, over 15), leaving
        // home at room tick `phase`.
        static SoloRoomTrapSettings Slam(string cap, float dy, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: cap, slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, dy), moveTicks: 8, holdTicks: 10, returnTicks: 15, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Beat, phaseTicks: phase, cooldownTicks: 33, movingKind: MovingTrapKind.Solid));

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, float column) =>
            new(new GeyserSettings(GeyserDirection.Up, columnHeight: column), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;

        // A lift rising `rise` and back every LiftPeriod from its phase (over LiftMove each way, LiftHold at the top).
        static SoloRoomTrapSettings Lift(float rise, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset: new Vector2(0f, rise), moveTicks: LiftMove, holdTicks: LiftHold, returnTicks: LiftMove, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: LiftPeriod, phaseTicks: phase, cooldownTicks: 2 * LiftMove + LiftHold, movingKind: MovingTrapKind.Solid));
    }
}
