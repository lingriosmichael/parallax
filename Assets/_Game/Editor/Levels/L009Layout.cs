using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-059 (D-085): level 9, both ways. S1 (top 5) under a roof at 14 that is walked upside down. The start is at S1's
    // left end and the door stands on a slab right above it; the way runs right along S1, up the floor flip at its right
    // end, and back left along the roof. The flip in front of the start and the ledge behind it ("straight up to the door")
    // are the dead ends.
    // PAX-107 (the developer: "Same feedback for level 9, I see no improvements from before. The level is too easy … I think
    // level 9 doesnt need any arrow launcher and we can make it hard in itself without it"; the developer's pick: mirror
    // spikes + platform gauntlet): no launchers. Three mirror pairs: spikes on S1 and spikes on the roof right above them, on
    // one 200-tick rhythm (each up half of it), the roof's half a rhythm off the floor's, so the strip a cat waited out on the
    // way right is up when it comes back over it upside down. S1 is cut by a chasm (x 15-26.5) crossed on two lifts that rise
    // toward spikes in the roof that impale what rides them up (and an upside-down cat walking the roof over them); a hinge floor at its east
    // end pushes a cat that stops past it onto the third floor strip.
    static class L009Layout
    {
        // PAX-107: the mirror rhythm and the lifts' places in it (fire ticks; a strip is up for the first half after its fire).
        internal const int Period = 200, Half = 100;
        internal const int F1Phase = 0, F2Phase = 100, F3Phase = 130, LiftAPhase = 130, LiftBPhase = 190;

        // PAX-107: a 1.5 u spike strip on the rhythm: up from `phase` for half a period.
        static SoloRoomElement Mirror(string name, float x, float y, int phase) =>
            E(SoloRoomElementKind.HiddenSpikes,name,(x,y),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,
                repeatMode:TrapRepeatMode.Periodic,periodTicks:Period,phaseTicks:phase % Period,cooldownTicks:Half));

        // PAX-107: a 1.5 x 0.5 lift (Carry), home flush with S1's top: up 8.7 u (its top 0.3 under the roof) over 50 ticks,
        // 30 there, down over 50, every 200.
        static SoloRoomElement Lift(string name, float x, int phase) =>
            E(SoloRoomElementKind.MovingTrap,name,(x,4.75f),(1.5f,.5f),settings:new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset:new Vector2(0f,8.7f),moveTicks:50,holdTicks:30,returnTicks:50,repeatMode:TrapRepeatMode.Periodic,periodTicks:Period,phaseTicks:phase,cooldownTicks:130,movingKind:MovingTrapKind.Solid)));

        // PAX-107: the roof strip over a lift, up from 40 ticks after the lift sets off, for 50.
        static SoloRoomElement LiftSpikes(string name, float x, int liftPhase) =>
            E(SoloRoomElementKind.HiddenSpikes,name,(x,13.85f),(1.5f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:6,
                repeatMode:TrapRepeatMode.Periodic,periodTicks:Period,phaseTicks:(liftPhase + 40) % Period,cooldownTicks:50));

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,7f),(1f,14f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3f,5f),(0f,0f)),
                // The roof, broken between the lifts by a section that gives way (x 17.5-20), and the recess behind it.
                E(SoloRoomElementKind.Ceiling,"Roof_L",(8.75f,14.5f),(17.5f,1f)),
                E(SoloRoomElementKind.Ceiling,"Roof_R",(26f,14.5f),(12f,1f)),
                E(SoloRoomElementKind.Wall,"Recess_L",(17.25f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Wall,"Recess_R",(20.25f,16f),(.5f,2f)),
                E(SoloRoomElementKind.Ceiling,"Recess_Top",(18.75f,17.5f),(3.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Recess5_Hazard",(18.75f,16.85f),(2.5f,.3f)),
                // PAX-107: S1 to the chasm's edge (x 15); S1_E1, an island between Lift_B and the hinge floor; S1_E2 past the
                // hinge floor to the wall. The chasm's kill strip is 3 u under S1's top.
                E(SoloRoomElementKind.Floor,"S1_A",(7.5f,4.5f),(15f,1f)),
                E(SoloRoomElementKind.Floor,"S1_E1",(23.25f,4.5f),(1.5f,1f)),
                E(SoloRoomElementKind.Floor,"S1_E2",(29.25f,4.5f),(5.5f,1f)),
                E(SoloRoomElementKind.PitBottom,"Chasm_Bottom",(20.75f,1.5f),(11.5f,1f)),
                E(SoloRoomElementKind.Hazard,"Chasm_Hazard",(20.75f,2.15f),(11.5f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                // The small slabs over the floating flips.
                // PAX-100 item 0: Slab_D reaches back to x 1.5 (it began at 4), so a cat that jumps into Flip_D1 moving left meets
                // its spikes too, instead of rising past its end to the roof by the door (the bypass PAX-099 found).
                // PAX-102: 0.5 u higher, so the door on it is far enough from the start (D-085).
                E(SoloRoomElementKind.Floor,"Slab_D",(5.1f,10.75f),(7.2f,.5f)),
                E(SoloRoomElementKind.Floor,"Slab_1",(12.6f,10.25f),(5.2f,.5f)),
            };
            var flip = new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true);
            // T1 (L4's lure): a flip floating over S1 sends a cat that jumps into it up onto spikes under the slab above it (they
            // show as the cat comes under the slab). PAX-107: it hangs between the first two floor strips, so neither can be
            // jumped: wait them out under it (it reaches F2's west edge, x 10.25-12).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_1",(11.125f,7.8f),(1.75f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_1",(12.725f,9.85f),(4.95f,.3f),(12.725f,7f),(4.95f,6f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // PAX-107: the mirror pairs. F1 (x 8.5-10) is up from the start; F2 (x 12-13.5) takes the other half; F3 (x 28-29.5)
            // is past the hinge floor. Each roof strip is its floor strip's mirror.
            elements.Add(Mirror("Spikes_F1",9.25f,5.15f,F1Phase));
            elements.Add(Mirror("Spikes_R1",9.25f,13.85f,F1Phase + Half));
            elements.Add(Mirror("Spikes_F2",12.75f,5.15f,F2Phase));
            elements.Add(Mirror("Spikes_R2",12.75f,13.85f,F2Phase + Half));
            elements.Add(Mirror("Spikes_F3",28.75f,5.15f,F3Phase));
            elements.Add(Mirror("Spikes_R3",28.75f,13.85f,F3Phase + Half));
            // PAX-107: the chasm's lifts, Lift_A (x 16-17.5, 1 u off S1's edge) and Lift_B (x 20-21.5, 2.5 u on, 1 u short of
            // S1_E1). Each is down for 70 ticks of the 200, Lift_B 60 after Lift_A: board A as it comes down, hop to B as it
            // comes down, and off B before it rises. Neither gap past a lift can be jumped from S1.
            elements.Add(Lift("Lift_A",16.75f,LiftAPhase));
            elements.Add(Lift("Lift_B",20.75f,LiftBPhase));
            // PAX-107: and the roof strips they rise toward, each up from 40 ticks into its lift's rise for 50 (its top hold and
            // a little either side): a cat that rides a lift up is spiked on them, and so is one walking the roof over it then.
            elements.Add(LiftSpikes("Spikes_LA",16.75f,LiftAPhase));
            elements.Add(LiftSpikes("Spikes_LB",20.75f,LiftBPhase));
            // PAX-107: the hinge floor, Hinge_9 (x 24-26.5, flush between S1_E1 and S1_E2, over the chasm): a cat past it (its
            // trigger is the space over S1_E2's west end, x 26.6-28.5) sets it off; 8 ticks later it swings up about its east
            // end over 10 into a wall (x 26-26.5) and slides 2 u east over 28, pushing a cat that stopped there onto F3.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_9",(25.25f,4.75f),(2.5f,.5f),(27.55f,7f),(1.9f,4f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:10,hingeAtRight:true),
                new SoloRoomTrapSettings(delayTicks:8,offset:new Vector2(2f,0f),moveTicks:28,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // T4: the floor flip at S1's right end is the way up (PAX-107: against the wall, past F3).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_4",(31.5f,6f),(1f,2f),settings:flip));
            // T5: the roof between the lifts gives way under a cat that stops (PAX-107: where a cat waits out the lifts).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Roof_5",(18.75f,15.5f),(2.5f,3f),settings:new SoloRoomTrapSettings(delayTicks:28)));
            // Dead ends, both "straight up to the door": a flip floating in front of the start, onto spikes under the slab
            // over it; and the ledge by the wall behind the start, "the first step up", which gives way onto spikes on S1 under
            // it (its trigger is the space over it).
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_D1",(5f,7.8f),(1.5f,2f),settings:flip));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D1",(5.1f,10.35f),(7.2f,.3f),(5.1f,7.75f),(7.2f,5.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(.75f,5.15f),(1.5f,.3f),(.75f,10.125f),(1.5f,7.75f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_D2",(.75f,6f),(1.5f,.5f),settings:new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_D2",delayTicks:1)));
            // PAX-102 (the developer: "I don't want to be able to solve this level by jumping on a door while inverted"): the
            // door stands on Slab_D's top (the platform on the far left), and a flip floating under the roof over Slab_D's east
            // end (x 7.5-8.5, from the roof down to the slab) turns a cat walking the roof upside down back down before it gets
            // there: it drops onto Slab_D and walks to the door standing. The flip fills the band, so it can't be passed upside
            // down. (PAX-099's Door_Ledge under the roof is gone.)
            elements.Add(E(SoloRoomElementKind.Door,"Door",(2.3f,11.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_E",(8f,12.5f),(1f,3f),settings:flip));
            // PAX-103 (the developer: "the exit shouldn't be reachable while I am inverted. It needs inverted spikes"): spikes
            // along the roof's underside over Slab_D, from the wall to Flip_E, so a cat upside down on the roof can't get above
            // the door (a standing cat's jump on Slab_D tops out at 13.16, under their tips at 13.7).
            elements.Add(E(SoloRoomElementKind.Hazard,"Roof_Spikes",(3.75f,13.85f),(7.5f,.3f)));
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),
                new[] { O(SoloRoomOpeningKind.Pit,15f,26.5f,"S1_A","S1_E2","Chasm_Bottom","Chasm_Hazard") },System.Array.Empty<RequiredJump>());
        }
    }
}
