using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 19, "Muscle Memory" (precision, the inverter and spears). The verb is EXECUTE a memorised
    // input map: everything in the run is chained from the cat's own triggers (the step off the cliff, the orbs, the cuts),
    // so the map is the same on every attempt; only the movers run on the room's clock, and boarding them is a read.
    // 64 x 16 (ruled 2026-09-28), right to left, descending, one precision section over the bridge and both maps (D-069).
    //  - Bridge (section 0): stepping towards the cliff's edge sets the volley off. Each spear flies east from the Rack, below
    //    the cat, and sticks in the west face of the solid under the step above (the cliff, then a thin pillar), so its
    //    shaft is the next step west, 1.5 lower. Then the sweepers (arrows) take the steps back in order.
    //  - Map A (section 1): hop Thorns_5 into Orb_A: the run is inverted as it starts. Three hops mirrored (P2 gives way
    //    under a cat that lingers); the inversion ends in the jump to P3, so the held direction switches back mid-air. Wait
    //    on P3 for Mover_1, which carries the cat west to Ledge_M.
    //  - Map B (section 2): the reverse. Orb_B (disguised) inverts the cat in the jump off Ledge_M, so it switches into the
    //    mirror mid-air onto Q1; Mover_2, a lift on its own period, takes it down into the corridor under the Slab. Stepping
    //    off sets Spear_9 off at jump height and Floor_C gives way under a cat that stops: walk on under both to the door.
    static class L019Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;
        internal const float CliffY = 13f, LandY = 4f, CeilingY = 16f, StepDrop = 1.5f, StepRun = 1.2f, CliffX = 60f;
        internal const int Steps = 5;
        // Step k (1..5): its shaft's top, and the west face it sticks in (the cliff's for step 1, pillar k's after).
        internal static float StepTop(int k) => CliffY - StepDrop * k;
        internal static float Face(int k) => CliffX - StepRun * (k - 1);
        const float RackX = 52.3f, RackW = .6f, RackBottom = 4.8f, PitY = -2.5f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,20f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(64.5f,7f),(1f,20f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(32f,CeilingY + .5f),(64f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(62f,CliffY),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(1f,.75f),(.6f,1.5f)),
                // The cliff (x 60-64, top 13) and the bridge's pit (x 53.6-60).
                E(SoloRoomElementKind.Floor,"Cliff",(62f,(CliffY - 3f) * .5f),(4f,CliffY + 3f)),
                // The Rack, hanging from the ceiling over Landing A's east end: every bridge launcher is in it. Its east face
                // is thorned (honest) from its foot to step 1, 1.1 west of step 5's end: a cat that runs off a step instead of
                // dropping from its end is carried into them.
                E(SoloRoomElementKind.Wall,"Rack",(RackX,(RackBottom + CeilingY) * .5f),(RackW,CeilingY - RackBottom)),
                E(SoloRoomElementKind.Hazard,"Thorns_R",(RackX + .3f,(RackBottom + StepTop(1)) * .5f),(.2f,StepTop(1) - RackBottom)),
                // Map A: Landing A (x 44-53.6, top 4), its pit (x 22-44), P1, P3 and P4 (P2 is a trap), Ledge_M.
                E(SoloRoomElementKind.Floor,"Landing_A",(48.8f,(LandY - 3f) * .5f),(9.6f,LandY + 3f)),
                E(SoloRoomElementKind.Hazard,"Thorns_5",(49.5f,LandY + .15f),(1f,.3f)),
                E(SoloRoomElementKind.Floor,"P1",(40.2f,LandY - .25f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"P3",(31.4f,LandY - .25f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"P4",(27.2f,LandY - .25f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"Ledge_M",(18.35f,(LandY - 3f) * .5f),(3.7f,LandY + 3f)),
                // Map B: Block_E (x 11.4-16.5, top 2.7, honest thorns) under Q1; the lift's shaft (x 8.9-11.4, its pit below);
                // the Slab (x 3.2-8.7, 2.2-2.7, thorns on top) roofing the corridor (floor 0) that runs west to the door, with
                // Floor_C over the Well and a stub at each end hanging from the Slab (Spear_9's launcher and its lane's end).
                E(SoloRoomElementKind.Floor,"Block_E",(13.95f,-.15f),(5.1f,5.7f)),
                E(SoloRoomElementKind.Hazard,"Thorns_M2",(13.95f,2.85f),(5.1f,.3f)),
                E(SoloRoomElementKind.Floor,"Q1",(12.3f,LandY - .25f),(1f,.5f)),
                E(SoloRoomElementKind.Floor,"Slab",(5.95f,2.45f),(5.5f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_W",(5.95f,2.85f),(5.5f,.3f)),
                E(SoloRoomElementKind.Wall,"Stub_9",(3.4f,1.6f),(.4f,1.2f)),
                E(SoloRoomElementKind.Wall,"Stub_E",(8.5f,1.65f),(.4f,1.1f)),
                E(SoloRoomElementKind.PitBottom,"Well_Floor",(5.4f,PitY - .25f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Door_Ledge",(2.2f,-1.5f),(4.4f,3f)),
                E(SoloRoomElementKind.Floor,"Corridor_E",(7.65f,-1.5f),(2.5f,3f)),
            };
            AddPit(elements, "B", 53.6f, 60f);
            AddPit(elements, "M1", 20.2f, 44f);
            AddPit(elements, "L", 8.9f, 11.4f);
            // The pillars under steps 1-4, from the pit's floor to the next step's top (they host spears 2-5).
            for (int k = 2; k <= Steps; k++)
                elements.Add(E(SoloRoomElementKind.Wall,$"Pillar_{k}",(Face(k) + .2f,(StepTop(k) + PitY) * .5f),(.4f,StepTop(k) - PitY)));

            // The volley: the cut on the cliff (x 61.3-61.7) fires Spear_1 20 ticks later; each next spear 12 after the last.
            elements.Add(Spear("Spear_1", StepTop(1) - .2f, Face(1), (61.5f, (CliffY + CeilingY) * .5f), (.4f, CeilingY - CliffY), new SoloRoomTrapSettings(delayTicks:20)));
            for (int k = 2; k <= Steps; k++)
                elements.Add(Spear($"Spear_{k}", StepTop(k) - .2f, Face(k), default, default, Chain($"Spear_{k - 1}", 12)));
            // The sweepers: arrows from the Rack across a step at standing height (or, Sweeper_H, jump height), ending at the
            // face of the solid above that step's west end; chained from Spear_5. Sweeper_2 takes a cat that stops on step 2,
            // Sweeper_4 one that runs on from step 3, Sweeper_3 one that stands on step 3; Sweeper_H crosses step 4 at jump
            // height while the cat waits there.
            elements.Add(Sweeper("Sweeper_2", StepTop(2) + .28f, Face(1), Chain("Spear_5", 64)));
            elements.Add(Sweeper("Sweeper_4", StepTop(4) + .28f, Face(3), Chain("Spear_5", 89)));
            elements.Add(Sweeper("Sweeper_3", StepTop(3) + .28f, Face(2), Chain("Spear_5", 100)));
            elements.Add(Sweeper("Sweeper_H", StepTop(4) + .85f, Face(3), Chain("Spear_5", 182)));

            // Map A. Orb_A (honest) inverts 150 steps; P2 gives way 20 ticks after a touch.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_A",(46.5f,LandY + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(150),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"P2",(35.8f,LandY - .25f),(1f,.5f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            // Mover_1 (Carry, on the room's clock, every 180): home by P3 (x 23.8-25.3), away by Ledge_M (20.3-21.8).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Mover_1",(24.55f,LandY - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(-3.5f,0f),moveTicks:40,holdTicks:30,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:180,phaseTicks:0,cooldownTicks:130,movingKind:MovingTrapKind.Solid))));

            // Map B. Orb_B (disguised) fires 14 ticks after the cut near Ledge_M's west edge (x 17.0-17.4): in the jump.
            elements.Add(E(SoloRoomElementKind.Inverter,"Orb_B",(17.2f,LandY + 1.5f),(.4f,3f),(17.2f,(LandY + CeilingY) * .5f),(.4f,CeilingY - LandY),
                new SoloRoomTrapSettings(new InverterSettings(70, disguised: true),new SoloRoomTrapSettings(delayTicks:14))));
            // Mover_2, a lift (Carry, every 200): home level with Q1 (x 8.9-10.4), down to the corridor.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Mover_2",(9.65f,LandY - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,-4f),moveTicks:40,holdTicks:30,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:200,phaseTicks:30,cooldownTicks:120,movingKind:MovingTrapKind.Solid))));
            // The corridor: a step west of the lift (the cut at x 7.1-7.5) fires Spear_9 (disguised, in Stub_9) east at jump height
            // into Stub_E; Floor_C (filling the Well, x 4.4-6.4) gives way 20 ticks after a touch onto Spikes_C (hidden).
            elements.Add(E(SoloRoomElementKind.Arrow,"Spear_9",(3.4f,1.3f),(.4f,.4f),(7.3f,1.1f),(.4f,2.2f),
                new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right, 1.3f, 8.3f, tellTicks: 12, disguised: true), new SoloRoomTrapSettings(delayTicks:4))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Floor_C",(5.4f,-.25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C",(5.4f,PitY + .15f),(2f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Floor_C")));

            var sections = new[] {
                CheckpointSection.Start("Bridge", new Vector2(62f, CliffY), new[] { "Spear_1", "Spear_2", "Spear_3", "Spear_4", "Spear_5", "Sweeper_2", "Sweeper_4", "Sweeper_3", "Sweeper_H" }),
                new CheckpointSection("Map_A", new Vector2(50.8f, LandY), new Rect(52.2f, LandY, .2f, RackBottom - LandY), new[] { "Orb_A", "P2", "Mover_1" }),
                new CheckpointSection("Map_B", new Vector2(18.9f, LandY), new Rect(19.6f, LandY, .2f, CeilingY - LandY), new[] { "Orb_B", "Mover_2", "Spear_9", "Floor_C", "Spikes_C" }),
            };
            var precision = new[] { new PrecisionSection("Run", Rect.MinMaxRect(2f, -3f, 60f, CeilingY)) };
            return new SoloRoomDefinition(0, 0f, 64f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, precision, null, sections);
        }

        // A pit from x0 to x1: its floor (top -2.5) and its honest hazard.
        static void AddPit(List<SoloRoomElement> elements, string name, float x0, float x1)
        {
            elements.Add(E(SoloRoomElementKind.PitBottom, $"Pit{name}_Floor", ((x0 + x1) * .5f, PitY - .25f), (x1 - x0, .5f)));
            elements.Add(E(SoloRoomElementKind.Hazard, $"Pit_{name}", ((x0 + x1) * .5f, PitY + .15f), (x1 - x0, .3f), hazardRole: Pit));
        }

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // A spear from the Rack, flying east along `laneY` and sticking in the west face at `laneEndX`.
        static SoloRoomElement Spear(string name, float laneY, float laneEndX, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (RackX, laneY), (RackW, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right, laneY, laneEndX, disguised: true), timing));

        // An arrow from the Rack, east along `laneY` to the face at `laneEndX` (a spear's host above the step it sweeps).
        static SoloRoomElement Sweeper(string name, float laneY, float laneEndX, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (RackX, laneY), (RackW, .4f), settings: new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right, laneY, laneEndX, unitsPerTick: .36f, tellTicks: 8, disguised: true), timing));
    }
}
