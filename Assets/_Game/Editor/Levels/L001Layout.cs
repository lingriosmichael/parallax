using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 1, left to right on a flat floor: the floor lies, and hesitating is the answer until it isn't.
    // D-111 (the developer, 2026-10-04): the floor between the falling grip walls shrinks away, the shelf and ledge are 25%
    // higher and the ledge falls on a cat passing under it, and the last falling floor is 4 u long. Unsolvable for now.
    // The ground is solid from y -4 to 0, and a trap floor over a pit fills its whole shaft (no tells). The two falling
    // blocks hang from the ceiling as walls and land as a grip shaft (PAX-105, D-110 amendment 3). The shelf (top 1.30) is the way over the hidden spikes; the high ledge above it is the dead end;
    // the last floor before the door is the memory check; the door backs away while the cat is on the shelf.
    static class L001Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,8.9f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(.6f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Ground_1",(2.5f,-2f),(5f,4f)),
                // D-111 (the developer, 2026-10-04): the ground between the two walls is a shrinking floor over a pit, and the
                // falling floor before the door runs from x 25.6: Ground_2 now ends at Block_2's landing (8.9) and Ground_2b runs
                // from Block_1's landing (12.5) to 25.6.
                E(SoloRoomElementKind.Floor,"Ground_2",(7.75f,-2f),(2.3f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2b",(19.05f,-2f),(13.1f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_3",(30.8f,-2f),(2.4f,4f)),
                // The developer (2026-10-02, "the spacing is not good"): the shelf as high as band 1's reach allowed (top 1.30)
                // and the gap to the ledge 10% wider. D-111 (2026-10-04): both raised 25% (shelf top 1.30 -> 1.625, ledge top
                // 2.735 -> 3.419); the shelf is now out of a ground jump's reach and is reached from Block_1's top.
                E(SoloRoomElementKind.Floor,"Shelf",(21f,1.375f),(9f,.5f)),
            };
            // T1: a floor that isn't there.
            AddShaft(elements, 1, 5f, 6.6f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_2",(5.8f,-1.5f),(1.6f,3f)));
            // T2: fires as the cat comes up to it (2 u short, past T1) and lands where a cat that runs on is: stop at once
            // and let it land in front. T3: chained from it, lands behind, on a cat that backs away from T2. Both are set off
            // only right at them, so they can't be set off from afar and waited out (developer's play, after PAX-059a).
            // PAX-105 (D-110 amendment 3, the developer 2026-10-04): both are grip walls 4.5 u tall, hanging from the ceiling
            // (y 3.9-8.4) until they fall 3.9 to the floor (0.1675 u a tick: they land 23 ticks after setting off, as the 1 u
            // blocks' 8.4 u fall at 0.36 did); landed, their faces are 3.6 u apart and the cat between them climbs out wall to
            // wall (Grab, then latch mode) onto Block_1's top.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(13f,6.15f),(1f,4.5f),(10.5f,4.2f),(.5f,8.4f),
                new SoloRoomTrapSettings(GripSettings.On, new SoloRoomTrapSettings(unitsPerTick:.1675f,travelDistance:3.9f))));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(8.4f,6.15f),(1f,4.5f),settings:
                new SoloRoomTrapSettings(GripSettings.On, new SoloRoomTrapSettings(delayTicks:1,unitsPerTick:.1675f,travelDistance:3.9f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1"))));
            // D-111: the floor between the walls (x 8.9-12.5, the pit's full depth) shrinks from both sides to nothing, starting
            // 30 ticks after Block_2 sets off (just after both walls have landed), over 75 ticks: grab a wall or fall.
            // Built after Block_2: a chain source is looked up among the traps built before it.
            AddShaft(elements, 2, 8.9f, 12.5f);
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Squeeze",(10.7f,-1.5f),(3.6f,3f),settings:new SoloRoomTrapSettings(new ShrinkSettings(75,0f,ShrinkFrom.Both),
                new SoloRoomTrapSettings(delayTicks:30,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2"))));
            // T4: under the shelf, where the cat can't jump; its trigger spans the spikes (they can be met from either side).
            // D-111: the trigger fills the gap under the raised shelf (y 0-1.125), so the cat can't hop over it.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(21.7f,.15f),(4.6f,.3f),(21f,.5625f),(7f,1.125f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // D-111: the ledge over the shelf (x 19.5-22.5, y 2.919-3.419) falls onto a cat that goes through the gap between
            // them (its trigger is the column over the shelf, up to the ceiling): 1.294 u at 0.06 u a tick, onto the shelf. Its
            // dead-end spikes are gone.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Ledge",(21f,3.169f),(3f,.5f),(21f,5.0125f),(3f,6.775f),new SoloRoomTrapSettings(unitsPerTick:.06f,travelDistance:1.294f)));
            // T5: T1 again, the last floor before the door: D-111, from x 25.6 (the stone after the shelf) to the door's platform.
            AddShaft(elements, 5, 25.6f, 29.6f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_7",(27.6f,-1.5f),(4f,3f)));
            // The door backs away while the cat is on the shelf.
            elements.Add(E(SoloRoomElementKind.Door,"Door",(30.3f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(23.25f,4.2f),(.5f,8.4f),settings:new SoloRoomTrapSettings(moveTicks:24,offset:new Vector2(1.3f,0f))));
            var jumps = new[] {
                J("Floor_2",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.4f,7f,0f,0f,3f,sourceName:"Ground_1",destinationName:"Ground_2"),
                // D-111: no jump up to the shelf (it's reached from Block_1's top) and none over the falling floor (4 u, out of
                // reach: L001 is unsolvable until the developer's solution).
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { Shaft(1,5f,6.6f), Shaft(2,8.9f,12.5f), Shaft(5,25.6f,29.6f) },jumps);
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
