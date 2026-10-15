using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-150): level 20, "The Machine" (the exam: every earlier lesson once, in a new order; P8 exempts it). The verb
    // is RIDE THE CHAIN REACTION: the cat's own triggers set one machine going, and the machine rebuilds the room around it.
    // Rebuilt on the approved L014/L016 model (the developer: L017-L020 "would I like to play them?"): four storeys 4.5 u apart,
    // climbed bottom to top in a spiral (east, west, east, west), stone between them, every walk busy.
    //  - Works (section 0, S1 and S2). S1, walked east: Hinge_0 stands up behind the start and pushes (jump Spikes_0 at once);
    //    Lift_1 shoots up into S2's slab under a running cat (stop short, let it go up and down); the bridge gives way piece
    //    by piece behind the cat into Pit_B while Arrow_Lo fires along it at shin height without stopping (run, jump the
    //    shot); G_1 launches the cat up the shaft. S2, walked west: Inv_2, in the shaft, swaps left and right in the launch,
    //    for the whole storey (to its end box at the turn): keep steering west onto S2, not east into the nook (Spikes_N).
    //    Slam_2 comes down ahead (stop, hop it); Flip_U2 throws the cat onto the ceiling over Spikes_F2, where CC_2 gives way
    //    into a spiked recess (jump it); Flip_D2 drops it at the turn, where the controls come back.
    //  - Storm (section 1, S3, walked east): V_3 climbs over Thorns_V to S3 and snaps V3Snap ticks after the cat takes it. The
    //    snap is the machine's second half: Inv_3 swaps the controls for 150 ticks, the Cloud wakes behind the cat and chases
    //    it at a run, and Arrow_Hi starts firing along S3 at jump height (walk under its shot, then jump Spikes_3). The mossy
    //    chimney climbs to S4, where the cloud follows.
    //  - Finale (section 2, S4, walked west): Hinge_F stands up behind (spiked), Slam_Fa comes down ahead (stop, hop onto it),
    //    Spikes_F come up, Slam_Fb comes down onto them (the step), and the door retreats onto Door_Ledge as the cat comes off
    //    it: Spikes_D come up under a cat that chases it.
    //  - Dead ends: the Alcove past G_1 (Spikes_A) and the nook east of the shaft (Spikes_N).
    static class L020Layout
    {
        // The storeys' walk lines (room-local y); every slab between them is 1 u of stone, the roof 2.
        internal const float S1 = 0f, S2 = 4.5f, S3 = 9f, S4 = 13.5f, Roof = 17.5f;
        // G_1's eruption (its tell is the 25 ticks before), every 150.
        internal const int G1Erupt = 300, G1Period = 150;
        // The bridge: Bridge_1's wait after Arrow_Lo's first shot, each next piece's after the last.
        internal const int BridgeDelay = 8, BridgeStep = 18;
        // Arrow_Lo (first shot after the bridge's cut, then every delay + cooldown) and Arrow_Hi (after V_3's snap).
        internal const int LoDelay = 25, LoCool = 50, HiDelay = 30, HiCool = 70;
        // The finale's chain, in ticks after Hinge_F fires.
        internal const int SlamFaLag = 4, SpikesFLag = 30, SlamFbLag = 80;
        internal const int V3Snap = 80;
        // V_3's top, just under Bracket_V's underside (11.8), as L014's vines hang 0.05 under their slabs.
        const float VineTop = 11.75f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,8.5f),(1f,22f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,8.5f),(1f,22f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(8.5f,S4 + 1.75f),(.6f,1.5f)),
            };

            // ---- S1, the ground (walk 0) ----
            // Hinge_0's notch (x 0-1.5), the start (x 1.5-10) with Spikes_0, Lift_1's seat (x 10-12), Walk_1a (x 12-13.5) with
            // Kerb_W, Pit_B under the bridge (x 13.5-21.5), Walk_1b (x 21.5-26.5) with Kerb_E, the Plinth (x 26.5-28, top 0.5) with
            // G_1's vent, and the Alcove (x 28-32) under the nook.
            Block(elements, SoloRoomElementKind.Floor, "Notch_0", 0f, 1.5f, -2.5f, -.5f);
            Block(elements, SoloRoomElementKind.Floor, "Start_Floor", 1.5f, 10f, -2.5f, S1);
            Spikes(elements, "Spikes_0", 5.5f, 6.5f, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_1", 10f, 12f, -2.5f, -.5f);
            Block(elements, SoloRoomElementKind.Floor, "Walk_1a", 12f, 13.5f, -2.5f, S1);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_W", 13f, 13.5f, S1, .5f);
            Block(elements, SoloRoomElementKind.PitBottom, "PitB_Floor", 13.5f, 21.5f, -2.5f, -2f);
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Hazard, "Pit_B", new Vector2(17.5f, -1.85f), new Vector2(8f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
            Block(elements, SoloRoomElementKind.Floor, "Walk_1b", 21.5f, 26.5f, -2.5f, S1);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_E", 21.5f, 22f, S1, .5f);
            Block(elements, SoloRoomElementKind.Floor, "Plinth", 26.5f, 28f, -2.5f, .5f);
            Block(elements, SoloRoomElementKind.Floor, "Alcove", 28f, 32f, -2.5f, S1);

            // ---- S2 (walk 4.5), slab 3.5-4.5 ----
            // S2_W (x 0-10) with Thorns_V under V_3, S2_L over Lift_1 (x 10-12, its crush partner), S2_M (x 12-24.5), the shaft
            // over G_1 (x 24.5-28), and the nook (x 28-32: the Alcove's roof).
            Block(elements, SoloRoomElementKind.Floor, "S2_W", 0f, 10f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "S2_L", 10f, 12f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "S2_M", 12f, 24.5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Nook", 28f, 32f, S2 - 1f, S2);
            Spikes(elements, "Thorns_V", 0f, 3f, S2);
            Spikes(elements, "Spikes_F2", 7.5f, 14.5f, S2);

            // ---- S3 (walk 9), slab 8-9 ----
            // V_3's gap (x 0-3.5), SB_1 (x 3.5-12), CC_2 under Cap_C2 (x 12-13.5), SB_2 (x 13.5-18.5), Slam_2 under Cap_S2
            // (x 18.5-20), SB_3 (x 20-32). Spikes_3 midway.
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 3.5f, 12f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C2", 12f, 13.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 13.5f, 18.5f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 18.5f, 20f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", 20f, 32f, S3 - 1f, S3);
            Spikes(elements, "Spikes_3", 15f, 16.2f, S3);
            // The chimney (x 30-31.75): Hang_C under S4's east end (down to 10.3; Arrow_Hi's host), mossy on its east face, and
            // Wall_R's face (Jamb_R over its moss, so the moss has no top to stand on). A cat walks under Hang_C into it.
            Block(elements, SoloRoomElementKind.Wall, "Hang_C", 29.25f, 29.75f, 10.3f, S4 - 1f);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_C", 29.75f, 30f, 10.3f, S4);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_R", 31.75f, 32f, 9.6f, S4);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_R", 31.75f, 32f, S4, Roof);

            // ---- S4 (walk 13.5), slab 12.5-13.5 ----
            // Door_Ledge (x 0-4.5, top 14.5, on Wall_L), S4_W (x 4.5-7.5), Door_Step (x 7.5-9.5, top 14.5), S4_M (x 9.5-23), Hinge_F
            // on its seat (x 23-24.5), S4_E (x 24.5-29.75).
            Block(elements, SoloRoomElementKind.Floor, "Door_Ledge", 0f, 4.5f, S4 - 1f, S4 + 1f);
            Block(elements, SoloRoomElementKind.Wall, "Bracket_V", 1f, 2.2f, VineTop + .05f, S4 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "S4_W", 4.5f, 7.5f, S4 - 1f, S4);
            Block(elements, SoloRoomElementKind.Floor, "Door_Step", 7.5f, 9.5f, S4 - 1f, S4 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "S4_M", 9.5f, 23f, S4 - 1f, S4);
            Block(elements, SoloRoomElementKind.Floor, "Seat_HF", 23f, 24.5f, S4 - 1f, S4 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "S4_E", 24.5f, 29.75f, S4 - 1f, S4);
            // The roof (17.5-19.5): Slam_Fb and Slam_Fa are its bottom unit under caps.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 13.5f, Roof, Roof + 2f);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_Fb", 13.5f, 15f, Roof + 1f, Roof + 2f);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 15f, 17.5f, Roof, Roof + 2f);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_Fa", 17.5f, 19.5f, Roof + 1f, Roof + 2f);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 19.5f, 32f, Roof, Roof + 2f);

            // ---------- Works (S1) ----------
            // Hinge_0, the start's west end (x 0-1.5, hinged at its east end, spiked: its spikes face the cat), stands up 30 ticks
            // into the room (its cut is the start) and slides 4 u east to Spikes_0's edge in 16 ticks: jump them at once.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_0",(.75f,-.25f),(1.5f,.5f),(1.7f,1.75f),(3.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(4f,0f),moveTicks:16,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Lift_1 (x 10-12) shoots 3.35 u up into S2_L 30 ticks after a cat coming east crosses x 8 (in the jump over
            // Spikes_0): a cat that runs on is on it. Stop short, let it go up and down, cross.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(11f,-.25f),(2f,.5f),(8f,1.75f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"S2_L"),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(0f,3.35f),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            var bridge = ((float, float))(17.5f, 1.75f); var bridgeSize = ((float, float))(8f, 3.5f);
            // Arrow_Lo, in Kerb_E, fires west along the bridge at shin height into Kerb_W once a cat is on the bridge, and never stops.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_Lo",(21.75f,.25f),(.5f,.4f),bridge,bridgeSize,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,.25f,13.5f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:LoDelay,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:LoCool))));
            // The bridge (x 13.5-21.5), four pieces: Bridge_1 gives way BridgeDelay ticks after Arrow_Lo's first shot (Arrow_Lo's
            // cut is the bridge's column), each next one BridgeStep after the last (2 u in 18 ticks, just slower than a run),
            // into Pit_B: a cat that stops on one falls onto the spikes.
            for (int i = 0; i < 4; i++)
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.CollapsingFloor,"Bridge_1",(14.5f,-.25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:BridgeDelay,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_Lo"))
                    : E(SoloRoomElementKind.CollapsingFloor,"Bridge_" + (i + 1),(14.5f + 2f * i,-.25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:BridgeStep,triggerSource:TrapTriggerSource.Chain,chainSource:"Bridge_" + i)));
            // G_1, in the Plinth, launches the cat up the shaft (x 24.5-28) to S2 every 150 ticks.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(27.25f,.35f),(1f,.3f),settings:Geyser(G1Period, G1Erupt)));
            // Dead end: the Alcove (Spikes_A, hidden, come up under a cat that walks in past the vent; the cut is the Alcove).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(30.25f,S1 + .15f),(2.5f,.3f),(30f,1.75f),(4f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // ---------- Mirror (S2) ----------
            // Inv_2 (honest, its orb in the shaft): its cut across the shaft just over S2's line, which the launch crosses, swaps
            // left and right until the cat reaches its end box at the turn (where Flip_D2 drops it).
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_2",(26.25f,6.6f),(.6f,1.2f),(26.3f,4.8f),(3.4f,.4f),new SoloRoomTrapSettings(
                new InverterSettings(L004Layout.WholeLevelTicks,endPosition:new Vector2(5.5f,5.75f),endSize:new Vector2(3f,2.5f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Dead end: the nook east of the shaft (Spikes_N, hidden, come up under a cat that lands there).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_N",(30.25f,S2 + .15f),(2.5f,.3f),(30f,S2 + 1.75f),(4f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Slam_2, S3's bottom unit at x 18.5-20, slams 3.5 u onto S2 4 ticks after a cat walking west comes to x 21.2, and
            // stays: stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(19.25f,S3 - .75f),(1.5f,.5f),(21.2f,S2 + 1.75f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,S2 - S3 + 1f),moveTicks:9,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U2",(15.5f,S2 + 1f),(1f,2f),settings:up));
            // CC_2, S3's bottom unit at x 12-13.5, gives way 3 ticks after a cat on the ceiling touches it; the cat falls up into
            // the recess onto Spikes_CC2 (hidden on Cap_C2's underside). Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_2",(12.75f,S3 - .75f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC2",(12.75f,S3 - .65f),(1.5f,.3f),settings:Chain("CC_2", 6)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D2",(6.8f,S3 - 2f),(1f,2f),settings:down));

            // ---------- Storm (S3) ----------
            // V_3 hangs from Bracket_V (stone under Door_Ledge, S4's slab) through its gap down to over Thorns_V; it snaps V3Snap
            // ticks after the cat takes it (its cut is its whole band, S2 to S4's slab). PAX-107 (D-150): it hung from nothing at
            // y 10.4. The bracket's underside (11.8) keeps the vine's top low enough that a jump off it doesn't reach S4, and clear
            // of the leap east onto S3.
            elements.Add(E(SoloRoomElementKind.Vine,"V_3",(1.6f,(S2 + .8f + VineTop) * .5f),(.6f,VineTop - S2 - .8f),(1.6f,(S2 + S4 - 1f) * .5f),(.6f,S4 - 1f - S2),new SoloRoomTrapSettings(delayTicks:V3Snap)));
            // The snap: Inv_3 (its orb high in the gap) swaps the controls for 150 ticks; the Cloud wakes over SB_1 and chases at a
            // run along S3, then (once the cat is up the chimney) along S4's east end; Arrow_Hi starts firing from Hang_C along S3
            // at jump height (over a standing cat), to the room's west end, and never stops.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_3",(.5f,S3 + 3f),(.6f,.8f),settings:new SoloRoomTrapSettings(new InverterSettings(150),Chain("V_3", 1))));
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(5f,S3 + 2.9f),(2f,.8f),settings:new SoloRoomTrapSettings(new StormCloudSettings(new[] {
                    new StormFloor(-100f, S3 + 2.9f, 4.5f, 28f),
                    new StormFloor(S4 - .1f, S4 + 2.9f, 24f, 29.5f) }, followSpeed:.12f, strikePeriod:90, climbSpeed:.24f), Chain("V_3", 25))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_Hi",(29.5f,10.5f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,10.5f,0f,unitsPerTick:.5f,tellTicks:10),
                new SoloRoomTrapSettings(delayTicks:HiDelay,triggerSource:TrapTriggerSource.Chain,chainSource:"V_3",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:HiCool))));

            // ---------- Finale (S4) ----------
            // From the cut at x 20-20.4: Hinge_F (S4's top unit at x 23-24.5, hinged at its west end, spiked: its spikes face the
            // cat) stands up behind the cat and slides 3.5 u west to Slam_Fa; Slam_Fa (x 17.5-19.5) slams down ahead and stays;
            // Spikes_F (x 12.5-17.5) come up; Slam_Fb (x 13.5-15) slams down onto them: the step west.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_F",(23.75f,S4 - .25f),(1.5f,.5f),(20.2f,(S4 + Roof) * .5f),(.4f,Roof - S4),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false,spiked:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(-3.5f,0f),moveTicks:40,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_Fa",(18.5f,Roof + .5f),(2f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:SlamFaLag,offset:new Vector2(0f,S4 - Roof),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_F",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_F",(15f,S4 + .15f),(5f,.3f),settings:Chain("Hinge_F", SpikesFLag)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_Fb",(14.25f,Roof + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:SlamFbLag,offset:new Vector2(0f,S4 - Roof),moveTicks:10,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_F",movingKind:MovingTrapKind.Solid))));
            // Retreat_F: as the cat comes off the slams (the cut at x 11-11.4), the door retreats 6.5 u west, off Door_Step onto
            // Door_Ledge, over 10 ticks; Spikes_D (hidden, x 5-7) come up 25 ticks after the same cut, under a cat that drops off
            // the step to chase the door: jump from the step to the ledge. (Their cut is the retreat's, so no wall jump reaches the
            // door's authored pose without setting them off.)
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat_F",(11.2f,(S4 + Roof) * .5f),(.4f,Roof - S4),settings:new SoloRoomTrapSettings(moveTicks:10,offset:new Vector2(-6.5f,0f))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(6f,S4 + .15f),(2f,.3f),(11.2f,(S4 + Roof) * .5f),(.4f,Roof - S4),new SoloRoomTrapSettings(revealDelayTicks:25)));

            var sections = new[] {
                CheckpointSection.Start("Works", new Vector2(3f, S1), new[] { "Hinge_0", "Lift_1", "Bridge_1", "Bridge_2", "Bridge_3", "Bridge_4", "Arrow_Lo", "G_1", "Spikes_A",
                    "Inv_2", "Spikes_N", "Slam_2", "Flip_U2", "CC_2", "Spikes_CC2", "Flip_D2" }),
                new CheckpointSection("Storm", new Vector2(4.6f, S3), new Rect(3.6f, S3, .2f, S4 - 1f - S3), new[] { "V_3", "Inv_3", "Cloud", "Arrow_Hi" }),
                new CheckpointSection("Finale", new Vector2(28.6f, S4), new Rect(29.25f, S4 - .9f, 2.5f, .6f),
                    new[] { "Hinge_F", "Slam_Fa", "Spikes_F", "Slam_Fb", "Retreat_F", "Spikes_D" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt) =>
            new(new GeyserSettings(), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: ((erupt - GeyserMath.DefaultTellTicks) % period + period) % period));
    }
}
