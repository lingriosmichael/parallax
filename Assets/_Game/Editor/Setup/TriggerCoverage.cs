using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-073 (D-074): the trigger-coverage geometry behind LevelLayoutValidator.ValidateTriggerCoverage.
    // For every Overlap, non-Periodic trap (collapsing floors and gravity flips trigger on their own
    // body, so they are covered by construction), the danger set is its KillVolumes, every chain
    // descendant's KillVolumes, and the door sweep of a DoorRetreat descendant. The trigger must be a
    // cut: over its x-span it spans the lowest standable top (pit interiors are death) up to the
    // ceiling underside, and every danger lies beyond its near edge as seen from the checkpoint. A
    // danger on a floor stretch enclosed by unjumpable floor hazards or the room's ends, and entered
    // only through gravity flips, is also covered when the trigger contains each of those flips and
    // the danger is below a gravity-up cat's reach from the ceiling.
    // Everything is in room-local coordinates, checked at the authored pose.
    // PAX-074 (D-078): an arrow's danger is its lane, which KillVolumes doesn't list. An unfired arrow
    // has no danger, so the lane on the checkpoint side of the trigger is not reachable danger: a lane
    // is covered when the trigger is a cut and the lane extends at least one collider width beyond its
    // near edge, or when the trigger contains the lane box (jump-arc triggers). Checked before the
    // band rule, for the root and every chain descendant that is an arrow.
    static class TriggerCoverage
    {
        const float Epsilon = 1e-3f;

        public static void Check(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravityStrength, List<string> errors, List<string> learnedBypasses)
        {
            float checkpointX = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position.x).DefaultIfEmpty(0f).First();
            foreach (SoloRoomElement trap in room.Elements)
            {
                if (!IsOverlapTrap(trap)) continue;
                string reason = trap.Settings.LearnedBypassReason;
                if (reason != null)
                {
                    if (string.IsNullOrWhiteSpace(reason)) errors.Add($"{levelId}: trigger coverage: {trap.Name} is marked as a learned bypass with an empty reason; a learned bypass needs a reason.");
                    else learnedBypasses?.Add($"{levelId}: learned bypass: {trap.Name} — {reason}");
                    continue;
                }

                List<(string owner, Rect volume)> dangers = Dangers(room, trap);
                List<(string owner, Rect lane)> lanes = ArrowLanes(room, trap);
                if (dangers.Count == 0 && lanes.Count == 0) continue;
                if (!TryTrigger(trap, out Rect trigger)) { errors.Add($"{levelId}: trigger coverage: {trap.Name} fires on overlap but has no trigger box."); continue; }
                if (!TryCeilingUnderside(room, trigger, out float ceiling)) { errors.Add($"{levelId}: no ceiling over {trap.Name}: coverage band undefined."); continue; }

                float low = BandLow(room, trigger);
                CheckArrowLanes(levelId, trap, trigger, low, ceiling, checkpointX, lanes, motor, errors);
                if (dangers.Count == 0) continue;
                if (trigger.yMin <= low + Epsilon && trigger.yMax >= ceiling - Epsilon)
                {
                    bool fromLeft = checkpointX <= trigger.center.x;
                    float nearEdge = fromLeft ? trigger.xMin : trigger.xMax;
                    foreach ((string owner, Rect volume) in dangers)
                    {
                        bool beyond = fromLeft ? volume.xMin >= nearEdge - Epsilon : volume.xMax <= nearEdge + Epsilon;
                        if (beyond) continue;
                        string chained = owner == trap.Name ? "" : $" (chained from {trap.Name})";
                        errors.Add($"{levelId}: trigger coverage: {owner}{chained} danger {Describe(volume)} lies before {trap.Name}'s trigger near edge x {nearEdge:F2}, seen from the checkpoint.");
                    }
                    continue;
                }

                string[] uncovered = dangers.Where(d => !CoveredByFlipEntry(room, trigger, d.volume, checkpointX, motor, gravityStrength)).Select(d => d.owner).Distinct().ToArray();
                if (uncovered.Length == 0) continue;
                errors.Add($"{levelId}: trigger coverage: {trap.Name}'s trigger {Describe(trigger)} does not cut the cat's band y [{low:F2}, {ceiling:F2}]: {UncoveredBands(trigger, low, ceiling)}; dangers reachable without it: {string.Join(", ", uncovered)}.");
            }
        }

        static bool IsOverlapTrap(SoloRoomElement e)
        {
            SoloRoomTrapSettings s = e.Settings;
            if (!s.IsConfigured || s.TriggerSource != TrapTriggerSource.Overlap || s.RepeatMode == TrapRepeatMode.Periodic) return false;
            return e.Kind == SoloRoomElementKind.HiddenSpikes || e.Kind == SoloRoomElementKind.FallingBlock || e.Kind == SoloRoomElementKind.MovingTrap || e.Kind == SoloRoomElementKind.DoorRetreat || e.Kind == SoloRoomElementKind.Arrow;
        }

        // PAX-074 (D-078): the lanes of the root and of every chain descendant that is an arrow.
        static List<(string owner, Rect lane)> ArrowLanes(SoloRoomDefinition room, SoloRoomElement root)
        {
            var lanes = new List<(string, Rect)>();
            if (LevelLayoutValidator.IsArrow(root)) lanes.Add((root.Name, LevelLayoutValidator.ArrowLaneBox(root, Vector2.zero)));
            var visited = new HashSet<string> { root.Name };
            var frontier = new Queue<string>(); frontier.Enqueue(root.Name);
            while (frontier.Count > 0)
            {
                string source = frontier.Dequeue();
                foreach (SoloRoomElement e in room.Elements)
                {
                    if (!e.Settings.IsConfigured || e.Settings.TriggerSource != TrapTriggerSource.Chain || e.Settings.ChainSource != source || !visited.Add(e.Name)) continue;
                    frontier.Enqueue(e.Name);
                    if (LevelLayoutValidator.IsArrow(e)) lanes.Add((e.Name, LevelLayoutValidator.ArrowLaneBox(e, Vector2.zero)));
                }
            }
            return lanes;
        }

        static void CheckArrowLanes(string levelId, SoloRoomElement trap, Rect trigger, float low, float ceiling, float checkpointX, List<(string owner, Rect lane)> lanes, CatMotorConfig motor, List<string> errors)
        {
            if (lanes.Count == 0) return;
            if (motor == null) { errors.Add($"{levelId}: trigger coverage: no CatMotorConfig; arrow coverage for {trap.Name} is undefined."); return; }
            float width = motor.ColliderSize.x;
            bool cut = trigger.yMin <= low + Epsilon && trigger.yMax >= ceiling - Epsilon;
            bool fromLeft = checkpointX <= trigger.center.x;
            float nearEdge = fromLeft ? trigger.xMin : trigger.xMax;
            foreach ((string owner, Rect lane) in lanes)
            {
                if (Contains(trigger, lane)) continue;
                string chained = owner == trap.Name ? "" : $" (chained from {trap.Name})";
                if (cut)
                {
                    float beyond = fromLeft ? lane.xMax - nearEdge : nearEdge - lane.xMin;
                    if (beyond >= width - Epsilon) continue;
                    errors.Add($"{levelId}: trigger coverage: {owner}{chained} lane {Describe(lane)} extends {Mathf.Max(0f, beyond):F2} u beyond {trap.Name}'s trigger near edge x {nearEdge:F2}; an arrow lane needs {width:F2} u (one collider width).");
                }
                else
                    errors.Add($"{levelId}: trigger coverage: {trap.Name}'s trigger {Describe(trigger)} neither cuts the cat's band y [{low:F2}, {ceiling:F2}] ({UncoveredBands(trigger, low, ceiling)}) nor contains {owner}{chained}'s lane {Describe(lane)}.");
            }
        }

        // HiddenSpikes and DoorRetreat fall back to their own box at runtime; FallingBlock and
        // MovingTrap disable themselves without a trigger box.
        internal static bool TryTrigger(SoloRoomElement trap, out Rect trigger)
        {
            if (trap.SecondarySize != Vector2.zero) { trigger = new Rect(trap.SecondaryPosition - trap.SecondarySize * .5f, trap.SecondarySize); return true; }
            trigger = Box(trap);
            return trap.Kind == SoloRoomElementKind.HiddenSpikes || trap.Kind == SoloRoomElementKind.DoorRetreat;
        }

        static List<(string owner, Rect volume)> Dangers(SoloRoomDefinition room, SoloRoomElement root)
        {
            var dangers = new List<(string, Rect)>();
            foreach (Rect kill in LevelLayoutValidator.KillVolumes(root, Vector2.zero)) dangers.Add((root.Name, kill));
            SoloRoomElement? door = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            var visited = new HashSet<string> { root.Name };
            var frontier = new Queue<string>(); frontier.Enqueue(root.Name);
            while (frontier.Count > 0)
            {
                string source = frontier.Dequeue();
                foreach (SoloRoomElement e in room.Elements)
                {
                    if (!e.Settings.IsConfigured || e.Settings.TriggerSource != TrapTriggerSource.Chain || e.Settings.ChainSource != source || !visited.Add(e.Name)) continue;
                    frontier.Enqueue(e.Name);
                    foreach (Rect kill in LevelLayoutValidator.KillVolumes(e, Vector2.zero)) dangers.Add((e.Name, kill));
                    if (e.Kind == SoloRoomElementKind.DoorRetreat && door.HasValue)
                    {
                        Rect authored = Box(door.Value), moved = authored;
                        moved.position += e.Settings.Offset;
                        dangers.Add((e.Name, Rect.MinMaxRect(Mathf.Min(authored.xMin, moved.xMin), Mathf.Min(authored.yMin, moved.yMin), Mathf.Max(authored.xMax, moved.xMax), Mathf.Max(authored.yMax, moved.yMax))));
                    }
                }
            }
            return dangers;
        }

        // The underside of the highest Ceiling element spanning the trigger's whole x-span.
        internal static bool TryCeilingUnderside(SoloRoomDefinition room, Rect trigger, out float underside)
        {
            underside = float.NegativeInfinity;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.Ceiling))
            {
                Rect c = Box(e);
                if (c.xMin <= trigger.xMin + Epsilon && c.xMax >= trigger.xMax - Epsilon) underside = Mathf.Max(underside, c.yMin);
            }
            return !float.IsNegativeInfinity(underside);
        }

        // The lowest top the cat can stand on anywhere under the x-span: a solid's top counts when
        // no other solid covers it. Pit interiors are death: an opening's closure (its PitBottom)
        // and any top under an OpeningBottom hazard never count; every other solid does.
        internal static float BandLow(SoloRoomDefinition room, Rect span)
        {
            var samples = new List<float> { span.xMin + Epsilon, span.center.x, span.xMax - Epsilon };
            foreach (SoloRoomElement e in room.Elements.Where(IsSolid))
            {
                Rect r = Box(e);
                foreach (float edge in new[] { r.xMin + Epsilon, r.xMax - Epsilon }) if (edge > span.xMin && edge < span.xMax) samples.Add(edge);
            }
            return samples.Min(x => LowestStandableTop(room, x));
        }

        static float LowestStandableTop(SoloRoomDefinition room, float x)
        {
            SoloRoomElement[] solids = room.Elements.Where(IsSolid).Where(e => Box(e).xMin <= x && Box(e).xMax >= x).ToArray();
            Rect[] pitHazards = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Hazard && e.HazardRole == SoloRoomHazardRole.OpeningBottom).Select(Box).ToArray();
            float lowest = float.PositiveInfinity;
            foreach (SoloRoomElement solid in solids)
            {
                Rect r = Box(solid);
                float top = r.yMax;
                var above = new Vector2(x, top + Epsilon);
                if (solids.Any(o => o.Name != solid.Name && Box(o).Contains(above))) continue;
                if (room.Openings.Any(o => o.ClosureName == solid.Name) || pitHazards.Any(h => h.Contains(above))) continue;
                lowest = Mathf.Min(lowest, top);
            }
            return float.IsPositiveInfinity(lowest) ? SoloRoomsLayout.FloorTop : lowest;
        }

        static bool CoveredByFlipEntry(SoloRoomDefinition room, Rect trigger, Rect danger, float checkpointX, CatMotorConfig motor, float gravityStrength)
        {
            if (motor == null || gravityStrength <= 0f) return false;
            // A gravity-up cat can walk the ceiling into the stretch without any flip, so the clause
            // only covers dangers below its reach (ceiling underside - collider height - jump height).
            if (!TryCeilingUnderside(room, danger, out float ceiling) || danger.yMax > ceiling - motor.ColliderSize.y - motor.JumpHeight - Epsilon) return false;
            float floor = LowestStandableTop(room, danger.center.x);
            float left = 0f, right = room.Width;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.Hazard && e.HazardRole == SoloRoomHazardRole.UnjumpableFloor))
            {
                Rect h = Box(e);
                if (h.xMax <= danger.xMin + Epsilon) left = Mathf.Max(left, h.xMax);
                else if (h.xMin >= danger.xMax - Epsilon) right = Mathf.Min(right, h.xMin);
            }
            if (checkpointX > left && checkpointX < right) return false;
            // Anything standable above the stretch's floor could be another way in.
            if (room.Elements.Where(IsSolid).Select(Box).Any(r => r.xMax > left && r.xMin < right && r.yMax > floor + Epsilon)) return false;

            float halfCat = motor.ColliderSize.x * .5f;
            foreach (SoloRoomElement flip in room.Elements.Where(e => e.Kind == SoloRoomElementKind.GravityFlip))
            {
                Rect f = Box(flip);
                // A cat flipped to gravity down at the flip's top drifts at most full run speed for its fall.
                float drift = motor.MaxSpeed * Mathf.Sqrt(2f * Mathf.Max(0f, f.yMax - floor) / gravityStrength) + halfCat;
                bool entersStretch = f.xMax + drift > left && f.xMin - drift < right;
                if (entersStretch && !Contains(trigger, f)) return false;
            }
            return true;
        }

        static bool IsSolid(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall || e.Kind == SoloRoomElementKind.PitBottom || e.Kind == SoloRoomElementKind.CollapsingFloor
            || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Solid);

        internal static string UncoveredBands(Rect trigger, float low, float ceiling)
        {
            var bands = new List<string>();
            if (trigger.yMin > low + Epsilon) bands.Add($"uncovered y [{low:F2}, {Mathf.Min(trigger.yMin, ceiling):F2}]");
            if (trigger.yMax < ceiling - Epsilon) bands.Add($"uncovered y [{Mathf.Max(trigger.yMax, low):F2}, {ceiling:F2}]");
            return string.Join(" and ", bands);
        }

        static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - Epsilon && inner.xMax <= outer.xMax + Epsilon && inner.yMin >= outer.yMin - Epsilon && inner.yMax <= outer.yMax + Epsilon;

        static string Describe(Rect r) => $"x [{r.xMin:F2}, {r.xMax:F2}] y [{r.yMin:F2}, {r.yMax:F2}]";
        static Rect Box(SoloRoomElement e) => new(e.Position - e.Size * .5f, e.Size);
    }
}
