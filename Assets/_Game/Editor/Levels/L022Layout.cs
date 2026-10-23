using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-166: level 22, "Wall Runner" (D-163's second). The verb is CLIMB BETWEEN THE PUNCHES: three corridors stacked on stone
    // slabs (L016's), walked east, west, east, joined by two mossy chimneys. In each chimney a stone block punches across
    // the shaft on a beat and crushes a cat climbing past it against the far wall: wait at the foot, then climb as it pulls
    // back.
    //  - Works (section 0, corridor 1, walked east): Hinge_0 stands up behind the start and pushes (jump Spikes_0 at once);
    //    Slam_1 comes down out of the slab ahead (stop short, hop it); Collapse_1 gives way over Pit_1 (jump it); Lift_2 shoots
    //    up into the slab (stop, let it go up and down); into chimney 1, where Punch_1 punches west across the shaft.
    //  - Gallery (section 1, corridor 2, walked west): Hinge_2 stands up behind the cat and pushes it toward Spikes_2 (jump
    //    them); Collapse_2 gives way over Briar_2 (jump it); Slam_2 comes down ahead (stop short, hop it); into chimney 2,
    //    where Punch_2 punches east.
    //  - Crown (section 2, corridor 3, walked east): Hinge_F, spiked, stands up behind and chases (jump Spikes_3); Slam_F
    //    comes down out of the roof ahead (stop short, hop onto it); Collapse_3 gives way over Briar_3 (jump it); Pop_F
    //    springs up by the door (jump it).
    static class L022Layout
    {
        // The corridors' floors and ceilings (room-local y); every slab between them is 2 u of stone, the roof 2.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f, RoofTop = 19.5f, Base = -2.5f;
        // A chimney, from its corridor's floor: the moss under the punch's recess (MossLo to PunchLo), the recess (PunchLo to
        // PunchHi), the moss over it (PunchHi to the next floor); the far wall from HangFoot up, bare stone (the anvil) to
        // PunchHi, mossy over it.
        internal const float MossLo = 0f, PunchLo = 2.2f, PunchHi = 3.4f, HangFoot = 2f;
        // The punches: out across the shaft over PunchOut ticks, held, back; every PunchPeriod ticks.
        internal const int PunchOut = 10, PunchHold = 12, PunchBack = 12, PunchPeriod = 90, Punch1Phase = 0, Punch2Phase = 45;
        // The finale's chase: Hinge_F's stand, east.
        internal const float ChaseDistance = 4f;
        internal const int ChaseDelay = 10, ChaseTicks = 36;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.9f,F3 + .75f),(.6f,1.5f)),
            };

            // ---- Corridor 1's floor (y -2.5..0) ----
            // Hinge_0's notch (x 0-1.5), the start (x 1.5-9.5) with Spikes_0, the floor under Slam_1 (x 9.5-17), Pit_1 under
            // Collapse_1 (x 17-19), on to Lift_2's seat (x 23-24.5), and the floor into chimney 1 (x 24.5-30).
            Block(elements, SoloRoomElementKind.Floor, "Notch_0", 0f, 1.5f, Base, -.5f);
            Block(elements, SoloRoomElementKind.Floor, "Start_Floor", 1.5f, 9.5f, Base, F1);
            Spikes(elements, "Spikes_0", 5.5f, 6.5f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Floor_1a", 9.5f, 17f, Base, F1);
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit1_Bottom",(18f,-1.5f),(2f,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Pit_1",(18f,-.85f),(2f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            Block(elements, SoloRoomElementKind.Floor, "Floor_1b", 19f, 23f, Base, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 23f, 24.5f, Base, F1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Floor_1c", 24.5f, 30f, Base, F1);

            // ---- Slab A (y 4.5-6.5): corridor 1's ceiling, corridor 2's floor ----
            // Slam_1 is its bottom unit under Cap_S1; Briar_2's recess (x 13.5-15.5) under Collapse_2; SA_L over Lift_2 (its crush
            // partner); Hinge_2 its top unit on Seat_H2; it ends at Hang_1, chimney 1's west wall.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 12f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 12f, 13.5f, C1 + .75f, F2);
            Block(elements, SoloRoomElementKind.PitBottom, "Briar2_Floor", 13.5f, 15.5f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 15.5f, 23f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_L", 23f, 24.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H2", 24.5f, 26f, C1, F2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_4", 26f, 27.75f, C1, F2);

            // ---- Chimney 1 (x 28-29.75, corridor 1 up to corridor 2) ----
            // West: Anvil_1, bare stone at Punch_1's height (what it crushes against), and Hang_1 over it under SA_4's end, mossy
            // on its east face up to corridor 2's floor. East: Jamb_1, stone from the floor to corridor 2's ceiling, mossy on its
            // west face below and above Punch_1's recess (x 30-31, y 2.2-3.4).
            Block(elements, SoloRoomElementKind.Wall, "Anvil_1", 27.25f, 28f, F1 + HangFoot, F1 + PunchHi);
            Block(elements, SoloRoomElementKind.Wall, "Hang_1", 27.25f, 27.75f, F1 + PunchHi, C1);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_W1", 27.75f, 28f, F1 + PunchHi, F2);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_1lo", 30f, 32f, Base, F1 + PunchLo);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_1back", 31f, 32f, F1 + PunchLo, F1 + PunchHi);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_1hi", 30f, 32f, F1 + PunchHi, C2);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_E1lo", 29.75f, 30f, F1 + MossLo, F1 + PunchLo);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_E1hi", 29.75f, 30f, F1 + PunchHi, F2);

            // ---- Slab B (y 11-13): corridor 2's ceiling, corridor 3's floor ----
            // Hang_2 hangs from SB_0 (chimney 2's east wall); Hinge_F is its top unit on Seat_F; Slam_2 its bottom unit under
            // Cap_S2; Briar_3's recess (x 20-22) under Collapse_3.
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 4.25f, 6f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_F", 6f, 7.5f, C2, F3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 7.5f, 9f, C2 + .75f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 9f, 20f, C2, F3);
            Block(elements, SoloRoomElementKind.PitBottom, "Briar3_Floor", 20f, 22f, C2, F3 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 22f, 32f, C2, F3);

            // ---- Chimney 2 (x 2.25-4, corridor 2 up to corridor 3) ----
            // West: Jamb_2, stone from corridor 2's floor to the roof, mossy on its east face below and above Punch_2's recess
            // (x 1-2). East: Anvil_2, bare stone at Punch_2's height, and Hang_2 over it under SB_0, mossy on its west face up to
            // corridor 3's floor.
            Block(elements, SoloRoomElementKind.Wall, "Jamb_2lo", 0f, 2f, F2, F2 + PunchLo);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_2back", 0f, 1f, F2 + PunchLo, F2 + PunchHi);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_2hi", 0f, 2f, F2 + PunchHi, C3);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_W2lo", 2f, 2.25f, F2 + MossLo, F2 + PunchLo);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_W2hi", 2f, 2.25f, F2 + PunchHi, F3);
            Block(elements, SoloRoomElementKind.Wall, "Anvil_2", 4f, 4.75f, F2 + HangFoot, F2 + PunchHi);
            Block(elements, SoloRoomElementKind.Wall, "Hang_2", 4.25f, 4.75f, F2 + PunchHi, C2);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_E2", 4f, 4.25f, F2 + PunchHi, F3);

            // ---- The roof (y 17.5-19.5): Slam_F is its bottom unit under Cap_F ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 15.5f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_F", 15.5f, 17f, C3 + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 17f, 32f, C3, RoofTop);

            // Honest spikes: Spikes_2 on corridor 2's floor, Spikes_3 on corridor 3's, and the briars in the recesses.
            Spikes(elements, "Spikes_2", 20f, 21.2f, F2);
            Spikes(elements, "Briar_2", 13.5f, 15.5f, F2 - 1f);
            Spikes(elements, "Spikes_3", 10f, 11.2f, F3);
            Spikes(elements, "Briar_3", 20f, 22f, F3 - 1f);

            // ---------- Works (corridor 1) ----------
            // Hinge_0, the start's west end (x 0-1.5, hinged at its east end, spiked: its spikes face the cat), stands up 30 ticks
            // into the room (its cut is the start) and slides 4 u east to Spikes_0's edge in 16 ticks: jump them at once (L020's).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_0",(.75f,-.25f),(1.5f,.5f),(1.7f,1.75f),(3.4f,3.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(4f,0f),moveTicks:16,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Slam_1 slams 4.5 u onto the floor 9 ticks after a cat walking east comes into its cut (x 10.3-10.7), and stays: a
            // cat that runs on is under it; stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(12.75f,C1 + .375f),(1.5f,.75f),(10.5f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,-C1),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Collapse_1 fills Pit_1's mouth and gives way 4 ticks after a touch: jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_1",(18f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // Lift_2 (x 23-24.5) shoots 4.5 u up into SA_L 15 ticks after a cat walking east crosses x 21.6: a cat that runs on is
            // on it. Stop short, let it go up and down, cross.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(23.75f,F1 - .5f),(1.5f,1f),(21.6f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_L"),
                new SoloRoomTrapSettings(delayTicks:15,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Punch_1 fills its recess in Jamb_1 (x 30-31) and punches 2 u west across the shaft to Moss_W1 every PunchPeriod
            // ticks: a cat at its height (on either wall, or jumping between them) is crushed against Anvil_1.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Punch_1",(30.5f,F1 + (PunchLo + PunchHi) * .5f),(1f,PunchHi - PunchLo),settings:Punch("Anvil_1", -2f, Punch1Phase)));

            // ---------- Gallery (corridor 2) ----------
            // Hinge_2, SA's top unit at x 24.5-26 (hinged at its west end), stands up behind a cat walking west past x 23.6 and
            // slides 3.3 u west to Spikes_2's edge in 17 ticks: a cat that stops short of them is shoved onto them. Its wait lets
            // a cat that runs on jump them clear of it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(25.25f,F2 - .25f),(1.5f,.5f),(23.6f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:10,offset:new Vector2(-3.3f,0f),moveTicks:17,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Collapse_2 fills Briar_2's recess (over its spikes) and gives way 4 ticks after a touch: jump it (Collapse_1's).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_2",(14.5f,F2 - .5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // Slam_2, SB's bottom unit at x 7.5-9, slams 4.5 u onto SA 9 ticks after a cat walking west comes into its cut
            // (x 10-10.4), and stays: stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(8.25f,C2 + .375f),(1.5f,.75f),(10.2f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F2 - C2),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Punch_2 fills its recess in Jamb_2 (x 1-2) and punches 2 u east across the shaft to Anvil_2, half a beat after Punch_1.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Punch_2",(1.5f,F2 + (PunchLo + PunchHi) * .5f),(1f,PunchHi - PunchLo),settings:Punch("Anvil_2", 2f, Punch2Phase)));

            // ---------- Crown (corridor 3) ----------
            // Hinge_F, SB's top unit at x 6-7.5 (hinged at its east end, spiked, so its spikes face the cat), stands up behind a
            // cat walking east past x 9 and slides ChaseDistance east to Spikes_3's far edge: a cat that stops short of them is caught.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_F",(6.75f,F3 - .25f),(1.5f,.5f),(9f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:ChaseDelay,offset:new Vector2(ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Slam_F, the roof's bottom unit at x 15.5-17, slams 4.5 u onto the floor 9 ticks after a cat walking east comes into its
            // cut (x 14.4-14.8), and stays: stop short, hop onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F",(16.25f,C3 + .375f),(1.5f,.75f),(14.6f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Collapse_3 fills Briar_3's recess and gives way 4 ticks after a touch: jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_3",(21f,F3 - .5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            // Pop_F: spikes in the floor at x 27-28.5 spring up as the cat comes within 1.5 u of them: jump them, into the door.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_F",(27.75f,F3 + .15f),(1.5f,.3f),(25.75f,(F3 + C3) * .5f),(1f,C3 - F3),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Each gate lies across its chimney's shaft near the top: the only way up to the checkpoint passes through it.
            var sections = new[] {
                CheckpointSection.Start("Works", new Vector2(3f, F1), new[] { "Hinge_0", "Slam_1", "Collapse_1", "Lift_2", "Punch_1" }),
                new CheckpointSection("Gallery", new Vector2(26.9f, F2), new Rect(28f, F2 - 1.1f, 1.75f, .2f),
                    new[] { "Hinge_2", "Collapse_2", "Slam_2", "Punch_2" }),
                new CheckpointSection("Crown", new Vector2(5.1f, F3), new Rect(2.25f, F3 - 1.1f, 1.75f, .2f),
                    new[] { "Hinge_F", "Slam_F", "Collapse_3", "Pop_F" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 17f, 19f, "Floor_1a", "Floor_1b", "Pit1_Bottom", "Pit_1") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A punch: a solid that shoves a cat in its way across the shaft and crushes it against `partner`, out over PunchOut
        // ticks, held PunchHold, back over PunchBack, every PunchPeriod (its first punch starts at room tick `phase`).
        static SoloRoomTrapSettings Punch(string partner, float dx, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Slip, pushes: true, crushPartner: partner),
                new SoloRoomTrapSettings(offset: new Vector2(dx, 0f), moveTicks: PunchOut, holdTicks: PunchHold, returnTicks: PunchBack,
                    repeatMode: TrapRepeatMode.Periodic, periodTicks: PunchPeriod, phaseTicks: phase, cooldownTicks: PunchOut + PunchHold + PunchBack,
                    movingKind: MovingTrapKind.Solid));
    }
}
