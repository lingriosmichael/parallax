using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-171: level 27, "Updraft" (D-163's seventh). The verb is RIDE THE AIR: every vine snaps soon after the cat takes it, and the
    // vents are what carry it from one to the next. Three storeys 5.5 u apart on 1 u slabs, walked east, west, east.
    //  - Lake (section 0, storey 1, walked east): V1 and V2 over the spike lake snap behind the cat; Pier_G stands in the lake
    //    with G_1 in it. G_1's eruption flings the cat east to V3, too far for a jump; a cat that rides it straight up meets
    //    Spikes_G. V3 snaps too: leap off it onto the ground. G_2 at the far end throws the cat up its hole to storey 2.
    //  - Gallery (section 1, storey 2, walked west): Slam_2 pounds on the beat (through in the gap). V4 is the only way over
    //    the spike bed, and it snaps. At the west end, V_S hangs in the shaft up to storey 3 and snaps too quickly to be climbed
    //    from a jump: G_3 throws the cat high up it (steer, or meet Spikes_C3), and it leaps from there onto storey 3.
    //  - Crown (section 2, storey 3, walked east): V5-V8 hang from the roof over the spike bed, each snapping after the cat
    //    takes it; GD_C, a vent in the roof between V6 and V7, blows down on the beat (wait on V6 for it to stop, then leap).
    //    Pop_3 springs ahead; G_D throws the cat up onto the door's ledge.
    static class L027Layout
    {
        // The storeys' walk lines (room-local y); each slab under the next storey is 1 u of stone, the roof 2 over 5 u of air.
        internal const float S1 = 0f, S2 = 5.5f, S3 = 11f, RoofU = 16f, RoofTop = 18f, Base = -4f, LakeBed = -3f, PierTop = -1f, LedgeTop = 13.5f;
        // The beat (room ticks) for the vents and Slam_2, and each one's place in it (an eruption's first tick; the slam's departure).
        internal const int Beat = 90, G1Erupt = 55, G2Erupt = 5, Slam2 = 55, G3Erupt = 70, GDErupt = 88, GDoorErupt = 0;
        // How long each vine holds after the cat first touches it.
        internal const int SnapLake = 45, SnapGallery = 45, SnapShaft = 28, SnapCrown = 45, SnapWait = 80;
        // The vines' x (centres).
        internal const float V1X = 5f, V2X = 8f, V3X = 19f, V4X = 12f, VSX = 1.6f, V5X = 6.6f, V6X = 9.6f, V7X = 12.6f, V8X = 15.6f;
        // The vents' x (centres).
        internal const float G1X = 12.5f, G2X = 27.2f, G3X = 4.6f, GDX = 11.1f, GDoorX = 26.8f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,S1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(31f,LedgeTop + .75f),(.6f,1.5f)),
            };

            // ---- Storey 1's floor (y -4..0) ----
            // Ground_0 (x 0-3.5); Pit_1 (x 3.5-11.6); Pier_G (x 11.6-13.4, top 1 u under the walk line), G_1 in it; Pit_2 (x 13.4-22);
            // Ground_1 (x 22-32), G_2 in it under slab A's hole.
            Block(elements, SoloRoomElementKind.Floor, "Ground_0", 0f, 3.5f, Base, S1);
            Pit(elements, "Pit_1", 3.5f, 11.6f);
            Block(elements, SoloRoomElementKind.Floor, "Pier_G", 11.6f, 13.4f, Base, PierTop);
            Pit(elements, "Pit_2", 13.4f, 22f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_1", 22f, 32f, Base, S1);

            // ---- Slab A (y 4.5-5.5): storey 1's ceiling, storey 2's floor ----
            // SA_0 (x 0-3.3) under Spikes_S, the shaft's foot; SA_1 (x 3.3-9), G_3 in it; Bed_2, the spike bed (x 9-15, half the slab);
            // SA_2 (x 15-26.45), the shrine; the hole over G_2 (x 26.45-27.95); SA_E.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 3.3f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 3.3f, 9f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "Bed_2", 9f, 15f, S2 - 1f, S2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 15f, 26.45f, S2 - 1f, S2);
            Block(elements, SoloRoomElementKind.Floor, "SA_E", 27.95f, 32f, S2 - 1f, S2);

            // ---- Slab B (y 10-11): storey 2's ceiling, storey 3's floor ----
            // The shaft (x 0-3) V_S climbs; SC_0 (x 3-5); Bed_3, the spike bed (x 5-19, half the slab); SB_1; Cap_2 over Slam_2
            // (x 20-21.5); SB_2 (x 21.5-28.4), G_D in it; Ledge_D (x 28.4-32, up to 13.5), the door's.
            Block(elements, SoloRoomElementKind.Floor, "SC_0", 3f, 5f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Bed_3", 5f, 19f, S3 - 1f, S3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 19f, 20f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_2", 20f, 21.5f, S3 - .5f, S3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 21.5f, 28.4f, S3 - 1f, S3);
            Block(elements, SoloRoomElementKind.Floor, "Ledge_D", 28.4f, 32f, S3 - 1f, LedgeTop);

            // ---- The roof (y 16-18) ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, 32f, RoofU, RoofTop);

            // Honest spikes: on the beds, on the shaft's foot, and under the slabs over G_1 and G_3 (a cat that rides a vent straight
            // up meets them).
            Spikes(elements, "Spikes_2", 9f, 15f, S2 - .5f);
            Spikes(elements, "Spikes_S", 0f, 3.3f, S2);
            Spikes(elements, "Spikes_3", 5f, 19f, S3 - .5f);
            Spikes(elements, "Spikes_G", G1X - 1f, G1X + 1f, S2 - 1f, down: true);
            Spikes(elements, "Spikes_C3", G3X - .5f, G3X + .5f, S3 - 1f, down: true);

            // ---------- Lake (storey 1) ----------
            // V1 and V2 hang from slab A over Pit_1, each snapping SnapLake ticks after the cat first touches it.
            Vine(elements, "V1", V1X, -1f, S2 - 1.05f, SnapLake);
            Vine(elements, "V2", V2X, -1f, S2 - 1.05f, SnapLake);
            // G_1, flush in Pier_G's top: its eruption flings a cat that steers east all the way to V3 (a jump from the pier falls short).
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(G1X,PierTop - .15f),(1f,.3f),settings:Geyser(Beat, G1Erupt, 1.5f)));
            Vine(elements, "V3", V3X, -1f, S2 - 1.05f, SnapLake);
            // G_2, flush in the ground under slab A's hole, throws the cat up onto storey 2 (a 3 u column).
            elements.Add(E(SoloRoomElementKind.Geyser,"G_2",(G2X,S1 - .15f),(1f,.3f),settings:Geyser(Beat, G2Erupt, 3f)));

            // ---------- Gallery (storey 2) ----------
            // Slam_2, slab B's bottom unit at x 20-21.5, pounds the floor on the beat.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(20.75f,S3 - .75f),(1.5f,.5f),settings:Slam("Cap_2", S2 - S3 + 1f, Slam2)));
            // V4 hangs from slab B over the middle of Bed_2.
            Vine(elements, "V4", V4X, S2 + .5f, S3 - 1.05f, SnapGallery);
            // G_3, flush in SA_1, throws a cat that steers west high up V_S; V_S, from just over Spikes_S up through the shaft, snaps
            // SnapShaft ticks after a touch: too soon to climb from a jump.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_3",(G3X,S2 - .15f),(1f,.3f),settings:Geyser(Beat, G3Erupt, 1.5f)));
            Vine(elements, "V_S", VSX, S2 + 1.9f, S3 + .6f, SnapShaft);

            // ---------- Crown (storey 3) ----------
            // V5-V8 hang from the roof over Bed_3; V6 holds longer (the cat waits on it for GD_C).
            Vine(elements, "V5", V5X, S3 + .8f, RoofU - .05f, SnapCrown);
            Vine(elements, "V6", V6X, S3 + .8f, RoofU - .05f, SnapWait);
            Vine(elements, "V7", V7X, S3 + .8f, RoofU - .05f, SnapCrown);
            Vine(elements, "V8", V8X, S3 + .8f, RoofU - .05f, SnapCrown);
            // GD_C, flush in the roof between V6 and V7, blows down on the beat (a 3 u column, down through every leap between them).
            elements.Add(E(SoloRoomElementKind.Geyser,"GD_C",(GDX,RoofU + .15f),(1f,.3f),settings:Geyser(Beat, GDErupt, 3f, GeyserDirection.Down)));
            // Pop_3: spikes in the floor at x 23.2-24.4 spring up as the cat comes within 1.6 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_3",(23.8f,S3 + .15f),(1.2f,.3f),(23f,(S3 + RoofU) * .5f),(2.8f,RoofU - S3),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // G_D, flush in SB_2 short of Ledge_D, throws a cat that steers east up onto the ledge (a 1 u column, so it stays under the roof).
            elements.Add(E(SoloRoomElementKind.Geyser,"G_D",(GDoorX,S3 - .15f),(1f,.3f),settings:Geyser(Beat, GDoorErupt, 1f)));

            var sections = new[] {
                CheckpointSection.Start("Lake", new Vector2(1.5f, S1), new[] { "V1", "V2", "G_1", "V3", "G_2" }),
                new CheckpointSection("Gallery", new Vector2(24.5f, S2), new Rect(26.45f, S2 - .6f, 1.5f, .2f),
                    new[] { "Slam_2", "V4", "G_3", "V_S" }),
                new CheckpointSection("Crown", new Vector2(4f, S3), new Rect(0f, S3 - .6f, 3f, .2f),
                    new[] { "V5", "V6", "GD_C", "V7", "V8", "Pop_3", "G_D" }),
            };
            var openings = new[] {
                Shaft("Pit_1", 3.5f, 11.6f, "Ground_0", "Pier_G"),
                Shaft("Pit_2", 13.4f, 22f, "Pier_G", "Ground_1"),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));

        // A snap vine at x from y0 to y1, snapping `snap` ticks after the cat first touches it.
        static void Vine(List<SoloRoomElement> e, string name, float x, float y0, float y1, int snap) =>
            e.Add(E(SoloRoomElementKind.Vine, name, (x, (y0 + y1) * .5f), (.6f, y1 - y0), settings: new SoloRoomTrapSettings(delayTicks: snap)));

        // A pit in storey 1's floor: its bottom from Base up to the lake bed, and the spikes on it.
        static void Pit(List<SoloRoomElement> e, string id, float x0, float x1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, id + "_Bottom", ((x0 + x1) * .5f, (Base + LakeBed) * .5f), (x1 - x0, LakeBed - Base)));
            e.Add(E(SoloRoomElementKind.Hazard, id, ((x0 + x1) * .5f, LakeBed + .15f), (x1 - x0, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
        }

        static SoloRoomOpening Shaft(string id, float x0, float x1, string left, string right) =>
            O(SoloRoomOpeningKind.Pit, x0, x1, left, right, id + "_Bottom", id);

        // A slam pounding `dy` down on the beat (over 8 ticks, 10 there) and back up into `cap` (its crush partner, over 15), leaving
        // home at room tick `phase`.
        static SoloRoomTrapSettings Slam(string cap, float dy, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: cap, slams: true),
                new SoloRoomTrapSettings(offset: new Vector2(0f, dy), moveTicks: 8, holdTicks: 10, returnTicks: 15, repeatMode: TrapRepeatMode.Periodic,
                    periodTicks: Beat, phaseTicks: phase, cooldownTicks: 33, movingKind: MovingTrapKind.Solid));

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, float column, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction, columnHeight: column), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
