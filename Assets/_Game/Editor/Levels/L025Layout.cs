using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-169: level 25, "Rhythm" (D-163's fifth). The verb is MOVE ON THE BEAT: every piston, slam, launcher and lift keeps one
    // beat (Beat ticks, or one and a half), so the room pulses and the cat goes in the gaps. Three storeys 4.5 u apart on 1 u
    // slabs (L020's), walked east, west, east.
    //  - Pistons (section 0, storey 1, walked east): Piston_A, B and C rise into the slab one after another, a third of a beat
    //    apart, a wave: set off as A comes down and run behind it. G_1, on the Plinth, launches the cat up the hole on the beat.
    //  - Slams (section 1, storey 2, walked west): Slam_2a and Slam_2b pound the floor half a beat apart: go through in the gaps.
    //    Lift_2 carries the cat up on its beat and a half.
    //  - Drums (section 2, storey 3, walked east): G_3 blows down out of the roof over Spikes_3 on the beat (wait it out, then jump
    //    them); Arrow_3 fires along the floor from behind, a beat apart, without stopping (jump each shot); over Kerb_3, and
    //    Slam_D pounds in front of the door: through on the off-beat.
    static class L025Layout
    {
        // The storeys' walk lines (room-local y); each slab under the next storey is 1 u of stone, the roof 2 over 4 u of air.
        internal const float S1 = 0f, S2 = 4.5f, S3 = 9f, RoofU = 13f, RoofTop = 15f, Base = -2f;
        // The beat, and each element's place in it (room ticks).
        internal const int Beat = 90, PistonA = 0, PistonB = 33, PistonC = 66, G1Erupt = 70, Slam2a = 0, Slam2b = 45;
        internal const int LiftPeriod = 135, LiftPhase = 72, LiftMove = 30, LiftHold = 15, G3Erupt = 43, SlamD = 66;
        // Arrow_3: its tell and speed; it fires every Beat ticks once the cat comes onto storey 3.
        internal const int ArrowTell = 10;
        internal const float ArrowSpeed = .5f, ArrowMouth = 1f, ArrowLaneY = S3 + .3f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.9f,S3 + .75f),(.6f,1.5f)),
            };

            // ---- Storey 1's floor (y -2..0): the pistons are its top unit, each on a seat; the Plinth (x 17.5-19.5, top 0.5) ----
            Block(elements, SoloRoomElementKind.Floor, "Ground_0", 0f, 5f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_A", 5f, 6.5f, Base, S1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", 6.5f, 9f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_B", 9f, 10.5f, Base, S1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_2", 10.5f, 13f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_C", 13f, 14.5f, Base, S1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_3", 14.5f, 17.5f, Base, S1);
            Block(elements, SoloRoomElementKind.Floor, "Plinth", 17.5f, 19.5f, Base, S1 + .5f);
            // The east block (x 19.5-32): solid stone from storey 1's floor up to storey 2's ceiling, where the cat never goes.
            Block(elements, SoloRoomElementKind.Wall, "Block_E", 19.5f, 32f, Base, S3 - 1f);

            // ---- Slab A (y 3.5-4.5): storey 1's ceiling, storey 2's floor ----
            // Lift_2's seat (x 1-2.5); each piston's crush partner over it; the hole over the Plinth (x 17.5-19.5).
            Block(elements, SoloRoomElementKind.Floor, "SA_W", 0f, 1f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L2", 1f, 2.5f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 2.5f, 5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_A", 5f, 6.5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 6.5f, 9f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_B", 9f, 10.5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 10.5f, 13f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_C", 13f, 14.5f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 14.5f, 17.5f, S2 - 1f, S2);

            // ---- Slab B (y 8-9): storey 2's ceiling, storey 3's floor ----
            // Lift_2's shaft (x 1-2.5); the slams are its bottom unit under caps.
            Block(elements, SoloRoomElementKind.Floor, "SB_W", 0f, 1f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 2.5f, 7f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2b", 7f, 8.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 8.5f, 13f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2a", 13f, 14.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 14.5f, 32f, S3 - 1f, S3);
            // Storey 3's west jamb (x 0-1), Arrow_3's host; Kerb_3, where its lane ends.
            Block(elements, SoloRoomElementKind.Wall, "Jamb_W", 0f, 1f, S3, RoofU);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_3", 22.5f, 23f, S3, S3 + .6f);
            Spikes(elements, "Spikes_3", 6f, 7.2f, S3);

            // ---- The roof (y 13-15): Slam_D is its bottom unit under Cap_D ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 27.5f, RoofU, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_D", 27.5f, 29f, RoofU + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 29f, 32f, RoofU, RoofTop);

            // ---------- Pistons (storey 1) ----------
            // Each rises 3.5 u into the slab over it (its crush partner) on its third of the beat: up over 8 ticks, 20 there, down
            // over 12.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_A",(5.75f,S1 - .5f),(1.5f,1f),settings:Piston("SA_A", PistonA)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_B",(9.75f,S1 - .5f),(1.5f,1f),settings:Piston("SA_B", PistonB)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_C",(13.75f,S1 - .5f),(1.5f,1f),settings:Piston("SA_C", PistonC)));
            // G_1, in the Plinth, launches the cat up the hole onto storey 2 on the beat.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(18.5f,S1 + .35f),(1f,.3f),settings:Geyser(Beat, G1Erupt)));

            // ---------- Slams (storey 2) ----------
            // Each comes down 3.5 u onto the floor (over 8 ticks, 10 there, back over 15) on its half of the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2a",(13.75f,S3 - .75f),(1.5f,.5f),settings:Slam("Cap_2a", S2 - S3 + 1f, Slam2a)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2b",(7.75f,S3 - .75f),(1.5f,.5f),settings:Slam("Cap_2b", S2 - S3 + 1f, Slam2b)));
            // Lift_2, on its seat by the west wall (x 1-2.5), up 4.5 u to storey 3's floor and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_2",(1.75f,S2 - .25f),(1.5f,.5f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,S3 - S2),moveTicks:LiftMove,holdTicks:LiftHold,returnTicks:LiftMove,repeatMode:TrapRepeatMode.Periodic,
                    periodTicks:LiftPeriod,phaseTicks:LiftPhase,cooldownTicks:2 * LiftMove + LiftHold,movingKind:MovingTrapKind.Solid))));

            // ---------- Drums (storey 3) ----------
            // Arrow_3, in Jamb_W, fires east along the floor at shin height to Kerb_3 from the moment the cat walks past x 11, every
            // Beat ticks, non-stop: jump each shot as it comes up behind.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_3",(.75f,ArrowLaneY),(.5f,.25f),(11f,(S3 + RoofU) * .5f),(.4f,RoofU - S3),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,ArrowLaneY,22.5f,unitsPerTick:ArrowSpeed,tellTicks:ArrowTell),
                new SoloRoomTrapSettings(delayTicks:1,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:Beat - 1))));
            // G_3, a vent in the roof at x 6.1-7.1, blows down 2 u over Spikes_3 on the beat: a cat that jumps them while it blows is
            // pushed onto them.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_3",(6.6f,RoofU + .15f),(1f,.3f),settings:Geyser(Beat, G3Erupt, GeyserDirection.Down, 2f)));
            // Slam_D, the roof's bottom unit at x 27.5-29, pounds the floor in front of the door on its half of the beat (down 4 u over
            // 8 ticks, 10 there, back over 15): through on the off-beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_D",(28.25f,RoofU + .375f),(1.5f,.75f),settings:Slam("Cap_D", S3 - RoofU, SlamD)));

            var sections = new[] {
                CheckpointSection.Start("Pistons", new Vector2(1.5f, S1), new[] { "Piston_A", "Piston_B", "Piston_C", "G_1" }),
                new CheckpointSection("Slams", new Vector2(16f, S2), new Rect(17.5f, S2 - .6f, 2f, .2f),
                    new[] { "Slam_2a", "Slam_2b", "Lift_2" }),
                new CheckpointSection("Drums", new Vector2(3.5f, S3), new Rect(1f, S3 - .6f, 1.5f, .2f),
                    new[] { "Arrow_3", "G_3", "Slam_D" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A piston rising 3.5 u into `partner` on the beat, leaving home at room tick `phase`.
        static SoloRoomTrapSettings Piston(string partner, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: partner),
                new SoloRoomTrapSettings(offset: new Vector2(0f, S2 - 1f - S1), moveTicks: 8, holdTicks: 20, returnTicks: 12, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Beat, phaseTicks: phase, cooldownTicks: 40, movingKind: MovingTrapKind.Solid));

        // A slam pounding `dy` down on the beat (over 8 ticks, 10 there) and back up into `cap` (its crush partner, over 15), leaving
        // home at room tick `phase`.
        static SoloRoomTrapSettings Slam(string cap, float dy, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: cap, slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, dy), moveTicks: 8, holdTicks: 10, returnTicks: 15, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Beat, phaseTicks: phase, cooldownTicks: 33, movingKind: MovingTrapKind.Solid));

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up, float column = GeyserMath.DefaultColumnHeight) =>
            new(new GeyserSettings(direction, columnHeight: column), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
