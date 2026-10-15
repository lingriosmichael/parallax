using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-148): level 18, "Eye of the Storm" (the storm cloud, geysers and a vine), rebuilt to the developer's taste
    // (compact stacked storeys, a moving threat every few steps, more inverters). The verb is GET ABOVE: lightning only
    // strikes down from the cloud's bottom, and the cloud follows the cat at its run, storey to storey (D-116 floors). Three
    // corridors 3.5 u tall on 1.5 u stone slabs, walked east, west, east; each climb (a geyser, a vine) is taken with the
    // cloud closing in, and puts the cat above it until the cloud climbs after it.
    //  - Ground (section 0, S1, walked east): the cloud wakes on the first step and Hinge_0 stands up behind the start and
    //    pushes toward Collapse_1 (jump it). Slam_1 comes down out of slab A ahead (stop short, hop it); Lift_1 shoots up into
    //    slab A as the cat lands (stop short; the cloud catches up: step out from under its charge; cross once it's down).
    //    Inv_1 swaps left and right for the last stretch, through Arrow_1's non-stop shots (jump one), onto the Plinth and G_1's
    //    vent; the cloud charges over the vent before it erupts (step off, back on after the strike); the swap ends in the hole,
    //    mid-launch, so the steer west onto S2 switches hands in the air.
    //  - Storm (section 1, S2, walked west): Hinge_2 stands up behind the landing and pushes a cat that stops onto Spikes_2
    //    (jump them). The eye, Inv_E, swaps left and right again: Slam_2 comes down ahead (stop short, hop onto it, step to its
    //    west end out of the cloud's charge), G_D blows a cat that jumps Spikes_G mid-eruption down onto them (wait it out).
    //    Arrow_2 shoots along the floor without stopping: hop Kerb_2 between shots and leap straight on for V_6 over Spikes_V,
    //    climb above the cloud, and off before V_6 snaps.
    //  - Crown (section 2, S3, walked east): the crumbles give way behind the cat over the Briar; Inv_3 swaps left and right
    //    until the dais: Piston_3 rises into the roof as the cat comes (stop short, cross once it's down); the finale: Hinge_3,
    //    spiked, stands up behind, Slam_3 comes down ahead (hop onto it before the hinge arrives), Spikes_7 come up to the
    //    dais, the cloud charges over the slam (step aside on it), and Slam_3b comes down onto the spikes: the step to the door.
    //  - Dead ends: steering east off the launch onto Ledge_E (Spikes_E); back west off S3's landing down the shaft onto
    //    Spikes_V.
    static class L018Layout
    {
        // The storeys' walk lines; each slab is 1.5 u of stone (its underside the corridor's ceiling below).
        internal const float S1 = 0f, S2 = 5f, S3 = 10f, Slab = 1.5f;
        internal const float A0 = S2 - Slab, B0 = S3 - Slab, R0 = 13.5f, RoofTop = 15.5f;
        // The cloud's height in each corridor (its box 0.8 tall, 0.1 under the slab).
        const float CloudDrop = .5f;
        // G_1's and G_D's beats (room ticks: period, first eruption); Lift_1's and Piston_3's delays after the cat's cut.
        internal const int G1Period = 120, G1Erupt = 115, GDPeriod = 100, GDErupt = 0, LiftDelay = 26, PistonDelay = 10;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,6.75f),(1f,17.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,6.75f),(1f,17.5f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.75f,S3 + .75f + .75f),(.6f,1.5f)),
            };

            // ---- S1, the ground (y -2..0) ----
            // Hinge_0 is the ground's top unit at x 0-1.5 (on Seat_0); Collapse_1 over Pit_1 (x 5-7); Lift_1 the top unit at
            // x 15-16.5 (on Seat_L1); Kerb_1 ends Arrow_1's lane; the Plinth (x 26-32, top 1) holds G_1's vent and Arrow_1.
            Block(elements, SoloRoomElementKind.Floor, "Seat_0", 0f, 1.5f, -2f, -.5f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_A", 1.5f, 5f, -2f, S1);
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_1_Bottom",(6f,-1.5f),(2f,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Pit_1",(6f,-.85f),(2f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 7f, 15f, -2f, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 15f, 16.5f, -2f, -1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_C", 16.5f, 32f, -2f, S1);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_1", 17f, 17.4f, S1, S1 + .6f);
            Block(elements, SoloRoomElementKind.Floor, "Plinth", 26f, 32f, S1, S1 + 1f);

            // ---- Slab A (y 3.5-5): S1's ceiling, S2's floor ----
            // Slam_1 is its bottom unit at x 10-11.5 (under Cap_S1); Hinge_2 its top unit at x 24-25.5 (on Seat_H2); the hole
            // over the Plinth (x 25.5-29.5) is G_1's way up.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 10f, A0, S2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 10f, 11.5f, A0 + .75f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 11.5f, 24f, A0, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H2", 24f, 25.5f, A0, S2 - .5f);
            // Ledge_E, east of the hole: the other ledge off the launch.
            Block(elements, SoloRoomElementKind.Floor, "Ledge_E", 29.5f, 32f, A0, S2);

            // ---- Slab B (y 8.5-10): S2's ceiling, S3's floor ----
            // The shaft (x 0-3) for V_6; the Briar's recess (x 5-9, 1 u deep) under the crumbles; G_D's vent in Lintel_D's
            // underside (x 10-11.5); Slam_2 its bottom unit at x 13-14.5 (under Cap_S2); Hinge_3 and Piston_3 its top units at
            // x 15.5-17 and 18-19.5 (on seats).
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 3f, 5f, B0, S3);
            Block(elements, SoloRoomElementKind.PitBottom, "Briar_Floor", 5f, 9f, B0, S3 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 9f, 10f, B0, S3);
            Block(elements, SoloRoomElementKind.Ceiling, "Lintel_D", 10f, 11.5f, B0, B0 + .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_D", 10f, 11.5f, B0 + .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 11.5f, 13f, B0, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 13f, 14.5f, B0 + .75f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", 14.5f, 15.5f, B0, S3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H3", 15.5f, 17f, B0, S3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_4", 17f, 18f, B0, S3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_P3", 18f, 19.5f, B0, S3 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_5", 19.5f, 32f, B0, S3);
            // The dais the door stands on (x 29.5-32, 0.75 high).
            Block(elements, SoloRoomElementKind.Floor, "Dais", 29.5f, 32f, S3, S3 + .75f);

            // ---- The roof (y 13.5-15.5) ----
            // Slam_3 (x 23-25) and Slam_3b (x 26.5-28) are its bottom units under caps.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 23f, R0, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3", 23f, 25f, R0 + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 25f, 26.5f, R0, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3b", 26.5f, 28f, R0 + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 28f, 32f, R0, RoofTop);

            // Honest spikes: Spikes_2 on S2 past the landing, Spikes_G on S2 under G_D, Spikes_V on S2 under V_6, the Briar.
            Spikes(elements, "Spikes_2", 18.5f, 20f, S2);
            Spikes(elements, "Spikes_G", 10.5f, 11.75f, S2);
            Spikes(elements, "Spikes_V", .5f, 2.6f, S2);
            Spikes(elements, "Briar", 5f, 9f, S3 - 1f);

            // ---- The storm ----
            // The Cloud wakes as the cat sets off (the start's cut, x 2.9-3.3) and follows it at its run, storey to storey: over S1
            // to the vent (x 27: it clears S2's checkpoint by the time the cat gets there), over S2 from x 22 (clear of the
            // checkpoint) to V_6, over S3 from x 5.5 (clear of the shaft's landing) to x 25 (clear of the door).
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(2.5f,A0 - CloudDrop),(2f,.8f),(3.1f,(S1 + A0) * .5f),(.4f,A0 - S1),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] {
                    new StormFloor(-100f, A0 - CloudDrop, 2f, 27f),
                    new StormFloor(S2 + .15f, B0 - CloudDrop, 1.6f, 22f),
                    new StormFloor(S3 + .15f, R0 - CloudDrop, 5.5f, 25f) }, followSpeed:.12f, firstStrikeDelay:50, strikePeriod:90, climbSpeed:.24f), new SoloRoomTrapSettings(delayTicks:0))));

            // ---- Ground (S1) ----
            // Hinge_0 stands up behind the start 30 ticks in (its west end the hinge) and slides 4.5 u east: a cat that waits is
            // pushed onto Collapse_1, which gives way 6 ticks after a touch: jump it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_0",(.75f,-.25f),(1.5f,.5f),(1.6f,(S1 + A0) * .5f),(3.2f,A0 - S1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(4.5f,0f),moveTicks:20,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_1",(6f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:6)));
            // Slam_1 slams 3.5 u onto S1 9 ticks after a cat walking east comes into its cut (x 8.4-9.6), and stays: a cat that
            // runs on is under it; stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(10.75f,A0 + .375f),(1.5f,.75f),(9f,(S1 + A0) * .5f),(1.2f,A0 - S1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,-A0),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Lift_1 shoots up into slab A 26 ticks after the cat crosses x 12.2 (in the hop over Slam_1), up over 8, held 30,
            // down over 15: a cat that runs on is carried up into the slab.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(15.75f,-.5f),(1.5f,1f),(12.2f,(S1 + A0) * .5f),(.4f,A0 - S1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_1"),
                new SoloRoomTrapSettings(delayTicks:LiftDelay,offset:new Vector2(0f,A0),moveTicks:8,holdTicks:30,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Inv_1 (honest) swaps left and right until the cat passes its end box, in the hole over the vent (mid-launch).
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_1",(22f,S1 + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(400,endPosition:new Vector2(27.5f,A0 + .9f),endSize:new Vector2(4f,.4f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Arrow_1 shoots west along S1 at shin height from the Plinth to Kerb_1, from the moment the cat crosses Kerb_1, non-stop.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1",(26.25f,S1 + .3f),(.5f,.4f),(18f,(S1 + A0) * .5f),(.4f,A0 - S1),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,S1 + .3f,17.4f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:10,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:50))));
            // Spikes_E, hidden on Ledge_E, come up as a cat comes east of x 28.85 (from the Plinth's top to slab B, so the drift onto
            // the ledge crosses it in time).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_E",(30.75f,S2 + .15f),(2.5f,.3f),(30.425f,(S1 + 1f + B0) * .5f),(3.15f,B0 - S1 - 1f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(27.5f,S1 + .85f),(1f,.3f),settings:Geyser(G1Period, G1Erupt)));

            // ---- Storm (S2) ----
            // Hinge_2, slab A's top unit at x 24-25.5 (its west end the hinge), stands up as the cat walks west past x 22.2 and
            // slides 3.5 u west: a cat that stops is pushed onto Spikes_2.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(24.75f,S2 - .25f),(1.5f,.5f),(22.2f,(S2 + B0) * .5f),(.4f,B0 - S2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(-3.5f,0f),moveTicks:15,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // The eye: Inv_E (honest) swaps left and right for 200 ticks, to V_6. G_D erupts every 100 ticks.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_E",(16.5f,S2 + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(200),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_D",(10.75f,B0 + .15f),(1f,.3f),settings:Geyser(GDPeriod, GDErupt, GeyserDirection.Down)));
            // Slam_2 slams 3.5 u onto S2 4 ticks after a cat walking west comes to x 15.6 and stays: stop short, hop onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(13.75f,B0 + .375f),(1.5f,.75f),(15.6f,(S2 + B0) * .5f),(.4f,B0 - S2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,S2 - B0),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            Block(elements, SoloRoomElementKind.Wall, "Kerb_2", 7.4f, 7.8f, S2, S2 + .6f);
            // Jamb_2, S2's west end under the shaft, hosts Arrow_2.
            Block(elements, SoloRoomElementKind.Wall, "Jamb_2", 0f, .5f, S2, B0);
            // Arrow_2 (disguised, in Jamb_2) shoots east along S2 at shin height to Kerb_2 from the moment Slam_2 comes down, every
            // 70 ticks.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2",(.25f,S2 + .3f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,S2 + .3f,7.4f,unitsPerTick:.3f,tellTicks:10,disguised:true),
                new SoloRoomTrapSettings(delayTicks:10,triggerSource:TrapTriggerSource.Chain,chainSource:"Slam_2",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:60))));
            // V_6 hangs from Roof_A (its top 0.05 under the roof's underside: the developer, nothing hangs in the air) down the
            // shaft to over Spikes_V; it snaps 90 ticks after the cat takes it. The roof is the level's top, so its top reaches no
            // storey above.
            elements.Add(E(SoloRoomElementKind.Vine,"V_6",(1.3f,(S2 + .6f + R0 - .05f) * .5f),(.6f,R0 - .05f - S2 - .6f),settings:new SoloRoomTrapSettings(delayTicks:90)));

            // ---- Crown (S3) ----
            // The crumbles over the Briar, each giving way 12 ticks after a touch (the Briar's floor is a pit bottom: no ledge to
            // jump up into them from).
            Crumble(elements, "TL_1", 5f, 6.5f);
            Crumble(elements, "TL_2", 6.5f, 8f);
            Crumble(elements, "TL_3", 8f, 9f);
            // Inv_3 (honest) swaps left and right until the dais (its end box): the finale is played mirrored.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_3",(12.5f,S3 + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(400,endPosition:new Vector2(30.75f,S3 + 1.5f),endSize:new Vector2(2.5f,1f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Piston_3, slab B's top unit at x 18-19.5, rises 3.5 u into the roof (its crush partner) 10 ticks after the cat crosses
            // x 16.5, holds 40 and comes back: a cat that runs on is carried up; stop short, cross once it's down.
            // The finale, from the cut at x 21.3-21.7: Hinge_3 (spiked, hinged at its east end, so its spikes face the cat) stands
            // up behind the cat and slides 5 u east to a cat's width short of Slam_3; Slam_3 slams down ahead 4 ticks after it and
            // stays (hop onto it); Spikes_7 come up over S3 to the dais; 130 ticks after Hinge_3, Slam_3b slams down onto them:
            // the step to the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_3",(18.75f,S3 - .5f),(1.5f,1f),(16.5f,(S3 + R0) * .5f),(.4f,R0 - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Roof_A"),
                new SoloRoomTrapSettings(delayTicks:PistonDelay,offset:new Vector2(0f,R0 - S3),moveTicks:8,holdTicks:40,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(16.25f,S3 - .25f),(1.5f,.5f),(21.5f,(S3 + R0) * .5f),(.4f,R0 - S3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(5f,0f),moveTicks:25,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3",(24f,R0 + .375f),(2f,.75f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,S3 - R0),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_7",(27.25f,S3 + .15f),(4.5f,.3f),settings:Chain("Hinge_3", 30)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3b",(27.25f,R0 + .375f),(1.5f,.75f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:130,offset:new Vector2(0f,S3 - R0),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));

            var sections = new[] {
                CheckpointSection.Start("Ground", new Vector2(2.5f, S1), new[] { "Cloud", "Hinge_0", "Collapse_1", "Slam_1", "Lift_1", "Inv_1", "Arrow_1", "Spikes_E", "G_1" }),
                new CheckpointSection("Storm", new Vector2(23.6f, S2), new Rect(22.8f, S2, .2f, B0 - S2),
                    new[] { "Hinge_2", "Inv_E", "G_D", "Slam_2", "Arrow_2", "V_6" }),
                new CheckpointSection("Crown", new Vector2(3.8f, S3), new Rect(4.6f, S3, .2f, R0 - S3),
                    new[] { "TL_1", "TL_2", "TL_3", "Inv_3", "Piston_3", "Hinge_3", "Slam_3", "Spikes_7", "Slam_3b" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 5f, 7f, "Ground_A", "Ground_B", "Pit_1_Bottom", "Pit_1") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // A crumble over the Briar: gives way 12 ticks after a touch.
        static void Crumble(List<SoloRoomElement> e, string name, float x0, float x1) =>
            e.Add(E(SoloRoomElementKind.CollapsingFloor, name, ((x0 + x1) * .5f, S3 - .25f), (x1 - x0, .5f), settings:new SoloRoomTrapSettings(delayTicks:12)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
