using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-263 (Docs/Design/L031-L035_concepts.md): level 32, "Overflow". A tower 24 wide, five storeys 5 apart, climbed through
    // wall-jump chimneys between circuit panels (grip walls, D-110) at alternate ends, while a laser flood rises from below. The
    // tower teaches "climb the safe way"; the flood punishes the slow way. Every storey has its own lie.
    //  - S0 (the ground, east): Flood_1 starts under the cat, rising out of the stone. Retract_0, a hard-light bridge over a hole, pulls back from under a
    //    cat's heels once it steps on it: run it. Chimney A (east) up to S1.
    //  - S1 (west): Bar_1, a hard-light bar, fires along the storey at a jumping cat's height as the cat comes to Step_1, the block
    //    it has to jump: a cat that jumps at once is hit; let it fly over (it sticks by Chimney B), then jump. Holo_1, a hologram,
    //    fills a hole in the floor: jump it. Chimney B (west) up to S2.
    //  - S2 (east, section 1): Flood_2 starts as the cat leaves the checkpoint. Squeeze_2, the lower half of S3's slab, comes down
    //    over the walk as the cat comes under it: don't stop. Flood_2 is at S2's floor soon after: straight up Chimney C.
    //  - S3 (west): a hole 8 u long in the floor. Flip_3U puts the cat on the ceiling (S4's underside); upside down, jump Flip_3T, an
    //    orb that would drop it into the hole; Inv_3, hidden past it, turns the stick round, so a cat still holding on walks back
    //    into Flip_3T. Flip_3D puts it back down by Chimney D, and the Surge rises up the chimney after it.
    //  - S4 (the top, east): Retract_4, over S3's hole, pulls back from under the cat over the flood; Bar_4 comes from behind at
    //    shin height (jump it) and sticks in the Sill by the door.
    static class L032Layout
    {
        internal const float Width = 24f, Base = -12f, RoofY = 25f, RoofTop = 27f, Slab = 1f;
        internal static readonly float[] F = { 0f, 5f, 10f, 15f, 20f };
        // The chimneys' air (2 wide): east x ChimE0-Width, west x 0-ChimW1. Pillars (a slab's end hung down) to PillarFoot above the
        // storey under it; grip strips GripT thick on the pillar's face and the tower wall's.
        internal const float ChimE0 = 22f, ChimW1 = 2f, PillarFoot = 1f, PillarT = .5f, GripT = .25f;
        // ---- Section 0 ----
        // Flood_1: a lethal laser band FloodH thick (thin, so it draws as a line, not a field of tall spikes), its top from
        // Flood1Top0 up to Flood1Top1 over Flood1Ticks from the start.
        internal const float FloodH = .3f, Flood1Top0 = -5f, Flood1Top1 = 8.4f;
        internal const int Flood1Ticks = 650;
        // The ground is solid stone (Flood_1 starts inside it, out of sight); only Pit_0 under Retract_0 is open, PitDepth deep.
        internal const float PitDepth = 2f;
        // Retract_0, S0's bridge (x Ret0X0-Ret0X1), shrinks from the west over Ret0Ticks once stepped on.
        internal const float Ret0X0 = 10f, Ret0X1 = 13f;
        internal const int Ret0Ticks = 40;
        // Step_1 on S1 (x Step1X0-Step1X1, 1 tall); Bar_1 from Bar1X west along S1 at Bar1Y, fired as a cat comes within Bar1TrigX.
        internal const float Step1X0 = 11f, Step1X1 = 12.5f, Bar1X = 18.25f, Bar1Y = 6.3f, Bar1TrigX0 = 12.5f, Bar1TrigX1 = 14.5f;
        internal const float Holo1X0 = 6f, Holo1X1 = 8f;
        // ---- Section 1 ----
        internal const float GateX = 3.6f, Checkpoint1X = 5.5f;
        // Flood_2: east of Flood2X0 (clear of Bar_1 stuck by Chimney B), up to Flood2Top1 (under S3's floor) over Flood2Ticks. The
        // Surge, set off as a cat comes down by Chimney D, fills the chimney's column (x 0-SurgeX1) up to SurgeTop1 (under S4's floor)
        // over SurgeTicks, a little faster than a cat climbs it. Both wait inside the stone (Flood_2 in S2's slab, the Surge in
        // Floor_3W), never in the air (the developer, 2026-10-31: "what is this spikes in the air"): a first leg of FloodHide ticks
        // that hardly moves stands in for the delay movers don't have, then each rises at its old speed, reaching its floor on
        // the same tick as when it waited lower down.
        internal const float Flood2X0 = 4f, Flood2Top0 = 9.4f, Flood2Top1 = 14.4f, SurgeX1 = 2.4f, SurgeTop0 = 14.5f, SurgeTop1 = 19.5f;
        internal const int Flood2Ticks = 493, Flood2Hide = 160, SurgeTicks = 155, SurgeHide = 64;
        // Squeeze_2 (x Sq2X0-Sq2X1), S3's lower layer, down Sq2Drop over Sq2Ticks once a cat comes to Sq2TrigX (its trigger ends where
        // it starts): a cat at a run is out from under it with about 5 ticks to spare.
        internal const float Sq2X0 = 12f, Sq2X1 = 18f, Sq2TrigX = 11.6f, Sq2Drop = 2.6f;
        internal const int Sq2Ticks = 72;
        // S3's hole (x Hole3X0-Hole3X1) and its orbs: Flip_3U (up), Flip_3T (down, on the ceiling: jump it), Inv_3, Flip_3D (down).
        internal const float Hole3X0 = 4f, Hole3X1 = 12f, Flip3UX = 13f, Flip3TX = 8f, Inv3X = 6.6f, Flip3DX = 3f;
        internal const int Inv3Ticks = 40;
        // Retract_4, S4's bridge over S3's hole (x Ret4X0-Ret4X1, as thick as the slab: its underside is the ceiling walk), shrinks
        // from the west over Ret4Ticks once stepped on. Bar_4 fires out of the Sill by the door west along S4 at shin height as a
        // cat passes Bar4TrigX, at the cat, and sticks in Wall_L.
        internal const float Ret4X0 = 5f, Ret4X1 = 11f, Bar4Y = 20.3f, Bar4TrigX = 13f, SillX0 = 19f, SillX1 = 20f, DoorX = 22.5f;
        internal const int Ret4Ticks = 60;

        public static SoloRoomDefinition Build()
        {
            var e = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,F[0]),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,F[4] + .75f),(.6f,1.5f)),
            };
            Block(e, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, RoofY, RoofTop);

            // ---- The tower's walls: plain, but for the grip strips facing each chimney ----
            // West: Chimney B (S1 to S2) and D (S3 to S4); east: Chimney A (S0 to S1) and C (S2 to S3).
            // The grip runs half a unit past the storey above, so a cat's last latch is never on the seam with the plain wall (two
            // colliders at once made the replays differ).
            Wall(e, "Wall_L", -1f, 0f, new[] { (F[1], F[2] + .5f), (F[3], F[4] + .5f) }, west: true);
            Wall(e, "Wall_R", Width, Width + 1f, new[] { (F[0], F[1] + .5f), (F[2], F[3] + .5f) }, west: false);

            // ---- S0: the ground (Retract_0 over its hole) ----
            Block(e, SoloRoomElementKind.Floor, "Floor_0a", 0f, Ret0X0, Base, F[0]);
            Block(e, SoloRoomElementKind.Floor, "Floor_0b", Ret0X1, Width, Base, F[0]);
            e.Add(E(SoloRoomElementKind.PitBottom,"Pit_0",((Ret0X0 + Ret0X1) * .5f,(Base - PitDepth) * .5f),(Ret0X1 - Ret0X0,-PitDepth - Base)));
            e.Add(E(SoloRoomElementKind.Hazard,"Pit_0_Laser",((Ret0X0 + Ret0X1) * .5f,-PitDepth + .15f),(Ret0X1 - Ret0X0,.3f)));
            e.Add(E(SoloRoomElementKind.ShrinkingFloor,"Retract_0",((Ret0X0 + Ret0X1) * .5f,F[0] - Slab * .5f),(Ret0X1 - Ret0X0,Slab),(Ret0X0 + .2f,F[0] + (F[1] - Slab - F[0]) * .5f),(.4f,F[1] - Slab - F[0]),
                new SoloRoomTrapSettings(new ShrinkSettings(Ret0Ticks, 0f, ShrinkFrom.Left), new SoloRoomTrapSettings(delayTicks:0))));
            // Flood_1, under the ground, rises from the start (its trigger is the spawn's storey band).
            e.Add(E(SoloRoomElementKind.MovingTrap,"Flood_1",(Width * .5f,Flood1Top0 - FloodH * .5f),(Width,FloodH),(2f,(F[0] + F[1] - Slab) * .5f),(4f,F[1] - Slab - F[0]),new SoloRoomTrapSettings(
                offset:new Vector2(0f,Flood1Top1 - Flood1Top0),moveTicks:Flood1Ticks,holdTicks:9999,movingKind:MovingTrapKind.Hazard)));

            // ---- S1 (Chimney A's pillar at its east end, Holo_1 in it) ----
            Pillar(e, "Pillar_A", ChimE0, F[1], east: true);
            Block(e, SoloRoomElementKind.Floor, "Floor_1a", 0f, Holo1X0, F[1] - Slab, F[1]);
            e.Add(E(SoloRoomElementKind.FakePlatform,"Holo_1",((Holo1X0 + Holo1X1) * .5f,F[1] - Slab * .5f),(Holo1X1 - Holo1X0,Slab)));
            Block(e, SoloRoomElementKind.Floor, "Floor_1b", Holo1X1, ChimE0, F[1] - Slab, F[1]);
            Block(e, SoloRoomElementKind.Floor, "Step_1", Step1X0, Step1X1, F[1], F[1] + 1f);
            // Bar_1's post, hung from S2 down to a walk's head room, and the bar: out of its post west along S1 at a jumping cat's
            // height to Pillar_B's face, where it sticks.
            Block(e, SoloRoomElementKind.Wall, "Post_1", Bar1X - .25f, Bar1X + .25f, Bar1Y - .4f, F[2] - Slab);
            e.Add(E(SoloRoomElementKind.Arrow,"Bar_1",(Bar1X,Bar1Y),(.5f,.4f),((Bar1TrigX0 + Bar1TrigX1) * .5f,(F[1] + F[2] - Slab) * .5f),(Bar1TrigX1 - Bar1TrigX0,F[2] - Slab - F[1]),
                new SoloRoomTrapSettings(ArrowLane.HardLightBar(ArrowDirection.Left, Bar1Y, ChimW1 + PillarT, tellTicks: 16), new SoloRoomTrapSettings(delayTicks: 0))));

            // ---- S2 (Chimney B's pillar at its west end) ----
            Pillar(e, "Pillar_B", ChimW1, F[2], east: false);
            Block(e, SoloRoomElementKind.Floor, "Floor_2", ChimW1, Width, F[2] - Slab, F[2]);
            // Flood_2: in S2's slab, rising once a cat is at Checkpoint_1 (the trigger covers it).
            e.Add(E(SoloRoomElementKind.MovingTrap,"Flood_2",((Flood2X0 + Width) * .5f,Flood2Top0 - FloodH * .5f),(Width - Flood2X0,FloodH),((ChimW1 + 8f) * .5f,(F[2] + RoofY) * .5f),(8f - ChimW1,RoofY - F[2]),Rise(Flood2Top1 - Flood2Top0, Flood2Hide, Flood2Ticks)));
            // The Surge: in Floor_3W; set off as a cat comes down by Chimney D (the trigger is S3's west end), it rises up the chimney after it.
            e.Add(E(SoloRoomElementKind.MovingTrap,"Surge",(SurgeX1 * .5f,SurgeTop0 - FloodH * .5f),(SurgeX1,FloodH),(Hole3X0 * .5f,(F[3] + RoofY) * .5f),(Hole3X0,RoofY - F[3]),Rise(SurgeTop1 - SurgeTop0, SurgeHide, SurgeTicks)));
            // ---- S3: two layers (y F3-2 to F3) east of the hole; Squeeze_2 is the lower layer's middle ----
            Block(e, SoloRoomElementKind.Floor, "Floor_3W", 0f, Hole3X0, F[3] - 2f * Slab, F[3]);
            Block(e, SoloRoomElementKind.Floor, "Floor_3E", Hole3X1, ChimE0, F[3] - Slab, F[3]);
            Block(e, SoloRoomElementKind.Ceiling, "Under_3b", Sq2X1, ChimE0 - PillarT, F[3] - 2f * Slab, F[3] - Slab);
            Pillar(e, "Pillar_C", ChimE0, F[3], east: true);
            e.Add(E(SoloRoomElementKind.MovingTrap,"Squeeze_2",((Sq2X0 + Sq2X1) * .5f,F[3] - 1.5f * Slab),(Sq2X1 - Sq2X0,Slab),(Sq2TrigX + .2f,(F[2] + F[4] - Slab) * .5f),(.4f,F[4] - Slab - F[2]),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                new SoloRoomTrapSettings(offset:new Vector2(0f,-Sq2Drop),moveTicks:Sq2Ticks,holdTicks:9999,movingKind:MovingTrapKind.Solid))));
            // The orbs, honest (drawn): Flip_3U on S3's floor, Flip_3T and Flip_3D on the ceiling (S4's underside); Inv_3 hidden.
            var up = new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceUp, rearmOnExit: true, rendererEnabled: true);
            var down = new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceDown, rearmOnExit: true, rendererEnabled: true);
            float ceil3 = F[4] - Slab;
            e.Add(E(SoloRoomElementKind.GravityFlip,"Flip_3U",(Flip3UX,F[3] + 1f),(1f,2f),settings:up));
            // Flip_3T is small (0.5 x 0.6), so a jump away from the ceiling (1.6) clears it at its height.
            e.Add(E(SoloRoomElementKind.GravityFlip,"Flip_3T",(Flip3TX,ceil3 - .3f),(.5f,.6f),settings:down));
            e.Add(E(SoloRoomElementKind.Inverter,"Inv_3",(Inv3X,ceil3 - 1f),(.4f,2f),settings:new SoloRoomTrapSettings(new InverterSettings(Inv3Ticks, disguised: true), new SoloRoomTrapSettings(delayTicks:0))));
            e.Add(E(SoloRoomElementKind.GravityFlip,"Flip_3D",(Flip3DX,ceil3 - .75f),(1f,1.5f),settings:down));

            // ---- S4: the top (Chimney D's pillar at its west end, Retract_4) ----
            Pillar(e, "Pillar_D", ChimW1, F[4], east: false);
            Block(e, SoloRoomElementKind.Floor, "Floor_4a", ChimW1, Ret4X0, F[4] - Slab, F[4]);
            Block(e, SoloRoomElementKind.Floor, "Floor_4b", Ret4X1, Width, F[4] - Slab, F[4]);
            e.Add(E(SoloRoomElementKind.ShrinkingFloor,"Retract_4",((Ret4X0 + Ret4X1) * .5f,F[4] - Slab * .5f),(Ret4X1 - Ret4X0,Slab),(Ret4X0 + .2f,(F[4] + RoofY) * .5f),(.4f,RoofY - F[4]),
                new SoloRoomTrapSettings(new ShrinkSettings(Ret4Ticks, 0f, ShrinkFrom.Left), new SoloRoomTrapSettings(delayTicks:0))));
            // The Sill by the door, and Bar_4: out of the Sill west along S4 at shin height, at the cat, into Wall_L.
            Block(e, SoloRoomElementKind.Floor, "Sill", SillX0, SillX1, F[4], F[4] + .6f);
            e.Add(E(SoloRoomElementKind.Arrow,"Bar_4",(SillX0 + .25f,Bar4Y),(.5f,.4f),(Bar4TrigX + .2f,(F[4] + RoofY) * .5f),(.4f,RoofY - F[4]),
                new SoloRoomTrapSettings(ArrowLane.HardLightBar(ArrowDirection.Left, Bar4Y, 0f, tellTicks: 12), new SoloRoomTrapSettings(delayTicks: 0))));

            var sections = new[] {
                CheckpointSection.Start("Lower", new Vector2(2f, F[0]), new[] { "Flood_1", "Retract_0", "Bar_1", "Holo_1" }),
                new CheckpointSection("Upper", new Vector2(Checkpoint1X, F[2]), new Rect(GateX, F[2], .4f, 1.5f),
                    new[] { "Flood_2", "Squeeze_2", "Flip_3U", "Flip_3T", "Inv_3", "Flip_3D", "Surge", "Retract_4", "Bar_4" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, e.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A tower wall from x0 to x1, Base to RoofTop: plain, but along each band a grip strip GripT thick on its inner face.
        static void Wall(List<SoloRoomElement> e, string name, float x0, float x1, (float y0, float y1)[] grip, bool west)
        {
            float y = Base;
            int k = 0;
            foreach ((float g0, float g1) in grip)
            {
                Block(e, SoloRoomElementKind.Wall, $"{name}_{k++}", x0, x1, y, g0);
                float inner = west ? x1 - GripT : x0 + GripT;
                Block(e, SoloRoomElementKind.Wall, $"{name}_{k++}", west ? x0 : inner, west ? inner : x1, g0, g1);
                e.Add(E(SoloRoomElementKind.GripWall, $"{name}_Grip{k}", (west ? x1 - GripT * .5f : x0 + GripT * .5f, (g0 + g1) * .5f), (GripT, g1 - g0)));
                y = g1;
            }
            Block(e, SoloRoomElementKind.Wall, $"{name}_{k}", x0, x1, y, RoofTop);
        }

        // A slab's end hung down as a chimney's pillar: from the slab's bottom (top - Slab) down to PillarFoot over the storey below,
        // a grip strip on the face toward the chimney (east of the pillar at chimX for the east chimneys, west for the west ones).
        static void Pillar(List<SoloRoomElement> e, string name, float chimX, float top, bool east)
        {
            float y0 = top - 5f + PillarFoot, y1 = top - Slab;
            if (east)
            {
                Block(e, SoloRoomElementKind.Wall, name, chimX - PillarT, chimX - GripT, y0, y1);
                e.Add(E(SoloRoomElementKind.GripWall, name + "_Grip", (chimX - GripT * .5f, (y0 + y1) * .5f), (GripT, y1 - y0)));
            }
            else
            {
                Block(e, SoloRoomElementKind.Wall, name, chimX + GripT, chimX + PillarT, y0, y1);
                e.Add(E(SoloRoomElementKind.GripWall, name + "_Grip", (chimX + GripT * .5f, (y0 + y1) * .5f), (GripT, y1 - y0)));
            }
        }

        // A flood that waits `hide` ticks in the stone (a first leg of a hundredth of a unit), then rises `rise` over the rest of
        // `ticks`, and stays.
        static SoloRoomTrapSettings Rise(float rise, int hide, int ticks) => new(new PathSettings(new[] { new Vector2(0f, .01f), new Vector2(0f, rise) }, firstLegTicks: hide),
            new SoloRoomTrapSettings(moveTicks: ticks, holdTicks: 9999, movingKind: MovingTrapKind.Hazard));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
