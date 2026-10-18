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
        internal const int VolleyPeriod = 120;   // D-156: the roof's vertical arrows, together
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
                // 2026-10-18 (the developer: "a section there on the left where is open and you can go out of the screen. Fill it up"):
                // up to the wall over S2 (y 10, was 9), closing the gap under the high ledge.
                E(SoloRoomElementKind.Wall,"Pillar_S1",(0f,7.5f),(1f,5f)),
                E(SoloRoomElementKind.Floor,"S2",(18.75f,9.5f),(26.5f,1f)),
                // Down at the left wall: the high ledge across a gap. (The low ledge under it, Ledge_Lo and its giving-way end
                // Ledge_Lo2 with T3's spikes, removed 2026-10-18, the developer: "Remove this platform entirely"; the cat drops
                // from S2's end straight to S1.)
                E(SoloRoomElementKind.Floor,"Ledge_Hi",(2.7f,9.75f),(2f,.5f)),
                // S1, with the nook, and a post at its right end.
                // 2026-10-18 (the developer: "it looks missing platform or wall, fill in the space"): S1_A and S1_B1 reach down to the
                // nook's underside (y 3), so the nook is a notch in one floor, not a box hanging under it.
                E(SoloRoomElementKind.Floor,"S1_A",(2.25f,4f),(4.5f,2f)),
                E(SoloRoomElementKind.Floor,"Nook",(5.5f,3.5f),(2f,1f)),
                // D-119 (the developer: "Level 5 is too easy … make the floors moving, there are longer sections of just plain
                // running"): S1 past the nook is cut by two holes wider than a jump (x 9.5-14 and 15.5-20), each crossed on a rider,
                // over the ground's pits.
                E(SoloRoomElementKind.Floor,"S1_B1",(8f,4f),(3f,2f)),
                E(SoloRoomElementKind.Floor,"S1_B2",(14.75f,4.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B3",(24f,4.5f),(8f,1f)),
                // 2026-10-18 (the developer: "Make this ledge thicker towards the left"): 1 u wide (x 27-28, was 27.5-28).
                E(SoloRoomElementKind.Wall,"Post",(27.5f,5.5f),(1f,1f)),
                // The ground, with a floor that isn't there before the door, and the high ledge near the door.
                E(SoloRoomElementKind.Floor,"Ground_1",(2.1f,-2f),(4.2f,4f)),
                // D-119: the ground back to the door has the pits under S1's holes, each crossed on a rider.
                E(SoloRoomElementKind.Floor,"Ground_2a",(7.65f,-2f),(3.7f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2b",(14.75f,-2f),(1.5f,4f)),
                // 2026-10-18: the ground under the crush ledge is its own section (Rise_D, below), between Ground_2c and Ground_2d.
                E(SoloRoomElementKind.Floor,"Ground_2c",(21.75f,-2f),(3.5f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2c_Base",(25.5f,-2.5f),(4f,3f)),
                E(SoloRoomElementKind.Floor,"Ground_2d",(29.75f,-2f),(4.5f,4f)),
            };
            // T1: an arrow along S2 at shin height, out of the pillar behind the cat. D-116 (the developer: "throw several arrows in
            // intervals, and it doesn't stop"; then "wait till the cat has moved at least 2 to the left", "lower intervals"): it
            // rearms, every 200 ticks while the cat is on S2 (its trigger, x 5.5-27.5: 2 u left of the start, so the first shot
            // waits for the cat to set off), and the walk along S2 jumps it as it comes (a jumped arrow, as L003's S1, D-113).
            // D-126 (the developer: "too slow. Half the current time"): every 100 ticks (was 200).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_A",(31.75f,10.3f),(.5f,.4f),(16.5f,12f),(22f,4f),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,10.3f,.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:100))));   // D-119: never stops once set off
            // T1b. D-156 (the developer, 2026-10-18: "no more diagonal arrow launchers and arrows ... change them to several spaced
            // out vertical arrows that shoot from the top to the bottom. Lets test that out in level 05"): the corbel and its two
            // angled launchers (Arrow_Drip at -60, Arrow_X at -45) are gone. Three launchers in the roof over S2 (x 14, 17, 20) shoot
            // straight down onto it together, every 120 ticks: wait right of them for a volley to land, then walk under all three.
            foreach ((string name, float x) in new[] { ("Arrow_V1", 14f), ("Arrow_V2", 17f), ("Arrow_V3", 20f) })
                elements.Add(E(SoloRoomElementKind.Arrow,name,(x,14.25f),(.5f,.5f),settings:new SoloRoomTrapSettings(
                    new ArrowLane(ArrowDirection.Right,14f,x,angleDegrees:-90f,laneEndY:10f),
                    new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:VolleyPeriod,phaseTicks:0,cooldownTicks:60))));
            // T2: an arrow across the gap to the high ledge, at jump height, when the cat reaches S2's end.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_B",(.25f,11.5f),(.5f,.4f),(6f,12f),(1f,4f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,11.5f,31.5f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:180))));   // D-119: then every 180
            // T4 and T5: two arrows along S1 at body height, out of the pillar behind the cat, the second a while after the
            // first. The nook is under their lane; both stop at the post.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_C",(.25f,5.3f),(.5f,.4f),(3.75f,6.275f),(.5f,2.55f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:20,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:160))));   // D-119: then every 180
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_D",(.25f,5.3f),(.5f,.4f),
                settings:new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27f,unitsPerTick:.36f,disguised:true),
                new SoloRoomTrapSettings(delayTicks:90,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_C",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:230))));   // D-119: then every 320
            // D-116 (the developer: "if the cat tries to go underneath it gets squished. If it goes on top the platform starts to
            // shrink but from the right, slowly sliding towards the spikes"): the crush ledge. A cat under it is crushed (it sinks
            // for an 8-tick tell, then drops onto the ground); one on top starts it shrinking from its right end, over 150 ticks,
            // down to its spiked left end; its trigger is the space over it (its top to S1's underside), so a cat it crushes from
            // below doesn't start the shrink. The way past: onto it, left along it, and over the spikes off its end. D-119: it
            // stands near the ground's right end (x 23.5-27.5, under S1_B3), right of where the drop off S1 comes down, clear of the pits.
            // 2026-10-18 (the developer: "Make this platform slightly higher"): 0.25 u higher (y 1-1.5). And ("I want the floor from
            // below to move up and squish you ... then it gives you a weird situation where spikes are just hanging in thin air.
            // Once it moves up it stays up"): the ledge no longer drops; the ground under it rises (Rise_D, below).
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Ledge_D",(25.5f,1.25f),(4f,.5f),(25.5f,2.75f),(4f,2.5f),new SoloRoomTrapSettings(
                new ShrinkSettings(150,1.5f,ShrinkFrom.Right),new SoloRoomTrapSettings(delayTicks:0))));
            // The ground's top unit under the ledge (x 23.5-27.5) rises 1 u into it, 8 ticks after a cat comes under it (the space
            // between them is its trigger), crushing it against the ledge; it stays up.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Rise_D",(25.5f,-.5f),(4f,1f),(25.5f,.5f),(4f,1f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Ledge_D"),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(0f,1f),moveTicks:6,holdTicks:0,returnTicks:0,repeatMode:TrapRepeatMode.Once,movingKind:MovingTrapKind.Solid))));
            // T7: spikes on the ledge's left end, as the cat comes over it; jump them off the end.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(24.25f,1.65f),(1.5f,.3f),(25.5f,2.75f),(4f,2.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // D-119: the riders. On S1 each waits at its hole's west side and carries the cat 3 u east; on the ground each waits at
            // its pit's east side and carries it 3 u west (Carry, Periodic, one 150-tick rhythm each).
            elements.Add(Rider("Ride_S1",(10.25f,4.75f),3f,RideS1Phase));
            elements.Add(Rider("Ride_S2",(16.25f,4.75f),3f,RideS2Phase));
            elements.Add(Rider("Ride_G2",(19.25f,-.25f),-3f,RideG2Phase));
            elements.Add(Rider("Ride_G1",(13.25f,-.25f),-3f,RideG1Phase));
            L001Layout.AddShaft(elements, 1, 9.5f, 14f);
            L001Layout.AddShaft(elements, 2, 15.5f, 20f);
            // D-119 (the developer: "I asked for a cloud in level 5"): a storm cloud follows the cat floor to floor. PAX-107 (the
            // developer: "make the cloud start appearing from the middle level, not from the top"; clouds 20 % faster, zap 10 %
            // faster): it waits over S1 and wakes as the cat comes down onto S1_A, then follows it under S2 over S1 and under S1
            // over the ground, at 0.12 u/tick (PAX-107, D-124: the cat's run), striking every 135 ticks, the first 80 ticks after it wakes (25-tick tell).
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(7f,8.3f),(2f,.8f),(2.25f,6f),(4.5f,2f),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] {
                    new StormFloor(-100f, 3.3f, 10.6f, 30.9f),   // 2026-10-18: from x 10.6 (was 7.6), clear of S1_B1's deeper body
                    new StormFloor(4.5f, 8.3f, 6.6f, 30.9f) }, followSpeed:.12f, firstStrikeDelay:80, strikePeriod:135, climbSpeed:.24f), new SoloRoomTrapSettings(delayTicks:0))));
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
