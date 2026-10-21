using Parallax.Core;
using Parallax.Gameplay.Rooms;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-064 (D-158): the sound each trap kind makes when it fires.</summary>
    public static class TrapCues
    {
        public static AudioCue For(RoomTrap trap) => trap switch
        {
            HiddenSpikesTrap => AudioCue.TrapSpikes,
            ArrowTrap => AudioCue.TrapArrow,
            CollapsingFloorTrap => AudioCue.TrapCollapse,
            FallingBlockTrap => AudioCue.TrapFallingBlock,
            MovingTrap => AudioCue.TrapMover,
            GravityFlipTrap => AudioCue.TrapFlip,
            InverterTrap => AudioCue.TrapInverter,
            GeyserTrap => AudioCue.TrapGeyser,
            StormCloudTrap => AudioCue.TrapStorm,
            ShrinkingFloorTrap => AudioCue.TrapShrink,
            DoorRetreatTrap => AudioCue.TrapDoor,
            ClimbVine => AudioCue.TrapVine,
            _ => AudioCue.TrapOther,
        };
    }
}
