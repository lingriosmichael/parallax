using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-112 (D-176): pulse gates ----------
        // A fixed laser barrier between two solids that turns on and off on a Periodic cycle: warnTicks of harmless flicker from
        // each fire, then onTicks lethal, then off. Separately named, part of ValidateKit. Its box is a kill volume (KillVolumes
        // lists it whole, so door clearance and trigger coverage treat it as always lethal, the safe side).
        //  (1) A configured PulseGateSettings on Periodic timing, with no trigger.
        //  (2) Warn >= MinWarnTicks (D-057's tell), on >= 1, warn + on < period.
        //  (3) The off gap fits a run across the barrier at run speed, from touching it to clear of it, plus D-079's 12-tick window.
        //  (4) The barrier sits inside the room's x range and touches a solid at both ends of its long axis (no way round it).

        internal static bool IsPulseGate(SoloRoomElement e) => e.Kind == SoloRoomElementKind.PulseGate;
        internal const int PulseGateWindowTicks = 12;

        public static List<string> ValidatePulseGate(string levelId, SoloRoomDefinition room, CatMotorConfig config)
        {
            var errors = new List<string>();
            SoloRoomElement[] gates = room.Elements.Where(IsPulseGate).ToArray();
            if (gates.Length == 0) return errors;
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; a pulse gate's off gap (R3) is undefined."); return errors; }
            const float eps = 1e-3f;
            foreach (SoloRoomElement e in gates)
            {
                PulseGateSettings g = e.Settings.Gate;
                SoloRoomTrapSettings s = e.Settings;
                if (!g.IsConfigured) { errors.Add($"{levelId}: {e.Name} is a PulseGate without PulseGateSettings (R1)."); continue; }
                if (s.RepeatMode != TrapRepeatMode.Periodic) errors.Add($"{levelId}: {e.Name} must be Periodic (R1); a pulse gate has no trigger.");
                if (e.SecondarySize != Vector2.zero) errors.Add($"{levelId}: {e.Name} has a trigger box; a pulse gate is Periodic and has none (R1).");
                if (g.WarnTicks < PulseGateMath.MinWarnTicks) errors.Add($"{levelId}: {e.Name}'s warning is {g.WarnTicks} ticks; it needs at least {PulseGateMath.MinWarnTicks} (R2, D-057).");
                if (g.OnTicks < 1) errors.Add($"{levelId}: {e.Name} is never on (R2).");
                if (g.WarnTicks + g.OnTicks >= s.PeriodTicks) errors.Add($"{levelId}: {e.Name}'s warning and on time ({g.WarnTicks} + {g.OnTicks}) fill its period {s.PeriodTicks}; it is never off (R2).");

                Rect box = Box(e, Vector2.zero);
                bool vertical = box.height >= box.width;
                float thickness = vertical ? box.width : box.height, catWidth = vertical ? config.ColliderSize.x : config.ColliderSize.y;
                float step = config.MaxSpeed * TickTime.SecondsPerTick;
                int cross = Mathf.CeilToInt((thickness + catWidth) / step - eps);
                int off = PulseGateMath.OffTicks(s.PeriodTicks, g.WarnTicks, g.OnTicks);
                if (off < cross + PulseGateWindowTicks)
                    errors.Add($"{levelId}: {e.Name} is off {off} ticks a period; a cat crossing it at run speed needs {cross} plus a {PulseGateWindowTicks}-tick window (R3).");

                if (box.xMin < -eps || box.xMax > room.Width + eps) errors.Add($"{levelId}: {e.Name} {Describe(box)} leaves the room's x range [0, {room.Width:F2}] (R4).");
                Rect endA = vertical ? Rect.MinMaxRect(box.xMin, box.yMin - .05f, box.xMax, box.yMin) : Rect.MinMaxRect(box.xMin - .05f, box.yMin, box.xMin, box.yMax);
                Rect endB = vertical ? Rect.MinMaxRect(box.xMin, box.yMax, box.xMax, box.yMax + .05f) : Rect.MinMaxRect(box.xMax, box.yMin, box.xMax + .05f, box.yMax);
                foreach ((Rect end, string which) in new[] { (endA, vertical ? "bottom" : "left"), (endB, vertical ? "top" : "right") })
                    if (!room.Elements.Any(o => IsPulseGateSolid(o) && Box(o, Vector2.zero).Overlaps(end)))
                        errors.Add($"{levelId}: {e.Name}'s {which} end {Describe(box)} touches no solid; a pulse gate runs from one solid to another (R4).");
            }
            return errors;
        }

        static bool IsPulseGateSolid(SoloRoomElement e) =>
            e.Kind is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.GripWall or SoloRoomElementKind.PitBottom;
    }
}
