using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-076 (D-083, KIT-4): precision sections (D-069), the difficulty band (D-065) and bait gaps. A room that
    // declares no section and no bait gap gets exactly D-056's results from the rules in LevelLayoutValidator.cs.
    public static partial class LevelLayoutValidator
    {
        // D-065: levels 1-10 are the easy band; precision starts at level 11.
        public const int EasyBandLastLevel = 10;
        public const string LevelListPath = "Assets/_Game/Data/LevelListConfig.asset";

        // Inside a section: reach and slack from PrecisionThresholds (D-069 (2)(a)); leads stay at 6 (D-057).
        // A jump is a precision jump when both its take-off and its landing are in one section; a jump with one end in
        // (the entry and exit jumps) keeps D-056's 0.75. A periodic trap or arrow is inside only when its footprint
        // (an arrow: its lane) is wholly inside; one partly inside keeps D-056's 12.
        public static List<string> ValidatePrecision(string levelId, SoloRoomDefinition room, CatMotorConfig config, float gravity, PrecisionThresholds thresholds)
        {
            var errors = new List<string>();
            if (!HasSections(room)) return errors;
            if (thresholds == null) { errors.Add($"{levelId}: precision sections need a PrecisionThresholds asset ({PrecisionThresholdsSetup.AssetPath}); run PARALLAX/Setup/Precision Thresholds (PAX-076) (D-083)."); return errors; }
            if (thresholds.ReachFraction < RequiredJumpReachFraction || thresholds.ReachFraction >= 1f || thresholds.SlackTicks <= 0 || thresholds.SlackTicks > PeriodicSlackTicks)
            { errors.Add($"{levelId}: PrecisionThresholds (reach {thresholds.ReachFraction}, slack {thresholds.SlackTicks}) is outside 0.75 <= reach < 1 and 0 < slack <= 12 (D-083)."); return errors; }
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; precision reach and slack are undefined."); return errors; }
            foreach (PrecisionSection section in room.PrecisionSections)
                if (string.IsNullOrEmpty(section.Name) || section.Region.width <= 0f || section.Region.height <= 0f)
                    errors.Add($"{levelId}: precision section '{section.Name}' has no name or an empty region.");

            if (gravity > 0f)
            {
                var byName = room.Elements.ToDictionary(e => e.Name);
                foreach (RequiredJump jump in room.RequiredJumps.Where(j => IsPrecisionJump(room, j)))
                    CheckJump(levelId, room, byName, jump, config, gravity, thresholds.ReachFraction, errors);
            }
            if (config.Acceleration > 0f && config.MaxSpeed > 0f)
            {
                foreach (SoloRoomElement trap in room.Elements)
                {
                    if (!trap.Settings.IsConfigured || trap.Settings.RepeatMode != TrapRepeatMode.Periodic) continue;
                    if (IsArrow(trap)) { if (WhollyInSection(room, ArrowLaneBox(trap, Vector2.zero))) CheckArrowPeriodicSlack(levelId, trap, config, thresholds.SlackTicks, errors); }
                    else if (WhollyInSection(room, Box(trap, Vector2.zero))) CheckPeriodicSlack(levelId, trap, config, thresholds.SlackTicks, errors);
                }
            }
            return errors;
        }

        // D-065: a precision section in a level numbered 1-10 is an error. The number is the level's place in
        // LevelListConfig (D-063) plus one; an id that isn't listed (the Trap Lab, fixtures) isn't a numbered level
        // and is exempt. LevelLayoutTests keeps every shipped layout listed, and NoDevRoomIsAListedLevel keeps the
        // Trap Lab out of the list.
        // PAX-084 (D-086): the shared "levels 11+ only" check. Each kit mechanic for levels 11+ adds its kind here
        // (spears first; KIT-6-KIT-9 extend it). PAX-085 (D-087): inverters. PAX-086 (D-088): geysers. PAX-087 (D-089): vines.
        public static List<string> ValidateBand(string levelId, SoloRoomDefinition room, LevelListConfig levels)
        {
            var errors = new List<string>();
            SoloRoomElement[] spears = room.Elements.Where(IsSpear).ToArray();
            SoloRoomElement[] inverters = room.Elements.Where(IsInverter).ToArray();
            SoloRoomElement[] geysers = room.Elements.Where(IsGeyser).ToArray();
            SoloRoomElement[] vines = room.Elements.Where(IsVine).ToArray();
            if (!HasSections(room) && spears.Length == 0 && inverters.Length == 0 && geysers.Length == 0 && vines.Length == 0) return errors;
            if (levels == null) { errors.Add($"{levelId}: no LevelListConfig ({LevelListPath}); the band (D-065) is undefined."); return errors; }
            int number = LevelNumber(levels, levelId);
            if (number <= 0 || number > EasyBandLastLevel) return errors;
            if (HasSections(room))
                errors.Add($"{levelId}: precision section '{room.PrecisionSections[0].Name}' in level {number}; levels 1-{EasyBandLastLevel} are the easy band and allow no precision (D-065).");
            foreach (SoloRoomElement spear in spears)
                errors.Add($"{levelId}: spear '{spear.Name}' in level {number}; spears are for levels {EasyBandLastLevel + 1}+ only (D-086).");
            foreach (SoloRoomElement inverter in inverters)
                errors.Add($"{levelId}: inverter '{inverter.Name}' in level {number}; inverters are for levels {EasyBandLastLevel + 1}+ only (D-087).");
            foreach (SoloRoomElement geyser in geysers)
                errors.Add($"{levelId}: geyser '{geyser.Name}' in level {number}; geysers are for levels {EasyBandLastLevel + 1}+ only (D-088).");
            foreach (SoloRoomElement vine in vines)
                errors.Add($"{levelId}: vine '{vine.Name}' in level {number}; vines are for levels {EasyBandLastLevel + 1}+ only (D-089).");
            return errors;
        }

        // 1-based place in LevelListConfig, or 0 when the id isn't listed.
        public static int LevelNumber(LevelListConfig levels, string levelId)
        {
            IReadOnlyList<string> ids = levels.OrderedIds();
            for (int i = 0; i < ids.Count; i++) if (ids[i] == levelId) return i + 1;
            return 0;
        }

        // §2.7: from the best take-off (full speed, trailing side at the edge, every coyote tick used), at fraction
        // 1.0, the target stays out of reach by at least one run tick of distance (MaxSpeed x tick, as D-075 (5)).
        public static List<string> ValidateBaitGaps(string levelId, SoloRoomDefinition room, CatMotorConfig config, float gravity)
        {
            var errors = new List<string>();
            if (room.BaitGaps == null || room.BaitGaps.Length == 0) return errors;
            if (config == null || gravity <= 0f) { errors.Add($"{levelId}: no CatMotorConfig or gravity; bait gaps can't be proven (D-083)."); return errors; }
            foreach (BaitGap gap in room.BaitGaps)
            {
                BaitGapReach(gap, config, gravity, out float reach, out float distance, out float margin);
                if (float.IsInfinity(reach)) continue;
                if (distance - reach < margin)
                    errors.Add($"{levelId}: bait gap {gap.Name} can be crossed: best take-off reach {reach:F2} u against a gap of {distance:F2} u (margin {distance - reach:F2} u, needs at least {margin:F2} u, one run tick) (D-083).");
            }
            return errors;
        }

        // reach is +infinity when the target is higher than the jump can rise. margin is one run tick of distance.
        public static void BaitGapReach(BaitGap gap, CatMotorConfig config, float gravity, out float reach, out float distance, out float margin)
        {
            distance = Mathf.Abs(gap.TargetX - gap.TakeoffX);
            margin = config.MaxSpeed * TickTime.SecondsPerTick;
            float vy = JumpReach.LaunchSpeed(gravity, config.JumpHeight);
            float deltaHeight = gap.TargetPawHeight - gap.TakeoffPawHeight;
            if (!JumpReach.CanRise(vy, gravity, deltaHeight)) { reach = float.PositiveInfinity; return; }
            // D-077: the motor's coyote window is whole ticks.
            float coyote = TickTime.ToSeconds(TickTime.ToWholeTicks(config.CoyoteTime));
            reach = JumpReach.EdgeReach(config.MaxSpeed, JumpReach.Flight(vy, gravity, deltaHeight), coyote, config.ColliderSize.x);
        }

        internal static PrecisionThresholds LoadPrecisionThresholds() => AssetDatabase.LoadAssetAtPath<PrecisionThresholds>(PrecisionThresholdsSetup.AssetPath);
        static LevelListConfig LoadLevelList() => AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListPath);

        internal static bool HasSections(SoloRoomDefinition room) => room.PrecisionSections != null && room.PrecisionSections.Length > 0;

        internal static bool IsPrecisionJump(SoloRoomDefinition room, RequiredJump jump)
        {
            if (!HasSections(room)) return false;
            var takeoff = new Vector2(jump.TakeoffX, jump.TakeoffPawHeight);
            var landing = new Vector2(jump.LandingX, jump.LandingPawHeight);
            foreach (PrecisionSection s in room.PrecisionSections) if (s.Contains(takeoff) && s.Contains(landing)) return true;
            return false;
        }

        internal static bool WhollyInSection(SoloRoomDefinition room, Rect box)
        {
            if (!HasSections(room)) return false;
            foreach (PrecisionSection s in room.PrecisionSections) if (s.Contains(box)) return true;
            return false;
        }

        internal static bool InSection(SoloRoomDefinition room, Vector2 point, out PrecisionSection section)
        {
            section = default;
            if (!HasSections(room)) return false;
            foreach (PrecisionSection s in room.PrecisionSections) if (s.Contains(point)) { section = s; return true; }
            return false;
        }
    }
}
