using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 7, trust nothing: the safe step is the trap. A tall climb, bottom right to top left. Three
    // storeys: the ground (top 0), S1 (a slab, top 5, x 5-27.5) and S2 (top 10, x 0-27.5) under a roof at 14. The ground
    // runs left to the left tower (treads over a pit), S1 back right to the right tower (a zigzag at the wall), and S2 left
    // to the door. S2's end section is chained to hidden spikes on S1 under it.
    static class L007Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(30f,0f),(0f,0f)),
                // The ground, with two shafts near the start and the left tower's pit.
                E(SoloRoomElementKind.Floor,"Ground_B",(13.5f,-2f),(17f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_M",(24.3f,-2f),(1f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_A",(29.4f,-2f),(5.2f,4f)),
                // The left tower, from the ground to S1, over the pit.
                E(SoloRoomElementKind.Floor,"Tread_LA",(3.25f,1f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_LB",(.75f,2.25f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_LC",(3.25f,3.5f),(1.5f,.5f)),
                // S1, from the left tower to the right tower, in sections.
                E(SoloRoomElementKind.Floor,"S1_A",(7.25f,4.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(15.4f,4.5f),(8.2f,1f)),
                // A low overhang over S1: a cat walks under it, a cat that jumps meets its underside.
                E(SoloRoomElementKind.Ceiling,"Overhang",(15.75f,7.1f),(3.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_D",(24.5f,4.5f),(6f,1f)),
                // The right tower, from S1 to S2: L3's left tower mirrored, then wall steps on up.
                E(SoloRoomElementKind.Floor,"Tread_RA",(28.95f,6f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_RB",(31.25f,7.25f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_RC",(28.95f,8.5f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_RD",(31.5f,10f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"Tread_RE",(28.95f,11.25f),(1.5f,.5f)),
                // S2, from the door to x 25.3; its end section (x 25.3-27.5) gives way.
                E(SoloRoomElementKind.Floor,"S2_Main",(12.65f,9.5f),(25.3f,1f)),
            };
            L001Layout.AddShaft(elements, 0, 0f, 5f);
            // T1: the floor one step ahead drops as the cat comes up to it (its trigger is the spikes in the shaft).
            L001Layout.AddShaft(elements, 1, 24.8f, 26.8f);
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(25.8f,-2.55f),(2f,.3f),(27.75f,7f),(.5f,14f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_1",(25.8f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_1",delayTicks:1)));
            // T2: the floor where T1's jump lands gives way under a cat that stops.
            L001Layout.AddShaft(elements, 2, 22f, 23.8f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_2",(22.9f,-1.5f),(1.8f,3f),settings:new SoloRoomTrapSettings(delayTicks:18)));
            // T4: an S1 section gives way under a cat that walks onto it (L3 taught jumping it).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"S1_T4",(10.4f,4.5f),(1.8f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(12.5f,.15f),(6f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"S1_T4")));
            // T5: the stretch after T4 is real, but spikes show under the overhang as the cat comes to it: a cat that jumps
            // it, as T4 taught, meets them; a cat that walks passes under.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5",(15.75f,6.45f),(3.5f,.3f),(15.4f,7f),(4.2f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T6 (T1, escalated): the floor ahead drops again, and spikes come up where the jump T1 taught lands, then go
            // back down: wait, then jump.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_6",(20.5f,.15f),(2f,.3f),(19.75f,7f),(3.5f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"S1_T6",(20.5f,4.5f),(2f,1f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_6",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_6b",(22.75f,5.15f),(2.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_6",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:60)));
            // T3 (L3, reversed): from Tread_RC, S2's end is the step L3 taught; it gives way onto spikes on S1. The wall
            // step (Tread_RD) is real.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(24.75f,5.15f),(5.5f,.3f),(26.4f,12f),(2.2f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"S2_End",(26.4f,9.5f),(2.2f,1f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_3",delayTicks:1)));
            // Dead end: the tower's rhythm goes on past S2; that tread gives way onto spikes on Tread_RD.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_F",(31.5f,10.4f),(1f,.3f),(31.5f,13.375f),(1f,1.25f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_RF",(31.5f,12.5f),(1f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_F",delayTicks:1)));
            // Dead end: the left tower's rhythm goes on at the wall, up toward the door; that tread gives way onto spikes on
            // Tread_LB. The trigger is the space over it (its top to S2's underside).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(.5f,2.65f),(1f,.3f),(.5f,7f),(1f,4f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tread_LD",(.5f,4.75f),(1f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(1.5f,10.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_1",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,27.1f,24.3f,0f,0f,3f,sourceName:"Ground_A",destinationName:"Ground_M"),
                J("Tread_LA",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,5.4f,3.6f,0f,1.25f,3f,sourceName:"Ground_B",destinationName:"Tread_LA"),
                J("Tread_LB",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,2.7f,1f,1.25f,2.5f,1f,sourceName:"Tread_LA",destinationName:"Tread_LB"),
                J("Tread_LC",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,1f,3f,2.5f,3.75f,1f,sourceName:"Tread_LB",destinationName:"Tread_LC"),
                J("S1_A",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.8f,5.6f,3.75f,5f,1f,sourceName:"Tread_LC",destinationName:"S1_A"),
                J("S1_T4",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.1f,11.8f,5f,5f,3f,sourceName:"S1_A",destinationName:"S1_B"),
                J("S1_T6",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,19.1f,21.9f,5f,5f,3f,sourceName:"S1_B",destinationName:"S1_D"),
                J("Tread_RA",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27f,29f,5f,6.25f,1f,sourceName:"S1_D",destinationName:"Tread_RA"),
                J("Tread_RB",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,29.2f,31.3f,6.25f,7.5f,1f,sourceName:"Tread_RA",destinationName:"Tread_RB"),
                J("Tread_RC",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,31.5f,29.4f,7.5f,8.75f,1f,sourceName:"Tread_RB",destinationName:"Tread_RC"),
                J("Tread_RE",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,31.2f,29.5f,10.25f,11.5f,1f,sourceName:"Tread_RD",destinationName:"Tread_RE"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(0,0f,5f), L001Layout.Shaft(1,24.8f,26.8f), L001Layout.Shaft(2,22f,23.8f) },jumps);
        }
    }
}
