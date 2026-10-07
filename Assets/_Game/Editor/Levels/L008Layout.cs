using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 8, chains: one trigger changes the room further on, and the cat sees it happen. A descent,
    // top left to bottom right. Three storeys: S2 (top 10, x 2-21), S1 (top 5, x 4-32) and the ground (top 0), under a
    // roof at 14. The way runs right along S2, drops off its end onto S1, runs left along S1, drops off its left end into
    // the column by the wall, and runs right along the ground to the door. Every long chain links permanent changes only
    // (spikes that come up and stay up), and each one's result is in view before the cat reaches it (Architect, half B Q3).
    // PAX-107 (the developer: "Level 8 can be better. I need moving platforms especially those that turn up and push you against a
    // trap … a waterfall of spikes that appear then disappear like a waterfall … or perhaps a wall climbing obstacle"; the pick was
    // the waterfall shaft with a wall-climb finish): S2's lift and collapse are a hinge floor that stands up behind the cat
    // and pushes it on toward the drop; the drop to S1 is a shaft whose walls put out spikes in a wave from top to bottom;
    // S1 (back left) has spikes under the hinge floor's hole and a second hinge floor that pushes a cat that stops onto more
    // spikes; the ground's second pit is a sunken chimney under a wall, climbed out on two mossy faces to the door. No arrows.
    static class L008Layout
    {
        // PAX-107: the waterfall. A cat reaching S2_B (the hinge floor's trigger) sets it going: WaterfallOff ticks later its top
        // bands put out spikes for WaterfallOut ticks, each band below WaterfallStep ticks after the one above, and it goes
        // again WaterfallOff ticks after each top band goes in (every WaterfallOff + WaterfallOut). Locked to the hinge floor:
        // its wall pushes a cat off S2_B's end as the second wave comes (L008Routes T3).
        internal const int WaterfallOff = 42, WaterfallOut = 30, WaterfallStep = 10;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3.2f,10f),(0f,0f)),
                // S2, from the hole behind the start to the shaft (x 21), cut by the hinge floor (x 13-15.5).
                E(SoloRoomElementKind.Floor,"S2_A",(7.5f,9.5f),(11f,1f)),
                E(SoloRoomElementKind.Floor,"S2_B",(18.25f,9.5f),(5.5f,1f)),
                // The waterfall shaft (x 21-22.4), from S2_B's end down to S1: its walls stop 1 u above S1 so the cat walks out
                // under them, the west one under S2_B's end, the east one reaching above S2 so the drop can't be jumped over.
                E(SoloRoomElementKind.Wall,"Shaft_W",(20.75f,7.5f),(.5f,3f)),
                E(SoloRoomElementKind.Wall,"Shaft_E",(22.65f,9f),(.5f,6f)),
                // S1, from the column by the wall to the east wall: Block_6's section (x 11.2-12.8), spikes under S2's hinge hole
                // (on S1_B), and S1's hinge floor (x 16-18.5).
                E(SoloRoomElementKind.Floor,"S1_W",(7.6f,4.5f),(7.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(14.4f,4.5f),(3.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1",(25.25f,4.5f),(13.5f,1f)),
                // The ground, the wall between the column the way drops down and the shaft behind the start, Ride_A's pit (x 15-19.5),
                // and the chimney: a sunken floor (x 21.5-26, top -3) under a wall (x 23.5-24, from y -1.5 up to S1) that the way
                // passes under, and the climb out between the wall's mossy east face and Ground_E's (x 24-26).
                E(SoloRoomElementKind.Floor,"Ground",(8.5f,-2f),(13f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_M",(20.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Sump_Floor",(23.875f,-3.5f),(4.75f,1f)),
                E(SoloRoomElementKind.GripWall,"Sump_Wall",(23.75f,1.25f),(.5f,5.5f)),
                E(SoloRoomElementKind.GripWall,"Sump_Moss",(26.125f,-1.5f),(.25f,3f)),
                E(SoloRoomElementKind.Floor,"Ground_E",(29.125f,-2f),(5.75f,4f)),
                E(SoloRoomElementKind.Wall,"Shaft_Wall",(2.25f,4.5f),(.5f,9f)),
            };
            // T1: a block in the roof, set off as the cat sets off; it lands on a cat that runs on.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(7f,14.5f),(1f,1f),(5.25f,12f),(.5f,4f),new SoloRoomTrapSettings(delayTicks:9,unitsPerTick:.36f,travelDistance:4f)));
            // T2 (the chain): the block's landing sets off spikes further along S2; they come up a while later and stay up.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(10.75f,10.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:48,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            // T3: S2's hinge floor (x 13-15.5, hinged at its east end). A cat past it (its trigger is the space over S2_B from
            // x 16.5) sets it off; 8 ticks later it swings up over 10 ticks into a wall behind the cat and slides 5.5 u east over
            // 110 ticks, shoving a cat still on S2_B off its end into the shaft about 125 ticks after the trigger, as the
            // waterfall's second wave comes.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S2",(14.25f,9.5f),(2.5f,1f),(18.75f,12f),(4.5f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:10,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(5.5f,0f),moveTicks:110,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // The waterfall: three bands of spikes on each of the shaft's walls (y 6-9), pointing in. The top two (Fall_W0, Fall_E0)
            // run on the hinge floor's trigger (Continuous); each band below follows the band above (a chain). Out, a band's two
            // strips leave 0.8 u between them, less than the cat (1 u): a cat in a band that is out dies.
            for (int i = 0; i < 3; i++)
            {
                float y = 8.5f - i;
                var top = new SoloRoomTrapSettings(revealDelayTicks:WaterfallOff,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:WaterfallOut);
                var below = new SoloRoomTrapSettings(revealDelayTicks:WaterfallStep,triggerSource:TrapTriggerSource.Chain,chainSource:$"Fall_W{i - 1}",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:WaterfallOut);
                if (i == 0)
                {
                    elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Fall_W0",(21.15f,y),(.3f,1f),(18.75f,12f),(4.5f,4f),top));
                    elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Fall_E0",(22.25f,y),(.3f,1f),(18.75f,12f),(4.5f,4f),top));
                    continue;
                }
                elements.Add(E(SoloRoomElementKind.HiddenSpikes,$"Fall_W{i}",(21.15f,y),(.3f,1f),settings:below));
                elements.Add(E(SoloRoomElementKind.HiddenSpikes,$"Fall_E{i}",(22.25f,y),(.3f,1f),settings:below));
            }
            // T5: spikes on S1 under S2's hinge floor (x 13.5-15: a cat that falls through its 2.5 u hole lands on them). They come
            // up as the cat comes over them on S2 or enters S1 east of them (their trigger spans S1 to the roof from x 13 to the
            // shaft) and are jumped on the way left.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(14.25f,5.15f),(1.5f,.3f),(17f,9.5f),(8f,9f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T6: S1's hinge floor (x 16-18.5, hinged at its west end). A cat past it (over Spikes_4, x 13.1-14.9) sets it off; it swings up behind
            // the cat and slides 5.5 u west over 73 ticks, pushing a cat that stops onto Spikes_5 (x 8.5-10); jumped on the way.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S1",(17.25f,4.5f),(2.5f,1f),(14f,7f),(1.8f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:10,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(-5.5f,0f),moveTicks:73,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_5",(9.25f,5.15f),(1.5f,.3f)));
            // T7: a block in S1's underside over the ground, set off as the cat comes up to it (its trigger spans it, since the
            // ground can be reached from either side); it lands on a cat that runs on. PAX-102: 1.6 u wide, a section of S1, so
            // it leaves a real hole; its trigger stays on the ground storey.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_6",(12f,4.5f),(1.6f,1f),(11.4f,2f),(4.4f,4f),new SoloRoomTrapSettings(delayTicks:13,unitsPerTick:.36f,travelDistance:4f)));
            // Dead end: the hole behind the start, "the way down", opens into a shaft whose floor isn't there.
            L001Layout.AddShaft(elements, 9, 0f, 2f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_9",(1f,-1.5f),(2f,3f)));
            // Dead end: east along S1, "the way toward the door" it runs over: spikes rise on its end (x 30-32) as a cat comes past
            // x 28 (their trigger spans S1's top to the roof).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_E",(31f,5.15f),(2f,.3f),(28f,9.5f),(.5f,9f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            // PAX-102: the ground's first pit, crossed only on a rider (Carry, Periodic, every 150) that waits at its west side,
            // carries the cat 3 u east, holds, and comes back.
            L001Layout.AddShaft(elements, 10, 15f, 19.5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_A",(15.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            var jumps = new[] {
                J("Spikes_2",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.3f,12.2f,10f,10f,3f,.3f),
                J("Spikes_4",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,15.7f,12.8f,5f,5f,3f,.3f),
                J("Spikes_5",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,10.7f,7.8f,5f,5f,3f,.3f),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(9,0f,2f), L001Layout.Shaft(10,15f,19.5f) },jumps);
        }
    }
}
