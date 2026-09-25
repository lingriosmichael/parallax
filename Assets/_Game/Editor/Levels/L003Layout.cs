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
    static class L003Layout
    {
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
                E(SoloRoomElementKind.Floor,"Ground_3",(22.3f,-2f),(19.4f,4f)),
                // The right tower, up to S1.
                E(SoloRoomElementKind.Floor,"Tread_R1",(26.5f,-1.375f),(2f,5.25f)),
                E(SoloRoomElementKind.Floor,"Tread_R2",(30.1f,-.75f),(3.8f,6.5f)),
                E(SoloRoomElementKind.Floor,"Tread_R3",(26.75f,3.5f),(1.5f,.5f)),
                // S1, from the left tower to x 25.3, with a section that gives way.
                E(SoloRoomElementKind.Floor,"S1_A",(9.8f,4.5f),(10.6f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(21.1f,4.5f),(8.4f,1f)),
                // The left tower, up to S2.
                E(SoloRoomElementKind.Floor,"Tread_A",(3.05f,6f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_B",(.75f,7.25f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_C",(3.05f,8.5f),(1.5f,.5f)),
                // S2, from the left tower to the right wall.
                E(SoloRoomElementKind.Floor,"S2",(18.25f,9.5f),(27.5f,1f)),
            };
            // T1: a floor that isn't there. T2: the floor where T1's jump lands gives way under a cat that stops.
            L001Layout.AddShaft(elements, 1, 8f, 9.8f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_1",(8.9f,-1.5f),(1.8f,3f)));
            L001Layout.AddShaft(elements, 3, 10.8f, 12.6f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_3",(11.7f,-1.5f),(1.8f,3f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            // Dead end: the right tower's rhythm goes on past S1; that tread gives way onto spikes on the tread below. The
            // spikes' trigger is the space over the tread (its top to S2's underside), so only a cat on the tread sets it off,
            // never one jumping under it from Tread_R2; the tread gives way as the spikes show. It sits right of the Tread_R2 to
            // Tread_R3 take-off (x 29.5-31), so that jump doesn't hit its underside.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_R",(30.75f,2.65f),(2.5f,.3f),(30.25f,7f),(1.5f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_R4",(30.25f,4.75f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_R",delayTicks:1)));
            // T3: a section of S1 gives way and drops the cat back to the ground (it walks back round).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"S1_Mid",(16f,4.5f),(1.8f,1f),settings:new SoloRoomTrapSettings(delayTicks:6)));
            // T4: the left tower's rhythm goes on to the wall at the top; that tread gives way onto spikes on the tread below.
            // As with Tread_R4, the trigger is the space over the tread (its top to the roof), out of reach of a jump from below.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_B",(.5f,7.65f),(1f,.3f),(.5f,12.125f),(1f,3.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_D",(.5f,10f),(1f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_B",delayTicks:1)));
            // T5: the bait: a block flush in the roof over S2.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_5",(20f,14.5f),(1f,1f),(17.25f,12f),(.5f,4f),new SoloRoomTrapSettings(delayTicks:16,unitsPerTick:.36f,travelDistance:4f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(30.5f,10.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_1",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,7.5f,10.3f,0f,0f,3f,sourceName:"Ground_1",destinationName:"Ground_2"),
                J("Tread_R1",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23.9f,26f,0f,1.25f,3f,sourceName:"Ground_3",destinationName:"Tread_R1"),
                J("Tread_R2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27f,28.7f,1.25f,2.5f,1f,sourceName:"Tread_R1",destinationName:"Tread_R2"),
                J("Tread_R3",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,28.7f,26.8f,2.5f,3.75f,1f,sourceName:"Tread_R2",destinationName:"Tread_R3"),
                J("S1_B",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,26.5f,24.4f,3.75f,5f,1f,sourceName:"Tread_R3",destinationName:"S1_B"),
                J("S1_Mid",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,17.4f,14.6f,5f,5f,3f,sourceName:"S1_B",destinationName:"S1_A"),
                J("Tread_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,5f,3f,5f,6.25f,1f,sourceName:"S1_A",destinationName:"Tread_A"),
                J("Tread_B",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,2.8f,.7f,6.25f,7.5f,1f,sourceName:"Tread_A",destinationName:"Tread_B"),
                J("Tread_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,.5f,2.6f,7.5f,8.75f,1f,sourceName:"Tread_B",destinationName:"Tread_C"),
                J("S2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.3f,5.4f,8.75f,10f,1f,sourceName:"Tread_C",destinationName:"S2"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(1,8f,9.8f), L001Layout.Shaft(3,10.8f,12.6f) },jumps);
        }
    }
}
