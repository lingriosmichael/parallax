using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 17, "Deja Vu" (spears and chains). The verb is PLAN THE WAY BACK: the door hangs over the
    // start, out of reach, and the way out builds the way back in plain view (the approved variant). 32 x 16, two
    // storeys: east along the lower storey (y 0), up the stair the far end builds, west along the upper storey (y 8).
    //  - Out (section 0): stepping off the start fires Spear_D into the Door_Ledge's west face: its shaft is the last step
    //    to the door. Crack_1 invites a hop into Spear_1's lane (walk it). Block_2 drops out of the upper floor onto the
    //    path (wait, then hop it) and sets off Collapse_U beside it, which leaves a hole in the upper floor. Collapse_3
    //    gives way under a cat that stops. Slide_4 (Carry) carries a cat that stands still over Pit_4.
    //  - Far (section 1): the far cut starts the rearrangement: Arrow_5a sweeps the shaft's floor at shin height (step
    //    back over the Sill into the nook), then the volley V1-V5 builds the stair, alternating walls.
    //  - Back (section 2): stepping off the stair drops Block_6 onto the upper floor's edge (go straight on) and brings up
    //    Spikes_5e, which stay (jump them); hop Post_8; Arrow_8 comes out of it along the upper storey at jump height (let
    //    it pass over, then jump the hole); under the Door_Ledge to the west end, up Spear_D's shaft onto the ledge.
    //  - Dead ends: the Shelf over the start, reached from Spear_1's shaft (hidden spikes, Dies); staying on Slide_4 as it
    //    rides home (Recovers).
    static class L017Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;
        // The upper floor is 0.7 thick (7.3-8), so Block_2 sits flush in it and lands with a short last step.
        internal const float UpperY = 8f, UpperBottom = 7.3f, CeilingY = 16f, FaceR = 31.6f, SpineX = 26.6f, LedgeY = 9.8f;
        // The volley's steps: tops 1.45 apart, alternating Face_R (odd steps) and the Spine's east face (even steps).
        internal static float StepTop(int k) => 1.45f * k;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,6.5f),(1f,21f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,6.5f),(1f,21f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,CeilingY + .5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)),
                // The lower storey (top 0): Ground_A (x 0-6.2), Crack_1 (6.2-6.8, narrower than the cat), Ground_B (6.8-15),
                // Pit_3 under Collapse_3 (15-17), Ground_C (17-19), Pit_4 (19-25) under Slide_4, Ground_F (25-32).
                E(SoloRoomElementKind.Floor,"Ground_A",(3.1f,-1.75f),(6.2f,3.5f)),
                E(SoloRoomElementKind.Floor,"Ground_B",(10.9f,-1.75f),(8.2f,3.5f)),
                E(SoloRoomElementKind.PitBottom,"Pit3_Floor",(16f,-3.25f),(2f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_3",(16f,-2.85f),(2f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.Floor,"Ground_C",(18f,-1.75f),(2f,3.5f)),
                E(SoloRoomElementKind.PitBottom,"Pit4_Floor",(22f,-3.25f),(6f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_4",(22f,-2.85f),(6f,.3f),hazardRole:Pit),
                E(SoloRoomElementKind.Floor,"Ground_F",(28.5f,-1.75f),(7f,3.5f)),
                // Pillar_1 (x 9-9.5, from 1.0 up) hosts Spear_1; the Shelf (x 1.8-3.8, top 2.95) over the start.
                E(SoloRoomElementKind.Wall,"Pillar_1",(9.25f,(1f + UpperBottom) * .5f),(.5f,UpperBottom - 1f)),
                E(SoloRoomElementKind.Floor,"Shelf",(2.8f,2.7f),(2f,.5f)),
                // The far end: the Spine (x 26.6-27.6, from 1.0 up: the nook's doorway under it), the Sill (x 28.6-29, 0.45
                // tall, a cat's width clear of the Spine so it's hopped under open air) closing the nook, the stair's shaft
                // (x 27.6-31.6) and Face_R (x 31.6-32).
                E(SoloRoomElementKind.Wall,"Spine",(SpineX + .5f,(1f + UpperBottom) * .5f),(1f,UpperBottom - 1f)),
                E(SoloRoomElementKind.Wall,"Sill",(28.8f,.225f),(.4f,.45f)),
                E(SoloRoomElementKind.Wall,"Face_R",(FaceR + .2f,CeilingY * .5f),(.4f,CeilingY)),
                // The upper storey (top 8): UF_W (x 0-11), Collapse_U (11-13), UF_E (13-27.6) with Block_2 flush in it (13-15).
                // Post_W (x 0-0.4) hosts Spear_D; Post_8 (x 19-19.4, top 9.2) hosts Arrow_8; the Door_Ledge (x 4.5-8,
                // 8.7-9.8) floats over UF_W, its underside above a standing cat.
                E(SoloRoomElementKind.Floor,"UF_W",(5.5f,UpperY - .35f),(11f,.7f)),
                // PAX-101 (D-106): the upper floor's east part in two, with Sink_9 (below) between them, past Post_8.
                E(SoloRoomElementKind.Floor,"UF_E1",(14.25f,UpperY - .35f),(2.5f,.7f)),
                E(SoloRoomElementKind.Floor,"UF_E",(22.8f,UpperY - .35f),(9.6f,.7f)),
                E(SoloRoomElementKind.Wall,"Post_W",(.2f,UpperY + .8f),(.4f,1.6f)),
                E(SoloRoomElementKind.Wall,"Post_8",(19.2f,UpperY + .6f),(.4f,1.2f)),
                E(SoloRoomElementKind.Floor,"Door_Ledge",(6.25f,(8.7f + LedgeY) * .5f),(3.5f,LedgeY - 8.7f)),
                E(SoloRoomElementKind.Door,"Door",(6.25f,LedgeY + .75f),(.6f,1.5f)),
            };

            // Out. The cut at x 3.4-3.8 fires Spear_D from Post_W into the Door_Ledge's west face (shaft x 3.1-4.5, top 9.45):
            // a jump from UF_W reaches it, and the ledge is 1.8 above UF_W, out of a jump's reach without it.
            elements.Add(Spear("Spear_D", .2f, LedgeY - .55f, ArrowDirection.Right, 4.5f, (3.6f, UpperBottom * .5f), (.4f, UpperBottom), new SoloRoomTrapSettings(delayTicks:0)));
            // The cut at x 5.0-5.4 fires Spear_1 from Pillar_1 at jump height over Crack_1 (a walking cat is under it); it
            // sticks in Wall_L over the start (shaft x 0-1.4, top 1.5), the step up to the Shelf.
            elements.Add(Spear("Spear_1", 9.2f, 1.3f, ArrowDirection.Left, 0f, (5.2f, UpperBottom * .5f), (.4f, UpperBottom), new SoloRoomTrapSettings(delayTicks:7)));
            // Block_2, flush in UF_E, drops 10 ticks after the cut at x 10.4-10.8 onto the path (x 13-15), where a walking cat
            // is by then; 2 ticks later Collapse_U, the upper floor beside it, gives way and leaves the hole for the way back.
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(14f,UpperY - .35f),(2f,.7f),(10.6f,UpperBottom * .5f),(.4f,UpperBottom),
                new SoloRoomTrapSettings(delayTicks:10,unitsPerTick:.36f,travelDistance:UpperBottom)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_U",(12f,UpperY - .35f),(2f,.7f),settings:Chain("Block_2", 2)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_3",(16f,-1.5f),(2f,3f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            // Slide_4 (Carry) over Pit_4: a touch sends it 4 u east, to Ground_F, over 120 ticks; it waits and comes home.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slide_4",(20f,-.25f),(2f,.5f),(20f,.75f),(2f,1.5f),
                new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry), new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(4f,0f),
                    moveTicks:120,holdTicks:60,returnTicks:120,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:310,movingKind:MovingTrapKind.Solid))));
            // Dead end: the Shelf (hidden spikes; the cut spans the Shelf's top up to the upper floor).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(2.8f,3.1f),(1.8f,.3f),(2.8f,(2.95f + UpperBottom) * .5f),(2f,UpperBottom - 2.95f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Far. The far cut (x 29.6-30, the shaft's full height) fires Arrow_5a from Face_R along the shaft's floor at shin
            // height into the Sill; 150 ticks later the volley, 12 ticks apart.
            // D-119 (the developer: all launchers shoot non-stop): Arrow_5a keeps its first shot's trigger, then fires every 200 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_5a",(FaceR + .2f,.3f),(.4f,.4f),(29.8f,CeilingY * .5f),(.4f,CeilingY),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,.3f,29f,unitsPerTick:.36f,tellTicks:16,disguised:true),new SoloRoomTrapSettings(delayTicks:30,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:200))));
            for (int k = 1; k <= 5; k++)
            {
                bool east = k % 2 == 1;   // odd steps stick in Face_R, even ones in the Spine's east face
                float lane = StepTop(k) - .2f;
                SoloRoomTrapSettings timing = Chain(k == 1 ? "Arrow_5a" : "V" + (k - 1), k == 1 ? 150 : 12);
                elements.Add(east
                    ? Spear("V" + k, SpineX + .8f, lane, ArrowDirection.Right, FaceR, default, default, timing)
                    : Spear("V" + k, FaceR + .2f, lane, ArrowDirection.Left, SpineX + 1f, default, default, timing));
            }

            // Back. A step off the stair (the cut at x 26.3-26.5) brings up Spikes_5e, which stay, and 8 ticks later drops
            // Block_6, flush in the ceiling, onto the upper floor where the cat stepped off (x 25.6-27).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_5e",(23.25f,UpperY + .15f),(1.5f,.3f),(26.4f,(UpperY + CeilingY) * .5f),(.2f,CeilingY - UpperY),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_6",(26.3f,CeilingY + .5f),(1.4f,1f),
                settings:new SoloRoomTrapSettings(delayTicks:8,unitsPerTick:.36f,travelDistance:CeilingY - UpperY,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_5e")));
            // Crossing x 15 fires Arrow_8 out of Post_8 along the upper storey at jump height (over a standing cat) into the
            // Door_Ledge's east face; it tells for 60 ticks.
            // T9 (PAX-101, D-106): the floor where the hop over Post_8 lands sinks while stood on. Sink_9 (x 15.5-18) goes 3 u
            // down over 60 ticks, 30 ticks after a landing (its trigger is its top strip; a cat that walks on is off it in about
            // 15), holds 40 and comes back, carrying a cat that stayed: it recovers, under Arrow_8's lane.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sink_9",(16.75f,UpperY - .35f),(2.5f,.7f),(16.75f,UpperY + .28f),(2.5f,.56f),new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:30,offset:new Vector2(0f,-3f),moveTicks:60,holdTicks:40,returnTicks:60,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:170,movingKind:MovingTrapKind.Solid))));
            // D-119 (the developer: all launchers shoot non-stop): Arrow_8 keeps its first shot's trigger, then fires every 250 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_8",(19.2f,UpperY + .9f),(.4f,.4f),(15f,(UpperY + CeilingY) * .5f),(.4f,CeilingY - UpperY),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,UpperY + .9f,8f,unitsPerTick:.36f,tellTicks:60,disguised:true),new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:250))));

            var sections = new[] {
                CheckpointSection.Start("Out", new Vector2(2f, 0f), new[] { "Spear_D", "Spear_1", "Block_2", "Collapse_U", "Collapse_3", "Slide_4", "Spikes_D1" }),
                new CheckpointSection("Far", new Vector2(25.9f, 0f), new Rect(25.4f, 0f, .2f, UpperBottom), new[] { "Arrow_5a", "V1", "V2", "V3", "V4", "V5" }),
                new CheckpointSection("Back", new Vector2(24.8f, UpperY), new Rect(25.2f, UpperY, .2f, CeilingY - UpperY), new[] { "Spikes_5e", "Block_6", "Sink_9", "Arrow_8" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (.4f, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX), timing));
    }
}
