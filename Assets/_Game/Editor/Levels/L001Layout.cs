using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 1, left to right on a flat floor: the floor lies, and hesitating is the answer until it isn't.
    // The ground is solid from y -4 to 0, and a trap floor over a pit fills its whole shaft (no tells). Falling blocks sit
    // flush in the ceiling. The shelf (top 1.25) is the way over the hidden spikes; the high ledge above it is the dead end;
    // the last floor before the door is the memory check; the door backs away while the cat is on the shelf.
    static class L001Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,8.9f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(.6f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Ground_1",(2.5f,-2f),(5f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2",(17.3f,-2f),(21.4f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_3",(30.8f,-2f),(2.4f,4f)),
                E(SoloRoomElementKind.Floor,"Shelf",(21f,1f),(9f,.5f)),
                E(SoloRoomElementKind.Floor,"Ledge",(21f,2.35f),(3f,.5f)),
            };
            // T1: a floor that isn't there.
            AddShaft(elements, 1, 5f, 6.6f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_2",(5.8f,-1.5f),(1.6f,3f)));
            // T2: fires as the cat comes up to it (2 u short, past T1) and lands where a cat that runs on is: stop at once
            // and let it land in front. T3: chained from it, lands behind, on a cat that backs away from T2. Both are set off
            // only right at them, so they can't be set off from afar and waited out (developer's play, after PAX-059a).
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(13f,8.9f),(1f,1f),(10.5f,4.2f),(.5f,8.4f),new SoloRoomTrapSettings(unitsPerTick:.36f,travelDistance:8.4f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(8.4f,8.9f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:1,unitsPerTick:.36f,travelDistance:8.4f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            // T4: under the shelf, where the cat can't jump; its trigger spans the spikes (they can be met from either side).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(21.7f,.15f),(4.6f,.3f),(21f,.375f),(7f,.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Dead end: the high ledge over the shelf.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(21.75f,2.75f),(1.5f,.3f),(21f,5.5f),(3f,5.8f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T5: T1 again, the last floor before the door.
            AddShaft(elements, 5, 28f, 29.6f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_7",(28.8f,-1.5f),(1.6f,3f)));
            // The door backs away while the cat is on the shelf.
            elements.Add(E(SoloRoomElementKind.Door,"Door",(30.3f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(23.25f,4.2f),(.5f,8.4f),settings:new SoloRoomTrapSettings(moveTicks:24,offset:new Vector2(1.3f,0f))));
            var jumps = new[] {
                J("Floor_2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.4f,7f,0f,0f,3f,sourceName:"Ground_1",destinationName:"Ground_2"),
                J("Shelf",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,14.9f,17f,0f,1.25f,3f,sourceName:"Ground_2",destinationName:"Shelf"),
                J("Floor_7",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27.4f,30.1f,0f,0f,3f,sourceName:"Ground_2",destinationName:"Ground_3"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { Shaft(1,5f,6.6f), Shaft(5,28f,29.6f) },jumps);
        }

        // A pit shaft between two ground blocks: its bottom at y -4..-3 and the hazard on it. The ground blocks are its
        // walls. A trap floor over it fills it from y -3 to 0, hiding the hazard until it gives way.
        internal static void AddShaft(List<SoloRoomElement> elements, int id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.PitBottom,$"Pit{id}_Bottom",(centre,-3.5f),(width,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"Pit{id}_Hazard",(centre,-2.85f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }

        internal static SoloRoomOpening Shaft(int id, float minX, float maxX) =>
            O(SoloRoomOpeningKind.Pit,minX,maxX,"","",$"Pit{id}_Bottom",$"Pit{id}_Hazard");
    }
}
