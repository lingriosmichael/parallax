using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>Plain PAX-045 component test bench; it is intentionally not a troll level.</summary>
    public static class TrapLabLayout
    {
        public static readonly IReadOnlyList<SoloRoomDefinition> Rooms = new[]
        {
            Room(0, 0f, new[] {
                E(SoloRoomElementKind.HiddenSpikes,"SourceSpikes",(6f,.15f),(2f,.3f),(5f,.5f),(.5f,1f),new SoloRoomTrapSettings(revealDelayTicks:6)),
                E(SoloRoomElementKind.FallingBlock,"Block_1",(8f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"SourceSpikes")),
                E(SoloRoomElementKind.FallingBlock,"Block_2",(14f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_1")),
                E(SoloRoomElementKind.FallingBlock,"Block_3",(20f,6f),(1f,1f),settings:new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.3f,travelDistance:5.5f,triggerSource:TrapTriggerSource.Chain,chainSource:"Block_2")),
                E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(22f,3.5f),(.5f,7f),settings:new SoloRoomTrapSettings(delayTicks:6,moveTicks:24,offset:new Vector2(6,0),triggerSource:TrapTriggerSource.Chain,chainSource:"Block_3")) }),
            Room(1, 45f, new[] {
                E(SoloRoomElementKind.MovingTrap,"SlidingSpikes",(8f,.15f),(2f,.3f),(6.5f,.5f),(.5f,1f),new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(6,0),moveTicks:36,holdTicks:24,returnTicks:36,cooldownTicks:96,movingKind:MovingTrapKind.Hazard)),
                E(SoloRoomElementKind.MovingTrap,"RisingFloor",(15f,.25f),(4f,.5f),(15f,.75f),(4f,1f),new SoloRoomTrapSettings(offset:new Vector2(0,5.8f),moveTicks:36,holdTicks:18,returnTicks:36,cooldownTicks:90,movingKind:MovingTrapKind.Solid)),
                E(SoloRoomElementKind.Hazard,"CeilingHazard",(15f,6.85f),(4f,.3f)),
                E(SoloRoomElementKind.MovingTrap,"Crusher",(26f,1f),(1f,2f),(23.3f,1f),(3f,2f),new SoloRoomTrapSettings(delayTicks:6,offset:new Vector2(-3,0),moveTicks:18,holdTicks:12,returnTicks:18,cooldownTicks:48,movingKind:MovingTrapKind.Solid)), E(SoloRoomElementKind.Wall,"FixedPillar",(22f,1f),(1f,2f)) }),
            Room(2, 90f, new[] {
                E(SoloRoomElementKind.HiddenSpikes,"PeriodicSpikes",(10f,.15f),(2f,.3f),settings:new SoloRoomTrapSettings(repeatMode:TrapRepeatMode.Periodic,periodTicks:96,phaseTicks:12,cooldownTicks:24)), E(SoloRoomElementKind.Ceiling,"LowSlab",(10f,1.45f),(4f,.5f)),
                E(SoloRoomElementKind.FallingBlock,"RearmBlock",(16f,6f),(1f,1f),(16f,.6f),(2f,1f),new SoloRoomTrapSettings(delayTicks:6,unitsPerTick:.3f,travelDistance:5.5f,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:48)),
                E(SoloRoomElementKind.CollapsingFloor,"RearmCollapse",(24f,-.5f),(3f,1f),settings:new SoloRoomTrapSettings(delayTicks:12,repeatMode:TrapRepeatMode.Rearm,cooldownTicks:48)), E(SoloRoomElementKind.Hazard,"PitHazard",(24f,-2.85f),(3f,.3f)) })
        };

        static SoloRoomDefinition Room(int id, float origin, SoloRoomElement[] traps)
        {
            var elements = new List<SoloRoomElement> { E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,7.5f),(32f,1f)), E(SoloRoomElementKind.Checkpoint,"Checkpoint",(2f,0),(0,0)), E(SoloRoomElementKind.Door,"Door",(id == 0 ? 22f : 30f,.75f),(.6f,1.5f)) };
            if (id == 2)
            {
                elements.Add(E(SoloRoomElementKind.Floor,"Floor_Left",(11.25f,-.5f),(22.5f,1f)));
                elements.Add(E(SoloRoomElementKind.Floor,"Floor_Right",(28.75f,-.5f),(6.5f,1f)));
                elements.Add(E(SoloRoomElementKind.PitBottom,"PitBottom",(24f,-3.5f),(3f,1f)));
                elements.Add(E(SoloRoomElementKind.Wall,"PitWall_Left",(22f,-2.5f),(1f,3f)));
                elements.Add(E(SoloRoomElementKind.Wall,"PitWall_Right",(26f,-2.5f),(1f,3f)));
            }
            else elements.Add(E(SoloRoomElementKind.Floor,"Floor",(16f,-.5f),(32f,1f)));
            elements.AddRange(traps);
            SoloRoomOpening[] openings = id == 2 ? new[] { new SoloRoomOpening(SoloRoomOpeningKind.Pit, 22.5f, 25.5f, "PitWall_Left", "PitWall_Right", "PitBottom", "PitHazard") } : System.Array.Empty<SoloRoomOpening>();
            return new SoloRoomDefinition(id, origin, 32f, elements.ToArray(), openings, System.Array.Empty<RequiredJump>());
        }
        static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x,float y) p, (float x,float y) s, (float x,float y) p2 = default, (float x,float y) s2 = default, SoloRoomTrapSettings settings = default) => new(kind, name, new Vector2(p.x,p.y), new Vector2(s.x,s.y), new Vector2(p2.x,p2.y), new Vector2(s2.x,s2.y), settings);
    }
}
