using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-143): level 14, "Snapping Canopy" (vines), rebuilt from nothing (the developer: "Remove level 14 ... I want a
    // fresh new idea"). The verb is HANG ON, MOVE ON: three storeys 5.5 u apart, climbed bottom to top (walked east, west,
    // east). Each storey crosses a spike trench vine to vine; the vines snap behind the cat in a wave, and launchers fire across
    // the trench at two heights without stopping, so the cat dodges by climbing up or down its vine. Each storey then has a
    // walkway of traps that act on the run, and a vine up to the next storey; the last climbs a mossy chimney to the door.
    //  - Field (section 0, the ground, walked east): Hinge_0 stands up behind the start and pushes toward the trench: leap onto
    //    V1 at once. V1-V4 snap in a wave from V1's grab. Arrow_1Hi (from Lintel_W1, y 2.6) kills a leap from high on a vine,
    //    so climb down before each leap; the way out under Pier_1 is a leap from low on V4, through Arrow_1Lo's lane (in the
    //    trench, from the east): go once its shot has passed. On the walk, Lift_1 shoots up into trench 2's bed on its own beat
    //    (wait for it), and Collapse_1 drops into Pit_1 (jump it). V_Up1 climbs the east shaft to S2.
    //  - Ledges (section 1, S2, walked west): the same crossing over trench 2 (V5-V7, Arrow_2Hi from the east, Arrow_2Lo from
    //    the west). Lift_2 shoots up under a running cat into trench 3's bed (stop short); Hinge_3 stands up behind the cat and
    //    pushes it onto Spikes_2 (jump them). V_Up2 climbs the west shaft to S3.
    //  - Roof (section 2, S3, walked east): V8-V11 under the roof; Slam_10 comes down V10's column as the cat leaps for V9 (wait
    //    for it to go back up); the Perch in the trench has hidden spikes on top (dead end). Slam_3 comes down on the walk (set
    //    it off, stop short, hop it); Hinge_5, spiked, stands up behind the cat and creeps onto it at Spikes_5 (jump them);
    //    V_Lure over them snaps (dead end). Under Hang_C into the chimney: Wall_C's moss, Hang_C's, then over Wall_C's top to
    //    the door.
    static class L014Layout
    {
        // The storeys' walk lines and the heights the trench vines hang from.
        internal const float S1 = 0f, S2 = 5.5f, S3 = 11f;
        // The waves (each vine's snap after the one before) and the launchers' timing (first shot after the trigger, then every
        // delay + cooldown ticks).
        internal const int V1Snap = 50, Wave1 = 50, V5Snap = 60, Wave2 = 60, V8Snap = 40, Wave3 = 60, SlamDelay = 6, Lift2Delay = 24;
        internal const int HiDelay1 = 5, HiCool1 = 50, LoDelay1 = 25, LoCool1 = 48;
        internal const int HiDelay2 = 8, HiCool2 = 45, LoDelay2 = 33, LoCool2 = 40;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,9f),(1f,22f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,9f),(1f,22f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,19.75f),(32f,.5f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(31f,16.25f),(.6f,1.5f)),

                // ---- S1, the ground (walk 0) ----
                // The start (x 1.5-4.5), Hinge_0's notch (x 0-1.5), and the west lintel over the trench's mouth (x 3.5-4.5, from y 2.2).
                E(SoloRoomElementKind.Floor,"Start_Floor",(3f,-1f),(3f,2f)),
                E(SoloRoomElementKind.Floor,"Notch_0",(.75f,-1.25f),(1.5f,1.5f)),
                E(SoloRoomElementKind.Wall,"Lintel_W1",(4f,3.6f),(1f,2.8f)),
                // Trench 1 (x 4.5-16): the bed 1.5 u down, thorns on it.
                E(SoloRoomElementKind.Floor,"Bed_1",(10.25f,-1.75f),(11.5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_1",(10.25f,-1.35f),(11.5f,.3f)),
                // The walk east of the trench: Walk_1a (x 16-22), Lift_1 on its seat (x 22-24), Walk_1b (x 24-26.5), Collapse_1 over
                // Pit_1 (x 26.5-28.7), Walk_1c (x 28.7-32: the east shaft's floor).
                E(SoloRoomElementKind.Floor,"Walk_1a",(19f,-1f),(6f,2f)),
                E(SoloRoomElementKind.Floor,"Seat_1",(23f,-1.25f),(2f,1.5f)),
                E(SoloRoomElementKind.Floor,"Walk_1b",(25.25f,-1f),(2.5f,2f)),
                E(SoloRoomElementKind.PitBottom,"Pit_1_Bottom",(27.6f,-1.25f),(2.2f,.5f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_1",new Vector2(27.6f,-.85f),new Vector2(2.2f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Floor,"Walk_1c",(30.35f,-1f),(3.3f,2f)),
                // Pier_1 (x 16-18, y 1.6-5.5): trench 1's east lintel (a leap out goes under it from low) and S2's footing between the trenches.
                E(SoloRoomElementKind.Floor,"Pier_1",(17f,3.55f),(2f,3.9f)),

                // ---- S2 (walk 5.5) ----
                // West of Pier_1: S2_A (x 0-6.3), Hinge_3 (x 6.3-7.8), S2_B (x 7.8-11.7), Lift_2 (x 11.7-13.9), S2_C (x 13.9-16).
                // S1's vines hang from their undersides (y 5).
                E(SoloRoomElementKind.Floor,"S2_A",(3.15f,5.25f),(6.3f,.5f)),
                E(SoloRoomElementKind.Floor,"S2_B",(9.75f,5.25f),(3.9f,.5f)),
                E(SoloRoomElementKind.Floor,"S2_C",(14.95f,5.25f),(2.1f,.5f)),
                E(SoloRoomElementKind.Hazard,"Spikes_2",(4f,5.65f),(1.2f,.3f)),
                // Trench 2 (x 18-27): its bed (y 3.5-4) is the ceiling of S1's walk.
                E(SoloRoomElementKind.Floor,"Bed_2",(22.5f,3.75f),(9f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_2",(22.5f,4.15f),(9f,.3f)),
                // Landing_2 (x 27-29.5), where V_Up1's leap lands; the east shaft (x 29.5-32) is open from the ground to Cap_E2 (from y 7.7).
                E(SoloRoomElementKind.Floor,"Landing_2",(28.25f,4.25f),(2.5f,2.5f)),
                E(SoloRoomElementKind.Wall,"Cap_E2",(29.5f,9.1f),(5f,2.8f)),

                // ---- S3 (walk 11) ----
                // S3_Land (x 2.5-4.5), trench 3 (x 4.5-16, its bed the ceiling of S2's walk), Pier_3 (x 16-18, y 7.5-10.5: trench 3's
                // east wall and trench 2's west lintel), and the walk to the chimney: S3_E (x 16-23), Hinge_5, S3_F (x 24.5-32).
                E(SoloRoomElementKind.Floor,"S3_Land",(3.5f,10f),(2f,2f)),
                E(SoloRoomElementKind.Floor,"Bed_3",(10.25f,9.25f),(11.5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_3",(6.9f,9.65f),(4.8f,.3f)),
                E(SoloRoomElementKind.Hazard,"Thorns_3b",(13.15f,9.65f),(5.7f,.3f)),
                // The Perch, a stone stub from trench 3's bed between V9 and V10, looks like a rest (dead end: hidden spikes on top).
                E(SoloRoomElementKind.Floor,"Perch",(9.8f,10.25f),(1f,1.5f)),
                E(SoloRoomElementKind.Floor,"Pier_3",(17f,9f),(2f,3f)),
                E(SoloRoomElementKind.Floor,"S3_E",(19.5f,10.75f),(7f,.5f)),
                E(SoloRoomElementKind.Floor,"S3_F",(28.25f,10.75f),(7.5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Spikes_5",(26.45f,11.15f),(.9f,.3f)),
                // The roof over S3 (y 15.5 up), down to y 13.5 over the west shaft; split around Slam_3 (x 18.5-20) and its cap.
                E(SoloRoomElementKind.Ceiling,"Roof_W",(1.25f,16.5f),(2.5f,6f)),
                E(SoloRoomElementKind.Ceiling,"Roof_3a",(6.55f,17.5f),(8.1f,4f)),
                E(SoloRoomElementKind.Ceiling,"Cap_10",(11.2f,18f),(1.2f,3f)),
                E(SoloRoomElementKind.Ceiling,"Roof_3c",(15.9f,17.5f),(8.2f,4f)),
                E(SoloRoomElementKind.Ceiling,"Cap_3",(20.75f,18f),(1.5f,3f)),
                E(SoloRoomElementKind.Ceiling,"Roof_3b",(24.75f,17.5f),(6.5f,4f)),
                // The chimney (x 28-30): Hang_C (x 27-28, under the roof's east end, down to y 13.3) with moss on its east face, and
                // Wall_C (x 30-32, from the walk to y 15.5) with moss on its west face. The door stands on Wall_C's top, under Roof_E.
                E(SoloRoomElementKind.Wall,"Hang_C",(27.375f,14.4f),(.75f,2.2f)),
                E(SoloRoomElementKind.GripWall,"Hang_Moss",(27.875f,14.4f),(.25f,2.2f)),
                E(SoloRoomElementKind.GripWall,"Wall_Moss",(30.125f,12.75f),(.25f,3.5f)),
                E(SoloRoomElementKind.Wall,"Wall_C_Hi",(30.125f,15f),(.25f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_C",(31.125f,13.25f),(1.75f,4.5f)),
                E(SoloRoomElementKind.Ceiling,"Roof_E",(30f,18.75f),(4f,1.5f)),
            };

            // ---- Field (S1) ----
            // Hinge_0, the ground behind the start (x 0-1.5, hinged at its west end), stands up 30 ticks into the room and slides 4 u
            // east to the trench's lip: a cat that waits is pushed in. D-144 (the developer: "all these traps need to always be
            // faster"): every hinge floor here swings up in 3 ticks and slides faster than a run. The start's cut (x 0-3.4, up to S2) also sets the launchers off.
            var start = ((float, float))(1.7f, 2.5f); var startSize = ((float, float))(3.4f, 5f);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_0",(.75f,-.25f),(1.5f,.5f),start,startSize,new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(3.9f,0f),moveTicks:16,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // V1-V4 hang from S2's underside into the trench. V1 snaps 50 ticks after the cat takes it; each next one 50 after the
            // one before (the wave).
            const float v1Y = 2.025f, v1H = 5.85f;
            elements.Add(E(SoloRoomElementKind.Vine,"V1",(5.6f,v1Y),(.6f,v1H),settings:new SoloRoomTrapSettings(delayTicks:V1Snap)));
            elements.Add(E(SoloRoomElementKind.Vine,"V2",(8.4f,v1Y),(.6f,v1H),settings:Chain("V1", Wave1)));
            elements.Add(E(SoloRoomElementKind.Vine,"V3",(11.2f,v1Y),(.6f,v1H),settings:Chain("V2", Wave1)));
            elements.Add(E(SoloRoomElementKind.Vine,"V4",(14.2f,v1Y),(.6f,v1H),settings:Chain("V3", Wave1)));
            // The launchers fire as the cat comes into the trench, then never stop. Arrow_1Hi flies east from Lintel_W1 across the vines at y 2.6: a
            // leap from higher than about 0.6 crosses its lane. Arrow_1Lo flies west along the trench just under the walk line: the
            // way out under Pier_1 (from y 1.6) is a leap from low on V4, through its lane.
            var field1 = ((float, float))(10.25f, 1f); var field1Size = ((float, float))(11.5f, 3.6f);
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1Lo",(16.25f,-.6f),(.5f,.4f),field1,field1Size,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,-.6f,4.5f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:LoDelay1,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:LoCool1))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1Hi",(4.25f,2.6f),(.5f,.4f),field1,field1Size,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,2.6f,16f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:HiDelay1,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:HiCool1))));
            // Lift_1, the walk at x 22-24, shoots 3.35 u up into trench 2's bed on its own beat (every 120 ticks, up over 8, held 30,
            // down over 12): a cat on it is crushed. Wait short of it, cross once it's down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(23f,-.25f),(2f,.5f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Bed_2"),
                new SoloRoomTrapSettings(offset:new Vector2(0f,3.35f),moveTicks:8,holdTicks:30,returnTicks:12,repeatMode:TrapRepeatMode.Periodic,periodTicks:120,phaseTicks:40,cooldownTicks:50,movingKind:MovingTrapKind.Solid))));
            // Collapse_1 over Pit_1 gives way 6 ticks after a touch: jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_1",(27.6f,-.5f),(2.2f,1f),settings:new SoloRoomTrapSettings(delayTicks:6)));
            // V_Up1, the east shaft's vine, from the ground to a cat's height over Landing_2.
            elements.Add(E(SoloRoomElementKind.Vine,"V_Up1",(30.75f,3.05f),(.6f,6.1f)));

            // ---- Ledges (S2) ----
            // V5-V7 hang from S3_E's underside into trench 2 (x 18-27); the wave starts 60 ticks after the cat takes V5, 60 apart.
            const float v2Y = 7.625f, v2H = 5.65f;
            elements.Add(E(SoloRoomElementKind.Vine,"V5",(25.4f,v2Y),(.6f,v2H),settings:new SoloRoomTrapSettings(delayTicks:V5Snap)));
            elements.Add(E(SoloRoomElementKind.Vine,"V6",(22.6f,v2Y),(.6f,v2H),settings:Chain("V5", Wave2)));
            elements.Add(E(SoloRoomElementKind.Vine,"V7",(19.8f,v2Y),(.6f,v2H),settings:Chain("V6", Wave2)));
            // Arrow_2Lo flies east along trench 2 from Pier_1, just under the walk line; Arrow_2Hi west from Cap_E2 at y 8.1. Both start
            // as the cat walks off Landing_2 (its cut, x 27.4-27.8).
            var land2 = ((float, float))(22.5f, 6.75f); var land2Size = ((float, float))(9f, 3.1f);
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2Lo",(17.75f,5.4f),(.5f,.4f),land2,land2Size,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.4f,27f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:LoDelay2,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:LoCool2))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2Hi",(27.25f,8.1f),(.5f,.4f),land2,land2Size,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,8.1f,18f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:HiDelay2,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:HiCool2))));
            // Lift_2, S2 at x 11.7-13.9, shoots 3.35 u up into trench 3's bed 24 ticks after a cat walking west comes to x 15.6 (held 20,
            // down over 15): a running cat is on it as it goes. Stop short, cross once it's down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(12.8f,5.25f),(2.2f,.5f),(15.4f,7.25f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Bed_3"),
                new SoloRoomTrapSettings(delayTicks:Lift2Delay,offset:new Vector2(0f,3.35f),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Hinge_3, S2 at x 6.3-7.8 (hinged at its east end), stands up behind a cat walking west past x 5.3 (off it) and slides 2.5 u west:
            // a cat that stops is pushed onto Spikes_2. Jump them.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(7.05f,5.25f),(1.5f,.5f),(5.1f,7.25f),(.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(-2.5f,0f),moveTicks:10,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // V_Up2, the west shaft's vine, from S2 to a cat's height over S3_Land.
            elements.Add(E(SoloRoomElementKind.Vine,"V_Up2",(1.25f,8.55f),(.6f,6.1f)));

            // ---- Roof (S3) ----
            const float v3Y = 12.9f, v3H = 5.1f;
            elements.Add(E(SoloRoomElementKind.Vine,"V8",(5.6f,v3Y),(.6f,v3H),settings:new SoloRoomTrapSettings(delayTicks:V8Snap)));
            elements.Add(E(SoloRoomElementKind.Vine,"V9",(8.4f,v3Y),(.6f,v3H),settings:Chain("V8", Wave3)));
            elements.Add(E(SoloRoomElementKind.Vine,"V10",(11.2f,v3Y),(.6f,v3H),settings:Chain("V9", Wave3)));
            elements.Add(E(SoloRoomElementKind.Vine,"V11",(14f,v3Y),(.6f,v3H),settings:Chain("V10", Wave3)));
            // Slam_10, the roof over V10 (x 10.6-11.8), slams 4.6 u down V10's column 6 ticks after the cat comes into the trench's
            // middle (x 8.1-12, as it leaps for V9) and goes back up: a cat on V10, or leaping to it, as it comes down is killed. Wait
            // on V9 until it's back.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_10",(11.2f,16f),(1.2f,1f),(10.05f,13.3f),(3.9f,7.6f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Cap_10",slams:true),
                new SoloRoomTrapSettings(delayTicks:SlamDelay,offset:new Vector2(0f,-4.6f),moveTicks:10,holdTicks:4,returnTicks:20,movingKind:MovingTrapKind.Solid))));
            // Spikes_P come up on the Perch's top as a cat comes over it (its cut, from the top to the roof).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_P",(9.8f,11.15f),(1f,.3f),(9.8f,13.25f),(1f,4.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Slam_3, the roof at x 20-21.5, slams 4.5 u onto the walk as a cat walking east comes to x 19, and stays: stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3",(20.75f,16f),(1.5f,1f),(20.35f,14.05f),(2.3f,6.1f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(0f,-4.5f),moveTicks:10,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Hinge_5, S3_E at x 23-24.5 (spiked), stands up behind a cat past x 25.5 and slides 1 u east: a cat that stops at
            // Spikes_5 (x 26-26.9) is caught. Jump them at once. D-144 (the developer: "The spikes here should be on the opposite
            // side"): hinged at its east end, so its spiked face looks east at the cat it chases (hinged at the west end it looked
            // back west); it stops 0.5 u short of Spikes_5, clear of a cat hanging on V_Lure. 7 ticks from the swing to the kill.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_5",(23.75f,10.75f),(1.5f,.5f),(25.7f,13.25f),(.4f,4.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(1f,0f),moveTicks:7,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // V_Lure hangs from the roof over Spikes_5 like the way over them, and snaps 12 ticks after a cat comes under it (its
            // trigger runs down to the walk, so no way to the door passes it unfired): a cat that climbs it falls onto them (dead end).
            elements.Add(E(SoloRoomElementKind.Vine,"V_Lure",(26.45f,13.925f),(.6f,3.05f),(26.45f,13.25f),(.6f,4.5f),new SoloRoomTrapSettings(delayTicks:12)));

            var sections = new[] {
                CheckpointSection.Start("Field", new Vector2(2.5f, S1), new[] { "Hinge_0", "V1", "V2", "V3", "V4", "Arrow_1Lo", "Arrow_1Hi", "Lift_1", "Collapse_1", "V_Up1" }),
                new CheckpointSection("Ledges", new Vector2(28.9f, S2), new Rect(28.1f, S2, .2f, 2.2f),
                    new[] { "V5", "V6", "V7", "Arrow_2Lo", "Arrow_2Hi", "Lift_2", "Hinge_3", "V_Up2" }),
                new CheckpointSection("Roof", new Vector2(3.6f, S3), new Rect(0f, 10.5f, 2.5f, .2f),
                    new[] { "V8", "V9", "V10", "V11", "Slam_10", "Spikes_P", "Slam_3", "Hinge_5", "V_Lure" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 26.5f, 28.7f, "Walk_1b", "Walk_1c", "Pit_1_Bottom", "Pit_1") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A chained trap: `delay` ticks after its source fires.
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);
    }
}
