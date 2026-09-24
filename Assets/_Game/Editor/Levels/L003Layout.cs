using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): L003 is SoloRoomsLayout.BuildCeilingRoom (SoloRoomsLayout.Rooms[2]),
    // converted to a standalone level's local space: room id 0 (was 2), origin x 0 (was 90).
    // Element positions are local offsets from Origin already, so they are unchanged.
    static class L003Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_2",(2f,0f),(0f,0f))
            };
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2.25f,-.5f),(4.5f,1f)));
            // PAX-082 (D-082): refit to the lower jump (apex 1.6): a 1.0 step over a 1.25 gap, 3.25 down to Floor_Pre,
            // a 1.6 lure over Pit 2, and Flip_A where the jump over it peaks; Flip_B within reach of a drop from the ceiling.
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(8.125f,.5f),(4.75f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_Pre",(14.375f,-.5f),(3.25f,1f)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(16.8f,-.5f),(1.6f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_Safe",(24.8f,-.5f),(14.4f,1f)));
            AddPit(elements,1,4.5f,12.75f);
            AddPit(elements,2,16f,17.6f);
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(18.1f,2.3f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.Hazard,"FloorHazard",(22.75f,.15f),(5.5f,.3f),hazardRole:SoloRoomHazardRole.UnjumpableFloor));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"PeriodicUp",(25.5f,1f),(1f,1f),settings:new SoloRoomTrapSettings(unitsPerTick:.36f,travelDistance:5.5f,direction:FallingBlockDirection.Up,repeatMode:TrapRepeatMode.Periodic,cooldownTicks:48,periodTicks:96,phaseTicks:24)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"CeilingSpikes",(27.5f,6.85f),(1.5f,.3f),(24f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_B",(30.5f,4.5f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(26.9f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(29f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:12,moveTicks:20,offset:new Vector2(1f,0f),triggerSource:TrapTriggerSource.Chain,chainSource:"CeilingSpikes")));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"ExitSpikes",(29.75f,.15f),(1f,.3f),(30.5f,4.5f),(1f,2f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4f,6.25f,0f,1f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,10f,13.25f,1f,0f,3f,sourceName:"Platform_B",destinationName:"Floor_Pre"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,15.5f,18.1f,0f,0f,1f,sourceName:"Floor_Pre",destinationName:"Floor_Safe"),
                J("CeilingSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,26.25f,28.75f,0f,0f,2f,.3f),
                J("ExitSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,30.75f,28.75f,0f,0f,1f,.3f)
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,4.5f,12.75f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard"), O(SoloRoomOpeningKind.Pit,16f,17.6f,"Pit2_L","Pit2_R","Pit2_Bottom","Pit2_Hazard") },jumps);
        }

        static void AddPit(List<SoloRoomElement> elements, int id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_L",(minX - .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_R",(maxX + .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.PitBottom,$"Pit{id}_Bottom",(centre,-3.5f),(width,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"Pit{id}_Hazard",(centre,-2.85f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }
    }
}
