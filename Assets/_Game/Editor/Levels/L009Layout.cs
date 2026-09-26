using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 9, both ways: the flips L4 taught fire arrows. S1 (a slab, top 5, with a nook) under a roof
    // at 14 that is walked upside down. The start is at S1's left end and the door hangs from the roof right above it;
    // the way runs right along S1, up the floor flip at its right end, and back left along the roof. Each floating flip
    // hangs under a slab with hidden spikes on its underside, where a cat that jumps into it lands. The flip in front of
    // the start and the ledge behind it ("straight up to the door") are the dead ends. The roof section that gives way
    // fills the recess behind it, so the recess's hazard stays hidden until it does.
    static class L009Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3f,5f),(0f,0f)),
                // The roof, broken by a section that gives way (x 18.5-21), and the recess behind it; a stub hangs from it
                // just past that section (the end of an arrow's lane).
                E(SoloRoomElementKind.Ceiling,"Roof_L",(9.25f,14.5f),(18.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_R",(26.5f,14.5f),(11f,1f)),
                E(SoloRoomElementKind.Wall,"Recess_L",(18.25f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_R",(21.25f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_Top",(19.75f,17.5f),(3.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Recess5_Hazard",(19.75f,16.85f),(2.5f,.3f)),
                E(SoloRoomElementKind.Wall,"Stub",(22.75f,13.75f),(.5f,.5f)),
                // S1, with the nook (x 13-14.5, a step down) under the arrow's lane, and a post (an arrow's wall) to hop.
                E(SoloRoomElementKind.Floor,"S1_A",(6.5f,4.5f),(13f,1f)),
                E(SoloRoomElementKind.Floor,"Nook",(13.75f,3.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(23.25f,4.5f),(17.5f,1f)),
                E(SoloRoomElementKind.Wall,"Post_2",(7.25f,5.5f),(.5f,1f)),
                E(SoloRoomElementKind.Wall,"Pillar_A",(31.75f,5.8f),(.5f,1.6f)),
                E(SoloRoomElementKind.Wall,"Pillar_B",(31.75f,13f),(.5f,2f)),
                // The small slabs over the floating flips.
                E(SoloRoomElementKind.Floor,"Slab_D",(6.35f,10.25f),(4.7f,.5f)),
                E(SoloRoomElementKind.Floor,"Slab_1",(12.6f,10.25f),(5.2f,.5f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // T1 (L4's lure): a flip floating over S1 sends a cat that jumps into it up onto spikes under the slab above it (they
            // show as the cat comes under the slab).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_1",(11f,7.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(12.725f,9.85f),(4.95f,.3f),(12.725f,7f),(4.95f,6f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T2 (L4's answer, punished): walking under that flip sends an arrow along S1 from the post behind; the nook is
            // under its lane.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_2",(7.25f,5.3f),(.5f,.4f),(11f,7.5f),(1.5f,5f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,31.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:45))));
            // T3: spikes on S1.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(21.75f,5.15f),(1.5f,.3f),(19.75f,11f),(.5f,12f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T4: the floor flip at S1's right end is the way up; it fires an arrow along the roof from the pillar behind the
            // cat walking back upside down (jump it). The arrow stops at the stub, which the cat then hops.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_4",(29f,6f),(1f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_4",(31.75f,13.7f),(.5f,.4f),(29f,9.5f),(1f,9f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,13.7f,23f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:58))));
            // T5: the roof where the hop over the stub lands gives way under a cat that stops.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_5",(19.75f,15.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:28)));
            // Dead ends, both "straight up to the door": a flip floating in front of the start, onto spikes under the slab
            // over it; and the ledge by the wall behind the start, "the first step up", which gives way onto spikes on S1 under
            // it (its trigger is the space over it).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D1",(5f,7.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(6.475f,9.85f),(4.45f,.3f),(6.475f,7.5f),(4.45f,5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(.75f,5.15f),(1.5f,.3f),(.75f,10.125f),(1.5f,7.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_D2",(.75f,6f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D2",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(3f,13.25f),(.6f,1.5f)));
            var jumps = new[] {
                J("Spikes_3",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,20.3f,23.2f,5f,5f,3f,.3f),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),System.Array.Empty<SoloRoomOpening>(),jumps);
        }
    }
}
