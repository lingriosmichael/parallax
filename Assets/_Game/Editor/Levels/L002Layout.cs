using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): L002 is SoloRoomsLayout.BuildLiftRoom (SoloRoomsLayout.Rooms[1]),
    // converted to a standalone level's local space: room id 0 (was 1), origin x 0 (was 45).
    // Element positions are local offsets from Origin already, so they are unchanged.
    static class L002Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_1",(2f,0f),(0f,0f))
            };
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2.5f,-.5f),(5f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(8.5f,1f),(3f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_C",(14f,0f),(4f,1f)));
            AddPit(elements,1,5f,24f);
            // PAX-078 (D-076): trigger centre x 19.55 -> 19.80 for 12 ticks of landing slack at 50 Hz.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift",(18f,-.25f),(4f,.5f),(19.8f,3.375f),(.3f,7.25f),new SoloRoomTrapSettings(offset:new Vector2(0f,1.5f),moveTicks:36,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.Floor,"Receiver",(21f,.25f),(2f,2.5f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"ReceiverBlock",(20.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:50,unitsPerTick:.3f,travelDistance:4f,triggerSource:TrapTriggerSource.Chain,chainSource:"Lift")));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(23f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DLeft",(26f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DMiddle",(29.5f,-.5f),(3f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DRight",(31.5f,-.5f),(1f,1f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sweep",(30.5f,.45f),(1f,.3f),(26f,3.5f),(.5f,7f),new SoloRoomTrapSettings(offset:new Vector2(-2f,0f),moveTicks:40,holdTicks:12,returnTicks:40,movingKind:MovingTrapKind.Hazard,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:92)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(29.5f,.15f),(1f,.3f),(25.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // PAX-080: claimed as a betrayal, but no replayed route dies on it (PAX-075: take-offs 25.2-28.4, waits
            // 0-30, no brake); the Sweep kills the cat that runs on. No layout change (D-069, PAX-078 R15).
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_A",(30.5f,6f),(.75f,.75f),settings:new SoloRoomTrapSettings(delayTicks:26,unitsPerTick:.3f,travelDistance:5.625f,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_A")));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new List<RequiredJump> {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.5f,7.5f,0f,1.5f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.5f,12.5f,1.5f,.5f,2f,sourceName:"Platform_B",destinationName:"Floor_C"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,24.5f,1.5f,0f,1f,sourceName:"Receiver",destinationName:"Floor_DLeft"),
                J("Spikes_A",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27.5f,31.5f,0f,0f,3f,1f,sourceName:"Floor_DLeft",destinationName:"Floor_DRight")
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { O(SoloRoomOpeningKind.Pit,5f,24f,"Pit1_L","Pit1_R","Pit1_Bottom","Pit1_Hazard") },jumps.ToArray());
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
