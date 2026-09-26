using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 10, the exam: every lesson once, in a new order, in one short loop. S1 (a slab, top 5) under
    // a roof at 14 that is walked upside down; a shelf hangs over S1 left of the start. The start is mid-S1 and the door
    // hangs from the roof right above it; the way runs left along S1, up the floor flip at its left end, and back right
    // along the roof to the door, which backs away once, over a roof section that gives way. The stair right of the start,
    // "straight up to the door", and the flip floating over S1 are the dead ends.
    static class L010Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(24.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(18f,5f),(0f,0f)),
                // The roof: an alcove (x 2.5-4, a step up for a cat upside down), a stub (an arrow's wall), and a section that
                // gives way (x 19-21.5) with the recess behind it.
                E(SoloRoomElementKind.Ceiling,"Roof_A",(1.25f,14.5f),(2.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Alcove_Top",(3.25f,15.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_B",(11.5f,14.5f),(15f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_C",(22.75f,14.5f),(2.5f,1f)),
                E(SoloRoomElementKind.Wall,"Recess_L",(18.75f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_R",(21.75f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_Top",(20.25f,17.5f),(3.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Recess6_Hazard",(20.25f,16.85f),(2.5f,.3f)),
                E(SoloRoomElementKind.Wall,"Pillar_L",(.25f,13f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Stub",(9.25f,13.75f),(.5f,.5f)),
                // S1, with a section that gives way (x 11.5-13.5), over the ground (top 1); the shelf over it left of the
                // start; the slab over the floating flip.
                E(SoloRoomElementKind.Floor,"S1_A",(5.75f,4.5f),(11.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(18.75f,4.5f),(10.5f,1f)),
                E(SoloRoomElementKind.Floor,"Ground",(12f,-1.5f),(24f,5f)),
                E(SoloRoomElementKind.Floor,"Shelf",(14.5f,9.5f),(4f,1f)),
                E(SoloRoomElementKind.Floor,"Slab_D",(4.4f,10.25f),(5.2f,.5f)),
                // The stair right of the start, "straight up to the door".
                E(SoloRoomElementKind.Floor,"Step_A",(20.25f,6f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Step_B",(22.75f,7.25f),(1.5f,.5f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // T1 (L2): a block in the shelf's underside, set off as the cat sets off; it lands on a cat that runs on. T5 (L8,
            // the chain): its landing brings up spikes on the roof, in view, where the way comes back; they stay up.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(15f,9.5f),(1f,1f),(14.65f,7f),(3.7f,4f),new SoloRoomTrapSettings(delayTicks:4,unitsPerTick:.36f,travelDistance:4f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5",(13.55f,13.85f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:30,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            // T2 (L3): the floor where the hop over the block lands gives way under a cat that stops, onto spikes below.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"S1_2",(12.5f,4.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(12.5f,1.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"S1_2")));
            // T3 (L6): spikes on S1 on a rhythm.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(8.25f,5.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:60,cooldownTicks:50)));
            // T4 (L9, L5): the floor flip at S1's left end is the way up; it fires an arrow out of the stub along the roof,
            // at the cat walking back upside down. The alcove is out of its lane.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_4",(1f,6f),(1f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_4",(9.25f,13.7f),(.5f,.4f),(1f,9.5f),(1f,9f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,13.7f,.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:150))));
            // T6 (L2, L1): as the cat comes along the roof, the door backs away once, over a roof section that gives way.
            elements.Add(E(SoloRoomElementKind.Door,"Door",(18f,13.25f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(14f,12f),(1f,4f),settings:new SoloRoomTrapSettings(moveTicks:30,offset:new Vector2(3.5f,0f))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_6",(20.25f,15.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:6)));
            // Dead end (L4's lure): a flip floating over S1 sends a cat that jumps into it onto spikes under the slab above.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D2",(5.5f,7.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(4.025f,9.85f),(4.45f,.3f),(5.5f,7.5f),(1.5f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Dead end, "straight up to the door" (L3, L7): the stair's third step gives way onto spikes on the first. The
            // trigger is the space over it (its top to the recess's top).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(20.25f,6.4f),(1.5f,.3f),(20.25f,12.875f),(1.5f,8.25f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Step_C",(20.25f,8.5f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D1",delayTicks:1)));
            var jumps = new[] {
                J("Spikes_5",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,11.9f,14.8f,0f,0f,3f,.3f),
                J("Roof_6",RequiredJumpKind.Pit,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,18.4f,21.3f,0f,0f,1f,sourceName:"Roof_B",destinationName:"Roof_C"),
            };
            return new SoloRoomDefinition(0,0f,24f,elements.ToArray(),System.Array.Empty<SoloRoomOpening>(),jumps);
        }
    }
}
