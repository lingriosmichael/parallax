using Parallax.Core;
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
        // D-291: an aiming laser (PAX-121) hung from the underside `under` at x, tracking a cat over `zone` (room coordinates): it aims 50 ticks,
        // locks 12 (its tell), fires 10, on its `period`. It's what took the storm cloud's place: stand still in its zone and it hits.
        internal const int LaserAim = 50, LaserLock = 12, LaserFire = 10;
        internal static SoloRoomElement Laser(string name, float x, float under, Rect zone, int phase, int period = 135, float range = 40f)
        {
            float muzzle = under - .15f;
            return E(SoloRoomElementKind.Turret, name, (x, muzzle), (.6f, .6f), settings: new SoloRoomTrapSettings(
                new TurretSettings(LaserAim, LaserLock, LaserFire, range: range, ceilingMount: true, aimZone: new Rect(zone.x - x, zone.y - muzzle, zone.width, zone.height)),
                new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: phase, cooldownTicks: LaserAim + LaserLock + LaserFire)));
        }

        public static SoloRoomElement E(SoloRoomElementKind kind, string name, (float x, float y) p, (float x, float y) s, (float x, float y) p2 = default, (float x, float y) s2 = default, SoloRoomTrapSettings settings = default, SoloRoomHazardRole hazardRole = SoloRoomHazardRole.Normal) => new(kind, name, new Vector2(p.x, p.y), new Vector2(s.x, s.y), new Vector2(p2.x, p2.y), new Vector2(s2.x, s2.y), settings, hazardRole);
        public static SoloRoomOpening O(SoloRoomOpeningKind kind, float minX, float maxX, string left, string right, string closure, string hazard) => new(kind, minX, maxX, left, right, closure, hazard);
        public static RequiredJump J(string reference, RequiredJumpKind kind, RequiredJumpFrame frame, RequiredJumpDirection direction, float takeoffX, float landingX, float takeoffHeight, float landingHeight, float runway, float hazardHeight = 0f, string sourceName = null, string destinationName = null) => new(reference, kind, frame, direction, takeoffX, landingX, takeoffHeight, landingHeight, runway, hazardHeight, sourceName, destinationName);
    }
}
