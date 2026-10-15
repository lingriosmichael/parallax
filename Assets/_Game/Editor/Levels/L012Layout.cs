using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 12, "Mirror, Mirror" (the inverter). The verb is UNLEARN YOUR HANDS: inversion stops being a
    // trap you wait out and becomes a state you plan around. Five halls stacked in a switchback (floors 20, 15, 10, 5, 0,
    // each hall 3 high over a 2-thick slab), walked west, east, west, east, west; the door is at the far west of the bottom
    // hall, and the Slot (x 1.4-3.2) is a shaft from hall 5 straight down into Pit_S.
    //  - Mirror (section 0, hall 5): hop Spikes_Back onto Floor_1, which gives way 55 ticks after a touch; Inv_1, a pillar
    //    you can't jump, flips your hands for 120 ticks. Keep holding left and you run back into Spikes_Back; wait it out and
    //    Floor_1 drops you into P1. Play it mirrored at once: jump P1, cross Floor_2 (it goes 40 ticks after a touch), jump P3,
    //    and switch hands as the flip ends.
    //  - Halls (section 1, halls 4 and 3): touching Orb_A (it re-arms) flips your hands for 140 ticks and sets the room off: Block_E
    //    drops behind you (+8), Collapse_B opens one step ahead (+10), Arrow_C tells at jump height from Stub_C ahead
    //    (+40), and Spikes_D rise a step past where the hop lands (+70). Every right answer has to be made mirrored; the unmirrored
    //    instinct (back off) walks into Block_E, which shoves it into PB (PAX-107); the flip ends on the run east (switch hands). In hall 3, Orb_B under the
    //    Shelf opens Collapse_F one step ahead (4 u, too wide): go back out from under the Shelf, jump onto it, and cross
    //    above.
    //  - Handoff (section 2, halls 2 and 1): Orb_G's inversion ends mid-air over Pit_H (the level's one precision beat):
    //    Orb_B's flip carries you through Orb_G; jump mirrored, switch hands on the last blink. In hall 1, jump PK; Inv_H
    //    (unseen, 30 ticks) flips you just past it (keep holding left and you run back into PK); then the door backs away
    //    west, past FakeFloor_D: jump over it.
    //  - PAX-107: Drop_3 falls from the ceiling in hall 3 (stop, let it land, hop it); Hinge_2 stands up behind the cat in
    //    hall 2 as Orb_G flips its hands and pushes a cat that stops into Pit_H; on hall 2's run east Slam_H2 comes down from
    //    the ceiling and Lift_H2 shoots up into it (stop short of each, round thirteen); Piston_1 rises out of hall 1's ground into
    //    the slab above as a cat runs onto it (wait for it to go back down). Block_E slides on after it lands, pushing a cat
    //    that stopped into PB; hall 4's run east gains Hinge_4 (stands up behind, pushes into P4) and Drop_4 (a block from
    //    the ceiling past P4); the Shelf's west half, Shelf_W, rises into hall 4's slab.
    //  - Dead ends: the Slot (from hall 5 onto Slot_Floor, which gives way into Pit_S); the Nook at hall 3's west end
    //    (hidden spikes).
    static class L012Layout
    {
        const float SlabH = 2f;
        // §14 R2: no inversion is waited out. Inv_1 lasts until the cat is over P3; Orb_A (re-armed as the hop over PB lands
        // in its box) until Drop_4 lands (PAX-107: 140 before).
        // PAX-107 round nineteen: Orb_A's flip ends as the cat crosses Hinge_4's notch (was 105, until Drop_4 landed); Orb_P,
        // just before P4, flips the hands again until Drop_4 lands.
        const int InvDuration = 120, OrbADuration = OrbADurationTicks, OrbPDuration = 60;
        internal const int OrbADurationTicks = 30;
        // PAX-107 round thirteen: hall 2's slam and lift, each timed onto a cat running east.
        const int SlamDelay = 15, LiftDelay = 11;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,10f),(1f,27f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,10f),(1f,27f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,23.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(28f,20f),(0f,0f)),
                // The Slot (x 1.4-3.2), a shaft from hall 5 into Pit_S: Slot_W west of it; Pit_S_E and Col4/3/2 between it and the halls.
                E(SoloRoomElementKind.Wall,"Slot_W",(.7f,9.5f),(1.4f,27f)),
                E(SoloRoomElementKind.Wall,"Col4",(3.45f,16.5f),(.5f,3f)),
                E(SoloRoomElementKind.Wall,"Col3",(3.45f,11.5f),(.5f,3f)),
                E(SoloRoomElementKind.Wall,"Col2",(3.45f,6.5f),(.5f,3f)),
            };
            // Hall 5 (floor 20): the drop hole x 4.7-6.7, P3 x 14.4-15.9, P2 under Floor_2 x 16.9-18.9, P1 x 20-21.5.
            // PAX-107: Block_E (x 6.7-7.7, under F5_Cap) is the drop hole's east side.
            Slab(elements, 5, "F5_A", 3.2f, 4.7f); Slab(elements, 5, "F5_B", 7.7f, 14.4f); Pit(elements, 5, "P3", 14.4f, 15.9f);
            elements.Add(E(SoloRoomElementKind.Floor,"F5_Cap",(7.2f,19.5f),(1f,1f)));
            Slab(elements, 5, "F5_C", 15.9f, 16.9f); Pit(elements, 5, "P2", 16.9f, 18.9f); Slab(elements, 5, "F5_D", 18.9f, 20f);
            Pit(elements, 5, "P1", 20f, 24.5f); Slab(elements, 5, "F5_E", 24.5f, 32f);
            // Hall 4 (floor 15): Collapse_B's pit x 10.1-12.3, the drop hole x 29-31. PAX-107 round thirteen (the developer: "if
            // you continue pressing forward you can get across without falling"): PB is 2.2 u wide (was 1.5), so a cat that
            // walks or is pushed off its lip drops in before it reaches the far side.
            // PAX-107: Hinge_4's notch x 17-18.5 (0.5 deep) and P4 x 21.5-23.
            Slab(elements, 4, "F4_A", 3.2f, 10.1f); Pit(elements, 4, "PB", 10.1f, 12.3f); Slab(elements, 4, "F4_B", 12.3f, 17f);
            elements.Add(E(SoloRoomElementKind.Floor,"F4_Notch",(17.75f,13.75f),(1.5f,1.5f))); Slab(elements, 4, "F4_B2", 18.5f, 21.5f);
            Pit(elements, 4, "P4", 21.5f, 23f); Slab(elements, 4, "F4_B3", 23f, 29f); Slab(elements, 4, "F4_C", 31f, 32f);
            // PAX-107 round twenty (the developer: "Move the arrow and hanging wall more to the right. There is no way to land
            // safely and jump across"): Stub_C is at x 16.4-16.9 (was 16-16.5), as far east as hall 5's slab over it reaches, so
            // a cat that jumps the risen Spikes_D (x 14-15.3) and meets its face drops onto the 1.1 u before it.
            elements.Add(E(SoloRoomElementKind.Wall,"Stub_C",(16.65f,16.85f),(.5f,2.3f)));   // hosts Arrow_C
            // Hall 3 (floor 10): the Nook (x 3.7-6.7), the drop hole x 6.7-8.7, Collapse_F's pit x 14-18 under the Shelf (x 14-20, top 11.5).
            Slab(elements, 3, "F3_A", 3.2f, 6.7f); Slab(elements, 3, "F3_B", 8.7f, 14f); Pit(elements, 3, "PF", 14f, 18f); Slab(elements, 3, "F3_C", 18f, 23f);
            // PAX-107 round thirteen: F3_C is cut at x 23-24.5 for Slam_H2 (hung in its underside), under F3_Cap, its crush partner.
            elements.Add(E(SoloRoomElementKind.Floor,"F3_Cap",(23.75f,9.5f),(1.5f,1f))); Slab(elements, 3, "F3_C2", 24.5f, 32f);
            // PAX-107: the Shelf's west half is Shelf_W, a lift.
            elements.Add(E(SoloRoomElementKind.Floor,"Shelf",(18.5f,11.25f),(3f,.5f)));
            // Hall 2 (floor 5): Pit_H x 17-19.5, the drop hole x 29-31. PAX-107 round thirteen: Lift_H2 is the slab's top unit
            // at x 26-27.5, on F2_Seat.
            // PAX-107: F2_A has a notch (x 8.5-10, 0.5 deep) that Hinge_2 lies in.
            Slab(elements, 2, "F2_A", 3.2f, 8.5f); Slab(elements, 2, "F2_A2", 10f, 17f); Pit(elements, 2, "PH", 17f, 19.5f);
            elements.Add(E(SoloRoomElementKind.Floor,"F2_Notch",(9.25f,3.75f),(1.5f,1.5f))); Slab(elements, 2, "F2_B", 19.5f, 26f); Slab(elements, 2, "F2_B3", 27.5f, 29f);
            elements.Add(E(SoloRoomElementKind.Floor,"F2_Seat",(26.75f,3.5f),(1.5f,1f))); Slab(elements, 2, "F2_C", 31f, 32f);
            // Hall 1 (floor 0): Pit_S at the Slot's foot, Door_Floor, FakeFloor_D over Pit_D, and the ground with the door on it.
            // Pit_S is a unit deeper than hall 1's other pits, so a cat dropping the Slot's 20 u sees Slot_Floor go at least
            // 6 ticks before it lands (the lead).
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_S_Bottom",(2.3f,-3.75f),(1.8f,.5f)));
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_S",new Vector2(2.3f,-3.35f),new Vector2(1.8f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            elements.Add(E(SoloRoomElementKind.Wall,"Pit_S_E",(3.45f,-.5f),(.5f,7f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Door_Floor",(4.45f,-1.5f),(1.5f,3f)));
            Pit(elements, 1, "PD", 5.2f, 7f);
            elements.Add(E(SoloRoomElementKind.Floor,"Ground_A",(11.5f,-1.5f),(9f,3f)));
            Pit(elements, 1, "PK", 16f, 17.5f);
            // PAX-107: Ground_B is cut at x 23.5-25 for Piston_1.
            elements.Add(E(SoloRoomElementKind.Floor,"Ground_B",(20.5f,-1.5f),(6f,3f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Ground_B2",(28.5f,-1.5f),(7f,3f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(7.6f,.75f),(.6f,1.5f)));

            // Mirror. T1: Inv_1 (honest, a pillar no jump clears) on Floor_1; Spikes_Back behind it. T2: Floor_1 and Floor_2 give
            // way under a cat that stops.
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_Back",(25.5f,20.15f),(1f,.3f)));
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_1",(23f,21.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(InvDuration),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_1",(23f,19.25f),(3f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:55)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_2",(17.9f,19.25f),(2f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:40)));

            // Halls. Orb_A (re-arms) sets the room off. Its trigger box runs from its face (the only side the cat reaches it
            // from) to x 14.4 and holds Collapse_B's top and Spikes_D's west end (they rise just past where the hop lands); Block_E sits flush in
            // hall 5's slab behind the orb (within 3 u of it).
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_A",(9.3f,16.5f),(.6f,3f),(11.7f,16.5f),(5.4f,3f),new SoloRoomTrapSettings(new InverterSettings(OrbADuration),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Rearm))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_B",(11.2f,14.25f),(2.2f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:10,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A")));
            // D-119 (the developer: all launchers shoot non-stop): Arrow_C keeps its first shot's trigger, then fires every 160 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_C",(16.65f,15.9f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,15.9f,3.7f,unitsPerTick:1f,tellTicks:10,disguised:true),new SoloRoomTrapSettings(delayTicks:40,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:160))));
            // PAX-107 round eighteen (the developer: "move these spikes 1 unit to the right. So there is space for the cat to land
            // and jumpover"): x 14-15.3, 1.7 u past PB's edge; they rise 70 ticks after Orb_A (was 55), once a cat that runs on
            // from the hop is past them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(14.65f,15.15f),(1.3f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:70,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A")));
            // PAX-107 (the developer: "when this wall falls, make it move right to push the cat towards the spikes"): Block_E
            // drops 3 u out of hall 5's slab, then slides 2.4 u east to PB's edge (a Solid moving along an L), and pushes a cat
            // that stopped at Orb_A into PB. PAX-107 round thirteen (the developer: "The pieces of the floor pushes slightly too
            // far ... The trick should be to jump back on the box"): it stops flush with PB's edge (it slid 0.2 u over the pit);
            // a cat that jumps onto it as it comes rides it out.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Block_E",(7.2f,18.5f),(1f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,riseFirst:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(2.4f,-3f),moveTicks:16,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A",movingKind:MovingTrapKind.Solid))));
            // Orb_B under the Shelf, its trigger box over Collapse_F (one step ahead of it) and under the Shelf. Its flip lasts
            // until Orb_G (PAX-107: 230 before Shelf_W's wait).
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_B",(18.5f,10.5f),(.6f,1f),(16.4f,10.5f),(4.8f,1f),new SoloRoomTrapSettings(new InverterSettings(295),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Rearm))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_F",(16f,9.25f),(4f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:8,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_B")));
            // Dead end: the Nook at hall 3's west end.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(4.45f,10.15f),(1.5f,.3f),(5.2f,11.5f),(3f,3f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // PAX-107 (the developer: "Level 12 to have some more traps, floors that go up and squish you, ceilings that fall, and
            // also portions of the floor that go vertical and push to a trap especially when directions are inverted"):
            // Drop_3, a block in hall 4's slab over hall 3 (x 23.5-25), drops 3 u onto hall 3's floor as a cat running west
            // comes up to it (its trigger is the hall's height at x 26.6-27): stop, let it land, hop it.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Drop_3",(24.25f,13.5f),(1.5f,1f),(26.8f,11.5f),(.4f,3f),new SoloRoomTrapSettings(delayTicks:14,unitsPerTick:.36f,travelDistance:3f)));
            // Hinge_2, hall 2's floor just past the Handoff checkpoint (moved to x 8.3; x 8.5-10, hinged at its west end): a cat past x 11.6 (the hall's
            // height to x 12) sets it off; it stands up behind the cat as Orb_G flips its hands and slides 8 u east to Pit_H's
            // edge, pushing a cat that stops into the pit. Keep running, mirrored.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(9.25f,4.75f),(1.5f,.5f),(11.8f,6.5f),(.4f,3f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(8f,0f),moveTicks:40,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Piston_1, hall 1's ground at x 23.5-25 (3.5 deep, so its foot stays in the ground): a cat running west past x 27.5
            // sets it off; 27 ticks later it shoots up 2.9 u in 6 ticks, into hall 2's slab, holds 40 and goes back down over 25.
            // A cat that runs on is on it as it rises; wait for it to come back down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_1",(24.25f,-1.75f),(1.5f,3.5f),(27.25f,1.5f),(.5f,3f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"F2_B"),
                new SoloRoomTrapSettings(delayTicks:27,offset:new Vector2(0f,2.9f),moveTicks:6,holdTicks:40,returnTicks:25,movingKind:MovingTrapKind.Solid))));

            // PAX-107 (the developer: "Add 2 traps in this section of the second floor"), hall 4's run east, played mirrored
            // (Orb_A's flip): Hinge_4 (x 17-18.5, hinged at its west end) stands up behind a cat past x 19.8 and slides 4 u east
            // to P4's edge, pushing a cat that stops into P4: keep running, jump P4. Drop_4, a block in hall 5's slab over x
            // 27-28.5, drops 3 u onto a cat that runs on past P4 (its trigger is the hall's height at x 24.8-25.2): stop, let
            // it land, hop it and down the hole.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_4",(17.75f,14.75f),(1.5f,.5f),(20f,16.5f),(.4f,3f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(4f,0f),moveTicks:20,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // PAX-107 round nineteen (the developer: "Make this wall stand up and move faster. And add another direction inverter
            // before the spikes"): Hinge_4 stands up in 3 ticks (was 6), 4 after the cat passes (was 6), and slides at 0.2 u a
            // tick (was 36 ticks for the 4 u), faster than a run. Orb_P, an honest orb pillar just before P4 (x 20.3-20.9), flips
            // the hands again (Orb_A's flip has just ended): a cat that keeps holding the same way runs back into Hinge_4, which
            // shoves it into P4. Switch hands at once, jump P4 mirrored.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_P",(20.6f,16.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(OrbPDuration),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Drop_4",(27.75f,18.5f),(1.5f,1f),(25f,16.5f),(.4f,3f),new SoloRoomTrapSettings(delayTicks:18,unitsPerTick:.36f,travelDistance:3f)));
            // PAX-107 (the developer: "Make the left half of the platform move up and attempt squishing the cat"): Shelf_W, the
            // Shelf's west half (x 14-17), rises 1.4 u into hall 4's slab 40 ticks after a cat jumps onto the Shelf (as a cat
            // that runs on is on it), holds 20 and goes back down over 15: wait on the Shelf for it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Shelf_W",(15.5f,11.25f),(3f,.5f),(18.5f,12.25f),(3f,1.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"F4_B"),
                new SoloRoomTrapSettings(delayTicks:40,offset:new Vector2(0f,1.4f),moveTicks:9,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));

            // PAX-107 round thirteen (the developer, on hall 2's run east: "Only 1 trap in this section, add more traps here"), two
            // traps that act on a cat running east from Pit_H to the drop hole: stop short of each, let it act, then pass.
            // Slam_H2, a block in hall 3's slab underside (x 23-24.5), slams 3 u onto hall 2's floor SlamDelay ticks after a cat
            // passes x 21.5 (the hall's height; clear of where the jump over Pit_H lands), holds 20 and goes back up over 15; a cat that rides it back up is crushed
            // against F3_Cap.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_H2",(23.75f,8.5f),(1.5f,1f),(21.7f,6.5f),(.4f,3f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"F3_Cap"),
                new SoloRoomTrapSettings(delayTicks:SlamDelay,offset:new Vector2(0f,-3f),moveTicks:6,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Lift_H2, hall 2's floor at x 26-27.5 (the slab's top unit, on F2_Seat), shoots 2.9 u up LiftDelay ticks after a
            // cat passes x 25.2, as a running cat is on it, into hall 3's slab (0.1 u under it), and stays: walk under it,
            // through the socket it leaves (1 u deep), and hop out.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_H2",(26.75f,4.5f),(1.5f,1f),(25.4f,6.5f),(.4f,3f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"F3_C2"),
                new SoloRoomTrapSettings(delayTicks:LiftDelay,offset:new Vector2(0f,2.9f),moveTicks:6,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            // Handoff. Orb_G's inversion ends while the cat is over Pit_H. The door backs away west, past FakeFloor_D.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_G",(12f,6.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(60),new SoloRoomTrapSettings(delayTicks:0))));
            // Hall 1: Inv_H (unseen, 30 ticks) waits just past PK: a cat that keeps holding left runs back into it.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_H",(13.5f,1.5f),(.4f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(30, disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"FakeFloor_D",(6.1f,-1.25f),(1.8f,2.5f)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Slot_Floor",(2.3f,-1.75f),(1.8f,3.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(9.2f,1.5f),(.4f,3f),settings:new SoloRoomTrapSettings(moveTicks:30,offset:new Vector2(-3.1f,0f))));

            var openings = new List<SoloRoomOpening> {
                O(SoloRoomOpeningKind.Pit, 14.4f, 15.9f, "F5_B", "F5_C", "P3_Bottom", "P3"),
                O(SoloRoomOpeningKind.Pit, 16.9f, 18.9f, "F5_C", "F5_D", "P2_Bottom", "P2"),
                O(SoloRoomOpeningKind.Pit, 20f, 24.5f, "F5_D", "F5_E", "P1_Bottom", "P1"),
                O(SoloRoomOpeningKind.Pit, 10.1f, 12.3f, "F4_A", "F4_B", "PB_Bottom", "PB"),
                O(SoloRoomOpeningKind.Pit, 21.5f, 23f, "F4_B2", "F4_B3", "P4_Bottom", "P4"),
                O(SoloRoomOpeningKind.Pit, 14f, 18f, "F3_B", "F3_C", "PF_Bottom", "PF"),
                O(SoloRoomOpeningKind.Pit, 17f, 19.5f, "F2_A2", "F2_B", "PH_Bottom", "PH"),
                O(SoloRoomOpeningKind.Pit, 1.4f, 3.2f, "Slot_W", "Pit_S_E", "Pit_S_Bottom", "Pit_S"),
                O(SoloRoomOpeningKind.Pit, 5.2f, 7f, "Door_Floor", "Ground_A", "PD_Bottom", "PD"),
                O(SoloRoomOpeningKind.Pit, 16f, 17.5f, "Ground_A", "Ground_B", "PK_Bottom", "PK"),
            };
            var jumps = new[] {
                new RequiredJump("PH_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,16.4f,19.3f,5f,5f,2f,sourceName:"F2_A2",destinationName:"F2_B"),
                new RequiredJump("PD_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,7.5f,4.7f,0f,0f,1.5f,sourceName:"Ground_A",destinationName:"Door_Floor"),
                new RequiredJump("PK_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,18f,15.5f,0f,0f,1.5f,sourceName:"Ground_B",destinationName:"Ground_A"),
            };
            var precision = new[] { new PrecisionSection("Handoff_Gap", Rect.MinMaxRect(15f, 5f, 21.5f, 8f)) };
            var sections = new[] {
                CheckpointSection.Start("Mirror", new Vector2(28f, 20f), new[] { "Inv_1", "Floor_1", "Floor_2" }),
                new CheckpointSection("Halls", new Vector2(5.6f, 15f), new Rect(6.8f, 15f, .2f, 3f), new[] { "Orb_A", "Collapse_B", "Arrow_C", "Spikes_D", "Block_E", "Orb_B", "Collapse_F", "Spikes_D2", "Drop_3", "Hinge_4", "Orb_P", "Drop_4", "Shelf_W" }),
                new CheckpointSection("Handoff", new Vector2(8.3f, 5f), new Rect(7.9f, 5f, .2f, 3f), new[] { "Orb_G", "Inv_H", "FakeFloor_D", "Retreat", "Slot_Floor", "Hinge_2", "Slam_H2", "Lift_H2", "Piston_1" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings.ToArray(), jumps, null, precision, null, sections);
        }

        // A hall's floor slab (2 thick) from x0 to x1, its top at the hall's floor (5 × (hall - 1)).
        static void Slab(List<SoloRoomElement> e, int hall, string name, float x0, float x1)
        {
            float top = 5f * (hall - 1);
            e.Add(E(SoloRoomElementKind.Floor, name, ((x0 + x1) * .5f, top - SlabH * .5f), (x1 - x0, SlabH)));
        }

        // A pit through a hall's slab: its bottom half a unit into the slab below the floor, with its hazard on it. Hall 1's
        // pit sinks into the ground block.
        static void Pit(List<SoloRoomElement> e, int hall, string name, float x0, float x1)
        {
            float top = 5f * (hall - 1), bottom = top - (hall == 1 ? 2.5f : 1.5f);
            e.Add(E(SoloRoomElementKind.PitBottom, name + "_Bottom", ((x0 + x1) * .5f, bottom - .25f), (x1 - x0, .5f)));
            e.Add(new SoloRoomElement(SoloRoomElementKind.Hazard, name, new Vector2((x0 + x1) * .5f, bottom + .15f), new Vector2(x1 - x0, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
        }
    }
}
