using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-085 (D-087): inverters ----------
        // Touching one swaps the cat's left and right for DurationTicks motor steps (default 150). Separately named, not part
        // of Validate(), like ValidateSpear. The level band (levels 11+ only) is ValidateBand's. An inverter is never a kill
        // volume (KillVolumes has no case for it), so door clearance ignores it.
        public const int InverterMinDurationTicks = 25, InverterMaxDurationTicks = 500;

        internal static bool IsInverter(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Inverter;

        public static List<string> ValidateInverter(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            SoloRoomElement[] inverters = room.Elements.Where(IsInverter).ToArray();
            if (inverters.Length == 0) return errors;
            (float minY, float maxY) = VerticalBounds(room);
            foreach (SoloRoomElement e in inverters)
            {
                int duration = e.Settings.Inverter.Duration;
                if (duration < InverterMinDurationTicks || duration > InverterMaxDurationTicks)
                    errors.Add($"{levelId}: {e.Name} duration {duration} ticks is outside {InverterMinDurationTicks}-{InverterMaxDurationTicks} (D-087).");
                if (e.Settings.IsConfigured && e.Settings.RepeatMode == TrapRepeatMode.Periodic)
                    errors.Add($"{levelId}: {e.Name} is an inverter set to Periodic; an inverter is Once or Rearm (D-087).");
                if (e.Size.x <= 0f || e.Size.y <= 0f)
                    errors.Add($"{levelId}: {e.Name} has no box; an inverter's body is its trigger or its look.");
                // The frame containment rule (ValidateFrameContainment) doesn't list inverters, so it's checked here.
                CheckFrame(levelId, e.Name, Box(e, Vector2.zero), room.Width, minY, maxY, errors);
                if (e.SecondarySize != Vector2.zero)
                    CheckFrame(levelId, e.Name + " trigger", new Rect(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize), room.Width, minY, maxY, errors);
            }
            return errors;
        }
    }
}
