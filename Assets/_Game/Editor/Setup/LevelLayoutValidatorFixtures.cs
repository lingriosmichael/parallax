using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Setup
{
    // PAX-051 (D-066): minimal, deliberately-invalid layouts used only by
    // LevelLayoutValidatorTests (EditMode) to exercise LevelLayoutValidator's negative cases.
    // Not used by any menu, and not part of LevelLayouts (not a real level).
    public static class LevelLayoutValidatorFixtures
    {
        public static SoloRoomDefinition TwoDoors() => new(0, 0f, 10f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(3f,.75f),(.6f,1.5f)),
            E(SoloRoomElementKind.Door,"Door_B",(7f,.75f),(.6f,1.5f)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        public static SoloRoomDefinition DoorOutsideBounds() => new(0, 0f, 10f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(100f,.75f),(.6f,1.5f)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        public static SoloRoomDefinition JumpBeyondReach() => new(0, 0f, 200f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(3f,.75f),(.6f,1.5f)),
        }, System.Array.Empty<SoloRoomOpening>(), new[] {
            J("Floor_A", RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, 0f, 100f, 0f, 0f, 3f)
        });

        public static SoloRoomDefinition SlackBelowTwelveTicks() => new(0, 0f, 20f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(3f,.75f),(.6f,1.5f)),
            E(SoloRoomElementKind.HiddenSpikes,"PeriodicSpikes",(9f,.15f),(1f,.3f),
                settings: new SoloRoomTrapSettings(revealDelayTicks:6, repeatMode:TrapRepeatMode.Periodic, periodTicks:20, cooldownTicks:19)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        public static SoloRoomDefinition RevealLeadBelowSix() => new(0, 0f, 20f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(3f,.75f),(.6f,1.5f)),
            E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(9f,.15f),(1f,.3f),
                settings: new SoloRoomTrapSettings(revealDelayTicks:2)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());

        public static SoloRoomDefinition ChainCycle() => new(0, 0f, 20f, new[] {
            E(SoloRoomElementKind.Ceiling,"Ceiling",(5f,7.5f),(10f,1f)),
            E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(1f,0f),(0f,0f)),
            E(SoloRoomElementKind.Floor,"Floor_A",(5f,-.5f),(10f,1f)),
            E(SoloRoomElementKind.Door,"Door_A",(3f,.75f),(.6f,1.5f)),
            E(SoloRoomElementKind.CollapsingFloor,"Collapse_A",(6f,-.5f),(1f,1f),
                settings: new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain, chainSource:"Collapse_B", delayTicks:12)),
            E(SoloRoomElementKind.CollapsingFloor,"Collapse_B",(7f,-.5f),(1f,1f),
                settings: new SoloRoomTrapSettings(triggerSource:TrapTriggerSource.Chain, chainSource:"Collapse_A", delayTicks:12)),
        }, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
    }
}
