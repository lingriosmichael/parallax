using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 15, "Hunted" (the storm cloud). The verb is KEEP MOVING, and find where you may stop. 64 x 14,
    // left to right, one floor (top 0) with pits down to -3 and a ceiling at 9. The cloud wakes a step after the start and
    // follows at 0.08 u a tick; it charges every 100 ticks and strikes where it stopped. A running cat (0.12) outruns it;
    // a cat that stops is struck, unless it stands under real cover: a fixed roof (y 2.3-2.8, above a jump), which stops the
    // strike on its top.
    //  - Open (section 0): jump the gap. Landing sets off Arrow_3 (disguised, shin height, from Post_C): jump it. The floor
    //    ahead (Collapse_2) gives way as the cat lands: shelter under Overhang_B and wait out a strike until it comes back.
    //    Dead end D1: back to the start alcove under Start_Roof.
    //  - Shelter (section 1): past Roof_4 to Overhang_4, at the mover pit's lip. Wait under it for
    //    Mover_M (Carry, 0.1 u a tick, faster than the cloud) to dock, ride it standing, step off onto Floor_D. Then the spear
    //    pit: walking on sets off Spear_P, which sticks in Floor_D's face as the one floor under Roof_S; standing on it sets
    //    off Spear_B (90 ticks later) as the bridge; wait under the roof through a strike, then jump onto the bridge.
    //  - Run (section 2): the cave-in (Block_1-3 falling behind a running cat); the last block knocks out D3, the dance's
    //    last platform, until the cooldown ends. The dance: on D1 and D2 over Pit_9, with nowhere to go on and no cover, hop
    //    out from under each lock (back, when D3 is gone), then on over D3 onto P1 under Roof_9, which sets off Arrow_9
    //    across P2: wait until it has stopped; then P2, and past the cloud's range. The
    //    finale: landing on Floor_F1 sets off Spear_10 (disguised, 36-tick tell) along Floor_F2 below it, the way to the
    //    door; stop dead on Floor_F1, let it stick in its face, then go down over it and hop Curb_F to the door.
    static class L015Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;
        // Cover: a fixed roof above a jump's reach (apex 1.6 + the cat's 0.56 = 2.16).
        const float CoverY = 2.55f, CoverH = .5f;
        // D3's cooldown: from Block_3 knocking it out until it's back, through the dance's locks.
        const int DanceGateTicks = 257;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,3.25f),(1f,13.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(64.5f,3.25f),(1f,13.5f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(32f,9.5f),(64f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(63.6f,.75f),(.6f,1.5f)),
                // Floors (tops 0) and pits (floors -3): the gap G (x 7-9), Pit_2 (13-17), Pit_M (25-30), Pit_S (34-40), Pit_9 (46.2-58).
                E(SoloRoomElementKind.Floor,"Floor_A",(3.5f,-1.75f),(7f,3.5f)),
                E(SoloRoomElementKind.Floor,"Floor_B",(11f,-1.75f),(4f,3.5f)),
                E(SoloRoomElementKind.Floor,"Floor_C",(21f,-1.75f),(8f,3.5f)),
                E(SoloRoomElementKind.Floor,"Floor_D",(32f,-1.75f),(4f,3.5f)),
                E(SoloRoomElementKind.Floor,"Floor_E",(43.1f,-1.75f),(6.2f,3.5f)),
                // The finale: Floor_F1 (x 58-60.4, top 0.9), the ledge to stop on; Floor_F2 (x 60.4-64, top 0) to the door.
                E(SoloRoomElementKind.Floor,"Floor_F1",(59.2f,-1.3f),(2.4f,4.4f)),
                E(SoloRoomElementKind.Floor,"Floor_F2",(62.2f,-1.75f),(3.6f,3.5f)),
                E(SoloRoomElementKind.PitBottom,"PitG_Floor",(8f,-3.25f),(2f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_G",(8f,-2.85f),(2f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"Pit2_Floor",(15f,-3.25f),(4f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_2",(15f,-2.85f),(4f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"PitM_Floor",(27.5f,-3.25f),(5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_M",(27.5f,-2.85f),(5f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"PitS_Floor",(37f,-3.25f),(6f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_S",(37f,-2.85f),(6f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.PitBottom,"Pit9_Floor",(52.1f,-3.25f),(11.8f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_9",(52.1f,-2.85f),(11.8f,.3f),hazardRole:Pit),
                // Cover (fixed roofs): the start alcove, Overhang_B, Roof_4, Overhang_4, Roof_S, Roof_9. Every roof under the
                // cloud is real: a strike through a roof reads as a bug, not a trap (the developer's play, D-093 (4c)).
                E(SoloRoomElementKind.Ceiling,"Start_Roof",(.9f,CoverY),(1.8f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Overhang_B",(10.7f,CoverY),(2.2f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Roof_4",(20.5f,CoverY),(2f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Overhang_4",(24.25f,CoverY),(1.5f,CoverH)),
                E(SoloRoomElementKind.Ceiling,"Roof_S",(34.4f,CoverY),(1.6f,CoverH)),
                // Roof_9 covers P1 (top 0.8), so it sits 0.8 higher than the others.
                E(SoloRoomElementKind.Ceiling,"Roof_9",(54.5f,CoverY + .8f),(1.8f,CoverH)),
                // Posts and curbs (arrow hosts and lane ends); Arrow_9's launcher is in Floor_F1.
                E(SoloRoomElementKind.Wall,"Curb_B",(9.2f,.3f),(.4f,.6f)),
                E(SoloRoomElementKind.Wall,"Post_C",(17.25f,.4f),(.5f,.8f)),
                // The dance over Pit_9 (precision section "Dance"): D1 (x 47.2-48.2, top 0), D2 (49.2-50.2, top -0.9), D3 (the
                // crumble, 51.5-52.5, top 0.2, below), then P1 (53.8-55.2, top 0.8, 1 u deep so its east face ends Arrow_9's lane).
                E(SoloRoomElementKind.Floor,"D1",(47.7f,-.25f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"D2",(49.7f,-1.15f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"P1",(54.5f,.3f),(1.4f,1f)),
                E(SoloRoomElementKind.Wall,"Stop_9",(55.1f,1.1f),(.2f,.6f)),
                E(SoloRoomElementKind.Floor,"P2",(56.5f,-.25f),(1f,.5f)),
                E(SoloRoomElementKind.Wall,"Curb_F",(62.8f,.3f),(.4f,.6f)),
            };

            // The cloud: wakes at the cut x 5-5.5; its centre ranges over x 2-55 (P2 from 56 is past it).
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(3f,7.6f),(2f,.8f),(5.25f,4.5f),(.5f,9f),
                new SoloRoomTrapSettings(new StormCloudSettings(2f, 55f), new SoloRoomTrapSettings(delayTicks:0))));

            // Open. Arrow_3's trigger is floor to ceiling from x 10.6 to its launcher (the cat enters it as it lands after the
            // gap), so it cuts the whole band for Collapse_2, chained from it. Collapse_2 (x 13-16.5; Pit_2 shows beyond it, before Post_C),
            // 3 u ahead, gives way with it and comes back 150 ticks later; Arrow_3 fires once, so it stays.
            // D-119 (the developer: all launchers shoot non-stop): Arrow_3 keeps its first shot's trigger, then fires every 260 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_3",(17.25f,.28f),(.5f,.4f),(13.8f,4.5f),(6.4f,9f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,.28f,9.4f,unitsPerTick:.36f,tellTicks:12,disguised:true),new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:260))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_2",(14.75f,-.25f),(3.5f,.5f),
                settings:new SoloRoomTrapSettings(delayTicks:1,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_3",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:150)));

            // Shelter.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Mover_M",(26.05f,-.25f),(2f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(2.9f,0f),moveTicks:29,holdTicks:40,returnTicks:29,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:98,movingKind:MovingTrapKind.Solid))));
            elements.Add(Spear("Spear_P", 40.25f, -.2f, ArrowDirection.Left, 34f, 1f, (32f, 4.5f), (.5f, 9f), new SoloRoomTrapSettings(delayTicks:0)));
            elements.Add(Spear("Spear_B", 33.75f, -.6f, ArrowDirection.Right, 40f, 2.8f, (34.65f, 1.15f), (1.1f, 2.3f), new SoloRoomTrapSettings(delayTicks:90)));

            // Run. The cave-in: the cut at x 42-42.4 drops Block_1 (10 ticks later), then Block_2 and Block_3 (each 12 after the
            // last), flush in the ceiling within 3 u of the cut, each landing just behind a running cat. Block_3 also knocks out
            // D3 (the dance's last platform, a crumble), which comes back after the cooldown: the gate.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(43f,9.5f),(1.2f,.8f),(42.2f,4.5f),(.4f,9f),Drop(new SoloRoomTrapSettings(delayTicks:10))));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(44.3f,9.5f),(1.2f,.8f),settings:Drop(Chain("Block_1", 12))));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_3",(45.5f,9.5f),(1.2f,.8f),settings:Drop(Chain("Block_2", 12))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"D3",(52f,-.05f),(1f,.5f),
                settings:new SoloRoomTrapSettings(delayTicks:1,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_3",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:DanceGateTicks)));
            // Arrow_9 crosses P2 once, from Floor_F1's west face to P1's east face (Stop_9 above it), set off by the cut at
            // x 54-54.4 as the cat jumps onto P1: a 50-tick tell the cat on P1 sees, then about 8 ticks across P2, where a cat
            // that hopped on at once has just landed. Wait under Roof_9 until it has stopped.
            // D-119 (the developer: all launchers shoot non-stop): Arrow_9 keeps its first shot's trigger, then fires every 230 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_9",(58.2f,.3f),(.4f,.4f),(54.2f,4.5f),(.4f,9f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,.3f,55.2f,unitsPerTick:.36f,tellTicks:50),new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:230))));
            // The finale: the cut at x 58.5-58.9, on Floor_F1, sets off Spear_10 (disguised, 36-tick tell) from Curb_F along
            // Floor_F2 at shin height; it sticks in Floor_F1's east face. A cat that stops on Floor_F1 is above the lane; a cat
            // that runs on drops onto Floor_F2 into it.
            elements.Add(Spear("Spear_10", 62.8f, .3f, ArrowDirection.Left, 60.4f, 1.2f, (58.7f, 4.5f), (.4f, 9f), new SoloRoomTrapSettings(delayTicks:0), tell: 36, disguised: true, launcherWidth: .4f));

            var sections = new[] {
                CheckpointSection.Start("Open", new Vector2(2f, 0f), new[] { "Cloud", "Arrow_3", "Collapse_2" }),
                new CheckpointSection("Shelter", new Vector2(18.5f, 0f), new Rect(17.9f, 0f, .2f, 9f), new[] { "Mover_M", "Spear_P", "Spear_B" }),
                new CheckpointSection("Run", new Vector2(41.2f, 0f), new Rect(40.6f, 0f, .2f, 9f), new[] { "Block_1", "Block_2", "Block_3", "D3", "Arrow_9", "Spear_10" }),
            };
            // D-083: the dance is a precision section (Floor_E's end to P1). Its bait gaps: with D3 gone, neither D2 (1.7 below
            // P1, above the jump's 1.6) nor D1 reaches P1.
            var precision = new[] { new PrecisionSection("Dance", Rect.MinMaxRect(46.2f, -3f, 53.8f, 9f)) };
            var baits = new[] { new BaitGap("D2_P1", 50.2f, -.9f, 53.8f, .8f), new BaitGap("D1_P1", 48.2f, 0f, 53.8f, .8f) };
            return new SoloRoomDefinition(0, 0f, 64f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, precision, baits, sections);
        }

        // A falling block flush in the ceiling (y 9.1-9.9) that falls to the floor.
        static SoloRoomTrapSettings Drop(SoloRoomTrapSettings timing) =>
            new(delayTicks: timing.DelayTicks, unitsPerTick: .36f, travelDistance: 9.1f, triggerSource: timing.TriggerSource, chainSource: timing.ChainSource);

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, float length, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing, int tell = 8, bool disguised = false, float launcherWidth = .5f) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (launcherWidth, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX, length: length, tellTicks: tell, disguised: disguised), timing));
    }
}
