using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-075 §2.6: the PAX-074 arrow rules, moved unchanged out of LevelLayoutValidator.cs. Same type
    // (a partial class), same method names, so reflection lookups are unaffected.
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-074 (D-078): arrows ----------
        // Six separately named rules, none part of Validate(). Lane geometry is shared with
        // TriggerCoverage and SoloRoomBuilder through the internal helpers below; KillVolumes is unchanged.

        internal static bool IsArrow(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Arrow;
        internal static float ArrowMouthX(SoloRoomElement e) => e.Position.x + ArrowMath.Sign(e.Settings.Arrow.Direction) * e.Size.x * .5f;
        internal static float ArrowTravel(SoloRoomElement e) => ArrowMath.Travel(ArrowMouthX(e), e.Settings.Arrow.LaneEndX, e.Settings.Arrow.Length, e.Settings.Arrow.AngleDegrees);
        internal static int ArrowFlightTicks(SoloRoomElement e) => ArrowMath.FlightTicks(ArrowTravel(e), e.Settings.Arrow.UnitsPerTick);
        // Tell plus every lethal tick: the arrow is stopped (harmless) from this many ticks after its fire.
        internal static int ArrowStopTick(SoloRoomElement e) => e.Settings.Arrow.TellTicks + ArrowFlightTicks(e) + 1;

        // The lane box: from the mouth to the lane end, LaneY +/- Thickness / 2, in origin + room-local space. PAX-099
        // (D-106): for an angled lane, the axis-aligned box around the turned lane (ArrowLaneCorners).
        internal static Rect ArrowLaneBox(SoloRoomElement e, Vector2 origin)
        {
            ArrowLane lane = e.Settings.Arrow;
            float mouth = ArrowMouthX(e);
            if (lane.AngleDegrees == 0f)
                return Rect.MinMaxRect(origin.x + Mathf.Min(mouth, lane.LaneEndX), origin.y + lane.LaneY - lane.Thickness * .5f,
                    origin.x + Mathf.Max(mouth, lane.LaneEndX), origin.y + lane.LaneY + lane.Thickness * .5f);
            Vector2[] c = ArrowLaneCorners(e, origin, 0f);
            return Rect.MinMaxRect(c.Min(p => p.x), c.Min(p => p.y), c.Max(p => p.x), c.Max(p => p.y));
        }

        // PAX-099 (D-106): the turned lane's corners, from the mouth to the tip's stop point, Thickness wide; `trim` cuts that
        // much off each end along the lane (the blocked test trims the ends where the lane meets its launcher and its end face).
        internal static Vector2[] ArrowLaneCorners(SoloRoomElement e, Vector2 origin, float trim)
        {
            ArrowLane lane = e.Settings.Arrow;
            Vector2 d = ArrowMath.Direction(lane.Direction, lane.AngleDegrees), n = new Vector2(-d.y, d.x) * (lane.Thickness * .5f);
            Vector2 start = origin + new Vector2(ArrowMouthX(e), lane.LaneY) + d * trim;
            Vector2 end = origin + ArrowTipStop(e) - d * trim;
            return new[] { start - n, end - n, end + n, start + n };
        }

        // The tip's centre where the arrow stops (room-local): x = LaneEndX on the lane line.
        internal static Vector2 ArrowTipStop(SoloRoomElement e)
        {
            ArrowLane lane = e.Settings.Arrow;
            float mouth = ArrowMouthX(e);
            return new Vector2(lane.LaneEndX, lane.LaneY + (lane.LaneEndX - mouth) * ArrowMath.Sign(lane.Direction) * Mathf.Tan(lane.AngleDegrees * Mathf.Deg2Rad));
        }

        // Separating axes: a convex quad against an axis-aligned rect, strictly (touching is not overlapping, as Rect.Overlaps).
        static bool QuadOverlapsRect(Vector2[] quad, Rect r)
        {
            if (quad.Max(p => p.x) <= r.xMin || quad.Min(p => p.x) >= r.xMax || quad.Max(p => p.y) <= r.yMin || quad.Min(p => p.y) >= r.yMax) return false;
            Vector2[] rect = { new(r.xMin, r.yMin), new(r.xMax, r.yMin), new(r.xMax, r.yMax), new(r.xMin, r.yMax) };
            for (int i = 0; i < 2; i++)
            {
                Vector2 edge = quad[i + 1] - quad[i], axis = new(-edge.y, edge.x);
                float qMin = quad.Min(p => Vector2.Dot(p, axis)), qMax = quad.Max(p => Vector2.Dot(p, axis));
                float rMin = rect.Min(p => Vector2.Dot(p, axis)), rMax = rect.Max(p => Vector2.Dot(p, axis));
                if (qMax <= rMin || rMax <= qMin) return false;
            }
            return true;
        }

        // D-057: the tell is the arrow's visible lead.
        public static List<string> ValidateArrowTell(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            foreach (SoloRoomElement e in room.Elements.Where(IsArrow))
                if (e.Settings.Arrow.TellTicks < RevealLeadTicks)
                    errors.Add($"{levelId}: {e.Name} tell {e.Settings.Arrow.TellTicks} is below {RevealLeadTicks} ticks (D-057).");
            return errors;
        }

        // No pass-through between ticks: v <= Length + (collider width - height) - 2 x run per tick. PAX-099 (D-106): an angled
        // lane closes at run x cos a + vertical x |sin a| (vertical = the larger of the jump launch and the fall cap).
        public static List<string> ValidateArrowSpeed(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            if (!room.Elements.Any(IsArrow)) return errors;
            CatMotorConfig config = Config();
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; the arrow speed cap is undefined."); return errors; }
            const float tolerance = 1e-4f;
            foreach (SoloRoomElement e in room.Elements.Where(IsArrow))
            {
                ArrowLane lane = e.Settings.Arrow;
                if (lane.Length <= 0f || lane.Thickness <= 0f) { errors.Add($"{levelId}: {e.Name} arrow size {lane.Length:F2} x {lane.Thickness:F2} must be positive."); continue; }
                float vertical = Mathf.Max(JumpMath.SpeedForHeight(config.JumpHeight, GravityStrength()), config.MaxFallSpeed) * TickTime.SecondsPerTick;
                float cap = ArrowMath.MaxUnitsPerTick(lane.Length, config.ColliderSize, config.MaxSpeed * TickTime.SecondsPerTick, vertical, lane.AngleDegrees);
                if (lane.UnitsPerTick <= 0f || lane.UnitsPerTick > cap + tolerance)
                    errors.Add($"{levelId}: {e.Name} speed {lane.UnitsPerTick:F2} u/tick is outside (0, {cap:F2}], the tunnelling cap for a {lane.Length:F2} arrow.");
            }
            return errors;
        }

        // The launcher is hosted in fixed geometry, the lane ends at a fixed face (or the room's end)
        // covering its band, nothing solid crosses it, and launcher, trigger and lane stay in the frame.
        public static List<string> ValidateArrowLane(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            const float eps = 1e-3f;
            (float minY, float maxY) = VerticalBounds(room);
            SoloRoomElement[] fixedSolids = room.Elements.Where(IsFixedSolid).ToArray();
            foreach (SoloRoomElement e in room.Elements.Where(IsArrow))
            {
                ArrowLane lane = e.Settings.Arrow;
                if (!lane.IsConfigured) { errors.Add($"{levelId}: {e.Name} is an arrow with no ArrowLane."); continue; }
                if (!ArrowMath.IsAllowedAngle(lane.AngleDegrees)) { errors.Add($"{levelId}: {e.Name} angle {lane.AngleDegrees:0.##} is not one of 0, ±30, ±45, ±60 (D-106)."); continue; }
                if (lane.Spear && lane.AngleDegrees != 0f) { errors.Add($"{levelId}: {e.Name} is a spear at {lane.AngleDegrees:0.##} degrees; spears fly level (D-106)."); continue; }
                if (lane.AngleDegrees != 0f) { CheckAngledLane(levelId, room, e, fixedSolids, minY, maxY, errors); continue; }
                float mouth = ArrowMouthX(e), sign = ArrowMath.Sign(lane.Direction);
                if ((lane.LaneEndX - mouth) * sign <= 0f || ArrowTravel(e) <= 0f)
                { errors.Add($"{levelId}: {e.Name} lane end x {lane.LaneEndX:F2} is not beyond its mouth x {mouth:F2} by more than the arrow length."); continue; }

                Rect launcherBox = Box(e, Vector2.zero), laneBox = ArrowLaneBox(e, Vector2.zero);
                if (!fixedSolids.Any(s => Box(s, Vector2.zero).Overlaps(launcherBox)))
                    errors.Add($"{levelId}: {e.Name} launcher {Describe(launcherBox)} is not hosted in fixed geometry.");

                bool roomEnd = Mathf.Abs(lane.LaneEndX) < eps || Mathf.Abs(lane.LaneEndX - room.Width) < eps;
                bool endFace = roomEnd || fixedSolids.Select(s => Box(s, Vector2.zero)).Any(r =>
                    Mathf.Abs((sign > 0f ? r.xMin : r.xMax) - lane.LaneEndX) < eps && r.yMin <= laneBox.yMin + eps && r.yMax >= laneBox.yMax - eps);
                if (!endFace) errors.Add($"{levelId}: {e.Name} lane end x {lane.LaneEndX:F2} is not a fixed solid's end face covering y [{laneBox.yMin:F2}, {laneBox.yMax:F2}].");

                foreach (SoloRoomElement s in fixedSolids)
                    if (Box(s, Vector2.zero).Overlaps(laneBox)) errors.Add($"{levelId}: {e.Name} lane {Describe(laneBox)} is blocked by {s.Name}.");
                foreach (SoloRoomElement s in room.Elements)
                {
                    bool movingSolid = s.Kind == SoloRoomElementKind.MovingTrap && s.Settings.MovingKind == MovingTrapKind.Solid;
                    if (!movingSolid && s.Kind != SoloRoomElementKind.CollapsingFloor) continue;
                    Rect swept = Box(s, Vector2.zero);
                    if (movingSolid) { Rect moved = swept; moved.position += s.Settings.Offset; swept = Envelope(swept, moved); }
                    if (swept.Overlaps(laneBox)) errors.Add($"{levelId}: {e.Name} lane {Describe(laneBox)} crosses {s.Name}'s swept path; a pushed cat breaks the speed cap.");
                }

                CheckFrame(levelId, e.Name + " launcher", launcherBox, room.Width, minY, maxY, errors);
                CheckFrame(levelId, e.Name + " lane", laneBox, room.Width, minY, maxY, errors);
                if (e.SecondarySize != Vector2.zero)
                    CheckFrame(levelId, e.Name + " trigger", new Rect(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize), room.Width, minY, maxY, errors);
            }
            return errors;
        }

        // PAX-099 (D-106): an angled lane leaves its launcher's side face and its tip stops on a fixed solid's face (or the
        // room's end). The turned lane, trimmed at both ends by where its thickness meets those faces, crosses no fixed solid;
        // the swept-path and frame checks use the box around it.
        static void CheckAngledLane(string levelId, SoloRoomDefinition room, SoloRoomElement e, SoloRoomElement[] fixedSolids, float minY, float maxY, List<string> errors)
        {
            const float eps = 1e-3f;
            ArrowLane lane = e.Settings.Arrow;
            float mouth = ArrowMouthX(e);
            if ((lane.LaneEndX - mouth) * ArrowMath.Sign(lane.Direction) <= 0f || ArrowTravel(e) <= 0f)
            { errors.Add($"{levelId}: {e.Name} lane end x {lane.LaneEndX:F2} is not beyond its mouth x {mouth:F2} by more than the arrow length."); return; }
            Rect launcherBox = Box(e, Vector2.zero), laneBox = ArrowLaneBox(e, Vector2.zero);
            if (!fixedSolids.Any(s => Box(s, Vector2.zero).Overlaps(launcherBox)))
                errors.Add($"{levelId}: {e.Name} launcher {Describe(launcherBox)} is not hosted in fixed geometry.");

            Vector2 tip = ArrowTipStop(e);
            bool roomEnd = Mathf.Abs(tip.x) < eps || Mathf.Abs(tip.x - room.Width) < eps;
            bool endFace = roomEnd || fixedSolids.Select(s => Box(s, Vector2.zero)).Any(r => DistanceToRect(tip, r) < eps);
            if (!endFace) errors.Add($"{levelId}: {e.Name} lane's tip stops at ({tip.x:F2}, {tip.y:F2}), not on a fixed solid's end face.");

            float a = Mathf.Abs(lane.AngleDegrees) * Mathf.Deg2Rad;
            float trim = lane.Thickness * .5f * Mathf.Max(Mathf.Tan(a), 1f / Mathf.Tan(a)) + eps;
            Vector2[] trimmed = ArrowLaneCorners(e, Vector2.zero, trim);
            foreach (SoloRoomElement s in fixedSolids)
                if (QuadOverlapsRect(trimmed, Box(s, Vector2.zero))) errors.Add($"{levelId}: {e.Name} lane {Describe(laneBox)} is blocked by {s.Name}.");
            foreach (SoloRoomElement s in room.Elements)
            {
                bool movingSolid = s.Kind == SoloRoomElementKind.MovingTrap && s.Settings.MovingKind == MovingTrapKind.Solid;
                if (!movingSolid && s.Kind != SoloRoomElementKind.CollapsingFloor) continue;
                Rect swept = Box(s, Vector2.zero);
                if (movingSolid) { Rect moved = swept; moved.position += s.Settings.Offset; swept = Envelope(swept, moved); }
                if (QuadOverlapsRect(ArrowLaneCorners(e, Vector2.zero, 0f), swept)) errors.Add($"{levelId}: {e.Name} lane {Describe(laneBox)} crosses {s.Name}'s swept path; a pushed cat breaks the speed cap.");
            }
            // The frame takes the trimmed lane: the untrimmed corners dip into the end face by design.
            Rect framed = Rect.MinMaxRect(trimmed.Min(p => p.x), trimmed.Min(p => p.y), trimmed.Max(p => p.x), trimmed.Max(p => p.y));
            CheckFrame(levelId, e.Name + " launcher", launcherBox, room.Width, minY, maxY, errors);
            CheckFrame(levelId, e.Name + " lane", framed, room.Width, minY, maxY, errors);
            if (e.SecondarySize != Vector2.zero)
                CheckFrame(levelId, e.Name + " trigger", new Rect(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize), room.Width, minY, maxY, errors);
        }

        static float DistanceToRect(Vector2 p, Rect r) =>
            new Vector2(Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax), Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax)).magnitude;

        // D-060/D-075: the door's authored pose and retreat sweep stay one tick at run speed clear of every lane.
        public static List<string> ValidateArrowDoorClearance(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            SoloRoomElement? doorOpt = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (!doorOpt.HasValue || !room.Elements.Any(IsArrow)) return errors;
            CatMotorConfig config = Config();
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; arrow door clearance (one tick at run speed) is undefined."); return errors; }
            float margin = config.MaxSpeed * TickTime.SecondsPerTick;
            Rect authored = Box(doorOpt.Value, room.Origin), retreated = authored;
            SoloRoomElement? retreatOpt = room.Elements.Where(e => e.Kind == SoloRoomElementKind.DoorRetreat).Cast<SoloRoomElement?>().FirstOrDefault();
            if (retreatOpt.HasValue) retreated.position += retreatOpt.Value.Settings.Offset;
            Rect sweep = Envelope(authored, retreated);
            foreach (SoloRoomElement e in room.Elements.Where(IsArrow))
                if (Grow(ArrowLaneBox(e, room.Origin), margin).Overlaps(sweep))
                    errors.Add($"{levelId}: door (authored pose or retreat sweep) is within {margin:F2} u of {e.Name}'s lane.");
            return errors;
        }

        // D-055: a Rearm or Periodic arrow's cooldown lasts until it has stopped, or it would snap back mid-flight.
        public static List<string> ValidateArrowCooldown(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            foreach (SoloRoomElement e in room.Elements.Where(IsArrow))
            {
                TrapRepeatMode repeat = e.Settings.RepeatMode;
                if (repeat != TrapRepeatMode.Rearm && repeat != TrapRepeatMode.Periodic && repeat != TrapRepeatMode.Continuous) continue;
                int stop = ArrowStopTick(e);
                if (e.Settings.CooldownTicks < stop)
                    errors.Add($"{levelId}: {e.Name} cooldown {e.Settings.CooldownTicks} ends before the arrow stops (tell + flight = {stop} ticks).");
                // PAX-099: a Periodic arrow fires only when armed, so a cooldown as long as the period skips shots.
                if (repeat == TrapRepeatMode.Periodic && e.Settings.CooldownTicks >= e.Settings.PeriodTicks)
                    errors.Add($"{levelId}: {e.Name} cooldown {e.Settings.CooldownTicks} is not below its period {e.Settings.PeriodTicks}; it would skip shots.");
            }
            return errors;
        }

        // D-113 (the developer, 2026-10-05): L003's S1 arrows fly non-stop and are jumped, not waited out; the solution's jumps
        // keep their 12-tick windows (the route validator).
        static readonly string[] JumpedArrows = { "L003/Arrow_L", "L003/Arrow_R", "L005/Arrow_A" };

        // D-056 (1): a Periodic arrow's window from its stop to the next fire (the next tell counts as
        // unsafe) clears a from-rest crossing of the lane by PeriodicSlackTicks.
        public static List<string> ValidateArrowPeriodicSlack(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            // D-119: a Continuous arrow repeats every cooldown + delay once set off; a level one runs along a walkway and is jumped
            // (the route validator's windows measure it), so only an angled one, which a walk crosses, is checked here.
            SoloRoomElement[] periodic = room.Elements.Where(e => IsArrow(e) && (e.Settings.RepeatMode == TrapRepeatMode.Periodic
                || e.Settings.RepeatMode == TrapRepeatMode.Continuous && e.Settings.Arrow.AngleDegrees != 0f)).ToArray();
            if (periodic.Length == 0) return errors;
            CatMotorConfig config = Config();
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; arrow periodic slack is undefined."); return errors; }
            foreach (SoloRoomElement e in periodic)
            {
                // PAX-076 (D-083): an arrow whose lane is wholly inside a precision section is ValidatePrecision's.
                if (WhollyInSection(room, ArrowLaneBox(e, Vector2.zero))) continue;
                if (System.Array.IndexOf(JumpedArrows, $"{levelId}/{e.Name}") >= 0) continue;
                CheckArrowPeriodicSlack(levelId, e, config, PeriodicSlackTicks, errors);
            }
            return errors;
        }

        static void CheckArrowPeriodicSlack(string levelId, SoloRoomElement e, CatMotorConfig config, int slackTicks, List<string> errors)
        {
            Rect lane = ArrowLaneBox(e, Vector2.zero);
            // PAX-099 (D-106): a cat on a walkway crosses an angled lane only where it cuts the cat's band (its height).
            float a = Mathf.Abs(e.Settings.Arrow.AngleDegrees) * Mathf.Deg2Rad;
            float footprint = a == 0f ? lane.width : Mathf.Min(lane.width, (config.ColliderSize.y + e.Settings.Arrow.Thickness / Mathf.Cos(a)) / Mathf.Tan(a));
            float crossing = FromRestCrossingTicks(footprint + config.ColliderSize.x + .5f, config);
            int period = e.Settings.RepeatMode == TrapRepeatMode.Continuous ? e.Settings.CooldownTicks + e.Settings.DelayTicks : e.Settings.PeriodTicks;
            int window = period - ArrowStopTick(e);
            if (window < crossing + slackTicks)
                errors.Add($"{levelId}: {e.Name} periodic slack {window - crossing:F1} is below {slackTicks} ticks (window {window} against a from-rest crossing of {crossing:F1}, D-056).");
        }
    }
}
