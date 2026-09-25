using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 5, the walls shoot. A zigzag down three storeys: S2 (a slab, top 10) from the top right to
    // the left, down past two ledges at the left wall to S1 (top 5), right along S1 to its end, down to the ground and
    // left to the door. The arrows are disguised in walls; once fired, an arrow stays where its lane ends, so the lane is
    // plain. A fall from S1 down to the ground only kills where a collapse has armed spikes for it.
    static class L005Layout
    {
        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,14.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,12f),(1f,4f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,12f),(1f,4f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(30f,10f),(0f,0f)),
                // The arrows' walls: pillars at both ends of S2, and one at S1's left end.
                E(SoloRoomElementKind.Wall,"Pillar_L",(.25f,12f),(.5f,4f)),
                E(SoloRoomElementKind.Wall,"Pillar_R",(31.75f,12f),(.5f,4f)),
                E(SoloRoomElementKind.Wall,"Pillar_S1",(0f,7f),(1f,4f)),
                E(SoloRoomElementKind.Floor,"S2",(18.75f,9.5f),(26.5f,1f)),
                // Down at the left wall: the high ledge across a gap, and the low ledge under it, whose right end gives way.
                E(SoloRoomElementKind.Floor,"Ledge_Hi",(2.7f,9.75f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Ledge_Lo",(3f,7.8f),(3f,.5f)),
                // S1, with the nook, and a post at its right end.
                E(SoloRoomElementKind.Floor,"S1_A",(2.25f,4.5f),(4.5f,1f)),
                E(SoloRoomElementKind.Floor,"Nook",(5.5f,3.5f),(2f,1f)),
                E(SoloRoomElementKind.Floor,"S1_B",(17.25f,4.5f),(21.5f,1f)),
                E(SoloRoomElementKind.Wall,"Post",(27.75f,5.5f),(.5f,1f)),
                // The ground, with a floor that isn't there before the door, and the high ledge near the door.
                E(SoloRoomElementKind.Floor,"Ground_1",(2.1f,-2f),(4.2f,4f)),
                E(SoloRoomElementKind.Floor,"Ground_2",(18.9f,-2f),(26.2f,4f)),
                E(SoloRoomElementKind.Floor,"Ledge_D",(15f,1f),(4f,.5f)),
            };
            // T1: an arrow along S2 at shin height, out of the pillar behind the cat, once it is well along S2.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_A",(31.75f,10.3f),(.5f,.4f),(24.25f,12f),(.5f,4f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Left,10.3f,.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            // T2: an arrow across the gap to the high ledge, at jump height, when the cat reaches S2's end.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_B",(.25f,11.5f),(.5f,.4f),(6f,12f),(1f,4f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,11.5f,31.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:0))));
            // T3: the low ledge's right end gives way onto spikes in the nook and just past it. They arm only then, so a cat
            // that took the other end stands safely on them in the nook.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_Lo2",(5.25f,7.8f),(1.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:9)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3",(5.5f,4.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Ledge_Lo2",delayTicks:1)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_3b",(7.5f,5.15f),(2f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,triggerSource:TrapTriggerSource.Chain,chainSource:"Ledge_Lo2",delayTicks:1)));
            // T4 and T5: two arrows along S1 at body height, out of the pillar behind the cat, the second a while after the
            // first. The nook is under their lane; both stop at the post.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_C",(.25f,5.3f),(.5f,.4f),(3.75f,6.275f),(.5f,2.55f),
                new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:20))));
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_D",(.25f,5.3f),(.5f,.4f),
                settings:new SoloRoomTrapSettings(new ArrowLane(ArrowDirection.Right,5.3f,27.5f,unitsPerTick:.36f,disguised:true),new SoloRoomTrapSettings(delayTicks:90,triggerSource:TrapTriggerSource.Chain,chainSource:"Arrow_C"))));
            // Dead end: spikes on the high ledge near the door.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D",(13.75f,1.4f),(1.5f,.3f),(15f,2.625f),(4f,2.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // T6: T1 of level 1 again, before the door.
            L001Layout.AddShaft(elements, 6, 4.2f, 5.8f);
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Floor_6",(5f,-1.5f),(1.6f,3f)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(1.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Floor_6",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,6.4f,3.7f,0f,0f,3f,sourceName:"Ground_2",destinationName:"Ground_1"),
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { L001Layout.Shaft(6,4.2f,5.8f) },jumps);
        }
    }
}
