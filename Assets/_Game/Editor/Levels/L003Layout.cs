using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 3, a tall climb, bottom left to top right. Three storeys: the ground (top 0), S1 (a slab,
    // top 5) and S2 (top 10) under a roof at 14. The ground runs right to the right tower, S1 back left to the left tower,
    // S2 right to the door. Each tower is a zigzag of treads; in each, the tread that keeps the rhythm past the storey
    // gives way (a collapsing tread chained to spikes on the tread below). A fall from a higher storey only kills where a
    // collapse has armed spikes for it; the storeys below are otherwise safe to land on.
    // D-113 (the developer: "Level 3 is way too easy"): S1 is the arrow floor: non-stop arrows from both ends at the cat's
    // height (a launcher in a low post at each end), a lift section that sinks to the ground and comes back up, and a section
    // that shrinks away under the cat; the holes are over the ground's spiked pits (a new one, Pit 5, under the lift's gap).
    // S2's arrows are gone: it is a spike bed with moving platforms, stepping stones and fake landings over it (D-113
    // amendment).
    static class L003Layout
    {
        // S1's arrow lane: at the cat's height, between the posts' faces.
        const float LaneY = 5.3f, PostLFace = 8f, PostRFace = 22.25f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,11.5f),(1f,7f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,11.5f),(1f,7f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                // The ground.
                E(SoloRoomElementKind.Floor,"Ground_1",(4f,-2f),(8f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2",(10.3f,-2f),(1f,4f)),
                // D-113: Ground_3 is split by Pit 5, under Lift_1's gap.
                E(SoloRoomElementKind.Floor,"Ground_3",(14.575f,-2f),(3.95f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_4",(25.225f,-2f),(13.55f,4f)),
                // The right tower, up to S1.
                E(SoloRoomElementKind.Floor,"Tread_R1",(26.5f,-1.375f),(2f,5.25f)),
                E(SoloRoomElementKind.Floor,"Tread_R2",(30.1f,-.75f),(3.8f,6.5f)),
                E(SoloRoomElementKind.Floor,"Tread_R3",(26.75f,3.5f),(1.5f,.5f)),
                // S1 (D-113), right to left: S1_B (the landing, with Post_R), Lift_1, its open gap (x 16.5-18.5, over Pit 5),
                // S1_C, Shrink_1 (over the ground's pits 1 and 3), and S1_A, raised 0.8 u (the developer: fill the step up to the
                // old Post_L) to the left tower; Arrow_L sits in its face.
                E(SoloRoomElementKind.Floor,"S1_A",(6.25f,4.9f),(3.5f,1.8f)),
                E(SoloRoomElementKind.Floor,"S1_C",(14.55f,4.5f),(3.9f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(23.15f,4.5f),(4.3f,1f)),
                // Post_R: 1.2 u tall (the developer: "make this 50% taller").
                E(SoloRoomElementKind.Wall,"Post_R",(22.75f,5.6f),(1f,1.2f)),
                // The left tower, up to S2.
                E(SoloRoomElementKind.Floor,"Tread_A",(3.05f,6f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_B",(.75f,7.25f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_C",(3.05f,8.5f),(1.5f,.5f)),
                // S2 (D-113 amendment), left to right: S2_A, the spike bed (x 9-26) with its platforms over it, S2_C to the door
                // (a solid block down to y 6.5: the developer, "fill the empty space below"; clear of the right tower's jumps).
                E(SoloRoomElementKind.Floor,"S2_A",(6.75f,9.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Floor,"S2_C",(29f,8.25f),(6f,3.5f)),
                E(SoloRoomElementKind.Floor,"Hop_1",(11.7f,10.55f),(1f,.5f)),
            };
            // T1: a floor that isn't there. T2: the floor where T1's jump lands gives way under a cat that stops.
            L001Layout.AddShaft(elements, 1, 8f, 9.8f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_1",(8.9f,-1.5f),(1.8f,3f)));
            L001Layout.AddShaft(elements, 3, 10.8f, 12.6f);
            L001Layout.AddShaft(elements, 5, 16.55f, 18.45f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_3",(11.7f,-1.5f),(1.8f,3f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            // Dead end: the right tower's rhythm goes on past S1; that tread gives way onto spikes on the tread below. The
            // spikes' trigger is the space over the tread (its top to S2's underside), so only a cat on the tread sets it off,
            // never one jumping under it from Tread_R2; the tread gives way as the spikes show. It sits right of the Tread_R2 to
            // Tread_R3 take-off (x 29.5-31), so that jump doesn't hit its underside.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_R",(30.75f,2.65f),(2.5f,.3f),(30.25f,7f),(1.5f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_R4",(30.25f,4.75f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_R",delayTicks:1)));
            // D-113, S1. The arrows: Arrow_L in S1_A's face flies right, Arrow_R in Post_R flies left, along the whole floor at the
            // cat's height: Arrow_R every 100 ticks, Arrow_L every 200 (the developer: "more time in between arrows"), on the
            // lift's rhythm; jump each one.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_L",(7.75f,LaneY),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,LaneY,PostRFace),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:200,phaseTicks:0,cooldownTicks:62))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_R",(22.5f,LaneY),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,LaneY,PostLFace),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:50,cooldownTicks:62))));
            // Lift_1 (2.5 u, carries the cat): a section of S1 that sinks to the ground and comes back up (down over 35 ticks,
            // 45 there, up over 35, every 200; 85 up). Up, it's a step between S1_B and the 2 u gap to S1_C; down, the gap is
            // 4.5 u and too wide to jump. Its seam with S1_B (x 21) is clear of where the cat lands off Post_R (a landing across
            // the seam made replays differ).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(19.75f,4.5f),(2.5f,1f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,-3.9f),moveTicks:35,holdTicks:45,returnTicks:35,repeatMode:TrapRepeatMode.Periodic,periodTicks:200,phaseTicks:0,cooldownTicks:115,movingKind:MovingTrapKind.Solid))));
            // Shrink_1 (4.6 u, over the ground's pits 1 and 3; too wide to jump across): shrinks away from its right end, behind
            // the cat, over 50 ticks from when the cat comes onto it; a cat that stops on it falls.
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Shrink_1",(10.3f,4.5f),(4.6f,1f),(12.1f,9.5f),(1f,9f),new SoloRoomTrapSettings(new ShrinkSettings(50,0f,ShrinkFrom.Right),new SoloRoomTrapSettings(delayTicks:0))));
            // T4: the left tower's rhythm goes on to the wall at the top; that tread gives way onto spikes on the tread below.
            // As with Tread_R4, the trigger is the space over the tread (its top to the roof), out of reach of a jump from below.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_B",(.5f,7.65f),(1f,.3f),(.5f,12.125f),(1f,3.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_D",(.5f,10f),(1f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_B",delayTicks:1)));
            // D-113 amendment (the developer: "make the whole bottom full of spikes, and all the platforms that are moving above
            // the ground ... precision jumping between them ... jump up and down onto the moving platforms. Make some false
            // landings"): S2 is one spike bed from S2_A to S2_C. Over it: False_1 (fake, the obvious first step), Hop_1, Ride_1
            // (shuttles right), Lift_2 (bobs up 1.4 u; jumped onto while it's up), False_2 (fake, in the gap after the lift),
            // Hop_2 (dropped onto, over False_2), Ride_2 (fetches the cat to S2_C). Nothing stands under a fake but spikes.
            AddTrench(elements, "Trench", 9f, 26f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"False_1",(10.2f,10.55f),(1f,.5f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"False_2",(20.1f,10.95f),(1f,.5f)));
            // Ride_1: home x 13-14.4 (top 10.4), out 2.4 u over 50 ticks, 20 there, back over 50, every 180 (60 at home).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_1",(13.7f,10.15f),(1.4f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(2.4f,0f),moveTicks:50,holdTicks:20,returnTicks:50,repeatMode:TrapRepeatMode.Periodic,periodTicks:180,phaseTicks:0,cooldownTicks:120,movingKind:MovingTrapKind.Solid))));
            // Lift_2: top 10.2, up 1.4 u over 30 ticks, 60 there, down over 30, every 180; up while Ride_1 waits at its far end.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(18.2f,9.95f),(1.2f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,1.4f),moveTicks:30,holdTicks:60,returnTicks:30,repeatMode:TrapRepeatMode.Periodic,periodTicks:180,phaseTicks:20,cooldownTicks:120,movingKind:MovingTrapKind.Solid))));
            // Hop_2 (the developer: "make this platform shift up"): rises 0.8 u over 30 ticks, waits 30, sinks over 30, every 180;
            // up while Ride_2 waits to fetch the cat (step down onto it), down while Ride_2 is home.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hop_2",(21.7f,10.55f),(1f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,.8f),moveTicks:30,holdTicks:30,returnTicks:30,repeatMode:TrapRepeatMode.Periodic,periodTicks:180,phaseTicks:110,cooldownTicks:90,movingKind:MovingTrapKind.Solid))));
            // Ride_2: home by S2_C (x 24-25.4), comes 1.2 u back to fetch the cat from Hop_2 over 40 ticks, waits 30, returns
            // over 40, every 180. A cat that steps off Hop_2 before it comes falls onto the spikes.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_2",(24.7f,10.15f),(1.4f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(-1.2f,0f),moveTicks:40,holdTicks:30,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:180,phaseTicks:120,cooldownTicks:110,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(30.5f,10.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_1",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,7.5f,10.3f,0f,0f,3f,sourceName:"Ground_1",destinationName:"Ground_2"),
                J("Pit5_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,16.05f,18.95f,0f,0f,3f,sourceName:"Ground_3",destinationName:"Ground_4"),
                J("Tread_R1",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23.9f,26f,0f,1.25f,3f,sourceName:"Ground_4",destinationName:"Tread_R1"),
                J("Tread_R2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27f,28.7f,1.25f,2.5f,1f,sourceName:"Tread_R1",destinationName:"Tread_R2"),
                J("Tread_R3",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,28.7f,26.8f,2.5f,3.75f,1f,sourceName:"Tread_R2",destinationName:"Tread_R3"),
                J("S1_B",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,26.5f,24.4f,3.75f,5f,1f,sourceName:"Tread_R3",destinationName:"S1_B"),
                J("Tread_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,5f,3f,5f,6.25f,1f,sourceName:"S1_A",destinationName:"Tread_A"),
                J("Tread_B",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,2.8f,.7f,6.25f,7.5f,1f,sourceName:"Tread_A",destinationName:"Tread_B"),
                J("Tread_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,.5f,2.6f,7.5f,8.75f,1f,sourceName:"Tread_B",destinationName:"Tread_C"),
                J("S2_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.3f,5.4f,8.75f,10f,1f,sourceName:"Tread_C",destinationName:"S2_A"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(1,8f,9.8f), L001Layout.Shaft(3,10.8f,12.6f), L001Layout.Shaft(5,16.55f,18.45f) },jumps);
        }

        // D-113: a 1 u deep trench in S2 with spikes at its bottom (honest): the spike bed.
        static void AddTrench(List<SoloRoomElement> elements, string id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.PitBottom,$"{id}_Bottom",(centre,8.75f),(width,.5f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"{id}_Hazard",(centre,9.15f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }
    }
}
