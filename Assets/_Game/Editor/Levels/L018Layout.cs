using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 18, "Eye of the Storm" (the storm cloud, geysers and a vine). The verb is GET ABOVE:
    // lightning only strikes down from the cloud's bottom (y 3.2), so a cat whose feet are above it can't be hit
    // (StormCloudMath.Hits). 32 x 22, start bottom right, door top right, a U: west along the ground under the cloud,
    // a launch above it and west across Sky A, back down into the storm on the drop, a vine up through the cloud's band,
    // east along Sky B under the downdraft, a launch onto the Top, and a last launch onto the Crown with the door.
    //  - Storm (section 0): the cloud wakes a step after the start. Fake_1 looks like cover and isn't. Arrow_2 comes
    //    along the ground from the Plinth: jump it. Roof_2 is real cover; wait under it through a strike, then step onto
    //    G_1's vent in its tell and ride the eruption above the cloud, steering hard left past Fake_A onto SL_1.
    //  - Sky (section 1): Spear_S crosses Sky A at shin height from Wall_L: jump it. SL_2 and SL_3 give way under a cat
    //    that lingers (Spikes_4 and Spikes_3 come up where it drops, and stay). Drop_3 takes the cat back down into the
    //    storm, and the floor to V_6 (Collapse_V) gives way: wait under Roof_5 through the strike until it's back, then
    //    climb V_6 through the cloud's band.
    //  - Top (section 2): on Sky B, G_D, a Down vent over the gap, blows a cat that jumps it during an eruption back
    //    down below the cloud line, onto Spikes_3; a cat that waits at the gap's edge is crushed by Block_T. G_B launches
    //    the cat onto the Top, where the floor crumbles behind it (TL_2-TL_4), Spikes_7 come up ahead and Block_7 falls
    //    where a cat would stop: the chaos moment. G_C lifts it onto the Crown.
    //  - Dead ends: east off the lowered drop onto Spikes_3 (Dies); back to the start's roof through a strike (Recovers).
    static class L018Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;
        // Cover: a fixed roof above a jump's reach (apex 1.6 + the cat's 0.56 = 2.16), below the cloud's bottom (3.2).
        const float CoverY = 2.55f, CoverH = .5f;
        // The cloud's box is y 3.2-4.0: nothing static may sit in its sweep. Sky A's tops (4.6) are above it; Sky B's
        // underside (6.9) is above a jump from Sky A (6.76); the Top's underside (10.0) above a jump from Sky B.
        const float CloudY = 3.6f, SkyTop = 4.6f, SkyBTop = 7.4f, TopY = 10.5f, CrownY = 15.3f, SummitY = 20f, CeilingY = 25.5f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,11.25f),(1f,29.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,11.25f),(1f,29.5f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,CeilingY + .5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(30.5f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(23.2f,SummitY + .75f),(.6f,1.5f)),
                // The ground (top 0): Ground_W1 (x 0-2.4), the pit (2.4-6, Spikes_V on its bottom) with Collapse_V over 2.4-5.6, Ground_W2 (6-7.8), the drop's well
                // (7.8-9.8, top -0.5), Ground_M (9.8-13), Pit_S (13-19), Ground_E.
                E(SoloRoomElementKind.Floor,"Ground_W1",(1.2f,-1.75f),(2.4f,3.5f)),
                E(SoloRoomElementKind.Floor,"Ground_W2",(6.9f,-1.75f),(1.8f,3.5f)),
                E(SoloRoomElementKind.PitBottom,"PitV_Floor",(4.2f,-3.25f),(3.6f,.5f)),
                // Lid_V over the pit (underside 1.0, top 1.9, above a jump): a walker on Collapse_V fits under it, a jump over the
                // open pit meets it, and nothing stands on it.
                E(SoloRoomElementKind.Ceiling,"Lid_V",(3.9f,1.45f),(3f,.9f)),
                E(SoloRoomElementKind.Floor,"Ground_M",(11.4f,-1.75f),(3.2f,3.5f)),
                E(SoloRoomElementKind.Floor,"Ground_E",(25.5f,-1.75f),(13f,3.5f)),
                E(SoloRoomElementKind.Floor,"Well",(8.8f,-2f),(2f,3f)),
                E(SoloRoomElementKind.PitBottom,"PitS_Floor",(16f,-3.25f),(6f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_S",(16f,-2.85f),(6f,.3f),hazardRole:Pit),
                // The Plinth (top 0.8) holds G_1's vent and Arrow_2's launcher; Curb_1 ends Arrow_2's lane.
                E(SoloRoomElementKind.Floor,"Plinth",(20.75f,.4f),(1.5f,.8f)),
                E(SoloRoomElementKind.Wall,"Curb_1",(26f,.3f),(.4f,.6f)),
                // Cover (fixed roofs): Start_Roof over the start, Roof_2 by the vent, Roof_5 by the drop.
                E(SoloRoomElementKind.Ceiling,"Start_Roof",(31.3f,CoverY),(1.4f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Roof_2",(22.7f,CoverY),(1.4f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Roof_5",(7.35f,CoverY),(.9f,CoverH)),
                // Sky A (tops 4.6): SL_1, the landing (SL_2, SL_3 and Drop_3 are traps); Post_W, Spear_S's host on Wall_L.
                E(SoloRoomElementKind.Floor,"SL_1",(16.2f,SkyTop - .25f),(2f,.5f)),
                E(SoloRoomElementKind.Wall,"Post_W",(.2f,SkyTop + .35f),(.4f,.7f)),
                // The vine through the cloud's band, standing on Ground_W1; its top 0.6 above Sky B.
                E(SoloRoomElementKind.Vine,"V_6",(1.6f,(SkyBTop + .6f) * .5f),(.6f,SkyBTop + .6f)),
                // Sky B (7.4): SB_W (x 4-10), the gap (10-12) under Lintel_D and G_D, SB_E (12-16.8) with Step_B, G_B's vent
                // and Post_B (Arrow_B's launcher).
                E(SoloRoomElementKind.Floor,"SB_W",(7f,SkyBTop - .25f),(6f,.5f)),
                E(SoloRoomElementKind.Floor,"SB_E",(14.4f,SkyBTop - .25f),(4.8f,.5f)),
                E(SoloRoomElementKind.Ceiling,"Lintel_D",(11f,SkyBTop + 2.55f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Step_B",(14.5f,SkyBTop + .3f),(1f,.6f)),
                E(SoloRoomElementKind.Wall,"Post_B",(16.6f,SkyBTop + .5f),(.4f,1f)),
                // The Top (10.5): Top_W (x 16.5-19), the crumbles (19-23.4), Top_E (23.4-32) with Plinth_C (top 11.3).
                E(SoloRoomElementKind.Floor,"Top_W",(17.75f,TopY - .25f),(2.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Top_E",(27.7f,TopY - .25f),(8.6f,.5f)),
                E(SoloRoomElementKind.Floor,"Plinth_C",(28f,TopY + .4f),(1.5f,.8f)),
                // The Briar (top 7.5, honest spikes) under the crumbles, out of a jump's reach of their undersides.
                E(SoloRoomElementKind.Floor,"Briar_Floor",(21.5f,7.25f),(6f,.5f)),
                E(SoloRoomElementKind.Hazard,"Briar",(21.5f,7.65f),(6f,.3f)),
                // The Crown (top 15.3, x 29.6-32), G_C's landing, with V_9 up to the Summit (top 20, x 22.8-30.8) and the door;
                // under the Crown, on Top_E, the honest Thorns_C. Block_7 is flush in the Summit's underside (x 23.5-24.7).
                E(SoloRoomElementKind.Floor,"Crown",(30.8f,CrownY - .25f),(2.4f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_C",(30.4f,TopY + .15f),(3.2f,.3f)),
                E(SoloRoomElementKind.Vine,"V_9",(31.4f,(CrownY + .01f + SummitY + .6f) * .5f),(.6f,SummitY + .6f - CrownY - .01f)),
                E(SoloRoomElementKind.Floor,"Summit",(26.8f,SummitY - .25f),(8f,.5f)),
            };

            // Storm. The cloud wakes at the cut x 29.4-29.8, one step from the start; its centre ranges over x 1-30.9.
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(30.5f,CloudY),(2f,.8f),(29.6f,5f),(.4f,10f),
                new SoloRoomTrapSettings(new StormCloudSettings(1f, 30.9f), new SoloRoomTrapSettings(delayTicks:0))));
            // Fake_1 gives way on a touch, so its underside (2.35) sits above a standing jump's reach (2.16) plus 0.15.
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_1",(28f,2.6f),(1.6f,CoverH)));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2",(21.25f,.28f),(.5f,.4f),(25.2f,5f),(.4f,10f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,.28f,25.8f,unitsPerTick:.36f,tellTicks:40,disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(20.75f,.65f),(1f,.3f),settings:Geyser(150, 230)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_A",(18.5f,SkyTop - .25f),(1.4f,.5f)));

            // Sky. Spear_S (disguised, in Post_W) along Sky A at shin height to Wall_R, set off by the cut at x 15.2-15.4 on
            // SL_1 (west of the checkpoint, so a respawned cat stands clear of it).
            elements.Add(Spear("Spear_S", .2f, SkyTop + .3f, ArrowDirection.Right, 32f, 1.4f, (15.3f, 5.75f), (.2f, 2.3f), new SoloRoomTrapSettings(delayTicks:0), tell: 24, disguised: true, launcherWidth: .4f));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"SL_2",(12.4f,SkyTop - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_4",(12.1f,.15f),(1.6f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"SL_2")));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"SL_3",(10.6f,SkyTop - .25f),(1.6f,.5f),settings:new SoloRoomTrapSettings(delayTicks:24)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(10.3f,.15f),(.9f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"SL_3")));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Drop_3",(8.8f,SkyTop - .25f),(2f,.5f),(8.8f,(SkyTop + SkyBTop - .5f) * .5f),(2f,SkyBTop - .5f - SkyTop),
                new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip), new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(0f,-4.4f),
                    moveTicks:15,holdTicks:90,returnTicks:60,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:170,movingKind:MovingTrapKind.Solid))));
            // Collapse_V, the floor between Roof_5 and V_6 (over the pit, under Lid_V), gives way as the cat lands on the drop and
            // comes back after the next strike: wait for it under the roof. Its root, Spikes_V on the pit's bottom, fires once on
            // the cut over the pit, the ground under the roof and the drop's column (x 1.9-9.8, floor to ceiling), which a cat crosses first from
            // either side.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_V",(4.2f,-2.85f),(3.6f,.3f),(5.85f,(CeilingY - 3f) * .5f),(7.9f,CeilingY + 3f),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_V",(4f,-.25f),(3.2f,.5f),
                settings:new SoloRoomTrapSettings(delayTicks:5,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_V",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:145)));

            // Top. Block_T, flush in Lintel_T over the gap's edge (low enough to be in view from its first move), drops 30
            // ticks after the cat reaches the edge.
            elements.Add(E(SoloRoomElementKind.Ceiling,"Lintel_T",(9.25f,13.4f),(1.5f,1f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_T",(9.25f,13.3f),(1.5f,.8f),(9.25f,(SkyBTop + 12.9f) * .5f),(1.5f,12.9f - SkyBTop),
                new SoloRoomTrapSettings(delayTicks:30,unitsPerTick:.36f,travelDistance:12.9f - SkyBTop)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_D",(11f,SkyBTop + 2.45f),(1f,.3f),settings:Geyser(100, 660, GeyserDirection.Down)));
            // Arrow_B (disguised, in Post_B) along SB_E at head height, over a standing cat and through one hopping Step_B,
            // set off by the landing cut at x 12.2-12.6.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_B",(16.6f,SkyBTop + .85f),(.4f,.4f),(12.4f,(SkyBTop + CeilingY) * .5f),(.4f,CeilingY - SkyBTop),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,SkyBTop + .85f,0f,unitsPerTick:.36f,tellTicks:10,disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_B",(15.7f,SkyBTop - .15f),(1f,.3f),settings:Geyser(120, 800)));
            // The crumbles behind a running cat (each on its own touch), then Spikes_7 ahead (chained from TL_2) and Block_7.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"TL_2",(19.9f,TopY - .25f),(1.8f,.5f),settings:new SoloRoomTrapSettings(delayTicks:18)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"TL_3",(21.45f,TopY - .25f),(1.3f,.5f),settings:new SoloRoomTrapSettings(delayTicks:15)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"TL_4",(22.75f,TopY - .25f),(1.3f,.5f),settings:new SoloRoomTrapSettings(delayTicks:15)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_7",(26.05f,TopY + .15f),(1.5f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"TL_2")));
            // Block_7, flush in the Summit's underside, drops onto Top_E where a cat would stop. The Summit stays whole: the
            // builder never cuts a flush block out of its host, so the gap it leaves is only drawn (PAX-060, L017's ruling).
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_7",(24.4f,SummitY - .2f),(1.2f,.4f),
                settings:new SoloRoomTrapSettings(delayTicks:8,unitsPerTick:.36f,travelDistance:SummitY - .4f - TopY,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_7")));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_C",(28f,TopY + .65f),(1f,.3f),settings:Geyser(150, 980)));

            var sections = new[] {
                CheckpointSection.Start("Storm", new Vector2(30.5f, 0f), new[] { "Cloud", "Fake_1", "Arrow_2", "G_1", "Fake_A" }),
                new CheckpointSection("Sky", new Vector2(16f, SkyTop), new Rect(16.4f, SkyTop, .2f, 2.3f), new[] { "Spear_S", "SL_2", "Spikes_4", "SL_3", "Spikes_3", "Drop_3", "Spikes_V", "Collapse_V", "V_6" }),
                new CheckpointSection("Top", new Vector2(4.7f, SkyBTop), new Rect(5.4f, SkyBTop, .2f, 2.3f),
                    new[] { "Block_T", "G_D", "Arrow_B", "G_B", "TL_2", "TL_3", "TL_4", "Spikes_7", "Block_7", "G_C", "V_9" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, float length, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing, int tell = 8, bool disguised = false, float launcherWidth = .5f) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (launcherWidth, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX, length: length, tellTicks: tell, disguised: disguised), timing));
    }
}
