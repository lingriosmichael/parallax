using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-084 (D-086): spears ----------
        // A spear is an arrow (every arrow rule applies unchanged) that fires once and sticks at the lane end, where its
        // shaft (the visible shaft, Length x Thickness) is solid from the tick after its stop. Separately named, not part
        // of Validate(). The level band (levels 11+ only) is ValidateBand's.

        internal static bool IsSpear(SoloRoomElement e) => IsArrow(e) && e.Settings.Arrow.Spear;

        // The stuck shaft, at the lane end, in origin + room-local space.
        internal static Rect StuckShaftBox(SoloRoomElement e, Vector2 origin)
        {
            ArrowLane lane = e.Settings.Arrow;
            float tip = lane.LaneEndX, back = tip - ArrowMath.Sign(lane.Direction) * lane.Length;
            return Rect.MinMaxRect(origin.x + Mathf.Min(back, tip), origin.y + lane.LaneY - lane.Thickness * .5f,
                origin.x + Mathf.Max(back, tip), origin.y + lane.LaneY + lane.Thickness * .5f);
        }

        public static List<string> ValidateSpear(string levelId, SoloRoomDefinition room, PlatformSizeConfig limits)
        {
            var errors = new List<string>();
            SoloRoomElement[] spears = room.Elements.Where(IsSpear).ToArray();
            if (spears.Length == 0) return errors;
            if (limits == null) { errors.Add($"{levelId}: no PlatformSizeConfig given; the spear's shaft limits are undefined."); return errors; }
            const float tolerance = 1e-4f;
            foreach (SoloRoomElement e in spears)
            {
                ArrowLane lane = e.Settings.Arrow;
                if (e.Settings.RepeatMode != TrapRepeatMode.Once)
                    errors.Add($"{levelId}: {e.Name} is a spear set to {e.Settings.RepeatMode}; a spear fires once per room life (Once only, D-086).");
                if (lane.Length < limits.MinWidth - tolerance)
                    errors.Add($"{levelId}: {e.Name} shaft length {lane.Length:F2} is below the minimum width {limits.MinWidth:F2} (PlatformSizeConfig): the stuck spear must be standable.");
                if (lane.Thickness < limits.SpearMinThickness - tolerance)
                    errors.Add($"{levelId}: {e.Name} shaft thickness {lane.Thickness:F2} is below the spear minimum {limits.SpearMinThickness:F2} (PlatformSizeConfig): it could be tunnelled.");
                CheckSpearHost(levelId, room, e, errors);
                CheckStuckShaftClear(levelId, room, e, errors);
            }
            return errors;
        }

        // The element whose face the spear sticks in is fixed geometry, never something that moves or goes away. The
        // room's own end is the builder's edge wall. A lane ending in open air is ValidateArrowLane's.
        static void CheckSpearHost(string levelId, SoloRoomDefinition room, SoloRoomElement spear, List<string> errors)
        {
            const float eps = 1e-3f;
            ArrowLane lane = spear.Settings.Arrow;
            if (Mathf.Abs(lane.LaneEndX) < eps || Mathf.Abs(lane.LaneEndX - room.Width) < eps) return;
            Rect band = ArrowLaneBox(spear, Vector2.zero);
            float sign = ArrowMath.Sign(lane.Direction);
            foreach (SoloRoomElement host in room.Elements)
            {
                if (host.Name == spear.Name || host.Size == Vector2.zero || IsFixedSolid(host)) continue;
                Rect r = Box(host, Vector2.zero);
                bool face = Mathf.Abs((sign > 0f ? r.xMin : r.xMax) - lane.LaneEndX) < eps && r.yMin < band.yMax - eps && r.yMax > band.yMin + eps;
                if (face) errors.Add($"{levelId}: {spear.Name} sticks in {host.Name}, a {host.Kind}; a spear sticks only in fixed geometry (Wall, Floor, Ceiling, PitBottom).");
            }
        }

        // The stuck shaft overlaps no other element and no other arrow's lane (touching is allowed).
        static void CheckStuckShaftClear(string levelId, SoloRoomDefinition room, SoloRoomElement spear, List<string> errors)
        {
            Rect shaft = StuckShaftBox(spear, Vector2.zero);
            foreach (SoloRoomElement other in room.Elements)
            {
                if (other.Name == spear.Name) continue;
                if (other.Size != Vector2.zero && Box(other, Vector2.zero).Overlaps(shaft))
                    errors.Add($"{levelId}: {spear.Name} stuck shaft {Describe(shaft)} overlaps {other.Name}.");
                else if (IsArrow(other) && ArrowLaneBox(other, Vector2.zero).Overlaps(shaft))
                    errors.Add($"{levelId}: {spear.Name} stuck shaft {Describe(shaft)} overlaps {other.Name}'s lane.");
            }
        }
    }
}
