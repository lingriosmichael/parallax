using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-093 (D-095): floors that move ----------
        // A mover is a Periodic MovingTrap Solid; a slide-away moves sideways once (Once or Rearm); a drop-and-return floor
        // drops and rises back (Rearm, move/hold/return); a push wall is a Solid with Pushes; a shrinking floor is its own kind.
        // Part of ValidateKit. Q1: in levels numbered 14+ and Trap Lab room 12 a sideways Solid declares Carry or Slip. Q4: a
        // repeating Solid's cooldown covers its move + hold + return. Q5: a shrinker's settings. Q6: a push path ends against its
        // named crush partner or in open space. D-056 (3): a Solid's swept path overlaps no fixed geometry (touching is
        // allowed). D-065: movers, shrinkers and push walls are levels 11+; slide-away and drop-and-return are allowed in 1-10.
        internal const int ExplicitSurfaceMotionFromLevel = 14;
        internal const string MovingFloorLabRoom = "TrapLab12";

        static bool IsMovingSolid(SoloRoomElement e) => e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Solid;

        public static List<string> ValidateMovingFloors(string levelId, SoloRoomDefinition room, LevelListConfig levels)
        {
            const float eps = 1e-3f;
            var errors = new List<string>();
            SoloRoomElement[] solids = room.Elements.Where(IsMovingSolid).ToArray();
            SoloRoomElement[] shrinkers = room.Elements.Where(e => e.Kind == SoloRoomElementKind.ShrinkingFloor).ToArray();
            if (solids.Length == 0 && shrinkers.Length == 0) return errors;
            int number = levels != null ? LevelNumber(levels, levelId) : 0;
            bool explicitMotion = number >= ExplicitSurfaceMotionFromLevel || levelId == MovingFloorLabRoom;
            bool easyBand = number > 0 && number <= EasyBandLastLevel;
            SoloRoomElement[] fixedSolids = room.Elements.Where(IsFixedSolid).ToArray();

            foreach (SoloRoomElement e in solids)
            {
                SoloRoomTrapSettings s = e.Settings;
                if (explicitMotion && Mathf.Abs(s.Offset.x) > eps && s.Floor.Motion == SurfaceMotion.Legacy)
                    errors.Add($"{levelId}: {e.Name} moves sideways with SurfaceMotion Legacy; in level {ExplicitSurfaceMotionFromLevel}+ and {MovingFloorLabRoom} it declares Carry or Slip (D-095 Q1).");
                int motion = s.MoveTicks + s.HoldTicks + s.ReturnTicks;
                if (s.RepeatMode != TrapRepeatMode.Once && s.CooldownTicks < motion)
                    errors.Add($"{levelId}: {e.Name}'s cooldown {s.CooldownTicks} ticks is below its move + hold + return ({motion}); it would snap home mid-motion (D-095 Q4).");
                Rect start = Box(e, Vector2.zero), end = start;
                end.position += s.Offset;
                Rect swept = Envelope(start, end);
                foreach (SoloRoomElement f in fixedSolids)
                    if (Overlap(swept, Box(f, Vector2.zero)) > eps)
                        errors.Add($"{levelId}: {e.Name}'s swept path {Describe(swept)} runs into {f.Name}; a moving Solid's path overlaps no fixed geometry (D-056 (3)).");
                if (s.Floor.Pushes) CheckPushPath(levelId, room, e, fixedSolids, errors);
                if (!easyBand) continue;
                if (s.RepeatMode == TrapRepeatMode.Periodic) errors.Add($"{levelId}: mover '{e.Name}' in level {number}; movers are for levels {EasyBandLastLevel + 1}+ only (D-095).");
                if (s.Floor.Pushes) errors.Add($"{levelId}: push wall '{e.Name}' in level {number}; push walls are for levels {EasyBandLastLevel + 1}+ only (D-095).");
            }

            foreach (SoloRoomElement e in shrinkers)
            {
                ShrinkSettings k = e.Settings.Shrink;
                if (!k.IsConfigured) errors.Add($"{levelId}: {e.Name} is a shrinking floor with no shrink settings (D-095 Q5).");
                else
                {
                    if (k.ShrinkTicks < 1) errors.Add($"{levelId}: {e.Name} shrinks over {k.ShrinkTicks} ticks; at least 1 (D-095 Q5).");
                    if (k.MinWidth < 0f || k.MinWidth >= e.Size.x - eps)
                        errors.Add($"{levelId}: {e.Name}'s minimum width {k.MinWidth:F2} is outside [0, its width {e.Size.x:F2}); a shrinking floor shrinks (D-095 Q5).");
                }
                if (easyBand) errors.Add($"{levelId}: shrinking floor '{e.Name}' in level {number}; shrinking floors are for levels {EasyBandLastLevel + 1}+ only (D-095).");
            }
            return errors;
        }

        // Q6: the push path is where the pushed cat goes: from the wall's leading edge to where it stops, plus one cat width, over
        // the wall's height. Any fixed solid in it but the named partner is an error; a named partner is at its far end.
        static void CheckPushPath(string levelId, SoloRoomDefinition room, SoloRoomElement wall, SoloRoomElement[] fixedSolids, List<string> errors)
        {
            const float eps = 1e-3f;
            float dx = wall.Settings.Offset.x;
            if (Mathf.Abs(dx) <= eps) { errors.Add($"{levelId}: push wall {wall.Name} doesn't move sideways (D-095 Q6)."); return; }
            float width = Config() != null ? Config().ColliderSize.x : 1f;
            Rect box = Box(wall, Vector2.zero);
            float lead0 = dx > 0f ? box.xMax : box.xMin, lead1 = lead0 + dx;
            Rect path = dx > 0f ? Rect.MinMaxRect(lead0, box.yMin, lead1 + width, box.yMax) : Rect.MinMaxRect(lead1 - width, box.yMin, lead0, box.yMax);
            Rect end = dx > 0f ? Rect.MinMaxRect(lead1, box.yMin, lead1 + width, box.yMax) : Rect.MinMaxRect(lead1 - width, box.yMin, lead1, box.yMax);
            string partner = wall.Settings.Floor.CrushPartner;
            foreach (SoloRoomElement f in fixedSolids)
                if (f.Name != partner && Overlap(path, Box(f, Vector2.zero)) > eps)
                    errors.Add($"{levelId}: {wall.Name}'s push path {Describe(path)} runs into {f.Name}, which isn't its crush partner; a push path ends against its named partner or in open space (D-056 (3), D-095 Q6).");
            if (string.IsNullOrEmpty(partner)) return;
            SoloRoomElement? named = room.Elements.Where(f => f.Name == partner).Cast<SoloRoomElement?>().FirstOrDefault();
            if (!named.HasValue || !IsFixedSolid(named.Value)) { errors.Add($"{levelId}: {wall.Name}'s crush partner '{partner}' is not a fixed solid of this room (D-095 Q6)."); return; }
            if (Overlap(end, Box(named.Value, Vector2.zero)) <= eps)
                errors.Add($"{levelId}: {wall.Name}'s push path ends at {Describe(end)}, short of its crush partner {partner}; a named partner is at the path's end (D-095 Q6).");
        }

        static float Overlap(Rect a, Rect b) =>
            Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) * Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
    }
}
