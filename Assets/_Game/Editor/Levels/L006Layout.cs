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
    // ground, under a low lid over its last stretch. PAX-107 round three: the left end is a climb; D-124: up the chimney, over the pillar, down the slot onto Lift_G, which carries the cat over the spikes. Leaving S1 by its right end is the
    // dead end: a cat that jumps or runs off lands on the lid (hidden spikes), one that steps off drops onto Floor_5.
    static class L006Layout
    {
        // D-119: Ride_7's place in its 160-tick rhythm.
        internal const int Ride7Phase = 0;
        // PAX-107 (D-124): Lift_G's wait, 2 s, from the cat's arrival to its rise.
        internal const int LiftDelay = 100;

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
                // PAX-107 (D-124): the ground's west 2.5 u is Lift_G's slot (below).
                E(SoloRoomElementKind.Floor,"Ground_1",(4.5f,-2f),(4f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_1W",(1.25f,-2.25f),(2.5f,3.5f)),
                // D-119 (the developer: "Level 6 is also extremely easy … a lot more moving platforms … floors that go from flat and
                // turn up 90 degrees and start moving and pushing the cat towards traps"): the ground under S1 is cut by Pit_7 (a
                // rider's) and Pit_8 (under the hinge floor).
                E(SoloRoomElementKind.Floor,"Ground_2a",(9.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2b",(18.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_3",(23.5f,-2f),(3f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_4",(30.75f,-2f),(2.5f,4f)),
                // PAX-107 (the developer: "Make the spikes and floor longer … an extra 5% to the left", then "1 u longer to the left",
                // then "another 3 units to the left"): the lid reaches x 17.5, under S1's end. Then (D-124: "this built too long I
                // wanted some space to trick the player into thinking they can go down that way … a little space at the length of a
                // cat"): it starts at x 20.75, a 1.25 u gap past S1's end (x 19.5) down onto Floor_5, which gives way.
                E(SoloRoomElementKind.Floor,"Lid",(26.375f,3.25f),(11.25f,.5f)),
                // PAX-107 round three (the developer's sketch, 2026-10-07: "an extension to that left side making it harder"): west of
                // S1, a shelf off the left wall (x 0-1.75, top 7), a plinth (x 1.5-4.25, y 3.5-5) with a pillar on its east end
                // (x 3.5-4.25, up to 8.25) whose east face is mossy above y 6, and a pillar hanging from the ceiling over S1's west end
                // (x 6.25-7, down to 7.25) whose west face is mossy below y 9.5: a 2 u chimney between the two mossy faces (D-110).
                // The post (and its sweep) is gone; spikes cover the ground under it all (x 2.5-6.5). No solution yet: the developer
                // designs the way through.
                E(SoloRoomElementKind.Floor,"Shelf",(.875f,6.5f),(1.75f,1f)),
                E(SoloRoomElementKind.Floor,"Plinth",(2.875f,4.25f),(2.75f,1.5f)),
                E(SoloRoomElementKind.Wall,"Pillar",(3.75f,6.625f),(.5f,3.25f)),
                E(SoloRoomElementKind.Wall,"Pillar_Base",(4.125f,5.5f),(.25f,1f)),
                E(SoloRoomElementKind.GripWall,"Pillar_Moss",(4.125f,7.125f),(.25f,2.25f)),
                E(SoloRoomElementKind.Wall,"Hang",(6.75f,8.625f),(.5f,2.75f)),
                E(SoloRoomElementKind.Wall,"Hang_Top",(6.375f,9.75f),(.25f,.5f)),
                E(SoloRoomElementKind.GripWall,"Hang_Moss",(6.375f,8.375f),(.25f,2.25f)),
                E(SoloRoomElementKind.Hazard,"Spikes_G",(4.5f,.15f),(4f,.3f)),
            };
            // PAX-107 (D-124; the developer: "the player has to wait a 2 seconds and the section without spikes automatically lifts
            // up and carries the cat over the spikes"): the ground between the wall and Spikes_G (x 0-2.5) is a lift. 2 s after a
            // cat comes onto it (its trigger is the space over it, under the plinth) it rises 1 u, then carries the cat 4 u east
            // over the spikes (an L: up first, then across, over 60 ticks), to x 4-6.5 by Floor_4, and stays there.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_G",(1.25f,-.25f),(2.5f,.5f),(1.25f,1.75f),(2.5f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,riseFirst:true),
                new SoloRoomTrapSettings(delayTicks:LiftDelay,offset:new Vector2(4f,1f),moveTicks:60,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // T1: spikes on S1 on a rhythm (honest). T2: a block in the roof over the spot a cat waits on for them; it
            // comes down about one cycle later.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(10.75f,5.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,repeatMode:TrapRepeatMode.Periodic,periodTicks:100,phaseTicks:16,cooldownTicks:50)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(12.75f,10.5f),(1f,1f),(13.75f,7.5f),(.5f,5f),new SoloRoomTrapSettings(delayTicks:80,unitsPerTick:.26f,travelDistance:5f)));
            // T4: the landing east of the spikes gives way under a cat that stops. T5: the next floor gives way under a cat
            // that runs on (and it is where a cat stepping off S1's end lands). T6 (PAX-100): the rider before the door, below.
            L001Layout.AddShaft(elements, 4, 6.5f, 8.5f);
            // PAX-107 (the developer: "the piece of the floor that falls here in the bottom floor, needs to fall faster"): Floor_4
            // goes 12 ticks after the cat first touches it (was 20); a cat running on from the lift is off it in about 9.
            // D-126 (the developer: "I am able to get down without going the official route … I need it to disappear much
            // faster"): 3 ticks. The cat jumps it from the lift.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_4",(7.5f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            // D-126 (the developer: "if you fall through we automatically invert your arrow direction, to force a death"): a hidden
            // inverter in the drop west of S1 (x 4.25-6.5, y 3.3-4.5), between the chimney and the ground. Every cat that drops
            // off S1's west end, or out of the chimney, passes through it below S1's top; a cat standing on S1 or riding Lift_G
            // (its jump tops out at y 3.16) never touches it. Pressing right then carries the cat left onto Spikes_G. S1_Lip, a
            // block under S1's west end (x 6.5-7.5, down to y 2.4), keeps a falling cat west of x 6.5 until it's nearly down, so one
            // that steers right anyway (holding left) still lands on Spikes_G' end: without it, the cat reached Floor_4 and rode up
            // Ground_2a's corner as Floor_4 went. The lift's riders pass under it (their tops at 1.56).
            elements.Add(E(SoloRoomElementKind.Wall,"S1_Lip",(7f,3.2f),(1f,1.6f)));
            elements.Add(E(SoloRoomElementKind.Inverter,"Invert_Drop",(5.375f,3.9f),(2.25f,1.2f),settings:new SoloRoomTrapSettings(
                new InverterSettings(disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            // D-119: Pit_7 (x 10.5-15, wider than a jump) and its rider, Ride_7 (Carry): it waits at the west side and carries the
            // cat 3 u east (out over 40 ticks, 20 there, back over 40, every 160). Then the hinge floor, Flip_8 (x 15-17.5, flush
            // over Pit_8): a cat past it (its trigger is the space over Ground_2b, x 17.6-19.5) sets it off; 8 ticks later it swings
            // up about its east end over 10 ticks (PAX-107; was 12) into a wall behind the cat (x 17-17.5, 2.5 tall), leaving Pit_8 open, and slides
            // 2 u east over 28 (PAX-107: 20 % faster, twice), pushing a cat that stopped on Ground_2b onto Floor_5. The way: off the rider's far end, over (or
            // across) the hinge floor, and straight on over Floor_5.
            L001Layout.AddShaft(elements, 7, 10.5f, 15f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_7",(11.25f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:160,phaseTicks:Ride7Phase,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            L001Layout.AddShaft(elements, 8, 15f, 17.5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Flip_8",(16.25f,-.25f),(2.5f,.5f),(18.55f,2f),(1.9f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(2f,0f),moveTicks:10,holdTicks:600,movingKind:MovingTrapKind.Solid))));   // PAX-107: 20 % faster, then 20 % faster again
            L001Layout.AddShaft(elements, 5, 19.5f, 22f);
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_5",(20.75f,-1.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // PAX-100 (D-106): T6 is a rider now. Pit_6 (x 25-29.5) is wider than a jump; Ride_6 (1.5 u, carries the cat) shuttles
            // 3 u across it on the rhythm (out over 60 ticks, 40 there, back over 60, every 200): wait for it at home, ride it,
            // step off onto the door's floor. A cat that walks on while it's away falls into the pit.
            L001Layout.AddShaft(elements, 6, 25f, 29.5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_6",(25.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:60,holdTicks:40,returnTicks:60,repeatMode:TrapRepeatMode.Periodic,periodTicks:200,phaseTicks:0,cooldownTicks:160,movingKind:MovingTrapKind.Solid))));
            // Dead end: the lid, "the way toward the door" off S1's end, has hidden spikes on top; a cat that jumps or runs
            // off S1's end lands on it. They show as the cat comes near S1's end (the trigger, x 17-19.5; the way never goes
            // there). D-124: a cat that steps off drops through the gap before the lid onto Floor_5, which gives way.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_L",(26.375f,3.65f),(11.25f,.3f),(18.25f,7.5f),(2.5f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_5",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,19.3f,22.2f,0f,0f,3f,sourceName:"Ground_2b",destinationName:"Ground_3"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(4,6.5f,8.5f), L001Layout.Shaft(7,10.5f,15f), L001Layout.Shaft(8,15f,17.5f), L001Layout.Shaft(5,19.5f,22f), L001Layout.Shaft(6,25f,29.5f) },jumps);
        }
    }
}
