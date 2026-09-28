using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Parallax.Core;
using Parallax.Core.Cameras;
using Parallax.Editor.Routes;
using Parallax.Gameplay.Cameras;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-060 (D-093): band 2's content rules (levels 11-20). ValidateBand2Content reads the layout and the declared routes;
    // ValidateBand2Duration takes the solution's length; Band2Chaos, Band2ElementBetrayals and Band2PrecisionSteps read
    // replays (pure functions of the records, so the rule tests can feed them synthetic ones); ValidateBand2Replays runs
    // the replays for a real level. Separate from Validate(), like the band-1 rules.
    public static partial class LevelLayoutValidator
    {
        public const int Band2MinLethal = 8, Band2MinSequence = 6, Band2MinDeadEnds = 2;
        public const int Band2MinTicks = 1100, Band2MaxTicks = 2500;   // §13 R4: the floor lowered from 1500
        public const int Band2MinSections = 2, Band2MaxSections = 3;
        public const int ChaosWindowTicks = 60, ChaosMinElements = 5;
        public const int Band2MinElementBetrayals = 3;
        public const int MaxTrapsPerAnswer = 3, MaxBaitTraps = 1;
        public static readonly int[] Band2PrecisionLevels = { 12, 15, 19 };
        // The one level whose precision section is a single beat: one section, at most one required jump and one timed step in it.
        public const int Band2OneBeatLevel = 12;
        public static readonly string[] AnswerCodes = { "J", "NJ", "W", "NW", "SS", "B", "OL", "LW", "BAIT" };
        // "T4 [BAIT]: ..." or "Dead end D1 [OL]: ...": the trap's label, then its learned answer (P7).
        static readonly Regex AnswerTag = new(@"^(?<trap>.+?) \[(?<code>[A-Z]+)\]: ", RegexOptions.CultureInvariant);

        public static bool IsBand2(int level) => level >= 11 && level <= 20;

        // D-093: each half-A level's new element. Half B names its elements when it is built (a level's kind covers them all).
        public static bool Band2Element(int level, out string kind, out Func<SoloRoomElement, bool> isKind)
        {
            switch (level)
            {
                case 11: kind = "spear"; isKind = e => e.Kind == SoloRoomElementKind.Arrow && e.Settings.Arrow.Spear; return true;
                case 12: kind = "inverter"; isKind = e => e.Kind == SoloRoomElementKind.Inverter; return true;
                case 13: kind = "geyser"; isKind = e => e.Kind == SoloRoomElementKind.Geyser; return true;
                case 14: kind = "vine"; isKind = e => e.Kind == SoloRoomElementKind.Vine; return true;
                case 15: kind = "storm cloud"; isKind = e => e.Kind == SoloRoomElementKind.StormCloud; return true;
                case 18: kind = "storm cloud, geyser or vine"; isKind = e => e.Kind is SoloRoomElementKind.StormCloud or SoloRoomElementKind.Geyser or SoloRoomElementKind.Vine; return true;
                default: kind = null; isKind = null; return false;
            }
        }

        public static List<string> ValidateBand2Content(string levelId, int level, SoloRoomDefinition room, RoomRoutes routes)
        {
            var errors = new List<string>();
            if (!IsBand2(level)) return errors;
            Betrayal[] dies = routes.Betrayals.Where(b => b.Outcome == BetrayalOutcome.Dies).ToArray();
            int lethal = dies.Select(b => b.Killer).Distinct().Count();
            if (lethal < Band2MinLethal)
                errors.Add($"{levelId}: {lethal} lethal betrayals (distinct killers); band 2 needs at least {Band2MinLethal} (D-093).");
            int chain = SequentialChain(routes).Count;
            if (chain < Band2MinSequence)
                errors.Add($"{levelId}: {chain} lethal betrayals in sequence; band 2 needs at least {Band2MinSequence} (D-093).");
            int deadEnds = routes.Betrayals.Count(IsDeadEnd);
            if (deadEnds < Band2MinDeadEnds)
                errors.Add($"{levelId}: {deadEnds} dead end(s) (Dies routes named \"{DeadEndPrefix}...\", plus Recovers routes); band 2 needs at least {Band2MinDeadEnds} (D-093).");

            // D-085's door distance, as band 1 checks it (§2.2: everything else as band 1).
            SoloRoomElement? checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Cast<SoloRoomElement?>().FirstOrDefault();
            SoloRoomElement? door = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Cast<SoloRoomElement?>().FirstOrDefault();
            if (checkpoint.HasValue && door.HasValue)
            {
                float floor = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Floor).Select(e => Box(e, Vector2.zero).yMax).DefaultIfEmpty(0f).Min();
                float roof = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Ceiling).Select(e => Box(e, Vector2.zero).yMin).DefaultIfEmpty(floor).Max();
                Vector2 d = door.Value.Position - checkpoint.Value.Position;
                if (Mathf.Abs(d.x) < room.Width * .5f && Mathf.Abs(d.y) < (roof - floor) * .5f)
                    errors.Add($"{levelId}: the door is {Mathf.Abs(d.x):F2} u across and {Mathf.Abs(d.y):F2} u up from the start; it must be at least half the room's width ({room.Width * .5f:F2}) or height ({(roof - floor) * .5f:F2}) away (D-085, D-093).");
            }

            int sections = room.CheckpointSections?.Length ?? 0;
            if (sections < Band2MinSections || sections > Band2MaxSections)
                errors.Add($"{levelId}: {sections} checkpoint sections; band 2 needs {Band2MinSections}-{Band2MaxSections} (D-091, D-093).");

            PrecisionSection[] precision = room.PrecisionSections ?? Array.Empty<PrecisionSection>();
            if (precision.Length > 0 && !Band2PrecisionLevels.Contains(level))
                errors.Add($"{levelId}: level {level} has a precision section; band 2 puts them only in levels {string.Join(", ", Band2PrecisionLevels)} (D-093).");
            if (level == Band2OneBeatLevel)
            {
                if (precision.Length > 1) errors.Add($"{levelId}: {precision.Length} precision sections; level {level}'s is one beat (D-093).");
                foreach (PrecisionSection s in precision)
                {
                    int jumps = room.RequiredJumps.Count(j => s.Contains(new Vector2(j.TakeoffX, j.TakeoffPawHeight)) && s.Contains(new Vector2(j.LandingX, j.LandingPawHeight)));
                    if (jumps > 1) errors.Add($"{levelId}: {jumps} required jumps inside precision section {s.Name}; level {level}'s is one beat (D-093).");
                }
            }

            // P7 through the answer tags: every betrayal carries one; counted per trap (the label before the tag), dead ends aside.
            var codeTraps = new Dictionary<string, HashSet<string>>();
            foreach (Betrayal b in routes.Betrayals)
            {
                Match m = AnswerTag.Match(b.Name);
                if (!m.Success || !AnswerCodes.Contains(m.Groups["code"].Value))
                {
                    errors.Add($"{levelId}: betrayal '{b.Name}' has no answer tag (\"T4 [BAIT]: ...\", one of {string.Join(" ", AnswerCodes)}) (D-093).");
                    continue;
                }
                if (IsDeadEnd(b)) continue;
                string code = m.Groups["code"].Value;
                if (!codeTraps.TryGetValue(code, out HashSet<string> traps)) codeTraps[code] = traps = new HashSet<string>();
                traps.Add(m.Groups["trap"].Value);
            }
            foreach (KeyValuePair<string, HashSet<string>> pair in codeTraps)
            {
                int max = pair.Key == "BAIT" ? MaxBaitTraps : MaxTrapsPerAnswer;
                if (pair.Value.Count > max)
                    errors.Add($"{levelId}: answer {pair.Key} is the answer to {pair.Value.Count} traps ({string.Join(", ", pair.Value.OrderBy(t => t))}); at most {max} (P7, D-093).");
            }
            return errors;
        }

        public static List<string> ValidateBand2Duration(string levelId, int level, int solutionTicks)
        {
            var errors = new List<string>();
            if (IsBand2(level) && (solutionTicks < Band2MinTicks || solutionTicks > Band2MaxTicks))
                errors.Add($"{levelId}: the solution takes {solutionTicks} ticks; band 2 needs {Band2MinTicks}-{Band2MaxTicks} (D-093).");
            return errors;
        }

        // ---------- the chaos moment ----------

        public sealed class ChaosOnset { public string Element; public int Tick; public bool InView; }
        public sealed class ChaosResult
        {
            public int FromTick = -1, InViewCount;
            public List<ChaosOnset> Onsets = new();   // the best window's first onset per element
            public override string ToString() => FromTick < 0 ? "no onsets"
                : $"t{FromTick}-t{FromTick + ChaosWindowTicks - 1}: {InViewCount} in view of {Onsets.Count} ({string.Join(", ", Onsets.Select(o => $"{o.Element} t{o.Tick}{(o.InView ? "" : " (off screen)")}"))})";
        }

        // An onset: the element's look changes at a tick after a tick with no change (or it's the first change). The cat's
        // extras, gate markers and doors aren't counted. inView(recordIndex, elementIndex) says whether the change is on
        // screen then. The best window is the 60-tick one with the most distinct elements in view (ties: the earliest).
        public static ChaosResult Band2Chaos(ReplayResult replay, SoloRoomDefinition room, Func<int, int, bool> inView)
        {
            var doors = new HashSet<string>(room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Select(e => e.Name));
            var onsets = new List<(int index, int element)>();
            List<TickRecord> r = replay.Records;
            for (int e = 0; e < replay.Elements.Count; e++)
            {
                string name = replay.Elements[e];
                if (name == R.CatGravity || name == R.CatInverted || name.EndsWith("_Marker", StringComparison.Ordinal) || doors.Contains(name)) continue;
                for (int i = 1; i < r.Count; i++)
                    if (r[i].Signature[e] != r[i - 1].Signature[e] && (i == 1 || r[i - 1].Signature[e] == r[i - 2].Signature[e])) onsets.Add((i, e));
            }
            var best = new ChaosResult();
            foreach ((int start, _) in onsets.OrderBy(o => o.index))
            {
                int from = r[start].Tick;
                var window = new Dictionary<int, ChaosOnset>();
                foreach ((int index, int element) in onsets.Where(o => r[o.index].Tick >= from && r[o.index].Tick < from + ChaosWindowTicks).OrderBy(o => o.index))
                {
                    bool visible = inView(index, element);
                    if (window.TryGetValue(element, out ChaosOnset seen)) { if (visible && !seen.InView) { seen.InView = true; seen.Tick = r[index].Tick; } continue; }
                    window[element] = new ChaosOnset { Element = replay.Elements[element], Tick = r[index].Tick, InView = visible };
                }
                int count = window.Values.Count(o => o.InView);
                if (count > best.InViewCount || best.FromTick < 0)
                    best = new ChaosResult { FromTick = from, InViewCount = count, Onsets = window.Values.OrderBy(o => o.Tick).ToList() };
            }
            return best;
        }

        public static List<string> ValidateBand2Chaos(string levelId, int level, ChaosResult chaos)
        {
            var errors = new List<string>();
            if (IsBand2(level) && chaos.InViewCount < ChaosMinElements)
                errors.Add($"{levelId}: the chaos moment has {chaos.InViewCount} elements changing in view within {ChaosWindowTicks} ticks ({chaos}); band 2 needs at least {ChaosMinElements} (D-093).");
            return errors;
        }

        // The camera tell rule's view model at 16:9, worst case: a change is in view at a record when its rendered bounds
        // (or, once it has vanished, the bounds it was last drawn at) overlap the view in all 24 camera cases.
        public static Func<int, int, bool> InView16x9(ReplayResult replay, SoloRoomDefinition room, LevelCameraConfig camera)
        {
            const float aspect = 16f / 9f;
            Bounds frameBounds = SoloRoomBuilder.ComputeRoomBounds(room, camera.ViewMargin);
            Vector2 frameCentre = (Vector2)frameBounds.center - room.Origin, frameSize = frameBounds.size;
            if (CameraMath.IsFitMode(frameSize, camera.MaxViewHeight, aspect)) return (i, e) => true;
            var views = new List<Rect[]>();
            foreach (int fps in CameraTellFramesPerSecond)
            foreach (float phase in CameraTellPhases)
            foreach (float direction in CameraTellStartDirections)
                views.Add(ViewPerRecord(replay, frameCentre, frameSize, aspect, camera, fps, phase, direction));
            return (i, e) => ChangeBounds(replay.Records, e, i, out Rect b) && views.All(v => b.Overlaps(v[i]));
        }

        // OnScreenLead's camera loop (the level camera's Start, then one CameraMath.Step per rendered frame), keeping the
        // view of the latest frame rendered at or before each record's tick.
        static Rect[] ViewPerRecord(ReplayResult replay, Vector2 frameCentre, Vector2 frameSize, float aspect, LevelCameraConfig camera, int framesPerSecond, float phase, float startDirection)
        {
            var p = new CameraMath.FollowParams(camera.MaxViewHeight, camera.LookAhead, camera.LookAheadFlipDistance, camera.DeadZoneHalfExtents, camera.SmoothTime, camera.MaxSpeed);
            List<TickRecord> records = replay.Records;
            var views = new Rect[records.Count];
            Vector2 start = new(records[0].CatX, records[0].CatY);
            var state = new CameraMath.FollowState { AnchorX = start.x, LastDirection = startDirection };
            float viewHeight = CameraMath.Step(ref state, start, frameCentre, frameSize, aspect, p, true, 0f);
            state.Velocity = Vector2.zero; state.AnchorX = start.x;
            double tick = TickTime.SecondsPerTick, frame = 1.0 / framesPerSecond;
            int f = 0;
            for (int k = 0; k < records.Count; k++)
            {
                while (true)
                {
                    double time = (f + phase) * frame;
                    int frameTick = (int)Math.Floor(time / tick + 1e-9);
                    if (frameTick > k) break;
                    float alpha = (float)(time / tick - frameTick);
                    Vector2 drawn = frameTick == 0 ? start : Vector2.Lerp(new Vector2(records[frameTick - 1].CatX, records[frameTick - 1].CatY), new Vector2(records[frameTick].CatX, records[frameTick].CatY), alpha);
                    viewHeight = CameraMath.Step(ref state, drawn, frameCentre, frameSize, aspect, p, false, (float)frame);
                    f++;
                }
                var half = new Vector2(viewHeight * .5f * aspect, viewHeight * .5f);
                views[k] = Rect.MinMaxRect(state.Centre.x - half.x, state.Centre.y - half.y, state.Centre.x + half.x, state.Centre.y + half.y);
            }
            return views;
        }

        // ---------- the level's element in >= 3 betrayals ----------

        // A Dies or Recovers betrayal uses the level's element when its killer or reveal is one (Cat.Inverted counts for
        // the inverter), or when its own part of the replay (from its first step that isn't the solution's, or from the
        // rewind) shows the element acting on the cat: climbing (vine), standing on a stuck shaft (spear), launched while
        // inside a geyser's drawn column (geyser), the inversion cue on (inverter).
        public static List<string> Band2ElementBetrayals(int level, SoloRoomDefinition room, RoomRoutes routes, IReadOnlyDictionary<string, ReplayResult> replays, Vector2 catSize)
        {
            var used = new List<string>();
            if (!Band2Element(level, out _, out Func<SoloRoomElement, bool> isKind)) return used;
            var names = new HashSet<string>(room.Elements.Where(isKind).Select(e => e.Name));
            foreach (Betrayal b in routes.Betrayals)
            {
                bool direct = names.Contains(b.Killer) && b.Outcome == BetrayalOutcome.Dies || names.Contains(b.RevealedBy) || (level == 12 && b.RevealedBy == R.CatInverted);
                if (direct || (replays.TryGetValue(b.Name, out ReplayResult replay) && ActsOnCat(level, names, routes.Solution, b.Route, replay, catSize)))
                    used.Add(b.Name);
            }
            return used;
        }

        static bool ActsOnCat(int level, HashSet<string> names, Route solution, Route route, ReplayResult replay, Vector2 catSize)
        {
            int from = route.RewindSection != null ? RouteValidator.RewindIndex(replay) : 0;
            int own = SharedSteps(solution, route);
            if (from == 0 && own > 0 && replay.StepStartTick.TryGetValue(own, out int ownTick))
                from = Math.Max(0, replay.Records.FindIndex(x => x.Tick >= ownTick));
            int inverted = replay.Elements.IndexOf(R.CatInverted);
            int[] kinds = names.Select(n => replay.Elements.IndexOf(n)).Where(i => i >= 0).ToArray();
            for (int i = Math.Max(1, from); i < replay.Records.Count; i++)
            {
                TickRecord t = replay.Records[i];
                switch (level)
                {
                    case 11: if (t.Grounded && t.Ground != null && t.Ground.EndsWith("_Shaft", StringComparison.Ordinal) && names.Contains(t.Ground.Substring(0, t.Ground.Length - 6))) return true; break;
                    case 12: if (inverted >= 0 && t.Signature[inverted] != replay.Records[0].Signature[inverted]) return true; break;
                    case 13:
                        var cat = new Rect(t.X - catSize.x * .5f, t.Y - catSize.y * .5f, catSize.x, catSize.y);
                        if (Mathf.Abs(t.Vy) >= GeyserLaunchEvidence && kinds.Any(k => t.Rendered[k] && t.RenderBounds[k].Overlaps(cat))) return true;
                        break;
                    case 14: if (t.IsClimbing) return true; break;
                    // Level 18: a launch (as 13) or a climb (as 14); the cloud acts only by killing, which the killer shows.
                    case 18:
                        if (t.IsClimbing) return true;
                        var launched = new Rect(t.X - catSize.x * .5f, t.Y - catSize.y * .5f, catSize.x, catSize.y);
                        if (Mathf.Abs(t.Vy) >= GeyserLaunchEvidence && kinds.Any(k => t.Rendered[k] && t.RenderBounds[k].Overlaps(launched))) return true;
                        break;
                }
            }
            return false;
        }

        // A launch is 14 u/s (D-088); a jump is 9.8. Anything at 13 or more inside a drawn column was pushed by it.
        const float GeyserLaunchEvidence = 13f;

        public static List<string> ValidateBand2Element(string levelId, int level, IReadOnlyCollection<string> used)
        {
            var errors = new List<string>();
            if (IsBand2(level) && Band2Element(level, out string kind, out _) && used.Count < Band2MinElementBetrayals)
                errors.Add($"{levelId}: the {kind} is used in {used.Count} lethal or Recovers betrayal(s) ({string.Join("; ", used)}); band 2 needs at least {Band2MinElementBetrayals} (D-093).");
            return errors;
        }

        // Level 12's one beat: the timed steps whose authored start is inside its precision section.
        public static List<string> Band2PrecisionSteps(SoloRoomDefinition room, Route solution, ReplayResult replay)
        {
            var steps = new List<string>();
            foreach (PrecisionSection s in room.PrecisionSections ?? Array.Empty<PrecisionSection>())
                for (int i = 0; i < solution.Steps.Count; i++)
                {
                    if (solution.Steps[i].Timing == TimedMode.None || !replay.StepStartTick.TryGetValue(i, out int t)) continue;
                    TickRecord at = replay.Records.FirstOrDefault(x => x.Tick == t);
                    if (at != null && s.Contains(new Vector2(at.X, at.Y))) steps.Add($"#{i} {solution.Steps[i].Label}");
                }
            return steps;
        }

        public static List<string> ValidateBand2PrecisionSteps(string levelId, int level, IReadOnlyCollection<string> steps)
        {
            var errors = new List<string>();
            if (level == Band2OneBeatLevel && steps.Count > 1)
                errors.Add($"{levelId}: {steps.Count} timed steps start inside the precision section ({string.Join(", ", steps)}); level {level}'s is one beat (D-093).");
            return errors;
        }

        // ---------- a real level: the replays ----------

        public sealed class Band2Report
        {
            public ChaosResult Chaos; public List<string> ElementBetrayals = new(), PrecisionSteps = new();
            public override string ToString() => $"  chaos: {Chaos}\n  element in: {ElementBetrayals.Count} ({string.Join("; ", ElementBetrayals)})\n  precision steps: {PrecisionSteps.Count}";
        }

        public static List<string> ValidateBand2Replays(RouteSession session, string levelId, int level, SoloRoomDefinition room, RoomRoutes routes, LevelCameraConfig camera, Vector2 catSize, out Band2Report report)
        {
            var errors = new List<string>();
            report = new Band2Report();
            ReplayResult solution = RouteHarness.Replay(session, room, routes.Solution);
            report.Chaos = Band2Chaos(solution, room, InView16x9(solution, room, camera));
            errors.AddRange(ValidateBand2Chaos(levelId, level, report.Chaos));
            var replays = new Dictionary<string, ReplayResult>();
            foreach (Betrayal b in routes.Betrayals) replays[b.Name] = RouteHarness.Replay(session, room, b.Route, new ReplayOptions { ResolveCause = false });
            report.ElementBetrayals = Band2ElementBetrayals(level, room, routes, replays, catSize);
            errors.AddRange(ValidateBand2Element(levelId, level, report.ElementBetrayals));
            report.PrecisionSteps = Band2PrecisionSteps(room, routes.Solution, solution);
            errors.AddRange(ValidateBand2PrecisionSteps(levelId, level, report.PrecisionSteps));
            return errors;
        }
    }
}
