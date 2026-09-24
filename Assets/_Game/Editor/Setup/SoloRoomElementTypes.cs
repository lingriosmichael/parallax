using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-051 (D-066): moved verbatim out of SoloRoomsLayout.cs so level-layout classes and
    // SoloRoomsLayout can both build SoloRoomDefinitions from the same types. No field, no
    // readonly modifier, and no behaviour changed by the move.
    // PAX-080 (D-080): FakePlatform is appended so every earlier value keeps its number. It looks like a Floor
    // and isn't solid; the builder makes it a CollapsingFloorTrap with a trigger body, Overlap, Once, delay 0.
    public enum SoloRoomElementKind { Floor, Ceiling, Wall, PitBottom, Checkpoint, Door, Hazard, CollapsingFloor, HiddenSpikes, FallingBlock, GravityFlip, DoorRetreat, MovingTrap, Arrow, FakePlatform }
    public enum SoloRoomOpeningKind { Pit, Recess }
    public enum SoloRoomHazardRole { Normal, OpeningBottom, OpeningCap, CeilingForceUpCoverage, UnjumpableFloor }
    public enum RequiredJumpKind { Pit, Hazard }
    public enum RequiredJumpFrame { Floor, Ceiling }
    public enum RequiredJumpDirection { Right, Left }

    // PAX-074 (D-078): an arrow's lane. The launcher is the element's Position/Size; the lane runs
    // from the launcher's face on the Direction side (the mouth) to LaneEndX, centred on LaneY.
    public readonly struct ArrowLane
    {
        public readonly bool IsConfigured; public readonly ArrowDirection Direction; public readonly float LaneY; public readonly float LaneEndX;
        public readonly float Length; public readonly float Thickness; public readonly float UnitsPerTick; public readonly int TellTicks; public readonly bool Disguised;
        public ArrowLane(ArrowDirection direction, float laneY, float laneEndX, float length = .8f, float thickness = .16f, float unitsPerTick = .3f, int tellTicks = 6, bool disguised = false)
        { IsConfigured = true; Direction = direction; LaneY = laneY; LaneEndX = laneEndX; Length = length; Thickness = thickness; UnitsPerTick = unitsPerTick; TellTicks = tellTicks; Disguised = disguised; }
    }

    public readonly struct SoloRoomTrapSettings
    {
        public readonly bool IsConfigured; public readonly int DelayTicks; public readonly int MoveTicks; public readonly int RevealDelayTicks;
        public readonly float UnitsPerTick; public readonly float TravelDistance; public readonly FallingBlockDirection Direction; public readonly GravityFlipMode GravityMode;
        public readonly bool RearmOnExit; public readonly bool RendererEnabled; public readonly Vector2 Offset; public readonly string TriggerName;
        public readonly TrapTriggerSource TriggerSource; public readonly string ChainSource; public readonly TrapRepeatMode RepeatMode; public readonly int CooldownTicks; public readonly int PeriodTicks; public readonly int PhaseTicks;
        public readonly MovingTrapKind MovingKind; public readonly int HoldTicks; public readonly int ReturnTicks; public readonly float CrushDepth;
        // PAX-073 (D-074): null = not a learned bypass. Non-null marks a trigger meant to be avoided as the
        // learned solution; LevelLayoutValidator skips it for trigger coverage, lists it, and rejects an empty reason.
        public readonly string LearnedBypassReason;
        // PAX-074 (D-078): default (IsConfigured false) for every non-arrow element.
        public readonly ArrowLane Arrow;
        public SoloRoomTrapSettings(int delayTicks = 0, int moveTicks = 0, int revealDelayTicks = 0, float unitsPerTick = 0f, float travelDistance = 0f, FallingBlockDirection direction = FallingBlockDirection.Down, GravityFlipMode gravityMode = GravityFlipMode.Flip, bool rearmOnExit = false, bool rendererEnabled = false, Vector2 offset = default, string triggerName = "Trigger", TrapTriggerSource triggerSource = TrapTriggerSource.Overlap, string chainSource = null, TrapRepeatMode repeatMode = TrapRepeatMode.Once, int cooldownTicks = 0, int periodTicks = 1, int phaseTicks = 0, MovingTrapKind movingKind = MovingTrapKind.Hazard, int holdTicks = 0, int returnTicks = 0, float crushDepth = 0f, string learnedBypassReason = null)
        { IsConfigured = true; DelayTicks = delayTicks; MoveTicks = moveTicks; RevealDelayTicks = revealDelayTicks; UnitsPerTick = unitsPerTick; TravelDistance = travelDistance; Direction = direction; GravityMode = gravityMode; RearmOnExit = rearmOnExit; RendererEnabled = rendererEnabled; Offset = offset; TriggerName = triggerName; TriggerSource = triggerSource; ChainSource = chainSource; RepeatMode = repeatMode; CooldownTicks = cooldownTicks; PeriodTicks = periodTicks; PhaseTicks = phaseTicks; MovingKind = movingKind; HoldTicks = holdTicks; ReturnTicks = returnTicks; CrushDepth = crushDepth; LearnedBypassReason = learnedBypassReason; Arrow = default; }
        // PAX-074 (D-078): an arrow's lane on top of ordinary trigger/repeat settings. Two parameters on
        // purpose: tests that build settings by reflection pick the longest constructor, which stays the one above.
        public SoloRoomTrapSettings(ArrowLane arrow, SoloRoomTrapSettings timing) { this = timing; Arrow = arrow; }
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

    // PAX-076 (D-083): a precision section, a region in room-local coordinates (edges inclusive). Held on the room,
    // not as an element, so SoloRoomBuilder, the room bounds and the camera frame never see it.
    public readonly struct PrecisionSection
    {
        public readonly string Name; public readonly Rect Region;
        public PrecisionSection(string name, Rect region) { Name = name; Region = region; }
        public bool Contains(Vector2 p) => p.x >= Region.xMin && p.x <= Region.xMax && p.y >= Region.yMin && p.y <= Region.yMax;
        public bool Contains(Rect r) => Contains(r.min) && Contains(r.max);
    }

    // PAX-076 (D-083): a gap the validator proves nobody can cross. TakeoffX is the take-off platform's edge and
    // TargetX the target platform's near edge (edges, not RequiredJump's cat-centre points: the rule adds the
    // collider itself); the paw heights are the two surfaces' tops.
    public readonly struct BaitGap
    {
        public readonly string Name; public readonly float TakeoffX; public readonly float TakeoffPawHeight; public readonly float TargetX; public readonly float TargetPawHeight;
        public BaitGap(string name, float takeoffX, float takeoffPawHeight, float targetX, float targetPawHeight)
        { Name = name; TakeoffX = takeoffX; TakeoffPawHeight = takeoffPawHeight; TargetX = targetX; TargetPawHeight = targetPawHeight; }
    }

    public readonly struct SoloRoomDefinition
    {
        public readonly int Id; public readonly Vector2 Origin; public readonly float Width; public readonly SoloRoomElement[] Elements; public readonly SoloRoomOpening[] Openings; public readonly RequiredJump[] RequiredJumps; public readonly RequiredStep[] RequiredSteps;
        // PAX-076 (D-083): never null through a constructor; null only on default(SoloRoomDefinition), read as none.
        public readonly PrecisionSection[] PrecisionSections; public readonly BaitGap[] BaitGaps;
        public SoloRoomDefinition(int id, float originX, float width, SoloRoomElement[] elements, SoloRoomOpening[] openings, RequiredJump[] requiredJumps)
            : this(id, originX, width, elements, openings, requiredJumps, System.Array.Empty<RequiredStep>()) { }
        public SoloRoomDefinition(int id, float originX, float width, SoloRoomElement[] elements, SoloRoomOpening[] openings, RequiredJump[] requiredJumps, RequiredStep[] requiredSteps = null)
            : this(id, originX, width, elements, openings, requiredJumps, requiredSteps, null) { }
        public SoloRoomDefinition(int id, float originX, float width, SoloRoomElement[] elements, SoloRoomOpening[] openings, RequiredJump[] requiredJumps, RequiredStep[] requiredSteps, PrecisionSection[] precisionSections, BaitGap[] baitGaps = null)
        { Id = id; Origin = new Vector2(originX, 0f); Width = width; Elements = elements; Openings = openings; RequiredJumps = requiredJumps; RequiredSteps = requiredSteps ?? System.Array.Empty<RequiredStep>(); PrecisionSections = precisionSections ?? System.Array.Empty<PrecisionSection>(); BaitGaps = baitGaps ?? System.Array.Empty<BaitGap>(); }
    }
}
