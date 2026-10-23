using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-168: level 24, "Ceiling Town" (D-163's fourth). The verb is LIVE UPSIDE DOWN: three corridors 4.5 u tall on 2 u slabs
    // (L016's), walked east, west, east on their ceilings over spiked streets; getting right side up is the trap, until the end.
    //  - Town (section 0, corridor 1, its ceiling walked east): the cat spawns in Flip_U0 and Inv_0, so it falls up onto the
    //    ceiling with left and right swapped. Spikes_R1 come and go on the ceiling; a cat that jumps them meets Flip_H1, hidden at
    //    the bottom of the jump, and drops onto Street_1a (wait for them to go and walk through). CC_1 gives way into a spiked
    //    recess (jump it); Lift_1 rises out of the street into the ceiling (stop, let it go up and down); over Kerb_C1 and on
    //    against Arrow_1's shots along the ceiling (jump each); up the shaft, where the controls come back.
    //  - Gallery (section 1, corridor 2, its ceiling walked west): Piston_2 rises into the ceiling on its beat (wait); Pop_C2
    //    springs out of the ceiling ahead (jump it); CC_2 gives way under a cat that stops on it; up the shaft.
    //  - Crown (section 2, corridor 3): on the ceiling, Push_3 comes down behind and shoves the cat at Spikes_C3 (jump them at
    //    once); Flip_D3 sets it right side up at last. On the floor Hinge_F, spiked, chases (jump Spikes_5); Slam_F comes down
    //    out of the roof ahead (stop short, hop onto it); the door.
    static class L024Layout
    {
        // The corridors' floors and ceilings (room-local y); every slab between them is 2 u of stone, the roof 2.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f, RoofTop = 19.5f, Base = -2f;
        // Spikes_R1: gone for RhythmDown ticks of every RhythmPeriod, from room tick RhythmFrom. Piston_2's beat. Arrow_1's shots.
        internal const int RhythmPeriod = 120, RhythmFrom = 80, RhythmDown = 50, PistonPeriod = 110, PistonPhase = 93, ArrowTell = 10, ArrowCool = 60;
        internal const float ArrowMouth = 31.5f, ArrowSpeed = .25f, ArrowLaneY = C1 - .25f;
        // The finale's chase: Hinge_F's stand, east.
        internal const float ChaseDistance = 4f;
        internal const int ChaseDelay = 10, ChaseTicks = 36;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.9f,F3 + .75f),(.6f,1.5f)),
            };

            // ---- The street (y -2..0): Lift_1 is its top unit on a seat; spiked from x 4 to the shaft ----
            Block(elements, SoloRoomElementKind.Floor, "Ground_A", 0f, 17.5f, Base, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 17.5f, 19f, Base, F1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 19f, 31.5f, Base, F1);
            Spikes(elements, "Street_1a", 4f, 17.5f, F1);
            Spikes(elements, "Street_1b", 19f, 31.5f, F1);
            // Jamb_A: the shaft's east wall (x 31.5-32), from the street to corridor 2's ceiling; Arrow_1 fires from it.
            Block(elements, SoloRoomElementKind.Wall, "Jamb_A", 31.5f, 32f, Base, C2);

            // ---- Slab A (y 4.5-6.5): corridor 1's ceiling (walked), corridor 2's street ----
            // CC_1 is its bottom unit under Cap_C1; SA_L over Lift_1 (its crush partner); Kerb_C1 hangs from SA_2; Piston_2 is its
            // top unit on Seat_P2; the shaft (x 29.5-31.5) runs up through it.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 12f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C1", 12f, 13.5f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 13.5f, 17.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_L", 17.5f, 19f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 19f, 22f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_P2", 22f, 23.5f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 23.5f, 29.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_C1", 19.6f, 20f, C1 - .6f, C1);
            Spikes(elements, "Street_2a", 3.5f, 22f, F2);
            Spikes(elements, "Street_2b", 23.5f, 29.5f, F2);

            // ---- Slab B (y 11-13): corridor 2's ceiling (walked), corridor 3's floor ----
            // The shaft (x 1.5-3.5) runs up through it; CC_2 is its bottom unit under Cap_C2; SB_P over Piston_2 (its crush
            // partner); Hinge_F is its top unit on Seat_F.
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 0f, 1.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 3.5f, 9f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C2", 9f, 10.5f, C2 + 1f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 10.5f, 14f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_F", 14f, 15.5f, C2, F3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", 15.5f, 22f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_P", 22f, 23.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_4", 23.5f, 32f, C2, F3);
            Spikes(elements, "Spikes_5", 18f, 19.2f, F3);

            // ---- The roof (y 17.5-19.5): Push_3 and Slam_F are its bottom unit under caps ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 3.5f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_P3", 3.5f, 5f, C3 + 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 5f, 23.5f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_F", 23.5f, 25f, C3 + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 25f, 32f, C3, RoofTop);
            Spikes(elements, "Spikes_C3", 8.2f, 9.4f, C3, down: true);

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);

            // ---------- Town (corridor 1) ----------
            // The cat spawns in Flip_U0 and rises through Inv_0: on the ceiling from the first tick, left and right swapped until it
            // rises through the shaft (Inv_0's end box), and again after every respawn at the start.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U0",(1.5f,F1 + 1f),(1f,2f),settings:up));
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_0",(1.5f,(F1 + 2f + C1) * .5f),(.6f,C1 - F1 - 2f),settings:new SoloRoomTrapSettings(
                new InverterSettings(450,endPosition:new Vector2(30.5f,(C1 + F2) * .5f),endSize:new Vector2(2f,F2 - C1)),new SoloRoomTrapSettings(delayTicks:0))));
            // Spikes_R1, honest, on the ceiling at x 5-6.5, gone RhythmDown ticks of every RhythmPeriod. The troll: Flip_H1, hidden
            // at the bottom of a jump over them (L016's Flip_H turned over), sets a cat that jumps them right side up, onto Street_1a.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_R1",(5.75f,C1 - .15f),(1.5f,.3f),settings:Rhythm(RhythmPeriod, RhythmFrom, RhythmFrom + RhythmDown)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_H1",(5.75f,C1 - 1.7f),(1.2f,.8f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown)));
            // CC_1, slab A's bottom unit at x 12-13.5, gives way 3 ticks after a cat on the ceiling touches it; the cat falls up into
            // the recess onto Spikes_CC1 (hidden on the cap's underside, shown with it). Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_1",(12.75f,C1 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC1",(12.75f,C1 + .85f),(1.5f,.3f),settings:Chain("CC_1", 6)));
            // Lift_1, the street's top unit at x 17.5-19, rises 4.5 u into SA_L 15 ticks after the cat on the ceiling comes into its
            // cut (x 15.9-16.3): stop short, let it go up and down, walk on.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(18.25f,F1 - .5f),(1.5f,1f),(16.1f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_L"),
                new SoloRoomTrapSettings(delayTicks:15,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Arrow_1, in Jamb_A, fires west along the ceiling at the hanging cat's height to Kerb_C1, from the moment the cat comes
            // over the kerb, every ArrowTell + ArrowCool ticks, non-stop: jump each shot (down and back).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1",(31.75f,ArrowLaneY),(.5f,.25f),(21f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,ArrowLaneY,20f,unitsPerTick:ArrowSpeed,tellTicks:ArrowTell),
                new SoloRoomTrapSettings(delayTicks:1,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:ArrowCool))));

            // ---------- Gallery (corridor 2) ----------
            // Piston_2, slab A's top unit at x 22-23.5, rises 4.5 u into SB_P every PistonPeriod ticks: wait for it to go back down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_2",(22.75f,F2 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SB_P"),
                new SoloRoomTrapSettings(offset:new Vector2(0f,C2 - F2),moveTicks:8,holdTicks:20,returnTicks:12,repeatMode:TrapRepeatMode.Periodic,periodTicks:PistonPeriod,phaseTicks:PistonPhase,cooldownTicks:40,movingKind:MovingTrapKind.Solid))));
            // Pop_C2: spikes in the ceiling at x 17-18.2 spring out 6 ticks after the cat comes within 1 u of them: jump them. Upside
            // down a jump on the reveal tick is caught, so the strip is 1.2 u (L002's Pop_C).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_C2",(17.6f,C2 - .15f),(1.2f,.3f),(19.5f,(F2 + C2) * .5f),(1f,C2 - F2),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // CC_2, slab B's bottom unit at x 9-10.5, gives way 24 ticks after a cat on the ceiling first touches it: a cat that walks
            // on is past it; one that stops on it falls up onto Spikes_CC2.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_2",(9.75f,C2 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:24)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC2",(9.75f,C2 + .85f),(1.5f,.3f),settings:Chain("CC_2", 6)));

            // ---------- Crown (corridor 3) ----------
            // Push_3, the roof's bottom unit at x 3.5-5, comes down 1 u 30 ticks after the cat walks under it from the shaft and
            // slides 3.2 u east: a cat that stops short of Spikes_C3 is shoved onto them (L016's).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Push_3",(4.25f,C3 + .5f),(1.5f,1f),(4.25f,(F3 + C3 + 1.6f) * .5f),(1.5f,C3 + 1.6f - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,riseFirst:true),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(3.2f,-1f),moveTicks:24,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D3",(12.5f,C3 - 1f),(1f,2f),settings:down));
            // Hinge_F, slab B's top unit at x 14-15.5 (hinged at its east end, spiked, so its spikes face the cat), stands up behind
            // a cat walking east past x 17 and slides ChaseDistance east to Spikes_5's far edge.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_F",(14.75f,F3 - .25f),(1.5f,.5f),(17f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:ChaseDelay,offset:new Vector2(ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Slam_F, the roof's bottom unit at x 23.5-25, slams 4.5 u onto the floor 9 ticks after a cat walking east comes into its
            // cut (x 22.4-22.8), and stays: stop short, hop onto it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F",(24.25f,C3 + .375f),(1.5f,.75f),(22.6f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            // Each later gate lies across its shaft, which the cat falls up through; Gallery and Crown start on their ceilings.
            var sections = new[] {
                CheckpointSection.Start("Town", new Vector2(1.5f, F1), new[] { "Flip_U0", "Inv_0", "Spikes_R1", "Flip_H1", "CC_1", "Spikes_CC1", "Lift_1", "Arrow_1" }),
                new CheckpointSection("Gallery", new Vector2(28.5f, C2), new Rect(29.5f, C1 + .9f, 2f, .2f),
                    new[] { "Piston_2", "Pop_C2", "CC_2", "Spikes_CC2" }, gravityUp: true),
                new CheckpointSection("Crown", new Vector2(2.5f, C3), new Rect(1.5f, C2 + .9f, 2f, .2f),
                    new[] { "Push_3", "Flip_D3", "Hinge_F", "Slam_F" }, gravityUp: true),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));

        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        // Periodic spikes that are down over [down, up) of every period and up for the rest (L016's Rhythm).
        static SoloRoomTrapSettings Rhythm(int period, int down, int up) =>
            new(revealDelayTicks: 6, repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: ((up % period) + period) % period, cooldownTicks: period - (up - down));
    }
}
