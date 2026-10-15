using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-149): level 19, "Muscle Memory" (the inverter and spears), rebuilt (the developer: make more use of the
    // direction inverters; the old one was 64 u wide, with 1 u platforms over a 22 u pit). The verb is still EXECUTE a
    // memorised input map: every trap is set off by the cat itself, so the map is the same on every attempt. 32 x 21.5,
    // three corridors 4.5 u from floor to ceiling on 2 u stone slabs (L016's frame), walked east, west, east. Most of it is
    // played with left and right swapped, and each swap ends in the air.
    //  - Mirror (section 0, corridor 1, floor 0): the cat starts inside Invert. Hinge_0 stands up behind it and pushes it
    //    toward Pit_1 (jump it at once); Lift_1 shoots up into slab A as the cat lands (stop, let it go up and down);
    //    Slam_1 comes down ahead (stop short, let it land, hop it, onto Ferry_1); Ferry_1 carries the cat over the spike
    //    bed; Crumb_1 gives way under it (hop on at once); the jump to the far side crosses Invert's end box, so left and
    //    right switch back in the air over Fake_1, the near ledge that isn't there. The spear stair: each spear sticks just
    //    ahead of the climbing cat, alternating walls, up to corridor 2.
    //  - Orb (section 1, corridor 2, floor 6.5): jump Thorns_2 into Orb_2 (honest, on a timer); mirrored, stop short of
    //    Slam_2 and hop it, stop short of Lift_2 as it rises into slab B, cross. Spikes_B come up in the trench as the cat
    //    nears it, and Orb_2's time runs out in the jump onto the bridge, which then falls behind the cat piece by piece.
    //    Lift_U carries the cat up through Orb_L (the ride swaps the hands again) into corridor 3, and stays up.
    //  - Run (section 2, corridor 3, floor 13): the alley. Two launchers never stop: Arrow_3Hi from behind at jump height,
    //    Arrow_3Lo from the kerb ahead at shin height; their first shots meet a cat that walks on. Stop, let the high one
    //    pass, jump the low one; walk on over Collapse_3, jump the kerb with the next low shot. The finale: Hinge_3 (spiked)
    //    stands up behind, Slam_3a comes down ahead (stop, hop onto it), the floor ahead turns to spikes, Slam_3b comes down
    //    onto them (the step); the last jump, to the door's step, crosses Orb_L's end box: switch hands in the air.
    //  - Dead ends: Fake_1 (the near ledge) and stepping down off Slam_3a (Spikes_3).
    static class L019Layout
    {
        // The corridors' floors and ceilings (room-local y); every slab between them is 2 u of stone.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f;
        const float Slab = 2f;
        // The spear stair: step k's top (1..4), and the faces they stick in (Jamb_E's east face, Wall_R).
        internal static float StepTop(int k) => 1.45f * k;
        internal const float JambX = 27.6f, FaceR = 31.6f;
        // The inverters' windows (Invert and Orb_L end in their end boxes, Orb_2 on its time).
        internal const int InvertTicks = 500, Orb2Ticks = 187, OrbLTicks = 450;
        // The bridge (corridor 2): Bridge_1's wait after the jump onto it crosses x 12, and each next piece's after the last.
        internal const int BridgeDelay = 36, BridgeStep = 13;
        // The alley's launchers: their first shots after the cat comes to x 3.6, then every AlleyEvery ticks.
        internal const int HiDelay = 4, LoDelay = 13, AlleyEvery = 100;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,8.75f),(1f,21.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,8.75f),(1f,21.5f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(31f,F3 + 1.75f),(.6f,1.5f)),
            };

            // ---- Corridor 1's ground (y -2..0) ----
            // Hinge_0's seat (x 0-1.5), the start (1.5-4.5), Pit_1 (4.5-6.5), the walk (6.5-16.5, Lift_1 at 9.7-11.2), the spike
            // bed (16.5-24.5) under Ferry_1, Crumb_1 and Fake_1, and the far side (24.5-32) with the stair's shaft at its east end.
            Block(elements, SoloRoomElementKind.Floor, "Seat_0", 0f, 1.5f, -2f, -.5f);
            Spikes(elements, "Thorns_0", 0f, 1.5f, -.5f);
            Block(elements, SoloRoomElementKind.Floor, "Start_A", 1.5f, 4.5f, -2f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Bed_1", 4.5f, 6.5f, -2f, -1.5f);
            Spikes(elements, "Thorns_1", 4.5f, 6.5f, -1.5f);
            Block(elements, SoloRoomElementKind.Floor, "Walk_1a", 6.5f, 9.7f, -2f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 9.7f, 11.2f, -2f, -1f);
            Block(elements, SoloRoomElementKind.Floor, "Walk_1b", 11.2f, 16.5f, -2f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Bed_F", 16.5f, 24.5f, -2f, -1.5f);
            Spikes(elements, "Thorns_F", 16.5f, 24.5f, -1.5f);
            Block(elements, SoloRoomElementKind.Floor, "Walk_1c", 24.5f, 32f, -2f, F1);
            // Face_R, the stair's east wall (x 31.6-32, up to corridor 2), hosts the west-flying spears.
            elements.Add(E(SoloRoomElementKind.Wall, "Face_R", (FaceR + .2f, F2 * .5f), (.4f, F2)));

            // ---- Slab A (corridor 1's ceiling, corridor 2's floor; y 4.5-6.5, x 0-27.6) ----
            // Lift_U's seat (x 0-2), SA_W (2-3.5), the bridge's trench (3.5-12.5, its bed 0.5 thick: Lift_1's crush partner), SA_M
            // (12.5-13.5), Slam_1's cap (13.5-15), Lift_2's seat (15-16.5), SA_E (16.5-27.6). Jamb_E hangs from its east end down
            // to y 1 (the stair's west wall below it; a cat walks under it).
            Block(elements, SoloRoomElementKind.Floor, "Seat_U", 0f, 2f, C1, C1 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_W", 2f, 3.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Bed_B", 3.5f, 12.5f, C1, C1 + .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_M", 12.5f, 13.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 13.5f, 15f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 15f, 16.5f, C1, C1 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_E", 16.5f, JambX, C1, F2);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_E", JambX - .4f, JambX, 1f, C1);
            Spikes(elements, "Thorns_2", 24.4f, 25.4f, F2);

            // ---- Slab B (corridor 2's ceiling, corridor 3's floor; y 11-13, x 2-32: Lift_U's shaft is x 0-2) ----
            // SB_A (2-6), Collapse_3 over Pit_3 (6-7.5), SB_B (7.5-15), Hinge_3's seat (15-16.5, over Lift_2: its crush partner),
            // SB_C (16.5-19), Slam_2's cap (19-20.5), SB_D (20.5-32).
            Block(elements, SoloRoomElementKind.Floor, "SB_A", 2f, 6f, C2, F3);
            elements.Add(E(SoloRoomElementKind.PitBottom, "Pit_3_Bottom", (6.75f, C2 + .25f), (1.5f, .5f)));
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Hazard, "Pit_3", new Vector2(6.75f, C2 + .65f), new Vector2(1.5f, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
            Block(elements, SoloRoomElementKind.Floor, "SB_B", 7.5f, 15f, C2, F3);
            // Kerb_3 (x 11.6-12, 0.6 tall) ends the alley and hosts Arrow_3Lo; hop it.
            Block(elements, SoloRoomElementKind.Wall, "Kerb_3", 11.6f, 12f, F3, F3 + .6f);
            // Lintel_3 (x 14.6-15) hangs from the roof to y 13.95: Arrow_3Hi's lane ends in its west face; a cat walks under it.
            Block(elements, SoloRoomElementKind.Wall, "Lintel_3", 14.6f, 15f, F3 + .95f, C3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H3", 15f, 16.5f, C2, F3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_C", 16.5f, 19f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 19f, 20.5f, C2 + 1f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_D", 20.5f, 32f, C2, F3);
            // The door's step (x 28.5-32, top 14), on slab B against the east wall.
            Block(elements, SoloRoomElementKind.Floor, "Door_Step", 28.5f, 32f, F3, F3 + 1f);

            // ---- The roof (y 17.5-19.5): Slam_3a and Slam_3b are its bottom unit under caps. Post_A (x 0-0.4, over Lift_U's
            // shaft) hosts Arrow_3Hi and ends Arrow_3Lo's lane.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 19.9f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3a", 19.9f, 21.4f, C3 + 1f, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 21.4f, 24f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3b", 24f, 25.5f, C3 + 1f, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_D", 25.5f, 32f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Wall, "Post_A", 0f, .4f, F3, C3);

            // ---------- Mirror (corridor 1) ----------
            // Invert: the cat starts inside it; left and right are swapped until the cat crosses its end box, in the air over
            // the gap between Crumb_1 and the far side.
            elements.Add(E(SoloRoomElementKind.Inverter,"Invert",(2.5f,1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(InvertTicks,endPosition:new Vector2(23.7f,1.3f),endSize:new Vector2(.4f,1.8f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Hinge_0, the ground behind the start (x 0-1.5, hinged at its west end), stands up 30 ticks into the room (the start's
            // cut, x 0-3.4) and slides 3.9 u east, a 3-tick swing and 16 ticks for the slide, to Pit_1's lip: a cat that waits is
            // pushed in. Jump the pit at once. Thorns_0 line its notch, under the flat: a cat that walks west onto it (pressing
            // right, the old way) drops onto them as it stands up, instead of being shut in behind it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_0",(.75f,-.25f),(1.5f,.5f),(1.7f,(F1 + C1) * .5f),(3.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(3.9f,0f),moveTicks:16,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Lift_1, the walk at x 9.7-11.2, shoots 4.5 u up into the trench's bed above 8 ticks after the cat comes to x 8.5 (where the
            // jump over Pit_1 lands), holds 20 and comes back: a cat that runs on is crushed. Stop short, cross once it's down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(10.45f,-.5f),(1.5f,1f),(8.5f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Bed_B"),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Slam_1, slab A's bottom unit at x 13.5-15, slams 4.5 u onto the walk 4 ticks after the cat comes to x 12.2 and
            // stays: stop short, let it land, hop it (onto Ferry_1).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(14.25f,C1 + .5f),(1.5f,1f),(12.2f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,-C1),moveTicks:9,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Ferry_1 (x 16.5-18, flush with the walk) carries a cat that comes over it 3 u east over the spike bed, holds, and comes
            // back (its trigger is the corridor's height over it).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry_1",(17.25f,-.25f),(1.5f,.5f),(17.25f,(F1 + C1) * .5f),(1.5f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(3f,0f),moveTicks:36,holdTicks:50,returnTicks:36,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:130,movingKind:MovingTrapKind.Solid))));
            // Crumb_1 (x 21.8-23, a block down to the bed) gives way 12 ticks after a touch: hop on and off. Fake_1 (x 23.3-24.5) looks like the far side's lip.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumb_1",(22.4f,-.75f),(1.2f,1.5f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_1",(23.9f,-.75f),(1.2f,1.5f)));
            // The spear stair: the cut at x 26.3-26.7 fires Spear_1 (east, from Jamb_E into Wall_R); each next one alternates walls,
            // StairStep ticks after the last, so each shaft sticks just ahead of a cat that keeps climbing.
            elements.Add(Spear("Spear_1", JambX - .2f, StepTop(1) - .2f, ArrowDirection.Right, FaceR, (26.5f, (F1 + C1) * .5f), (.4f, C1 - F1), new SoloRoomTrapSettings(delayTicks:12)));
            for (int k = 2; k <= 4; k++)
                elements.Add(k % 2 == 0
                    ? Spear($"Spear_{k}", FaceR + .2f, StepTop(k) - .2f, ArrowDirection.Left, JambX, default, default, Chain($"Spear_{k - 1}", StairStep))
                    : Spear($"Spear_{k}", JambX - .2f, StepTop(k) - .2f, ArrowDirection.Right, FaceR, default, default, Chain($"Spear_{k - 1}", StairStep)));

            // ---------- Orb (corridor 2) ----------
            // Orb_2 (honest) stands where the jump over Thorns_2 lands: Orb2Ticks of swapped hands, which run out in the jump onto the bridge.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_2",(23.6f,(F2 + C2) * .5f),(.6f,C2 - F2),settings:new SoloRoomTrapSettings(new InverterSettings(Orb2Ticks),new SoloRoomTrapSettings(delayTicks:0))));
            // Slam_2, slab B's bottom unit at x 19-20.5, slams onto the walk 4 ticks after a cat walking west comes to x 21.5 and stays.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(19.75f,C2 + .5f),(1.5f,1f),(21.5f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,F2 - C2),moveTicks:9,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Lift_2, slab A's top unit at x 15-16.5, rises 4.5 u into slab B 20 ticks after a cat comes over x 17.7 (in the hop over
            // Slam_2), holds 20 and comes back: a cat that walks on is crushed. Stop short, cross once it's down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(15.75f,F2 - .5f),(1.5f,1f),(17.7f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Seat_H3"),
                new SoloRoomTrapSettings(delayTicks:20,offset:new Vector2(0f,C2 - F2),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Spikes_B (hidden) on the trench's bed come up as the cat comes to the trench's lip (their cut, x 12.7-13.1, the
            // corridor's height), and set the bridge falling.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_B",(8f,C1 + .65f),(9f,.3f),(12.9f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // The bridge over the trench (x 3.5-10.5; the gap at 10.5-12.5 is jumped): Bridge_1 gives way BridgeDelay ticks after
            // Spikes_B come up,
            // each next piece west BridgeStep after the last: the fall follows the cat.
            for (int i = 0; i < 5; i++)
            {
                float x1 = 10.5f - 1.4f * i;
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.CollapsingFloor, "Bridge_1", (x1 - .7f, F2 - .25f), (1.4f, .5f), settings: Chain("Spikes_B", BridgeDelay))
                    : E(SoloRoomElementKind.CollapsingFloor, $"Bridge_{i + 1}", (x1 - .7f, F2 - .25f), (1.4f, .5f), settings: Chain($"Bridge_{i}", BridgeStep)));
            }
            // Lift_U, slab A's top unit at x 0-2, carries a cat that steps on it 6.5 u up through slab B into corridor 3 (Orb_L hangs
            // in the shaft: the ride swaps the hands) and stays there, corridor 3's floor at its west end.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_U",(1f,F2 - .5f),(2f,1f),(1f,F2 + .75f),(2f,1.5f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:10,offset:new Vector2(0f,F3 - F2),moveTicks:40,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_L",(1f,C2),(.6f,1.4f),settings:new SoloRoomTrapSettings(
                new InverterSettings(OrbLTicks,endPosition:new Vector2(27.4f,F3 + 2.2f),endSize:new Vector2(.4f,1.8f)),new SoloRoomTrapSettings(delayTicks:0))));

            // ---------- Run (corridor 3) ----------
            // The alley (x 2-11.6): the cut at x 3.4-3.8 starts both launchers, then they never stop. Arrow_3Hi flies east from Post_A
            // at jump height, over a standing cat, into Lintel_3 (don't be in the air as it passes); Arrow_3Lo flies west from Kerb_3
            // at shin height into Post_A (jump each shot). Their first shots meet a cat that walks on at x 7.7: stop short, let
            // the high one pass, then jump the low one.
            var alley = ((float, float))(3.6f, (F3 + C3) * .5f); var alleySize = ((float, float))(.4f, C3 - F3);
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_3Lo",(11.8f,F3 + .3f),(.4f,.4f),alley,alleySize,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,F3 + .3f,.4f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:LoDelay,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:AlleyEvery - LoDelay))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_3Hi",(.2f,F3 + 1.1f),(.4f,.4f),alley,alleySize,
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,F3 + 1.1f,14.6f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:HiDelay,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:AlleyEvery - HiDelay))));
            // Collapse_3 (x 6-7.5) gives way 26 ticks after a touch into Pit_3: run on.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_3",(6.75f,C2 + 1.15f),(1.5f,1.7f),settings:new SoloRoomTrapSettings(delayTicks:26)));
            // The finale, from the cut at x 18.4-18.8: Hinge_3 (slab B's top at x 15-16.5, spiked, hinged at its east end so its
            // spikes face the cat) stands up in 3 ticks and slides 2.5 u east; Slam_3a (x 19.9-21.4) comes down ahead and stays;
            // Spikes_3 (x 21.4-24) and Spikes_3b (x 25.5-28.5) come up; Slam_3b (x 24-25.5) comes down onto them: the step to the
            // door's step. Orb_L's end box is over Spikes_3b, in the last jump.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(15.75f,F3 - .25f),(1.5f,.5f),(18.6f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(2.5f,0f),moveTicks:30,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3a",(20.65f,C3 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(22.7f,F3 + .15f),(2.6f,.3f),settings:Chain("Hinge_3", 28)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3b",(27f,F3 + .15f),(3f,.3f),settings:Chain("Hinge_3", 40)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3b",(24.75f,C3 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:55,offset:new Vector2(0f,F3 - C3),moveTicks:9,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));

            var sections = new[] {
                CheckpointSection.Start("Mirror", new Vector2(2.5f, F1), new[] { "Invert", "Hinge_0", "Lift_1", "Slam_1", "Ferry_1", "Crumb_1", "Fake_1", "Spear_1", "Spear_2", "Spear_3", "Spear_4" }),
                new CheckpointSection("Orb", new Vector2(27f, F2), new Rect(26.6f, F2, .2f, 2.2f),
                    new[] { "Orb_2", "Slam_2", "Lift_2", "Spikes_B", "Bridge_1", "Bridge_2", "Bridge_3", "Bridge_4", "Bridge_5", "Lift_U", "Orb_L" }),
                new CheckpointSection("Run", new Vector2(2.8f, F3), new Rect(.1f, F3 - .4f, 1.8f, .2f),
                    new[] { "Arrow_3Lo", "Arrow_3Hi", "Collapse_3", "Hinge_3", "Slam_3a", "Spikes_3", "Spikes_3b", "Slam_3b" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 6f, 7.5f, "SB_A", "SB_B", "Pit_3_Bottom", "Pit_3") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        internal const int StairStep = 34;

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top at `face`.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // A spear from a launcher at x, flying along `laneY` and sticking in the face at `laneEndX`.
        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (.4f, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX), timing));
    }
}
