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

        // D-117 (the developer, 2026-10-06): L004 is played inverted from the start to the end; its inverter is exempt from the
        // duration cap and from the levels-11+ band (ValidateBand).
        // PAX-107 (the developer: "In level 9 the cat starts with arrow directions inverted until it reaches the gravity inverter
        // in the bottom right of the map"): L009's, from the start to Flip_4 (its end box).
        // PAX-107 (D-141, the developer: "arrow inverter at the begining of this level 13"; "the ground floor"): L013's, from
        // the start to G_1's launch (its end box).
        // PAX-107 (D-150): L020's Inv_2, from G_1's launch to its end box at S2's turn (Flip_D2).
        static readonly string[] WholeLevelInverters = { "L004/Invert", "L009/Invert", "L013/Invert", "L020/Inv_2" };
        internal static bool IsWholeLevelInverter(string levelId, SoloRoomElement e) => System.Array.IndexOf(WholeLevelInverters, $"{levelId}/{e.Name}") >= 0;

        // D-126 (the developer: "if you fall through we automatically invert your arrow direction, to force a death"): L006's
        // hidden inverter in the drop beside S1 closes a shortcut; it is exempt from the levels-11+ band, not the duration cap.
        static readonly string[] BandOneInverters = { "L006/Invert_Drop" };
        internal static bool IsBandExemptInverter(string levelId, SoloRoomElement e) =>
            IsWholeLevelInverter(levelId, e) || System.Array.IndexOf(BandOneInverters, $"{levelId}/{e.Name}") >= 0;

        public static List<string> ValidateInverter(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            SoloRoomElement[] inverters = room.Elements.Where(IsInverter).ToArray();
            if (inverters.Length == 0) return errors;
            (float minY, float maxY) = VerticalBounds(room);
            foreach (SoloRoomElement e in inverters)
            {
                int duration = e.Settings.Inverter.Duration;
                if ((duration < InverterMinDurationTicks || duration > InverterMaxDurationTicks) && !IsWholeLevelInverter(levelId, e))
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
