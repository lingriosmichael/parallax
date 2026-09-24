using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-051 (D-066): the generic rules from SoloRoomsLayoutTests.cs (D-055 chain acyclicity via
    // TrapLayoutValidator, D-056 reach/timing slack, D-057 6-tick reveal lead, D-058 every
    // element inside the room's frame with checkpoint/door inside the baked bounds, D-060 door
    // clearance, one checkpoint/one door), reimplemented without changing any threshold so a
    // level layout can be validated outside a specific room's narrative assertions. Room-specific
    // tests in SoloRoomsLayoutTests.cs are untouched and still run against SoloRoomsLayout
    // directly; this validator does not replace them.
    public static class LevelLayoutValidator
    {
        // D-056: SoloRoomsLayout.RequiredJumpReachFraction.
        public const float RequiredJumpReachFraction = .75f;
        // D-057: six ticks of visible lead before a trap can kill.
        public const int RevealLeadTicks = 6;
        // D-056: a Periodic trap's safe window must clear a from-rest crossing by 12 ticks.
        public const int PeriodicSlackTicks = 12;
        // RoomSafetyConfig's default BoundsMargin, used when baking/validating baked bounds.
        public const float DefaultBoundsMargin = 2f;

        public static List<string> Validate(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();

            int checkpoints = room.Elements.Count(e => e.Kind == SoloRoomElementKind.Checkpoint);
            int doors = room.Elements.Count(e => e.Kind == SoloRoomElementKind.Door);
            if (checkpoints != 1) errors.Add($"{levelId}: expected exactly one checkpoint, found {checkpoints}.");
            if (doors != 1) errors.Add($"{levelId}: expected exactly one door, found {doors}.");

            if (!TrapLayoutValidator.TryValidate(room, out string chainError)) errors.Add($"{levelId}: {chainError}");

            ValidateFrameContainment(levelId, room, errors);
            ValidateCheckpointAndDoorInBakedBounds(levelId, room, errors);
            ValidateDoorClearance(levelId, room, errors);
            ValidateReach(levelId, room, errors);
            ValidateRevealLead(levelId, room, errors);
            ValidatePeriodicSlack(levelId, room, errors);

            return errors;
        }

        // D-058: every gameplay element (and its trigger box) stays inside the room's own frame
        // — x in [0, Width], y in the room's vertical extent (floor/ceiling, widened by any
        // opening's closure). Mirrors SoloRoomsLayoutTests.Containment_..., the one generic,
        // non-narrative bounds rule: unlike a check against ComputeRoomBounds (which is defined
        // as the union of every element, so no single element can ever fail it), this can
        // actually catch a mispositioned element.
        static void ValidateFrameContainment(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            (float minY, float maxY) = VerticalBounds(room);
            foreach (SoloRoomElement e in room.Elements.Where(IsGameplayElement))
            {
                Rect bounds = Box(e, Vector2.zero);
                CheckFrame(levelId, e.Name, bounds, room.Width, minY, maxY, errors);
                if (e.SecondarySize != Vector2.zero)
                {
                    Rect trigger = new(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize);
                    CheckFrame(levelId, e.Name + " trigger", trigger, room.Width, minY, maxY, errors);
                }
            }
        }

        static void CheckFrame(string levelId, string name, Rect bounds, float width, float minY, float maxY, List<string> errors)
        {
            if (bounds.xMin < 0f || bounds.xMax > width || bounds.yMin < minY || bounds.yMax > maxY)
                errors.Add($"{levelId}: {name} lies outside the room's frame (x [{bounds.xMin:F2},{bounds.xMax:F2}] of [0,{width:F2}], y [{bounds.yMin:F2},{bounds.yMax:F2}] of [{minY:F2},{maxY:F2}]).");
        }

        static (float minY, float maxY) VerticalBounds(SoloRoomDefinition room)
        {
            float minY = SoloRoomsLayout.FloorTop, maxY = SoloRoomsLayout.CeilingUnderside + SoloRoomsLayout.SurfaceThickness;
            var byName = room.Elements.ToDictionary(e => e.Name);
            foreach (SoloRoomOpening opening in room.Openings)
            {
                if (!byName.TryGetValue(opening.ClosureName, out SoloRoomElement closure)) continue;
                Rect closureBounds = Box(closure, Vector2.zero);
                if (opening.Kind == SoloRoomOpeningKind.Pit) minY = Mathf.Min(minY, closureBounds.yMin);
                else maxY = Mathf.Max(maxY, closureBounds.yMax);
            }
            return (minY, maxY);
        }

        static bool IsGameplayElement(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Door || e.Kind == SoloRoomElementKind.Hazard || e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.HiddenSpikes || e.Kind == SoloRoomElementKind.FallingBlock || e.Kind == SoloRoomElementKind.GravityFlip || e.Kind == SoloRoomElementKind.DoorRetreat || e.Kind == SoloRoomElementKind.MovingTrap || e.Kind == SoloRoomElementKind.Checkpoint;

        // D-058: the checkpoint and door also sit inside the baked bounds (ComputeRoomBounds
        // with RoomSafetyConfig's default margin) — the same volume RoomManager checks at
        // runtime to kill a cat that leaves the room (D-058/D-059).
        static void ValidateCheckpointAndDoorInBakedBounds(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            Bounds bounds = SoloRoomBuilder.ComputeRoomBounds(room, DefaultBoundsMargin);
            SoloRoomElement? checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Cast<SoloRoomElement?>().FirstOrDefault();
            SoloRoomElement? door = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (checkpoint.HasValue && !bounds.Contains(room.Origin + checkpoint.Value.Position)) errors.Add($"{levelId}: checkpoint sits outside the room's baked bounds.");
            if (door.HasValue && !Contains(bounds, Box(door.Value, room.Origin))) errors.Add($"{levelId}: door sits outside the room's baked bounds.");
        }

        static void ValidateDoorClearance(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            SoloRoomElement? doorOpt = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (!doorOpt.HasValue) return;
            Rect authored = Box(doorOpt.Value, room.Origin);
            SoloRoomElement? retreatOpt = room.Elements.Where(e => e.Kind == SoloRoomElementKind.DoorRetreat).Cast<SoloRoomElement?>().FirstOrDefault();
            Rect retreated = authored;
            if (retreatOpt.HasValue) retreated.position += retreatOpt.Value.Settings.Offset;
            Rect sweep = Envelope(authored, retreated);
            // D-060/D-075: one tick at run speed, 0.12 u at 50 Hz. Door clearance ran without a
            // motor config before PAX-077, so a missing one is an error, not a silent skip.
            CatMotorConfig config = Config();
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; door clearance (one tick at run speed) is undefined."); return; }
            float margin = config.MaxSpeed * TickTime.SecondsPerTick;

            foreach (SoloRoomElement e in room.Elements)
            foreach (Rect kill in KillVolumes(e, room.Origin))
            {
                Rect expanded = Grow(kill, margin);
                if (expanded.Overlaps(authored)) errors.Add($"{levelId}: door (authored pose) is within {margin:F2} u of {e.Name}'s kill volume.");
                if (expanded.Overlaps(sweep)) errors.Add($"{levelId}: door (retreated pose or retreat sweep) is within {margin:F2} u of {e.Name}'s kill volume.");
            }
        }

        // PAX-073: internal so TriggerCoverage uses the same danger volumes (D-074).
        internal static IEnumerable<Rect> KillVolumes(SoloRoomElement e, Vector2 origin)
        {
            switch (e.Kind)
            {
                case SoloRoomElementKind.Hazard:
                case SoloRoomElementKind.HiddenSpikes:
                    yield return Box(e, origin);
                    break;
                case SoloRoomElementKind.FallingBlock:
                {
                    Rect primary = Box(e, origin);
                    Vector2 direction = e.Settings.Direction == FallingBlockDirection.Up ? Vector2.up : Vector2.down;
                    Rect rest = primary; rest.position += direction * e.Settings.TravelDistance;
                    yield return Envelope(primary, rest);
                    break;
                }
                case SoloRoomElementKind.MovingTrap when e.Settings.MovingKind == MovingTrapKind.Hazard:
                {
                    Rect primary = Box(e, origin);
                    Rect moved = primary; moved.position += e.Settings.Offset;
                    yield return Envelope(primary, moved);
                    break;
                }
            }
        }

        // D-056: a required jump's distance stays within RequiredJumpReachFraction of the
        // measured full reach, and a hazard's clearance stays within that fraction of the
        // kill-window a full-speed run gives.
        static void ValidateReach(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            CatMotorConfig config = Config();
            if (config == null) return;
            float gravity = GravityStrength();
            if (gravity <= 0f) return;
            var byName = room.Elements.ToDictionary(e => e.Name);

            foreach (RequiredJump jump in room.RequiredJumps)
            {
                float takeoff = jump.TakeoffX, landing = jump.LandingX;
                float runway = jump.Runway, deltaHeight = jump.LandingPawHeight - jump.TakeoffPawHeight;
                float vy = Mathf.Sqrt(2f * gravity * config.JumpHeight);
                if (vy * vy < 2f * gravity * deltaHeight) { errors.Add($"{levelId}: {jump.ReferenceName} jump height exceeds reach."); continue; }
                float vx = Mathf.Min(config.MaxSpeed, Mathf.Sqrt(2f * config.Acceleration * runway));
                float flight = (vy + Mathf.Sqrt(vy * vy - 2f * gravity * deltaHeight)) / gravity;
                float reach = vx * flight;
                float distance = Mathf.Abs(landing - takeoff);
                if (jump.Kind == RequiredJumpKind.Hazard && byName.TryGetValue(jump.ReferenceName, out SoloRoomElement reference))
                {
                    float window = vx * 2f * Mathf.Sqrt(vy * vy - 2f * gravity * jump.HazardHeight) / gravity;
                    if (reference.Size.x > RequiredJumpReachFraction * window) errors.Add($"{levelId}: {jump.ReferenceName} clearance exceeds {RequiredJumpReachFraction} of the jump window.");
                }
                if (distance > RequiredJumpReachFraction * reach) errors.Add($"{levelId}: {jump.ReferenceName} jump beyond {RequiredJumpReachFraction} of measured reach (distance {distance:F2} > {RequiredJumpReachFraction * reach:F2}).");
            }
        }

        static void ValidateRevealLead(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            foreach (SoloRoomElement e in room.Elements)
            {
                if (!e.Settings.IsConfigured) continue;
                if (e.Kind == SoloRoomElementKind.HiddenSpikes && e.Settings.RevealDelayTicks < RevealLeadTicks)
                    errors.Add($"{levelId}: {e.Name} reveal lead {e.Settings.RevealDelayTicks} is below {RevealLeadTicks} ticks (D-057).");
            }
        }

        // D-056: a Periodic trap's safe window (PeriodTicks - CooldownTicks) must clear a
        // from-rest crossing of its own footprint by PeriodicSlackTicks (mirrors
        // SoloRoomsLayoutTests.V3_PeriodicRouteHasTwelveTicksBeyondFromRestCrossing).
        static void ValidatePeriodicSlack(string levelId, SoloRoomDefinition room, List<string> errors)
        {
            CatMotorConfig config = Config();
            if (config == null) return;
            // D-075: per-tick motion through the one tick source.
            float secondsPerTick = TickTime.SecondsPerTick;
            float tickAcceleration = config.Acceleration * secondsPerTick * secondsPerTick;
            float tickSpeed = config.MaxSpeed * secondsPerTick;
            if (tickAcceleration <= 0f || tickSpeed <= 0f) return;
            float accelerateTicks = tickSpeed / tickAcceleration;
            float accelerationDistance = tickSpeed * tickSpeed / (2f * tickAcceleration);

            foreach (SoloRoomElement trap in room.Elements)
            {
                if (!trap.Settings.IsConfigured || trap.Settings.RepeatMode != TrapRepeatMode.Periodic) continue;
                float distance = trap.Size.x + config.ColliderSize.x + .5f;
                float crossing = distance <= accelerationDistance
                    ? Mathf.Sqrt(2f * distance / tickAcceleration)
                    : accelerateTicks + (distance - accelerationDistance) / tickSpeed;
                if (trap.Settings.PeriodTicks - trap.Settings.CooldownTicks < crossing + PeriodicSlackTicks)
                    errors.Add($"{levelId}: {trap.Name} periodic slack is below {PeriodicSlackTicks} ticks beyond its from-rest crossing (D-056).");
            }
        }

        // PAX-073 (D-074): trigger coverage. Deliberately not part of Validate(): it runs over
        // LevelLayouts entries only, and SoloRoomsLayout (checked through Validate() by the Parity
        // test) keeps the L001 Spikes_A gap on purpose. Learned bypasses are appended to
        // learnedBypasses as "{levelId}: learned bypass: {trap} — {reason}".
        public static List<string> ValidateTriggerCoverage(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravityStrength, List<string> learnedBypasses)
        {
            var errors = new List<string>();
            TriggerCoverage.Check(levelId, room, motor, gravityStrength, errors, learnedBypasses);
            return errors;
        }

        // PAX-073 (D-074): every Floor is at least limits.MinThickness thick and limits.MinWidth wide.
        public static List<string> ValidatePlatformSizes(string levelId, SoloRoomDefinition room, PlatformSizeConfig limits)
        {
            var errors = new List<string>();
            if (limits == null) { errors.Add($"{levelId}: no PlatformSizeConfig given."); return errors; }
            const float tolerance = 1e-4f;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.Floor))
            {
                if (e.Size.y < limits.MinThickness - tolerance) errors.Add($"{levelId}: {e.Name} thickness {e.Size.y:F2} is below the minimum {limits.MinThickness:F2} (PlatformSizeConfig).");
                if (e.Size.x < limits.MinWidth - tolerance) errors.Add($"{levelId}: {e.Name} width {e.Size.x:F2} is below the minimum {limits.MinWidth:F2} (PlatformSizeConfig).");
            }
            return errors;
        }

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
                Rect lane = ArrowLaneBox(e, Vector2.zero);
                float crossing = FromRestCrossingTicks(lane.width + config.ColliderSize.x + .5f, config);
                int window = e.Settings.PeriodTicks - ArrowStopTick(e);
                if (window < crossing + PeriodicSlackTicks)
                    errors.Add($"{levelId}: {e.Name} periodic slack {window - crossing:F1} is below {PeriodicSlackTicks} ticks (window {window} against a from-rest crossing of {crossing:F1}, D-056).");
            }
            return errors;
        }

        // The same from-rest crossing ValidatePeriodicSlack computes, per tick through TickTime (D-075).
        static float FromRestCrossingTicks(float distance, CatMotorConfig config)
        {
            float secondsPerTick = TickTime.SecondsPerTick;
            float tickAcceleration = config.Acceleration * secondsPerTick * secondsPerTick;
            float tickSpeed = config.MaxSpeed * secondsPerTick;
            if (tickAcceleration <= 0f || tickSpeed <= 0f) return float.PositiveInfinity;
            float accelerateTicks = tickSpeed / tickAcceleration;
            float accelerationDistance = tickSpeed * tickSpeed / (2f * tickAcceleration);
            return distance <= accelerationDistance
                ? Mathf.Sqrt(2f * distance / tickAcceleration)
                : accelerateTicks + (distance - accelerationDistance) / tickSpeed;
        }

        static bool IsFixedSolid(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall || e.Kind == SoloRoomElementKind.Ceiling || e.Kind == SoloRoomElementKind.PitBottom;
        static string Describe(Rect r) => $"x [{r.xMin:F2}, {r.xMax:F2}] y [{r.yMin:F2}, {r.yMax:F2}]";

        static CatMotorConfig Config() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static float GravityStrength()
        {
            GameObject cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab");
            GravityReceiver gravity = cat != null ? cat.GetComponent<GravityReceiver>() : null;
            return gravity != null ? gravity.Strength : 0f;
        }

        static Rect Box(SoloRoomElement e, Vector2 origin) => new(origin + e.Position - e.Size * .5f, e.Size);
        static bool Contains(Bounds bounds, Rect rect) => bounds.Contains(new Vector3(rect.xMin, rect.yMin)) && bounds.Contains(new Vector3(rect.xMax, rect.yMax));
        static Rect Envelope(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);
    }
}
