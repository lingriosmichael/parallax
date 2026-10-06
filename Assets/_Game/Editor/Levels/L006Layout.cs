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
        // D-119: Ride_7's place in its 160-tick rhythm.
        internal const int Ride7Phase = 0;

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
                // D-119 (the developer: "Level 6 is also extremely easy … a lot more moving platforms … floors that go from flat and
                // turn up 90 degrees and start moving and pushing the cat towards traps"): the ground under S1 is cut by Pit_7 (a
                // rider's) and Pit_8 (under the hinge floor).
                E(SoloRoomElementKind.Floor,"Ground_2a",(9.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2b",(18.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_3",(23.5f,-2f),(3f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_4",(30.75f,-2f),(2.5f,4f)),
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
            // that runs on (and it is where a cat stepping off S1's end lands). T6 (PAX-100): the rider before the door, below.
            L001Layout.AddShaft(elements, 4, 6.5f, 8.5f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_4",(7.5f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            // D-119: Pit_7 (x 10.5-15, wider than a jump) and its rider, Ride_7 (Carry): it waits at the west side and carries the
            // cat 3 u east (out over 40 ticks, 20 there, back over 40, every 160). Then the hinge floor, Flip_8 (x 15-17.5, flush
            // over Pit_8): a cat past it (its trigger is the space over Ground_2b, x 17.6-19.5) sets it off; 8 ticks later it swings
            // up about its east end over 12 ticks into a wall behind the cat (x 17-17.5, 2.5 tall), leaving Pit_8 open, and slides
            // 2 u east over 40, pushing a cat that stopped on Ground_2b onto Floor_5. The way: off the rider's far end, over (or
            // across) the hinge floor, and straight on over Floor_5.
            L001Layout.AddShaft(elements, 7, 10.5f, 15f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_7",(11.25f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:160,phaseTicks:Ride7Phase,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            L001Layout.AddShaft(elements, 8, 15f, 17.5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Flip_8",(16.25f,-.25f),(2.5f,.5f),(18.55f,2f),(1.9f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:12,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(2f,0f),moveTicks:40,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            L001Layout.AddShaft(elements, 5, 19.5f, 22f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_5",(20.75f,-1.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // PAX-100 (D-106): T6 is a rider now. Pit_6 (x 25-29.5) is wider than a jump; Ride_6 (1.5 u, carries the cat) shuttles
            // 3 u across it on the rhythm (out over 60 ticks, 40 there, back over 60, every 200): wait for it at home, ride it,
            // step off onto the door's floor. A cat that walks on while it's away falls into the pit.
            L001Layout.AddShaft(elements, 6, 25f, 29.5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_6",(25.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:60,holdTicks:40,returnTicks:60,repeatMode:TrapRepeatMode.Periodic,periodTicks:200,phaseTicks:0,cooldownTicks:160,movingKind:MovingTrapKind.Solid))));
            // Dead end: the lid, "the way toward the door" off S1's end, has hidden spikes on top; a cat that jumps or runs
            // off S1's end lands on it. They show as the cat comes to S1's end (the way never goes there). A cat that steps off
            // drops onto Floor_5 instead.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_L",(27f,3.65f),(10f,.3f),(19f,7.5f),(1f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_5",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,19.3f,22.2f,0f,0f,3f,sourceName:"Ground_2b",destinationName:"Ground_3"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(4,6.5f,8.5f), L001Layout.Shaft(7,10.5f,15f), L001Layout.Shaft(8,15f,17.5f), L001Layout.Shaft(5,19.5f,22f), L001Layout.Shaft(6,25f,29.5f) },jumps);
        }
    }
}
