using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): L004 is SoloRoomsLayout.BuildFinalRoom (SoloRoomsLayout.Rooms[3]),
    // converted to a standalone level's local space: room id 0 (was 3), origin x 0 (was 135).
    // Element positions are local offsets from Origin already, so they are unchanged.
    static class L004Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_3",(2f,0f),(0f,0f))
            };
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2f,-.5f),(4f,1f)));
            // PAX-082 (D-082): refit to the lower jump (apex 1.6): two 0.5 steps over 1.5 gaps, a 3.0 drop onto FalseLanding,
            // Flip_A low and wide enough that every hop clearing SourceSpikes flips, Flip_B within reach from the ceiling.
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(7.5f,0f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_C",(14f,.5f),(6f,1f)));
            AddPit(elements,1,4f,22f);
            // PAX-080: claimed as a false landing, but no route is betrayed by it (PAX-075: a full-speed take-off from
            // x >= 15.28 skips it; the solution's braked landing stays below its trigger). Its trigger doesn't cut the
            // band, so it is LevelLayoutValidator.SurfaceCoverageExemptions' one entry. No layout change (D-069, PAX-078 R15).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"FalseLanding",(20.5f,-.5f),(3f,1f),(20.5f,3.5f),(1f,3f),new SoloRoomTrapSettings(offset:new Vector2(-3f,0f),moveTicks:20,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_D",(27f,-.5f),(10f,1f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"SourceSpikes",(24f,.15f),(1f,.3f),(20.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // PAX-078 (D-076): retimed for 50 Hz (Block_1 delay 37 -> 28, Block_2 25 -> 4) and Flip_A moved
            // x 26.5 -> 25.0, out of Block_1's column, so the full-speed flip is survivable.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(26.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:28,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"SourceSpikes")));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(29f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:4,unitsPerTick:.36f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(24.5f,1.8f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"CeilingHiddenSpikes",(28.75f,6.85f),(1f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:8,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_B",(31.5f,4.5f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.5f,6f,0f,.5f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9f,11.5f,.5f,1f,3f,sourceName:"Platform_B",destinationName:"Platform_C"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,16.5f,19.5f,1f,0f,3f,sourceName:"Platform_C",destinationName:"FalseLanding"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,22.5f,0f,0f,1f,sourceName:"FalseLanding",destinationName:"Floor_D"),
                J("SourceSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23f,25f,0f,0f,2f,.3f),
                J("CeilingHiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,27.75f,29.75f,0f,0f,2f,.3f)
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,4f,22f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard") },jumps);
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
