using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public enum SoloRoomElementKind { Floor, Ceiling, Wall, PitBottom, Checkpoint, Door, Hazard, CollapsingFloor, HiddenSpikes, FallingBlock, GravityFlip, DoorRetreat }

    public readonly struct SoloRoomElement
    {
        public readonly SoloRoomElementKind Kind;
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly Vector2 SecondaryPosition;
        public readonly Vector2 SecondarySize;

        public SoloRoomElement(SoloRoomElementKind kind, string name, Vector2 position, Vector2 size, Vector2 secondaryPosition = default, Vector2 secondarySize = default)
        { Kind = kind; Name = name; Position = position; Size = size; SecondaryPosition = secondaryPosition; SecondarySize = secondarySize; }
    }

    public readonly struct SoloRoomDefinition
    {
        public readonly int Id;
        public readonly Vector2 Origin;
        public readonly float Width;
        public readonly SoloRoomElement[] Elements;
        public SoloRoomDefinition(int id, float originX, SoloRoomElement[] elements) { Id = id; Origin = new Vector2(originX, 0f); Width = 24f; Elements = elements; }
    }

    public static class SoloRoomsLayout
    {
        public const float FloorTop = 0f;
        public const float CeilingUnderside = 7f;
        public static readonly IReadOnlyList<SoloRoomDefinition> Rooms = new[]
        {
            new SoloRoomDefinition(0, 0f, new[] {
                E(SoloRoomElementKind.Floor,"Floor_Left",(7,-.5f),(14,1)), E(SoloRoomElementKind.Floor,"Floor_Right",(20.5f,-.5f),(7,1)),
                E(SoloRoomElementKind.Floor,"PitWall_Left",(13.5f,-2.5f),(1,3)), E(SoloRoomElementKind.Floor,"PitWall_Right",(17.5f,-2.5f),(1,3)),
                E(SoloRoomElementKind.PitBottom,"PitBottom",(15.5f,-3.5f),(3,1)), E(SoloRoomElementKind.Hazard,"PitHazard",(15.5f,-2.85f),(3,.3f)),
                E(SoloRoomElementKind.CollapsingFloor,"Collapse",(15.5f,-.5f),(3,1)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2,0),(0,0)),
                E(SoloRoomElementKind.Door,"Door_0",(10,.75f),(.6f,1.5f)), E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(7.5f,3.5f),(1,7)) }),
            new SoloRoomDefinition(1, 40f, new[] {
                E(SoloRoomElementKind.Floor,"Floor",(12,-.5f),(24,1)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_1",(2,0),(0,0)),
                E(SoloRoomElementKind.Hazard,"StaticHazard",(8.5f,.15f),(1,.3f)), E(SoloRoomElementKind.HiddenSpikes,"HiddenSpikes",(12,.15f),(4,.3f),(8.5f,2),(2,4)),
                E(SoloRoomElementKind.Floor,"Step",(4,1),(2,2)), E(SoloRoomElementKind.Floor,"LedgeSolid",(7,4.25f),(2,.5f)), E(SoloRoomElementKind.CollapsingFloor,"LedgeCollapse",(10,4.25f),(4,.5f)),
                E(SoloRoomElementKind.Door,"Door_1",(21,.75f),(.6f,1.5f)) }),
            new SoloRoomDefinition(2, 80f, new[] {
                E(SoloRoomElementKind.Floor,"Floor",(12,-.5f),(24,1)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_2",(2,0),(0,0)),
                E(SoloRoomElementKind.FallingBlock,"FallingBlock",(11.75f,6.25f),(1.5f,1.5f),(8.25f,3.5f),(.5f,7)), E(SoloRoomElementKind.GravityFlip,"HiddenFlip",(15.5f,1),(3,2)),
                E(SoloRoomElementKind.Hazard,"CeilingHazard",(18.5f,6.85f),(11,.3f)), E(SoloRoomElementKind.Door,"Door_2",(21,.75f),(.6f,1.5f)) }),
            new SoloRoomDefinition(3, 120f, new[] {
                E(SoloRoomElementKind.Floor,"Floor",(12,-.5f),(24,1)), E(SoloRoomElementKind.Checkpoint,"Checkpoint_3",(2,0),(0,0)),
                E(SoloRoomElementKind.GravityFlip,"FlipTool",(6,3.2f),(2,2)), E(SoloRoomElementKind.Hazard,"RecessHazard",(12.5f,9.85f),(3,.3f)),
                E(SoloRoomElementKind.Floor,"RecessWall_Left",(10.5f,9),(1,2)), E(SoloRoomElementKind.Floor,"RecessWall_Right",(14.5f,9),(1,2)), E(SoloRoomElementKind.Floor,"RecessCap",(12.5f,10.5f),(3,1)),
                E(SoloRoomElementKind.CollapsingFloor,"CeilingCollapse",(12.5f,7.5f),(3,1)), E(SoloRoomElementKind.Door,"Door_3",(20,.75f),(.6f,1.5f)), E(SoloRoomElementKind.DoorRetreat,"DoorRetreat",(16.5f,3.5f),(1,7)) })
        };

        static SoloRoomElement E(SoloRoomElementKind kind, string name, Vector2 position, Vector2 size, Vector2 secondaryPosition = default, Vector2 secondarySize = default) => new(kind, name, position, size, secondaryPosition, secondarySize);
        static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x, float y) position, (float x, float y) size, (float x, float y) secondaryPosition = default, (float x, float y) secondarySize = default) =>
            new(kind, name, new Vector2(position.x, position.y), new Vector2(size.x, size.y), new Vector2(secondaryPosition.x, secondaryPosition.y), new Vector2(secondarySize.x, secondarySize.y));
    }
}
