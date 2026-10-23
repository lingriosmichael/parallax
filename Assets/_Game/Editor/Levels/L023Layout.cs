using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-167: level 23, "Ferryman" (D-163's third). The verb is RIDE ACROSS: three storeys of spike lakes, walked east, west,
    // east, crossed on moving platforms that betray the rider, and joined by lifts.
    //  - Docks (section 0, storey 1, walked east): the cat starts on Pier_0, which gives way under it: board Ferry_1 at once,
    //    ride it out across Lake_1 and jump to Isle_1 at the far end. Ferry_2 sets off when the cat steps on it and stalls in
    //    mid-lake, where Slam_2 comes down on it: jump to Isle_2 before it does. Lift_U carries the cat up; a cat that steps
    //    into its well while it's up is crushed when it comes down.
    //  - Gallery (section 1, storey 2, walked west): Sink_3 sinks into Lake_3 10 ticks after the cat lands on it: jump straight
    //    on to Isle_3. Ride_4 shuttles across Lake_4 (ride it, step off at the far end). Lift_U2 carries the cat up.
    //  - Crown (section 2, storey 3, walked east): Ride_5a and Ride_5b shuttle toward each other across Lake_5: ride 5a out,
    //    hop onto 5b while both wait at their far ends, ride 5b home. Hinge_F, spiked, stands up behind and chases (jump
    //    Spikes_5); Slam_F comes down out of the roof ahead (stop short, hop onto it); the door.
    static class L023Layout
    {
        // The storeys' walk lines and ceilings (room-local y); every slab between them is 2 u of stone, the roof 2.
        internal const float F1 = 0f, C1 = 4.5f, F2 = 6.5f, C2 = 11f, F3 = 13f, C3 = 17.5f, RoofTop = 19.5f, Base = -2.5f;
        // The platforms: Ferry_1's shuttle (leaves home at room tick Ferry1Phase), the lifts', the rides'.
        internal const int Ferry1Period = 150, Ferry1Phase = 22, Ferry1Move = 45, Ferry1Hold = 15;
        internal const int LiftPeriod = 170, LiftUPhase = 101, LiftU2Phase = 126, LiftMove = 50, LiftHold = 30;
        internal const int Ride4Period = 140, RidePeriod = 160, Ride4Phase = 112, Ride5aPhase = 144, Ride5bPhase = Ride5aPhase - 5, RideMove = 40;
        // Ferry_2: its wait after the cat steps on, its move and its stall; Slam_2 lands on it SlamLag ticks after it sets off.
        internal const int Ferry2Delay = 10, Ferry2Move = 32, SlamLag = 70;
        // The finale's chase: Hinge_F's stand, east.
        internal const float ChaseDistance = 4f;
        internal const int ChaseDelay = 10, ChaseTicks = 36;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(3.2f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.9f,F3 + .75f),(.6f,1.5f)),
            };

            // ---- Storey 1 (y -2.5..0) ----
            // Dock_0 (x 0-2.5) and Pier_0 over Lake_1's west end; Lake_1 (x 2.5-12.5); Isle_1 (x 12.5-16); Lake_2 (x 16-23.5);
            // Isle_2 (x 23.5-28.5); Lift_U's well (x 28.5-30); Jamb_U (x 30-32), stone up to storey 2's ceiling.
            Block(elements, SoloRoomElementKind.Floor, "Dock_0", 0f, 2.5f, Base, F1);
            Lake(elements, "Lake_1", 2.5f, 12.5f, Base, Base + .5f);
            Block(elements, SoloRoomElementKind.Floor, "Isle_1", 12.5f, 16f, Base, F1);
            Lake(elements, "Lake_2", 16f, 23.5f, Base, Base + .5f);
            Block(elements, SoloRoomElementKind.Floor, "Isle_2", 23.5f, 28.5f, Base, F1);
            Block(elements, SoloRoomElementKind.Floor, "Seat_U", 28.5f, 30f, Base, F1 - .5f);
            Block(elements, SoloRoomElementKind.Wall, "Jamb_U", 30f, 32f, Base, C2);

            // ---- Slab A (y 4.5-6.5): storey 1's ceiling, storey 2's floor ----
            // Lift_U2's seat (x 0-1.5); Dock_3 (x 1.5-3.5); Lake_4 (x 3.5-9), recessed; Isle_3 (x 9-13); Lake_3 (x 13-20),
            // recessed; Cap_S2 over Slam_2 (x 20-21.5) and Dock_2 (x 21.5-28.5); Lift_U's shaft (x 28.5-30).
            Block(elements, SoloRoomElementKind.Floor, "Seat_U2", 0f, 1.5f, C1, F2 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Dock_3", 1.5f, 3.5f, C1, F2);
            Lake(elements, "Lake_4", 3.5f, 9f, C1, C1 + .5f, inSlab: true);
            Block(elements, SoloRoomElementKind.Floor, "Isle_3", 9f, 13f, C1, F2);
            Lake(elements, "Lake_3", 13f, 20f, C1, C1 + .5f, inSlab: true);
            Block(elements, SoloRoomElementKind.Floor, "Cap_S2", 20f, 21.5f, C1 + .75f, F2);
            Block(elements, SoloRoomElementKind.Floor, "Dock_2", 21.5f, 28.5f, C1, F2);

            // ---- Slab B (y 11-13): storey 2's ceiling, storey 3's floor ----
            // Lift_U2's shaft (x 0-1.5); Dock_4 (x 1.5-7); Lake_5 (x 7-18), recessed; Hinge_F on Seat_F (x 18-19.5); Isle_5.
            Block(elements, SoloRoomElementKind.Floor, "Dock_4", 1.5f, 7f, C2, F3);
            Lake(elements, "Lake_5", 7f, 18f, C2, C2 + .5f, inSlab: true);
            Block(elements, SoloRoomElementKind.Floor, "Seat_F", 18f, 19.5f, C2, F3 - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Isle_5", 19.5f, 32f, C2, F3);

            // ---- The roof (y 17.5-19.5): Slam_F is its bottom unit under Cap_F ----
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_A", 0f, 27.5f, C3, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Cap_F", 27.5f, 29f, C3 + .75f, RoofTop);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_B", 29f, 32f, C3, RoofTop);
            Spikes(elements, "Spikes_5", 22f, 23.2f, F3);

            // ---------- Docks (storey 1) ----------
            // Pier_0 fills Lake_1's mouth by the start (x 2.5-4) and gives way 30 ticks after the cat's first touch (it starts on it).
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Pier_0",(3.25f,-.5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:30)));
            // Ferry_1: home by the pier (x 4-5.5), out 5.5 u east over Ferry1Move ticks, waits Ferry1Hold, back; every Ferry1Period,
            // leaving at room tick Ferry1Phase. Its far end is 1.5 u short of Isle_1: jump.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry_1",(4.75f,-.25f),(1.5f,.5f),settings:Shuttle(5.5f, 0f, Ferry1Move, Ferry1Hold, Ferry1Move, Ferry1Period, Ferry1Phase)));
            // Ferry_2: moored by Isle_1 (x 16-17.5); Ferry2Delay ticks after the cat steps onto it, it carries the cat 4 u east and
            // stalls in mid-lake, 2 u short of Isle_2, for good.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ferry_2",(16.75f,-.25f),(1.5f,.5f),(16.75f,(F1 + C1) * .5f),(1.5f,C1 - F1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:Ferry2Delay,offset:new Vector2(4f,0f),moveTicks:Ferry2Move,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Slam_2, slab A's bottom unit at x 20-21.5 (over Ferry_2's stall), slams 4.5 u onto it SlamLag ticks after Ferry_2 sets
            // off, and stays: a cat that waits on the stalled ferry is crushed.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_2",(20.75f,C1 + .375f),(1.5f,.75f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:SlamLag,offset:new Vector2(0f,-C1),moveTicks:8,holdTicks:600,triggerSource:TrapTriggerSource.Chain,chainSource:"Ferry_2",movingKind:MovingTrapKind.Solid))));
            // Lift_U: in its well by Jamb_U (x 28.5-30), up 6.5 u to storey 2's floor and back, every LiftPeriod. A cat in the well
            // while it's up is crushed when it comes down.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_U",(29.25f,-.25f),(1.5f,.5f),settings:Shuttle(0f, F2 - F1, LiftMove, LiftHold, LiftMove, LiftPeriod, LiftUPhase)));

            // ---------- Gallery (storey 2) ----------
            // Sink_3, moored 2 u off Dock_2 (x 15-18), sinks 1.3 u onto Lake_3's bed (its spikes round the rider) 10 ticks after
            // the cat lands on it, and comes back up later: jump straight on to Isle_3.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sink_3",(16.5f,F2 - .1f),(3f,.2f),(16.5f,F2 + .4f),(3f,.8f),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:10,offset:new Vector2(0f,-1.3f),moveTicks:6,holdTicks:40,returnTicks:30,movingKind:MovingTrapKind.Solid))));
            // Ride_4: home by Isle_3 (x 7.5-9), out 3.5 u west to Dock_3's edge and back, every RidePeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_4",(8.25f,F2 - .25f),(1.5f,.5f),settings:Shuttle(-3.5f, 0f, RideMove, 20, RideMove, Ride4Period, Ride4Phase)));
            // Lift_U2: on its seat by Wall_L (x 0-1.5), up 6.5 u to storey 3's floor and back, every LiftPeriod.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_U2",(.75f,F2 - .25f),(1.5f,.5f),settings:Shuttle(0f, F3 - F2, LiftMove, LiftHold, LiftMove, LiftPeriod, LiftU2Phase)));

            // ---------- Crown (storey 3) ----------
            // Ride_5a: home by Dock_4 (x 7-8.5), out 3.5 u east, waits 30, back. Ride_5b: home by Seat_F (x 16.5-18), out 3 u west,
            // waits 40, back, setting off 5 ticks earlier: both wait at their far ends, 1.5 u apart, at the same time.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_5a",(7.75f,F3 - .25f),(1.5f,.5f),settings:Shuttle(3.5f, 0f, RideMove, 30, RideMove, RidePeriod, Ride5aPhase)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ride_5b",(17.25f,F3 - .25f),(1.5f,.5f),settings:Shuttle(-3f, 0f, RideMove, 40, RideMove, RidePeriod, Ride5bPhase)));
            // Hinge_F, slab B's top unit at x 18-19.5 (hinged at its east end, spiked, so its spikes face the cat), stands up behind
            // a cat walking east past x 21 and slides ChaseDistance east to Spikes_5's far edge: a cat that stops short of them is caught.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_F",(18.75f,F3 - .25f),(1.5f,.5f),(21f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:ChaseDelay,offset:new Vector2(ChaseDistance,0f),moveTicks:ChaseTicks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Slam_F, the roof's bottom unit at x 27.5-29, slams 4.5 u onto the floor 9 ticks after a cat walking east comes into its
            // cut (x 26.4-26.8), and stays: stop short, hop onto it, into the door.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Slam_F",(28.25f,C3 + .375f),(1.5f,.75f),(26.6f,(F3 + C3) * .5f),(.4f,C3 - F3),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true),
                new SoloRoomTrapSettings(delayTicks:9,offset:new Vector2(0f,F3 - C3),moveTicks:8,holdTicks:600,movingKind:MovingTrapKind.Solid))));

            // Each later gate lies across its lift's shaft: the only way up to the checkpoint is the ride through it.
            var sections = new[] {
                CheckpointSection.Start("Docks", new Vector2(3.2f, F1), new[] { "Pier_0", "Ferry_1", "Ferry_2", "Slam_2", "Lift_U" }),
                new CheckpointSection("Gallery", new Vector2(26.5f, F2), new Rect(28.5f, F2 - 1.1f, 1.5f, .2f),
                    new[] { "Sink_3", "Ride_4", "Lift_U2" }),
                new CheckpointSection("Crown", new Vector2(3.5f, F3), new Rect(0f, F3 - 1.1f, 1.5f, .2f),
                    new[] { "Ride_5a", "Ride_5b", "Hinge_F", "Slam_F" }),
            };
            var openings = new[] {
                O(SoloRoomOpeningKind.Pit, 2.5f, 12.5f, "Dock_0", "Isle_1", "Lake_1_Bottom", "Lake_1"),
                O(SoloRoomOpeningKind.Pit, 16f, 23.5f, "Isle_1", "Isle_2", "Lake_2_Bottom", "Lake_2"),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A spike lake: its bed from y0 to `bed`, and the spikes on it. On storey 1 it's a pit (the opening's bottom); in a slab
        // the bed is the slab's own stone (a floor: the storey below's ceiling) with honest spikes on it.
        static void Lake(List<SoloRoomElement> e, string name, float x0, float x1, float y0, float bed, bool inSlab = false)
        {
            e.Add(E(inSlab ? SoloRoomElementKind.Floor : SoloRoomElementKind.PitBottom, name + "_Bottom", ((x0 + x1) * .5f, (y0 + bed) * .5f), (x1 - x0, bed - y0)));
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, bed + .15f), (x1 - x0, .3f), hazardRole: inSlab ? SoloRoomHazardRole.Normal : SoloRoomHazardRole.OpeningBottom));
        }

        // A carrying platform that shuttles by (dx, dy) and back on a beat: out over `move` ticks, waits `hold`, back over `back`,
        // every `period` ticks, leaving home at room tick `phase`.
        static SoloRoomTrapSettings Shuttle(float dx, float dy, int move, int hold, int back, int period, int phase) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(offset: new Vector2(dx, dy), moveTicks: move, holdTicks: hold, returnTicks: back,
                    repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: move + hold + back,
                    movingKind: MovingTrapKind.Solid));
    }
}
