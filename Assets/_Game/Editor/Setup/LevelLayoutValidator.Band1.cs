using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-059 (D-085): band 1's content rules (levels 1-10). ValidateBand1Content reads the layout and the declared
    // routes only; ValidateBand1Duration takes the solution's length from a replay; ValidateBand1Tells checks that
    // nothing visible gives a trap away. Separate from Validate(), like ValidateRoutes.
    public static partial class LevelLayoutValidator
    {
        // Today's frame: a 32 u room between its two walls.
        public const float Band1MaxWidth = 32f;
        // 10-30 s once known (the 1500-tick replay cap), and L001's floor (the one exemption): "about 300" ticks, set to 280
        // by the developer when L001's blocks moved next to their triggers (after PAX-059a play).
        public const int Band1MinTicks = 500, Band1MaxTicks = 1500, Band1FirstLevelMinTicks = 280;

        // A Dies route named "Dead end..." is a tempting side route (D-085), outside the sequence; every other Dies route
        // is on the way to the door.
        public const string DeadEndPrefix = "Dead end";
        public static bool IsDeadEnd(Betrayal b) => b.Outcome == BetrayalOutcome.Recovers || b.Name.StartsWith(DeadEndPrefix, System.StringComparison.Ordinal);

        public static bool IsBand1(int level) => level >= 1 && level <= 10;
        public static int Band1MinLethal(int level) => level <= 5 ? 5 : level <= 7 ? 6 : 7;
        public static int Band1MinSequence(int level) => level <= 5 ? 4 : 5;
        public static int Band1MinDeadEnds(int level) => level <= 5 ? 1 : 2;

        public static List<string> ValidateBand1Content(string levelId, int level, SoloRoomDefinition room, RoomRoutes routes)
        {
            var errors = new List<string>();
            if (!IsBand1(level)) return errors;
            Betrayal[] dies = routes.Betrayals.Where(b => b.Outcome == BetrayalOutcome.Dies).ToArray();
            int lethal = dies.Select(b => b.Killer).Distinct().Count();
            if (lethal < Band1MinLethal(level))
                errors.Add($"{levelId}: {lethal} lethal betrayals (distinct killers); level {level} needs at least {Band1MinLethal(level)} (D-085).");
            List<Betrayal> chain = SequentialChain(routes);
            if (chain.Count < Band1MinSequence(level))
                errors.Add($"{levelId}: {chain.Count} lethal betrayals in sequence (each surviving the earlier ones); level {level} needs at least {Band1MinSequence(level)} (D-085, PAX-059 §3.4).");
            int deadEnds = routes.Betrayals.Count(IsDeadEnd);
            if (deadEnds < Band1MinDeadEnds(level))
                errors.Add($"{levelId}: {deadEnds} dead end(s) (Dies routes named \"{DeadEndPrefix}...\", plus Recovers routes); level {level} needs at least {Band1MinDeadEnds(level)} (D-085).");
            if (room.PrecisionSections.Length > 0)
                errors.Add($"{levelId}: band 1 has no precision sections (D-065, D-083).");
            if (room.Width > Band1MaxWidth + 1e-3f)
                errors.Add($"{levelId}: the room is {room.Width:F2} u wide, wider than the frame ({Band1MaxWidth:F0} u, D-085).");
            SoloRoomElement? checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Cast<SoloRoomElement?>().FirstOrDefault();
            SoloRoomElement? door = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (checkpoint.HasValue && door.HasValue)
            {
                // The room's height is the space the cat moves through: the lowest floor top to the highest ceiling underside
                // (not the solid ground and pit shafts under the floor).
                float floor = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Floor).Select(e => Box(e, Vector2.zero).yMax).DefaultIfEmpty(0f).Min();
                float roof = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Ceiling).Select(e => Box(e, Vector2.zero).yMin).DefaultIfEmpty(floor).Max();
                Vector2 d = door.Value.Position - checkpoint.Value.Position;
                if (Mathf.Abs(d.x) < room.Width * .5f && Mathf.Abs(d.y) < (roof - floor) * .5f)
                    errors.Add($"{levelId}: the door is {Mathf.Abs(d.x):F2} u across and {Mathf.Abs(d.y):F2} u up from the start; it must be at least half the room's width ({room.Width * .5f:F2}) or height ({(roof - floor) * .5f:F2}) away (D-085).");
            }
            return errors;
        }

        public static List<string> ValidateBand1Duration(string levelId, int level, int solutionTicks)
        {
            var errors = new List<string>();
            int min = level == 1 ? Band1FirstLevelMinTicks : Band1MinTicks;
            if (IsBand1(level) && (solutionTicks < min || solutionTicks > Band1MaxTicks))
                errors.Add($"{levelId}: the solution takes {solutionTicks} ticks; level {level} needs {min}-{Band1MaxTicks} (D-085).");
            return errors;
        }

        // PAX-059 §3.4: the lethal betrayals in sequence (dead ends aside). A betrayal route is the solution's start and then its own steps;
        // one that shares a longer start has run every learned answer before it. The chain is the longest run of shared
        // lengths that strictly grows, one betrayal per length, with distinct killers.
        public static List<Betrayal> SequentialChain(RoomRoutes routes)
        {
            var chain = new List<Betrayal>();
            var killers = new HashSet<string>();
            foreach (IGrouping<int, Betrayal> group in routes.Betrayals.Where(b => !IsDeadEnd(b))
                .GroupBy(b => SharedSteps(routes.Solution, b.Route)).OrderBy(g => g.Key))
            {
                Betrayal pick = group.FirstOrDefault(b => !killers.Contains(b.Killer));
                if (pick == null) continue;
                chain.Add(pick); killers.Add(pick.Killer);
            }
            return chain;
        }

        static int SharedSteps(Route solution, Route route)
        {
            int n = 0;
            while (n < solution.Steps.Count && n < route.Steps.Count && solution.Steps[n].Kind == route.Steps[n].Kind && solution.Steps[n].Label == route.Steps[n].Label) n++;
            return n;
        }

        // No tells (D-085): a trap floor (collapsing or fake) must not have an honest hazard that points at it in the
        // direction it drops the cat (down, or up for one in a ceiling), down to the next solid, lying under no other part
        // of that surface; one a trap floor's box hides is covered (the Q4 way: trap floors draw over it). A falling block sits flush inside a ceiling,
        // floor or wall until it moves.
        public static List<string> ValidateBand1Tells(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            const float eps = 1e-3f;
            Rect[] hazards = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Hazard || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Hazard))
                .Select(e => Box(e, Vector2.zero)).ToArray();
            string[] hazardNames = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Hazard || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Hazard))
                .Select(e => e.Name).ToArray();
            Rect[] trapFloors = room.Elements.Where(e => e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.FakePlatform).Select(e => Box(e, Vector2.zero)).ToArray();
            foreach (SoloRoomElement trap in room.Elements.Where(e => e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.FakePlatform))
            {
                Rect t = Box(trap, Vector2.zero);
                bool up = room.Elements.Any(c => c.Kind == SoloRoomElementKind.Ceiling && Mathf.Abs(Box(c, Vector2.zero).yMin - t.yMin) < eps
                    && Box(c, Vector2.zero).xMax >= t.xMin - eps && Box(c, Vector2.zero).xMin <= t.xMax + eps);
                float stop = FallStop(room, trap, t, up);
                for (int i = 0; i < hazards.Length; i++)
                {
                    Rect h = hazards[i];
                    bool inColumn = h.xMax > t.xMin + eps && h.xMin < t.xMax - eps && (up ? h.yMin >= t.yMax - eps && h.yMin < stop : h.yMax <= t.yMin + eps && h.yMax > stop);
                    bool hidden = trapFloors.Any(f => h.xMin >= f.xMin - eps && h.xMax <= f.xMax + eps && h.yMin >= f.yMin - eps && h.yMax <= f.yMax + eps);
                    bool singled = h.xMin >= t.xMin - eps && h.xMax <= t.xMax + eps;
                    if (inColumn && !hidden && singled)
                        errors.Add($"{levelId}: {hazardNames[i]} {Describe(h)} lies {(up ? "above" : "under")} {trap.Name} and nowhere else on its surface, so it points at the trap (no tells, D-085).");
                }
            }
            foreach (SoloRoomElement block in room.Elements.Where(e => e.Kind == SoloRoomElementKind.FallingBlock))
            {
                Rect b = Box(block, Vector2.zero);
                bool flush = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Ceiling || e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall)
                    .Any(e => { Rect r = Box(e, Vector2.zero); return b.xMin >= r.xMin - eps && b.xMax <= r.xMax + eps && b.yMin >= r.yMin - eps && b.yMax <= r.yMax + eps; });
                if (!flush) errors.Add($"{levelId}: {block.Name} {Describe(b)} isn't flush inside a ceiling, floor or wall, so it shows before it moves (no tells, D-085).");
            }
            return errors;
        }

        // PAX-059 (a kit quirk; kit-gap ticket proposed): FallingBlockTrap tests a kill against the block's pose from the tick
        // before (MovePosition lands in the next physics step) and stops testing once its travel is complete. So a block kills
        // a cat standing where it lands only if its second-to-last pose already overlaps the cat by more than the test's
        // shrink; otherwise it pins the cat without killing it. With N moving ticks and a last step r, that pose is r + one
        // step above the rest pose, and it must be below the cat's height less the shrink.
        public const float BlockKillShrink = .04f;

        public static List<string> ValidateFallingBlockLanding(string levelId, SoloRoomDefinition room, Parallax.Gameplay.Player.CatMotorConfig motor)
        {
            var errors = new List<string>();
            if (motor == null) { errors.Add($"{levelId}: no CatMotorConfig; a falling block's landing kill is undefined."); return errors; }
            float limit = motor.ColliderSize.y - BlockKillShrink;
            foreach (SoloRoomElement block in room.Elements.Where(e => e.Kind == SoloRoomElementKind.FallingBlock))
            {
                float step = block.Settings.UnitsPerTick, travel = block.Settings.TravelDistance;
                if (step <= 0f || travel <= 0f) continue;
                int ticks = Mathf.CeilToInt(travel / step - 1e-4f);
                float checkedAbove = travel - (ticks - 2) * step;
                if (ticks < 2 || checkedAbove > limit - 1e-3f)
                    errors.Add($"{levelId}: {block.Name}'s last kill test (its pose {checkedAbove:F2} u above where it lands) misses a cat standing there ({limit:F2} u at most); it would pin the cat without killing it. Change its travel (a last step of at most {limit - step:F2} u).");
            }
            return errors;
        }

        // A trap floor that gives way on the cat's own touch (a fake platform, or an Overlap, unchained, non-periodic
        // collapsing floor) must only go when the cat gets onto it. CollapsingFloorTrap counts a touch within its touchSkin
        // on every side, so a cat jumping straight up from a surface under it could set it off from below (L003's
        // Tread_R4, found in play after PAX-059a). The highest top directly under it (overlapping it in x) plus the cat's
        // height and jump height must stay HeadroomMargin below its underside less the skin. Gravity down only.
        public const float TrapFloorTouchSkin = .05f, HeadroomMargin = .1f;

        public static List<string> ValidateTrapFloorHeadroom(string levelId, SoloRoomDefinition room, Parallax.Gameplay.Player.CatMotorConfig motor)
        {
            var errors = new List<string>();
            if (motor == null) { errors.Add($"{levelId}: no CatMotorConfig; a trap floor's headroom is undefined."); return errors; }
            float reach = motor.ColliderSize.y + motor.JumpHeight;
            foreach (SoloRoomElement trap in room.Elements.Where(IsSelfCovered))
            {
                Rect t = Box(trap, Vector2.zero);
                float highest = room.Elements
                    .Where(e => e.Name != trap.Name && (e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall || e.Kind == SoloRoomElementKind.CollapsingFloor))
                    .Select(e => Box(e, Vector2.zero))
                    .Where(r => r.xMax > t.xMin + 1e-3f && r.xMin < t.xMax - 1e-3f && r.yMax < t.yMin - 1e-3f)
                    .Select(r => r.yMax).DefaultIfEmpty(float.NegativeInfinity).Max();
                if (float.IsNegativeInfinity(highest)) continue;
                float head = highest + reach, limit = t.yMin - TrapFloorTouchSkin - HeadroomMargin;
                if (head > limit + 1e-3f)
                    errors.Add($"{levelId}: {trap.Name} gives way on a touch, and a cat jumping straight up from the top at y {highest:F2} under it reaches y {head:F2}, within {TrapFloorTouchSkin + HeadroomMargin:F2} of its underside ({t.yMin:F2}); raise it to an underside of at least {head + TrapFloorTouchSkin + HeadroomMargin:F2}, or trigger it from a box over its top.");
            }
            return errors;
        }

        // A trap goes off where the cat is, never from afar (developer's play of L001 after PAX-059a: a trigger 8.5 u before
        // its block, and a block chained from it, let a cat set both off and step back to wait them out). The trigger that
        // starts a trap that is only deadly while it moves (a falling block, a hazard mover; its own trigger, or its chain
        // root's, and a root with no trigger box is set off by touching its body) must lie within NearTrapDistance of the
        // trap's own x span. Spikes and floors that give way stay deadly once set off, so waiting gains nothing; arrows are
        // checked by ValidateArrowLanes (the trigger is in the lane).
        public const float NearTrapDistance = 3f;

        public static List<string> ValidateTriggerNearTrap(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            var byName = room.Elements.ToDictionary(e => e.Name);
            foreach (SoloRoomElement trap in room.Elements.Where(IsNearTrapChecked))
            {
                SoloRoomElement root = ChainRoot(trap, byName);
                if (root.Kind == SoloRoomElementKind.Arrow) continue;
                Rect trigger = TriggerCoverage.TryTrigger(root, out Rect box) ? box : Box(root, Vector2.zero);
                Rect danger = Box(trap, Vector2.zero);
                float gap = Mathf.Max(0f, Mathf.Max(trigger.xMin - danger.xMax, danger.xMin - trigger.xMax));
                string via = root.Name == trap.Name ? "its trigger" : $"{root.Name}'s trigger (its chain root)";
                if (gap > NearTrapDistance + 1e-3f)
                    errors.Add($"{levelId}: {trap.Name} is set off by {via} at x [{trigger.xMin:F2}, {trigger.xMax:F2}], {gap:F2} u from it (at most {NearTrapDistance:F2}); a cat can set it off from afar and wait it out.");
            }
            return errors;
        }

        static bool IsNearTrapChecked(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.FallingBlock || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Hazard);

        // The first fixed solid past the trap in the direction it drops the cat, at the trap's centre.
        static float FallStop(SoloRoomDefinition room, SoloRoomElement trap, Rect t, bool up)
        {
            float x = t.center.x, stop = up ? float.PositiveInfinity : float.NegativeInfinity;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Name != trap.Name && IsFixedSolid(e)))
            {
                Rect r = Box(e, Vector2.zero);
                if (r.xMin > x || r.xMax < x) continue;
                if (up && r.yMin >= t.yMax - 1e-3f) stop = Mathf.Min(stop, r.yMin);
                if (!up && r.yMax <= t.yMin + 1e-3f) stop = Mathf.Max(stop, r.yMax);
            }
            return stop;
        }
    }
}
