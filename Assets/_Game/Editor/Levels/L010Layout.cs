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
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,8f),(1f,18f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(18f,5f),(0f,0f)),
                // The roof: an alcove (x 2.5-4, a step up for a cat upside down), a stub (an arrow's wall), and a section that
                // gives way (x 19-21.5) with the recess behind it.
                E(SoloRoomElementKind.Ceiling,"Roof_A",(1.25f,14.5f),(2.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Alcove_Top",(3.25f,15.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_B",(10.75f,14.5f),(13.5f,1f)),
                // PAX-102: the roof over the new east end is 2 u higher, so a cat upside down under it can't reach the door below.
                E(SoloRoomElementKind.Ceiling,"Roof_C",(24.75f,16.5f),(14.5f,1f)),
                E(SoloRoomElementKind.Wall,"Pillar_L",(.25f,13f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Stub",(9.25f,13.75f),(.5f,.5f)),
                // S1, with a section that gives way (x 11.5-13.5), over the ground (top 1); the shelf over it left of the
                // start; the slab over the floating flip.
                // PAX-100 (D-106): S1_A in two, with Drop_10 (below) between them over a pit cut in the ground.
                E(SoloRoomElementKind.Floor,"S1_A",(1.5f,4.5f),(3f,1f)),
                E(SoloRoomElementKind.Floor,"S1_A2",(8.75f,4.5f),(5.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(22.75f,4.5f),(18.5f,1f)),
                E(SoloRoomElementKind.Floor,"Ground_L",(1.5f,-1.5f),(3f,5f)),
                E(SoloRoomElementKind.Floor,"Ground_R",(19f,-1.5f),(26f,5f)),
                // The pit under Drop_10 (x 3-6): its kill strip across the shaft (y -1 to -0.7) and its bottom deeper (y -4 to -3).
                E(SoloRoomElementKind.PitBottom,"Pit10_Bottom",(4.5f,-3.5f),(3f,1f)),
                E(SoloRoomElementKind.Hazard,"Pit10_Hazard",(4.5f,-.85f),(3f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                // The ground looks whole over the pit (no tells, D-085): a fake floor fills it from below the strip up to the ground's
                // top, so its box hides the strip; Drop_10 and a falling cat pass through it.
                E(SoloRoomElementKind.FakePlatform,"Pit10_Cover",(4.5f,0f),(3f,2f)),
                // PAX-102 (D-085 amendment): the shelf split around Block_1 (x 14.2-15.8), so its fall leaves a real hole.
                E(SoloRoomElementKind.Floor,"Shelf",(13.35f,9.5f),(1.7f,1f)),
                E(SoloRoomElementKind.Floor,"Shelf_E",(16.3f,9.5f),(1f,1f)),
                E(SoloRoomElementKind.Floor,"Slab_D",(4.4f,10.25f),(5.2f,.5f)),
                // The stair right of the start, "straight up to the door".
                E(SoloRoomElementKind.Floor,"Step_A",(20.25f,6f),(1.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Step_B",(22.75f,7.25f),(1.5f,.5f)),
                // PAX-102: where the room's east wall stood (x 24); the stair's steps still end against it.
                E(SoloRoomElementKind.Wall,"Stair_Wall",(24.25f,7f),(.5f,4f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // T1 (L2): a block in the shelf's underside, set off as the cat sets off; it lands on a cat that runs on. T5 (L8,
            // the chain): its landing brings up spikes on the roof, in view, where the way comes back; they stay up.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(15f,9.5f),(1.6f,1f),(14.65f,9.5f),(3.7f,9f),new SoloRoomTrapSettings(delayTicks:4,unitsPerTick:.36f,travelDistance:4f)));
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
            // PAX-102 (the developer: "add an inverter before you reach the door, I don't want the cat to reach a solution while
            // being in the ceiling"; "more moving platforms"): the room runs on east to x 32. A flip floating under the roof's
            // east end (x 16.5-17.5, from the roof down to y 11.75) turns a cat walking the roof upside down back down: it drops
            // onto Ledge_E1. A gap wider than a jump (x 21-25.5, a kill strip across its bottom) is crossed only on Ride_E; where
            // it sets the cat down, Sink_E sinks into the strip under a cat that stops; the door stands on Ledge_E3. The door's
            // top (13.25) is out of reach of a cat upside down under Roof_C (its jump reaches down to 13.84).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_E",(17f,12.875f),(1f,2.25f),settings:flip));
            elements.Add(E(SoloRoomElementKind.Floor,"Ledge_E1",(19.5f,11.5f),(3f,.5f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Ledge_E3",(29.75f,11.5f),(4.5f,.5f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"GapE_Hazard",(24.25f,10.9f),(6.5f,.3f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_E",(21.75f,11.5f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sink_E",(26.5f,11.5f),(2f,.5f),(26.5f,12.03f),(2f,.56f),new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(0f,-1f),moveTicks:30,holdTicks:40,returnTicks:30,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(30.5f,12.5f),(.6f,1.5f)));
            // Dead end (L4's lure): a flip floating over S1 sends a cat that jumps into it onto spikes under the slab above.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D2",(5.5f,7.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(4.025f,9.85f),(4.45f,.3f),(5.5f,7.5f),(1.5f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Dead end, "straight up to the door" (L3, L7): the stair's third step gives way onto spikes on the first. The
            // trigger is the space over it (its top to the recess's top).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(20.25f,6.4f),(1.5f,.3f),(20.25f,12.375f),(1.5f,7.25f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Step_C",(20.25f,8.5f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D1",delayTicks:1)));
            // T7 (PAX-100, D-106): the floor before the floor flip sinks under a cat that stops on it. Drop_10 (S1, x 3-6) goes 40
            // ticks after a cat lands on it (its trigger is its top strip; a cat walking across is off it in about 33), 6.5 u down
            // at 0.1 u a tick, carrying a cat that stays (it rests on it) through the fake ground into the pit's kill strip; it
            // comes back up after a hold. Slow, so its first ticks stay in the camera's view (D-083).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Drop_10",(4.5f,4.5f),(3f,1f),(4.5f,5.28f),(3f,.56f),new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip),
                new SoloRoomTrapSettings(delayTicks:40,offset:new Vector2(0f,-6.5f),moveTicks:65,holdTicks:60,returnTicks:60,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:190,movingKind:MovingTrapKind.Solid))));
            var jumps = new[] {
                J("Spikes_5",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,11.9f,14.8f,0f,0f,3f,.3f),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,3f,6f,"Ground_L","Ground_R","Pit10_Bottom","Pit10_Hazard") },jumps);
        }
    }
}
