using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-183 + D-189 (the developer, 2026-10-24: "the platforms can move slightly slower from left to right and we can have arrows
    // shooting from every direction, ceilings that fall with spikes on it. But in terms of the idea and the layout I do approve"):
    // level 30, "The Elevator". D-254 (the developer, 2026-10-29: "Cat looks like it is sliding" on Lift_H, "I want more traps more
    // things that kill you"): Lift_H rides slower than the cat walks (no moonwalk), the ride's spinners are gone (repeated from
    // L021-L024) for more slams and arrows, and a third part, Summit, runs east from the shaft's top. The verb is still RIDE, then RUN.
    //  - Ride (section 0, across the spiked lake under the corridor's low roof). D-257 (the developer: "I want multiplatforms moving
    //    around on the same lake… one has to switch platforms… no checkpoints, Make some platforms a bit higher make others a bit
    //    lower, add more arrows coming from both directions… I want the lake chasm to be long"): one 58 u spiked lake (x 4-62), no
    //    checkpoint in it (its section's cap raised, as L023's), crossed on platforms that shuttle back and forth over their own
    //    stretches on one beat, all the time, low (top 1), level with the shores (2) or high (3); neighbours meet end to end, one
    //    going out as the other comes back: switch at each meeting. Arrows fly at the low and the high platforms' shin height
    //    from both ends (a cat on the other heights is under or over them). D-259 (the developer: "more platforms and that the
    //    platforms are the exact size of the length of the cat. So the player needs to constantly jump platforms and time it
    //    perfectly in order to evade traps. Also remove the spikes coming down from the ceiling replace with lasers"): fourteen
    //    platforms, Lift_H and Raft_1-13, each the cat's length, so the cat hops about every 1.5 s; lasers (aiming turrets in the
    //    roof) over five platforms' ends lock on as the cat gets there: jump on. The arrows stay (the developer, asked).
    //  - Shaft (up; D-261: in section 0, no checkpoint of its own): Lift_V1 climbs (D-258: no thorns out of the walls beside it); Spikes_V slams down the lane soon after it
    //    stops; the alcove beside the stop is Fake_V over spikes; Lift_V2 rises in the next lane and Spikes_D slams onto it: step
    //    west onto Lift_P, up and over to Ledge_D, under Arrow_D's lane (it shoots straight down on a beat).
    //  - Summit (section 1, east along a 4.5 u stone corridor, no lifts): Hinge_S stands up spiked behind the cat and chases it to
    //    Pit_S1; across it Crumble_S gives way under a cat that stops where it lands; Block_1-3 drop from the ceiling onto a cat that
    //    slows under them; Inv_S, hidden over Pit_S2, turns the stick round in mid-leap (a cat still holding the way on falls
    //    short: let go or press back); past it Ceiling_F comes down over the last 12 u and the door slides back 5.5 u as the cat
    //    comes: chase it before the ceiling meets the floor.
    static class L030Layout
    {
        internal const float Ride = 2f, CorridorRoof = 6f, RoofTop = 8f, Base = -2f, Roof = 32f;
        internal const float Stop1 = 12f, LedgeM = 20f, LedgeD = 26f;
        // D-256: the lake's second leg puts the landing, the shaft and Summit East further east (D-257: one lake, x 4-62).
        internal const float East = 30f;
        internal const float Width = 80f + East;
        // D-257: the lake, x LiftHome-LandingE, its spikes ChasmSpikes tall.
        internal const float LiftHome = 4f, LandingE = 62f, ChasmSpikes = .6f;
        // The platforms, west to east (Lift_H, then Raft_1-6): widths and tops (low 1, the shores' 2, high 3: an arrow at one
        // height's shins passes under a cat a height up and over one a height down). Each shuttles RaftTravel east of its home and
        // back at RaftSpeed (0.04 u a tick, 2 u/s: under a walk, D-254), holding RaftHold at its east end and resting RaftRest at
        // home (D-056's slack: a cat crosses it from rest with 12 ticks to spare); the odd ones leave as the even ones' hold ends,
        // so neighbours meet end to end (RaftGap apart) once a beat, each way. The travel fills the lake.
        // D-259: fourteen, each the cat's length (its body, 1 u); up or down one height, or down two, from each to the next.
        internal static readonly float[] RaftTop = { Ride, 3f, 1f, Ride, 3f, Ride, 1f, Ride, 3f, 1f, Ride, 3f, Ride, 1f };
        internal static readonly string[] RaftNames = Enumerable.Range(0, RaftTop.Length).Select(k => k == 0 ? "Lift_H" : $"Raft_{k}").ToArray();
        internal static readonly float[] RaftW = RaftTop.Select(_ => 1f).ToArray();
        internal const float RaftGap = 1.4f, RaftSpeed = .04f;
        internal static readonly float RaftTravel = (LandingE - LiftHome - RaftW.Sum() - RaftW.Length * RaftGap) / RaftW.Length;
        internal static readonly int RaftTicks = Mathf.RoundToInt(RaftTravel / RaftSpeed);
        // D-259: RaftHold long enough that the next one is still home when a cat that jumped as this one arrived lands (a drop
        // from the high ones takes 42 ticks).
        internal const int RaftHold = 48, RaftRest = 48, Beat0 = 60;
        internal static readonly int RaftPeriod = 2 * RaftTicks + RaftHold + RaftRest;
        internal static float RaftHome(int k) { float x = LiftHome; for (int i = 0; i < k; i++) x += RaftTravel + RaftW[i] + RaftGap; return x; }
        // The even ones leave at Beat0 (the cat has walked onto Lift_H), the odd ones as the even ones' hold ends.
        internal static int RaftPhase(int k) => Beat0 + (k % 2 == 0 ? 0 : RaftTicks + RaftHold);
        // Platform k's west edge at room tick `tick`.
        internal static float RaftAt(int k, int tick) => RaftHome(k) + (tick < RaftPhase(k) ? 0f
            : RaftTravel * TrapPath.Progress((tick - RaftPhase(k)) % RaftPeriod, RaftTicks, RaftHold, RaftTicks));
        // D-259: the tick platform k's rider takes off for the next on the solution's beat (a tick after k reaches its east end).
        internal static int HopTick(int k) => RaftPhase(k) + k / 2 * RaftPeriod + RaftTicks + 1;
        // The arrows' lanes: the low and the high platforms' shin height.
        internal const float LowLane = 1.3f, HighLane = 3.3f, LintelX = 64.5f, LintelY = 3f;
        // Lift_V1: up Stop1 - Ride over V1Ticks; Spikes_V lands V1Ticks + SpikesVGrace after it leaves. Lift_V2: home at Stop1 in the
        // next lane, rising to LedgeM over V2Up V2Delay after a cat stands on it; Spikes_D lands V2Up + SpikesDGrace after it leaves.
        // D-260 (the developer, of Spikes_V and Spikes_D: "make these traps come faster"): down at twice the speed (was 30 and 20
        // ticks). D-261 ("need to start coming down before the elevator reaches the floor, forcing the player to jump to evade it"):
        // each starts down SpikesVMove/SpikesDMove - Grace ticks before its lift arrives and lands Grace after: jump for the next
        // platform from the lift on its way up.
        internal const int ShaftDelay = 20, V1Ticks = 200, SpikesVGrace = 2, SpikesVMove = 14;
        // D-261: Lift_V2 waits V2Delay 40 (was 15), so a cat jumping onto it has landed before it rises (one landing on it already
        // rising couldn't jump off it in the route harness); Spikes_D starts down as it arrives and lands SpikesDGrace after.
        internal const int V2Delay = 40, V2Up = 100, SpikesDGrace = 8, SpikesDMove = 10;
        // Lift_P: up and over to Ledge_D.
        internal const int PDelay = 10, PTicks = 120;
        // Arrow_D: down onto Ledge_D, the way into Summit, every DPeriod.
        internal const int DPeriod = 110, DPhase = 0;
        // Summit: floor F, ceiling C (4.5 u), from the shaft's east side (x 42) to the east wall.
        internal const float F = LedgeD, C = LedgeD + 4.5f, SummitX = 42f + East;
        internal const float HingeX = 43f + East, HingeTrigX = 46f + East, Pit1X0 = 49f + East, Pit1X1 = 50.4f + East, CrumbleX1 = 53.6f + East, PitDepth = 1.5f;
        internal static readonly float[] BlockX = { 55f + East, 57.6f + East, 60.2f + East };
        internal const float BlockW = 1.2f, Pit2X0 = 63.4f + East, Pit2X1 = 65.8f + East, FinaleX = 67f + East, DoorX = 73.5f + East, DoorRetreatX = 71f + East, DoorRide = 5.5f;
        // D-261: Ceiling_F starts FinaleDelay after a cat crosses FinaleX (about 2 u in at a run).
        internal const int FinaleDelay = 17;
        // D-256: FinaleTicks 130 (was 115): Ceiling_F is spiked now, and its teeth reach a running cat sooner. D-261 (the developer:
        // "Make this come down faster as well"): 105, starting FinaleDelay after the cat crosses FinaleX, so a running cat
        // reaches the door about 12 ticks ahead of its teeth.
        // D-260 (the developer, of Hinge_S: "MAKE THIS TRAP FASTER AS WELL"): its chase 26 ticks (was 40), faster than a run.
        internal const int HingeMove = 26, CrumbleDelay = 12, BlockDelay = 16, InvTicks = 45, FinaleTicks = 105, DoorRideTicks = 55;

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + Roof + 1f) * .5f),(1f,Roof + 1f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,Ride),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(DoorX,F + .75f),(.6f,1.5f)),
            };
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 0f, Width, Roof, Roof + 1f);

            // ---- The ground: the start (x 0-4), the lake (x 4-62, spiked; D-257: one, no Rock), the landing (62-65), Lift_V1's seat
            // (65-68), and the pillar under Lift_V2's lane (68-72, up to its home) ----
            const float E0 = East;
            Block(elements, SoloRoomElementKind.Floor, "Start", 0f, LiftHome, Base, Ride);
            // Jamb_W, the corridor's west end: the high arrows' launchers from the west, and their lanes' end from the east.
            Block(elements, SoloRoomElementKind.Wall, "Jamb_W", 0f, .5f, Ride, CorridorRoof);
            elements.Add(E(SoloRoomElementKind.PitBottom,"Chasm_Bottom",((LiftHome + LandingE) * .5f,Base + .5f),(LandingE - LiftHome,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Chasm",((LiftHome + LandingE) * .5f,Base + 1f + ChasmSpikes * .5f),(LandingE - LiftHome,ChasmSpikes)));
            Block(elements, SoloRoomElementKind.Floor, "Landing", LandingE, 35f + E0, Base, Ride);
            Block(elements, SoloRoomElementKind.Floor, "Seat_V1", 35f + E0, 38f + E0, Base, Ride - .5f);
            Block(elements, SoloRoomElementKind.Floor, "Pillar_B", 38f + E0, 42f + E0, Base, Stop1 - .5f);
            // D-257: Lintel_E, hung from the corridor's roof at its east end, a walk's head room over the landing: the high arrows'
            // launchers from the east, and their lanes' end from the west (they stop short of the shaft).
            Block(elements, SoloRoomElementKind.Wall, "Lintel_E", LintelX, 35f + E0, LintelY, CorridorRoof);

            // ---------- Ride (D-257) ----------
            // The platforms, on their beat from the room's start (periodic, no trigger), out and back along the lake.
            for (int k = 0; k < RaftNames.Length; k++)
                elements.Add(E(SoloRoomElementKind.MovingTrap,RaftNames[k],(RaftHome(k) + RaftW[k] * .5f,RaftTop[k] - .25f),(RaftW[k],.5f),settings:new SoloRoomTrapSettings(
                    new PathSettings(new[] { new Vector2(RaftTravel, 0f) }),
                    new(new MovingFloorSettings(SurfaceMotion.Carry),
                        new SoloRoomTrapSettings(moveTicks:RaftTicks,holdTicks:RaftHold,returnTicks:RaftTicks,repeatMode:TrapRepeatMode.Periodic,periodTicks:RaftPeriod,
                            phaseTicks:RaftPhase(k),cooldownTicks:2 * RaftTicks + RaftHold,movingKind:MovingTrapKind.Solid)))));

            // D-259 (the developer: "remove the spikes coming down from the ceiling replace with lasers"): the corridor's roof is
            // whole again (y 6-8, x 0-65; solid above it to the roof, x 0-62), and lasers hang under it: aiming turrets, each straight
            // over platform k's east end, watching only a cat standing there. Each locks (its tell) two ticks before the hop and fires
            // twelve after it locks: jump for the next platform as it locks; a cat still there is shot.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_C", 0f, 35f + E0, CorridorRoof, RoofTop);
            Block(elements, SoloRoomElementKind.Floor, "Fill", 0f, 32f + E0, RoofTop, Roof);
            foreach (int k in LaserAt)
            {
                float x = RaftHome(k) + RaftTravel + RaftW[k] * .5f, muzzle = CorridorRoof - .15f;
                var zone = new Rect(-.8f, RaftTop[k] - muzzle, 1.6f, 1.2f);
                int phase = ((HopTick(k) - 2 - LaserAim) % RaftPeriod + RaftPeriod) % RaftPeriod;
                elements.Add(E(SoloRoomElementKind.Turret,$"Laser_{k}",(x,muzzle),(.6f,.6f),settings:new SoloRoomTrapSettings(
                    new TurretSettings(LaserAim, LaserLock, LaserFire, ceilingMount: true, aimZone: zone),
                    new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:RaftPeriod,phaseTicks:phase,cooldownTicks:LaserAim + LaserLock + LaserFire))));
            }
            // Arrows, every other beat, at the low platforms' shins (out of the start's and the landing's faces, under the shores'
            // tops) and the high ones' (out of Jamb_W and Lintel_E). D-259: timed past a cat on the beat (on the other heights or in
            // the air as they pass); they shoot one that's late.
            var arrows = new (string name, bool fromWest, float y, int phase)[] {
                ("Arrow_W1", true, LowLane, 94), ("Arrow_E1", false, LowLane, 22), ("Arrow_W2", true, LowLane, 278), ("Arrow_E2", false, LowLane, 280),
                ("Arrow_W3", true, HighLane, 35), ("Arrow_E3", false, HighLane, 165), ("Arrow_W4", true, HighLane, 215),
            };
            foreach (var (name, fromWest, y, phase) in arrows)
            {
                bool low = y < Ride;
                float launcher = fromWest ? (low ? LiftHome - .25f : .25f) : (low ? LandingE + .25f : LintelX + .25f);
                float end = fromWest ? (low ? LandingE : LintelX) : (low ? LiftHome : .5f);
                elements.Add(E(SoloRoomElementKind.Arrow,name,(launcher,y),(.5f,.4f),settings:new SoloRoomTrapSettings(
                    new ArrowLane(fromWest ? ArrowDirection.Right : ArrowDirection.Left,y,end,unitsPerTick:.3f,tellTicks:10),
                    new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:2 * RaftPeriod,phaseTicks:phase,cooldownTicks:ArrowCooldown))));
            }

            // ---- The shaft's west side: Wall_V0, the alcove (x 63.5-65, floor at Stop1: Fake_V over a spiked pit), Ledge_M over it ----
            Block(elements, SoloRoomElementKind.Floor, "Wall_V0", 32f + E0, 33.5f + E0, RoofTop, Stop1 + 2.5f);
            // D-261: its spikes AlcoveFloor, 3 under the step (were just under it), so a cat that falls through dies clear of
            // Spikes_V coming down beside it.
            const float AlcoveFloor = Stop1 - 3f;
            elements.Add(E(SoloRoomElementKind.PitBottom,"Alcove_Bottom",(34.25f + E0,(RoofTop + AlcoveFloor) * .5f),(1.5f,AlcoveFloor - RoofTop)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Alcove_Spikes",(34.25f + E0,AlcoveFloor + .15f),(1.5f,.3f)));
            // D-261: down to the pit's floor, over the spikes (a trap floor draws over what it hides): it reads as a solid step.
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Fake_V",(34.25f + E0,(Stop1 + AlcoveFloor) * .5f),(1.5f,Stop1 - AlcoveFloor)));
            Block(elements, SoloRoomElementKind.Floor, "Ledge_M", 32f + E0, 35f + E0, Stop1 + 2.5f, LedgeM);
            // Ledge_D, Summit's threshold, over Lift_V2's whole lane (D-255: Spikes_D, under it, stuck out 1 u past its end, in the air).
            Block(elements, SoloRoomElementKind.Floor, "Ledge_D", 38f + E0, SummitX, LedgeD - 1f, LedgeD);

            // ---------- Shaft ----------
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_V1",(36.5f + E0,Ride - .25f),(3f,.5f),(36.5f + E0,(Ride + Roof) * .5f),(3f,Roof - Ride),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:ShaftDelay,offset:new Vector2(0f,Stop1 - Ride),moveTicks:V1Ticks,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // D-258 (the developer, of Jut_W and Jut_E: "I dont like the spikes here. Remove them"): no thorns out of the shaft's walls.
            // Spikes_V, a spiked block under Lift_P's home, slams down the lane onto the lift as it stops (D-261: it starts down as the
            // lift comes up), and goes back up.
            // D-261: 2.8 wide, short of the lane's east 0.2, clear of a cat on Lift_V2's west end (any cat on Lift_V1 is under it).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Spikes_V",(36.4f + E0,LedgeM - .8f),(2.8f,.6f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:V1Ticks + SpikesVGrace - SpikesVMove,offset:new Vector2(0f,Stop1 + .4f - (LedgeM - 1.1f)),moveTicks:SpikesVMove,holdTicks:30,returnTicks:90,
                    triggerSource:TrapTriggerSource.Chain,chainSource:"Lift_V1",movingKind:MovingTrapKind.Solid))));
            // Lift_V2, home on Pillar_B, rises V2Delay after a cat stands on it, level with Ledge_M, and stays; Spikes_D, a spiked block
            // under Ledge_D, slams down onto it as it stops (D-261: starting down as it comes up).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_V2",(39.5f + E0,Stop1 - .25f),(3f,.5f),(39.5f + E0,(Stop1 + Roof) * .5f),(3f,Roof - Stop1),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry),
                new SoloRoomTrapSettings(delayTicks:V2Delay,offset:new Vector2(0f,LedgeM - Stop1),moveTicks:V2Up,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // D-261: 2.6 wide, short of Lift_V2's west 0.4, so a cat that has jumped onto Lift_P's east end is clear of it (any cat on
            // Lift_V2 is under it).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Spikes_D",(39.7f + E0,LedgeD - 1.3f),(2.6f,.6f),settings:new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:V2Up + SpikesDGrace - SpikesDMove,offset:new Vector2(0f,LedgeM + .4f - (LedgeD - 1.6f)),moveTicks:SpikesDMove,holdTicks:30,returnTicks:60,
                    triggerSource:TrapTriggerSource.Chain,chainSource:"Lift_V2",movingKind:MovingTrapKind.Solid))));
            // Arrow_D, out of the roof, straight down onto Ledge_D on its beat.
            elements.Add(E(SoloRoomElementKind.Arrow,"Arrow_D",(40.4f + E0,Roof + .25f),(.5f,.5f),settings:new SoloRoomTrapSettings(
                new ArrowLane(ArrowDirection.Right,Roof,40.4f + E0,angleDegrees:-90f,laneEndY:LedgeD),
                new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:DPeriod,phaseTicks:DPhase,cooldownTicks:60))));
            // Lift_P, home flush with Ledge_M in Lift_V1's lane, carries a cat that steps onto it up 6, beside Ledge_D's end (D-255: it
            // went up and over 1 to a shorter Ledge_D).
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift_P",(36.5f + E0,LedgeM - .25f),(3f,.5f),(36.5f + E0,LedgeM + .3f),(3f,.6f),new SoloRoomTrapSettings(
                new PathSettings(new[] { new Vector2(0f, LedgeD - LedgeM) }),
                new(new MovingFloorSettings(SurfaceMotion.Carry),
                    new SoloRoomTrapSettings(delayTicks:PDelay,moveTicks:PTicks,holdTicks:600,movingKind:MovingTrapKind.Solid)))));

            // ---------- Summit ----------
            // The corridor's floor: solid from the base, cut by Hinge_S's seat and the two pits; its ceiling: solid up to the roof,
            // recessed for Block_1-3 and Ceiling_F.
            float under = F - 1f, pitFloor = F - PitDepth;
            Block(elements, SoloRoomElementKind.Floor, "Sum_A", SummitX, HingeX, Base, F);
            Block(elements, SoloRoomElementKind.Floor, "Sum_Seat", HingeX, HingeX + 1.5f, Base, under);
            Block(elements, SoloRoomElementKind.Floor, "Sum_B", HingeX + 1.5f, Pit1X0, Base, F);
            Block(elements, SoloRoomElementKind.Floor, "Sum_C", CrumbleX1, Pit2X0, Base, F);
            Block(elements, SoloRoomElementKind.Floor, "Sum_D", Pit2X1, Width, Base, F);
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_S1",((Pit1X0 + CrumbleX1) * .5f,(Base + pitFloor) * .5f),(CrumbleX1 - Pit1X0,pitFloor - Base)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Pit_S1_Spikes",((Pit1X0 + CrumbleX1) * .5f,pitFloor + .15f),(CrumbleX1 - Pit1X0,.3f)));
            elements.Add(E(SoloRoomElementKind.PitBottom,"Pit_S2",((Pit2X0 + Pit2X1) * .5f,(Base + pitFloor) * .5f),(Pit2X1 - Pit2X0,pitFloor - Base)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Pit_S2_Spikes",((Pit2X0 + Pit2X1) * .5f,pitFloor + .15f),(Pit2X1 - Pit2X0,.3f)));
            // The ceiling: solid up to the roof (Block_1-3 sit flush inside it), recessed half a unit for Ceiling_F.
            Block(elements, SoloRoomElementKind.Ceiling, "Sum_Roof", SummitX, FinaleX + .5f, C, Roof);
            Block(elements, SoloRoomElementKind.Ceiling, "Sum_Cap", FinaleX + .5f, Width, C + .5f, Roof);
            // Hinge_S, Sum_A's east unit (spiked), stands up behind a cat past HingeTrigX and slides HingeMove ticks east to Pit_S1's
            // edge: a cat that dawdles is caught. Hinged at its east end, so its spikes face the cat it chases.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Hinge_S",(HingeX + .75f,F - .5f),(1.5f,1f),(HingeTrigX + .2f,(F + C) * .5f),(.4f,C - F),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Slip,pushes:true,hingeTicks:3,hingeAtRight:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:4,offset:new Vector2(Pit1X0 - (HingeX + 2.5f),0f),moveTicks:HingeMove,holdTicks:600,movingKind:MovingTrapKind.Solid))));
            // Crumble_S, the far side of Pit_S1, fills its part of the pit to the bottom, over its spikes (D-255: they showed through the gap under
            // it), and gives way CrumbleDelay after a cat lands: keep running.
            float crumbleBottom = pitFloor;   // over the spikes there too (a trap floor draws over what it hides)
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumble_S",((Pit1X1 + CrumbleX1) * .5f,(F + crumbleBottom) * .5f),(CrumbleX1 - Pit1X1,F - crumbleBottom),settings:new SoloRoomTrapSettings(delayTicks:CrumbleDelay)));
            // Block_1-3 drop BlockDelay after a cat's front reaches 0.4 short of each: one running at full speed is past.
            for (int i = 0; i < BlockX.Length; i++)
                elements.Add(E(SoloRoomElementKind.FallingBlock,$"Block_{i + 1}",(BlockX[i] + BlockW * .5f,C + .5f),(BlockW,1f),(BlockX[i] - .2f,(F + C) * .5f),(.4f,C - F),
                    new SoloRoomTrapSettings(delayTicks:BlockDelay,unitsPerTick:.37f,travelDistance:C - F)));   // a last step of 0.06 u (the kill test sees a cat under it)
            // Inv_S, hidden over Pit_S2's middle at a leaping cat's height: the stick turns round for InvTicks in mid-air.
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_S",((Pit2X0 + Pit2X1) * .5f,F + 1.6f),(.4f,1.6f),settings:new SoloRoomTrapSettings(new InverterSettings(InvTicks, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));
            // D-256 (the developer: "So many straight edges"): spiked, so the stone coming down ends in teeth, not one straight cut.
            // Ceiling_F, the last 12 u of the ceiling, comes down from a cat crossing FinaleX to 0.4 over the floor over FinaleTicks; the
            // door slides DoorRide east as the cat reaches DoorRetreatX. Chase it at a run.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Ceiling_F",((FinaleX + .5f + Width) * .5f,C + .25f),(Width - FinaleX - .5f,.5f),(FinaleX + .2f,(F + C) * .5f),(.4f,C - F),new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry,slams:true,spiked:true),
                new SoloRoomTrapSettings(delayTicks:FinaleDelay,offset:new Vector2(0f,F + .4f - C),moveTicks:FinaleTicks,holdTicks:9999,movingKind:MovingTrapKind.Solid))));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(DoorRetreatX,(F + C) * .5f),(.5f,C - F),settings:new SoloRoomTrapSettings(moveTicks:DoorRideTicks,offset:new Vector2(DoorRide,0f))));

            var sections = new[] {
                // D-261 (the developer: "remove the the first checkpoint"): no Shaft checkpoint; the start's section runs to Summit.
                CheckpointSection.Start("Ride", new Vector2(2f, Ride), RaftNames.Concat(LaserAt.Select(k => $"Laser_{k}")).Concat(arrows.Select(a => a.name))
                    .Concat(new[] { "Lift_V1", "Spikes_V", "Fake_V", "Lift_V2", "Spikes_D", "Lift_P" }).ToArray()),
                new CheckpointSection("Summit", new Vector2(SummitX + .5f, F), new Rect(SummitX + .2f, F, .2f, 1.5f), new[] { "Arrow_D", "Hinge_S", "Crumble_S", "Block_1", "Block_2", "Block_3", "Inv_S", "Ceiling_F", "Retreat" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An arrow's shot (its tell and its flight the lake's length) is over before the next (every other beat).
        const int ArrowCooldown = 240;
        // D-259: the lasers (L021's turret): over which platforms' ends, and their aim, lock (the tell) and fire, once a beat (the rest
        // of the beat they rest, well over D-056's slack).
        internal static readonly int[] LaserAt = { 1, 3, 6, 9, 12 };
        internal const int LaserAim = 50, LaserLock = 12, LaserFire = 10;
    }
}
