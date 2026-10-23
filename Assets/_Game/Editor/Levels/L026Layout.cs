using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-170: level 26, "Liar's Temple" (D-163's sixth). The verb is DON'T TRUST IT: stone that isn't there, a door that leaves, a
    // checkpoint that turns on the cat. Three storeys 4.5 u apart on 1 u slabs (L025's), walked east, west, east.
    //  - Hall (section 0, storey 1, walked east): the first stride of ground past the start is fake (jump it). The spike lake
    //    has a big stepping stone with a small one either side, and both small ones are lies: long-jump onto the big one and off
    //    it. Pop_1 springs out of the ground ahead (jump it), Slam_1 pounds on the beat (through in the gap), and the door, in
    //    sight at the end of the hall, rides its lift up the shaft out of reach as the cat comes to it. G_1 launches the cat up
    //    after it.
    //  - Shrine (section 1, storey 2, walked west): the checkpoint is a liar: Slam_S comes down on it a little after the cat
    //    arrives (or respawns), so a cat that rests there is crushed. Hinge_2 stands up behind and shoves a cat that stops onto
    //    Fake_2b; Fake_2a is the next stride of floor but one (jump both). Slam_2 pounds on the beat. Lift_2 carries the cat up.
    //  - Crown (section 2, storey 3, walked east): the storm cloud chases. Fake_3a and Fake_3b lie over the storey-2 fakes (each
    //    fake column drops a cat all the way into the lake), Pop_3 springs ahead, Slam_D comes down in front of the door and
    //    stays (stop short, hop onto it), and the door stands on its lift where the shaft ends (at the lift's west edge, so the
    //    cat touches it from the stone).
    static class L026Layout
    {
        // The storeys' walk lines (room-local y); each slab under the next storey is 1 u of stone, the roof 2 over 4 u of air.
        internal const float S1 = 0f, S2 = 4.5f, S3 = 9f, RoofU = 13f, RoofTop = 15f, Base = -4f, LakeBed = -3f;
        // The beat for Slam_1, G_1 and Slam_2 (room ticks), and each one's place in it.
        internal const int Beat = 90, Slam1 = 69, G1Erupt = 70, Slam2 = 58;
        internal const int LiftPeriod = 135, LiftPhase = 14, LiftMove = 30, LiftHold = 15;
        // The door's ride: up the shaft from storey 1 to storey 3 over DoorRideTicks, as the cat comes within Retreat's cut.
        internal static readonly Vector2 DoorRide = new(0f, S3 - S1);
        internal const int DoorRideTicks = 40;
        // Slam_S: how long after the cat comes onto the shrine it comes down.
        internal const int ShrineDelay = 55;
        // The fake columns (x): the same stride of stone is a lie on every storey. Each is 2 u wide, so a cat that walks on drops
        // through (a narrower gap is walked over).
        internal const float ColA0 = 9.1f, ColA1 = 11.1f, ColB0 = 15f, ColB1 = 17f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.7f,S1 + .75f),(.6f,1.5f)),
            };

            // ---- Storey 1's floor (y -4..0) ----
            // Ground_0 (x 0-4); Pit_0 (x 4-6.2), Fake_0 over its west end; Ground_1 (x 6.2-8.5); the lake (x 8.5-17.6): Pit_1 with Fake_a in it,
            // Stone_a (x 11.6-14.4), Pit_2 with Fake_b in it; Ground_2 (x 17.6-30.4), G_1's vent in it; Lift_D's seat (x 30.4-32).
            Block(elements, SoloRoomElementKind.Floor, "Ground_0", 0f, 4f, Base, S1);
            Pit(elements, "Pit_0", 4f, 6.2f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", 6.2f, 8.5f, Base, S1);
            Pit(elements, "Pit_1", 8.5f, 11.6f);
            Block(elements, SoloRoomElementKind.Floor, "Stone_a", 11.6f, 14.4f, Base, S1);
            Pit(elements, "Pit_2", 14.4f, 17.6f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_2", 17.6f, 30.4f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_D", 30.4f, 32f, Base, S1 - .5f);

            // ---- Slab A (y 3.5-4.5): storey 1's ceiling, storey 2's floor ----
            // Lift_2's seat (x 1-2.5); Fake_2a and Fake_2b over the lake's fakes; Hinge_2's seat (x 18-19.5); Cap_1 over Slam_1
            // (x 25.45-26.95); the hole over G_1 (x 26.95-28.45); SA_E; Lift_D's shaft (x 30.4-32).
            Block(elements, SoloRoomElementKind.Floor, "SA_W", 0f, 1f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 1f, 2.5f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 2.5f, ColA0, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", ColA1, ColB0, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", ColB1, 18f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H2", 18f, 19.5f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 19.5f, 25.45f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_1", 25.45f, 26.95f, S2 - .5f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_E", 28.45f, 30.4f, S2 - 1f, S2);

            // ---- Slab B (y 8-9): storey 2's ceiling, storey 3's floor ----
            // Lift_2's shaft (x 1-2.5); Cap_2 over Slam_2 (x 4-5.5); Fake_3a and Fake_3b over the storey-2 fakes; Cap_S over the
            // shrine (x 23.75-25.25); Lift_D's hole (x 30.4-32), which its lift fills when it comes up.
            Block(elements, SoloRoomElementKind.Floor, "SB_W", 0f, 1f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 2.5f, 4f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2", 4f, 5.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 5.5f, ColA0, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", ColA1, ColB0, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", ColB1, 23.75f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S", 23.75f, 25.25f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_4", 25.25f, 30.4f, S3 - 1f, S3);

            // ---- The roof (y 13-15): Slam_D is its bottom unit under Cap_D ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 27f, RoofU, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_D", 27f, 28.5f, RoofU + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 28.5f, 32f, RoofU, RoofTop);

            // ---------- Hall (storey 1) ----------
            // Fake_0 looks like the ground running on past the start to a small gap; Fake_a and Fake_b like the small stepping stones either side of
            // Stone_a. Each stands on its pit's spikes.
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_0",(4.8f,(LakeBed + .3f + S1) * .5f),(1.6f,S1 - LakeBed - .3f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_a",((ColA0 + ColA1) * .5f,(LakeBed + .3f + S1) * .5f),(ColA1 - ColA0,S1 - LakeBed - .3f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_b",((ColB0 + ColB1) * .5f,(LakeBed + .3f + S1) * .5f),(ColB1 - ColB0,S1 - LakeBed - .3f)));
            // Pop_1: spikes in the ground at x 20.8-22 spring up as the cat comes within 1.6 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_1",(21.4f,S1 + .15f),(1.2f,.3f),(20.6f,(S1 + S2 - 1f) * .5f),(2.8f,S2 - 1f - S1),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Slam_1, slab A's bottom unit at x 25.45-26.95, pounds the ground on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(26.2f,S2 - .75f),(1.5f,.5f),settings:Slam("Cap_1", S1 - S2 + 1f, Slam1)));
            // The door rides its lift (the seat's lid at x 30.4-32) up the shaft to storey 3's floor as the cat comes within x 25.2,
            // and stays there: the lift fills slab B's hole. Both have the same trigger, so the door rides the lift.
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(25.2f,(S1 + S2 - 1f) * .5f),(.4f,S2 - 1f - S1),settings:new SoloRoomTrapSettings(moveTicks:DoorRideTicks,offset:DoorRide)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_D",(31.2f,S1 - .25f),(1.6f,.5f),(25.2f,(S1 + S2 - 1f) * .5f),(.4f,S2 - 1f - S1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:DoorRide,moveTicks:DoorRideTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // G_1, flush in the ground under the hole (x 27.2-28.2), launches the cat up onto storey 2 on the beat (a 2 u column).
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(27.7f,S1 - .15f),(1f,.3f),settings:Geyser(Beat, G1Erupt, 2f)));

            // ---------- Shrine (storey 2) ----------
            // Slam_S, slab B's bottom unit over the shrine, comes down ShrineDelay ticks after the cat comes onto it (and again while
            // it stays): the checkpoint isn't a place to rest.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_S",(24.5f,S3 - .75f),(1.5f,.5f),(24.575f,(S2 + S3 - .5f) * .5f),(3.65f,S3 - .5f - S2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Cap_S",slams:true),
                new SoloRoomTrapSettings(delayTicks:ShrineDelay,offset:new Vector2(0f,S2 - S3 + 1f),moveTicks:8,holdTicks:20,returnTicks:15,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:43,movingKind:MovingTrapKind.Solid))));
            // Hinge_2, slab A's top unit at x 18-19.5 (hinged at its east end), stands up behind a cat walking west past x 17.5 (off
            // it, by Fake_2b) and slides 2 u west to Fake_2b's edge: a cat that stops short of it is shoved onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(18.75f,S2 - .25f),(1.5f,.5f),(17.3f,(S2 + S3 - 1f) * .5f),(.4f,S3 - 1f - S2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(ColB1 - 19f,0f),moveTicks:12,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_2b",((ColB0 + ColB1) * .5f,S2 - .5f),(ColB1 - ColB0,1f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_2a",((ColA0 + ColA1) * .5f,S2 - .5f),(ColA1 - ColA0,1f)));
            // Slam_2, slab B's bottom unit at x 4-5.5, pounds the floor on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(4.75f,S3 - .75f),(1.5f,.5f),settings:Slam("Cap_2", S2 - S3 + 1f, Slam2)));
            // Lift_2, on its seat by the west wall (x 1-2.5), up 4.5 u to storey 3's floor and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(1.75f,S2 - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,S3 - S2),moveTicks:LiftMove,holdTicks:LiftHold,returnTicks:LiftMove,repeatMode:TrapRepeatMode.Periodic,
                    periodTicks:LiftPeriod,phaseTicks:LiftPhase,cooldownTicks:2 * LiftMove + LiftHold,movingKind:MovingTrapKind.Solid))));

            // ---------- Crown (storey 3) ----------
            // The storm cloud wakes as the cat steps off the lift and chases it east, a little slower than a run.
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(1.5f,RoofU - .9f),(2f,.8f),(4.5f,(S3 + RoofU) * .5f),(.4f,RoofU - S3),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] { new StormFloor(-100f, RoofU - .9f, 1.5f, 25.5f) },
                    followSpeed:.1f, firstStrikeDelay:70, strikePeriod:90), new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_3a",((ColA0 + ColA1) * .5f,S3 - .5f),(ColA1 - ColA0,1f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_3b",((ColB0 + ColB1) * .5f,S3 - .5f),(ColB1 - ColB0,1f)));
            // Pop_3: spikes in the floor at x 21.6-22.8 spring up as the cat comes within 1.6 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_3",(22.2f,S3 + .15f),(1.2f,.3f),(21.4f,(S3 + RoofU) * .5f),(2.8f,RoofU - S3),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Slam_D, the roof's bottom unit at x 27-28.5, slams 4 u onto the floor 9 ticks after a cat walking east comes within x 25.6 (the
            // trigger runs on to the lift's hole, so the slam's top is covered from both sides), and stays: stop short, hop onto it, into the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_D",(27.75f,RoofU + .375f),(1.5f,.75f),(28f,(S3 + RoofU + .75f) * .5f),(4.8f,RoofU + .75f - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,S3 - RoofU),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            var sections = new[] {
                CheckpointSection.Start("Hall", new Vector2(1.5f, S1), new[] { "Fake_0", "Fake_a", "Fake_b", "Pop_1", "Slam_1", "Retreat", "Lift_D", "G_1" }),
                new CheckpointSection("Shrine", new Vector2(24.5f, S2), new Rect(26.95f, S2 - .6f, 1.5f, .2f),
                    new[] { "Slam_S", "Hinge_2", "Fake_2b", "Fake_2a", "Slam_2", "Lift_2" }),
                new CheckpointSection("Crown", new Vector2(3.5f, S3), new Rect(1f, S3 - .6f, 1.5f, .2f),
                    new[] { "Cloud", "Fake_3a", "Fake_3b", "Pop_3", "Slam_D" }),
            };
            var openings = new[] {
                Shaft("Pit_0", 4f, 6.2f, "Ground_0", "Ground_1"),
                Shaft("Pit_1", 8.5f, 11.6f, "Ground_1", "Stone_a"),
                Shaft("Pit_2", 14.4f, 17.6f, "Stone_a", "Ground_2"),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // A pit in storey 1's floor: its bottom from Base up to the lake bed, and the spikes on it.
        static void Pit(List<SoloRoomElement> e, string id, float x0, float x1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, id + "_Bottom", ((x0 + x1) * .5f, (Base + LakeBed) * .5f), (x1 - x0, LakeBed - Base)));
            e.Add(E(SoloRoomElementKind.Hazard, id, ((x0 + x1) * .5f, LakeBed + .15f), (x1 - x0, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
        }

        static SoloRoomOpening Shaft(string id, float x0, float x1, string left, string right) =>
            O(SoloRoomOpeningKind.Pit, x0, x1, left, right, id + "_Bottom", id);

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
    }
}
