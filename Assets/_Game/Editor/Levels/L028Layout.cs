using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-173: level 28, "Mirror Lanes" (D-163's eighth; Golden Ruins, D-172). The verb is THE OTHER SIDE: each corridor is its own
    // reflection turned upside down. The cat walks the first half on one face, a flip orb in the middle throws it onto the other
    // face, and the second half holds the same traps again, mirrored onto that face and met in the reverse order. L016's frame:
    // three corridors 4.5 u high on 2 u slabs, walked east, west, east. Honest spikes cover the face the cat isn't meant to use.
    //  - Mirror (section 0, corridor 1, floor first): Pop_F1 springs out of the floor (jump it), Slam_F1 comes down ahead and stays
    //    (stop short, hop it), Collapse_F1 gives way over Pit_C1 (jump it); Flip_U1 throws the cat onto the ceiling, where the
    //    same three come back mirrored: CC_C1 gives way into a spiked recess (jump it), Piston_C1 rises out of the floor into the
    //    ceiling and stays (stop short, hop it), Pop_C1 springs out of the ceiling (jump it), and the hole in slab A lets the cat
    //    fall up into corridor 2.
    //  - Arrows (section 1, corridor 2, ceiling first): Arrow_M2 shoots along the lane the cat is on, floor or ceiling (it mirrors
    //    with the cat's gravity): jump each shot. Over Kerb_C2, over Pop_C2, into Flip_D2, over its mirror Pop_F2 on the floor,
    //    and Flip_U3 throws the cat up the shaft into corridor 3.
    //  - Crown (section 2, corridor 3, ceiling first): Pop_C3, Slam_C3 rising out of the floor, CC_C3; Flip_D3 drops the cat to
    //    the floor, where Collapse_F3, Slam_F3 and Pop_F3 mirror them on the way to the door.
    static class L028Layout
    {
        // The corridors' floors and ceilings (room-local y); every slab between them is 2 u of stone (L016's frame).
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f, Base = -2f, RoofTop = 19.5f;
        // Arrow_M2's floor lane (shin height in corridor 2), the line it mirrors across (corridor 2's middle) and its end (the kerbs).
        internal const float ArrowLaneY = F2 + .3f, MirrorAxis = (F2 + C2) * .5f, LaneEnd = 29f, JambFace = 1.5f;
        internal const int ArrowTell = 10, ArrowDelay = 37, ArrowCooldown = 209;
        const float Mid1 = (F1 + C1) * .5f, Mid2 = (F2 + C2) * .5f, Mid3 = (F3 + C3) * .5f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.5f,F3 + .75f),(.6f,1.5f)),
            };

            // ---- The ground (y -2..0) ----
            // Collapse_F1 over Pit_C1 (x 11.4-12.9); Piston_C1 is the top unit at x 24.8-26.3, on its seat.
            Block(elements, SoloRoomElementKind.Floor, "G1_A", 0f, 11.4f, Base, F1);
            Pit(elements, "Pit_C1", 11.4f, 12.9f, Base, Base + 1f);
            Block(elements, SoloRoomElementKind.Floor, "G1_B", 12.9f, 24.8f, Base, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_P1", 24.8f, 26.3f, Base, Base + 1f);
            Block(elements, SoloRoomElementKind.Floor, "G1_C", 26.3f, 32f, Base, F1);

            // ---- Slab A (y 4.5-6.5): corridor 1's ceiling, corridor 2's floor ----
            // Slam_F1 is its bottom unit at x 8.5-10 under Cap_S1; CC_C1 its
            // bottom unit at x 20.4-21.9 under Cap_CC1; the hole (x 31-32) lets the cat fall up into corridor 2.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 8.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 8.5f, 10f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 10f, 20.4f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_CC1", 20.4f, 21.9f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 21.9f, 31f, C1, F2);

            // ---- Slab B (y 11-13): corridor 2's ceiling, corridor 3's floor ----
            // The shaft (x 1.5-3.5) up to corridor 3; Slam_C3 is its top unit at x 9.2-10.7 on Seat_R3; Collapse_F3 its top unit
            // over Pit_F3 (x 17.5-19).
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 0f, 1.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 3.5f, 9.2f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_R3", 9.2f, 10.7f, C2, C2 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_1a", 10.7f, 17.5f, C2, F3);
            Pit(elements, "Pit_F3", 17.5f, 19f, C2, C2 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 19f, 32f, C2, F3);
            // Corridor 2's west end: the jamb (up to the shaft's west side) that hosts Arrow_M2, and the kerbs where its lanes end.
            Block(elements, SoloRoomElementKind.Wall, "Jamb", 0f, JambFace, F2, C2);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_F2", LaneEnd, LaneEnd + .5f, F2, F2 + .6f);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_C2", LaneEnd, LaneEnd + .5f, C2 - .6f, C2);

            // ---- The roof (y 17.5-19.5) ----
            // CC_C3 is its bottom unit at x 12.4-13.9 under Cap_CC3; Slam_F3 its bottom unit at x 22.5-24 under Cap_S3.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 12.4f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_CC3", 12.4f, 13.9f, C3 + 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 13.9f, 22.5f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3", 22.5f, 24f, C3 + 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 24f, 32f, C3, RoofTop);

            // Honest spikes: on each corridor, the face the cat isn't meant to walk (the ceiling over the first half, the floor under
            // the second), each strip the other's reflection.
            Spikes(elements, "Spikes_C1a", 2.4f, 8.5f, C1, down: true);
            Spikes(elements, "Spikes_C1b", 10f, 13.5f, C1, down: true);
            Spikes(elements, "Spikes_F1a", 16.6f, 24.8f, F1);
            Spikes(elements, "Spikes_F1b", 26.3f, 32f, F1);
            Spikes(elements, "Spikes_F2", 16.6f, LaneEnd, F2);
            Spikes(elements, "Spikes_C2", 3.5f, 14.3f, C2, down: true);
            Spikes(elements, "Spikes_F3a", 3.5f, 9.2f, F3);
            Spikes(elements, "Spikes_F3b", 10.7f, 15f, F3);
            Spikes(elements, "Spikes_C3a", 17.5f, 22.5f, C3, down: true);
            Spikes(elements, "Spikes_C3b", 24f, 32f, C3, down: true);

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);

            // ---------- Mirror (corridor 1) ----------
            // The floor half, walked east.
            Pop(elements, "Pop_F1", 4.4f, F1, Mid1, false);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F1",(9.25f,C1 + .5f),(1.5f,1f),(7.9f,Mid1),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F1 - C1),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_F1",(12.15f,F1 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U1",(15.4f,F1 + 1f),(1f,2f),settings:up));
            // The ceiling half, walked east: the same three, mirrored onto the ceiling and met the other way round.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_C1",(21.15f,C1 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC1",(21.15f,C1 + .85f),(1.5f,.3f),settings:Chain("CC_C1", 6)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_C1",(25.55f,F1 - .5f),(1.5f,1f),(24.1f,Mid1),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_2"),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,C1 - F1),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            Pop(elements, "Pop_C1", 29.4f, C1, Mid1, true);

            // ---------- Arrows (corridor 2) ----------
            // Arrow_M2, in the jamb, fires east along corridor 2 from the moment the cat comes to the hole (its trigger runs from x 28
            // to the east wall, down to corridor 1's floor) and every 220 ticks after (a 10-tick tell), non-stop: one shot meets the
            // cat on the ceiling, the next on the floor. It mirrors with the cat's gravity: with the cat on the ceiling, the shot flies along the ceiling.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_M2",(JambFace - .25f,ArrowLaneY),(.5f,.25f),(30f,(F1 + C2) * .5f),(4f,C2 - F1),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,ArrowLaneY,LaneEnd,tellTicks:ArrowTell,mirrorWithGravity:true,mirrorAxisY:MirrorAxis),
                new SoloRoomTrapSettings(delayTicks:ArrowDelay,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:ArrowCooldown))));
            // The ceiling half, walked west: Pop_C2 springs out of the ceiling (jump it), and Flip_D2 drops the cat to the floor.
            // Its cut runs up to corridor 3's ceiling: Collapse_F3's pit opens corridor 2 into corridor 3 over it.
            Pop(elements, "Pop_C2", 17f, C2, (F2 + C3) * .5f, true, west: true, height: C3 - F2);
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D2",(15.1f,C2 - 1f),(1f,2f),settings:down));
            // The floor half, walked west: Pop_C2's mirror, Pop_F2, then the orb up the shaft.
            Pop(elements, "Pop_F2", 10.4f, F2, Mid2, false, west: true);
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U3",(2.2f,F2 + 1f),(1f,2f),settings:up));

            // ---------- Crown (corridor 3) ----------
            // The ceiling half, walked east.
            Pop(elements, "Pop_C3", 5.2f, C3, Mid3, true);
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_C3",(9.95f,F3 - .5f),(1.5f,1f),(8.5f,Mid3),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Roof_A"),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,C3 - F3),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_C3",(13.15f,C3 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC3",(13.15f,C3 + .85f),(1.5f,.3f),settings:Chain("CC_C3", 6)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D3",(16.5f,C3 - 1f),(1f,2f),settings:down));
            // The floor half, walked east to the door.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_F3",(18.25f,F3 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F3",(23.25f,C3 + .5f),(1.5f,1f),(22f,Mid3),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            Pop(elements, "Pop_F3", 27.2f, F3, Mid3, false);

            var sections = new[] {
                CheckpointSection.Start("Mirror", new Vector2(1.5f, F1), new[] { "Pop_F1", "Slam_F1", "Collapse_F1", "Flip_U1", "CC_C1", "Spikes_CC1", "Piston_C1", "Pop_C1" }),
                new CheckpointSection("Arrows", new Vector2(31.5f, C2), new Rect(31f, C1 + .9f, 1f, .2f),
                    new[] { "Arrow_M2", "Pop_C2", "Flip_D2", "Pop_F2", "Flip_U3" }, gravityUp: true),
                new CheckpointSection("Crown", new Vector2(2.5f, C3), new Rect(1.5f, C2 + .9f, 2f, .2f),
                    new[] { "Pop_C3", "Slam_C3", "CC_C3", "Spikes_CC3", "Flip_D3", "Collapse_F3", "Slam_F3", "Pop_F3" }, gravityUp: true),
            };
            var openings = new[] {
                O(SoloRoomOpeningKind.Pit, 11.4f, 12.9f, "G1_A", "G1_B", "Pit_C1_Bottom", "Pit_C1"),
                O(SoloRoomOpeningKind.Pit, 17.5f, 19f, "SB_1a", "SB_1b", "Pit_F3_Bottom", "Pit_F3"),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));

        // A pit under a collapsing floor: its bottom (y0..y1) and the spikes on it, inside the collapsing floor's box (D-085).
        static void Pit(List<SoloRoomElement> e, string id, float x0, float x1, float y0, float y1)
        {
            e.Add(E(SoloRoomElementKind.PitBottom, id + "_Bottom", ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
            e.Add(E(SoloRoomElementKind.Hazard, id, ((x0 + x1) * .5f, y1 + .15f), (x1 - x0, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom));
        }

        // Pop-up spikes 1.2 u wide from x0, in a floor's top (or, onCeiling, a ceiling's underside at `face`), springing 6 ticks
        // after a cat walking east (or west) comes within 1.6 u of them (their cut: the corridor's height, from 1.6 u short of them).
        static void Pop(List<SoloRoomElement> e, string name, float x0, float face, float mid, bool onCeiling, bool west = false, float height = 4.5f) =>
            e.Add(E(SoloRoomElementKind.HiddenSpikes, name, (x0 + .6f, onCeiling ? face - .15f : face + .15f), (1.2f, .3f), (west ? x0 + 1.4f : x0 - .2f, mid), (2.8f, height),
                new SoloRoomTrapSettings(revealDelayTicks: 6)));

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);
    }
}
