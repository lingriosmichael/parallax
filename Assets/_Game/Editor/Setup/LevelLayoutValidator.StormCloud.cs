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

            // The range: the authored pose on it (D-116: on one of its floors), each floor's swept cloud inside the frame and
            // clear of static elements, a static top under every column pose. D-116: the climb between floors may pass a slab.
            Rect strikes;
            if (!c.HasFloors)
            {
                if (c.MinX > c.MaxX || e.Position.x < c.MinX || e.Position.x > c.MaxX)
                    errors.Add($"{levelId}: {e.Name} authored x {e.Position.x:F2} is outside its range [{c.MinX:F2}, {c.MaxX:F2}] (D-090).");
                strikes = CheckCloudRange(levelId, room, e, e.Name, e.Position.y, c.MinX, c.MaxX, errors);
            }
            else
            {
                bool onAFloor = false;
                for (int i = 0; i < c.Floors.Length; i++)
                {
                    StormFloor f = c.Floors[i];
                    if (i > 0 && f.CatMinY <= c.Floors[i - 1].CatMinY) errors.Add($"{levelId}: {e.Name}'s floors aren't in ascending order of the cat's height (D-116).");
                    if (f.MinX > f.MaxX) errors.Add($"{levelId}: {e.Name}'s floor {i} range [{f.MinX:F2}, {f.MaxX:F2}] is empty (D-116).");
                    if (Mathf.Abs(e.Position.y - f.CloudY) < 1e-3f && e.Position.x >= f.MinX && e.Position.x <= f.MaxX) onAFloor = true;
                    CheckCloudRange(levelId, room, e, $"{e.Name}'s floor {i}", f.CloudY, f.MinX, f.MaxX, errors);
                }
                if (!onAFloor) errors.Add($"{levelId}: {e.Name}'s authored pose ({e.Position.x:F2}, {e.Position.y:F2}) is on none of its floors (D-116).");
                strikes = c.Floors.Select(f => CheckCloudStrikes(room, e, f.CloudY, f.MinX, f.MaxX)).Aggregate(Envelope);
                // D-060 per floor: one box around every floor's strikes would cover a door no floor's strikes reach.
                foreach (StormFloor f in c.Floors) CheckStormCloudDoorClearance(levelId, room, e, CheckCloudStrikes(room, e, f.CloudY, f.MinX, f.MaxX), config, errors);
            }

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

            // D-060: reaching the door is never a lightning test (D-116: with floors, checked per floor above).
            if (!c.HasFloors) CheckStormCloudDoorClearance(levelId, room, e, strikes, config, errors);
        }

        // One height's range (the cloud's, or one D-116 floor's): the swept cloud inside the frame and clear of static elements,
        // a static top under every column pose; returns the strikes' envelope over it.
        static Rect CheckCloudRange(string levelId, SoloRoomDefinition room, SoloRoomElement e, string what, float y, float minX, float maxX, List<string> errors)
        {
            float hw = e.Settings.Cloud.StrikeWidth * .5f, cloudBottom = y - e.Size.y * .5f;
            List<Vector3> tops = StrikeProfile(room).Where(t => t.z <= cloudBottom).ToList();
            Rect sweep = Rect.MinMaxRect(minX - e.Size.x * .5f, cloudBottom, maxX + e.Size.x * .5f, y + e.Size.y * .5f);
            (float minY, float maxY) = VerticalBounds(room);
            CheckFrame(levelId, $"{what}'s range", sweep, room.Width, minY, maxY, errors);
            foreach (SoloRoomElement solid in room.Elements.Where(IsFixedSolid))
                if (Box(solid, Vector2.zero).Overlaps(sweep)) errors.Add($"{levelId}: {what}'s range {Describe(sweep)} overlaps {solid.Name}; a cloud flies clear of static geometry (D-090).");
            if (!CoversRange(tops, minX, maxX, hw, out float gap))
                errors.Add($"{levelId}: {what} has no static top under its column at x {gap:F2}; every x in [{minX:F2}, {maxX:F2}] needs one (D-090).");
            return CheckCloudStrikes(room, e, y, minX, maxX);
        }

        // The strikes' envelope over a range at one height, from the lowest top any column pose can end on.
        static Rect CheckCloudStrikes(SoloRoomDefinition room, SoloRoomElement e, float y, float minX, float maxX)
        {
            float hw = e.Settings.Cloud.StrikeWidth * .5f, cloudBottom = y - e.Size.y * .5f;
            float lowest = StrikeProfile(room).Where(t => t.z <= cloudBottom && t.x < maxX + hw && t.y > minX - hw).Select(t => t.z).DefaultIfEmpty(cloudBottom).Min();
            return Rect.MinMaxRect(minX - hw, lowest, maxX + hw, cloudBottom);
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
