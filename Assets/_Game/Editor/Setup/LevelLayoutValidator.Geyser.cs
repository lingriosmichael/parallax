using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-086 (D-088): geysers ----------
        // A vent flush in a Floor (Up) or a Ceiling (Down) that erupts on a Periodic cycle and sets the cat's velocity along
        // its push to LaunchSpeed while the cat overlaps its column. Separately named, not part of Validate(), like
        // ValidateSpear and ValidateInverter. The level band (levels 11+ only) is ValidateBand's. A geyser is never a kill
        // volume (KillVolumes has no case for it), and the vent is inside its host, so frame containment doesn't list it.

        internal static bool IsGeyser(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Geyser;

        public static List<string> ValidateGeyser(string levelId, SoloRoomDefinition room, CatMotorConfig config)
        {
            var errors = new List<string>();
            SoloRoomElement[] geysers = room.Elements.Where(IsGeyser).ToArray();
            if (geysers.Length == 0) return errors;
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; a geyser's column clearance (R5) is undefined."); return errors; }
            foreach (SoloRoomElement e in geysers)
            {
                GeyserSettings g = e.Settings.Geyser.Resolved;
                CheckGeyserCycle(levelId, e, g, errors);
                if (g.ColumnWidth <= 0f || g.ColumnHeight <= 0f || g.LaunchSpeed <= 0f)
                    errors.Add($"{levelId}: {e.Name} needs a positive column width, column height and launch speed.");
                if (!TryGeyserFace(room, e, out string host, out GeyserDirection face, out float faceY))
                { errors.Add($"{levelId}: {e.Name}'s vent {Describe(Box(e, Vector2.zero))} sits in no Floor or Ceiling face; a vent is flush in its host's face."); continue; }
                if (face != g.Direction)
                { errors.Add($"{levelId}: {e.Name} direction {g.Direction} doesn't match its host face ({host}'s {(face == GeyserDirection.Up ? "top" : "underside")}: {face}) (R6)."); continue; }
                // R5: the column plus the cat's height beyond it, so a cat still in the column is never pushed into a solid.
                Rect clearance = GeyserReach(e, g, faceY, g.ColumnHeight + config.ColliderSize.y);
                foreach (SoloRoomElement solid in room.Elements.Where(s => s.Name != e.Name && IsGeyserSolid(s)))
                    if (Box(solid, Vector2.zero).Overlaps(clearance))
                        errors.Add($"{levelId}: {e.Name}'s column with the cat's {config.ColliderSize.y:F2} u above it {Describe(clearance)} overlaps {solid.Name}; the cat would be pushed into it every erupting tick (R5).");
            }
            return errors;
        }

        // R2: the launch envelope, from the first push to the apex: the column's width plus one run tick each side per tick of
        // flight, from the vent's face out to the pushes, the discrete rise and the cat's height. (1) It stays inside the
        // room's frame. (2) A disguised hazard (hidden spikes, a disguised arrow's lane, a chained trap's kill volume) inside
        // it needs a declared Dies betrayal with it as the killer, so the route validator measures its lead. Honest hazards
        // are allowed without one (D-056 (4)).
        public static List<string> ValidateGeyserEnvelope(string levelId, SoloRoomDefinition room, CatMotorConfig config, float gravityStrength, Routes.RoomRoutes routes)
        {
            var errors = new List<string>();
            SoloRoomElement[] geysers = room.Elements.Where(IsGeyser).ToArray();
            if (geysers.Length == 0) return errors;
            if (config == null || gravityStrength <= 0f) { errors.Add($"{levelId}: no CatMotorConfig or gravity; a geyser's launch envelope is undefined."); return errors; }
            (float minY, float maxY) = VerticalBounds(room);
            foreach (SoloRoomElement e in geysers)
            {
                GeyserSettings g = e.Settings.Geyser.Resolved;
                if (!TryGeyserFace(room, e, out _, out _, out float faceY)) continue;   // ValidateGeyser's error
                Rect envelope = LaunchEnvelope(e, g, faceY, config, gravityStrength);
                CheckFrame(levelId, $"{e.Name}'s launch envelope", envelope, room.Width, minY, maxY, errors);
                foreach (SoloRoomElement other in room.Elements.Where(o => o.Name != e.Name && IsDisguisedHazard(o)))
                {
                    bool inside = KillVolumes(other, Vector2.zero).Any(v => v.Overlaps(envelope)) || (IsArrow(other) && ArrowLaneBox(other, Vector2.zero).Overlaps(envelope));
                    if (!inside) continue;
                    bool declared = routes != null && routes.Betrayals.Any(b => b.Outcome == Routes.BetrayalOutcome.Dies && b.Killer == other.Name);
                    if (!declared)
                        errors.Add($"{levelId}: {other.Name}, a disguised hazard, lies in {e.Name}'s launch envelope {Describe(envelope)}, but no declared betrayal route dies on it; its lead must be measured (R2).");
                }
            }
            return errors;
        }

        /// <summary>R2's envelope in room-local space (see ValidateGeyserEnvelope).</summary>
        internal static Rect LaunchEnvelope(SoloRoomElement e, GeyserSettings g, float faceY, CatMotorConfig config, float gravityStrength)
        {
            float dt = TickTime.SecondsPerTick;
            int pushes = GeyserMath.PushTicks(g.ColumnHeight, g.LaunchSpeed, dt);
            float rise = GeyserMath.Rise(g.LaunchSpeed, gravityStrength, dt, out int riseTicks);
            float spread = (pushes + riseTicks) * config.MaxSpeed * dt;
            Rect reach = GeyserReach(e, g, faceY, Mathf.Max(g.ColumnHeight, pushes * g.LaunchSpeed * dt) + rise + config.ColliderSize.y);
            return Rect.MinMaxRect(reach.xMin - spread, reach.yMin, reach.xMax + spread, reach.yMax);
        }

        static void CheckGeyserCycle(string levelId, SoloRoomElement e, GeyserSettings g, List<string> errors)
        {
            if (!e.Settings.IsConfigured || e.Settings.RepeatMode != TrapRepeatMode.Periodic)
            { errors.Add($"{levelId}: {e.Name} is a geyser set to {e.Settings.RepeatMode}; a geyser is always Periodic (D-055 (2), D-088)."); return; }
            if (g.TellTicks < GeyserMath.MinTellTicks) errors.Add($"{levelId}: {e.Name} tell {g.TellTicks} ticks is below {GeyserMath.MinTellTicks} (D-057).");
            if (g.EruptTicks < 1) errors.Add($"{levelId}: {e.Name} erupt {g.EruptTicks} ticks is below 1.");
            if (g.TellTicks + g.EruptTicks >= e.Settings.PeriodTicks)
                errors.Add($"{levelId}: {e.Name} tell + erupt ({g.TellTicks + g.EruptTicks} ticks) must be shorter than its period ({e.Settings.PeriodTicks} ticks).");
        }

        // The Floor whose top, or the Ceiling whose underside, the vent is flush with, the vent's box inside the host's.
        static bool TryGeyserFace(SoloRoomDefinition room, SoloRoomElement e, out string host, out GeyserDirection face, out float faceY)
        {
            const float eps = 1e-3f;
            Rect vent = Box(e, Vector2.zero);
            foreach (SoloRoomElement h in room.Elements.Where(h => h.Kind == SoloRoomElementKind.Floor || h.Kind == SoloRoomElementKind.Ceiling))
            {
                Rect r = Box(h, Vector2.zero);
                if (vent.xMin < r.xMin - eps || vent.xMax > r.xMax + eps || vent.yMin < r.yMin - eps || vent.yMax > r.yMax + eps) continue;
                if (h.Kind == SoloRoomElementKind.Floor && Mathf.Abs(vent.yMax - r.yMax) < eps) { host = h.Name; face = GeyserDirection.Up; faceY = r.yMax; return true; }
                if (h.Kind == SoloRoomElementKind.Ceiling && Mathf.Abs(vent.yMin - r.yMin) < eps) { host = h.Name; face = GeyserDirection.Down; faceY = r.yMin; return true; }
            }
            host = null; face = default; faceY = 0f;
            return false;
        }

        // A box the column's width, from the vent's face `length` along the push.
        static Rect GeyserReach(SoloRoomElement e, GeyserSettings g, float faceY, float length)
        {
            float x0 = e.Position.x - g.ColumnWidth * .5f, x1 = e.Position.x + g.ColumnWidth * .5f;
            return g.Direction == GeyserDirection.Up ? Rect.MinMaxRect(x0, faceY, x1, faceY + length) : Rect.MinMaxRect(x0, faceY - length, x1, faceY);
        }

        static bool IsGeyserSolid(SoloRoomElement e) =>
            IsFixedSolid(e) || e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.FallingBlock
            || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Solid);

        static bool IsDisguisedHazard(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.HiddenSpikes
            || (IsArrow(e) && e.Settings.Arrow.Disguised)
            || (e.Settings.IsConfigured && e.Settings.TriggerSource == TrapTriggerSource.Chain);
    }
}
