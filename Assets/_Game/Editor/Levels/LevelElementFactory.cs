using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Levels
{
    // PAX-051 (D-066): the same tuple-argument element/opening/jump builders SoloRoomsLayout
    // uses privately, made available to per-level layout classes so they can build
    // SoloRoomDefinitions with the existing types without duplicating SoloRoomsLayout.cs itself.
    static class LevelElementFactory
    {
        public static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x, float y) p, (float x, float y) s, (float x, float y) p2 = default, (float x, float y) s2 = default, SoloRoomTrapSettings settings = default, SoloRoomHazardRole hazardRole = SoloRoomHazardRole.Normal) => new(kind, name, new Vector2(p.x, p.y), new Vector2(s.x, s.y), new Vector2(p2.x, p2.y), new Vector2(s2.x, s2.y), settings, hazardRole);
        public static SoloRoomOpening O(SoloRoomOpeningKind kind, float minX, float maxX, string left, string right, string closure, string hazard) => new(kind, minX, maxX, left, right, closure, hazard);
        public static RequiredJump J(string reference, RequiredJumpKind kind, RequiredJumpFrame frame, RequiredJumpDirection direction, float takeoffX, float landingX, float takeoffHeight, float landingHeight, float runway, float hazardHeight = 0f, string sourceName = null, string destinationName = null) => new(reference, kind, frame, direction, takeoffX, landingX, takeoffHeight, landingHeight, runway, hazardHeight, sourceName, destinationName);
    }
}
