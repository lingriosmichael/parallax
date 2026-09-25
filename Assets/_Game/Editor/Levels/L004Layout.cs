using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 4, gravity. Two bands: the lower one from the ground (top 0) to a slab (underside 7, x 0-24),
    // and the upper one from the slab's top (8) to the roof (underside 15). The door hangs from the roof at the left. The
    // start is mid-room; the way runs right along the ground, up a flip through the gap past the slab's end to the roof,
    // back left upside down, down a flip to the slab's top, and up a flip under the door. Every roof section that gives way
    // fills the recess behind it, so the recess's hazard stays hidden until it does.
    static class L004Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,13.5f),(1f,11f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,13.5f),(1f,11f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(9.5f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Ground",(16f,-2f),(32f,4f)),
                E(SoloRoomElementKind.Floor,"Slab",(12f,7.5f),(24f,1f)),
                // The roof, broken by two sections that give way (x 3-6 and 16-19.5).
                E(SoloRoomElementKind.Ceiling,"Roof_L",(1.5f,15.5f),(3f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_M",(11f,15.5f),(10f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_R",(25.75f,15.5f),(12.5f,1f)),
                // The recesses behind them.
                E(SoloRoomElementKind.Wall,"Recess_A_L",(2.75f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_A_R",(6.25f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_A_Top",(4.5f,18.5f),(4f,1f)),
                E(SoloRoomElementKind.Wall,"Recess_B_L",(15.75f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_B_R",(19.75f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_B_Top",(17.75f,18.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Recess5_Hazard",(4.5f,17.85f),(3f,.3f)),
                E(SoloRoomElementKind.Hazard,"Recess4_Hazard",(17.75f,17.85f),(3.5f,.3f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // Dead end: the flip toward the door, onto spikes under the slab.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_L",(5.5f,1f),(1f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_L",(3f,6.85f),(6f,.3f),(5.5f,3.5f),(1f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T1: spikes on the ground. T2: the flip floating over the ground, onto spikes under the slab.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(15.75f,.15f),(1.5f,.3f),(13.25f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(19.25f,2.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(21.25f,6.85f),(5.5f,.3f),(19.25f,3.5f),(1.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // The way up: a flip on the ground under the gap past the slab.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_R",(30.5f,1f),(1f,2f),settings:flip));
            // T3: spikes on the roof; T4: the roof where the jump over them lands gives way under a cat that stops.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(21f,14.85f),(1f,.3f),(23.75f,11.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_4",(17.75f,16.5f),(3.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            // T5: the door backs away along the roof over a section that gives way. The way down: a flip floating under the
            // roof; the way back up: a flip on the slab's top under the door's new place.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D",(12.25f,12.75f),(1.5f,1.5f),settings:flip));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_E",(2.5f,9f),(1f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(6.8f,14.25f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(10.75f,11.5f),(.5f,7f),settings:new SoloRoomTrapSettings(moveTicks:30,offset:new Vector2(-4.3f,0f))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_5",(4.5f,16.5f),(3f,3f),settings:new SoloRoomTrapSettings()));
            var jumps = new[] {
                J("Spikes_1",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,14.3f,17f,0f,0f,3f,.3f),
                J("Spikes_3",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Left,22.4f,20f,0f,0f,3f,.3f),
                J("Roof_4",RequiredJumpKind.Pit,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Left,17.5f,15.4f,0f,0f,1f,sourceName:"Roof_4",destinationName:"Roof_M"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),System.Array.Empty<SoloRoomOpening>(),jumps);
        }
    }
}
