using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public enum SoloRoomElementKind { Floor, Ceiling, Wall, PitBottom, Checkpoint, Door, Hazard, CollapsingFloor, HiddenSpikes, FallingBlock, GravityFlip, DoorRetreat, MovingTrap }
    public enum SoloRoomOpeningKind { Pit, Recess }
    public enum SoloRoomHazardRole { Normal, OpeningBottom, OpeningCap, CeilingForceUpCoverage, UnjumpableFloor }
    public enum RequiredJumpKind { Pit, Hazard }
    public enum RequiredJumpFrame { Floor, Ceiling }
    public enum RequiredJumpDirection { Right, Left }

    public readonly struct SoloRoomTrapSettings
    {
        public readonly bool IsConfigured; public readonly int DelayTicks; public readonly int MoveTicks; public readonly int RevealDelayTicks;
        public readonly float UnitsPerTick; public readonly float TravelDistance; public readonly FallingBlockDirection Direction; public readonly GravityFlipMode GravityMode;
        public readonly bool RearmOnExit; public readonly bool RendererEnabled; public readonly Vector2 Offset; public readonly string TriggerName;
        public readonly TrapTriggerSource TriggerSource; public readonly string ChainSource; public readonly TrapRepeatMode RepeatMode; public readonly int CooldownTicks; public readonly int PeriodTicks; public readonly int PhaseTicks;
        public readonly MovingTrapKind MovingKind; public readonly int HoldTicks; public readonly int ReturnTicks; public readonly float CrushDepth;
        public SoloRoomTrapSettings(int delayTicks = 0, int moveTicks = 0, int revealDelayTicks = 0, float unitsPerTick = 0f, float travelDistance = 0f, FallingBlockDirection direction = FallingBlockDirection.Down, GravityFlipMode gravityMode = GravityFlipMode.Flip, bool rearmOnExit = false, bool rendererEnabled = false, Vector2 offset = default, string triggerName = "Trigger", TrapTriggerSource triggerSource = TrapTriggerSource.Overlap, string chainSource = null, TrapRepeatMode repeatMode = TrapRepeatMode.Once, int cooldownTicks = 0, int periodTicks = 1, int phaseTicks = 0, MovingTrapKind movingKind = MovingTrapKind.Hazard, int holdTicks = 0, int returnTicks = 0, float crushDepth = 0f)
        { IsConfigured = true; DelayTicks = delayTicks; MoveTicks = moveTicks; RevealDelayTicks = revealDelayTicks; UnitsPerTick = unitsPerTick; TravelDistance = travelDistance; Direction = direction; GravityMode = gravityMode; RearmOnExit = rearmOnExit; RendererEnabled = rendererEnabled; Offset = offset; TriggerName = triggerName; TriggerSource = triggerSource; ChainSource = chainSource; RepeatMode = repeatMode; CooldownTicks = cooldownTicks; PeriodTicks = periodTicks; PhaseTicks = phaseTicks; MovingKind = movingKind; HoldTicks = holdTicks; ReturnTicks = returnTicks; CrushDepth = crushDepth; }
    }

    public readonly struct SoloRoomElement
    {
        public readonly SoloRoomElementKind Kind; public readonly string Name; public readonly Vector2 Position; public readonly Vector2 Size; public readonly Vector2 SecondaryPosition; public readonly Vector2 SecondarySize; public readonly SoloRoomTrapSettings Settings; public readonly SoloRoomHazardRole HazardRole;
        public SoloRoomElement(SoloRoomElementKind kind, string name, Vector2 position, Vector2 size, Vector2 secondaryPosition, Vector2 secondarySize, SoloRoomTrapSettings settings)
            : this(kind, name, position, size, secondaryPosition, secondarySize, settings, SoloRoomHazardRole.Normal) { }
        public SoloRoomElement(SoloRoomElementKind kind, string name, Vector2 position, Vector2 size, Vector2 secondaryPosition = default, Vector2 secondarySize = default, SoloRoomTrapSettings settings = default, SoloRoomHazardRole hazardRole = SoloRoomHazardRole.Normal)
        { Kind = kind; Name = name; Position = position; Size = size; SecondaryPosition = secondaryPosition; SecondarySize = secondarySize; Settings = settings; HazardRole = hazardRole; }
    }

    public readonly struct SoloRoomOpening
    {
        public readonly SoloRoomOpeningKind Kind; public readonly float MinX; public readonly float MaxX; public readonly string LeftWallName; public readonly string RightWallName; public readonly string ClosureName; public readonly string HazardName;
        public SoloRoomOpening(SoloRoomOpeningKind kind, float minX, float maxX, string leftWallName, string rightWallName, string closureName, string hazardName)
        { Kind = kind; MinX = minX; MaxX = maxX; LeftWallName = leftWallName; RightWallName = rightWallName; ClosureName = closureName; HazardName = hazardName; }
    }

    public readonly struct RequiredJump
    {
        public readonly string ReferenceName; public readonly string SourceName; public readonly string DestinationName; public readonly RequiredJumpKind Kind; public readonly RequiredJumpFrame Frame; public readonly RequiredJumpDirection Direction;
        public readonly float TakeoffX; public readonly float LandingX; public readonly float TakeoffPawHeight; public readonly float LandingPawHeight; public readonly float Runway; public readonly float HazardHeight;
        public RequiredJump(string referenceName, RequiredJumpKind kind, RequiredJumpFrame frame, RequiredJumpDirection direction, float takeoffX, float landingX, float takeoffPawHeight, float landingPawHeight, float runway, float hazardHeight = 0f, string sourceName = null, string destinationName = null)
        { ReferenceName = referenceName; SourceName = sourceName; DestinationName = destinationName; Kind = kind; Frame = frame; Direction = direction; TakeoffX = takeoffX; LandingX = landingX; TakeoffPawHeight = takeoffPawHeight; LandingPawHeight = landingPawHeight; Runway = runway; HazardHeight = hazardHeight; }
    }
    public readonly struct RequiredStep { public readonly float Height; public RequiredStep(float height) { Height = height; } }
    public readonly struct SoloRoomDefinition
    {
        public readonly int Id; public readonly Vector2 Origin; public readonly float Width; public readonly SoloRoomElement[] Elements; public readonly SoloRoomOpening[] Openings; public readonly RequiredJump[] RequiredJumps; public readonly RequiredStep[] RequiredSteps;
        public SoloRoomDefinition(int id, float originX, float width, SoloRoomElement[] elements, SoloRoomOpening[] openings, RequiredJump[] requiredJumps)
            : this(id, originX, width, elements, openings, requiredJumps, System.Array.Empty<RequiredStep>()) { }
        public SoloRoomDefinition(int id, float originX, float width, SoloRoomElement[] elements, SoloRoomOpening[] openings, RequiredJump[] requiredJumps, RequiredStep[] requiredSteps = null) { Id = id; Origin = new Vector2(originX, 0f); Width = width; Elements = elements; Openings = openings; RequiredJumps = requiredJumps; RequiredSteps = requiredSteps ?? System.Array.Empty<RequiredStep>(); }
    }

    public static class SoloRoomsLayout
    {
        public const float FloorTop = 0f, CeilingUnderside = 7f, SurfaceThickness = 1f, ForceUpDrift = 4.1f, HiddenForceUpCoverage = 4.6f, MinimumRoomGap = 10f, RequiredJumpReachFraction = .75f, RequiredStepHeightFraction = .8f;
        public static readonly IReadOnlyList<SoloRoomDefinition> Rooms = new[]
        {
            BuildChainRoom(), BuildLiftRoom(), BuildCeilingRoom(), BuildFinalRoom()
        };

        static List<SoloRoomElement> RoomFrame(int id) => new() {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
            E(SoloRoomElementKind.Checkpoint,$"Checkpoint_{id}",(2f,0f),(0f,0f))
        };

        static void AddPit(List<SoloRoomElement> elements, int id, float minX, float maxX)
        {
            float centre = (minX + maxX) * .5f, width = maxX - minX;
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_L",(minX - .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.Wall,$"Pit{id}_R",(maxX + .5f,-2.5f),(1f,3f)));
            elements.Add(E(SoloRoomElementKind.PitBottom,$"Pit{id}_Bottom",(centre,-3.5f),(width,1f)));
            elements.Add(E(SoloRoomElementKind.Hazard,$"Pit{id}_Hazard",(centre,-2.85f),(width,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom));
        }

        static SoloRoomOpening Pit(int id, float minX, float maxX) =>
            O(SoloRoomOpeningKind.Pit,minX,maxX,$"Pit{id}_L",$"Pit{id}_R",$"Pit{id}_Bottom",$"Pit{id}_Hazard");

        static SoloRoomDefinition BuildChainRoom()
        {
            var elements = RoomFrame(0);
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(3f,-.5f),(6f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(10f,.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_C",(16f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(19f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_D",(26f,-.5f),(12f,1f)));
            // One catch basin also covers the underside of elevated Platform_B.
            AddPit(elements,1,6f,14f);
            AddPit(elements,2,18f,20f);
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(25f,.15f),(1.5f,.3f),(20f,.5f),(1f,1f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_A",(28f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:78,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_A")));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(29f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(27f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:12,moveTicks:24,offset:new Vector2(2f,0f),triggerSource:TrapTriggerSource.Chain,chainSource:"Block_A")));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,5.5f,8.5f,0f,1f,3.5f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,11.5f,14.5f,1f,0f,3f,sourceName:"Platform_B",destinationName:"Floor_C"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,17.5f,20.5f,0f,0f,2f,sourceName:"Floor_C",destinationName:"Floor_D"),
                J("Spikes_A",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23.75f,26.25f,0f,0f,2f,.3f)
            };
            return new SoloRoomDefinition(0,0f,32f,elements.ToArray(),new[] { Pit(1,6f,14f), Pit(2,18f,20f) },jumps);
        }

        static SoloRoomDefinition BuildLiftRoom()
        {
            var elements = RoomFrame(1);
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2.5f,-.5f),(5f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(8.5f,1f),(3f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_C",(14f,0f),(4f,1f)));
            // This basin catches falls beneath both raised platforms, the raised
            // Lift, the Receiver, and the collapsed slab, in every trap pose.
            AddPit(elements,1,5f,24f);
            // Trigger only once the cat is fully off Floor_C and standing on the Lift.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Lift",(18f,-.25f),(4f,.5f),(19.55f,.25f),(.3f,1f),new SoloRoomTrapSettings(offset:new Vector2(0f,1.5f),moveTicks:36,movingKind:MovingTrapKind.Solid)));
            // Its base closes the approach at the pit-wall tops; there is no cat-sized lip.
            elements.Add(E(SoloRoomElementKind.Floor,"Receiver",(21f,.25f),(2f,2.5f)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"ReceiverBlock",(20.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:50,unitsPerTick:.3f,travelDistance:4f,triggerSource:TrapTriggerSource.Chain,chainSource:"Lift")));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(23f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DLeft",(26f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DMiddle",(29.5f,-.5f),(3f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_DRight",(31.5f,-.5f),(1f,1f)));
            elements.Add(E(SoloRoomElementKind.MovingTrap,"Sweep",(30.5f,.45f),(1f,.3f),(26f,3.5f),(.5f,7f),new SoloRoomTrapSettings(offset:new Vector2(-2f,0f),moveTicks:40,holdTicks:12,returnTicks:40,movingKind:MovingTrapKind.Hazard,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:92)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(29.5f,.15f),(1f,.3f),(25.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_A",(30.5f,6f),(.75f,.75f),settings:new SoloRoomTrapSettings(delayTicks:26,unitsPerTick:.3f,travelDistance:5.625f,triggerSource:TrapTriggerSource.Chain,chainSource:"Spikes_A")));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new List<RequiredJump> {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4.5f,7.5f,0f,1.5f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.5f,12.5f,1.5f,.5f,2f,sourceName:"Platform_B",destinationName:"Floor_C"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,24.5f,1.5f,0f,1f,sourceName:"Receiver",destinationName:"Floor_DLeft"),
                J("Spikes_A",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,27.5f,31.5f,0f,0f,3f,1f,sourceName:"Floor_DLeft",destinationName:"Floor_DRight")
            };
            return new SoloRoomDefinition(1,45f,32f,elements.ToArray(),new[] { Pit(1,5f,24f) },jumps.ToArray());
        }

        static SoloRoomDefinition BuildCeilingRoom()
        {
            var elements = RoomFrame(2);
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2.25f,-.5f),(4.5f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(8.5f,1f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_Pre",(14.5f,-.5f),(3f,1f)));
            // Room 1 dropped x18-20; here that position is solid and x16-18 drops.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Collapse_C",(17f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_Safe",(25f,-.5f),(14f,1f)));
            AddPit(elements,1,4.5f,13f);
            AddPit(elements,2,16f,18f);
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(19f,3f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            // PAX-048 (D-060): shortened from 7 to 5.5 to clear a 1 u margin for the door on both
            // sides (see Door below). Still unjumpable: 5.5 exceeds both the code assertion's
            // 4.54 u threshold and the stricter ~5.3 u measured full-speed reach (see PAX-048
            // review notes).
            elements.Add(E(SoloRoomElementKind.Hazard,"FloorHazard",(22.75f,.15f),(5.5f,.3f),hazardRole:SoloRoomHazardRole.UnjumpableFloor));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"PeriodicUp",(25.5f,1f),(1f,1f),settings:new SoloRoomTrapSettings(unitsPerTick:.3f,travelDistance:5.5f,direction:FallingBlockDirection.Up,repeatMode:TrapRepeatMode.Periodic,cooldownTicks:48,periodTicks:96,phaseTicks:24)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"CeilingSpikes",(27.5f,6.85f),(1.5f,.3f),(24f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            // Flip_B begins beyond the ceiling jump's landing footprint.
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_B",(30.5f,4f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            // PAX-048 (D-060): moved from 28.5 so the retreated pose (offset unchanged, still a
            // full 1 u retreat) clears ExitSpikes by 1 u, and the authored pose clears the
            // shortened FloorHazard by 1 u. Never entered over a hazard in either pose.
            elements.Add(E(SoloRoomElementKind.Door,"Door",(26.9f,.75f),(.6f,1.5f)));
            elements.Add(E(SoloRoomElementKind.DoorRetreat,"Retreat",(29f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:12,moveTicks:24,offset:new Vector2(1f,0f),triggerSource:TrapTriggerSource.Chain,chainSource:"CeilingSpikes")));
            // A straight drop after Flip_B lands in revealed spikes; shift right to safety, then jump left to the door.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"ExitSpikes",(29.75f,.15f),(1f,.3f),(30.5f,4f),(1f,2f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,4f,7f,0f,1.5f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,10f,13.5f,1.5f,0f,3f,sourceName:"Platform_B",destinationName:"Floor_Pre"),
                J("Collapse_C",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,15.5f,18.5f,0f,0f,1f,sourceName:"Floor_Pre",destinationName:"Floor_Safe"),
                J("CeilingSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,26.25f,28.75f,0f,0f,2f,.3f),
                J("ExitSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,30.75f,28.75f,0f,0f,1f,.3f)
            };
            return new SoloRoomDefinition(2,90f,32f,elements.ToArray(),new[] { Pit(1,4.5f,13f), Pit(2,16f,18f) },jumps);
        }

        static SoloRoomDefinition BuildFinalRoom()
        {
            var elements = RoomFrame(3);
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_A",(2f,-.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_B",(8f,.5f),(4f,1f)));
            elements.Add(E(SoloRoomElementKind.Floor,"Platform_C",(14f,1.5f),(4f,1f)));
            // Includes the full x16-22 area exposed by FalseLanding at either pose.
            AddPit(elements,1,4f,22f);
            // The high trigger fires during the leap, never while a cat stands on this sliding Solid.
            elements.Add(E(SoloRoomElementKind.MovingTrap,"FalseLanding",(20.5f,-.5f),(3f,1f),(20.5f,3.5f),(1f,3f),new SoloRoomTrapSettings(offset:new Vector2(-3f,0f),moveTicks:24,movingKind:MovingTrapKind.Solid)));
            elements.Add(E(SoloRoomElementKind.Floor,"Floor_D",(27f,-.5f),(10f,1f)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"SourceSpikes",(24f,.15f),(1f,.3f),(20.5f,3.5f),(.5f,7f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_1",(26.5f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:37,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"SourceSpikes")));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_2",(29f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:25,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_A",(26.5f,3f),(.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"CeilingHiddenSpikes",(28.75f,6.85f),(1f,.3f),settings:new SoloRoomTrapSettings(revealDelayTicks:8,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2")));
            elements.Add(E(SoloRoomElementKind.GravityFlip,"Flip_B",(31.5f,4f),(1f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)));
            elements.Add(E(SoloRoomElementKind.Door,"Door",(31.5f,.75f),(.6f,1.5f)));
            var jumps = new[] {
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,3.5f,6.5f,0f,1f,3f,sourceName:"Floor_A",destinationName:"Platform_B"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9.5f,12.5f,1f,2f,3f,sourceName:"Platform_B",destinationName:"Platform_C"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,15.5f,19.5f,2f,0f,3f,sourceName:"Platform_C",destinationName:"FalseLanding"),
                J("Pit1_Bottom",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,21.5f,22.5f,0f,0f,1f,sourceName:"FalseLanding",destinationName:"Floor_D"),
                J("SourceSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,23f,25f,0f,0f,2f,.3f),
                J("CeilingHiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,27.75f,29.75f,0f,0f,2f,.3f)
            };
            return new SoloRoomDefinition(3,135f,32f,elements.ToArray(),new[] { Pit(1,4f,22f) },jumps);
        }

        static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x, float y) p, (float x, float y) s, (float x, float y) p2 = default, (float x, float y) s2 = default, SoloRoomTrapSettings settings = default, SoloRoomHazardRole hazardRole = SoloRoomHazardRole.Normal) => new(kind,name,new Vector2(p.x,p.y),new Vector2(s.x,s.y),new Vector2(p2.x,p2.y),new Vector2(s2.x,s2.y),settings,hazardRole);
        static SoloRoomOpening O(SoloRoomOpeningKind kind, float minX, float maxX, string left, string right, string closure, string hazard) => new(kind,minX,maxX,left,right,closure,hazard);
        static RequiredJump J(string reference, RequiredJumpKind kind, RequiredJumpFrame frame, RequiredJumpDirection direction, float takeoffX, float landingX, float takeoffHeight, float landingHeight, float runway, float hazardHeight = 0f, string sourceName = null, string destinationName = null) => new(reference,kind,frame,direction,takeoffX,landingX,takeoffHeight,landingHeight,runway,hazardHeight,sourceName,destinationName);
        static RequiredStep S(float height) => new(height);
    }
}
