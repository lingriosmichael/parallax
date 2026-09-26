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
                // S1, from the column by the wall to the ledge at x 28.
                E(SoloRoomElementKind.Floor,"S1",(16f,4.5f),(24f,1f)),
                // The ground, and the wall between the column the way drops down and the shaft behind the start.
                E(SoloRoomElementKind.Floor,"Ground",(17f,-2f),(30f,4f)),
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
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5",(11.75f,5.15f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:60,triggerSource:TrapTriggerSource.Chain,chainSource:"Floor_4")));
            // T6: a block in S1's underside over the ground, set off as the cat comes up to it (its trigger spans it, since the
            // ground can be reached from either side); it lands on a cat that runs on.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_6",(12f,4.5f),(1f,1f),(11f,2f),(3f,4f),new SoloRoomTrapSettings(delayTicks:13,unitsPerTick:.36f,travelDistance:4f)));
            // Dead end: the hole behind the start, "the way down", opens into a shaft whose floor isn't there.
            L001Layout.AddShaft(elements, 9, 0f, 2f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_9",(1f,-1.5f),(2f,3f)));
            // Dead end: at S1's end, a ledge down toward the door gives way onto spikes on the ground.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(29.25f,.15f),(2.5f,.3f),(29.25f,8.75f),(2.5f,10.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_D",(29.25f,3.25f),(2.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Spikes_2",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.3f,12.2f,10f,10f,3f,.3f),
                J("Lift_3",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,13.5f,16.4f,10f,10f,3f,sourceName:"S2_A",destinationName:"Floor_4"),
                J("Spikes_5",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,13.2f,10.3f,5f,5f,3f,.3f),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(9,0f,2f) },jumps);
        }
    }
}
