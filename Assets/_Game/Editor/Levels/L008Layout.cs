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
    // PAX-107 round three (the developer: "so much empty space here … a maze of sorts and traps, it should be an alternative route if
    // you dont manage to get down through the waterfall"; "the top section should be covered in moss so it allows you to jump over to
    // the right"; "more falling ceilings and floors that move up to squish the cat"): the shaft's top is a 2 u chimney between a
    // mossy column hanging over S2_B's end and Shaft_E's mossy upper face; climbed, it leads over Shaft_E into a three-tier maze
    // east of the shaft (a press, spikes on a rhythm, a piston floor) that comes down onto S1 short of Spikes_E. A press comes
    // down onto S1 and a piston rises out of the ground under it.
    static class L008Layout
    {
        // PAX-107: the waterfall. A cat reaching S2_B (the hinge floor's trigger) sets it going: WaterfallOff ticks later its top
        // bands put out spikes for WaterfallOut ticks, each band below WaterfallStep ticks after the one above, and it goes
        // again WaterfallOff ticks after each top band goes in (every WaterfallOff + WaterfallOut). Locked to the hinge floor:
        // its wall pushes a cat off S2_B's end as the second wave comes (L008Routes T3).
        internal const int WaterfallOff = 42, WaterfallOut = 30, WaterfallStep = 10;
        // PAX-107 round three: the crushers' rhythms.
        internal const int PressPeriod = 140, PistonPeriod = 140, PistonPhase = 80, MazePeriod = 120;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3.2f,10f),(0f,0f)),
                // S2, from the hole behind the start to the shaft (x 21), cut by the hinge floor (x 13-14.5).
                E(SoloRoomElementKind.Floor,"S2_A",(7.5f,9.5f),(11f,1f)),
                E(SoloRoomElementKind.Floor,"S2_B",(17.75f,9.5f),(6.5f,1f)),
                // The waterfall shaft (x 21-23), from S2_B's end down to S1: its walls stop 1 u above S1 so the cat walks out
                // under them, the west one under S2_B's end, the east one reaching above S2 so the drop can't be jumped over.
                // PAX-107 round three: 2 u wide (was 1.4), and its top is a chimney: a mossy column hangs from the roof over S2_B's
                // end (Column_Moss, x 20-21, down to y 11.5, where the hinge floor's wall stops under it) and Shaft_E's upper part
                // is mossy (Shaft_Moss, from y 9.25, over the waterfall, to its top at 12). Two wall jumps up it carry the cat over
                // Shaft_E onto the maze's top tier.
                E(SoloRoomElementKind.Wall,"Shaft_W",(20.75f,7.5f),(.5f,3f)),
                E(SoloRoomElementKind.Wall,"Shaft_E",(23.25f,7.625f),(.5f,3.25f)),
                E(SoloRoomElementKind.GripWall,"Shaft_Moss",(23.25f,10.625f),(.5f,2.75f)),
                E(SoloRoomElementKind.GripWall,"Column_Moss",(20.5f,12.75f),(1f,2.5f)),
                // PAX-107 round three: the maze east of the shaft, three tiers 2 u apart. Maze_1 (top 12) runs east from Shaft_E's
                // top under a press; Maze_2 (top 9.5) runs west from the east wall past spikes on a rhythm; Maze_3 (top 7) runs east
                // from Shaft_E's foot over a piston floor and ends at x 28.5, a drop onto S1 1.5 u short of Spikes_E.
                E(SoloRoomElementKind.Floor,"Maze_1",(26.5f,11.75f),(6f,.5f)),
                E(SoloRoomElementKind.Floor,"Maze_2",(28.5f,9.25f),(7f,.5f)),
                // PAX-107 (the developer: "even this out … when the wall hangs lower than the floor"): Maze_3 (and Piston_3 in it)
                // 1 u deep, its underside flush with Shaft_E's foot (y 6).
                E(SoloRoomElementKind.Floor,"Maze_3a",(24.5f,6.5f),(2f,1f)),
                E(SoloRoomElementKind.Floor,"Maze_3b",(27.75f,6.5f),(1.5f,1f)),
                // S1, from the column by the wall to the east wall: Block_6's section (x 11.2-12.8), spikes under S2's hinge hole
                // (on S1_B), and S1's hinge floor (x 16-18.5).
                E(SoloRoomElementKind.Floor,"S1_W",(7.6f,4.5f),(7.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(14.4f,4.5f),(3.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1",(25.25f,4.5f),(13.5f,1f)),
                // The ground, the wall between the column the way drops down and the shaft behind the start, Ride_A's pit (x 15-19.5),
                // and the chimney: a sunken floor (x 21.5-26, top -3) under a wall (x 23.5-24, from y -1.5 up to S1) that the way
                // passes under, and the climb out between the wall's mossy east face and Ground_E's (x 24-26).
                // PAX-107 round three: the ground under S1_W is cut by a notch (x 5-6.5, 0.5 deep) that Piston_G sits in.
                E(SoloRoomElementKind.Floor,"Ground",(3.5f,-2f),(3f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_N",(5.75f,-2.25f),(1.5f,3.5f)),
                E(SoloRoomElementKind.Floor,"Ground_B",(10.75f,-2f),(8.5f,4f)),
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
            // T3: S2's hinge floor (x 13-14.5, hinged at its east end; PAX-107 round three: 1.5 u, so its wall, 1.5 tall, stops
            // under Column_Moss). A cat past it (its trigger is the space over S2_B from x 16.5) sets it off; 8 ticks later it
            // swings up over 8 ticks into a wall behind the cat and slides 6.5 u east over 96 ticks (PAX-107: 20 % faster), shoving a cat still on S2_B
            // off its end into the shaft as the waterfall's second wave comes. PAX-107 (the developer: "This wall is moving too slow"): it
            // slides at the cat's run (6.5 u over 54 ticks, 0.12 u a tick) and stands up 38 ticks after the trigger (was 8), so it
            // reaches a cat waiting at the shaft when it did. PAX-107 (the developer: "when floor stand up and
            // starts moving right that its covered in spikes as well", D-124): its face toward the cat is spiked, so the wall kills
            // a cat it reaches instead of shoving it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S2",(13.75f,9.5f),(1.5f,1f),(18.75f,12f),(4.5f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:64,offset:new Vector2(6.5f,0f),moveTicks:33,holdTicks:600,movingKind:MovingTrapKind.Solid))));   // PAX-107: was delay 8, move 96 (and 115 before)
            // The waterfall: three bands of spikes on each of the shaft's walls (y 6-9), pointing in. The top two (Fall_W0, Fall_E0)
            // run on the hinge floor's trigger (Continuous); each band below follows the band above (a chain). Out, a band's two
            // strips leave 0.9 u between them (PAX-107 round three: 0.55 u deep, the shaft 2 u wide), less than the cat (1 u): a
            // cat in a band that is out dies.
            for (int i = 0; i < 3; i++)
            {
                float y = 8.5f - i;
                var top = new SoloRoomTrapSettings(revealDelayTicks:WaterfallOff,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:WaterfallOut);
                var below = new SoloRoomTrapSettings(revealDelayTicks:WaterfallStep,triggerSource:TrapTriggerSource.Chain,chainSource:$"Fall_W{i - 1}",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:WaterfallOut);
                if (i == 0)
                {
                    elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Fall_W0",(21.275f,y),(.55f,1f),(18.75f,12f),(4.5f,4f),top));
                    elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Fall_E0",(22.725f,y),(.55f,1f),(18.75f,12f),(4.5f,4f),top));
                    continue;
                }
                elements.Add(E(SoloRoomElementKind.HiddenSpikes,$"Fall_W{i}",(21.275f,y),(.55f,1f),settings:below));
                elements.Add(E(SoloRoomElementKind.HiddenSpikes,$"Fall_E{i}",(22.725f,y),(.55f,1f),settings:below));
            }
            // T5: spikes on S1 under S2's hinge floor (x 13.5-15: a cat that falls through its 2.5 u hole lands on them). They come
            // up as the cat comes over them on S2 or enters S1 east of them (their trigger spans S1 to the roof from x 13 to the
            // shaft) and are jumped on the way left.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(14.25f,5.15f),(1.5f,.3f),(17f,9.5f),(8f,9f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T6: S1's hinge floor (x 16-18.5, hinged at its west end). A cat past it (over Spikes_4, x 13.1-14.9) sets it off; it swings up behind
            // the cat and slides 5.5 u west, pushing a cat that stops onto Spikes_5 (x 8.5-10); jumped on the way. PAX-107 round
            // three (the developer: "Make this wall stand up and move faster"): up over 7 ticks (was 10), across over 55 (was 73); then
            // ("all the floors that turn vertical and move sideways, need to move 20% faster") up over 6, across over 46.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S1",(17.25f,4.5f),(2.5f,1f),(14f,7f),(1.8f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:18,offset:new Vector2(-5.5f,0f),moveTicks:28,holdTicks:600,movingKind:MovingTrapKind.Solid))));   // PAX-107: 20 % faster (was 7 and 55)
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_5",(9.25f,5.15f),(1.5f,.3f)));
            // PAX-107 round three: Press_S1, a falling ceiling. A block under S2_A (x 4.5-6, by S1_W's west end) comes down onto
            // S1_W over 12 ticks, stays 30, goes back up over 30, every 140: wait where the jump over Spikes_5 lands for it to go
            // up, then run under it and off S1_W's end.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Press_S1",(5.25f,8.5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                offset:new Vector2(0f,-3f),moveTicks:12,holdTicks:30,returnTicks:30,repeatMode:TrapRepeatMode.Periodic,periodTicks:PressPeriod,phaseTicks:0,cooldownTicks:72,movingKind:MovingTrapKind.Solid)));
            // PAX-107 round three: Piston_G, a floor that comes up. It sits in the ground's notch (x 5-6.5) and rises 3.9 u, into
            // S1_W's underside, over 25 ticks, stays 20, goes back down over 25, every 140: wait by the wall for it to go down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_G",(5.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"S1_W"),new SoloRoomTrapSettings(
                offset:new Vector2(0f,3.9f),moveTicks:25,holdTicks:20,returnTicks:25,repeatMode:TrapRepeatMode.Periodic,periodTicks:PistonPeriod,phaseTicks:PistonPhase,cooldownTicks:70,movingKind:MovingTrapKind.Solid))));
            // PAX-107 round three: the maze's traps. Press_1 (x 26-27.5) comes down from the roof onto Maze_1; Spikes_M2 (x 27-28.5)
            // come up on Maze_2 on a rhythm; Piston_3, Maze_3's middle (x 25.5-27), rises into Maze_2's underside.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Press_1",(26.75f,13.5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                offset:new Vector2(0f,-1f),moveTicks:16,holdTicks:30,returnTicks:20,repeatMode:TrapRepeatMode.Periodic,periodTicks:MazePeriod,phaseTicks:0,cooldownTicks:66,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_M2",(27.75f,9.65f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,
                repeatMode:TrapRepeatMode.Periodic,periodTicks:MazePeriod,phaseTicks:100,cooldownTicks:MazePeriod / 2)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_3",(26.25f,6.5f),(1.5f,1f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Maze_2"),new SoloRoomTrapSettings(
                offset:new Vector2(0f,1.6f),moveTicks:15,holdTicks:25,returnTicks:20,repeatMode:TrapRepeatMode.Periodic,periodTicks:MazePeriod,phaseTicks:30,cooldownTicks:60,movingKind:MovingTrapKind.Solid))));
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
