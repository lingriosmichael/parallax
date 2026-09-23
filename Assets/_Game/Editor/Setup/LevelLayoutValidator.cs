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
        // D-060: one tick at run speed (.1 u/tick).
        public const float DoorClearanceMargin = .1f;
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

            foreach (SoloRoomElement e in room.Elements)
            foreach (Rect kill in KillVolumes(e, room.Origin))
            {
                Rect expanded = Grow(kill, DoorClearanceMargin);
                if (expanded.Overlaps(authored)) errors.Add($"{levelId}: door (authored pose) is within {DoorClearanceMargin} u of {e.Name}'s kill volume.");
                if (expanded.Overlaps(sweep)) errors.Add($"{levelId}: door (retreated pose or retreat sweep) is within {DoorClearanceMargin} u of {e.Name}'s kill volume.");
            }
        }

        static IEnumerable<Rect> KillVolumes(SoloRoomElement e, Vector2 origin)
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
            float tickAcceleration = config.Acceleration / 3600f;
            float tickSpeed = config.MaxSpeed / 60f;
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
