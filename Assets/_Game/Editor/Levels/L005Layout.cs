using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 5, the walls shoot. A zigzag down three storeys: S2 (a slab, top 10) from the top right to
    // the left, down past two ledges at the left wall to S1 (top 5), right along S1 to its end, down to the ground and
    // left to the door. The arrows are disguised in walls; once fired, an arrow stays where its lane ends, so the lane is
    // plain. A fall from S1 down to the ground only kills where a collapse has armed spikes for it.
    static class L005Layout
    {
        // D-116: Arrow_X's place in the drip's 110-tick rhythm (the drip fires at 0).
        internal const int ArrowXPhase = 30;
        // D-119: the riders' places in their 150-tick rhythm.
        internal const int RideS1Phase = 0, RideS2Phase = 75, RideG2Phase = 50, RideG1Phase = 125;

        // D-119: a 1.5 x 0.5 rider (Carry, Periodic every 150 ticks): out `dx` over 40 ticks, holds 20, back over 40.
        static SoloRoomElement Rider(string name, (float x, float y) at, float dx, int phase) =>
            E(SoloRoomElementKind.MovingTrap,name,at,(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(dx,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:phase,cooldownTicks:100,movingKind:MovingTrapKind.Solid)));

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,12f),(1f,4f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,12f),(1f,4f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(30f,10f),(0f,0f)),
                // The arrows' walls: pillars at both ends of S2, and one at S1's left end.
                E(SoloRoomElementKind.Wall,"Pillar_L",(.25f,12f),(.5f,4f)),
                E(SoloRoomElementKind.Wall,"Pillar_R",(31.75f,12f),(.5f,4f)),
                E(SoloRoomElementKind.Wall,"Pillar_S1",(0f,7f),(1f,4f)),
                E(SoloRoomElementKind.Floor,"S2",(18.75f,9.5f),(26.5f,1f)),
                // Down at the left wall: the high ledge across a gap, and the low ledge under it, whose right end gives way.
                E(SoloRoomElementKind.Floor,"Ledge_Hi",(2.7f,9.75f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Ledge_Lo",(3f,7.8f),(3f,.5f)),
                // S1, with the nook, and a post at its right end.
                E(SoloRoomElementKind.Floor,"S1_A",(2.25f,4.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Floor,"Nook",(5.5f,3.5f),(2f,1f)),
                // D-119 (the developer: "Level 5 is too easy … make the floors moving, there are longer sections of just plain
                // running"): S1 past the nook is cut by two holes wider than a jump (x 9.5-14 and 15.5-20), each crossed on a rider,
                // over the ground's pits.
                E(SoloRoomElementKind.Floor,"S1_B1",(8f,4.5f),(3f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B2",(14.75f,4.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B3",(24f,4.5f),(8f,1f)),
                E(SoloRoomElementKind.Wall,"Post",(27.75f,5.5f),(.5f,1f)),
                // The ground, with a floor that isn't there before the door, and the high ledge near the door.
                E(SoloRoomElementKind.Floor,"Ground_1",(2.1f,-2f),(4.2f,4f)),
                // D-119: the ground back to the door has the pits under S1's holes, each crossed on a rider.
                E(SoloRoomElementKind.Floor,"Ground_2a",(7.65f,-2f),(3.7f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2b",(14.75f,-2f),(1.5f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2c",(26f,-2f),(12f,4f)),
            };
            // T1: an arrow along S2 at shin height, out of the pillar behind the cat. D-116 (the developer: "throw several arrows in
            // intervals, and it doesn't stop"; then "wait till the cat has moved at least 2 to the left", "lower intervals"): it
            // rearms, every 200 ticks while the cat is on S2 (its trigger, x 5.5-27.5: 2 u left of the start, so the first shot
            // waits for the cat to set off), and the walk along S2 jumps it as it comes (a jumped arrow, as L003's S1, D-113).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_A",(31.75f,10.3f),(.5f,.4f),(16.5f,12f),(22f,4f),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,10.3f,.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:200))));   // D-119: never stops once set off
            // T1b (PAX-100, D-106): a repeating arrow. A corbel hangs from the roof over S2 (x 15.5-16.5, down to y 11.7, above
            // Arrow_B's lane); its honest launcher fires down-left at -60 degrees every 110 ticks, onto S2 at x 14.3. Its lane
            // crosses the cat's band only over about half a unit: count, then pass; a cat that stops under it is hit.
            elements.Add(E(SoloRoomElementKind.Wall,"Corbel",(16f,12.85f),(1f,2.3f)));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_Drip",(15.75f,12f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,12f,15.5f - 2f / Mathf.Tan(60f * Mathf.Deg2Rad),angleDegrees:-60f),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:110,phaseTicks:0,cooldownTicks:60))));
            // D-116 (the developer: "another launcher here throwing at 45 degree angle but in the opposite direction"): the
            // corbel's right face fires down-right at -45 degrees onto S2 at x 18.5, on the same 110-tick rhythm, 30 ticks into it.
            // Wait right of its lane for a shot, then walk under the corbel: the drip comes 80 ticks after it, once the cat is past.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_X",(16.25f,12f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,12f,16.5f + 2f / Mathf.Tan(45f * Mathf.Deg2Rad),angleDegrees:-45f),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:110,phaseTicks:ArrowXPhase,cooldownTicks:60))));
            // T2: an arrow across the gap to the high ledge, at jump height, when the cat reaches S2's end.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_B",(.25f,11.5f),(.5f,.4f),(6f,12f),(1f,4f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,11.5f,31.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:180))));   // D-119: then every 180
            // T3: the low ledge's right end gives way onto spikes in the nook and just past it. They arm only then, so a cat
            // that took the other end stands safely on them in the nook.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_Lo2",(5.25f,7.8f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:9)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(5.5f,4.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Ledge_Lo2",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3b",(7.5f,5.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Ledge_Lo2",delayTicks:1)));
            // T4 and T5: two arrows along S1 at body height, out of the pillar behind the cat, the second a while after the
            // first. The nook is under their lane; both stop at the post.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_C",(.25f,5.3f),(.5f,.4f),(3.75f,6.275f),(.5f,2.55f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:20,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:160))));   // D-119: then every 180
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_D",(.25f,5.3f),(.5f,.4f),
                settings:new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:90,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_C",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:230))));   // D-119: then every 320
            // D-116 (the developer: "if the cat tries to go underneath it gets squished. If it goes on top the platform starts to
            // shrink but from the right, slowly sliding towards the spikes"): the crush ledge. A cat under it is crushed (it sinks
            // for an 8-tick tell, then drops onto the ground); one on top starts it shrinking from its right end, over 150 ticks,
            // down to its spiked left end; its trigger is the space over it (its top to S1's underside), so a cat it crushes from
            // below doesn't start the shrink. The way past: onto it, left along it, and over the spikes off its end. D-119: it
            // stands near the ground's right end (x 23.5-27.5, under S1_B3), right of where the drop off S1 comes down, clear of the pits.
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Ledge_D",(25.5f,1f),(4f,.5f),(25.5f,2.625f),(4f,2.75f),new SoloRoomTrapSettings(
                new ShrinkSettings(150,1.5f,ShrinkFrom.Right,dropDistance:.75f),new SoloRoomTrapSettings(delayTicks:0))));
            // T7: spikes on the ledge's left end, as the cat comes over it; jump them off the end.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(24.25f,1.4f),(1.5f,.3f),(25.5f,2.625f),(4f,2.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // D-119: the riders. On S1 each waits at its hole's west side and carries the cat 3 u east; on the ground each waits at
            // its pit's east side and carries it 3 u west (Carry, Periodic, one 150-tick rhythm each).
            elements.Add(Rider("Ride_S1",(10.25f,4.75f),3f,RideS1Phase));
            elements.Add(Rider("Ride_S2",(16.25f,4.75f),3f,RideS2Phase));
            elements.Add(Rider("Ride_G2",(19.25f,-.25f),-3f,RideG2Phase));
            elements.Add(Rider("Ride_G1",(13.25f,-.25f),-3f,RideG1Phase));
            L001Layout.AddShaft(elements, 1, 9.5f, 14f);
            L001Layout.AddShaft(elements, 2, 15.5f, 20f);
            // D-119 (the developer: "I asked for a cloud in level 5"; all the way through): a storm cloud wakes at the start and
            // follows the cat floor to floor: under the corbel over S2, under S2 over S1, and under S1 over the ground. It follows at
            // 0.05 u/tick, strikes every 150 ticks, the first 80 ticks after it wakes (25-tick tell).
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(28.5f,11.2f),(2f,.8f),(30f,11f),(2f,2f),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] {
                    new StormFloor(-100f, 3.3f, 7.6f, 30.9f),
                    new StormFloor(4.5f, 8.3f, 6.6f, 30.9f),
                    new StormFloor(9.5f, 11.2f, 2.5f, 30.4f) }, followSpeed:.05f, firstStrikeDelay:80, strikePeriod:150), new SoloRoomTrapSettings(delayTicks:0))));
            // T6: T1 of level 1 again, before the door.
            L001Layout.AddShaft(elements, 6, 4.2f, 5.8f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_6",(5f,-1.5f),(1.6f,3f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(1.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_6",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,6.4f,3.7f,0f,0f,3f,sourceName:"Ground_2a",destinationName:"Ground_1"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(6,4.2f,5.8f), L001Layout.Shaft(1,9.5f,14f), L001Layout.Shaft(2,15.5f,20f) },jumps);
        }
    }
}
