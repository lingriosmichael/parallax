using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>Plain PAX-045 component test bench; it is intentionally not a troll level.</summary>
    public static class TrapLabLayout
    {
        public static readonly IReadOnlyList<SoloRoomDefinition> Rooms = new[]
        {
            // PAX-076: rooms 0-2 refitted to D-082's cat (apex 1.6), as rooms 3-4 were in PAX-082: ThinPlatform's top
            // 2.0 -> 1.1 (underside 0.6 clears a walking cat), FixedPillar and Crusher 2 -> 1 tall. Every trigger now
            // spans the cat's band (D-074) and PeriodicSpikes has a 6-tick reveal (D-057).
            Room(0, 0f, new[] {
                E(SoloRoomElementKind.Floor,"ThinPlatform",(3.5f,.85f),(1f,.5f)),
                E(SoloRoomElementKind.HiddenSpikes,"SourceSpikes",(6f,.15f),(2f,.3f),(5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)),
                E(SoloRoomElementKind.FallingBlock,"Block_1",(8f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"SourceSpikes")),
                E(SoloRoomElementKind.FallingBlock,"Block_2",(14f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")),
                E(SoloRoomElementKind.FallingBlock,"Block_3",(20f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2")),
                E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(22f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:6,moveTicks:20,offset:new Vector2(6,0),triggerSource:TrapTriggerSource.Chain,chainSource:"Block_3")) }),
            Room(1, 45f, new[] {
                E(SoloRoomElementKind.MovingTrap,"SlidingSpikes",(8f,.15f),(2f,.3f),(6.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(6,0),moveTicks:30,holdTicks:24,returnTicks:30,cooldownTicks:96,movingKind:MovingTrapKind.Hazard)),
                E(SoloRoomElementKind.MovingTrap,"RisingFloor",(15f,.25f),(4f,.5f),(15f,.75f),(4f,1f),new SoloRoomTrapSettings(offset:new Vector2(0,5.8f),moveTicks:30,holdTicks:18,returnTicks:30,cooldownTicks:90,movingKind:MovingTrapKind.Solid)),
                E(SoloRoomElementKind.Hazard,"CeilingHazard",(15f,6.85f),(4f,.3f)),
                E(SoloRoomElementKind.MovingTrap,"Crusher",(26f,.5f),(1f,1f),(23.3f,3.5f),(3f,7f),new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(-3,0),moveTicks:15,holdTicks:12,returnTicks:15,cooldownTicks:48,movingKind:MovingTrapKind.Solid)), E(SoloRoomElementKind.Wall,"FixedPillar",(22f,.5f),(1f,1f)) }),
            Room(2, 90f, new[] {
                E(SoloRoomElementKind.HiddenSpikes,"PeriodicSpikes",(10f,.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,repeatMode:TrapRepeatMode.Periodic,periodTicks:96,phaseTicks:12,cooldownTicks:24)), E(SoloRoomElementKind.Ceiling,"LowSlab",(10f,1.45f),(4f,.5f)),
                E(SoloRoomElementKind.FallingBlock,"RearmBlock",(16f,6f),(1f,1f),(16f,3.5f),(2f,7f),new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.36f,travelDistance:5.5f,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:48)),
                E(SoloRoomElementKind.CollapsingFloor,"RearmCollapse",(24f,-.5f),(3f,1f),settings:new SoloRoomTrapSettings(delayTicks:12,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:48)), E(SoloRoomElementKind.Hazard,"PitHazard",(24f,-2.85f),(3f,.3f)) }),
            // PAX-074 (D-078): the arrow room. ArrowA: honest, Periodic, fires right along the ShieldA-StopA lane
            // (wait behind ShieldA for an arrow to stop, then cross). ArrowB: disguised in PillarB, fires left at
            // shin height when the cat crosses x 17. ArrowC: honest, in the Overhang, fires left at head height
            // when a jump reaches the band over PillarB (jump-arc trigger containing the lane).
            // PAX-082 (D-082): rescaled to the lower jump (apex 1.6): the walls are 0.6 high (PillarB still hides ArrowB),
            // the Backboard reaches down to y 1.5 (the hop over StopA still hits it; its face ends ArrowC's lane at y 1.7).
            Room(3, 135f, new[] {
                E(SoloRoomElementKind.Wall,"ShieldA",(7.5f,.3f),(1f,.6f)),
                E(SoloRoomElementKind.Wall,"StopA",(14.25f,.3f),(.5f,.6f)),
                E(SoloRoomElementKind.Wall,"PillarB",(22.5f,.3f),(1f,.6f)),
                E(SoloRoomElementKind.Wall,"Backboard",(15.25f,4.25f),(.5f,5.5f)),
                E(SoloRoomElementKind.Wall,"Overhang",(27.25f,4.2f),(.5f,5.6f)),
                E(SoloRoomElementKind.Arrow,"ArrowA",(7.75f,.3f),(.5f,.4f),settings:new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,.3f,14f,unitsPerTick:.36f),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:120,phaseTicks:30,cooldownTicks:60))),
                E(SoloRoomElementKind.Arrow,"ArrowB",(22.25f,.3f),(.5f,.4f),(17.25f,3.5f),(.5f,7f),new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,.3f,14.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:0))),
                E(SoloRoomElementKind.Arrow,"ArrowC",(27.25f,1.7f),(.5f,.4f),(21.25f,1.7f),(11.5f,.4f),new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,1.7f,15.5f,unitsPerTick:.36f),new SoloRoomTrapSettings(delayTicks:0))) })
            // PAX-080 (D-080): the troll-route room (leading comma so room 3's line stays unchanged).
            , Room4()
            // PAX-076 (D-083): the precision room.
            , Room5()
            // PAX-084 (D-086): the spear room.
            , Room6()
            // PAX-085 (D-087): the inverter room.
            , Room7()
            // PAX-086 (D-088): the geyser room.
            , Room8()
            // PAX-087 (D-089): the vine room.
            , Room9()
        };

        // One valid route: Start_Floor -> Up_1 -> Up_2 -> Exit_Perch (door). Betrayals: Stone_A (fake, looks like the
        // start floor going on) and Thin_Collapse (the stone between Up_1 and Up_2) drop the cat into the pit;
        // Ledge_End (fake, looks like Up_2 going on) drops it into the Gutter, a dead end it climbs out of back to
        // Up_2; running on from the Gutter crosses ArrowD's trigger: the arrow fires over the Bridge, then the
        // Bridge goes. The door sits left of ArrowD's trigger, so the valid route never fires it; the Exit_Perch is
        // too high to reach from the Gutter.
        static SoloRoomDefinition Room4()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Start_Floor",(2.5f,-.5f),(5f,1f)),
                E(SoloRoomElementKind.Wall,"Pit_L",(4.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.Wall,"Pit_R",(31.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"Pit_Bottom",(18f,-3.5f),(26f,1f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_Hazard",new Vector2(18f,-2.85f),new Vector2(26f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                // PAX-082 (D-082): rescaled to the lower jump (apex 1.6): Up_1/Thin_Collapse/Up_2 at 1.1 (their underside, 0.6,
                // clears a walking cat), the perch at 2.1 (1.0 above Up_2, out of reach from the Gutter). The climb back jumps
                // straight up from the gap left of the perch, then steers onto Up_2.
                E(SoloRoomElementKind.FakePlatform,"Stone_A",(5.625f,-.25f),(1.25f,.5f)),
                E(SoloRoomElementKind.Floor,"Up_1",(7.75f,.85f),(3f,.5f)),
                E(SoloRoomElementKind.CollapsingFloor,"Thin_Collapse",(10.1f,.85f),(.6f,.5f),settings:new SoloRoomTrapSettings(delayTicks:12)),
                E(SoloRoomElementKind.Floor,"Up_2",(12.4f,.85f),(3f,.5f)),
                E(SoloRoomElementKind.FakePlatform,"Ledge_End",(14.55f,.85f),(1.3f,.5f)),
                E(SoloRoomElementKind.Floor,"Exit_Perch",(16.7f,1.85f),(3f,.5f)),
                E(SoloRoomElementKind.Door,"Door",(17.7f,2.85f),(.6f,1.5f)),
                E(SoloRoomElementKind.Floor,"Gutter",(18.7f,-.5f),(9.6f,1f)),
                E(SoloRoomElementKind.Wall,"Stop_D",(23.75f,3.25f),(.5f,3.5f)),
                E(SoloRoomElementKind.Wall,"Backstop",(31.5f,3.25f),(1f,3.5f)),
                E(SoloRoomElementKind.Floor,"Far_Floor",(31.5f,-.5f),(1f,1f)),
                E(SoloRoomElementKind.Arrow,"ArrowD",(31.25f,2.5f),(.5f,.4f),(23.75f,3.5f),(.5f,7f),new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,2.5f,24f,unitsPerTick:.36f),new SoloRoomTrapSettings(delayTicks:0))),
                E(SoloRoomElementKind.CollapsingFloor,"Bridge",(27.5f,-.5f),(7f,1f),settings:new SoloRoomTrapSettings(delayTicks:24,triggerSource:TrapTriggerSource.Chain,chainSource:"ArrowD")),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 5f, 31f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            var jumps = new[] {
                new RequiredJump("Pit_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.5f,6.75f,0f,1.1f,2.5f,sourceName:"Start_Floor",destinationName:"Up_1"),
                new RequiredJump("Pit_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,8.75f,11.4f,1.1f,1.1f,2f,sourceName:"Up_1",destinationName:"Up_2"),
                new RequiredJump("Pit_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,13.4f,15.7f,1.1f,2.1f,2f,sourceName:"Up_2",destinationName:"Exit_Perch"),
                new RequiredJump("Pit_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,14.6f,13.4f,0f,1.1f,.5f,sourceName:"Gutter",destinationName:"Up_2"),
            };
            return new SoloRoomDefinition(4, 180f, 32f, elements, openings, jumps);
        }

        // PAX-076 (D-083): the precision room, wider than one screen. Eight narrow, thin platforms (P1-P8, 2 wide, 0.5
        // thick) over a pit, alternating comfortable gaps (1.9, a 2.9 jump: D-056's 0.75) and precision gaps (2.2, a
        // 3.2 jump: only at the section's 0.85). Block_P5 falls on a cat that stops on P5. From P8 the door's ledge
        // looks one jump away: that's the bait gap (6.5 u, out of reach at full reach). The way round is Step, a wide
        // low platform under the gap: drop onto it, then a precision jump up to the Exit.
        static SoloRoomDefinition Room5()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(24.5f,7.5f),(49f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Start_Floor",(2.5f,-.5f),(5f,1f)),
                E(SoloRoomElementKind.Wall,"Pit_L",(4.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.Wall,"Pit_R",(48.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"Pit_Bottom",(26.5f,-3.5f),(43f,1f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_Hazard",new Vector2(26.5f,-2.85f),new Vector2(43f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
            };
            float[] left = { 6.9f, 11.1f, 15f, 19.2f, 23.1f, 27.3f, 31.2f, 35.4f };
            for (int i = 0; i < left.Length; i++) elements.Add(E(SoloRoomElementKind.Floor,"P" + (i + 1),(left[i] + 1f,-.25f),(2f,.5f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_P5",(24.1f,6f),(1f,1f),(24.1f,3.5f),(1f,7f),new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.36f,travelDistance:5.5f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Step",(40.4f,-1.25f),(3f,.5f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Exit",(45.4f,-.75f),(3f,.5f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(46.2f,.25f),(.6f,1.5f)));

            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 5f, 48f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            // Take-off 0.5 inside the source's edge, landing 0.5 inside the destination's (the collider's half-width).
            var jumps = new List<RequiredJump> { Jump(4.5f, 7.4f, 0f, 0f, "Start_Floor", "P1") };
            for (int i = 0; i < left.Length - 1; i++) jumps.Add(Jump(left[i] + 1.5f, left[i + 1] + .5f, 0f, 0f, "P" + (i + 1), "P" + (i + 2)));
            jumps.Add(Jump(36.9f, 39.4f, 0f, -1f, "P8", "Step"));
            jumps.Add(Jump(41.4f, 44.4f, -1f, -.5f, "Step", "Exit"));
            var sections = new[] { new PrecisionSection("Precision_Run", Rect.MinMaxRect(6f, -1.5f, 48f, 2.5f)) };
            var baits = new[] { new BaitGap("Exit_Gap", 37.4f, 0f, 43.9f, -.5f) };
            return new SoloRoomDefinition(5, 225f, 49f, elements.ToArray(), openings, jumps.ToArray(), null, sections, baits);
        }

        // PAX-084 (D-086): the spear room, origin 287 (room 5 ends at 274; the same 13 u gap as rooms 4-5). A far wall
        // (FarWall, top 4.0, the door on it) no jump can climb. Crossing the cut at x 3.5 sets off a volley 30 ticks later:
        // three honest spears from Launcher_Wall into FarWall's face at rising heights (lanes y 0.5, 1.6, 2.7; 3.6, 2.4 and
        // 1.2 long), Spear_2 72 ticks after Spear_1 (so a cat that climbs as soon as Spear_1 sticks meets it), Spear_3 12
        // after that. Spear_1 flies at shin height over the whole floor; the only cover is the Dip (0.4 deep), where a cat is
        // under every lane. Stuck, the spears are a staircase up to the door: each step 1.1 higher and 1.2 further out, so
        // each is reachable only from the one below.
        static SoloRoomDefinition Room6()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(8f,7.5f),(16f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Floor_A",(2.5f,-.5f),(5f,1f)),
                E(SoloRoomElementKind.Floor,"Dip",(5.7f,-.95f),(1.4f,1.1f)),   // bottom -1.5, not -1.4: keeps the room bounds exact in float
                E(SoloRoomElementKind.Floor,"Floor_B",(11.2f,-.5f),(9.6f,1f)),
                E(SoloRoomElementKind.Wall,"Launcher_Wall",(.5f,3.5f),(1f,7f)),
                E(SoloRoomElementKind.Wall,"FarWall",(14f,2f),(4f,4f)),
                E(SoloRoomElementKind.Door,"Door",(14.5f,4.75f),(.6f,1.5f)),
                E(SoloRoomElementKind.Arrow,"Spear_1",(.75f,.5f),(.5f,.4f),(3.75f,3.5f),(.5f,7f),new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right,.5f,12f,length:3.6f),new SoloRoomTrapSettings(delayTicks:30))),
                E(SoloRoomElementKind.Arrow,"Spear_2",(.75f,1.6f),(.5f,.4f),settings:new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right,1.6f,12f,length:2.4f),new SoloRoomTrapSettings(delayTicks:72,triggerSource:TrapTriggerSource.Chain,chainSource:"Spear_1"))),
                E(SoloRoomElementKind.Arrow,"Spear_3",(.75f,2.7f),(.5f,.4f),settings:new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right,2.7f,12f,length:1.2f),new SoloRoomTrapSettings(delayTicks:12,triggerSource:TrapTriggerSource.Chain,chainSource:"Spear_2"))),
            };
            // Dip -> Floor_B, then Floor_B -> Spear_1 -> Spear_2 -> Spear_3 -> FarWall: the spears' exposed tops are x[8.4,9.6],
            // [9.6,10.8] and [10.8,12]; each climb takes off 0.5 inside one step and lands 0.5 inside the next.
            var jumps = new[] {
                J6(5.9f, 6.9f, -.4f, 0f, "Dip", "Floor_B"),
                J6(7.9f, 8.9f, 0f, .7f, "Floor_B", "Spear_1"),
                J6(9.1f, 10.1f, .7f, 1.8f, "Spear_1", "Spear_2"),
                J6(10.3f, 11.3f, 1.8f, 2.9f, "Spear_2", "Spear_3"),
                J6(11.5f, 12.5f, 2.9f, 4f, "Spear_3", "FarWall"),
            };
            return new SoloRoomDefinition(6, 287f, 16f, elements, System.Array.Empty<SoloRoomOpening>(), jumps);
        }

        // PAX-085 (D-087): the inverter room, origin 316 (room 6 ends at 303; the same 13 u gap). A corridor: hop Spikes_Back,
        // then the Inverter (an honest cyan pillar, 3 tall, so no jump clears it) just before a 1.5 u spike pit, and the
        // door beyond. Touching it swaps left and right for 150 ticks: a cat still holding right runs back into Spikes_Back.
        // Wait it out (the cue blinks over its last 30 ticks), or play it inverted: hold left to go right.
        static SoloRoomDefinition Room7()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(10f,7.5f),(20f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Floor_A",(5.5f,-.5f),(11f,1f)),
                E(SoloRoomElementKind.Hazard,"Spikes_Back",(5.5f,.15f),(1f,.3f)),
                E(SoloRoomElementKind.Inverter,"Inverter",(10f,1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(),new SoloRoomTrapSettings(delayTicks:0))),
                E(SoloRoomElementKind.Wall,"Pit_L",(10.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.Wall,"Pit_R",(13f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"Pit_Bottom",(11.75f,-3.5f),(1.5f,1f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_Hazard",new Vector2(11.75f,-2.85f),new Vector2(1.5f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Floor,"Floor_C",(16.25f,-.5f),(7.5f,1f)),
                E(SoloRoomElementKind.Door,"Door",(18f,.75f),(.6f,1.5f)),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 11f, 12.5f, "Pit_L", "Pit_R", "Pit_Bottom", "Pit_Hazard") };
            var jumps = new[] {
                new RequiredJump("Spikes_Back",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.5f,6.5f,0f,0f,2.5f,hazardHeight:.3f,sourceName:"Floor_A",destinationName:"Floor_A"),
                new RequiredJump("Pit_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,10.5f,13f,0f,0f,1f,sourceName:"Floor_A",destinationName:"Floor_C"),
            };
            return new SoloRoomDefinition(7, 316f, 20f, elements, openings, jumps);
        }

        // PAX-086 (D-088): the geyser room, origin 349 (room 7 ends at 336; the same 13 u gap). The exit Ledge (top 3.5, over
        // the start) is out of any jump's reach. The Geyser, a vent in the floor at x 9, erupts every 100 ticks (2 s): tell
        // from room tick 80, erupt 105-144. Stand on it and ride the eruption, then steer left onto the Ledge and the door.
        // Right of the vent, the side the solution never goes: a LowCeiling with hidden Ceiling_Spikes under it, revealed by
        // a full-storey cut (x 9.6-10.1, floor to ceiling underside, R4), so a launch steered right rises into them. A cat that
        // walks there on the floor sees them pop out, harmlessly. Stepping on the vent during the tell and walking off leaves
        // the cat under the Ledge; it walks back and rides the next eruption.
        static SoloRoomDefinition Room8()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(10f,7.5f),(20f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Floor",(10f,-.5f),(20f,1f)),
                E(SoloRoomElementKind.Floor,"Ledge",(3.75f,3.25f),(7.5f,.5f)),
                E(SoloRoomElementKind.Door,"Door",(2.5f,4.25f),(.6f,1.5f)),
                E(SoloRoomElementKind.Geyser,"Geyser",(9f,-.15f),(1f,.3f),settings:new SoloRoomTrapSettings(new GeyserSettings(),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:80))),
                E(SoloRoomElementKind.Ceiling,"LowCeiling",(15.5f,5.5f),(9f,.6f)),
                E(SoloRoomElementKind.HiddenSpikes,"Ceiling_Spikes",(15.5f,5.05f),(9f,.3f),(9.85f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)),
            };
            return new SoloRoomDefinition(8, 349f, 20f, elements, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }

        // PAX-087 (D-089): the vine room, origin 382 (room 8 ends at 369; the same 13 u gap). The Cliff (x 11-20, top 4.5)
        // holds the door; no jump reaches it. Two vines, both reaching y 5.1 (the Cliff's top plus about the cat's height, so
        // a cat stopped at a vine's top has its feet level with the Cliff): Vine_Real stands on Floor_Left at x 7.9; climb
        // it and leap right onto the Cliff. Vine_Obvious hangs over the Pit (x 9-11) at x 9.6, next to the Cliff: a snap
        // vine whose Trigger is at y 2-2.5; 12 ticks after a climbing cat touches it, it vanishes and the cat falls onto the
        // PitHazard. A cat that leaps back to Floor_Left in those 12 ticks sees it snap, and takes Vine_Real.
        static SoloRoomDefinition Room9()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(10f,7.5f),(20f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"Floor_Left",(4.5f,-.5f),(9f,1f)),
                E(SoloRoomElementKind.Wall,"PitWall_Left",(8.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"PitBottom",(10f,-3.5f),(2f,1f)),
                E(SoloRoomElementKind.Hazard,"PitHazard",(10f,-2.85f),(2f,.3f)),
                E(SoloRoomElementKind.Wall,"Cliff",(15.5f,.25f),(9f,8.5f)),
                E(SoloRoomElementKind.Door,"Door",(18f,5.25f),(.6f,1.5f)),
                E(SoloRoomElementKind.Vine,"Vine_Real",(7.9f,2.55f),(.6f,5.1f)),
                E(SoloRoomElementKind.Vine,"Vine_Obvious",(9.6f,2.55f),(.6f,5.1f),(9.6f,2.25f),(.6f,.5f),new SoloRoomTrapSettings(delayTicks:12)),
            };
            var openings = new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 9f, 11f, "PitWall_Left", "Cliff", "PitBottom", "PitHazard") };
            return new SoloRoomDefinition(9, 382f, 20f, elements, openings, System.Array.Empty<RequiredJump>());
        }

        static RequiredJump J6(float takeoffX, float landingX, float takeoffPaw, float landingPaw, string source, string destination) =>
            new(destination, RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, takeoffX, landingX, takeoffPaw, landingPaw, .5f, sourceName: source, destinationName: destination);

        static RequiredJump Jump(float takeoffX, float landingX, float takeoffPaw, float landingPaw, string source, string destination) =>
            new("Pit_Bottom", RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, takeoffX, landingX, takeoffPaw, landingPaw, 1.5f, sourceName: source, destinationName: destination);

        static SoloRoomDefinition Room(int id, float origin, SoloRoomElement[] traps)
        {
            var elements = new List<SoloRoomElement> { E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)), E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)), E(SoloRoomElementKind.Door,"Door",(id == 0 ? 22f : 30f,.75f),(.6f,1.5f)) };
            if (id == 2)
            {
                elements.Add(E(SoloRoomElementKind.Floor,"Floor_Left",(11.25f,-.5f),(22.5f,1f)));
                elements.Add(E(SoloRoomElementKind.Floor,"Floor_Right",(28.75f,-.5f),(6.5f,1f)));
                elements.Add(E(SoloRoomElementKind.PitBottom,"PitBottom",(24f,-3.5f),(3f,1f)));
                elements.Add(E(SoloRoomElementKind.Wall,"PitWall_Left",(22f,-2.5f),(1f,3f)));
                elements.Add(E(SoloRoomElementKind.Wall,"PitWall_Right",(26f,-2.5f),(1f,3f)));
            }
            else elements.Add(E(SoloRoomElementKind.Floor,"Floor",(16f,-.5f),(32f,1f)));
            elements.AddRange(traps);
            SoloRoomOpening[] openings = id == 2 ? new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 22.5f, 25.5f, "PitWall_Left", "PitWall_Right", "PitBottom", "PitHazard") } : System.Array.Empty<SoloRoomOpening>();
            return new SoloRoomDefinition(id, origin, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>());
        }
        static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x,float y) p, (float x,float y) s, (float x,float y) p2 = default, (float x,float y) s2 = default, SoloRoomTrapSettings settings = default) => new(kind, name, new Vector2(p.x,p.y), new Vector2(s.x,s.y), new Vector2(p2.x,p2.y), new Vector2(s2.x,s2.y), settings);
    }
}
