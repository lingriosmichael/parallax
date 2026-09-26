using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 6, rhythm. The start is on S1 (top 5), the door on the ground at the right, in view past
    // S1's right end; the way runs left along S1 (count the spikes, then go), down at its left end, and right along the
    // ground (stand clear of the sweep, then go), under a low lid over its last stretch. Leaving S1 by its right end is the
    // dead end: a cat that jumps or runs off lands on the lid (hidden spikes), one that steps off drops onto Floor_5.
    static class L006Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,10.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,5f),(1f,10f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,5f),(1f,10f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(14.9f,5f),(0f,0f)),
                // S1, from the drop by the wall to x 19.5.
                E(SoloRoomElementKind.Floor,"S1_A",(13f,4.5f),(13f,1f)),
                // The ground, with three pits and a post the sweep lives in; a low lid roofs its last stretch.
                E(SoloRoomElementKind.Floor,"Ground_1",(3.25f,-2f),(6.5f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2",(14f,-2f),(11f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_3",(23.5f,-2f),(3f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_4",(29.75f,-2f),(4.5f,4f)),
                E(SoloRoomElementKind.Wall,"Post",(5.5f,.5f),(1f,1f)),
                E(SoloRoomElementKind.Floor,"Lid",(27f,3.25f),(10f,.5f)),
            };
            // T1: spikes on S1 on a rhythm (honest). T2: a block in the roof over the spot a cat waits on for them; it
            // comes down about one cycle later.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(10.75f,5.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:16,cooldownTicks:50)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(12.75f,10.5f),(1f,1f),(13.75f,7.5f),(.5f,5f),new SoloRoomTrapSettings(delayTicks:80,unitsPerTick:.26f,travelDistance:5f)));
            // T3: a sweep out of the post toward the wall, set off by coming down by the wall; it stops 3.5 u short of the wall,
            // so a cat that stands where it landed is safe.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sweep_3",(5.5f,.4f),(1f,.8f),(3f,5f),(6f,10f),new SoloRoomTrapSettings(offset:new Vector2(-1.5f,0f),moveTicks:24,holdTicks:100,returnTicks:24,movingKind:MovingTrapKind.Hazard)));
            // T4: the landing past the post gives way under a cat that stops. T5: the next floor gives way under a cat
            // that runs on (and it is where a cat stepping off S1's end lands). T6: the floor before the door isn't there.
            L001Layout.AddShaft(elements, 4, 6.5f, 8.5f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_4",(7.5f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            L001Layout.AddShaft(elements, 5, 19.5f, 22f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_5",(20.75f,-1.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            L001Layout.AddShaft(elements, 6, 25f, 27.5f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_6",(26.25f,-1.5f),(2.5f,3f)));
            // Dead end: the lid, "the way toward the door" off S1's end, has hidden spikes on top; a cat that jumps or runs
            // off S1's end lands on it. They show as the cat comes to S1's end (the way never goes there). A cat that steps off
            // drops onto Floor_5 instead.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_L",(27f,3.65f),(10f,.3f),(19f,7.5f),(1f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_5",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,19.3f,22.2f,0f,0f,3f,sourceName:"Ground_2",destinationName:"Ground_3"),
                J("Floor_6",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,24.8f,27.7f,0f,0f,3f,sourceName:"Ground_3",destinationName:"Ground_4"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(4,6.5f,8.5f), L001Layout.Shaft(5,19.5f,22f), L001Layout.Shaft(6,25f,27.5f) },jumps);
        }
    }
}
