using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 8, chains: one trigger changes the room further on, and the cat sees it happen. A descent,
    // top left to bottom right. Three storeys: S2 (top 10, x 2-21), S1 (top 5, x 4-28) and the ground (top 0), under a
    // roof at 14. The way runs right along S2, drops off its end onto S1, runs left along S1, drops off its left end into
    // the column by the wall, and runs right along the ground to the door. Every long chain links permanent changes only
    // (spikes that come up and stay up), and each one's result is in view before the cat reaches it (Architect, half B Q3).
    static class L008Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3.2f,10f),(0f,0f)),
                // S2, from the hole behind the start to x 21: a lift (x 15-16) and a floor that gives way (x 16-18.5) in it.
                E(SoloRoomElementKind.Floor,"S2_A",(8.5f,9.5f),(13f,1f)),
                E(SoloRoomElementKind.Floor,"S2_B",(19.75f,9.5f),(2.5f,1f)),
                // S1, from the column by the wall to the ledge at x 28. PAX-102 (D-085 amendment): split around Block_6 (x 11.2-12.8),
                // so its fall leaves a real hole, wide enough for the cat; drawn as one surface until then.
                E(SoloRoomElementKind.Floor,"S1",(20.4f,4.5f),(15.2f,1f)),
                E(SoloRoomElementKind.Floor,"S1_W",(7.6f,4.5f),(7.2f,1f)),
                // The ground, and the wall between the column the way drops down and the shaft behind the start. PAX-102: the
                // ground is cut by two pits wider than a jump (x 15-19.5 and 21.5-26), each crossed only on a rider.
                E(SoloRoomElementKind.Floor,"Ground",(8.5f,-2f),(13f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_M",(20.5f,-2f),(2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_E",(29f,-2f),(6f,4f)),
                E(SoloRoomElementKind.Wall,"Shaft_Wall",(2.25f,4.5f),(.5f,9f)),
                // A pillar by the door: the ground passes under it; a cat that drops from the ledge can't pass it.
                E(SoloRoomElementKind.Wall,"Pillar",(30.75f,3.5f),(.5f,5f)),
            };
            // T1: a block in the roof, set off as the cat sets off; it lands on a cat that runs on.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(7f,14.5f),(1f,1f),(5.25f,12f),(.5f,4f),new SoloRoomTrapSettings(delayTicks:9,unitsPerTick:.36f,travelDistance:4f)));
            // T2 (the chain): the block's landing sets off spikes further along S2; they come up a while later and stay up.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(10.75f,10.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:48,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            // T3: a section of S2 is a lift, under a long run of spikes on the roof (x 10.5-20.5) that are always in view and
            // out of reach of a jump from S2, so they don't single the lift out. Its trigger is its top strip, so a cat that
            // stands or walks on it rides it into them, and a cat that jumps it never sets it off.
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_3",(15.5f,13.85f),(10f,.3f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_3",(15.5f,9.5f),(1f,1f),(15.5f,10.28f),(1f,.56f),new SoloRoomTrapSettings(offset:new Vector2(0f,3.2f),moveTicks:8,holdTicks:60,returnTicks:20,movingKind:MovingTrapKind.Solid)));
            // T4: the floor where the jump over the lift lands gives way under a cat that stops, onto spikes on S1 (they go
            // back down before the way comes past).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_4",(17.25f,9.5f),(2.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(17.25f,5.15f),(2.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Floor_4",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:40)));
            // T5 (the long chain): the same collapse sets off spikes on S1 further on; they come up while the cat is still
            // above them, and stay up.
            // PAX-102: east of Block_6's hole.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5",(14f,5.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:60,triggerSource:TrapTriggerSource.Chain,chainSource:"Floor_4")));
            // T6: a block in S1's underside over the ground, set off as the cat comes up to it (its trigger spans it, since the
            // ground can be reached from either side); it lands on a cat that runs on. PAX-102: 1.6 u wide, a section of S1, so
            // it leaves a real hole; its trigger stays on the ground storey.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_6",(12f,4.5f),(1.6f,1f),(11.4f,2f),(4.4f,4f),new SoloRoomTrapSettings(delayTicks:13,unitsPerTick:.36f,travelDistance:4f)));
            // Dead end: the hole behind the start, "the way down", opens into a shaft whose floor isn't there.
            L001Layout.AddShaft(elements, 9, 0f, 2f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_9",(1f,-1.5f),(2f,3f)));
            // Dead end: at S1's end, a ledge down toward the door gives way onto spikes on the ground.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(29.25f,.15f),(2.5f,.3f),(29.25f,8.75f),(2.5f,10.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_D",(29.25f,3.25f),(2.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            // PAX-102 (the developer: "gaps where you have to jump onto a moving platform"): the ground's two pits, each crossed
            // only on a rider (Carry, Periodic, on the level's 150-tick rhythm) that waits at its west side, carries the cat 3 u
            // east, holds, and comes back. Wait for it, ride it, step off; a cat that runs on as it arrives finds it gone.
            L001Layout.AddShaft(elements, 10, 15f, 19.5f);
            L001Layout.AddShaft(elements, 11, 21.5f, 26f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_A",(15.75f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_B",(22.25f,-.25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(3f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:75,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            // PAX-102 ("arrow launchers on both"): a repeating arrow where the cat lands on S1 (from a corbel under S2_B, every 150
            // ticks: wait where you land, then go), and a single angled shot on the ground as the cat sets off from the column
            // (from a corbel under S1's west end, down-right at -45 degrees as the cat passes under it, ahead of a cat that stops
            // at once).
            elements.Add(E(SoloRoomElementKind.Wall,"Corbel_S",(20f,8.25f),(1f,1.5f)));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_S",(19.75f,7.9f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,7.9f,19.5f - 2.9f / Mathf.Tan(60f * Mathf.Deg2Rad),angleDegrees:-60f),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:60))));
            elements.Add(E(SoloRoomElementKind.Wall,"Corbel_O",(4.5f,3.25f),(1f,1.5f)));
            // D-119 (the developer: all launchers shoot non-stop): Arrow_O keeps its first shot's trigger, then fires every 150 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_O",(4.75f,2.9f),(.5f,.4f),(5.75f,2f),(.5f,4f),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,2.9f,5f + 2.9f,angleDegrees:-45f),new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:150))));
            var jumps = new[] {
                J("Spikes_2",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.3f,12.2f,10f,10f,3f,.3f),
                J("Lift_3",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,13.5f,16.4f,10f,10f,3f,sourceName:"S2_A",destinationName:"Floor_4"),
                J("Spikes_5",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,15.45f,12.55f,5f,5f,3f,.3f),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(9,0f,2f), L001Layout.Shaft(10,15f,19.5f), L001Layout.Shaft(11,21.5f,26f) },jumps);
        }
    }
}
