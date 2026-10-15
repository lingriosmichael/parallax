using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-107 (D-142): level 16, "Flipside" (gravity flips). The developer rejected the old Upside-Down Garden (2026-10-15:
    // "traps are poorly placed, not challenging ... too many things off screen") and picked "Flipside corridors": three
    // stacked corridors, each 4.5 u from floor to ceiling, so the floor and the ceiling are both on screen wherever the cat
    // is. Honest flip orbs move the cat between the two faces, and every face has its traps. Walked east, west, east.
    //  - Floor (section 0, corridor 1, floor 0): Slam_1 comes down as the cat sets off (back off, let it land, hop it);
    //    Flip_U1 throws the cat onto the ceiling over Spikes_F1; on the ceiling CC_1 gives way into a spiked recess (jump
    //    it), Lift_1 rises out of the floor into the ceiling (wait) and Lift_1b right after it (stop between them); Flip_D1
    //    drops the cat back to the floor, where Hinge_1 stands up behind it and pushes it onto Spikes_F2 (don't stop); the
    //    mossy chimney climbs to corridor 2.
    //  - Arrows (section 1, corridor 2, floor 6.5): Piston_2 rises into the ceiling on its beat (wait for it); Slam_2 comes
    //    down out of the ceiling (stand still); Flip_U2 throws the cat onto the ceiling over Spikes_F3, and Arrow_M, which was
    //    shooting along the floor, now shoots along the ceiling (it mirrors with the cat's gravity: jump it); CC_2 gives way
    //    under a cat that stops; the shaft lets the cat fall up into corridor 3.
    //  - Top (section 2, corridor 3, floor 13): on the ceiling Push_3 comes down behind the cat and shoves it onto Spikes_C3
    //    (don't stop); Flip_D3 drops it to the floor. Spikes_R come and go on a beat: a cat that jumps them meets the hidden
    //    Flip_H at the top of its jump and falls up onto Spikes_CT (the troll: wait and walk through). The finale: Hinge_3
    //    stands up behind, Slam_3a comes down ahead (stop), hop onto it before the hinge arrives, wait there for Slam_3b,
    //    onto it, over Spikes_3b to the door.
    //  - Dead ends: back west on corridor 1's ceiling (Spikes_CW), and back west along corridor 3's floor toward the shaft
    //    (Spikes_D2).
    static class L016Layout
    {
        // The corridors' floors and ceilings (room-local y); every slab between them is 2 u of stone.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f;
        const float Slab = 2f;
        // Arrow_M's floor lane (shin height in corridor 2) and the line it mirrors across (corridor 2's middle).
        internal const float ArrowLaneY = F2 + .3f, MirrorAxis = (F2 + C2) * .5f;
        // Piston_2's beat and Spikes_R's: room ticks.
        internal const int PistonPeriod = 110, PistonPhase = 14, RhythmPeriod = 120, RhythmDown = 50;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,8.75f),(1f,21.5f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,8.75f),(1f,21.5f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1.5f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(31.4f,F3 + .75f),(.6f,1.5f)),
            };
            // The ground (y -2..0): Lift_1, Lift_1b and Hinge_1 are its top unit, each on a seat.
            Block(elements, SoloRoomElementKind.Floor, "Ground_A", 0f, 17.5f, -2f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1", 17.5f, 19f, -2f, -1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_B", 19f, 20.5f, -2f, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_L1b", 20.5f, 22f, -2f, -1f);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H1", 22f, 23.5f, -2f, -1f);
            Block(elements, SoloRoomElementKind.Floor, "Ground_C", 23.5f, 32f, -2f, F1);
            // Slab A (corridor 1's ceiling, corridor 2's floor): Slam_1 and CC_1 are its bottom unit under caps, Piston_2 its
            // top unit on a seat; the chimney (x 30-32) runs up through it.
            Block(elements, SoloRoomElementKind.Floor, "SA_0", 0f, 3.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S1", 3.5f, 5.5f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_1", 5.5f, 13.5f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C1", 13.5f, 15f, C1 + 1f, F2);
            Block(elements, SoloRoomElementKind.Floor, "SA_2", 15f, 26f, C1, F2);
            Block(elements, SoloRoomElementKind.Floor, "Seat_P2", 26f, 27.5f, C1, C1 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SA_3", 27.5f, 29.75f, C1, F2);
            // The chimney: Moss_W1 hangs from slab A's east end, Moss_E1 is the east wall's face; 1.75 u apart.
            // D-145 (the developer: "moss just in the air is weird. It needs to be connected to a wall"): Moss_W1 is the face of
            // Hang_W1, a stone pillar hung under slab A's east end down to the moss's foot.
            Block(elements, SoloRoomElementKind.Wall, "Hang_W1", 29.25f, 29.75f, 2.4f, C1);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_W1", 29.75f, 30f, 2.4f, F2);
            Block(elements, SoloRoomElementKind.GripWall, "Moss_E1", 31.75f, 32f, .6f, F2);
            // Slab B (corridor 2's ceiling, corridor 3's floor): the shaft (x 1.5-3.5) near its west end; CC_2 and Slam_2 are
            // its bottom unit under caps; Hinge_3 is its top unit on a seat.
            Block(elements, SoloRoomElementKind.Floor, "SB_0", 0f, 1.5f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_1", 3.5f, 13.6f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_C2", 13.6f, 15.1f, C2 + 1f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_2", 15.1f, 19f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Seat_H3", 19f, 20.5f, C2, C2 + 1f);
            Block(elements, SoloRoomElementKind.Floor, "SB_3", 20.5f, 22f, C2, F3);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 22f, 23.5f, C2 + 1f, F3);
            Block(elements, SoloRoomElementKind.Floor, "SB_4", 23.5f, 32f, C2, F3);
            // Corridor 2's west end: the jamb (up to the shaft's west side) that hosts Arrow_M, and the kerbs where its two lanes end.
            Block(elements, SoloRoomElementKind.Wall, "Jamb", 0f, 1.5f, F2, C2);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_F", 15.5f, 16f, F2, F2 + .6f);
            Block(elements, SoloRoomElementKind.Wall, "Kerb_C", 15.5f, 16f, C2 - .6f, C2);
            // The roof (y 17.5-19.5): Push_3, Slam_3a and Slam_3b are its bottom unit under caps.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 3.5f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_P3", 3.5f, 5f, C3 + 1f, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 5f, 24.5f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3a", 24.5f, 26.5f, C3 + 1f, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 26.5f, 27.5f, C3, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_S3b", 27.5f, 29f, C3 + 1f, C3 + Slab);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_D", 29f, 32f, C3, C3 + Slab);

            // Honest spikes: on corridor 1's ceiling west of Flip_U1 (the way back), its floor past Flip_U1 and before the
            // chimney, corridor 2's floor west of Flip_U2, and corridor 3's ceiling.
            Spikes(elements, "Spikes_F1", 9.5f, 15.6f, F1);
            Spikes(elements, "Spikes_F2", 27.5f, 28.7f, F1);
            Spikes(elements, "Spikes_F3", 1.5f, 15.5f, F2);
            Spikes(elements, "Spikes_C3", 8.2f, 9.4f, C3, down: true);
            Spikes(elements, "Spikes_CT", 17.5f, 24.5f, C3, down: true);

            // Dead ends. Spikes_CW, hidden on corridor 1's ceiling west of Flip_U1, come up over the cat as it walks the floor
            // toward Flip_U1 (their trigger is the corridor's height under them, to x 8): the way back on the ceiling. Spikes_D2,
            // hidden on corridor 3's floor at x 4-6, come up under the cat as it walks the ceiling past x 3.5 (their trigger is the
            // corridor's height over them, to x 6.8): the way back along the floor, to the shaft.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CW",(6.35f,C1 - .15f),(1.7f,.3f),(6.75f,(F1 + C1) * .5f),(2.5f,C1 - F1),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(5f,F3 + .15f),(2f,.3f),(5.15f,(F3 + C3 + 1f) * .5f),(3.3f,C3 + 1f - F3),new SoloRoomTrapSettings(revealDelayTicks:6)));

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);
            var down = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rearmOnExit:true,rendererEnabled:true);

            // ---------- Floor (corridor 1) ----------
            // Slam_1, slab A's bottom unit at x 3.5-5.5, slams 4.5 u onto the floor 3 ticks after the cat sets off (its cut
            // x 2.4-3.4) and stays: back off, let it land, hop it.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_1",(4.5f,C1 + .5f),(2f,1f),(2.9f,(F1 + C1) * .5f),(1f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:3,offset:new Vector2(0f,-C1),moveTicks:9,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U1",(8.7f,F1 + 1f),(1f,2f),settings:up));
            // CC_1, slab A's bottom unit at x 13.5-15, gives way 3 ticks after a cat on the ceiling touches it; the cat falls
            // up into the recess onto Spikes_CC1 (hidden on the cap's underside, shown with it). Jump it.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_1",(14.25f,C1 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC1",(14.25f,C1 + .85f),(1.5f,.3f),settings:Chain("CC_1", 6)));
            // Lift_1, the ground's top unit at x 17.5-19, rises 4.5 u into slab A (its crush partner) as the cat on the ceiling
            // comes within x 16.3 (15 ticks later, so it meets a cat that walks on over it): stop where the jump over CC_1 lands,
            // let it go up and down, walk on. Lift_1b (x 20.5-22) follows it 85 ticks later, under a cat that walks straight on:
            // stop between them and let it go up and down too.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1",(18.25f,F1 - .5f),(1.5f,1f),(16.1f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_2"),
                new SoloRoomTrapSettings(delayTicks:15,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_1b",(21.25f,F1 - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SA_2"),
                new SoloRoomTrapSettings(delayTicks:85,offset:new Vector2(0f,C1),moveTicks:8,holdTicks:20,returnTicks:15,triggerSource:TrapTriggerSource.Chain,chainSource:"Lift_1",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D1",(22.7f,C1 - 1f),(1f,2f),settings:down));
            // Hinge_1, the ground's top unit at x 22-23.5 (hinged at its west end), stands up behind a cat walking east past
            // x 24.6 (where Flip_D1 drops it) and slides 4 u east to Spikes_F2's edge: a cat that stops is pushed onto them.
            // D-145: a 3-tick swing and 4 u in 20 ticks (was 6 and 24); at 16 it caught the cat taking off over Spikes_F2.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_1",(22.75f,F1 - .5f),(1.5f,1f),(24.8f,(F1 + C1) * .5f),(.4f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:false),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(4f,0f),moveTicks:20,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            // ---------- Arrows (corridor 2) ----------
            // Piston_2, slab A's top unit at x 26-27.5, rises 4.5 u into slab B (its crush partner) every 110 ticks: wait for
            // it to go back down, then cross.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Piston_2",(26.75f,F2 - .5f),(1.5f,1f),(26.75f,(F2 + C2) * .5f),(1.5f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"SB_4"),
                new SoloRoomTrapSettings(offset:new Vector2(0f,C2 - F2),moveTicks:8,holdTicks:20,returnTicks:12,repeatMode:TrapRepeatMode.Periodic,periodTicks:PistonPeriod,phaseTicks:PistonPhase,cooldownTicks:42,movingKind:MovingTrapKind.Solid))));
            // Slam_2, slab B's bottom unit at x 22-23.5, slams onto a cat walking west past x 24.9, holds and goes back up
            // into Cap_S2 (its crush partner): stop short, stand still while it's down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(22.75f,C2 + .5f),(1.5f,1f),(24.68f,(F2 + C2) * .5f),(.4f,C2 - F2),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,crushPartner:"Cap_S2",slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,F2 - C2),moveTicks:18,holdTicks:20,returnTicks:15,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_U2",(16.7f,F2 + 1f),(1f,2f),settings:up));
            // Arrow_M, in the jamb, fires east along corridor 2 at shin height from the moment Slam_2 comes down, every 70 ticks
            // (a 10-tick tell), non-stop. It mirrors with the cat's gravity: with the cat on the ceiling, the shot flies along the ceiling.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_M",(1.25f,ArrowLaneY),(.5f,.25f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,ArrowLaneY,15.5f,tellTicks:10,mirrorWithGravity:true,mirrorAxisY:MirrorAxis),
                new SoloRoomTrapSettings(delayTicks:1,triggerSource:TrapTriggerSource.Chain,chainSource:"Slam_2",repeatMode:TrapRepeatMode.Continuous,cooldownTicks:69))));
            // CC_2, slab B's bottom unit at x 13.6-15.1, where Flip_U2 lands the cat, gives way 16 ticks after a cat on the ceiling
            // first touches it: a cat that runs on is past it; one that stops there to watch the arrows falls up onto Spikes_CC2.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"CC_2",(14.35f,C2 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:16)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_CC2",(14.35f,C2 + .85f),(1.5f,.3f),settings:Chain("CC_2", 6)));

            // ---------- Top (corridor 3) ----------
            // Push_3, the roof's bottom unit at x 3.5-5, comes down 1 u 30 ticks after the cat walks under it from the shaft (its
            // trigger is its own column, cap included), behind a cat that walks on, then slides 3.2 u east: a cat that stops short
            // of Spikes_C3 is shoved onto them.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Push_3",(4.25f,C3 + .5f),(1.5f,1f),(4.25f,(F3 + C3 + 1.6f) * .5f),(1.5f,C3 + 1.6f - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,riseFirst:true),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(3.2f,-1f),moveTicks:24,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D3",(12.5f,C3 - 1f),(1f,2f),settings:down));
            // Spikes_R, honest, on corridor 3's floor at x 17-18.5, down 50 ticks of every 120. The troll: Flip_H, hidden at the
            // top of a jump over them (0.8 u high, 1.3 u over the floor), throws a cat that jumps them up onto Spikes_CT. Wait
            // for them to go down and walk through.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_R",(17.75f,F3 + .15f),(1.5f,.3f),settings:Rhythm(RhythmPeriod, 0, RhythmDown)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_H",(17.75f,F3 + 1.7f),(1.2f,.8f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp)));
            // The finale, from the cut at x 22.6-23: Hinge_3 (slab B's top unit at x 19-20.5, spiked) stands up behind the cat and
            // slides 3 u east (D-145, the developer: "The spikes should also be on the other side. and move faster": hinged at its
            // east end, so its spikes face the cat; a 3-tick swing and 0.083 u a tick, was 6 and 0.058; Slam_3a comes down 4
            // ticks after it, over 8, so the hop onto it still beats the wall); Slam_3a (x 24.5-26.5) slams down ahead and stays; Spikes_3 (x 26.5-29) and Spikes_3b (x 29-30.7)
            // come up over the whole floor to the door, and Slam_3b (x 27.5-29) slams down onto them: the one step to the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_3",(19.75f,F3 - .5f),(1.5f,1f),(22.8f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(3f,0f),moveTicks:36,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3a",(25.5f,C3 + .5f),(2f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(27.75f,F3 + .15f),(2.5f,.3f),settings:Chain("Hinge_3", 30)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_3b",(28.25f,C3 + .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:80,offset:new Vector2(0f,F3 - C3),moveTicks:10,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Hinge_3",movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3b",(29.85f,F3 + .15f),(1.7f,.3f),settings:Chain("Hinge_3", 45)));

            var sections = new[] {
                CheckpointSection.Start("Floor", new Vector2(1.5f, F1), new[] { "Slam_1", "Flip_U1", "Spikes_CW", "CC_1", "Spikes_CC1", "Lift_1", "Lift_1b", "Flip_D1", "Hinge_1" }),
                new CheckpointSection("Arrows", new Vector2(28.6f, F2), new Rect(30f, F2 - .6f, 1.75f, .2f),
                    new[] { "Piston_2", "Slam_2", "Flip_U2", "Arrow_M", "CC_2", "Spikes_CC2" }),
                new CheckpointSection("Top", new Vector2(2.5f, C3), new Rect(1.5f, F3 + 1f, 2f, .2f),
                    new[] { "Push_3", "Spikes_D2", "Flip_D3", "Spikes_R", "Flip_H", "Hinge_3", "Slam_3a", "Spikes_3", "Slam_3b", "Spikes_3b" }, gravityUp: true),
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

        // Periodic spikes that are down over [down, up) of every period and up for the rest (L013's Rhythm): a periodic trap
        // shows on its fire tick and hides `cooldown` ticks later.
        static SoloRoomTrapSettings Rhythm(int period, int down, int up) =>
            new(revealDelayTicks: 6, repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: ((up % period) + period) % period, cooldownTicks: period - (up - down));
    }
}
