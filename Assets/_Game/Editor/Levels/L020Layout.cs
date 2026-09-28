using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 20, "The Machine" (the exam: every earlier lesson once, in a new order; P8 exempts it). The
    // verb is RIDE THE CHAIN REACTION: the cat's own triggers set one machine going, which rebuilds the room around it,
    // and the cat moves with it. 32 x 24, a spiral that doubles back: from the start in the middle west to the lever, back
    // east along the mid storey, up the chimney by the east wall, west along the Top over the start, up V_L to the Loft,
    // east along it, and up G_9's column into the door.
    //  - Out (section 0): the way east is shut (Gate_1). The lever, a cut at the storey's west end, starts the machine:
    //    Gate_1 slides up; Arrow_1 flies east from the west wall along the storey at standing height (it catches the cat
    //    on its way back: jump it); Push_3, low, follows the cat east and crushes it into Post_P if it's still in front of
    //    it (hop the post). Past the gate, Collapse_2 gives way a step early (stop, let it go, jump the gap). The chimney's
    //    mouth fires the volley, Spear_4a-4c, 30 ticks apart, alternating walls: wait for each to stick. The Perch tops the
    //    stair; V_5 hangs beside it.
    //  - Back (section 1, from the Perch): climbing V_5 sets it to snap, and the snap wakes the machine's second half:
    //    Inv_6 flips the controls, the Cloud wakes over the Top's east half. Leap onto the Top before the snap; crossing the
    //    Spine's top drops Block_7a and Block_7b behind the cat, closing the way back. Mirrored, keep moving west out of the
    //    cloud's reach; wait out the flip; on over Shrink_8 (don't stop on it) and the vent to V_L.
    //  - Last (section 2, the Loft): the door backs away east off the Loft, down into G_9's column (Retreat_10): don't
    //    chase it off the edge; drop back, stand on the vent and ride the next eruption into it.
    //  - Dead ends: G_9 on the way west (it throws the cat up short of the Loft; it comes down and goes on) and Roof_N, a
    //    shelter from the cloud (a strike passes; the cat goes on). Both recover.
    static class L020Layout
    {
        internal const float MidY = 8f, TopY = 16f, CeilingY = 24f, SpineX = 26f, FaceR = 31f, PostX = 14.5f;
        const float WellY = 4f, BasinY = 11f, LoftY = 22.5f;
        // The volley's steps: tops 1.45 apart, alternating the east wall (1, 3) and the Spine's east face (2, and the Perch).
        internal static float StepTop(int k) => MidY + 1.45f * k;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,12f),(1f,28f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(FaceR + .5f,12f),(1f,28f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,CeilingY + .5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(16.2f,MidY),(0f,0f)),
                // The mid storey: Mid_W (x 0-19, with Post_P, 1 u tall, at x 14.5-14.9), Collapse_2's well (x 19-21), Mid_E (x 21-31).
                // The start is in the pen between Post_P and Gate_1.
                E(SoloRoomElementKind.Floor,"Mid_W",(9.5f,MidY - .25f),(19f,.5f)),
                E(SoloRoomElementKind.Wall,"Post_P",(PostX + .2f,MidY + .5f),(.4f,1f)),
                E(SoloRoomElementKind.Wall,"Post_W",(.2f,(MidY + TopY - .5f) * .5f),(.4f,TopY - .5f - MidY)),
                E(SoloRoomElementKind.Floor,"Core",(17.5f,3f),(3f,9f)),
                E(SoloRoomElementKind.PitBottom,"Well2_Floor",(20f,WellY - .25f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Mid_E",(26f,(MidY - 3f) * .5f),(10f,MidY + 3f)),
                // The Spine (x 26-27, from 9.05 up to the Top) hosts the east-flying spears; the chimney is x 27-31, the Perch (x
                // 27-28.4) its last step, Thorns_5 its floor under V_5.
                E(SoloRoomElementKind.Wall,"Spine",(SpineX + .5f,(9.05f + TopY) * .5f),(1f,TopY - 9.05f)),
                E(SoloRoomElementKind.Floor,"Perch",(27.7f,StepTop(4) - .25f),(1.4f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_5",(28.8f,MidY + .15f),(1.2f,.3f)),
                E(SoloRoomElementKind.Ceiling,"Lintel_5",(29.7f,18.75f),(2.6f,.5f)),
                // The Top: Top_E (x 17-26) with Roof_N, Shrink_8 (x 12-17) over its well, Top_W (x 0-12; G_9's vent at its east
                // end, x 11-12, over Post_9: one piece, so no seam to step across at the vent). V_L, a plain vine by the west wall,
                // climbs to the Loft (x 2.3-10, top 22.5), where the door is.
                E(SoloRoomElementKind.Floor,"Top_E",(21.5f,TopY - .25f),(9f,.5f)),
                E(SoloRoomElementKind.Floor,"Roof_N",(21.5f,TopY + 1.6f),(2f,.5f)),
                E(SoloRoomElementKind.PitBottom,"Well8_Floor",(14.5f,BasinY - .25f),(5f,.5f)),
                E(SoloRoomElementKind.Floor,"Post_9",(11.5f,(BasinY + TopY - .5f) * .5f),(1f,TopY - .5f - BasinY)),
                E(SoloRoomElementKind.Floor,"Top_W",(6f,TopY - .25f),(12f,.5f)),
                E(SoloRoomElementKind.Ceiling,"Lintel_7",(26f,20f),(1f,2f)),
                E(SoloRoomElementKind.Vine,"V_L",(1.2f,(TopY + .2f + CeilingY) * .5f),(.6f,CeilingY - TopY - .2f)),
                E(SoloRoomElementKind.Floor,"Loft",(6.15f,LoftY - .25f),(7.7f,.5f)),
                E(SoloRoomElementKind.Door,"Door",(7.5f,LoftY + .75f),(.6f,1.5f)),
            };

            // Out. The lever, the cut at x 2.3-2.7, sets Gate_1 (x 18-18.8, too tall to jump) sliding up, Arrow_1 flying east
            // from Post_W at standing height to Post_P, and Push_3 (0.4 tall, under the arrow's lane) shoving east to a
            // cat's width short of Post_P, its crush partner.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_1",(.2f,MidY + .53f),(.4f,.4f),(2.5f,(MidY + TopY) * .5f),(.4f,TopY - MidY),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,MidY + .53f,PostX,unitsPerTick:.36f,tellTicks:8,disguised:true),new SoloRoomTrapSettings(delayTicks:32))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Gate_1",(18.4f,MidY + 1.25f),(.8f,2.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip),
                new SoloRoomTrapSettings(offset:new Vector2(0f,4f),moveTicks:20,holdTicks:5000,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_1",delayTicks:1,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Push_3",(.9f,MidY + .2f),(1f,.4f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,crushPartner:"Post_P"),
                new SoloRoomTrapSettings(offset:new Vector2(PostX - .7f - 1.4f,0f),moveTicks:155,holdTicks:5000,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_1",delayTicks:20,movingKind:MovingTrapKind.Solid))));
            // Collapse_2 fills its well and gives way a step early: its root is Spikes_2 (hidden, on the well's floor), whose cut
            // (x 18.4-18.8, where the gate stood) a cat crosses a step before the floor.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(20f,WellY + .15f),(2f,.3f),(18.6f,(MidY + TopY) * .5f),(.4f,TopY - MidY),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_2",(20f,MidY - .25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:1,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_2")));
            // The volley: the cut in the chimney's mouth (x 27.2-27.6) fires Spear_4a, each next one 30 ticks after. A cat that
            // stands on the floor is under every lane; one that hops onto a step before the next has stuck is in its lane.
            elements.Add(Spear("Spear_4a", SpineX + .5f, StepTop(1) - .2f, ArrowDirection.Right, FaceR, (27.4f, (MidY + CeilingY) * .5f), (.4f, CeilingY - MidY), new SoloRoomTrapSettings(delayTicks:4)));
            elements.Add(Spear("Spear_4b", FaceR + .5f, StepTop(2) - .2f, ArrowDirection.Left, SpineX + 1f, default, default, Chain("Spear_4a", 30)));
            elements.Add(Spear("Spear_4c", SpineX + .5f, StepTop(3) - .2f, ArrowDirection.Right, FaceR, default, default, Chain("Spear_4b", 30)));

            // Back. V_5 hangs from Lintel_5 beside the Perch; climbing into its cut (y 14.8-15.6) snaps it 60 ticks later (a cat
            // that leaps at once is on the Top first). The snap flips the controls (Inv_6) and wakes the Cloud over x 20-30
            // (which also strikes a cat left in the chimney). Crossing the Spine's top (the cut at x 26.3-26.7) drops Block_7a,
            // then Block_7b onto it, out of Lintel_7 onto the Top's east end.
            elements.Add(E(SoloRoomElementKind.Vine,"V_5",(28.8f,(StepTop(4) + .1f + 18.5f) * .5f),(.6f,18.5f - StepTop(4) - .1f),(28.8f,15.2f),(.6f,.8f),new SoloRoomTrapSettings(delayTicks:60)));
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_6",(24f,TopY + 1.5f),(.6f,3f),settings:new SoloRoomTrapSettings(new InverterSettings(150),Chain("V_5", 1))));
            elements.Add(E(SoloRoomElementKind.StormCloud,"Cloud",(29f,22.5f),(2f,.8f),
                settings:new SoloRoomTrapSettings(new StormCloudSettings(20f, 30f), Chain("V_5", 25))));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_7a",(26f,19.5f),(1f,1f),(26.5f,(TopY + CeilingY) * .5f),(.4f,CeilingY - TopY),
                new SoloRoomTrapSettings(delayTicks:41,unitsPerTick:.36f,travelDistance:3f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_7b",(26f,20.5f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:4,unitsPerTick:.36f,travelDistance:3f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_7a")));
            // Shrink_8 narrows from both edges over 180 ticks, 20 after a touch, over its well (Spikes_8, hidden, on the well's
            // floor; a shrinker can't start a chain, so they fire on their own cut, the well's column up to the ceiling, which
            // a cat crosses stepping onto the shrinker).
            elements.Add(E(SoloRoomElementKind.ShrinkingFloor,"Shrink_8",(14.5f,TopY - .25f),(5f,.5f),settings:new SoloRoomTrapSettings(new ShrinkSettings(180,0f,ShrinkFrom.Both),new SoloRoomTrapSettings(delayTicks:20))));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_8",(14.5f,BasinY + .15f),(5f,.3f),(14.5f,(BasinY + CeilingY) * .5f),(5f,CeilingY - BasinY),
                new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_9",(11.5f,TopY - .15f),(1f,.3f),settings:Geyser(150, 85)));
            // Retreat_10: the cut on the Loft at x 4.8-5.2 backs the door east off the Loft, down into G_9's column, over 10
            // ticks, to y 18.3-19.8: above a jump from the vent (18.17), below every walk or run off the Loft's end where it
            // crosses the column, inside the eruption's reach. The Loft is above that reach, so G_9 never puts a cat on it.
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat_10",(5f,(LoftY + CeilingY) * .5f),(.4f,CeilingY - LoftY),settings:new SoloRoomTrapSettings(moveTicks:10,offset:new Vector2(4f,19.05f - LoftY - .75f))));

            var sections = new[] {
                CheckpointSection.Start("Out", new Vector2(16.2f, MidY), new[] { "Arrow_1", "Gate_1", "Push_3", "Spikes_2", "Collapse_2", "Spear_4a", "Spear_4b", "Spear_4c" }),
                new CheckpointSection("Back", new Vector2(27.6f, StepTop(4)), new Rect(27f, StepTop(4) - .6f, 4f, .2f),
                    new[] { "V_5", "Inv_6", "Cloud", "Block_7a", "Block_7b", "Shrink_8", "Spikes_8", "G_9", "V_L" }),
                new CheckpointSection("Last", new Vector2(3f, LoftY), new Rect(3.4f, LoftY, .2f, 2f), new[] { "Retreat_10" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static SoloRoomTrapSettings Chain(string source, int delay) => new(delayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: source);

        static SoloRoomTrapSettings Geyser(int period, int erupt) =>
            new(new GeyserSettings(), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: ((erupt - GeyserMath.DefaultTellTicks) % period + period) % period));

        static SoloRoomElement Spear(string name, float x, float laneY, ArrowDirection direction, float laneEndX, (float, float) trigger, (float, float) triggerSize, SoloRoomTrapSettings timing) =>
            E(SoloRoomElementKind.Arrow, name, (x, laneY), (1f, .4f), trigger, triggerSize, new SoloRoomTrapSettings(ArrowLane.SpearLane(direction, laneY, laneEndX, disguised: true), timing));
    }
}
