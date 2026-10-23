using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-164: level 21, "Ignition", the first of levels 21-30 (the developer, 2026-10-22). The verb is RIDE THE VENTS UP: a run
    // along the ground into a shaft, three thermal vents climbed one after another, each landing giving way under the cat,
    // and a run home along the top with a spiked wall chasing.
    //  - Ground (section 0, walked east): the cat starts on a bridge over a spike chasm and it falls away tile by tile behind
    //    it from its first step. G_T, a vent in the ground under Spikes_T, throws a cat that crosses it while it erupts into
    //    the spikes (wait it out). Slam_1 comes down out of the stone ahead (stop short, hop it); Arrow_1 shoots along the
    //    ground from the Plinth without stopping (hop Kerb_1 between shots); Collapse_P gives way under a cat that steps on
    //    it (jump straight onto the Plinth).
    //  - Shaft (section 1): V1 on the Plinth throws the cat up onto Crumb_2, which gives way under it: on at once to V2 at
    //    Ledge_2's end, whose eruption throws it up onto Crumb_3, which gives way too: on at once to V3, which throws it up
    //    onto the Lip. The vents erupt one after another, 60 ticks apart, every 120 ticks. Below the shaft, Spikes_Sh.
    //  - Crown (section 2, walked west): Hinge_F, spiked, stands up behind the cat and chases it west at a little under a
    //    run. G_FD, a vent in the roof, is still blowing down over Spikes_FD (wait it out, with the wall coming, then jump
    //    them); Slam_F comes down out of the roof ahead (stop short, hop onto it before the wall arrives); the crumbles give
    //    way over the Briar (jump them); Pop_F springs up from the floor by the door (jump it).
    static class L021Layout
    {
        // The ground's walk line, the Core's underside and top (the top storey's walk line), the roof's underside and top.
        internal const float S1 = 0f, C1 = 3.5f, ST = 13.8f, RT = 17.3f, RoofTop = 19.3f, PitFloor = -3f;
        // The shaft's ledges: V1 on the Plinth (top 1), Ledge_2 (top 5.2) on the east wall, Ledge_3 (top 9.5) on the Core.
        internal const float P1Top = 1f, L2Top = 5.2f, L3Top = 9.5f;
        // The bridge: five tiles from the start ledge to the ground, each falling BridgeStep ticks after the one before.
        internal const int BridgeCount = 5, BridgeFirstDelay = 10, BridgeStep = 15;
        internal const float LipTop = ST - .02f, LedgeTop = S1 + .02f, BridgeStart = 1.3f, BridgeWidth = 1.3f;
        // The vents' beats (room ticks: period, first eruption). G_T keeps its own; V1-V3 go off 60 ticks apart.
        internal const int GTPeriod = 90, GTErupt = 70, VPeriod = 120, V1Erupt = 265, V2Erupt = V1Erupt + 60, V3Erupt = V1Erupt + 120, FDErupt = V3Erupt + 28;
        // The chase: from Hinge_F's stand, west, a little under a run (0.12 u a tick).
        internal const float ChaseDistance = 11f;
        internal const int ChaseDelay = 16, ChaseTicks = 125;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(PitFloor - 1f + RoofTop) * .5f),(1f,RoofTop - PitFloor + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(PitFloor - 1f + RoofTop) * .5f),(1f,RoofTop - PitFloor + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(.6f,LedgeTop),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(.8f,ST + .75f),(.6f,1.5f)),
            };

            // ---- The ground (y -2..0) and the shaft's floor ----
            // The start ledge on the west wall; the chasm under the bridge (x 1.3-7.8); the ground from x 7.8 to the shaft (x 20);
            // Collapse_P over PitP (x 20-22.5); the Plinth (x 22.5-25.5, top 1) holds V1 and Arrow_1; Spikes_Sh on the shaft's
            // floor east of it.
            Block(elements, SoloRoomElementKind.Floor, "Ledge_S", 0f, BridgeStart, S1 - 1.5f, LedgeTop);
            Pit(elements, "PitW", BridgeStart, 7.8f, opening: false);
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 7.8f, 20f, -4f, S1);
            elements.Add(E(SoloRoomElementKind.PitBottom,"PitP_Bottom",(21.25f,-1.5f),(2.5f,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"PitP",(21.25f,-.85f),(2.5f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
            Block(elements, SoloRoomElementKind.Floor, "Plinth", 22.5f, 25.5f, -4f, P1Top);
            Block(elements, SoloRoomElementKind.Floor, "Floor_Sh", 25.5f, 32f, -4f, S1);
            Spikes(elements, "Spikes_Sh", 25.5f, 32f, S1);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_1", 16.6f, 17f, S1, S1 + .6f);

            // ---- The Core (y 3.5-13.8): the ground's ceiling below, the top storey's floor above ----
            // Slam_1 is its bottom unit at x 13.5-15 (under Cap_1); the Briar's recess (x 6.5-9.5, 1 u deep) under the crumbles;
            // Hinge_F the top unit at x 21-22.5 (on Seat_F) of the Lip, which reaches over the shaft to x 22.5.
            Block(elements, SoloRoomElementKind.Floor, "Core_W", 0f, 6.5f, C1, ST);
            Block(elements, SoloRoomElementKind.PitBottom, "Briar_Floor", 6.5f, 9.5f, C1, ST - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Core_M", 9.5f, 13.5f, C1, ST);
            Block(elements, SoloRoomElementKind.Floor, "Cap_1", 13.5f, 15f, C1 + .75f, ST);
            Block(elements, SoloRoomElementKind.Floor, "Core_E", 15f, 20f, C1, ST);
            // The Lip stands a hair below Hinge_F's unit (and clear of its sweep), so a landing across their seam settles on one of them
            // (L001's start ledge).
            Block(elements, SoloRoomElementKind.Floor, "Lip", 20f, 21f, ST - 1f, LipTop);
            Block(elements, SoloRoomElementKind.Floor, "Seat_F", 21f, 22.5f, ST - 1f, ST - .5f);
            // The shaft's ledges: Ledge_2 on the east wall (Crumb_2 its west end), Ledge_3 on the Core's east face (Crumb_3 its
            // east end).
            Block(elements, SoloRoomElementKind.Floor, "Ledge_2", 27.4f, 32f, L2Top - 1f, L2Top);
            Block(elements, SoloRoomElementKind.Floor, "Ledge_3", 20f, 25f, L3Top - 1f, L3Top);

            // ---- The roof (y 17.3-19.3) ----
            // Slam_F (x 12.5-14) is its bottom unit under Cap_F; G_FD's vent is in Roof_B's underside at x 17-18.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 12.5f, RT, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_F", 12.5f, 14f, RT + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 14f, 32f, RT, RoofTop);

            // Honest spikes: Spikes_T on the Core's underside over G_T, and the Briar.
            Spikes(elements, "Spikes_T", 9.6f, 11.4f, C1, down: true);
            Spikes(elements, "Briar", 6.5f, 9.5f, ST - 1f);
            Spikes(elements, "Spikes_FD", 16.9f, 18.1f, ST);

            // ---------- Ground ----------
            // The bridge: five tiles over the chasm. The cat's first step onto it (its trigger is the chasm's full height, up to the roof) sets
            // Bridge_1 off; each next tile goes BridgeStep ticks after the one before, a little slower than a run: a cat that runs
            // is on the ground before the wave reaches it, one that stops goes down with its tile onto PitW's spikes (each tile
            // lands with its top under the spikes' tips).
            for (int i = 0; i < BridgeCount; i++)
            {
                float x0 = BridgeStart + i * BridgeWidth, cx = x0 + BridgeWidth * .5f;
                const float travel = S1 - PitFloor + .25f;
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.FallingBlock, "Bridge_1", (cx, S1 - .25f), (BridgeWidth, .5f), ((BridgeStart - .05f + 7.8f) * .5f, (PitFloor + RT) * .5f), (7.8f - BridgeStart + .05f, RT - PitFloor),
                        new SoloRoomTrapSettings(delayTicks:BridgeFirstDelay,unitsPerTick:.25f,travelDistance:travel))
                    : E(SoloRoomElementKind.FallingBlock, "Bridge_" + (i + 1), (cx, S1 - .25f), (BridgeWidth, .5f),
                        settings:new SoloRoomTrapSettings(delayTicks:BridgeStep,unitsPerTick:.25f,travelDistance:travel,triggerSource:TrapTriggerSource.Chain,chainSource:"Bridge_" + i)));
            }
            // G_T, a vent in the ground at x 10-11 under Spikes_T: its eruption throws a cat on it into the spikes.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_T",(10.5f,S1 - .15f),(1f,.3f),settings:Geyser(GTPeriod, GTErupt)));
            // Slam_1 slams 3.5 u onto the ground 9 ticks after a cat walking east comes into its cut (x 11.8-12.2), and stays:
            // a cat that runs on is under it; stop short, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(14.25f,C1 + .375f),(1.5f,.75f),(12f,(S1 + C1) * .5f),(.4f,C1 - S1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,-C1),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Arrow_1 shoots west along the ground at shin height from the Plinth to Kerb_1, from the moment the cat crosses x 15.4,
            // non-stop.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1",(22.75f,S1 + .3f),(.5f,.4f),(15.4f,(S1 + C1) * .5f),(.4f,C1 - S1),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,S1 + .3f,17f,unitsPerTick:.3f,tellTicks:10),new SoloRoomTrapSettings(delayTicks:10,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:50))));
            // Collapse_P fills PitP's mouth and gives way 4 ticks after a touch: jump from the ground straight onto the Plinth.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_P",(21.25f,-.5f),(2.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));

            // ---------- Shaft ----------
            // The vents, 60 ticks apart: V1 on the Plinth (x 24-25), V2 at Ledge_2 (x 27.5-28.5), V3 on Ledge_3 (x 23-24). Each
            // landing is a crumble that gives way 16 ticks after a touch: the cat has to go straight on to the next vent.
            elements.Add(E(SoloRoomElementKind.Geyser,"V1",(24.5f,P1Top - .15f),(1f,.3f),settings:Geyser(VPeriod, V1Erupt)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumb_2",(26.7f,L2Top - .25f),(1.4f,.5f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            elements.Add(E(SoloRoomElementKind.Geyser,"V2",(28f,L2Top - .15f),(1f,.3f),settings:Geyser(VPeriod, V2Erupt)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumb_3",(25.75f,L3Top - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            elements.Add(E(SoloRoomElementKind.Geyser,"V3",(23.5f,L3Top - .15f),(1f,.3f),settings:Geyser(VPeriod, V3Erupt)));

            // ---------- Crown ----------
            // Hinge_F, the Lip's top unit at x 21-22.5 (hinged at its west end, spiked, so its spikes face the cat), stands up
            // behind the cat as it walks west past x 19.6 and slides ChaseDistance west over ChaseTicks: a cat that stops is caught.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_F",(21.75f,ST - .25f),(1.5f,.5f),(19.6f,(ST + RT) * .5f),(.4f,RT - ST),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false,spiked:true),
                new SoloRoomTrapSettings(delayTicks:ChaseDelay,offset:new Vector2(-ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // G_FD, a vent in the roof at x 17-18 over Spikes_FD, on the shaft's 120-tick clock, its column 2 u (down to y 15.3, so
            // every jump over the spikes passes through it): its eruption blows a cat that jumps
            // the spikes down onto them, and it is still erupting as the cat comes off the Lip with the wall behind it.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_FD",(17.5f,RT + .15f),(1f,.3f),settings:Geyser(VPeriod, FDErupt, GeyserDirection.Down, 2f)));
            // Slam_F slams 3.5 u onto the floor 4 ticks after a cat walking west reaches x 15 (its trigger holds its whole travel),
            // and stays: stop short, hop onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F",(13.25f,RT + .375f),(1.5f,.75f),(13.75f,(ST + RT + 1.4f) * .5f),(2.5f,RT + 1.4f - ST),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,ST - RT),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Crumble_F and Crumble_G over the Briar give way 3 ticks after a touch: jump them.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumble_F",(8.75f,ST - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumble_G",(7.25f,ST - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            // Pop_F: spikes in the floor at x 2.5-4 spring up as the cat comes within 1.5 u of them: jump them, into the door.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_F",(3.25f,ST + .15f),(1.5f,.3f),(4f,(ST + RT) * .5f),(3f,RT - ST),new SoloRoomTrapSettings(revealDelayTicks:6)));

            var sections = new[] {
                CheckpointSection.Start("Ground", new Vector2(.6f, LedgeTop), new[] { "Bridge_1", "Bridge_2", "Bridge_3", "Bridge_4", "Bridge_5", "G_T", "Slam_1", "Arrow_1", "Collapse_P" }),
                new CheckpointSection("Shaft", new Vector2(23.1f, P1Top), new Rect(22.9f, P1Top, .2f, C1),
                    new[] { "V1", "Crumb_2", "V2", "Crumb_3", "V3" }),
                new CheckpointSection("Crown", new Vector2(20.5f, LipTop), new Rect(21.1f, ST, .2f, RT - ST),
                    new[] { "Hinge_F", "G_FD", "Slam_F", "Crumble_F", "Crumble_G", "Pop_F" }),
            };
            var openings = new[] { O(SoloRoomOpeningKind.Pit, 20f, 22.5f, "Ground_B", "Plinth", "PitP_Bottom", "PitP") };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));

        // A pit between two solids: its bottom from y -4 up to PitFloor and the spikes on it (open air under a free-standing
        // bridge is no opening).
        static void Pit(List<SoloRoomElement> elements, string id, float minX, float maxX, bool opening = true)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.PitBottom,$"{id}_Bottom",(centre,(PitFloor - 4f) * .5f),(width,PitFloor + 4f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"{id}_Hazard",(centre,PitFloor + .15f),(width,.3f),hazardRole:opening ? SoloRoomHazardRole.OpeningBottom : SoloRoomHazardRole.Normal));
        }

        // A geyser (defaults: tell 25, erupt 40, column 1.5 u) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up, float column = GeyserMath.DefaultColumnHeight) =>
            new(new GeyserSettings(direction, columnHeight: column), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
