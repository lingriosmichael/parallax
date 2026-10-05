using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 2, look up. Two storeys: the ground (top 0) and S1 (a slab, top 5) under a roof at 10. The
    // door is on S1, right above the start; the way to it runs left along the ground, up the tower at the left wall,
    // and back right along S1. The blocks sit flush in the slab above the ground. The stair behind the start is the
    // obvious way up to the door, and a dead end; the high ledge on S1 is the other one.
    // PAX-102 (D-085 amendment; the developer: "If part of the floor falls, there should be a hole there"): each block is a
    // 1.6 u section of S1, so its fall leaves a real hole the cat hops on the way back right. Spikes_4 moved west to leave
    // a landing strip before Block_3's hole; the door's end of S1 moved 1.5 u east to leave room after Block_1's.
    // D-112 (the developer: "level two is too easy ... some sort of moving platform that requires precision jumping"): the
    // end of S1 is a ferry crossing. S1_A4 is a 1.2 u dock; the door starts on it and backs away across the gap to S1_B, a
    // 1 u ledge, as the cat hops Block_1's hole. Ferry (1.2 u, carries the cat) shuttles 1.25 u below the dock; the cat drops
    // onto it at home, rides it out and jumps up 1.25 u across 1.9 u to the door. Stone, a fake slab at dock height in the
    // gap, is the shortcut that isn't; Tread_3, the stair's top step where the ferry docks, is fake too.
    static class L002Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,10.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,9.5f),(1f,3f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,9.5f),(1f,3f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(24f,0f),(0f,0f)),
                // The ground, with two open pits (x 8.4-10 and 12-13.6) around the ledge, and a hidden shaft at the right wall.
                E(SoloRoomElementKind.Floor,"Ground_1",(4.2f,-2f),(8.4f,4f)),
                E(SoloRoomElementKind.Floor,"Ledge",(11f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2",(22.05f,-2f),(16.9f,4f)),
                // The tower up to S1 at the left wall.
                E(SoloRoomElementKind.Floor,"Tread_A",(3f,-1.375f),(2f,5.25f)),
                E(SoloRoomElementKind.Floor,"Tread_B",(.5f,-.75f),(1f,6.5f)),
                E(SoloRoomElementKind.Floor,"Tread_C",(3f,3.5f),(2f,.5f)),
                // S1: a slab from the tower to x 28, broken at x 23.8-25.4 by a section that isn't there. PAX-102: split around
                // the three blocks (x 10.4-12, 15.8-17.4, 20.2-21.8), drawn as one surface until they fall.
                E(SoloRoomElementKind.Floor,"S1_A",(7.2f,4.5f),(6.4f,1f)),
                E(SoloRoomElementKind.Floor,"S1_A2",(13.9f,4.5f),(3.8f,1f)),
                E(SoloRoomElementKind.Floor,"S1_A3",(18.8f,4.5f),(2.8f,1f)),
                E(SoloRoomElementKind.Floor,"S1_A4",(22.4f,4.5f),(1.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(30f,4.5f),(1f,1f)),
                // The high ledge on S1, whose end gives way. Step_1 up 0.25 u (the developer: "move slightly higher like maybe 20%").
                E(SoloRoomElementKind.Floor,"Step_1",(14.25f,6.25f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Ledge_H",(17.25f,7.25f),(2.5f,.5f)),
                // The stair behind the start, up to S1's end.
                E(SoloRoomElementKind.Floor,"Tread_1",(28.75f,-1.375f),(1.5f,5.25f)),
            };
            AddPit(elements, "PitA", 12f, 13.6f);
            AddPit(elements, "PitB", 8.4f, 10f);
            L001Layout.AddShaft(elements, 9, 30.5f, 32f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_D",(31.25f,-1.5f),(1.5f,3f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Tread_2",(31.25f,2.25f),(1.5f,.5f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Tread_3",(28.75f,3.85f),(1.5f,.5f)));
            // T1: fires as the cat sets off; lands ahead of a cat that runs on (the one bait: touch the trigger, let it land).
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(21f,4.5f),(1.6f,1f),(22.5f,2f),(.5f,4f),new SoloRoomTrapSettings(delayTicks:7,unitsPerTick:.36f,travelDistance:4f)));
            // T2: set off where the hop over T1 lands, and lands on a cat that runs on (stop, let it land, hop it). Its own
            // trigger, right at it, so it can't be set off from afar and waited out (developer's play, after PAX-059a).
            // T3: on a cat that stops on the ledge between the pits.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(16.6f,4.5f),(1.6f,1f),(18.75f,2f),(1f,4f),new SoloRoomTrapSettings(delayTicks:9,unitsPerTick:.36f,travelDistance:4f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_3",(11.2f,4.5f),(1.6f,1f),(11.75f,2f),(.5f,4f),new SoloRoomTrapSettings(delayTicks:32,unitsPerTick:.36f,travelDistance:4f)));
            // T4: on S1, where it heads back right (PAX-102: set off as the cat comes up onto S1, so there's a strip to land on
            // between the spikes and Block_3's hole).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(7.75f,5.15f),(1f,.3f),(3.75f,6.875f),(.5f,6.25f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T5 (D-112): the door backs away from the dock across the gap as the cat comes to Block_1's hole (the trigger is the
            // end of S1_A3 before it); spikes rise on the ground under the gap and on Tread_1.
            elements.Add(E(SoloRoomElementKind.Door,"Door",(22.4f,5.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(19.9f,7.5f),(.6f,5f),settings:new SoloRoomTrapSettings(moveTicks:40,offset:new Vector2(7.6f,0f))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5",(25.25f,.15f),(5.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Retreat")));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5b",(28.75f,1.4f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Retreat")));
            // T6 (D-112): the ferry. Home x 24-25.2, out 2.4 u over 50 ticks, 8 there, back over 50, every 170 (62 at home).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry",(24.6f,3.5f),(1.2f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(2.4f,0f),moveTicks:50,holdTicks:8,returnTicks:50,repeatMode:TrapRepeatMode.Periodic,periodTicks:170,phaseTicks:0,cooldownTicks:108,movingKind:MovingTrapKind.Solid))));
            // T7 (D-112): the shortcut that isn't: a slab at dock height in the gap, clear of the ferry at home.
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Stone",(25.9f,4.75f),(1f,.5f)));
            // The high ledge's end gives way onto spikes on S1 below it. The spikes' trigger is the space over the ledge (its
            // top to the roof), so only a cat on the ledge sets it off, never one jumping from S1 under it; the ledge goes as
            // the spikes show (developer's play, after PAX-059a: it was a harmless drop that a jump from below set off).
            // PAX-102: on S1_A3 only, clear of Block_1's hole.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_6",(19.35f,5.15f),(1.7f,.3f),(20f,8.75f),(3f,2.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_2",(20f,7.25f),(3f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_6",delayTicks:1)));
            var jumps = new[] {
                J("PitA_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,14.1f,11.5f,0f,0f,3f,sourceName:"Ground_2",destinationName:"Ledge"),
                J("PitB_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,10.5f,7.9f,0f,0f,1f,sourceName:"Ledge",destinationName:"Ground_1"),
                J("Tread_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,5.4f,3.4f,0f,1.25f,3f,sourceName:"Ground_1",destinationName:"Tread_A"),
                J("Tread_B",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,2.5f,.5f,1.25f,2.5f,1f,sourceName:"Tread_A",destinationName:"Tread_B"),
                J("Tread_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,.5f,2.6f,2.5f,3.75f,1f,sourceName:"Tread_B",destinationName:"Tread_C"),
                J("S1_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,2.6f,4.6f,3.75f,5f,1f,sourceName:"Tread_C",destinationName:"S1_A"),
                J("Spikes_4",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,6.5f,9f,5f,5f,1f,.3f),
                // PAX-102: the holes the blocks leave in S1.
                J("S1_A2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.9f,12.5f,5f,5f,1f,sourceName:"S1_A",destinationName:"S1_A2"),
                J("S1_A3",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,15.5f,18.1f,5f,5f,1f,sourceName:"S1_A2",destinationName:"S1_A3"),
                J("S1_A4",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,19.7f,22.3f,5f,5f,1f,sourceName:"S1_A3",destinationName:"S1_A4"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(9,30.5f,32f) },jumps);
        }

        // An open pit in the ground: a visible hazard at its bottom (honest, not a betrayal).
        static void AddPit(List<SoloRoomElement> elements, string id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.PitBottom,$"{id}_Bottom",(centre,-3.5f),(width,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"{id}_Hazard",(centre,-2.85f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }
    }
}
