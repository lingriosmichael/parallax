using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 13, "Old Faithful" (the geyser). The verb is COUNT: two rhythms (100 and 150 ticks) run through
    // the whole level, and the way up is the eruption you waited for, never the one erupting now. A climb from the bottom
    // left to the door at the top right over five storeys (floors 0, 4, 8, 12, 21; pads at 15 and 18), walked right, left,
    // right, left, right.
    //  - Rhythm (section 0, S1 and S2): PAX-107 (D-141), the ground is played with left and right swapped (Invert at the start,
    //    until G_1's launch). The ground: Hinge_1 stands up behind the start: jump Collapse_1 (Pit_1) at once, let Slam_1 come down and hop it,
    //    wait short of Lift_1, count Spikes_S down, ride Ferry_1 over Pit_M. G_1 launches the cat off S1; steered left, it
    //    lands on S2, where Slam_2 comes down behind it; wait short of Lift_2 and Collapse_2, then cross; jump Slam_1's hole. On S2, G_A erupts as the cat arrives and throws it into hidden
    //    spikes under S3; wait for it to stop, pass it, and wait on G_B's vent for G_B.
    //  - Sync (section 1, S3 and S4): Hinge_3 stands up behind the cat and pushes it onto Spikes_3 (PAX-107). Block_C drops beside G_C 60 ticks after the cat comes near; the one safe place to
    //    wait is the vent itself. G_C lifts the cat to S4, where Hinge_S4 stands up behind it: jump Spikes_4. Every 300 ticks G_S1, G_S2 and G_S3 erupt within 55 ticks of each
    //    other and the pads' spikes sink with their vents: ridden on that beat, the three launches carry the cat to S5
    //    (the chaos moment, with G_D). Off the beat, the cat lands on Spikes_P2. Ridden straight up, G_D blows it back down
    //    onto Spikes_P3; the nearer landing, Ledge_5, isn't there.
    //  - Top (section 2, S5): the direct walk crosses G_G, which throws the cat into hidden spikes; the notch drops into a
    //    corridor with two spike rhythms (Spikes_C1 every 100, a safe tile, Spikes_C2 every 150), and G_H lifts the cat
    //    back to S5 by the door.
    //  - Dead ends: the Alcove (an off-beat ride steered left; it gives way and drops the cat back to S4) and S3's west
    //    ledge (G_B's launch steered left, onto hidden spikes).
    static class L013Layout
    {
        // The timeline, in room ticks: each geyser's eruption start (its tell is the 25 ticks before) and each spike strip's
        // down window. The routes wait on the same ticks. PAX-107: G_A at 530 (was 455), after S2's new traps.
        internal const int S1Down = 170, S1Up = 230, G1Erupt = 310, GAErupt = 530, GBErupt = 640, GCErupt = 840;
        // The sync: G_S1 erupts at SyncTick (and every 100); G_S2 20 ticks later (every 150), so the two line up every 300;
        // G_S3 55 and G_D 62 ticks later (every 300). Each pad's spikes are down from 15 ticks before its vent erupts until
        // 45 after.
        internal const int SyncTick = 1210, S2Lag = 20, S3Lag = 55, GDLag = 62;
        internal const int GGErupt = 1350, C1Down = 1395, C1Up = 1455, C2Down = 1470, C2Up = 1530, GHErupt = 1580;

        public static SoloRoomDefinition Build()
        {
            int e2 = SyncTick + S2Lag, e3 = SyncTick + S3Lag;
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,12.5f),(1f,28f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,12.5f),(1f,28f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,25.4f),(32f,2.2f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.8f,21.75f),(.6f,1.5f)),
                // Storeys: S1 (the ground), S2 (4), S3 (8; S3_W west of G_B's shaft), S4 (12; G_C's shaft at x 19-21.5).
                // PAX-107 (D-131): every storey is cut where a floor falls, slams, rises or slides, so each leaves a real hole.
                // The ground: Collapse_1 over Pit_1 (x 5-7), Lift_1 (x 17-18.5, over Notch_1), Ferry_1 over Pit_M (x 21.5-26.5).
                // PAX-107 round fifteen: Hinge_1 is the ground at x 0-1.5, on Notch_0.
                E(SoloRoomElementKind.Floor,"Ground_A",(3.25f,-.5f),(3.5f,1f)),
                E(SoloRoomElementKind.Floor,"Notch_0",(.75f,-.75f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Ground",(12f,-.5f),(10f,1f)),
                E(SoloRoomElementKind.Floor,"Notch_1",(17.75f,-.75f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Ground_C",(20f,-.5f),(3f,1f)),
                E(SoloRoomElementKind.Floor,"Ground_D",(29.25f,-.5f),(5.5f,1f)),
                // S2: Spike_2 (x 7.6-9), Slam_1 (x 13-15), Lift_2 (x 20.1-21.6), Collapse_2 (x 21.6-23); the cat lands from G_1
                // on S2 (x 23-26.5). PAX-107 (the developer: "if this is supposed to be trap it doesnt do anything"): no filler
                // block hangs under a hinge floor in a thin slab; it leaves a real hole once it stands.
                E(SoloRoomElementKind.Floor,"S2_W",(3.8f,3.75f),(7.6f,.5f)),
                E(SoloRoomElementKind.Floor,"S2_M",(11f,3.75f),(4f,.5f)),
                E(SoloRoomElementKind.Floor,"S2_L",(17.55f,3.75f),(5.1f,.5f)),
                E(SoloRoomElementKind.Floor,"S2",(24.75f,3.75f),(3.5f,.5f)),
                // S3: Hinge_3 (x 10.5-12); Slam_2 (x 24.6-26.2) falls out of it onto S2.
                E(SoloRoomElementKind.Floor,"S3_W",(2.5f,7.75f),(5f,.5f)),
                E(SoloRoomElementKind.Floor,"S3",(9f,7.75f),(3f,.5f)),
                E(SoloRoomElementKind.Floor,"S3_B",(18.3f,7.75f),(12.6f,.5f)),
                E(SoloRoomElementKind.Floor,"S3_C",(29.1f,7.75f),(5.8f,.5f)),
                // S4: Lift_4 (x 7-8.5) under Pad_2; Block_C (x 17.25-19) is the section of S4 between S4_W and G_C's shaft.
                E(SoloRoomElementKind.Floor,"S4_A",(3.5f,11.75f),(7f,.5f)),
                // PAX-107 round fifteen: Hinge_S4 is S4_W's section at x 14.75-16.25; S4_W2 east of it is where G_C's launch lands.
                E(SoloRoomElementKind.Floor,"S4_W",(11.625f,11.75f),(6.25f,.5f)),
                E(SoloRoomElementKind.Floor,"S4_W2",(16.75f,11.75f),(1f,.5f)),
                // PAX-107 (the developer: "So much dead space, just cover it wall ... its not used open space"): east of G_C's
                // shaft nothing is played between S3 and the corridor, so S4_E is solid stone from S3's top to the corridor floor
                // (x 21.5-32, y 8-17); S3 stays under it as S2's ceiling.
                E(SoloRoomElementKind.Floor,"S4_E",(26.75f,12.5f),(10.5f,9f)),
                // The stack's pads, and the corridor's floor (with the pit under Ledge_5 at its west end) under S5.
                E(SoloRoomElementKind.Floor,"Pad_2",(7.5f,14.75f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Pad_3",(11f,17.75f),(2.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Sill_F",(13.55f,17.25f),(2.1f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_F",(13.55f,17.65f),(2.1f,.3f)),
                E(SoloRoomElementKind.Floor,"Corr_Floor",(23.3f,17.25f),(17.4f,.5f)),
                E(SoloRoomElementKind.Wall,"Corr_W",(14.85f,19f),(.5f,3f)),
                // S5 (21): the notch x 18.5-20, G_H's shaft x 27.25-28.75.
                E(SoloRoomElementKind.Floor,"S5_A",(16.55f,20.75f),(3.9f,.5f)),
                E(SoloRoomElementKind.Floor,"S5_B",(23.625f,20.75f),(7.25f,.5f)),
                E(SoloRoomElementKind.Floor,"S5_C",(30.375f,20.75f),(3.25f,.5f)),
            };

            // Rhythm. T1: Ledge_2 gives way on a touch over the honest Spikes_1. T2: G_A (every 100) under hidden Spikes_A,
            // revealed by a cut over x 7.6-11, which a cat crosses on its way in from either side (from the west after
            // dropping back down G_B's shaft); G_B (every 150) is the ride.
            // §14 R2: Spikes_S on the walk to G_1 (every 100, down 170-230): the first count, before the first ride.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_S",(20f,.15f),(2f,.3f),settings:Rhythm(100, S1Down, S1Up)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(28f,-.15f),(1f,.3f),settings:Geyser(150, G1Erupt)));
            // PAX-107 (the developer: "This trap with the spikes makes absolutely no sense here in the corner, remove it"):
            // Ledge_2 and Spikes_1 are gone; a launch steered right falls back to the ground by G_1.
            // PAX-107 (the developer: "I can easily run through the bottom floor ... more falling floors that kills you with
            // spikes. Floors that move up suddenly squishing you. I want at least 4 of these in level as you go up ... I want
            // platforms / floors that move sideways"). Every one is on the path a run takes:
            // Collapse_1, the ground at x 5-7, gives way 4 ticks after a touch into Pit_1's spikes: jump it.
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_1_Bottom",(6f,-1.25f),(2f,.5f)));
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_1",new Vector2(6f,-.85f),new Vector2(2f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_1",(6f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // PAX-107 round fifteen (the developer: "add traps in level where the floor stands up and pushes the cat towards a
            // trap"): Hinge_1, the ground at x 0-1.5 behind the start (hinged at its west end), stands up behind a cat walking
            // east past x 3.2 and slides 4.5 u east to Pit_1's edge: a cat that stops short of the jump is pushed onto
            // Collapse_1 and into Pit_1. Jump at once.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_1",(.75f,-.25f),(1.5f,.5f),(3.4f,1.75f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(4.5f,0f),moveTicks:22,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // PAX-107 (D-141, the developer: "arrow inverter at the begining of this level 13"; asked how long, "the ground floor"):
            // the cat spawns in an honest inverter, so left and right are swapped from the first tick (and after every respawn
            // here), until G_1's launch carries it through the end box over the vent (y 3-3.6, above any jump from the ground).
            elements.Add(E(SoloRoomElementKind.Inverter,"Invert",(2.5f,1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(L004Layout.WholeLevelTicks,endPosition:new Vector2(28f,3.3f),endSize:new Vector2(2f,.6f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Slam_1, S2 at x 13-15, slams 3.5 u onto a cat walking under it (trigger: the ground's storey from x 12.4, and its
            // own top) and stays down, leaving its hole in S2: stop short, hop it; on S2, jump the hole.
            // PAX-107 (D-140, the developer: "When it comes down sometimes it doesnt kill you"): Slam_1 and Slam_2 are slams: coming
            // down they kill a cat they land on, even one at their edge that physics used to fling aside.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(14f,3.75f),(2f,.5f),(13.7f,2.28f),(2.6f,4.56f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(0f,-3.5f),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Lift_1, the ground at x 17-18.5 (where a cat would wait for Spikes_S), shoots 3.4 u up into S2 on its own beat
            // (the developer: "appears too slow, I had already stood there waited and jumped over the spikes and then it moved
            // up"): every 100 ticks from 130, up over 8, held 20, down over 10. A cat waiting on it is crushed; count it, and
            // cross once it's down, with Spikes_S.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(17.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"S2_L"),
                new SoloRoomTrapSettings(offset:new Vector2(0f,3.4f),moveTicks:8,holdTicks:20,returnTicks:10,repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:30,cooldownTicks:38,movingKind:MovingTrapKind.Solid))));
            // Ferry_1, the only way over Pit_M (5 u of spikes): at home (x 21.5-23) from 168, out 3.5 u east over 40 from 228,
            // there for 150 (away while Collapse_2 drops a cat into the pit), back over 40; every 290.
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_M_Bottom",(24f,-1.25f),(5f,.5f)));
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_M",new Vector2(24f,-.85f),new Vector2(5f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry_1",(22.25f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3.5f,0f),moveTicks:40,holdTicks:150,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:290,phaseTicks:228,cooldownTicks:230,movingKind:MovingTrapKind.Solid))));
            // S2. Slam_2, S3 at x 24.6-26.2, slams onto where G_1's launch lands (trigger: the arc from x 27.2, S2's storey
            // and its top): move off at once. Collapse_2 (x 21.6-23), where a cat would wait for Lift_2, crumbles 30 ticks
            // after a touch into Pit_M. Lift_2 (x 20.1-21.6) rises 3.4 u into S3 25 ticks after a cat comes within x 24:
            // wait on S2, short of both, then cross.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(25.4f,7.75f),(1.6f,.5f),(25.9f,6.28f),(2.6f,4.56f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:46,offset:new Vector2(0f,-3.5f),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_2",(22.3f,3.75f),(1.4f,.5f),settings:new SoloRoomTrapSettings(delayTicks:30)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(20.85f,3.75f),(1.5f,.5f),(22.05f,5.75f),(3.9f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"S3_B"),
                new SoloRoomTrapSettings(delayTicks:25,offset:new Vector2(0f,3.4f),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_A",(9.5f,3.85f),(1f,.3f),settings:Geyser(100, GAErupt)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_B",(6f,3.85f),(1f,.3f),settings:Geyser(150, GBErupt)));
            // PAX-107: Spike_2, S2 at x 7.6-9, hinged at its west end so its spiked face looks west at G_B's vent (the
            // developer: "the spikes are on the other side"), stands up 30 ticks after a cat passes x 6.7 and slides 2.6 u west
            // over the vent: it arrives just after G_B's launch, so a cat waiting beside the vent, or late onto it, is skewered.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Spike_2",(8.3f,3.75f),(1.4f,.5f),(6.5f,7.75f),(.4f,7.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,hingeTicks:6,hingeAtRight:false,spiked:true),
                new SoloRoomTrapSettings(delayTicks:59,offset:new Vector2(-2.6f,0f),moveTicks:13,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(9.5f,7.35f),(1f,.3f),(9.3f,5.75f),(3.4f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Sync. Spikes_D2 cover S3's west ledge; their trigger is the ledge's whole storey (§13 R3: a cut the coverage search
            // accepts without seeing G_B's launch; every way in crosses it at x 5 as before). Block_C, flush in S4_W beside G_C's shaft, drops 60 ticks after the cat
            // crosses its cut. The Alcove gives way 10 ticks after a touch.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(2.5f,8.15f),(5f,.3f),(2.5f,9.75f),(5f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_C",(20f,7.85f),(1f,.3f),settings:Geyser(150, GCErupt)));
            // PAX-107: Hinge_3, S3 at x 10.5-12 (hinged at its west end), stands up behind a cat past x 13.1 and slides 3 u east
            // to Spikes_3 (x 14-15.2, honest): a cat that stops is pushed onto them. Keep going and jump them.
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_3",(14.6f,8.15f),(1.2f,.3f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(11.25f,7.75f),(1.5f,.5f),(13.3f,9.75f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(3f,0f),moveTicks:15,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // D-131 (PAX-107): Block_C is the section of S4 between S4_W and G_C's shaft (x 17.25-19, its whole thickness), a
            // Solid that drops 3.5 u onto S3 60 ticks after the cat crosses its cut (x 15.6-17.25, under S4_W), so its fall
            // leaves a real hole, east of where G_C's launch lands the cat. PAX-107 round fifteen: its cut runs up to Sill_F (with
            // Hinge_S4 cut out of S4 over it, S4 no longer closes the storey there); it has fired long before a cat is up on S4.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Block_C",(18.125f,11.75f),(1.75f,.5f),(16.425f,12.5f),(1.65f,9f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:60,offset:new Vector2(0f,-3.5f),moveTicks:12,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // PAX-107 round fifteen (the developer: "add traps in level where the floor stands up and pushes the cat towards a
            // trap"): Hinge_S4, S4_W at x 14.75-16.25 (hinged at its east end), stands up behind a cat walking west past x 14.15
            // and slides 2.35 u west to Spikes_4 (x 12.2-13.4, honest, on S4_W): a cat that stops is pushed onto them. Keep going
            // and jump them, short of Lift_4.
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_4",(12.8f,12.15f),(1.2f,.3f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S4",(15.5f,11.75f),(1.5f,.5f),(14.35f,14.5f),(.4f,5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(-2.35f,0f),moveTicks:12,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S1",(4f,11.85f),(1f,.3f),settings:Geyser(100, SyncTick)));
            // PAX-107: Lift_4, S4 at x 7-8.5, rises 2.4 u into Pad_2 12 ticks after a cat walking west comes within x 9.3:
            // stop short, go once it's back down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_4",(7.75f,11.75f),(1.5f,.5f),(8.15f,13.25f),(2.3f,2.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Pad_2"),
                new SoloRoomTrapSettings(delayTicks:12,offset:new Vector2(0f,2.4f),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Alcove",(1f,15.25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:10)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S2",(7.5f,14.85f),(1f,.3f),settings:Geyser(150, e2)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_P2",(7.5f,15.15f),(2f,.3f),settings:Rhythm(150, e2 - 15, e2 + 45)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S3",(11f,17.85f),(1f,.3f),settings:Geyser(300, e3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_P3",(11f,18.15f),(2.5f,.3f),settings:Rhythm(300, e3 - 15, e3 + 45)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_D",(11f,24.45f),(1f,.3f),settings:Geyser(300, SyncTick + GDLag, GeyserDirection.Down)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Ledge_5",(13.5f,20.75f),(1.4f,.5f)));

            // Top. G_G under hidden Spikes_G (a cut over its whole span, so it's covered from both sides); the corridor's two
            // spike rhythms; G_H.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_G",(22.5f,20.85f),(1f,.3f),settings:Geyser(100, GGErupt)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_G",(22.5f,24.15f),(1f,.3f),(22f,22.65f),(2.8f,3.3f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C1",(22f,17.65f),(2f,.3f),settings:Rhythm(100, C1Down, C1Up)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C2",(25.6f,17.65f),(2f,.3f),settings:Rhythm(150, C2Down, C2Up)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_H",(28f,17.35f),(1f,.3f),settings:Geyser(150, GHErupt)));

            var sections = new[] {
                CheckpointSection.Start("Rhythm", new Vector2(2.5f, 0f), new[] { "Invert", "Hinge_1", "Collapse_1", "Slam_1", "Spikes_S", "Lift_1", "Ferry_1", "G_1", "Slam_2", "Collapse_2", "Lift_2", "G_A", "G_B", "Spike_2", "Spikes_A" }),
                new CheckpointSection("Sync", new Vector2(9f, 8f), new Rect(5f, 7.55f, 2.5f, .2f),
                    new[] { "Spikes_D2", "Hinge_3", "G_C", "Block_C", "Hinge_S4", "G_S1", "Lift_4", "Alcove", "G_S2", "Spikes_P2", "G_S3", "Spikes_P3", "G_D", "Ledge_5" }),
                new CheckpointSection("Top", new Vector2(18f, 21f), new Rect(17.6f, 21f, .2f, 3.3f), new[] { "G_G", "Spikes_G", "Spikes_C1", "Spikes_C2", "G_H" }),
            };
            var openings = new[] {
                O(SoloRoomOpeningKind.Pit, 5f, 7f, "Ground_A", "Ground", "Pit_1_Bottom", "Pit_1"),
                O(SoloRoomOpeningKind.Pit, 21.5f, 26.5f, "Ground_C", "Ground_D", "Pit_M_Bottom", "Pit_M"),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        // Periodic spikes that are down over [down, up) and up for the rest of every period: a periodic trap shows on its
        // fire tick (measured; the 6-tick reveal is the overlap delay) and hides `cooldown` ticks later.
        static SoloRoomTrapSettings Rhythm(int period, int down, int up)
        {
            int cooldown = period - (up - down);
            return new SoloRoomTrapSettings(revealDelayTicks: 6, repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: Mod(up, period), cooldownTicks: cooldown);
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
