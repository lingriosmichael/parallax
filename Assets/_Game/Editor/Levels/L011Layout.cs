using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 11, "Scaffold" (the spear). The verb is BUILD: every step above the ground is a spear the cat
    // fires itself, and the solution is a build order: which trigger to cross, where to be when that spear flies, when to
    // stand on it. A Z through the room: west along the ground, up the west shaft, east along the gallery, up the east
    // shaft, west along the top storey to the door.
    //  - Foot (section 0): walking west past x 9 fires Spear_1 from G_East along the storey at jump height; at the gap it
    //    runs a hopping cat through, and sticks as the first step. The floor at the gap's edge gives way 55 ticks after a
    //    touch. Every later step (Spear_2-8) fires when the cat jumps into its lane: land, hop, wait for it to stick, climb.
    //    Steps rise 1.45 and alternate walls, so a hop reaches only the next lane and no jump grazes the step 2.9 above on its
    //    own side. Spear_L sweeps step 4 if you linger there; Ledge_M, in the open middle of the shaft, is a fake. Spear_1 also fires Spear_Door, high by the door: you see it stick now and use it in the last act.
    //  - Crossing (section 1): Spear_G sweeps the gallery from behind at shin height (jump it). Past the tunnel the cat drops
    //    into the Dip; the cut on the floor beyond it fires V1-V5 12 ticks apart from both walls, V1 at shin height over the
    //    floor. The one spot no lane crosses is down in the Dip. Stuck, they are the stair (V2 11.25 ... V5 15.6). Jumping
    //    onto V5 fires Spear_Top along V5 at body height (and Spear_K sweeps V3 behind you): step straight back down, let it stick, and it's the step to Tower_E.
    //  - Summit (section 2): Spear_Gap, from Post_G, sweeps the top storey at shin height (jump it) and bridges the chasm.
    //    The Lip in front of the door ledge gives way; Spear_Door is the way. Stepping onto the door ledge drops Block_Door
    //    between the cat and the door: stop at the edge, let it land, hop it.
    //  - Dead ends: the ledge east of the stairs (hidden spikes); Ledge_Hi, above the door ledge (hidden spikes). The chasm
    //    and the pit are declared bait gaps: out of reach without their spears.
    static class L011Layout
    {
        public static SoloRoomDefinition Build()
        {
            const float h = 2.9f;   // everything from the gallery up sits above the eight-step shaft
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,11f),(1f,31f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,11f),(1f,31f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,23.5f+h),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(20f,0f),(0f,0f)),
                // The ground storey and the foot of the west shaft (x 1-5): a ledge, the gap, a floor that gives way.
                E(SoloRoomElementKind.Wall,"Core_WW",(.5f,2.65f+h*.5f),(1f,13.3f+h)),
                E(SoloRoomElementKind.Floor,"Foot_L",(1.8f,-2f),(1.6f,4f)),
                E(SoloRoomElementKind.PitBottom,"Foot_Pit",(3.8f,-3.25f),(2.4f,.5f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_Foot",new Vector2(3.8f,-2.85f),new Vector2(2.4f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Floor,"Ground",(12.75f,-2f),(15.5f,4f)),
                E(SoloRoomElementKind.Wall,"G_East",(21f,1.925f+h*.5f),(1f,11.85f+h)),
                E(SoloRoomElementKind.Wall,"Core_W",(5.5f,4.775f+h*.5f),(1f,7.15f+h)),
                // PAX-A16 (D-100): Spear_1's mount, a low lintel under Core_W's east half (the walk passes under it, 0.7 high).
                E(SoloRoomElementKind.Wall,"Mount_1",(5.76f,.95f),(.48f,.5f)),
                // The gallery (x 6-21.5, top 11.25), the Curb at its east end, and the tunnel floor under Mast_W.
                E(SoloRoomElementKind.Floor,"Gallery",(13.75f,8.1f+h),(15.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Tunnel_Floor",(22.5f,2.175f+h*.5f),(2f,12.35f+h)),
                E(SoloRoomElementKind.Wall,"Curb_M",(20.65f,8.65f+h),(.5f,.6f)),
                // The east shaft (x 23.5-28, floor 9.8) with the Dip at its west end (0.7 deep), and the tower east of it.
                E(SoloRoomElementKind.Floor,"Shaft_Floor",(26.85f,1.45f+h*.5f),(2.3f,10.9f+h)),
                E(SoloRoomElementKind.Floor,"Dip",(24.6f,1.1f+h*.5f),(2.2f,10.2f+h)),
                E(SoloRoomElementKind.Wall,"Mast_W",(23f,12.275f+h),(1f,5.75f)),
                E(SoloRoomElementKind.Wall,"Tower_E",(30f,5.325f+h*.5f),(4f,18.65f+h)),
                // The top storey (18.55): east of the chasm, west of it (the Curb at its end), the posts, the high ledge,
                // the pit in front of the door ledge (19.55, level with Spear_Door once it has stuck).
                E(SoloRoomElementKind.Floor,"T_East",(24.35f,15.4f+h),(3.7f,.5f)),
                E(SoloRoomElementKind.Wall,"Post_G",(23.85f,16.2f+h),(.5f,1.1f)),
                E(SoloRoomElementKind.Floor,"T_West",(12.8f,15.4f+h),(8.6f,.5f)),
                E(SoloRoomElementKind.Wall,"Curb_T",(16.85f,15.95f+h),(.5f,.6f)),
                E(SoloRoomElementKind.Wall,"Post_D",(12.25f,16.2f+h),(.5f,1.1f)),
                E(SoloRoomElementKind.Floor,"Ledge_Hi",(10f,17.75f+h),(2f,.5f)),
                E(SoloRoomElementKind.Wall,"Pit_E",(8.75f,13.65f+h),(.5f,3f)),
                E(SoloRoomElementKind.PitBottom,"Pit_T_Bottom",(6f,12.4f+h),(5f,.5f)),
                new SoloRoomElement(SoloRoomElementKind.Hazard,"Pit_T",new Vector2(6f,12.8f+h),new Vector2(5f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.Wall,"Core_A",(1.75f,12.975f+h),(3.5f,7.35f)),
                E(SoloRoomElementKind.Ceiling,"Door_Roof",(1.75f,23f),(3.5f,1f)),
                E(SoloRoomElementKind.Door,"Door",(.8f,17.4f+h),(.6f,1.5f)),
            };

            // Foot. Walking west past x 9 fires Spear_1 from G_East, along the whole storey at jump height (and, chained,
            // Spear_Door by the door). The floor at the gap's edge gives way 55 ticks after a touch.
            elements.Add(Spear("Spear_1", 5.75f, .9f, ArrowDirection.Left, 1f, 1.4f, (9f, 5.375f), (.4f, 10.75f), new SoloRoomTrapSettings(delayTicks:41)));
            elements.Add(Spear("Spear_Door", 12.25f, 16.45f+h, ArrowDirection.Left, 3.5f, 2.8f, default, default, new SoloRoomTrapSettings(delayTicks:1,triggerSource:TrapTriggerSource.Chain,chainSource:"Spear_1")));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Foot_C",(4.2f,-1.5f),(1.6f,3f),settings:new SoloRoomTrapSettings(delayTicks:55)));
            // Each later step fires when the cat jumps into its lane (the lane box is the trigger): the climb onto Spear_1
            // brings Spear_2; after that, a hop on each step brings the next. Steps rise 1.45 and alternate walls, so a hop
            // reaches only the next lane and no jump grazes the step 2.9 above on its own side.
            float[] tops = { 2.55f, 4f, 5.45f, 6.9f, 8.35f, 9.8f, 11.25f };
            for (int i = 0; i < tops.Length; i++)
            {
                bool east = i % 2 == 0;   // the even spears stick on Core_W's face (x 5), the odd ones on Core_WW's (x 1)
                float lane = tops[i] - .2f;
                elements.Add(east
                    ? Spear("Spear_" + (i + 2), .75f, lane, ArrowDirection.Right, 5f, 1.4f, (2.875f, lane), (4.25f, .4f), new SoloRoomTrapSettings(delayTicks:32))
                    : Spear("Spear_" + (i + 2), 5.25f, lane, ArrowDirection.Left, 1f, 1.4f, (3.125f, lane), (4.25f, .4f), new SoloRoomTrapSettings(delayTicks:32)));
            }

            // §14 R2: two betrayals inside the stair. Spear_L, set off by the same hop that brings Spear_4, sweeps step 4 at
            // body height about 145 ticks later: a cat still standing there is run through (keep building). Ledge_M, a
            // ledge in the open middle of the shaft between Spear_7 and the next lane, is a fake: the next step is the one
            // you fire, and the middle drops straight to the foot.
            elements.Add(Spear("Spear_L", .75f, 5.75f, ArrowDirection.Right, 5f, 1.4f, (2.875f, 5.75f), (4.25f, .4f), new SoloRoomTrapSettings(delayTicks:135)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Ledge_M",(3f,7.45f),(1.2f,.5f)));

            // Crossing. Spear_G, from Core_WW behind the cat, sweeps the gallery at shin height and sticks against Curb_M.
            elements.Add(Spear("Spear_G", .75f, 8.65f+h, ArrowDirection.Right, 20.4f, 1.4f, (11.2f, 11.75f+h), (.4f, 6.8f), new SoloRoomTrapSettings(delayTicks:4)));
            // The volley: the cut on the shaft floor fires V1 (shin height over the floor, over the Dip); V2-V5 follow 12 ticks apart. Spear_Top fires on entering its lane.
            elements.Add(Spear("V1", 28.25f, 7.2f+h, ArrowDirection.Left, 23.5f, 1f, (26f, 13.575f), (.4f, 8.95f), new SoloRoomTrapSettings(delayTicks:34)));
            elements.Add(Spear("V2", 23.25f, 8.15f+h, ArrowDirection.Right, 28f, 1f, default, default, Chain("V1")));
            elements.Add(Spear("V3", 28.25f, 9.6f+h, ArrowDirection.Left, 23.5f, 1.4f, default, default, Chain("V2")));
            elements.Add(Spear("V4", 23.25f, 11.05f+h, ArrowDirection.Right, 28f, 2.4f, default, default, Chain("V3")));
            elements.Add(Spear("V5", 28.25f, 12.5f+h, ArrowDirection.Left, 23.5f, 1.4f, default, default, Chain("V4")));
            // §14 R2: the volley's stair burns too. Spear_K, set off by the jump up to V3, sweeps V3 at body height soon after.
            elements.Add(Spear("Spear_K", 28.25f, 10.1f+h, ArrowDirection.Left, 23.5f, 1.4f, (25.75f, 10.1f+h), (4.5f, .4f), new SoloRoomTrapSettings(delayTicks:27)));
            elements.Add(Spear("Spear_Top", 23.25f, 13f+h, ArrowDirection.Right, 28f, 1.2f, (25.625f, 13f+h), (4.75f, .4f), new SoloRoomTrapSettings(delayTicks:50)));
            // Dead end: the ledge east of the stairs.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(31.8f,14.8f+h),(.4f,.3f),(30f,18.825f+h),(.4f,8.35f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Summit. Spear_Gap sweeps the top storey at shin height and bridges the chasm; the Lip gives way; stepping onto
            // the door ledge drops Block_Door onto it, between the cat and the door.
            elements.Add(Spear("Spear_Gap", 23.85f, 15.95f+h, ArrowDirection.Left, 17.1f, 3f, (22.8f, 19.325f+h), (.4f, 7.35f), new SoloRoomTrapSettings(delayTicks:30), tell: 12));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Lip",(6f,14.15f+h),(5f,3f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_Door",(2.15f,23f),(1.5f,1f),(3.3f,21.025f),(.4f,2.95f),new SoloRoomTrapSettings(delayTicks:3,unitsPerTick:.36f,travelDistance:2.95f)));
            // Dead end: the high ledge, above the door ledge. PAX-091: its trigger holds Ledge_Hi's whole top strip; Stop_Hi, a post
            // on its west face (top 21.4, a 1.85 rise from the stuck Spear_Door), keeps the ledge out of a jump's reach from the west.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(9.7f,18.15f+h),(1.4f,.3f),(10f,20.5f+h),(2f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Wall,"Stop_Hi",(8.85f,18f+h),(.3f,1f)));

            var openings = new[] {
                O(SoloRoomOpeningKind.Pit, 2.6f, 5f, "Foot_L", "Ground", "Foot_Pit", "Pit_Foot"),
                O(SoloRoomOpeningKind.Pit, 3.5f, 8.5f, "Core_A", "Pit_E", "Pit_T_Bottom", "Pit_T"),
            };
            var sections = new[] {
                CheckpointSection.Start("Foot", new Vector2(20f, 0f), new[] { "Spear_1", "Spear_Door", "Foot_C", "Spear_2", "Spear_3", "Spear_4", "Spear_5", "Spear_6", "Spear_7", "Spear_8", "Spear_L", "Ledge_M" }),
                new CheckpointSection("Crossing", new Vector2(5.5f, 8.35f+h), new Rect(6.4f, 8.35f+h, .2f, 3.8f), new[] { "Spear_G", "V1", "V2", "V3", "V4", "V5", "Spear_K", "Spear_Top", "Spikes_D1" }),
                new CheckpointSection("Summit", new Vector2(25.7f, 15.65f+h), new Rect(24.9f, 15.65f+h, .2f, 7.35f), new[] { "Spear_Gap", "Lip", "Block_Door", "Spikes_D2" }),
            };
            // Without their spears, the chasm and the pit are out of reach.
            var baits = new[] { new BaitGap("Chasm", 22.5f, 15.65f+h, 17.1f, 16.25f+h), new BaitGap("Pit", 8.5f, 15.65f+h, 3.5f, 16.65f+h) };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, baits, sections);
        }

        static SoloRoomTrapSettings Chain(string source) => new(delayTicks: 12, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // A spear launched from (x, laneY) inside its host, flying to laneEndX, `length` long once stuck. The trigger box
        // (a cut, or the lane box itself) is omitted for a chained spear. Spear_Gap tells for 12 ticks, so its jump keeps slack.
        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, float length, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing, int tell = 8) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (.5f, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX, length: length, tellTicks: tell), timing));
    }
}
