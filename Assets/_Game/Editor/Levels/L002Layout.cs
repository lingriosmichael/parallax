using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-160 (the developer, 2026-10-20: "I have one opportunity to catch a users attention ... it has to be fun ... I dont care
    // about progression in difficulty anymore"; they picked "Backwards"): level 2 is two corridors 4.5 u tall on stone slabs (L016's
    // "Flipside" style: the floor and the ceiling are both in view), and from the first second it is played with left and right
    // swapped.
    //  - The lower corridor, west: the cat runs into Invert a stride from the start, and from then on left is right. Hinge_1
    //    stands up behind it and shoves it west toward Spikes_1 (press the other way and jump them); Flip_U1 throws it onto the
    //    ceiling; on the ceiling CC_1 gives way into a spiked recess (jump it), Lift_1 rises out of the spiked floor into the
    //    ceiling where the jump lands (stop, let it go up and down), and Pop_C springs out of the ceiling ahead (jump it); the
    //    jump carries the cat over the shaft, and it falls up into the upper corridor.
    //  - The upper corridor, east, on its ceiling: Push_2 comes down behind and shoves the cat toward Spikes_C2 (jump them);
    //    Flip_D2 drops it to the floor.
    //  - The finale: Hinge_W, spiked, stands up behind the cat and chases it east to the door, a little slower than a run;
    //    Pop_2 springs up ahead (jump it) and Crumble gives way into a spiked recess (jump it). Stop, and the wall has you.
    static class L002Layout
    {
        // The corridors' floors and ceilings (room-local y); the slab between them and the roof are 2 u of stone.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, RoofTop = 13f;
        const float Ground = -2f;
        // The inverter lasts longer than any attempt; a death resets it, and the cat runs into it again (L004's).
        internal const int InvertTicks = L004Layout.WholeLevelTicks;
        // Hinge_W's chase: from its stand to 16 u east, a little slower than a run (0.12 u a tick).
        internal const float ChaseDistance = 16f;
        internal const int ChaseTicks = 140;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Ground + RoofTop) * .5f),(1f,RoofTop - Ground)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Ground + RoofTop) * .5f),(1f,RoofTop - Ground)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(30.6f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.9f,F2 + .75f),(.6f,1.5f)),
            };
            // The ground (y -2..0): Lift_1 and Hinge_1 are its top unit, each on a seat.
            Block(elements, SoloRoomElementKind.Floor, "Ground_A", 0f, 8.7f, Ground, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 8.7f, 10.2f, Ground, F1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 10.2f, 27.6f, Ground, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H1", 27.6f, 29.1f, Ground, F1 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_C", 29.1f, 32f, Ground, F1);
            // Slab A (the lower corridor's ceiling, the upper one's floor): the shaft at x 1-3.5; CC_1 is its bottom unit under a
            // cap, Hinge_W and Crumble its top units, on a seat and over a recess.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 1f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 3.5f, 11.2f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_HW", 11.2f, 12.7f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C1", 12.7f, 14.2f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 14.2f, 24.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_Cr", 24.5f, 26f, C1, F2 - 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 26f, 32f, C1, F2);
            // The upper corridor's west end, beside the shaft, is stone (the cat crosses it on the ceiling only).
            Block(elements, SoloRoomElementKind.Wall, "Jamb", 0f, 1f, F2, C2);
            // The roof: Push_2 is its bottom unit under a cap.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 3.5f, C2, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_P2", 3.5f, 5f, C2 + 1f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 5f, 32f, C2, RoofTop);

            // Honest spikes: Spikes_1 on the lower corridor's floor; the floor under the ceiling walk (either side of Lift_1);
            // Spikes_C2 on the roof's underside.
            Spikes(elements, "Spikes_1", 22.7f, 23.9f, F1);
            Spikes(elements, "Spikes_F1", 10.2f, 18.5f, F1);
            Spikes(elements, "Spikes_F2", 0f, 8.7f, F1);
            Spikes(elements, "Spikes_C2", 8.2f, 9.4f, C2, down: true);

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);

            // ---------- The lower corridor ----------
            // Invert fills the corridor a stride west of the start: from the cat's touch to the end of the attempt, left is right.
            // Hinge_1, the ground's top unit at x 27.6-29.1 (hinged at its east end), has the same trigger: 6 ticks later it stands
            // up behind the cat and slides 4 u west, faster than a run, to Spikes_1's edge. A cat that stops, or that keeps pressing
            // the way it was going (and so turns back), is shoved onto them; one that presses the other way jumps them.
            elements.Add(E(SoloRoomElementKind.Inverter,"Invert",(25.9f,(F1 + C1) * .5f),(.6f,C1 - F1),settings:new SoloRoomTrapSettings(new InverterSettings(InvertTicks),new SoloRoomTrapSettings(delayTicks:0))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_1",(28.35f,F1 - .5f),(1.5f,1f),(25.9f,(F1 + C1) * .5f),(.6f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(-4f,0f),moveTicks:20,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U1",(19f,F1 + 1f),(1f,2f),settings:up));
            // CC_1, slab A's bottom unit at x 12.7-14.2, gives way 3 ticks after a cat on the ceiling touches it; the cat falls up
            // into the recess onto Spikes_CC1 (hidden on the cap's underside, shown with it). Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_1",(13.45f,C1 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC1",(13.45f,C1 + .85f),(1.5f,.3f),settings:Chain("CC_1", 6)));
            // Lift_1, the ground's top unit at x 8.7-10.2, rises 4.5 u into slab A (its crush partner) 15 ticks after the cat on the
            // ceiling crosses x 11.6: it meets a cat that walks on from where the jump over CC_1 lands. Stop, let it go up and down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(9.45f,F1 - .5f),(1.5f,1f),(11.6f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_1"),
                new SoloRoomTrapSettings(delayTicks:15,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            // Pop_C: spikes in the ceiling at x 5.8-7 spring out 6 ticks after the cat comes within 1.7 u of them: jump them, and the
            // jump carries the cat on toward the shaft (x 1-3.5), up which it falls into the upper corridor. Upside down a jump on the
            // reveal tick is caught, so the strip is 1.2 u (not the floor pops' 1.5) to keep the jump's window at 12.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_C",(6.4f,C1 - .15f),(1.2f,.3f),(7.1f,(F1 + C1) * .5f),(3.2f,C1 - F1),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // ---------- The upper corridor, on its ceiling ----------
            // Push_2, the roof's bottom unit at x 3.5-5, comes down 1 u 30 ticks after the cat walks under it from the shaft (its
            // trigger is its own column, cap included), behind a cat that walks on, then slides 3.2 u east: a cat that stops short
            // of Spikes_C2 is shoved onto them (L016's Push_3).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Push_2",(4.25f,C2 + .5f),(1.5f,1f),(4.25f,(F2 + C2 + 1.6f) * .5f),(1.5f,C2 + 1.6f - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,riseFirst:true),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(3.2f,-1f),moveTicks:24,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D2",(12.5f,C2 - 1f),(1f,2f),settings:down));

            // ---------- The finale ----------
            // Hinge_W, slab A's top unit at x 11.2-12.7 (hinged at its east end, spiked, so its spikes face the cat), stands up
            // behind the cat as it drops past x 13.6 and slides ChaseDistance east over ChaseTicks: a cat that stops is caught.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_W",(11.95f,F2 - .5f),(1.5f,1f),(13.6f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Pop_2: spikes in the floor at x 19.5-21 spring up as the cat comes within 1.7 u of them: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Pop_2",(20.25f,F2 + .15f),(1.5f,.3f),(19.4f,(F2 + C2) * .5f),(3.2f,C2 - F2),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Crumble, slab A's top unit at x 24.5-26, gives way 2 ticks after a touch; the cat drops into the recess onto
            // Spikes_Cr (hidden on the recess's floor, shown with it). Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumble",(25.25f,F2 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:2)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_Cr",(25.25f,F2 - .85f),(1.5f,.3f),settings:Chain("Crumble", 6)));

            var sections = new[] {
                CheckpointSection.Start("Lower", new Vector2(30.6f, F1), new[] { "Invert", "Hinge_1", "Flip_U1", "CC_1", "Spikes_CC1", "Lift_1", "Pop_C" }),
                new CheckpointSection("Upper", new Vector2(2.5f, C2), new Rect(1f, F2 + 1f, 2.5f, .2f),
                    new[] { "Push_2", "Flip_D2", "Hinge_W", "Pop_2", "Crumble", "Spikes_Cr" }, gravityUp: true),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top (or, down, against a ceiling's underside at `face`).
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face, bool down = false) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, down ? face - .15f : face + .15f), (x1 - x0, .3f)));

        // A chained trap: `delay` ticks after its source fires (a hidden spike's reveal delay is its delay).
        static SoloRoomTrapSettings Chain(string source, int delay) =>
            new(delayTicks: delay, revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);
    }
}
