using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 16, "Upside-Down Garden" (gravity flips and vines). The verb is TWO SURFACES: the Bed, one
    // slab (x 0-29, y 9-10), has faces above and below, and every stretch is open on only one of them; the cat switches
    // faces with flips, and a flip lets go of a vine. 32 x 20, a coil: east along the Bed's top, down the gap to the
    // floor, west along it, up a vine onto the underside, east along it upside down, up the gap to the ceiling, west along
    // the ceiling, down onto the Bed's top west of the Hedge in the storm and back up, to the door.
    //  - Bed (section 0): Lip_2, the Bed's east end, gives way under a cat that walks onto it (Spikes_2 where it lands): jump from
    //    before it into the gap. Landing in the Dip sends Arrow_3 over it at jump height: stand still in the Dip. Two vines
    //    lead up to the underside, each with a flip at its top: V_A's lands the cat on Tile_6, which gives way under a cat
    //    that stays onto Thorns_6 in the Bed; take V_B.
    //  - Underside (section 1, gravity up): the Bed's recesses are crossed by vines: over Recess_7 (thorns in it) the jump
    //    meets the hidden Flip_H7 (Spikes_7 on the floor): go down V_7 and leap across under it; Recess_8 is too wide to
    //    jump (Spikes_8 hidden in it): go down V_8 and leap across. East to the gap and up it.
    //  - Sky (section 2, gravity up): Thorns_A close the ceiling; Flip_S0 drops the cat onto the Bed's top west of the Hedge
    //    and sets the storm off; Flip_S2 takes it back up past the thorns; west along the ceiling to the door.
    //  - Dead ends: V_D, straight up at the door from the floor, to Spikes_X under the Bed; and west under the Bed from
    //    V_B's top, onto them too.
    static class L016Layout
    {
        const SoloRoomHazardRole Pit = SoloRoomHazardRole.OpeningBottom;
        internal const float BedBottom = 9f, BedTop = 10f, CeilingY = 19f;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,9.5f),(1f,23f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,9.5f),(1f,23f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,CeilingY + .5f),(32f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(22f,BedTop),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(2.5f,CeilingY - .75f),(.6f,1.5f)),
                // PAX-099 (D-106): the door stands on a ledge, so it never hangs in the air.
                E(SoloRoomElementKind.Floor,"Door_Ledge",(2.5f,CeilingY - 1.75f),(1f,.5f)),
                // The floor (top 0): Floor_W (x 0-29) with Post_E (x 25.8-26.2, 1.4 tall), the Dip (29-32, top -0.5).
                E(SoloRoomElementKind.Floor,"Floor_W",(14.5f,-.5f),(29f,1f)),
                E(SoloRoomElementKind.Floor,"Dip",(30.5f,-1f),(3f,1f)),
                E(SoloRoomElementKind.Wall,"Post_E",(26f,.7f),(.4f,1.4f)),
                // The Bed (y 9-10): Bed_W (x 0-7.5), Tile_6 (7.5-8.5: the underside's bottom layer, Thorns_6 over it inside the
                // Bed), Bed_A (8.5-10), Recess_7 (10-12: only its top half, with Thorns_7 hanging in it), Bed_B (12-15), Recess_8
                // (15-20: its top half), Bed_E (20-26.5), Lip_2 (26.5-29); the gap (29-32) joins the floor to the ceiling. The
                // Hedge (x 19.5-20, 3 tall) on the Bed's top parts the start's stretch from the storm's.
                E(SoloRoomElementKind.Floor,"Bed_W",(3.75f,9.5f),(7.5f,1f)),
                E(SoloRoomElementKind.Floor,"Bed_6",(8f,9.75f),(1f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_6",(8f,9.425f),(1f,.15f)),
                E(SoloRoomElementKind.Floor,"Bed_A",(9.25f,9.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Floor,"Bed_7",(11f,9.75f),(2f,.5f)),
                E(SoloRoomElementKind.Hazard,"Thorns_7",(11f,9.35f),(2f,.3f)),
                E(SoloRoomElementKind.Floor,"Bed_B",(13.5f,9.5f),(3f,1f)),
                E(SoloRoomElementKind.Floor,"Bed_8",(17.5f,9.75f),(5f,.5f)),
                E(SoloRoomElementKind.Floor,"Bed_E",(23.25f,9.5f),(6.5f,1f)),
                E(SoloRoomElementKind.Wall,"Hedge",(19.75f,BedTop + 1.5f),(.5f,3f)),
                // Thorns_A on the ceiling between Flip_S2 and Flip_S0 (x 13.5-17).
                E(SoloRoomElementKind.Hazard,"Thorns_A",(15.25f,CeilingY - .15f),(3.5f,.3f)),
                // The vines, all from 1.5 u over the floor: V_B and V_A to just under the Bed, V_D, V_7 and V_8 up to its underside.
                E(SoloRoomElementKind.Vine,"V_D",(.8f,5.25f),(LevelLayoutValidator.VineWidth,7.5f)),
                E(SoloRoomElementKind.Vine,"V_B",(3f,4.9f),(LevelLayoutValidator.VineWidth,6.8f)),
                E(SoloRoomElementKind.Vine,"V_A",(8f,4.9f),(LevelLayoutValidator.VineWidth,6.8f)),
                E(SoloRoomElementKind.Vine,"V_7",(9.7f,5.25f),(LevelLayoutValidator.VineWidth,7.5f)),
                E(SoloRoomElementKind.Vine,"V_8",(14.7f,5.25f),(LevelLayoutValidator.VineWidth,7.5f)),
            };

            var up = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp,rearmOnExit:true,rendererEnabled:true);

            // Bed. Lip_2 gives way 8 ticks after a touch and brings up Spikes_2 in the Dip's east end (x 31-32), where a cat that
            // walks off the Bed's end lands; one that jumps from before the lip lands short of them.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Lip_2",(27.75f,9.5f),(2.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:8)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_2",(31.5f,-.35f),(1f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Lip_2")));
            // The gap's column (x 29-32, floor to ceiling) sends Arrow_3, 41 ticks later, from Post_E east at jump height over the
            // Dip into Wall_R: over a cat standing in the Dip, through one jumping out of it.
            // T3b (PAX-101, D-106): the Bed's underside shoots at the floor. The honest launcher sits in Bed_E's west face under
            // Bed_8's end (x 20, y 9-9.5, the step of Recess_8), embedded in the Bed so nothing hangs in the underside walk; it fires
            // down-left at -45 degrees when the cat, walking the floor west, reaches x 16.8 (its cut spans the floor band, 0-9.5),
            // and the arrow stops in the floor at x 10.8, where a cat that walks on is (ticks 44-47 of the fire). Stop at once, let
            // it land, walk on through it.
            // D-119 (the developer: all launchers shoot non-stop): Arrow_U keeps its first shot's trigger, then fires every 190 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_U",(20.25f,9.2f),(.5f,.4f),(16.05f,4.75f),(.5f,9.5f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,9.2f,10.8f,angleDegrees:-45f),new SoloRoomTrapSettings(delayTicks:0,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:190))));
            // D-119 (the developer: all launchers shoot non-stop): Arrow_3 keeps its first shot's trigger, then fires every 240 ticks (Continuous).
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_3",(26f,1.2f),(.4f,.4f),(30.5f,(CeilingY - .5f) * .5f),(3f,CeilingY + .5f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,1.2f,32f,unitsPerTick:.36f,tellTicks:8,disguised:true),new SoloRoomTrapSettings(delayTicks:41,repeatMode:TrapRepeatMode.Continuous,cooldownTicks:240))));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_VA",(8f,7.6f),(1f,1f),settings:up));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_VB",(3f,7.6f),(1f,1f),settings:up));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Tile_6",(8f,9.175f),(1f,.35f),settings:new SoloRoomTrapSettings(delayTicks:20)));
            // Dead ends: Spikes_X on the underside by the west wall (x 0-0.8), set off by the column x 0.5-2.4 (floor to Bed).
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_X",(.4f,BedBottom - .15f),(.8f,.3f),(1.45f,BedBottom * .5f),(1.9f,BedBottom),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Underside. Flip_H7, hidden, hangs where a jump over Recess_7 goes; it sets off Spikes_7 on the floor under it and
            // beyond (a flipped cat drifts on while it falls). Spikes_8, hidden in Recess_8, come up on a cat in it.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_H7",(11f,7.6f),(1.4f,1f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_7",(13.75f,.15f),(6.5f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Flip_H7")));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_8",(17.5f,9.35f),(5f,.3f),(17.5f,4.75f),(5f,9.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Sky. Lip_2 giving way (under the cat leaving the underside for the gap) brings up Spikes_C on the ceiling (x
            // 22.75-23.75) 30 ticks later, in view as it lands: jump them.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C",(23.25f,CeilingY - .15f),(1f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:30,triggerSource:TrapTriggerSource.Chain,chainSource:"Lip_2")));
            // Flip_S0 (Once), on the ceiling east of Thorns_A, drops the cat onto the Bed's top west of the Hedge and sets
            // the storm off: Spikes_H on the Hedge's west face (+20), Spikes_S1 ahead (+30; down again 60 ticks later), Spikes_S4
            // under Bed_B (+40), Block_S3 out of the ceiling onto the landing (+45), Spikes_S5 on the ceiling past Flip_S2 (+50,
            // for good). Flip_S2 takes the cat back up.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_S0",(17.5f,CeilingY - 1f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceDown,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_H",(19.35f,BedTop + 1.5f),(.3f,3f),settings:Storm(20)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_S1",(14.25f,BedTop + .15f),(1.5f,.3f),
                settings:new SoloRoomTrapSettings(revealDelayTicks:30,triggerSource:TrapTriggerSource.Chain,chainSource:"Flip_S0",repeatMode:TrapRepeatMode.Rearm,cooldownTicks:60)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_S4",(13.5f,BedBottom - .15f),(1f,.3f),settings:Storm(40)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_S3",(18.5f,CeilingY + .5f),(1.4f,1f),
                settings:new SoloRoomTrapSettings(delayTicks:45,unitsPerTick:.37f,travelDistance:CeilingY - BedTop,triggerSource:TrapTriggerSource.Chain,chainSource:"Flip_S0")));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_S5",(8.25f,CeilingY - .15f),(1.5f,.3f),settings:Storm(50)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_S2",(11.5f,BedTop + 1f),(1f,2f),settings:up));

            var sections = new[] {
                CheckpointSection.Start("Bed", new Vector2(22f, BedTop), new[] { "Lip_2", "Spikes_2", "Arrow_3", "Arrow_U", "Flip_VA", "Flip_VB", "Tile_6", "V_A", "V_B", "V_D", "Spikes_X" }),
                new CheckpointSection("Underside", new Vector2(4f, BedBottom), new Rect(4.4f, BedBottom - 4f, .2f, 4f), new[] { "V_7", "Flip_H7", "Spikes_7", "V_8", "Spikes_8" }, gravityUp: true),
                new CheckpointSection("Sky", new Vector2(30.5f, CeilingY), new Rect(29f, 14f, 3f, .2f),
                    new[] { "Spikes_C", "Flip_S0", "Spikes_H", "Spikes_S1", "Spikes_S4", "Block_S3", "Spikes_S5", "Flip_S2" }, gravityUp: true),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // Hidden spikes that come up `delay` ticks after Flip_S0 and stay (a hidden-spike chain waits its reveal delay).
        static SoloRoomTrapSettings Storm(int delay) =>
            new(revealDelayTicks: delay, triggerSource: TrapTriggerSource.Chain, chainSource: "Flip_S0");
    }
}
