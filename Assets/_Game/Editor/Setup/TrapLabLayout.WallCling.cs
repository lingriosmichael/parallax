using Parallax.Core;
using Parallax.Gameplay.Rooms;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Setup
{
    public static partial class TrapLabLayout
    {
        // PAX-105 (D-110 and amendment 2): the wall room, origin 659 (room 13 ends at 646; the same 13 u gap), width 40, a flat
        // floor under a ceiling at 9. Only grip walls can be grabbed. Left to right:
        //  - Plain_Wall (x 2-3, y 0.8-6): a plain wall, hung over the floor. Grab does nothing against it. Spike_Floor, hidden at
        //    its foot (x 3-4.6), shows when the cat walks left over its trigger (x 4.6-6) and kills a cat that jumps at the wall
        //    hoping to grab it and drops at its foot.
        //  - The checkpoint (x 8).
        //  - Lone_Wall (x 11-12, y 0.8-6): one grip wall alone, hung over the floor. A cat can grab it and kick off, but never
        //    climb it: the face just left can't be grabbed again before landing, and its top is too high to kick back onto.
        //  - Block_F (x 14.5-15.5, y 1.2-3.2): a falling block that rises 1.2 when a cat jumps at it (its trigger is beside its
        //    left face, above the walking cat). Not a grip wall: Grab does nothing.
        //  - The shaft: Shaft_L (grip, x 23-24, y 1-7, hung so the cat walks under it) and Shaft_R (grip, x 26-27, up to 5.5,
        //    the Plateau's face), 2 u apart. Grab Shaft_L and wall-jump from side to side (latch mode: one Grab) up onto the
        //    Plateau, and on to the door.
        static SoloRoomDefinition Room14()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(20f,9.5f),(40f,1f)),
                E(SoloRoomElementKind.Floor,"Floor",(13f,-.5f),(26f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(8f,0),(0,0)),
                E(SoloRoomElementKind.Wall,"Plain_Wall",(2.5f,3.4f),(1f,5.2f)),
                E(SoloRoomElementKind.HiddenSpikes,"Spike_Floor",(3.8f,.15f),(1.6f,.3f),(5.3f,.5f),(1.4f,1f),new SoloRoomTrapSettings(revealDelayTicks:6)),
                E(SoloRoomElementKind.GripWall,"Lone_Wall",(11.5f,3.4f),(1f,5.2f)),
                E(SoloRoomElementKind.FallingBlock,"Block_F",(15f,2.2f),(1f,2f),(13.75f,2.2f),(1.5f,2f),
                    new SoloRoomTrapSettings(delayTicks:20,unitsPerTick:.12f,travelDistance:1.2f,direction:FallingBlockDirection.Up)),
                E(SoloRoomElementKind.GripWall,"Shaft_L",(23.5f,4f),(1f,6f)),
                E(SoloRoomElementKind.GripWall,"Shaft_R",(26.5f,2.75f),(1f,5.5f)),
                E(SoloRoomElementKind.Floor,"Plateau",(33.5f,2.75f),(13f,5.5f)),
                E(SoloRoomElementKind.Door,"Door",(38f,6.25f),(.6f,1.5f)),
            };
            return new SoloRoomDefinition(14, 659f, 40f, elements, System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>());
        }
    }
}
