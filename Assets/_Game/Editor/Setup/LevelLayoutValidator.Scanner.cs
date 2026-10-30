using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-113 (D-178): scanners ----------
        // A scanner is a MovingTrap of kind Hazard (KillVolumes already lists its whole sweep, so door clearance and trigger coverage
        // treat it as lethal along its path; it is never a surface) that warns at home before each move. Separately named, part of
        // ValidateKit.
        //  (1) Kind Hazard, warn >= MinScanWarnTicks (D-057's tell).
        //  (2) A vertical beam (taller than wide) that moves only sideways, its sweep inside the room's x range.
        //  (3) It spans floor to ceiling: at its home and at its far end, its bottom touches a solid and its top touches a solid
        //      (no gap to duck under or jump over).

        internal static bool IsScanner(SoloRoomElement e) => e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.Scan.IsConfigured;
        internal const int MinScanWarnTicks = 6;

        public static List<string> ValidateScanner(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            const float eps = 1e-3f;
            foreach (SoloRoomElement e in room.Elements.Where(IsScanner))
            {
                SoloRoomTrapSettings s = e.Settings;
                if (s.MovingKind != MovingTrapKind.Hazard) errors.Add($"{levelId}: {e.Name} is a scanner but a {s.MovingKind} mover; a scanner's beam is a Hazard (R1).");
                if (s.Scan.WarnTicks < MinScanWarnTicks) errors.Add($"{levelId}: {e.Name}'s warning is {s.Scan.WarnTicks} ticks; it needs at least {MinScanWarnTicks} (R1, D-057).");
                Rect home = Box(e, Vector2.zero);
                if (home.height <= home.width) errors.Add($"{levelId}: {e.Name} {Describe(home)} isn't a vertical beam (R2).");
                if (Mathf.Abs(s.Offset.y) > eps) errors.Add($"{levelId}: {e.Name} moves {s.Offset.y:F2} u up or down; a scanner sweeps sideways only (R2).");
                Rect end = home; end.position += new Vector2(s.Offset.x, 0f);
                if (Mathf.Min(home.xMin, end.xMin) < -eps || Mathf.Max(home.xMax, end.xMax) > room.Width + eps)
                    errors.Add($"{levelId}: {e.Name}'s sweep x [{Mathf.Min(home.xMin, end.xMin):F2}, {Mathf.Max(home.xMax, end.xMax):F2}] leaves the room's x range [0, {room.Width:F2}] (R2).");
                foreach ((Rect pose, string where) in new[] { (home, "home"), (end, "far end") })
                {
                    Rect below = Rect.MinMaxRect(pose.xMin, pose.yMin - .05f, pose.xMax, pose.yMin), above = Rect.MinMaxRect(pose.xMin, pose.yMax, pose.xMax, pose.yMax + .05f);
                    if (!room.Elements.Any(o => IsPulseGateSolid(o) && Box(o, Vector2.zero).Overlaps(below)))
                        errors.Add($"{levelId}: {e.Name} at its {where} {Describe(pose)} doesn't reach the floor; a scanner spans floor to ceiling (R3).");
                    if (!room.Elements.Any(o => IsPulseGateSolid(o) && Box(o, Vector2.zero).Overlaps(above)))
                        errors.Add($"{levelId}: {e.Name} at its {where} {Describe(pose)} doesn't reach the ceiling; a scanner spans floor to ceiling (R3).");
                }
            }
            return errors;
        }
    }
}
