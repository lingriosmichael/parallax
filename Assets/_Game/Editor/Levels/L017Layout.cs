using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-147): level 17, "Deja Vu" (spears and chains), rebuilt. The developer's play-tests ask for compact stacked
    // lanes, a moving threat every few steps, a chase at the end and more inverters. Three lanes 5 u apart (floors 0, 5, 10),
    // walked east, west, east; stone fills everything that isn't played. The way out builds the way up: at each lane's end a
    // spear volley sticks its shafts into the well's walls one step ahead of the climbing cat. Lane 2 is lane 1 again, turned
    // around, and played with left and right swapped (Inv_2), so the same thumbs make the same moves: deja vu.
    //  - Out (section 0, lane 1, east): the first step onto the bridge bares Pit_1's spikes and the bridge (Br_1-Br_4)
    //    crumbles behind the cat; Slam_1 comes down on the walkway (stop, let it land, hop onto it and over Kerb_1); Arrow_1
    //    shoots from the Spine along the lane at jump height, non-stop (walk under it; let a shot pass before the jump);
    //    Hinge_1 stands up behind the cat and catches one that stops short of Spikes_1 (jump them); under the Spine into
    //    well 1, where V1-V4 stick one by one, alternating walls: each shaft is the next step, and a cat that jumps before it
    //    sticks is run through.
    //  - Back (section 1, lane 2, west): Inv_2 at the top of the stair swaps left and right until the cat walks onto lane 3
    //    (its end box). The same again, turned around: bridge 2 over Pit_2, Slam_2 and Kerb_2, Arrow_2 from Spine_2, Hinge_2
    //    and Spikes_2, under Spine_2 into well 2, and W1-W4, climbed with swapped hands.
    //  - Last (section 2, lane 3, east): Inv_3 swaps the hands again for a moment that ends in the jump over Pit_3 (switch
    //    back in the air); then Slam_3 comes down ahead while Push_3 stands up behind and chases the cat to it (hop onto the
    //    slam before the wall arrives), and Spear_D sticks its long shaft across Pit_4, too wide to jump, as the last step to
    //    the door.
    //  - Dead ends: back west over the crumbling bridge (Pit_1); the old hand on bridge 2, back east (Pit_2).
    static class L017Layout
    {
        // The lanes' floors and ceilings (room-local y); the slabs between them are 1.5 u of stone.
        internal const float F1 = 0f, C1 = 3.5f, F2 = 5f, C2 = 8.5f, F3 = 10f, C3 = 13.5f, Roof = 15.5f, Bottom = -3f;
        // The wells' steps: tops 1.25 apart from the lane's floor, the fourth flush with the next lane's floor.
        internal const float Rise = 1.25f;
        // The arrows fly at jump height (over a walking cat), into kerbs this tall; the troughs' depth under the lanes.
        internal const float ArrowY = 1.3f, KerbH = 1.6f, Trough = 1f;
        // The wells: well 1 (x 28-32) under the Spine (x 27-28), well 2 (x 0-4) under Spine_2 (x 4-5).
        internal const float Well1 = 28f, Well2 = 4f, SpineBottom = .85f, FaceR = 31.6f, FaceL = .4f;
        // The bridges' pieces: the first gives way BridgeDelay ticks after its pit's spikes are bared, each next one BridgeStep
        // after the last; each volley's spears VolleyStep ticks apart; the arrows' cooldown between shots.
        internal const int BridgeDelay = 22, BridgeStep = 16, VolleyStep = 48, ArrowCooldown = 90;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(31f,F3 + .75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Wall, "Wall_L", -1f, 0f, Bottom, Roof);
            Block(elements, SoloRoomElementKind.Wall, "Wall_R", 32f, 33f, Bottom, Roof);

            // ---------- Lane 1's ground (y -3..0) ----------
            Block(elements, SoloRoomElementKind.Floor, "Ground_A", 0f, 3f, Bottom, F1);
            Block(elements, SoloRoomElementKind.PitBottom, "Pit1_Floor", 3f, 11f, Bottom, Bottom + .5f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 11f, 21f, Bottom, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H1", 21f, 22f, Bottom, F1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_C", 22f, 32f, Bottom, F1);
            Spikes(elements, "Spikes_1", 23.6f, 24.9f, F1);
            // The Spine closes lane 1's ceiling over well 1's doorway (a cat walks under it, 0.85 u clear) and hosts Arrow_1.
            Block(elements, SoloRoomElementKind.Wall, "Spine", 27f, Well1, SpineBottom, C1);
            // Well 1's east face (x 31.6-32) hosts V2 and V4; Kerb_1 (x 15-15.4, 1.6 tall) by Slam_1 is where Arrow_1's shots end.
            elements.Add(E(SoloRoomElementKind.Wall, "Face_R", (FaceR + .2f, (F1 + C2) * .5f), (.4f, C2 - F1)));
            Block(elements, SoloRoomElementKind.Wall, "Kerb_1", 15f, 15.4f, F1, F1 + KerbH);

            // ---------- Slab A (lane 1's ceiling, lane 2's floor, y 3.5-5) ----------
            // Hinge_2 is its top unit on a seat; Slam_1 its bottom unit under a cap; Pit_2 a trough in it under bridge 2.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 10f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H2", 10f, 11f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 11f, 13f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 13f, 15f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 15f, 20.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_P2", 20.5f, 26.5f, C1, F2 - Trough);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 26.5f, Well1, C1, F2);
            Spikes(elements, "Spikes_2", 7.1f, 8.4f, F2);
            // Spine_2 closes lane 2's ceiling over well 2's doorway and hosts Arrow_2.
            Block(elements, SoloRoomElementKind.Wall, "Spine_2", Well2, 5f, F2 + SpineBottom, C2);
            // Well 2's west face (x 0-0.4) hosts W2 and W4; Kerb_2 (x 16.6-17, 1.6 tall) by Slam_2 is where Arrow_2's shots end.
            elements.Add(E(SoloRoomElementKind.Wall, "Face_L", (FaceL - .2f, (F2 + C3) * .5f), (.4f, C3 - F2)));
            Block(elements, SoloRoomElementKind.Wall, "Kerb_2", 16.5f, 17f, F2, F2 + KerbH);

            // ---------- Slab B (lane 2's ceiling, lane 3's floor, y 8.5-10) ----------
            // Push_3 is its top unit on a seat; Slam_2 its bottom unit under a cap; Pit_3 and Pit_4 troughs in it.
            Block(elements, SoloRoomElementKind.Floor, "SB_0", Well2, 10.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_P3", 10.5f, 13.5f, C2, F3 - Trough);
            Spikes(elements, "Pit_3", 10.5f, 13.5f, F3 - Trough);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 13.5f, 14.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_P3", 14.5f, 16f, C2, F3 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 16f, 17f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 17f, 19f, C2 + 1f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", 19f, 24f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_P4", 24f, 30f, C2, F3 - Trough);
            Spikes(elements, "Pit_4", 24f, 30f, F3 - Trough);
            Block(elements, SoloRoomElementKind.Floor, "SB_4", 30f, 32f, C2, F3);

            // ---------- The roof (y 13.5-15.5): Slam_3 is its bottom unit under a cap ----------
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 21.5f, C3, Roof);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3", 21.5f, 23.5f, C3 + 1f, Roof);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 23.5f, 32f, C3, Roof);

            // ---------- Out (lane 1, east) ----------
            // The bridge: four pieces over the pit, from the cat's first step. The step onto it (its trigger holds the pit and the
            // lane over it, x 3-11) bares Pit_1's spikes on the pit's floor; Br_1 gives way BridgeDelay ticks later, each next
            // piece BridgeStep after the last: it crumbles behind a cat that keeps running, under one that stops.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes, "Pit_1", (7f, Bottom + .65f), (8f, .3f), (7f, (Bottom + .5f + C1) * .5f), (8f, C1 - Bottom - .5f),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            for (int i = 0; i < 4; i++)
                elements.Add(E(SoloRoomElementKind.CollapsingFloor, $"Br_{i + 1}", (4f + 2f * i, F1 - .25f), (2f, .5f),
                    settings:Chain(i == 0 ? "Pit_1" : $"Br_{i}", i == 0 ? BridgeDelay : BridgeStep)));
            // Slam_1, slab A's bottom unit at x 13-15, slams onto the walkway as the cat steps off the bridge (the cut x 11.4-11.8)
            // and stays: stop, let it land, hop onto it and over Kerb_1.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(14f,C1 + .5f),(2f,1f),(11.6f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(0f,-C1),moveTicks:9,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            // Arrow_1 shoots from the Spine west along lane 1 at jump height (over a walking cat) into Kerb_1, non-stop, from the
            // moment Slam_1 lands: the jump over Spikes_1 goes between two shots.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1",(27.2f,ArrowY),(.4f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,ArrowY,15.4f,tellTicks:10),
                new SoloRoomTrapSettings(delayTicks:20,triggerSource:TrapTriggerSource.Chain,chainSource:"Slam_1",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:ArrowCooldown))));
            // Hinge_1, the ground's top unit at x 21-22 (hinged at its east end, spiked, so its spikes face the cat; 1 u tall when
            // it stands, under the arrows), stands up behind a cat that walks on past x 22.7 and slides 1.2 u east toward Spikes_1,
            // faster than a run: it catches a cat that stops to aim the jump over them (one that keeps holding on stays ahead).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_1",(21.5f,F1 - .5f),(1f,1f),(23.4f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(1.2f,0f),moveTicks:8,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            // Well 1's volley: the cut up the well (x 28.4-28.8) fires V1, each next one VolleyStep ticks after the last,
            // alternating walls: V1 and V3 from the Spine into Face_R, V2 and V4 from Face_R into the Spine and slab A. V4's
            // shaft is flush with lane 2's floor.
            elements.Add(Spear("V1", Well1 - .2f, Rise * 1 - .2f, ArrowDirection.Right, FaceR, (28.6f, (F1 + C2) * .5f), (.4f, C2 - F1), new SoloRoomTrapSettings(delayTicks:2)));
            elements.Add(Spear("V2", FaceR + .2f, Rise * 2 - .2f, ArrowDirection.Left, Well1, default, default, Chain("V1", VolleyStep)));
            elements.Add(Spear("V3", Well1 - .2f, Rise * 3 - .2f, ArrowDirection.Right, FaceR, default, default, Chain("V2", VolleyStep)));
            elements.Add(Spear("V4", FaceR + .2f, Rise * 4 - .2f, ArrowDirection.Left, Well1, default, default, Chain("V3", VolleyStep)));

            // ---------- Back (lane 2, west, left and right swapped) ----------
            // Inv_2 at the top of well 1 (over section 1's checkpoint, so a respawned cat is swapped again) swaps the cat's left
            // and right until it walks onto lane 3 (the end box, x 4-7 at its floor, over section 2's checkpoint).
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_2",(27.4f,F2 + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(500,endPosition:new Vector2(5.5f,F3 + .3f),endSize:new Vector2(3f,.6f)),new SoloRoomTrapSettings(delayTicks:0))));
            // Lane 2 is lane 1 turned around (x -> 32 - x from the slam to the well). Bridge 2: three pieces over the trough in slab
            // A (x 20.5-26.5); the step onto it bares Pit_2's spikes, and it crumbles from the east behind the cat, timed as bridge 1.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes, "Pit_2", (23.5f, F2 - Trough + .15f), (6f, .3f), (23.5f, (F2 - Trough + C2) * .5f), (6f, C2 - F2 + Trough),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            for (int i = 0; i < 3; i++)
                elements.Add(E(SoloRoomElementKind.CollapsingFloor, $"Br2_{i + 1}", (25.5f - 2f * i, F2 - .25f), (2f, .5f),
                    settings:Chain(i == 0 ? "Pit_2" : $"Br2_{i}", i == 0 ? BridgeDelay : BridgeStep)));
            // Slam_2, slab B's bottom unit at x 17-19, slams onto lane 2 as the cat steps off bridge 2 (the cut x 20.2-20.6).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(18f,C2 + .5f),(2f,1f),(20.4f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(0f,F2 - C2),moveTicks:9,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            // Arrow_2 shoots from Spine_2 east along lane 2 at jump height into Kerb_2, non-stop, from the moment the cat steps down
            // into its lane (its trigger holds the lane, so a cat back from well 2 sets it off too).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2",(4.8f,F2 + ArrowY),(.4f,.4f),(10.75f,(F2 + C2) * .5f),(11.7f,C2 - F2),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,F2 + ArrowY,16.5f,tellTicks:10),
                new SoloRoomTrapSettings(delayTicks:45,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:ArrowCooldown))));
            // Hinge_2, slab A's top unit at x 10-11 (hinged at its west end, spiked, facing the cat), stands up behind a cat that
            // walks on past x 9.3 and slides 1.2 u west toward Spikes_2, as Hinge_1.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_2",(10.5f,F2 - .5f),(1f,1f),(8.6f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(-1.2f,0f),moveTicks:8,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            // Well 2's volley, the same again: the cut up the well (x 2.6-3.0) fires W1; W1 and W3 from Spine_2 into Face_L,
            // W2 and W4 from Face_L into Spine_2 and slab B. W4's shaft is flush with lane 3's floor.
            elements.Add(Spear("W1", Well2 + .2f, F2 + Rise * 1 - .2f, ArrowDirection.Left, FaceL, (2.8f, (F2 + C3) * .5f), (.4f, C3 - F2), new SoloRoomTrapSettings(delayTicks:2)));
            elements.Add(Spear("W2", FaceL - .2f, F2 + Rise * 2 - .2f, ArrowDirection.Right, Well2, default, default, Chain("W1", VolleyStep)));
            elements.Add(Spear("W3", Well2 + .2f, F2 + Rise * 3 - .2f, ArrowDirection.Left, FaceL, default, default, Chain("W2", VolleyStep)));
            elements.Add(Spear("W4", FaceL - .2f, F2 + Rise * 4 - .2f, ArrowDirection.Right, Well2, default, default, Chain("W3", VolleyStep)));

            // ---------- Last (lane 3, east) ----------
            // Inv_3 swaps the hands for 36 ticks: a cat that runs on takes off over Pit_3 swapped and is swapped back in the air.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_3",(8.5f,F3 + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(
                new InverterSettings(36),new SoloRoomTrapSettings(delayTicks:0))));
            // The finale, from the cut at x 19.2-19.6: Slam_3 (the roof's bottom unit at x 21.5-23.5) comes down ahead and stays;
            // Push_3 (slab B's top unit at x 14.5-16, hinged at its east end, spiked) stands up behind and slides 5.5 u east to the
            // slam: hop onto the slam before it arrives. Spear_D, in slab B's face at the pit's west side, sticks its long shaft
            // across Pit_4 (6 u, too wide to jump even from the slam) into SB_4's face (x 27.5-30) soon after the slam lands: the
            // last step to the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3",(22.5f,C3 + .5f),(2f,1f),(19.4f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:11,offset:new Vector2(0f,F3 - C3),moveTicks:9,holdTicks:5000,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Push_3",(15.25f,F3 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(5.5f,0f),moveTicks:36,holdTicks:5000,triggerSource:TrapTriggerSource.Chain,chainSource:"Slam_3",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Spear_D",(23.8f,F3 - .2f),(.4f,.4f),settings:new SoloRoomTrapSettings(
                ArrowLane.SpearLane(ArrowDirection.Right,F3 - .2f,30f,length:2.5f),Chain("Slam_3", 24))));

            var sections = new[] {
                CheckpointSection.Start("Out", new Vector2(1.5f, F1), new[] { "Pit_1", "Br_1", "Br_2", "Br_3", "Br_4", "Slam_1", "Arrow_1", "Hinge_1", "V1", "V2", "V3", "V4" }),
                new CheckpointSection("Back", new Vector2(27.4f, F2), new Rect(Well1, F2 - .2f, FaceR - Well1, .2f),
                    new[] { "Inv_2", "Pit_2", "Br2_1", "Br2_2", "Br2_3", "Slam_2", "Arrow_2", "Hinge_2", "W1", "W2", "W3", "W4" }),
                new CheckpointSection("Last", new Vector2(6.4f, F3), new Rect(FaceL, F3 - .2f, Well2 - FaceL, .2f), new[] { "Inv_3", "Slam_3", "Push_3", "Spear_D" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top at `face`.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (.4f, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX), timing));
    }
}
