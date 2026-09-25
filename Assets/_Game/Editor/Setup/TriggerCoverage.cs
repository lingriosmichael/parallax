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
            Vector2 checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position).DefaultIfEmpty(Vector2.zero).First();
            float checkpointX = checkpoint.x;
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

                // PAX-059 (C3): the band is the trigger's storey, and the sides are those the cat can reach it from.
                float low = StoreyLow(room, trigger);
                if (!TryStoreyCeiling(room, trigger, low, out float ceiling)) { errors.Add($"{levelId}: no ceiling over {trap.Name}: coverage band undefined."); continue; }
                Side sides = ApproachSides(room, trigger, low, ceiling, checkpoint, motor, gravityStrength);
                CheckArrowLanes(levelId, trap, trigger, low, ceiling, sides, lanes, motor, errors);
                if (dangers.Count == 0) continue;
                if (trigger.yMin <= low + Epsilon && trigger.yMax >= ceiling - Epsilon)
                {
                    foreach ((string owner, Rect volume) in dangers)
                    foreach (Side side in new[] { Side.Left, Side.Right })
                    {
                        if ((sides & side) == 0) continue;
                        float nearEdge = side == Side.Left ? trigger.xMin : trigger.xMax;
                        bool beyond = side == Side.Left ? volume.xMin >= nearEdge - Epsilon : volume.xMax <= nearEdge + Epsilon;
                        if (beyond) continue;
                        // A block chained from the trigger is harmless until it falls, so it may hang just behind the trigger on
                        // purpose, within ValidateTriggerNearTrap's reach of it (L001's Block_2 lands on a cat that backs away;
                        // developer's play, after PAX-059a). Further back it is still rejected (R5).
                        float behind = side == Side.Left ? nearEdge - volume.xMax : volume.xMin - nearEdge;
                        if (owner != trap.Name && behind <= LevelLayoutValidator.NearTrapDistance + Epsilon
                            && room.Elements.Any(e => e.Name == owner && e.Kind == SoloRoomElementKind.FallingBlock)) continue;
                        string chained = owner == trap.Name ? "" : $" (chained from {trap.Name})";
                        errors.Add($"{levelId}: trigger coverage: {owner}{chained} danger {Describe(volume)} lies before {trap.Name}'s trigger near edge x {nearEdge:F2}, {SeenFrom(side, sides)}.");
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

        static void CheckArrowLanes(string levelId, SoloRoomElement trap, Rect trigger, float low, float ceiling, Side sides, List<(string owner, Rect lane)> lanes, CatMotorConfig motor, List<string> errors)
        {
            if (lanes.Count == 0) return;
            if (motor == null) { errors.Add($"{levelId}: trigger coverage: no CatMotorConfig; arrow coverage for {trap.Name} is undefined."); return; }
            float width = motor.ColliderSize.x;
            bool cut = trigger.yMin <= low + Epsilon && trigger.yMax >= ceiling - Epsilon;
            foreach ((string owner, Rect lane) in lanes)
            {
                if (Contains(trigger, lane)) continue;
                string chained = owner == trap.Name ? "" : $" (chained from {trap.Name})";
                if (cut)
                {
                    foreach (Side side in new[] { Side.Left, Side.Right })
                    {
                        if ((sides & side) == 0) continue;
                        float nearEdge = side == Side.Left ? trigger.xMin : trigger.xMax;
                        float beyond = side == Side.Left ? lane.xMax - nearEdge : nearEdge - lane.xMin;
                        if (beyond >= width - Epsilon) continue;
                        errors.Add($"{levelId}: trigger coverage: {owner}{chained} lane {Describe(lane)} extends {Mathf.Max(0f, beyond):F2} u beyond {trap.Name}'s trigger near edge x {nearEdge:F2}; an arrow lane needs {width:F2} u (one collider width).");
                    }
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
            float lowest = StandableTops(room, x).DefaultIfEmpty(float.PositiveInfinity).Min();
            return float.IsPositiveInfinity(lowest) ? SoloRoomsLayout.FloorTop : lowest;
        }

        static IEnumerable<float> StandableTops(SoloRoomDefinition room, float x)
        {
            SoloRoomElement[] solids = room.Elements.Where(IsSolid).Where(e => Box(e).xMin <= x && Box(e).xMax >= x).ToArray();
            Rect[] pitHazards = PitHazards(room);
            foreach (SoloRoomElement solid in solids)
            {
                float top = Box(solid).yMax;
                var above = new Vector2(x, top + Epsilon);
                if (solids.Any(o => o.Name != solid.Name && Box(o).Contains(above))) continue;
                if (room.Openings.Any(o => o.ClosureName == solid.Name) || pitHazards.Any(h => h.Contains(above))) continue;
                yield return top;
            }
        }

        static Rect[] PitHazards(SoloRoomDefinition room) =>
            room.Elements.Where(e => e.Kind == SoloRoomElementKind.Hazard && e.HazardRole == SoloRoomHazardRole.OpeningBottom).Select(Box).ToArray();

        // ---------- PAX-059 (C3): storeys ----------

        [System.Flags] internal enum Side { None = 0, Left = 1, Right = 2 }

        static string SeenFrom(Side side, Side sides) => sides == (Side.Left | Side.Right)
            ? $"reached from both sides (here from the {(side == Side.Left ? "left" : "right")})"
            : "seen from where the cat enters its storey";

        static List<float> Samples(SoloRoomDefinition room, Rect span)
        {
            var samples = new List<float> { span.xMin + Epsilon, span.center.x, span.xMax - Epsilon };
            foreach (SoloRoomElement e in room.Elements.Where(e => IsSolid(e) || IsOverhead(e)))
            {
                Rect r = Box(e);
                foreach (float edge in new[] { r.xMin + Epsilon, r.xMax - Epsilon }) if (edge > span.xMin && edge < span.xMax) samples.Add(edge);
            }
            return samples;
        }

        // The storey's floor: under each sample, the highest standable top at or below the trigger's bottom (the lowest
        // standable top, as before PAX-059, where there is none); the band starts at the lowest of them.
        internal static float StoreyLow(SoloRoomDefinition room, Rect trigger) =>
            Samples(room, trigger).Min(x =>
            {
                float best = StandableTops(room, x).Where(t => t <= trigger.yMin + Epsilon).DefaultIfEmpty(float.NegativeInfinity).Max();
                return float.IsNegativeInfinity(best) ? LowestStandableTop(room, x) : best;
            });

        // The storey's ceiling: under each sample, the nearest solid underside above the storey's floor (a ceiling, a slab
        // or a ledge); the band ends at the highest of them. False where a sample has nothing overhead.
        internal static bool TryStoreyCeiling(SoloRoomDefinition room, Rect trigger, float low, out float ceiling)
        {
            ceiling = float.NegativeInfinity;
            foreach (float x in Samples(room, trigger))
            {
                float nearest = room.Elements.Where(IsOverhead).Select(Box).Where(r => r.xMin <= x && r.xMax >= x && r.yMin > low + Epsilon)
                    .Select(r => r.yMin).DefaultIfEmpty(float.PositiveInfinity).Min();
                if (float.IsPositiveInfinity(nearest)) return false;
                ceiling = Mathf.Max(ceiling, nearest);
            }
            return true;
        }

        static bool IsOverhead(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.Ceiling || e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall;

        // "Seen from the checkpoint", per storey: the sides of the trigger from which the cat can reach its band without
        // crossing it. Standable surfaces (and, in a room with gravity flips, the undersides a flipped cat walks) are
        // searched from the checkpoint's; a surface in the band is split at the trigger, and a walk, jump, drop or flip
        // whose path passes through the trigger inside the band is not taken. Without a motor, or when nothing in the
        // band is reached, it falls back to the checkpoint's side, the rule before PAX-059.
        internal static Side ApproachSides(SoloRoomDefinition room, Rect trigger, float low, float ceiling, Vector2 checkpoint, CatMotorConfig motor, float gravity)
        {
            Side fallback = checkpoint.x <= trigger.center.x ? Side.Left : Side.Right;
            if (motor == null || gravity <= 0f) return fallback;
            var reach = new Reach(motor, gravity);
            List<Piece> pieces = Pieces(room, trigger, low, ceiling, motor.ColliderSize.y);
            Piece start = pieces.Where(p => !p.Up && p.XMin - Epsilon <= checkpoint.x && p.XMax + Epsilon >= checkpoint.x && p.Y <= checkpoint.y + Epsilon)
                .OrderByDescending(p => p.Y).FirstOrDefault();
            if (start == null) return fallback;
            SoloRoomElement[] flips = room.Elements.Where(e => e.Kind == SoloRoomElementKind.GravityFlip).ToArray();
            var reached = new HashSet<Piece> { start };
            var frontier = new Queue<Piece>(); frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                Piece a = frontier.Dequeue();
                foreach (Piece b in pieces)
                    if (!reached.Contains(b) && (Walks(a, b, trigger, low, ceiling, reach, room.Width) || flips.Any(f => Flips(room, a, b, Box(f), trigger, low, ceiling, reach))))
                    { reached.Add(b); frontier.Enqueue(b); }
            }
            Side sides = Side.None;
            foreach (Piece p in reached.Where(p => p.InBand))
            {
                if (p.XMax <= trigger.xMin + Epsilon) sides |= Side.Left;
                if (p.XMin >= trigger.xMax - Epsilon) sides |= Side.Right;
            }
            return sides == Side.None ? fallback : sides;
        }

        sealed class Piece { public float XMin, XMax, Y; public bool Up, InBand; }

        readonly struct Reach
        {
            public readonly float Rise, Height, HalfWidth, Speed, Gravity, Launch, Coyote, Width;
            public Reach(CatMotorConfig m, float g)
            {
                Rise = m.JumpHeight; Height = m.ColliderSize.y; Width = m.ColliderSize.x; HalfWidth = Width * .5f; Speed = m.MaxSpeed; Gravity = g;
                Launch = JumpReach.LaunchSpeed(g, m.JumpHeight); Coyote = m.CoyoteTime;
            }
            // Edge to edge, at full speed; a drop (rise < 0) reaches further.
            public float Gap(float rise) => JumpReach.CanRise(Launch, Gravity, rise) ? JumpReach.EdgeReach(Speed, JumpReach.Flight(Launch, Gravity, rise), Coyote, Width) : -1f;
            public float Drift(float height) => Speed * Mathf.Sqrt(2f * Mathf.Max(0f, height) / Gravity);
        }

        // Tops the cat stands on and, in a room with flips, the undersides a flipped cat walks, split at the trigger where
        // they lie in its band.
        static List<Piece> Pieces(SoloRoomDefinition room, Rect trigger, float low, float ceiling, float catHeight)
        {
            var pieces = new List<Piece>();
            SoloRoomElement[] solids = room.Elements.Where(IsSolid).ToArray();
            Rect[] pitHazards = PitHazards(room);
            foreach (SoloRoomElement e in solids)
            {
                Rect r = Box(e);
                var above = new Vector2(r.center.x, r.yMax + Epsilon);
                if (room.Openings.Any(o => o.ClosureName == e.Name) || solids.Any(o => o.Name != e.Name && Box(o).Contains(above)) || pitHazards.Any(h => h.Contains(above))) continue;
                AddPiece(pieces, r.xMin, r.xMax, r.yMax, false, trigger, low, ceiling, catHeight, room.Width);
            }
            if (!room.Elements.Any(e => e.Kind == SoloRoomElementKind.GravityFlip)) return pieces;
            SoloRoomElement[] overhead = room.Elements.Where(e => IsOverhead(e) || e.Kind == SoloRoomElementKind.CollapsingFloor).ToArray();
            foreach (SoloRoomElement e in overhead)
            {
                Rect r = Box(e);
                var below = new Vector2(r.center.x, r.yMin - Epsilon);
                if (overhead.Any(o => o.Name != e.Name && Box(o).Contains(below))) continue;
                AddPiece(pieces, r.xMin, r.xMax, r.yMin, true, trigger, low, ceiling, catHeight, room.Width);
            }
            return pieces;
        }

        // A surface the cat stands on inside the trigger box is split there: standing on it fires the trigger. Only the part
        // between the room's side walls (x 0 to its width) is a surface; a wall element outside them has none.
        static void AddPiece(List<Piece> pieces, float xMin, float xMax, float y, bool up, Rect trigger, float low, float ceiling, float catHeight, float width)
        {
            xMin = Mathf.Max(xMin, 0f); xMax = Mathf.Min(xMax, width);
            if (xMax - xMin < Epsilon) return;
            bool inBand = y >= low - Epsilon && y <= ceiling + Epsilon;
            float body0 = up ? y - catHeight : y, body1 = up ? y : y + catHeight;
            bool touches = body1 > trigger.yMin + Epsilon && body0 < trigger.yMax - Epsilon;
            if (!touches || xMax <= trigger.xMin + Epsilon || xMin >= trigger.xMax - Epsilon) { pieces.Add(new Piece { XMin = xMin, XMax = xMax, Y = y, Up = up, InBand = inBand }); return; }
            if (xMin < trigger.xMin - Epsilon) pieces.Add(new Piece { XMin = xMin, XMax = trigger.xMin, Y = y, Up = up, InBand = inBand });
            if (xMax > trigger.xMax + Epsilon) pieces.Add(new Piece { XMin = trigger.xMax, XMax = xMax, Y = y, Up = up, InBand = inBand });
        }

        // A walk, jump or drop between two surfaces of the same gravity, not through the trigger inside its band. The
        // cat leaves a surface at an end (it can't pass through a solid), and never at the room's side walls; the path is
        // the horizontal stretch it covers, from the lower surface up to a jump's height above the higher one (mirrored
        // for undersides).
        static bool Walks(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, float width)
        {
            if (a.Up != b.Up) return false;
            float rise = a.Up ? a.Y - b.Y : b.Y - a.Y;
            float gap = Mathf.Max(0f, Mathf.Max(b.XMin - a.XMax, a.XMin - b.XMax));
            float limit = reach.Gap(rise);
            if (limit < 0f || gap > limit + Epsilon) return false;
            float xa, xb;
            if (b.XMin >= a.XMax - Epsilon) { xa = a.XMax; xb = b.XMin; }
            else if (b.XMax <= a.XMin + Epsilon) { xa = a.XMin; xb = b.XMax; }
            else
            {
                // Overlapping spans: up onto b round one of b's ends, or off one of a's ends down onto b.
                IEnumerable<float> ends = (rise > 0f ? new[] { b.XMin, b.XMax }.Where(x => x >= a.XMin - Epsilon && x <= a.XMax + Epsilon)
                                                     : new[] { a.XMin, a.XMax }.Where(x => x >= b.XMin - Epsilon && x <= b.XMax + Epsilon))
                    .Where(x => x > Epsilon && x < width - Epsilon);
                return ends.Any(x => !Crosses(x - reach.HalfWidth, x + reach.HalfWidth, a, b, trigger, low, ceiling, reach));
            }
            return !Crosses(Mathf.Min(xa, xb), Mathf.Max(xa, xb), a, b, trigger, low, ceiling, reach);
        }

        static bool Crosses(float x0, float x1, Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach)
        {
            if (x1 <= trigger.xMin + Epsilon || x0 >= trigger.xMax - Epsilon) return false;
            float lift = reach.Rise + reach.Height;
            float y0 = Mathf.Min(a.Y, b.Y) - (a.Up ? lift : 0f), y1 = Mathf.Max(a.Y, b.Y) + (a.Up ? 0f : lift);
            return y1 > trigger.yMin + Epsilon && y0 < trigger.yMax - Epsilon;
        }

        // A flip reached from a (within a jump of it, within a jump's gap of a's span) that carries the cat to b, the
        // first surface of the other gravity it meets on its way, within the drift of the flip's span.
        static bool Flips(SoloRoomDefinition room, Piece a, Piece b, Rect flip, Rect trigger, float low, float ceiling, Reach reach)
        {
            if (a.Up == b.Up) return false;
            float lift = reach.Rise + reach.Height, flat = reach.Gap(0f);
            bool inReach = a.Up ? flip.yMax >= a.Y - lift && flip.yMin <= a.Y : flip.yMin <= a.Y + lift && flip.yMax >= a.Y;
            if (!inReach || flip.xMax < a.XMin - flat || flip.xMin > a.XMax + flat) return false;
            bool beyond = b.Up ? b.Y > flip.yMax - Epsilon : b.Y < flip.yMin + Epsilon;
            if (!beyond) return false;
            float drift = reach.Drift(b.Up ? b.Y - flip.yMin : flip.yMax - b.Y) + reach.HalfWidth;
            float from = flip.xMin - drift, to = flip.xMax + drift;
            if (b.XMax < from || b.XMin > to) return false;
            float x = Mathf.Clamp(flip.center.x, b.XMin, b.XMax);
            // Another surface between the flip and b, anywhere between the flip's centre and x, stops the cat first.
            float x0 = Mathf.Min(x, flip.center.x), x1 = Mathf.Max(x, flip.center.x);
            bool stoppedFirst = room.Elements.Where(e => IsOverhead(e) || IsSolid(e)).Select(Box).Any(r => r.xMin <= x1 && r.xMax >= x0
                && (b.Up ? r.yMin > flip.yMax + Epsilon && r.yMin < b.Y - Epsilon : r.yMax < flip.yMin - Epsilon && r.yMax > b.Y + Epsilon));
            if (stoppedFirst) return false;
            float ax = Mathf.Clamp(flip.center.x, a.XMin, a.XMax);
            float body = reach.Height;
            bool approach = Mathf.Max(ax, flip.center.x) > trigger.xMin + Epsilon && Mathf.Min(ax, flip.center.x) < trigger.xMax - Epsilon
                && (a.Up ? a.Y : a.Y + body) > trigger.yMin + Epsilon && (a.Up ? a.Y - body : a.Y) < trigger.yMax - Epsilon;
            float y0 = Mathf.Min(a.Y, b.Y), y1 = Mathf.Max(a.Y, b.Y);
            bool rise = x + reach.HalfWidth > trigger.xMin + Epsilon && x - reach.HalfWidth < trigger.xMax - Epsilon && y1 > trigger.yMin + Epsilon && y0 < trigger.yMax - Epsilon;
            return !approach && !rise;
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
