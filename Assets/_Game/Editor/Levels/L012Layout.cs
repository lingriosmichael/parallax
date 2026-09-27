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
    //    (+40), and Spikes_D rise where the hop lands (+55). Every right answer has to be made mirrored; the unmirrored
    //    instinct (back off) walks under Block_E; the flip ends on the run east (switch hands). In hall 3, Orb_B under the
    //    Shelf opens Collapse_F one step ahead (4 u, too wide): go back out from under the Shelf, jump onto it, and cross
    //    above.
    //  - Handoff (section 2, halls 2 and 1): Orb_G's inversion ends mid-air over Pit_H (the level's one precision beat):
    //    Orb_B's flip carries you through Orb_G; jump mirrored, switch hands on the last blink. In hall 1, jump PK; Inv_H
    //    (unseen, 30 ticks) flips you just past it (keep holding left and you run back into PK); then the door backs away
    //    west, past FakeFloor_D: jump over it.
    //  - Dead ends: the Slot (from hall 5 onto Slot_Floor, which gives way into Pit_S); the Nook at hall 3's west end
    //    (hidden spikes).
    static class L012Layout
    {
        const float SlabH = 2f;
        // §14 R2: no inversion is waited out. Inv_1 lasts until the cat is over P3; Orb_A until the run to hall 3's hole.
        const int InvDuration = 120, OrbADuration = 140;

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
            Slab(elements, 5, "F5_A", 3.2f, 4.7f); Slab(elements, 5, "F5_B", 6.7f, 14.4f); Pit(elements, 5, "P3", 14.4f, 15.9f);
            Slab(elements, 5, "F5_C", 15.9f, 16.9f); Pit(elements, 5, "P2", 16.9f, 18.9f); Slab(elements, 5, "F5_D", 18.9f, 20f);
            Pit(elements, 5, "P1", 20f, 24.5f); Slab(elements, 5, "F5_E", 24.5f, 32f);
            // Hall 4 (floor 15): Collapse_B's pit x 10.1-11.6, the drop hole x 29-31.
            Slab(elements, 4, "F4_A", 3.2f, 10.1f); Pit(elements, 4, "PB", 10.1f, 11.6f); Slab(elements, 4, "F4_B", 11.6f, 29f); Slab(elements, 4, "F4_C", 31f, 32f);
            elements.Add(E(SoloRoomElementKind.Wall,"Stub_C",(16.25f,16.85f),(.5f,2.3f)));   // hosts Arrow_C
            // Hall 3 (floor 10): the Nook (x 3.7-6.7), the drop hole x 6.7-8.7, Collapse_F's pit x 14-18 under the Shelf (x 14-20, top 11.5).
            Slab(elements, 3, "F3_A", 3.2f, 6.7f); Slab(elements, 3, "F3_B", 8.7f, 14f); Pit(elements, 3, "PF", 14f, 18f); Slab(elements, 3, "F3_C", 18f, 32f);
            elements.Add(E(SoloRoomElementKind.Floor,"Shelf",(17f,11.25f),(6f,.5f)));
            // Hall 2 (floor 5): Pit_H x 17-19.5, the drop hole x 29-31.
            Slab(elements, 2, "F2_A", 3.2f, 17f); Pit(elements, 2, "PH", 17f, 19.5f); Slab(elements, 2, "F2_B", 19.5f, 29f); Slab(elements, 2, "F2_C", 31f, 32f);
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
            elements.Add(E(SoloRoomElementKind.Floor,"Ground_B",(24.75f,-1.5f),(14.5f,3f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(7.6f,.75f),(.6f,1.5f)));

            // Mirror. T1: Inv_1 (honest, a pillar no jump clears) on Floor_1; Spikes_Back behind it. T2: Floor_1 and Floor_2 give
            // way under a cat that stops.
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_Back",(25.5f,20.15f),(1f,.3f)));
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_1",(23f,21.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(InvDuration),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_1",(23f,19.25f),(3f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:55)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_2",(17.9f,19.25f),(2f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:40)));

            // Halls. Orb_A (re-arms) sets the room off. Its trigger box runs from its face (the only side the cat reaches it
            // from) to x 14.4 and holds Collapse_B's top and Spikes_D, which rise where the hop lands; Block_E sits flush in
            // hall 5's slab behind the orb (within 3 u of it).
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_A",(9.3f,16.5f),(.6f,3f),(11.7f,16.5f),(5.4f,3f),new SoloRoomTrapSettings(new InverterSettings(OrbADuration),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Rearm))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_B",(10.85f,14.25f),(1.5f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:10,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A")));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_C",(16.25f,15.9f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,15.9f,3.7f,unitsPerTick:1f,tellTicks:10,disguised:true),new SoloRoomTrapSettings(delayTicks:40,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A"))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(13.65f,15.15f),(1.3f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:55,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A")));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_E",(7.6f,18.5f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:8,unitsPerTick:.36f,travelDistance:3f,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_A")));
            // Orb_B under the Shelf, its trigger box over Collapse_F (one step ahead of it) and under the Shelf.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_B",(18.5f,10.5f),(.6f,1f),(16.4f,10.5f),(4.8f,1f),new SoloRoomTrapSettings(new InverterSettings(230),new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Rearm))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_F",(16f,9.25f),(4f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:8,triggerSource:TrapTriggerSource.Chain,chainSource:"Orb_B")));
            // Dead end: the Nook at hall 3's west end.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(4.45f,10.15f),(1.5f,.3f),(5.2f,11.5f),(3f,3f),new SoloRoomTrapSettings(revealDelayTicks:6)));

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
                O(SoloRoomOpeningKind.Pit, 10.1f, 11.6f, "F4_A", "F4_B", "PB_Bottom", "PB"),
                O(SoloRoomOpeningKind.Pit, 14f, 18f, "F3_B", "F3_C", "PF_Bottom", "PF"),
                O(SoloRoomOpeningKind.Pit, 17f, 19.5f, "F2_A", "F2_B", "PH_Bottom", "PH"),
                O(SoloRoomOpeningKind.Pit, 1.4f, 3.2f, "Slot_W", "Pit_S_E", "Pit_S_Bottom", "Pit_S"),
                O(SoloRoomOpeningKind.Pit, 5.2f, 7f, "Door_Floor", "Ground_A", "PD_Bottom", "PD"),
                O(SoloRoomOpeningKind.Pit, 16f, 17.5f, "Ground_A", "Ground_B", "PK_Bottom", "PK"),
            };
            var jumps = new[] {
                new RequiredJump("PH_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,16.4f,19.3f,5f,5f,2f,sourceName:"F2_A",destinationName:"F2_B"),
                new RequiredJump("PD_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,7.5f,4.7f,0f,0f,1.5f,sourceName:"Ground_A",destinationName:"Door_Floor"),
                new RequiredJump("PK_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,18f,15.5f,0f,0f,1.5f,sourceName:"Ground_B",destinationName:"Ground_A"),
            };
            var precision = new[] { new PrecisionSection("Handoff_Gap", Rect.MinMaxRect(15f, 5f, 21.5f, 8f)) };
            var sections = new[] {
                CheckpointSection.Start("Mirror", new Vector2(28f, 20f), new[] { "Inv_1", "Floor_1", "Floor_2" }),
                new CheckpointSection("Halls", new Vector2(5.6f, 15f), new Rect(6.8f, 15f, .2f, 3f), new[] { "Orb_A", "Collapse_B", "Arrow_C", "Spikes_D", "Block_E", "Orb_B", "Collapse_F", "Spikes_D2" }),
                new CheckpointSection("Handoff", new Vector2(9.6f, 5f), new Rect(8.8f, 5f, .2f, 3f), new[] { "Orb_G", "Inv_H", "FakeFloor_D", "Retreat", "Slot_Floor" }),
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
