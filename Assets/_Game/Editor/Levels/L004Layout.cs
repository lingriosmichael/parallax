using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 4, gravity. Two bands: the lower one from the ground (top 0) to a slab (underside 7, x 0-24),
    // and the upper one from the slab's top (8) to the roof (underside 15). The door hangs from the roof at the left. The
    // start is mid-room; the way runs right along the ground, up a flip through the gap past the slab's end to the roof,
    // back left upside down, down a flip to the slab's top, and up a climb of six thin platforms to the door. Every roof
    // section that gives way fills the recess behind it, so the recess's hazard stays hidden until it does.
    // PAX-103 (the developer: "Remove this inverter instead add platforms … moving traps especially sideways, spikes in the
    // bottom that kill you if you fall off the platforms. Do like 6 thin platforms, only one solution"; "invert the
    // spikes"): the flip under the door, its pads and the door's retreat are gone; spikes cover the slab's top under the
    // climb and the roof's underside over it, so the door is only reached standing.
    static class L004Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,13.5f),(1f,11f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,13.5f),(1f,11f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(9.5f,0f),(0f,0f)),
                E(SoloRoomElementKind.Floor,"Ground",(16f,-2f),(32f,4f)),
                E(SoloRoomElementKind.Floor,"Slab",(12f,7.5f),(24f,1f)),
                // The roof, broken by a section that gives way (x 16-19.5). PAX-103: x 3-6 no longer gives way (its recess went
                // with the door's retreat).
                E(SoloRoomElementKind.Ceiling,"Roof_L",(1.5f,15.5f),(3f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_5",(4.5f,15.5f),(3f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_M",(11f,15.5f),(10f,1f)),
                // PAX-102: Roof_R in two, with the rising section (Sink_R, below) between them over a new recess.
                E(SoloRoomElementKind.Ceiling,"Roof_R",(23f,15.5f),(7f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_R2",(30.25f,15.5f),(3.5f,1f)),
                E(SoloRoomElementKind.Wall,"Recess_C_L",(26.25f,18.75f),(.5f,5.5f)),
                E(SoloRoomElementKind.Wall,"Recess_C_R",(28.75f,18.75f),(.5f,5.5f)),
                E(SoloRoomElementKind.Ceiling,"Recess_C_Top",(27.5f,22f),(3f,1f)),
                // The recess behind Roof_4.
                E(SoloRoomElementKind.Wall,"Recess_B_L",(15.75f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_B_R",(19.75f,17f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_B_Top",(17.75f,18.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Recess4_Hazard",(17.75f,17.85f),(3.5f,.3f)),
                // PAX-102: the kill strip across the new recess, hidden inside Sink_R (which fills the recess, as Roof_4 does), so
                // it shows nothing until the section rises with a cat under it into it.
                E(SoloRoomElementKind.Hazard,"RecessC_Hazard",(27.5f,17.35f),(2f,.3f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // Dead end: the flip toward the door, onto spikes under the slab.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_L",(5.5f,1f),(1f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_L",(3f,6.85f),(6f,.3f),(5.5f,3.5f),(1f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T1: spikes on the ground. T2: the flip floating over the ground, onto spikes under the slab.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(15.75f,.15f),(1.5f,.3f),(13.25f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(19.25f,2.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(21.25f,6.85f),(5.5f,.3f),(19.25f,3.5f),(1.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // The way up: a flip on the ground under the gap past the slab.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_R",(30.5f,1f),(1f,2f),settings:flip));
            // T3: spikes on the roof; T4: the roof where the jump over them lands gives way under a cat that stops.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(21f,14.85f),(1f,.3f),(22.75f,11.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));   // PAX-102: trigger left of Pillar_7
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_4",(17.75f,16.5f),(3.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            // The way down: a flip floating under the roof.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D",(12.25f,12.75f),(1.5f,1.5f),settings:flip));
            // PAX-103: the climb. Six thin platforms (1 x 0.5) in a line up and to the left from the slab's top to the door at the
            // wall, each 0.82 u above the one before, so none can be skipped (two steps up is 1.64, past a jump's 1.6), and none
            // over another's jumps. Ride_2 and Ride_4 move 1.5 u left and back (Carry, Periodic, one 150-tick rhythm): hop on
            // as one comes home, ride it out, step off. Spikes cover the slab's top under the climb, and the roof's underside
            // over it (a cat that jumps on the door step meets them; the door is 7 u from the bare roof, out of an upside-down
            // cat's reach).
            elements.Add(E(SoloRoomElementKind.Hazard,"Climb_Spikes",(5.625f,8.15f),(11.25f,.3f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Roof_Spikes",(6.375f,14.85f),(12.75f,.3f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Step_1",(10.75f,8.57f),(1f,.5f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_2",(9.25f,9.39f),(1f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(-1.5f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:0,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Floor,"Step_3",(6.25f,10.21f),(1f,.5f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_4",(4.75f,11.03f),(1f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(-1.5f,0f),moveTicks:40,holdTicks:20,returnTicks:40,repeatMode:TrapRepeatMode.Periodic,periodTicks:150,phaseTicks:75,cooldownTicks:100,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.Floor,"Step_5",(1.75f,11.85f),(1f,.5f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Door_Step",(.5f,12.67f),(1f,.5f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(.5f,13.67f),(.6f,1.5f)));
            // PAX-102 (approved): the opening run's angled arrow. A corbel hangs from the slab (x 14.5-15.5, down to y 2.5); its
            // honest launcher fires down-left at -60 degrees 8 ticks after the cat sets off, onto the ground at x 12.8, ahead of a cat that
            // stops at once. Stop, let it land, walk on through it (L003's T3a, before the flips).
            elements.Add(E(SoloRoomElementKind.Wall,"Corbel_O",(15f,4.75f),(1f,4.5f)));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_O",(14.75f,2.9f),(.5f,.4f),(10.85f,3.5f),(.5f,7f),new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Left,2.9f,14.5f - 2.9f / Mathf.Tan(60f * Mathf.Deg2Rad),angleDegrees:-60f),new SoloRoomTrapSettings(delayTicks:8))));
            // PAX-102 (approved): the roof walk's rising section. Upside down on the roof, Sink_R (x 26.5-28.5) rises 3 u into the
            // new recess, 40 ticks after a cat stops under it, carrying it into RecessC_Hazard; it comes back after a hold. A cat
            // walking across is off it in about 25 ticks.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sink_R",(27.5f,16.5f),(2f,3f),(27.5f,14.72f),(2f,.56f),new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:40,offset:new Vector2(0f,3f),moveTicks:60,holdTicks:40,returnTicks:60,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:170,movingKind:MovingTrapKind.Solid))));
            // PAX-102 (approved, the developer's condition: a safe spot off the rising section): a repeating arrow up onto the roof
            // walk. A pillar stands on the slab's right end (x 23-24, up to y 13, in view of a cat on the roof); its launcher fires
            // up-right at +60 degrees (world up) every 130 ticks, with an 18-tick tell, onto the roof's underside at x 25.4,
            // between Spikes_3 and Sink_R. Its first shot comes as the cat waits on Roof_R2 (x 28.5-32, fixed): wait there,
            // then cross Sink_R and the lane together. A cat that stops in the lane can still step out of it after the tell
            // (D-097's escape).
            elements.Add(E(SoloRoomElementKind.Wall,"Pillar_7",(23.5f,10.5f),(1f,5f)));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_7",(23.75f,12.6f),(.5f,.4f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,12.6f,24f + 2.4f / Mathf.Tan(60f * Mathf.Deg2Rad),tellTicks:18,angleDegrees:60f),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:130,phaseTicks:300,cooldownTicks:60))));
            var jumps = new[] {
                J("Spikes_1",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,14.3f,17f,0f,0f,3f,.3f),
                J("Spikes_3",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Left,22.4f,20f,0f,0f,3f,.3f),
                J("Roof_4",RequiredJumpKind.Pit,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Left,17.5f,15.4f,0f,0f,1f,sourceName:"Roof_4",destinationName:"Roof_M"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),System.Array.Empty<SoloRoomOpening>(),jumps);
        }
    }
}
