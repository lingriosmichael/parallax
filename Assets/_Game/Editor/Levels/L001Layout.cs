using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): L001 is SoloRoomsLayout.BuildChainRoom (SoloRoomsLayout.Rooms[0]),
    // converted to a standalone level's local space: room id 0 (already was 0), origin x 0
    // (already was 0). Element positions are unchanged — they are local offsets from Origin
    // already, so a level's own Origin being (0,0) needs no translation of any element.
    static class L001Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f))
            };
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(3f,-.5f),(6f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(10f,.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_C",(16f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(19f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_D",(26f,-.5f),(12f,1f)));
            AddPit(elements,1,6f,14f);
            AddPit(elements,2,18f,20f);
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(25f,.15f),(1.5f,.3f),(20f,.5f),(1f,1f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_A",(28f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:78,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_A")));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(29f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(27f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:12,moveTicks:24,offset:new Vector2(2f,0f),triggerSource:TrapTriggerSource.Chain,chainSource:"Block_A")));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,5.5f,8.5f,0f,1f,3.5f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,11.5f,14.5f,1f,0f,3f,sourceName:"Platform_B",destinationName:"Floor_C"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,17.5f,20.5f,0f,0f,2f,sourceName:"Floor_C",destinationName:"Floor_D"),
                J("Spikes_A",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23.75f,26.25f,0f,0f,2f,.3f)
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,6f,14f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard"), O(SoloRoomOpeningKind.Pit,18f,20f,"Pit2_L","Pit2_R","Pit2_Bottom","Pit2_Hazard") },jumps);
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
