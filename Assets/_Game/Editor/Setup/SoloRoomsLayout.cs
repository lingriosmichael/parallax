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
            new SoloRoomDefinition(0, 0f, 32f, new[] {
                E(SoloRoomElementKind.Floor,"Floor_Left",(3f,-.5f),(6f,1f)), E(SoloRoomElementKind.Floor,"Floor_Right",(20.5f,-.5f),(23f,1f)), E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)),
                E(SoloRoomElementKind.Wall,"PitWall_Left",(5.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"PitWall_Right",(9.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.PitBottom,"PitBottom",(7.5f,-3.5f),(3f,1f)), E(SoloRoomElementKind.Hazard,"PitHazard",(7.5f,-2.85f),(3f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom),
                E(SoloRoomElementKind.CollapsingFloor,"Collapse",(7.5f,-.5f),(3f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2f,0f),(0f,0f)), E(SoloRoomElementKind.HiddenSpikes,"HiddenSpikes",(14.5f,.15f),(2f,.3f),(10f,.5f),(2f,1f),new SoloRoomTrapSettings(revealDelayTicks:0)),
                E(SoloRoomElementKind.FallingBlock,"FallingBlock",(20.25f,6.25f),(1.5f,1.5f),(16.75f,3.5f),(.5f,7f),new SoloRoomTrapSettings(delayTicks:12,unitsPerTick:.3f,travelDistance:5.5f,direction:FallingBlockDirection.Down)), E(SoloRoomElementKind.Door,"Door",(28f,.75f),(.6f,1.5f)), E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(24.5f,3.5f),(1f,7f),settings:new SoloRoomTrapSettings(moveTicks:10,offset:new Vector2(-17.5f,0f))) },
                new[] { O(SoloRoomOpeningKind.Pit,6f,9f,"PitWall_Left","PitWall_Right","PitBottom","PitHazard") }, new[] { J("Collapse",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,5.5f,9.5f,0f,0f,3.5f,sourceName:"Floor_Left",destinationName:"Floor_Right"), J("HiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,13f,16f,0f,0f,3.5f,.3f), J("HiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Left,16f,13f,0f,0f,3f,.3f) }, new[] { S(1.5f) }),

            new SoloRoomDefinition(1, 45f, 32f, new[] {
                E(SoloRoomElementKind.Floor,"Floor",(16f,-.5f),(32f,1f)), E(SoloRoomElementKind.Ceiling,"Ceiling_Left",(9.5f,7.5f),(19f,1f)), E(SoloRoomElementKind.Ceiling,"Ceiling_Right",(27f,7.5f),(10f,1f)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_1",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.GravityFlip,"HiddenForceUp",(8f,.75f),(2f,1.5f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp)), E(SoloRoomElementKind.Hazard,"CeilingForceUpHazard",(7.8f,6.85f),(11.6f,.3f),hazardRole:SoloRoomHazardRole.CeilingForceUpCoverage), E(SoloRoomElementKind.GravityFlip,"FlipTool_A",(14.75f,3f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)),
                E(SoloRoomElementKind.Hazard,"FloorHazard",(20.5f,.15f),(6f,.3f),hazardRole:SoloRoomHazardRole.UnjumpableFloor), E(SoloRoomElementKind.Wall,"RecessWall_Left",(18.5f,9f),(1f,2f)), E(SoloRoomElementKind.Wall,"RecessWall_Right",(22.5f,9f),(1f,2f)), E(SoloRoomElementKind.Ceiling,"RecessCap",(20.5f,10.5f),(3f,1f)), E(SoloRoomElementKind.Hazard,"RecessHazard",(20.5f,9.85f),(3f,.3f),hazardRole:SoloRoomHazardRole.OpeningCap), E(SoloRoomElementKind.CollapsingFloor,"CeilingCollapse",(20.5f,7.5f),(3f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)),
                E(SoloRoomElementKind.GravityFlip,"FlipTool_B",(25.75f,4f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)), E(SoloRoomElementKind.Door,"Door",(30f,.75f),(.6f,1.5f)), E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(27.5f,3.5f),(1f,7f),settings:new SoloRoomTrapSettings(moveTicks:10,offset:new Vector2(0f,5.5f))) },
                new[] { O(SoloRoomOpeningKind.Recess,19f,22f,"RecessWall_Left","RecessWall_Right","RecessCap","RecessHazard") }, new[] { J("HiddenForceUp",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,6.5f,9.5f,0f,0f,4.5f,1.5f), J("CeilingCollapse",RequiredJumpKind.Pit,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,18.5f,22.5f,0f,0f,4.4f) }),

            new SoloRoomDefinition(2, 90f, 32f, new[] {
                E(SoloRoomElementKind.Floor,"Floor_Left",(5f,-.5f),(10f,1f)), E(SoloRoomElementKind.Floor,"Floor_Right",(22f,-.5f),(20f,1f)), E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)), E(SoloRoomElementKind.Wall,"PitWall_Left",(9.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"PitWall_Right",(12.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.PitBottom,"PitBottom",(11f,-3.5f),(2f,1f)), E(SoloRoomElementKind.Hazard,"PitHazard",(11f,-2.85f),(2f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom), E(SoloRoomElementKind.CollapsingFloor,"Collapse",(11f,-.5f),(2f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_2",(2f,0f),(0f,0f)),
                E(SoloRoomElementKind.FallingBlock,"FallingBlock_A",(8.25f,6.25f),(2.5f,1.5f),(5.25f,3.5f),(.5f,7f),new SoloRoomTrapSettings(delayTicks:12,unitsPerTick:.3f,travelDistance:5.5f,direction:FallingBlockDirection.Down)), E(SoloRoomElementKind.FallingBlock,"FallingBlock_B",(6f,6.5f),(1f,1f),(5.25f,3.5f),(.5f,7f),new SoloRoomTrapSettings(delayTicks:20,unitsPerTick:.3f,travelDistance:6f,direction:FallingBlockDirection.Down)), E(SoloRoomElementKind.HiddenSpikes,"HiddenSpikes",(17f,.15f),(2f,.3f),(12.75f,.5f),(1.5f,1f),new SoloRoomTrapSettings(revealDelayTicks:0)), E(SoloRoomElementKind.GravityFlip,"HiddenForceUp",(27f,.75f),(2f,1.5f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.ForceUp)), E(SoloRoomElementKind.Hazard,"CeilingForceUpHazard",(26f,6.85f),(12f,.3f),hazardRole:SoloRoomHazardRole.CeilingForceUpCoverage), E(SoloRoomElementKind.Door,"Door",(30.5f,.75f),(.6f,1.5f)) },
                new[] { O(SoloRoomOpeningKind.Pit,10f,12f,"PitWall_Left","PitWall_Right","PitBottom","PitHazard") }, new[] { J("Collapse",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,9f,12.5f,1.5f,0f,1.5f,sourceName:"FallingBlock_A"), J("HiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,15.5f,18.5f,0f,0f,3f,.3f), J("HiddenForceUp",RequiredJumpKind.Hazard,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,25.5f,28.5f,0f,0f,7f,1.5f) }, new[] { S(1f), S(.5f) }),

            new SoloRoomDefinition(3, 135f, 32f, new[] {
                E(SoloRoomElementKind.Floor,"Floor_Left",(4.5f,-.5f),(9f,1f)), E(SoloRoomElementKind.Floor,"Floor_Right",(22f,-.5f),(20f,1f)), E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)), E(SoloRoomElementKind.Wall,"PitWall_Left",(8.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"PitWall_Right",(12.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.PitBottom,"PitBottom",(10.5f,-3.5f),(3f,1f)), E(SoloRoomElementKind.Hazard,"PitHazard",(10.5f,-2.85f),(3f,.3f),hazardRole:SoloRoomHazardRole.OpeningBottom), E(SoloRoomElementKind.CollapsingFloor,"Collapse",(10.5f,-.5f),(3f,1f),settings:new SoloRoomTrapSettings(delayTicks:12)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_3",(2f,0f),(0f,0f)), E(SoloRoomElementKind.Door,"Door",(6f,.75f),(.6f,1.5f)), E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(4.25f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(moveTicks:10,offset:new Vector2(23f,0f))), E(SoloRoomElementKind.GravityFlip,"FlipTool_A",(14.75f,3f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)), E(SoloRoomElementKind.Hazard,"FloorHazard",(19.5f,.15f),(6f,.3f),hazardRole:SoloRoomHazardRole.UnjumpableFloor), E(SoloRoomElementKind.HiddenSpikes,"CeilingHiddenSpikes",(21f,6.85f),(2f,.3f),(17.25f,6f),(.5f,2f),new SoloRoomTrapSettings(revealDelayTicks:0)), E(SoloRoomElementKind.GravityFlip,"FlipTool_B",(25.25f,4f),(1.5f,2f),settings:new SoloRoomTrapSettings(gravityMode:GravityFlipMode.Flip,rearmOnExit:true,rendererEnabled:true)), E(SoloRoomElementKind.HiddenSpikes,"FloorHiddenSpikes",(25.5f,.15f),(3f,.3f),(25.25f,4f),(2.5f,2f),new SoloRoomTrapSettings(revealDelayTicks:0)) },
                new[] { O(SoloRoomOpeningKind.Pit,9f,12f,"PitWall_Left","PitWall_Right","PitBottom","PitHazard") }, new[] { J("Collapse",RequiredJumpKind.Pit,RequiredJumpFrame.Floor,RequiredJumpDirection.Right,8.5f,12.5f,0f,0f,6.5f), J("CeilingHiddenSpikes",RequiredJumpKind.Hazard,RequiredJumpFrame.Ceiling,RequiredJumpDirection.Right,19.5f,22.5f,0f,0f,3f,.3f) })
        };

        static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x, float y) p, (float x, float y) s, (float x, float y) p2 = default, (float x, float y) s2 = default, SoloRoomTrapSettings settings = default, SoloRoomHazardRole hazardRole = SoloRoomHazardRole.Normal) => new(kind,name,new Vector2(p.x,p.y),new Vector2(s.x,s.y),new Vector2(p2.x,p2.y),new Vector2(s2.x,s2.y),settings,hazardRole);
        static SoloRoomOpening O(SoloRoomOpeningKind kind, float minX, float maxX, string left, string right, string closure, string hazard) => new(kind,minX,maxX,left,right,closure,hazard);
        static RequiredJump J(string reference, RequiredJumpKind kind, RequiredJumpFrame frame, RequiredJumpDirection direction, float takeoffX, float landingX, float takeoffHeight, float landingHeight, float runway, float hazardHeight = 0f, string sourceName = null, string destinationName = null) => new(reference,kind,frame,direction,takeoffX,landingX,takeoffHeight,landingHeight,runway,hazardHeight,sourceName,destinationName);
        static RequiredStep S(float height) => new(height);
    }
}
