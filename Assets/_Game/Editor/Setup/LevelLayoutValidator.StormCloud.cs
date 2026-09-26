using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-088 (D-090): the storm cloud ----------
        // Separately named, not part of Validate(), like ValidateGeyser. The level band (levels 11+ only) is ValidateBand's.
        // A cloud is never a KillVolumes entry (TriggerCoverage is untouched); its door clearance is checked here. Its cycle
        // isn't TrapTiming's Periodic, so ValidatePeriodicSlack never sees it: the dodge rule replaces it (§11).

        internal static bool IsStormCloud(SoloRoomElement e) => e.Kind == SoloRoomElementKind.StormCloud;

        /// <summary>Every static top in the room as (xMin, xMax, top), room-local: the IsFixedSolid elements (Floor, Wall,
        /// PitBottom, Ceiling). Fake platforms, collapsing floors, moving Solids, falling blocks and spears don't block lightning
        /// (§11 Q3). The builder bakes this onto the trap.</summary>
        public static Vector3[] StrikeProfile(SoloRoomDefinition room) =>
            room.Elements.Where(IsFixedSolid).Select(e => { Rect r = Box(e, Vector2.zero); return new Vector3(r.xMin, r.xMax, r.yMax); }).ToArray();

        public static List<string> ValidateStormCloud(string levelId, SoloRoomDefinition room, CatMotorConfig config)
        {
            var errors = new List<string>();
            SoloRoomElement[] clouds = room.Elements.Where(IsStormCloud).ToArray();
            if (clouds.Length == 0) return errors;
            if (clouds.Length > 1) errors.Add($"{levelId}: {clouds.Length} storm clouds; a room has at most one (D-090).");
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; a storm cloud's dodge rule is undefined."); return errors; }
            foreach (SoloRoomElement e in clouds) CheckStormCloud(levelId, room, e, config, errors);
            return errors;
        }

        static void CheckStormCloud(string levelId, SoloRoomDefinition room, SoloRoomElement e, CatMotorConfig config, List<string> errors)
        {
            StormCloudSettings c = e.Settings.Cloud;
            if (!c.IsConfigured) { errors.Add($"{levelId}: {e.Name} has no StormCloudSettings (D-090)."); return; }
            if (e.Settings.RepeatMode != TrapRepeatMode.Once) errors.Add($"{levelId}: {e.Name} repeat mode {e.Settings.RepeatMode}; a storm cloud wakes Once (D-090).");

            // Ruling B: the cycle.
            if (c.StrikeTicks < 1) errors.Add($"{levelId}: {e.Name} strike {c.StrikeTicks} ticks is below 1 (D-090).");
            if (c.FirstStrikeDelay < 1) errors.Add($"{levelId}: {e.Name} first strike delay {c.FirstStrikeDelay} is below 1 (D-090).");
            if (c.StrikePeriod < c.TellTicks + c.StrikeTicks + 1)
                errors.Add($"{levelId}: {e.Name} period {c.StrikePeriod} is shorter than tell {c.TellTicks} + strike {c.StrikeTicks} + 1 = {c.TellTicks + c.StrikeTicks + 1}; the cloud must move between strikes (D-090).");

            float hw = c.StrikeWidth * .5f, cloudBottom = e.Position.y - e.Size.y * .5f;
            List<Vector3> tops = StrikeProfile(room).Where(t => t.z <= cloudBottom).ToList();

            // The range: the authored x inside it, the swept cloud inside the frame and clear of static elements.
            if (c.MinX > c.MaxX || e.Position.x < c.MinX || e.Position.x > c.MaxX)
                errors.Add($"{levelId}: {e.Name} authored x {e.Position.x:F2} is outside its range [{c.MinX:F2}, {c.MaxX:F2}] (D-090).");
            Rect sweep = Rect.MinMaxRect(c.MinX - e.Size.x * .5f, cloudBottom, c.MaxX + e.Size.x * .5f, e.Position.y + e.Size.y * .5f);
            (float minY, float maxY) = VerticalBounds(room);
            CheckFrame(levelId, $"{e.Name}'s range", sweep, room.Width, minY, maxY, errors);
            foreach (SoloRoomElement solid in room.Elements.Where(IsFixedSolid))
                if (Box(solid, Vector2.zero).Overlaps(sweep)) errors.Add($"{levelId}: {e.Name}'s range {Describe(sweep)} overlaps {solid.Name}; a cloud flies clear of static geometry (D-090).");

            // A static top under every column pose (ruling A: a top counts where it overlaps any part of the width).
            if (!CoversRange(tops, c.MinX, c.MaxX, hw, out float gap))
                errors.Add($"{levelId}: {e.Name} has no static top under its column at x {gap:F2}; every x in [{c.MinX:F2}, {c.MaxX:F2}] needs one (D-090).");

            // The strikes' envelope over the whole range, from the lowest top any column pose can end on.
            float lowest = tops.Where(t => t.x < c.MaxX + hw && t.y > c.MinX - hw).Select(t => t.z).DefaultIfEmpty(cloudBottom).Min();
            Rect strikes = Rect.MinMaxRect(c.MinX - hw, lowest, c.MaxX + hw, cloudBottom);

            // §2.4 dodge rule: from rest under the column's centre, clear of it within the tell, plus the slack.
            float distance = StormCloudMath.ClearDistance(c.StrikeWidth, config.ColliderSize.x);
            float crossing = FromRestCrossingTicks(distance, config);
            int slack = PeriodicSlackTicks;
            if (WhollyInSection(room, strikes))
            {
                var thresholds = AssetDatabase.LoadAssetAtPath<PrecisionThresholds>(PrecisionThresholdsSetup.AssetPath);
                if (thresholds == null) { errors.Add($"{levelId}: {e.Name} is in a precision section but there's no PrecisionThresholds asset ({PrecisionThresholdsSetup.AssetPath}) (D-083)."); return; }
                slack = thresholds.SlackTicks;
            }
            int required = Mathf.CeilToInt(crossing - 1e-3f) + slack;
            if (c.TellTicks < required)
                errors.Add($"{levelId}: {e.Name} tell {c.TellTicks} ticks is below {required}: {crossing:F1} ticks from rest to clear {distance:F2} u, plus {slack} slack (D-056 (1), D-090).");

            // D-060: reaching the door is never a lightning test.
            CheckStormCloudDoorClearance(levelId, room, e, strikes, config, errors);
        }

        static void CheckStormCloudDoorClearance(string levelId, SoloRoomDefinition room, SoloRoomElement e, Rect strikes, CatMotorConfig config, List<string> errors)
        {
            SoloRoomElement? doorOpt = room.Elements.Where(x => x.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (!doorOpt.HasValue) return;
            Rect door = Box(doorOpt.Value, Vector2.zero);
            SoloRoomElement? retreatOpt = room.Elements.Where(x => x.Kind == SoloRoomElementKind.DoorRetreat).Cast<SoloRoomElement?>().FirstOrDefault();
            if (retreatOpt.HasValue) { Rect retreated = door; retreated.position += retreatOpt.Value.Settings.Offset; door = Envelope(door, retreated); }
            float margin = config.MaxSpeed * TickTime.SecondsPerTick;
            if (Grow(strikes, margin).Overlaps(door))
                errors.Add($"{levelId}: the door is within {margin:F2} u of {e.Name}'s strikes {Describe(strikes)} over its range (D-060, D-090).");
        }

        // Every x in [minX, maxX] lies strictly inside some (xMin - hw, xMax + hw); gap is the first x that doesn't.
        static bool CoversRange(List<Vector3> tops, float minX, float maxX, float hw, out float gap)
        {
            float reach = minX;
            while (true)
            {
                float best = reach;
                foreach (Vector3 t in tops)
                    if (t.x - hw < reach && t.y + hw > reach && t.y + hw > best) best = t.y + hw;
                if (best == reach) { gap = reach; return false; }
                if (best > maxX) { gap = float.NaN; return true; }
                reach = best;
            }
        }
    }
}
