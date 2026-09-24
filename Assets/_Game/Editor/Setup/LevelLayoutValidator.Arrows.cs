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
        internal static float ArrowTravel(SoloRoomElement e) => ArrowMath.Travel(ArrowMouthX(e), e.Settings.Arrow.LaneEndX, e.Settings.Arrow.Length);
        internal static int ArrowFlightTicks(SoloRoomElement e) => ArrowMath.FlightTicks(ArrowTravel(e), e.Settings.Arrow.UnitsPerTick);
        // Tell plus every lethal tick: the arrow is stopped (harmless) from this many ticks after its fire.
        internal static int ArrowStopTick(SoloRoomElement e) => e.Settings.Arrow.TellTicks + ArrowFlightTicks(e) + 1;

        // The lane box: from the mouth to the lane end, LaneY +/- Thickness / 2, in origin + room-local space.
        internal static Rect ArrowLaneBox(SoloRoomElement e, Vector2 origin)
        {
            ArrowLane lane = e.Settings.Arrow;
            float mouth = ArrowMouthX(e);
            return Rect.MinMaxRect(origin.x + Mathf.Min(mouth, lane.LaneEndX), origin.y + lane.LaneY - lane.Thickness * .5f,
                origin.x + Mathf.Max(mouth, lane.LaneEndX), origin.y + lane.LaneY + lane.Thickness * .5f);
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

        // No pass-through between ticks: v <= Length + (collider width - height) - 2 x run per tick.
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
                float cap = ArrowMath.MaxUnitsPerTick(lane.Length, config.ColliderSize, config.MaxSpeed * TickTime.SecondsPerTick);
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
                if (repeat != TrapRepeatMode.Rearm && repeat != TrapRepeatMode.Periodic) continue;
                int stop = ArrowStopTick(e);
                if (e.Settings.CooldownTicks < stop)
                    errors.Add($"{levelId}: {e.Name} cooldown {e.Settings.CooldownTicks} ends before the arrow stops (tell + flight = {stop} ticks).");
            }
            return errors;
        }

        // D-056 (1): a Periodic arrow's window from its stop to the next fire (the next tell counts as
        // unsafe) clears a from-rest crossing of the lane by PeriodicSlackTicks.
        public static List<string> ValidateArrowPeriodicSlack(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            SoloRoomElement[] periodic = room.Elements.Where(e => IsArrow(e) && e.Settings.RepeatMode == TrapRepeatMode.Periodic).ToArray();
            if (periodic.Length == 0) return errors;
            CatMotorConfig config = Config();
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; arrow periodic slack is undefined."); return errors; }
            foreach (SoloRoomElement e in periodic)
            {
                // PAX-076 (D-083): an arrow whose lane is wholly inside a precision section is ValidatePrecision's.
                if (WhollyInSection(room, ArrowLaneBox(e, Vector2.zero))) continue;
                CheckArrowPeriodicSlack(levelId, e, config, PeriodicSlackTicks, errors);
            }
            return errors;
        }

        static void CheckArrowPeriodicSlack(string levelId, SoloRoomElement e, CatMotorConfig config, int slackTicks, List<string> errors)
        {
            Rect lane = ArrowLaneBox(e, Vector2.zero);
            float crossing = FromRestCrossingTicks(lane.width + config.ColliderSize.x + .5f, config);
            int window = e.Settings.PeriodTicks - ArrowStopTick(e);
            if (window < crossing + slackTicks)
                errors.Add($"{levelId}: {e.Name} periodic slack {window - crossing:F1} is below {slackTicks} ticks (window {window} against a from-rest crossing of {crossing:F1}, D-056).");
        }
    }
}
