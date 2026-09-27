using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class TrapLabLayout
    {
        // PAX-093 (D-095): the moving-floor room, origin 535 (room 11 ends at 522; the same 13 u gap), width 66. One of each
        // pattern, left to right, each over its own pit, in three checkpoint sections:
        //  - Ride (start, checkpoint x 2): Mover (Carry, Periodic: x 6-9 out to 15-18 and back every 280 ticks, leaving at
        //    room tick 80) is the only way over P1 (x 6-18): board it at home, ride it, step off onto F1. Then Slider (Slip, x
        //    24.5-27.5, floating in P2 x 24-31) slides 3.5 right the tick you land on it: land and jump again at once to F2.
        //  - Sink (gate x 32.5, checkpoint x 33.5): Drop (Slip, x 39-41, floating in the deep P3 x 37-43) goes 20 ticks after
        //    a touch, 4.5 down through the pit's hazard strip (y -4.2..-3.9), and rises back over 60 ticks after a 60-tick hold
        //    (Rearm): hop on and off. Then Shrink (x 47-53, the whole bridge over P4) shrinks from its left edge 10 ticks after
        //    a touch, over 60 ticks (5 u/s, slower than a run): run across and don't stop.
        //  - Shove (gate x 53.7, checkpoint x 54.3): the cut at x 58 sends Pusher (x 64-65, 0.8 tall, at the far end of F5) 11
        //    left over 55 ticks (10 u/s): its path ends in open space over P4, where the bridge was. Jump up onto Ledge_P (x
        //    60-63, underside 0.85, above the push path) as you cross the cut and let it pass under; then on to the door.
        static SoloRoomDefinition Room12()
        {
            var elements = new[] {
                E(SoloRoomElementKind.Ceiling,"Ceiling",(33f,7.5f),(66f,1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)),
                E(SoloRoomElementKind.Floor,"F0",(3f,-.5f),(6f,1f)),
                E(SoloRoomElementKind.Floor,"F1",(21f,-.5f),(6f,1f)),
                E(SoloRoomElementKind.Floor,"F2",(34f,-.5f),(6f,1f)),
                E(SoloRoomElementKind.Floor,"F3",(45f,-.5f),(4f,1f)),
                E(SoloRoomElementKind.Floor,"F5",(59.5f,-.5f),(13f,1f)),
                // P1 x 6-18, P2 x 24-31, P4 x 47-53: walls under the floors' ends, bottom y -4..-3, hazard y -3..-2.7.
                E(SoloRoomElementKind.Wall,"P1_L",(5.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"P1_R",(18.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"P1_Bottom",(12f,-3.5f),(12f,1f)), Pit("P1_Hazard",12f,12f,-2.85f),
                E(SoloRoomElementKind.Wall,"P2_L",(23.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"P2_R",(31.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"P2_Bottom",(27.5f,-3.5f),(7f,1f)), Pit("P2_Hazard",27.5f,7f,-2.85f),
                // P3 x 37-43, deep: bottom y -6..-5, its hazard a strip across the pit (y -4.2..-3.9) that the dropped floor passes
                // and ends under (top -4.5), so a cat on it or falling after it dies there.
                E(SoloRoomElementKind.Wall,"P3_L",(36.5f,-3.5f),(1f,5f)), E(SoloRoomElementKind.Wall,"P3_R",(43.5f,-3.5f),(1f,5f)),
                E(SoloRoomElementKind.PitBottom,"P3_Bottom",(40f,-5.5f),(6f,1f)), Pit("P3_Hazard",40f,6f,-4.05f),
                E(SoloRoomElementKind.Wall,"P4_L",(46.5f,-2.5f),(1f,3f)), E(SoloRoomElementKind.Wall,"P4_R",(53.5f,-2.5f),(1f,3f)),
                E(SoloRoomElementKind.PitBottom,"P4_Bottom",(50f,-3.5f),(6f,1f)), Pit("P4_Hazard",50f,6f,-2.85f),
                // Ride.
                E(SoloRoomElementKind.MovingTrap,"Mover",(7.5f,-.25f),(3f,.5f),settings:Floor(SurfaceMotion.Carry,new SoloRoomTrapSettings(offset:new Vector2(9f,0f),
                    moveTicks:90,holdTicks:30,returnTicks:90,repeatMode:TrapRepeatMode.Periodic,periodTicks:280,phaseTicks:80,cooldownTicks:210,movingKind:MovingTrapKind.Solid))),
                E(SoloRoomElementKind.MovingTrap,"Slider",(26f,-.25f),(3f,.5f),(26f,.5f),(3f,1f),Floor(SurfaceMotion.Slip,new SoloRoomTrapSettings(offset:new Vector2(3.5f,0f),
                    moveTicks:10,holdTicks:600,movingKind:MovingTrapKind.Solid))),
                // Drop.
                E(SoloRoomElementKind.MovingTrap,"Drop",(40f,-.25f),(2f,.5f),(40f,.5f),(2f,1f),Floor(SurfaceMotion.Slip,new SoloRoomTrapSettings(delayTicks:20,offset:new Vector2(0f,-4.5f),
                    moveTicks:15,holdTicks:60,returnTicks:60,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:135,movingKind:MovingTrapKind.Solid))),
                E(SoloRoomElementKind.ShrinkingFloor,"Shrink",(50f,-.25f),(6f,.5f),settings:new SoloRoomTrapSettings(new ShrinkSettings(60,0f,ShrinkFrom.Left),new SoloRoomTrapSettings(delayTicks:10))),
                // Shove.
                E(SoloRoomElementKind.MovingTrap,"Pusher",(64.5f,.4f),(1f,.8f),(58f,3.5f),(.5f,7f),new SoloRoomTrapSettings(new MovingFloorSettings(SurfaceMotion.Slip,pushes:true),
                    new SoloRoomTrapSettings(offset:new Vector2(-11f,0f),moveTicks:55,holdTicks:600,movingKind:MovingTrapKind.Solid))),
                E(SoloRoomElementKind.Floor,"Ledge_P",(61.5f,1.1f),(3f,.5f)),
                E(SoloRoomElementKind.Door,"Door",(65.6f,.75f),(.6f,1.5f)),
            };
            var openings = new[] {
                new SoloRoomOpening(SoloRoomOpeningKind.Pit, 6f, 18f, "P1_L", "P1_R", "P1_Bottom", "P1_Hazard"),
                new SoloRoomOpening(SoloRoomOpeningKind.Pit, 24f, 31f, "P2_L", "P2_R", "P2_Bottom", "P2_Hazard"),
                new SoloRoomOpening(SoloRoomOpeningKind.Pit, 37f, 43f, "P3_L", "P3_R", "P3_Bottom", "P3_Hazard"),
                new SoloRoomOpening(SoloRoomOpeningKind.Pit, 47f, 53f, "P4_L", "P4_R", "P4_Bottom", "P4_Hazard"),
            };
            var sections = new[] {
                CheckpointSection.Start("Ride", new Vector2(2f, 0f), new[] { "Mover", "Slider" }),
                new CheckpointSection("Sink", new Vector2(33.5f, 0f), new Rect(32.4f, -1f, .2f, 8f), new[] { "Drop", "Shrink" }),
                new CheckpointSection("Shove", new Vector2(54.3f, 0f), new Rect(53.6f, -1f, .2f, 8f), new[] { "Pusher" }),
            };
            return new SoloRoomDefinition(12, 535f, 66f, elements, openings, System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static SoloRoomTrapSettings Floor(SurfaceMotion motion, SoloRoomTrapSettings timing) => new(new MovingFloorSettings(motion), timing);

        static SoloRoomElement Pit(string name, float x, float width, float y) =>
            new(SoloRoomElementKind.Hazard, name, new Vector2(x, y), new Vector2(width, .3f), hazardRole: SoloRoomHazardRole.OpeningBottom);
    }
}
