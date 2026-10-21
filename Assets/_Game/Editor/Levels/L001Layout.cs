using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-159 (the developer, 2026-10-20: "I have one opportunity to catch a users attention, and that is in level 1 ... it has to
    // be fun"; they picked "Run!"): level 1 is a loop over two storeys 5 u apart, so both share the screen.
    //  - The bridge: the cat starts on a bridge over a spike chasm, and it falls away tile by tile behind the cat from its
    //    first step. Run.
    //  - The top storey, east: Slam_1 comes down out of the roof on a cat that runs on (stop, let it go back up, pass);
    //    Hinge_1 stands up behind the cat and shoves it toward Spikes_H (jump them, don't stop); Block_1 drops out of the roof
    //    ahead (stop, then hop it); off the end, down to the lower storey.
    //  - The lower storey, west, with the storm cloud chasing at a run: Crumble gives way (jump it), Pop springs up ahead (jump
    //    it), and the door rides its lift up and away over the pit as the cat comes to it: leap after it.
    // The chasm under the bridge is walled off by the Pier, so the only way down is the drop at the far end.
    static class L001Layout
    {
        // The lower storey's floor, the top slab's underside and top, and the roof's underside and top (room-local y).
        internal const float F1 = 0f, C1 = 3f, F2 = 5f, C2 = 9.5f, RoofTop = 11.5f, PitFloor = -3f;
        // The bridge: four tiles from the start ledge to the Pier, each falling BridgeStep ticks after the one before.
        internal const int BridgeCount = 4, BridgeFirstDelay = 10, BridgeStep = 15;
        // The start ledge stands a hair above the bridge, so the cat steps down onto it (a level seam between stone and a
        // falling tile let two replays' contacts differ by a millionth).
        internal const float LedgeTop = F2 + .02f;
        internal const float BridgeStart = 1.3f, BridgeWidth = 1.3f, BridgeTravel = 7.75f;
        // The door's lift: up and west, over the pit, as the cat comes within Retreat's trigger.
        internal static readonly Vector2 DoorRide = new(-2.8f, .8f);
        internal const int DoorRideTicks = 30;
        static readonly (float x, float y) DoorTrigger = (17.75f, (F1 + C1) * .5f);

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,3.75f),(1f,15.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,3.75f),(1f,15.5f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(.6f,LedgeTop),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(13.6f,F1 + .75f),(.6f,1.5f)),
            };
            // The start: a ledge on the west wall, where the bridge begins.
            Block(elements, SoloRoomElementKind.Floor, "Ledge_S", 0f, BridgeStart, F2 - 1.5f, LedgeTop);
            // The Pier: stone from the chasm's floor to the top storey, between the chasm and the lower storey's pit.
            Block(elements, SoloRoomElementKind.Floor, "Pier", 6.5f, 8.5f, -4f, F2);
            // The lower storey's ground, around Crumble's shaft (x 24-26); the door's pit runs from the Pier to x 14.5.
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", 14.5f, 24f, -4f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Ground_2", 26f, 32f, -4f, F1);
            // The top storey's slab (its underside is the lower storey's ceiling), around Hinge_1's seat; it ends at x 30, where
            // the cat drops to the lower storey.
            Block(elements, SoloRoomElementKind.Floor, "Slab_A", 8.5f, 15f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H", 15f, 16.5f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Slab_B", 16.5f, 30f, C1, F2);
            // The roof, 2 u of stone, around Slam_1's cap; Block_1 is a section of Roof_B's underside.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 11.8f, C2, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S", 11.8f, 13.3f, C2 + 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 13.3f, 32f, C2, RoofTop);

            // The pits: the chasm under the bridge, Crumble's shaft, and the door's pit. The chasm is open air under a
            // free-standing bridge, so it is no opening and has no back wall: the sky shows through it, before and after the
            // bridge falls (the developer's rule, 2026-10-19). The two pits in the lower storey's ground show its recessed stone.
            Pit(elements, "PitW", 0f, 6.5f, opening: false);
            Pit(elements, "PitC", 24f, 26f);
            // The door's pit is 1 u deep, so its spikes are in view from the lower storey: the lift carries the door over them.
            Pit(elements, "PitD", 8.5f, 14.5f, floor: F1 - 1f);

            // ---------- The bridge ----------
            // Four tiles over the chasm. The cat's first step onto it (its trigger is the chasm's full height, its floor to the roof)
            // sets Bridge_1 off; each next tile goes BridgeStep ticks after the one before, a little slower than a run: a cat that
            // runs is on the Pier before the wave reaches it, one that stops goes down with its tile onto PitW's spikes (each
            // tile lands with its top under the spikes' tips, so a cat riding one dies on them).
            for (int i = 0; i < BridgeCount; i++)
            {
                float x0 = BridgeStart + i * BridgeWidth, cx = x0 + BridgeWidth * .5f;
                elements.Add(i == 0
                    ? E(SoloRoomElementKind.FallingBlock, "Bridge_1", (cx, F2 - .25f), (BridgeWidth, .5f), ((BridgeStart - .05f + 6.5f) * .5f, (PitFloor + C2) * .5f), (6.5f - BridgeStart + .05f, C2 - PitFloor),
                        new SoloRoomTrapSettings(delayTicks:BridgeFirstDelay,unitsPerTick:.25f,travelDistance:BridgeTravel))
                    : E(SoloRoomElementKind.FallingBlock, "Bridge_" + (i + 1), (cx, F2 - .25f), (BridgeWidth, .5f),
                        settings:new SoloRoomTrapSettings(delayTicks:BridgeStep,unitsPerTick:.25f,travelDistance:BridgeTravel,triggerSource:TrapTriggerSource.Chain,chainSource:"Bridge_" + i)));
            }

            // ---------- The top storey ----------
            // Slam_1, the roof's bottom unit at x 11.8-13.3 (under Cap_S), slams 4.5 u onto the slab 16 ticks after the cat
            // crosses x 9.6: on a cat that runs on. One that stops as it starts down is clear of it; it holds, goes back up, and
            // the way is open.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(12.55f,C2 + .5f),(1.5f,1f),(9.6f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Cap_S",slams:true),
                new SoloRoomTrapSettings(delayTicks:16,offset:new Vector2(0f,F2 - C2),moveTicks:7,holdTicks:25,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Hinge_1, the slab's top unit at x 15-16.5 (on Seat_H, hinged at its west end), stands up behind a cat walking east
            // past x 17.8 and slides 4 u east to Spikes_H's edge: a cat that stops short of them is shoved onto them (L016's
            // Hinge_1: a 3-tick swing, 4 u in 20 ticks).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_1",(15.75f,F2 - .5f),(1.5f,1f),(17.8f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(4f,0f),moveTicks:20,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            Spikes(elements, "Spikes_H", 20.5f, 21.7f, F2);
            // Block_1, a section of the roof's underside at x 26-27.5, drops 4.5 u onto the slab 2 ticks after the cat crosses
            // x 24.75: on a cat that runs on. One that stops lets it land in front, hops onto it and runs off the slab's end.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(26.75f,C2 + .5f),(1.5f,1f),(24.75f,(F2 + C2) * .5f),(.5f,C2 - F2),
                new SoloRoomTrapSettings(delayTicks:2,unitsPerTick:.25f,travelDistance:C2 - F2)));

            // ---------- The lower storey ----------
            // The storm cloud wakes as the cat lands from the drop and chases it west, a little slower than a run (D-125 clouds
            // run at 0.12 u a tick; this one at 0.1, so a cat that keeps running pulls ahead of its strikes, and one that stops is struck).
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(31f,2.05f),(2f,.8f),(31f,(F1 + C1) * .5f),(2f,C1 - F1),
                new SoloRoomTrapSettings(new StormCloudSettings(new[] { new StormFloor(-100f, 2.05f, 15.5f, 31f) },
                    followSpeed:.1f, firstStrikeDelay:70, strikePeriod:90, climbSpeed:.24f), new SoloRoomTrapSettings(delayTicks:0))));
            // Crumble fills its shaft and gives way 2 ticks after a touch: a cat that runs on falls onto PitC's spikes. Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumble",(25f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(delayTicks:2)));
            // Pop: spikes in the floor at x 18.5-20 spring up as the cat comes within 1.7 u of them (the trigger holds them too): jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop",(19.25f,F1 + .15f),(1.5f,.3f),(20.1f,(F1 + C1) * .5f),(3.2f,C1 - F1),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // The door rides its lift up and away over PitD as the cat comes within x 18: the lift (the pit's lid at x 12.7-14.5)
            // carries it 2.8 u west and 0.8 u up and stays. Leap after it from the pit's edge (2.3 u, up 0.8).
            // Both have the same trigger, so they set off on the same tick and the door rides the lift (a chain is a tick late).
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(DoorTrigger.x,DoorTrigger.y),(.5f,C1 - F1),settings:new SoloRoomTrapSettings(moveTicks:DoorRideTicks,offset:DoorRide)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_D",(13.6f,F1 - .25f),(1.8f,.5f),(DoorTrigger.x,DoorTrigger.y),(.5f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:DoorRide,moveTicks:DoorRideTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), new[] { Shaft("PitC", 24f, 26f), Shaft("PitD", 8.5f, 14.5f) },
                System.Array.Empty<RequiredJump>());
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A pit between two solids: its bottom from y -4 up to `floor` (-3 by default) and the spikes on it.
        static void Pit(List<SoloRoomElement> elements, string id, float minX, float maxX, bool opening = true, float floor = -3f)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.PitBottom,$"{id}_Bottom",(centre,(floor - 4f) * .5f),(width,floor + 4f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"{id}_Hazard",(centre,floor + .15f),(width,.3f),hazardRole:opening ? SoloRoomHazardRole.OpeningBottom : SoloRoomHazardRole.Normal));
        }

        static SoloRoomOpening Shaft(string id, float minX, float maxX) =>
            O(SoloRoomOpeningKind.Pit,minX,maxX,"","",$"{id}_Bottom",$"{id}_Hazard");

        // L002 keeps using these (its pits and shaft were built with them).
        internal static void AddShaft(List<SoloRoomElement> elements, int id, float minX, float maxX) => Pit(elements, $"Pit{id}", minX, maxX);

        internal static SoloRoomOpening Shaft(int id, float minX, float maxX) => Shaft($"Pit{id}", minX, maxX);
    }
}
