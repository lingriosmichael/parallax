using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 14, "Root System" (vines). The verb is GO DOWN: every vine says "climb", and the door is at the
    // bottom. From the top storey (40) down through the Trunk storey (28), the Low storey (16) and the Canopy (6) to the door
    // at the bottom right: walked left, right, left, right.
    //  - Drop (section 0): the Top ends at a lip over Pit_1. Walking off it, the cat falls into Pit_1 unless it catches V1 on
    //    the way down. V1 snaps 60 ticks after the first touch, onto Spikes_V1 (hidden on the pit floor under it, shown by the
    //    snap): leap back under the lip to V2 and climb down to the Trunk. Rest_D2, beside V2, is a dead end (hidden spikes).
    //  - Trunk (section 1): east past V_Up (the lit alcove's vine, a dead end that snaps and drops the cat back) to V3, down
    //    the east shaft. Climbing into V3's lower half sets off Spear_4 from Wall_R across the shaft at body height: climb back
    //    up out of its lane, let it stick in Low_E, and leap onto it; a leap before it has stuck falls into Pit_3, and Shaft_W
    //    stops a leap from higher up. West along the Low storey: Spear_L sweeps it at body height, and the Trough is the one
    //    place under its lane; then to the Knot: Shrink (the last 3 u) narrows from the side the
    //    cat came in, so the cat can't stand and choose. V4 (near) holds; V5 (far, T2's lesson) snaps over Pit_K. Waiting on
    //    Shrink drops the cat onto Spikes_K on the lip's edge.
    //  - Canopy (section 2): from the lip, jump Spikes_K to C1. Reaching C1's column starts the wave: Block_7 falling onto
    //    C2's column onto the Perch under it (+20), C1 snaps (+35), C2 and spikes on the exit's near half (+45), C3 (+75),
    //    C4-C6 40 apart.
    //    Vine to vine faster than they fall, then far onto the exit and to the door.
    static class L014Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,22f),(1f,46f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,22f),(1f,46f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,44.5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(22f,40f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.8f,6.75f),(.6f,1.5f)),
                // Storeys: the Top (40, x 13-32), the Trunk (28, x 13-26), the Low storey (16, with Low_E at 17 by the east
                // shaft), the Canopy's lip (6, x 6.2-9) and the Exit (6, x 24-32).
                E(SoloRoomElementKind.Floor,"Top",(23f,39.75f),(18f,.5f)),
                E(SoloRoomElementKind.Floor,"Trunk_Floor",(20.25f,27f),(11.5f,2f)),
                E(SoloRoomElementKind.Wall,"Shaft_E",(31.5f,19.25f),(1f,13.5f)),
                E(SoloRoomElementKind.Wall,"Shaft_W",(25f,23.5f),(1f,5f)),
                E(SoloRoomElementKind.Floor,"Low",(13.65f,14.75f),(5.7f,2.5f)),
                E(SoloRoomElementKind.Floor,"Trough",(17.5f,14.25f),(2f,1.5f)),
                E(SoloRoomElementKind.Floor,"Low_B",(19f,14.75f),(1f,2.5f)),
                E(SoloRoomElementKind.Wall,"Curb_L",(11.05f,16.4f),(.5f,.8f)),
                E(SoloRoomElementKind.Floor,"Low_E",(20.25f,15.25f),(1.5f,3.5f)),
                E(SoloRoomElementKind.Floor,"Lip",(7.6f,3f),(2.8f,6f)),
                E(SoloRoomElementKind.Floor,"Exit",(29f,3f),(6f,6f)),
                // The pits: Pit_1 under the Top's lip (its floor 25), Pit_3 at the foot of the east shaft (13.6, above the canopy's storey), Pit_K under
                // the Knot and the thorn pit under the canopy (3.5).
                E(SoloRoomElementKind.PitBottom,"Pit1_Floor",(7.25f,24.75f),(14.5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_1",(5.15f,25.15f),(10.3f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.Hazard,"Pit_1E",(13.1f,25.15f),(2.8f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"Pit3_Floor",(26.5f,13.35f),(11f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_3",(26f,13.75f),(10f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"PitK_Floor",(3.1f,1.75f),(6.2f,3.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_K",(3.1f,3.65f),(6.2f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"Thorn_Floor",(17.5f,1.75f),(17f,3.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_W",(10.05f,3.65f),(2.1f,.3f),hazardRole:Pit),
                // Under C1 the thorns are floor thorns (as lethal): the pit floor there is C1's storey floor for trigger coverage.
                E(SoloRoomElementKind.Hazard,"Thorns_C1",(11.4f,3.65f),(.6f,.3f)),
                E(SoloRoomElementKind.Hazard,"Thorns",(18.85f,3.65f),(14.3f,.3f),hazardRole:Pit),
            };

            // Drop. Lip_1, the Top's last 1 u, gives way 6 ticks after a touch: a cat that stops at the edge feels it go and
            // drops straight into Pit_1E (the Trunk starts at x 14.5); holding Down and steering right, it catches V2 below.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Lip_1",(13.5f,39.75f),(1f,.5f),settings:new SoloRoomTrapSettings(delayTicks:6)));
            // V1's trigger is its whole column (full storey, pit floor to its top), so every way past it sets off the
            // snap; Spikes_V1 show with the snap. Rest_D2's trigger covers the air it's leapt into from V2.
            elements.Add(E(SoloRoomElementKind.Vine,"V1",(11f,33.85f),(.6f,13.5f),(11f,34.5f),(1.4f,19f),new SoloRoomTrapSettings(delayTicks:60)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_V1",(11f,25.15f),(1.4f,.3f),settings:Chain("V1", 6)));
            elements.Add(E(SoloRoomElementKind.Vine,"V2",(14.6f,33.725f),(.6f,11.35f)));
            // Spear_V2 fires from the Beam (x 23.8) west across V2's lower half at y 32 and sticks at the west wall (x 0-1.4),
            // clear of every vine; the shaft is 1 thick (y 31.5-32.5). Its trigger holds the lane and the band above it (y
            // 31.5-33.4, below where a cat on V1 gets before V1 snaps), so a cat climbing down V2 sets it off about 14 ticks
            // before it reaches the lane: wait above it (the 22-tick tell), then climb on. A cat falling through the trigger (T1,
            // T2) is dead, and the room frozen, before the spear reaches the Pit_1 column.
            elements.Add(Spear("Spear_V2", 24.05f, 32f, ArrowDirection.Left, 0f, 1.4f, (11.9f, 32.45f), (23.8f, 1.9f), new SoloRoomTrapSettings(delayTicks:0), tell: 22, thickness: 1f));
            elements.Add(E(SoloRoomElementKind.Floor,"Rest_D2",(17.5f,33.75f),(4.2f,.5f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(17.5f,34.15f),(4.2f,.3f),(17.375f,33.75f),(4.45f,11.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Trunk. V_Up (the alcove's vine) snaps once the cat has climbed past its middle. Spear_4 fires across V3's lower
            // half and sticks in Low_E, its top level with Low_E's.
            elements.Add(E(SoloRoomElementKind.Vine,"V_Up",(21f,32.05f),(.6f,8f),(21f,30.5f),(.6f,1f),new SoloRoomTrapSettings(delayTicks:10)));
            elements.Add(E(SoloRoomElementKind.Floor,"Alcove",(22.6f,35.75f),(1.8f,.5f)));
            // The Beam hangs from the Top over the walk's east end; crossing the cut before it drops Block_T out of it onto the
            // walk (a bait: set it off, back off, let it land, hop it).
            elements.Add(E(SoloRoomElementKind.Ceiling,"Beam",(24.6f,35.25f),(1.6f,8.5f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_T",(24.6f,31.4f),(1.2f,.8f),(24.5f,33.75f),(3f,11.5f),new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.2f,travelDistance:3f)));
            elements.Add(E(SoloRoomElementKind.Vine,"V3",(28f,21.5f),(.6f,13.8f)));
            elements.Add(Spear("Spear_4", 31.25f, 16.8f, ArrowDirection.Left, 21f, 4f, (26f, 17.3f), (10f, 1.4f), new SoloRoomTrapSettings(delayTicks:0), tell: 12));
            // Spear_L sweeps the Low walk at body height from Low_E once the cat is on it, and sticks in Curb_L: the one place
            // below its lane is the Trough.
            elements.Add(Spear("Spear_L", 19.75f, 16.35f, ArrowDirection.Left, 11.3f, 1.4f, (15.4f, 16.35f), (8.2f, .4f), new SoloRoomTrapSettings(delayTicks:0), tell: 16));

            // The Knot: Shrink narrows from its east edge 5 ticks after a touch, over 50 ticks. V4 holds; V5 snaps.
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Shrink",(9.3f,15.75f),(3f,.5f),settings:new SoloRoomTrapSettings(new ShrinkSettings(50, 0f, ShrinkFrom.Right), new SoloRoomTrapSettings(delayTicks:5))));
            elements.Add(E(SoloRoomElementKind.Vine,"V4",(6.8f,11.3f),(.6f,10.5f)));
            elements.Add(E(SoloRoomElementKind.Vine,"V5",(3f,11.05f),(.6f,11f),settings:new SoloRoomTrapSettings(delayTicks:15)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_K",(8.25f,6.15f),(1.5f,.3f)));

            // Canopy. C2 is short, over the Perch; Block_7 sits flush in the Low floor's underside over C2 and lands on the Perch.
            // C1's trigger is its whole column, under the Low floor; the wave chains from it. Block_7 sits flush in the Low
            const float cY = 8.6f, cH = 6f;
            var c1 = ((float, float))(11.4f, 8.5f); var c1Size = ((float, float))(.6f, 10f);
            elements.Add(E(SoloRoomElementKind.Vine,"C1",(11.4f,cY),(.6f,cH),c1,c1Size,new SoloRoomTrapSettings(delayTicks:35)));
            elements.Add(E(SoloRoomElementKind.Vine,"C2",(14f,9.3f),(.6f,4.6f),settings:Chain("C1", 10)));
            elements.Add(E(SoloRoomElementKind.Floor,"Perch",(14f,6.25f),(1.2f,.5f)));
            elements.Add(E(SoloRoomElementKind.Vine,"C3",(16.6f,cY),(.6f,cH),settings:Chain("C2", 30)));
            elements.Add(E(SoloRoomElementKind.Vine,"C4",(19.2f,cY),(.6f,cH),settings:Chain("C3", 40)));
            elements.Add(E(SoloRoomElementKind.Vine,"C5",(21.8f,cY),(.6f,cH),settings:Chain("C4", 40)));
            elements.Add(E(SoloRoomElementKind.Vine,"C6",(24.4f,cY),(.6f,cH),settings:Chain("C5", 40)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_7",(14f,13.9f),(1.2f,.8f),c1,c1Size,new SoloRoomTrapSettings(delayTicks:20,unitsPerTick:.2f,travelDistance:7f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_8",(27.25f,6.15f),(2.5f,.3f),settings:Chain("C1", 10)));

            var sections = new[] {
                CheckpointSection.Start("Drop", new Vector2(22f, 40f), new[] { "Lip_1", "V1", "Spikes_V1", "V2", "Spear_V2", "Spikes_D2" }),
                new CheckpointSection("Trunk", new Vector2(16f, 28f), new Rect(14.1f, 30f, 1f, .2f), new[] { "V_Up", "Block_T", "V3", "Spear_4", "Spear_L", "Shrink", "V4", "V5" }),
                new CheckpointSection("Canopy", new Vector2(6.8f, 6f), new Rect(6.3f, 6.6f, 1f, .2f),
                    new[] { "C1", "C2", "C3", "C4", "C5", "C6", "Block_7", "Spikes_8" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, float length, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing, int tell = 8, float thickness = .4f) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (.5f, thickness), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX, length: length, thickness: thickness, tellTicks: tell), timing));
    }
}
