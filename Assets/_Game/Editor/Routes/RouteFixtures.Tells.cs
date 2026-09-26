using System;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Routes
{
    // PAX-059b (the developer's review): tells for lifts. A floor section (x 10-11) is a lift that rises 3.2 under spikes
    // on the roof; singled puts the spikes over the lift alone, spread runs them 4 u each side of it.
    public static partial class RouteFixtures
    {
        public static SoloRoomDefinition LiftUnderSpikesRoom(bool spread) => new(0, 0f, 20f, new[] {
            El(SoloRoomElementKind.Ceiling, "Roof", 10f, 9.5f, 20f, 1f),
            El(SoloRoomElementKind.Checkpoint, "Checkpoint", 2f, 0f, 0f, 0f),
            El(SoloRoomElementKind.Floor, "Ground_L", 5f, -.5f, 10f, 1f),
            El(SoloRoomElementKind.Floor, "Ground_R", 15.5f, -.5f, 9f, 1f),
            new SoloRoomElement(SoloRoomElementKind.MovingTrap, "Lift", new Vector2(10.5f, -.5f), new Vector2(1f, 1f), new Vector2(10.5f, .28f), new Vector2(1f, .56f),
                new SoloRoomTrapSettings(offset: new Vector2(0f, 3.2f), moveTicks: 8, holdTicks: 60, returnTicks: 20, movingKind: MovingTrapKind.Solid)),
            El(SoloRoomElementKind.Hazard, "Roof_Spikes", 10.5f, 8.85f, spread ? 9f : 1f, .3f),
            El(SoloRoomElementKind.Door, "Door", 18f, .75f, .6f, 1.5f) }, Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());
    }
}
