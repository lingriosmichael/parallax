using System.Collections.Generic;
using Parallax.Editor.Setup;
using UnityEngine;

namespace Parallax.Editor.Routes
{
    // PAX-075 (D-079): the route rule. Per room: (a) the solution completes, every timed step keeps a
    // window of >= 12 surviving start ticks (R3) and every margin holds (R7); (b) each betrayal dies at
    // its expected killer and cause with a measured lead >= 6 (R5, R6); (c) two replays of the solution
    // give the same per-tick record. PAX-080 (D-080): a Recovers betrayal instead completes the room after its
    // RevealedBy has visibly changed; a replay that ends alive at a tick cap is a possible soft-lock (D-053).
    public static class RouteValidator
    {
        public const int WindowTicks = 12;       // D-056 (1)
        public const int LeadTicks = 6;          // D-057
        public const int SweepRange = 25;        // R12 lever 4

        public static List<string> Validate(string levelId, SoloRoomDefinition room, RoomRoutes routes)
        {
            using var session = new RouteSession();
            return Run(session, levelId, room, routes).Errors;
        }

        public static RouteReport Run(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes) =>
            RunWithThresholds(session, levelId, room, routes, LevelLayoutValidator.HasSections(room) ? LevelLayoutValidator.LoadPrecisionThresholds() : null);

        // PAX-076 (D-083) R2: the same run against given precision thresholds (only a room with sections reads them).
        public static RouteReport RunWithThresholds(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes, PrecisionThresholds thresholds)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var report = new RouteReport { LevelId = levelId };

            ReplayResult solution = RouteHarness.Replay(session, room, routes.Solution);
            report.Replays++;
            report.SolutionCompleted = solution.Completed;
            if (!solution.Completed)
                report.Errors.Add($"{levelId}: solution '{routes.Solution.Name}' does not complete ({Why(solution)}).");

            ReplayResult again = RouteHarness.Replay(session, room, routes.Solution);
            report.Replays++;
            report.Deterministic = SameRecord(solution, again);
            if (!report.Deterministic) report.Errors.Add($"{levelId}: two replays of '{routes.Solution.Name}' differ.");

            foreach (MarginResult margin in Margins(solution, routes.Solution))
            {
                report.Margins.Add(margin);
                if (!margin.Passed) report.Errors.Add($"{levelId}: {margin}.");
            }

            if (solution.Completed)
            {
                // PAX-076 (D-083) R2: a timed step wholly inside a precision section uses the section's slack.
                if (LevelLayoutValidator.HasSections(room) && thresholds == null)
                    report.Errors.Add($"{levelId}: precision sections need a PrecisionThresholds asset ({PrecisionThresholdsSetup.AssetPath}) for their route windows (D-083).");
                foreach (WindowResult window in Sweep(session, room, routes.Solution, solution, ref report.Replays))
                {
                    report.Windows.Add(window);
                    int required = RequiredWindowTicks(room, solution, window, thresholds);
                    if (window.Count < required) report.Errors.Add($"{levelId}: {window} is below {required}.");
                }
            }

            foreach (Betrayal betrayal in routes.Betrayals)
            {
                report.Replays++;
                if (betrayal.Outcome == BetrayalOutcome.Recovers) report.Recoveries.Add(CheckRecovery(session, levelId, room, betrayal, report.Errors));
                else report.Leads.Add(CheckBetrayal(session, levelId, room, betrayal, report.Errors));
            }

            // PAX-090 (D-091): a room with checkpoint sections also passes the section rules (its errors are the report's).
            if (room.HasCheckpointSections) report.Errors.AddRange(ValidateSections(session, levelId, room, routes, solution).Errors);

            report.Seconds = clock.Elapsed.TotalSeconds;
            return report;
        }

        // One betrayal alone (the §5.4 fixture and the per-betrayal tests).
        public static LeadResult CheckBetrayal(RouteSession session, string levelId, SoloRoomDefinition room, Betrayal betrayal, List<string> errors)
        {
            ReplayResult replay = RouteHarness.Replay(session, room, betrayal.Route);
            LeadResult lead = Lead(replay, betrayal);
            errors.AddRange(BetrayalErrors(levelId, betrayal, replay, lead));
            return lead;
        }

        // PAX-080 (D-080): one Recovers betrayal alone.
        public static RecoveryResult CheckRecovery(RouteSession session, string levelId, SoloRoomDefinition room, Betrayal betrayal, List<string> errors)
        {
            ReplayResult replay = RouteHarness.Replay(session, room, betrayal.Route);
            var result = new RecoveryResult
            {
                Betrayal = betrayal.Name, RevealedBy = betrayal.RevealedBy, Died = replay.Kill != null, Completed = replay.Completed,
                FirstVisibleTick = FirstVisibleChange(replay, betrayal.Route, betrayal.RevealedBy), Failure = replay.Failure,
                CompletionTick = replay.Completed ? replay.Records[replay.Records.Count - 1].Tick : -1,
            };
            string name = $"{levelId}: betrayal '{betrayal.Name}'";
            if (result.Died) errors.Add($"{name} should recover but dies ({Why(replay)}).");
            else if (!result.Completed) errors.Add($"{name} should recover but doesn't complete the room ({Why(replay)}).");
            if (result.FirstVisibleTick < 0) errors.Add($"{name}: {betrayal.RevealedBy} never changes visibly, so the betrayal never happened.");
            else if (result.Completed && result.FirstVisibleTick >= result.CompletionTick) errors.Add($"{name}: {betrayal.RevealedBy} changes visibly at t{result.FirstVisibleTick}, not before the room completes at t{result.CompletionTick}.");
            return result;
        }

        // PAX-076 (D-083) R2: WindowTicks (D-056's 12), or the section's slack when the cat's recorded position (its
        // collider centre, in the authored replay) is inside one precision section at every tick the window spans,
        // from its first start tick (AuthoredTick + Low) to its last (AuthoredTick + High).
        public static int RequiredWindowTicks(SoloRoomDefinition room, ReplayResult authored, WindowResult window, PrecisionThresholds thresholds)
        {
            if (thresholds == null || !LevelLayoutValidator.HasSections(room) || authored.Records.Count == 0) return WindowTicks;
            int last = authored.Records.Count - 1;
            int from = Mathf.Clamp(window.AuthoredTick + window.Low, 0, last), to = Mathf.Clamp(window.AuthoredTick + window.High, 0, last);
            TickRecord first = authored.Records[from];
            if (!LevelLayoutValidator.InSection(room, new Vector2(first.X, first.Y), out PrecisionSection section)) return WindowTicks;
            for (int t = from; t <= to; t++)
                if (!section.Contains(new Vector2(authored.Records[t].X, authored.Records[t].Y))) return WindowTicks;
            return thresholds.SlackTicks;
        }

        // R3/R7: each timed step alone, walking outward from d = 0 until the first failure or the range.
        public static List<WindowResult> Sweep(RouteSession session, SoloRoomDefinition room, Route route, ReplayResult authored, ref int replays, ReplayOptions start = null)
        {
            var windows = new List<WindowResult>();
            for (int i = 0; i < route.Steps.Count; i++)
            {
                RouteStep step = route.Steps[i];
                if (step.Timing == TimedMode.None) continue;
                authored.StepStartTick.TryGetValue(i, out int t0);
                var w = new WindowResult { Route = route.Name, Step = $"#{i} {step.Label}", Mode = step.Timing, AuthoredTick = t0 };
                if (!authored.Completed) { windows.Add(w); continue; }
                w.Count = 1;
                int d = 1;
                for (; d <= SweepRange; d++) { replays++; if (!Survives(session, room, route, i, step.Timing, d, t0, start)) break; w.Count++; }
                w.High = d - 1; w.OpenHigh = d > SweepRange;
                if (step.Timing == TimedMode.Shift)
                {
                    for (d = -1; d >= -SweepRange; d--) { replays++; if (!Survives(session, room, route, i, step.Timing, d, t0, start)) break; w.Count++; }
                    w.Low = d + 1; w.OpenLow = d < -SweepRange;
                }
                windows.Add(w);
            }
            return windows;
        }

        static bool Survives(RouteSession session, SoloRoomDefinition room, Route route, int step, TimedMode mode, int d, int t0, ReplayOptions start)
        {
            var options = new ReplayOptions { TimedStep = step, Mode = mode, Delta = d, AuthoredStartTick = t0, ResolveCause = false,
                StartCentre = start?.StartCentre, StartVelocity = start?.StartVelocity ?? Vector2.zero };
            return RouteHarness.Replay(session, room, route, options).Completed;
        }

        public static List<MarginResult> Margins(ReplayResult replay, Route route)
        {
            var results = new List<MarginResult>();
            foreach (RouteStep step in route.Steps)
            {
                if (step.Kind != RouteStepKind.Margin) continue;
                var m = new MarginResult { Route = route.Name, Name = step.MarginName, AtLeast = step.MarginAtLeast };
                m.From = FirstTick(replay, step.MarginFrom);
                m.To = FirstTick(replay, step.MarginTo);
                if (m.From >= 0 && m.To >= 0) m.Value = m.To - m.From;
                results.Add(m);
            }
            return results;
        }

        // The first tick whose end-of-tick record satisfies the condition.
        public static int FirstTick(ReplayResult replay, RouteCondition condition)
        {
            var probe = new ReplayResult { Elements = replay.Elements };
            var view = new RouteView { Result = probe };
            for (int i = 0; i < replay.Records.Count; i++)
            {
                probe.Records.Add(replay.Records[i]);
                if (i > 0 && condition.Test(view)) return replay.Records[i].Tick;
            }
            return -1;
        }

        // R5: min(kill tick, the killer's first lethal tick where its kind exposes one) - first visible change of RevealedBy.
        public static LeadResult Lead(ReplayResult replay, Betrayal betrayal)
        {
            var lead = new LeadResult { Betrayal = betrayal.Name, ExpectedKiller = betrayal.Killer, ExpectedCause = betrayal.Cause, RevealedBy = betrayal.RevealedBy };
            if (replay.Kill == null) return lead;
            lead.KillTick = replay.Kill.Tick;
            lead.Killer = replay.Kill.Killer;
            lead.CauseKnown = replay.Kill.CauseKnown; lead.Cause = replay.Kill.Cause;
            int end = lead.KillTick;
            if (replay.ArrowFirstLethalTick.TryGetValue(betrayal.Killer, out int lethal) && lethal >= 0) { lead.FirstLethalTick = lethal; if (lethal < end) end = lethal; }
            lead.FirstVisibleTick = FirstVisibleChange(replay, betrayal.Route, betrayal.RevealedBy);
            // PAX-099 (D-106): a repeating killer (one that fired again before the kill) is revealed by the shot that kills, not
            // by its first one, which may have flown long before, anywhere on screen or off it.
            int shot = KillingShotTick(replay, betrayal.Killer);
            if (shot >= 0 && betrayal.RevealedBy == betrayal.Killer && shot > lead.FirstVisibleTick) lead.FirstVisibleTick = shot;
            if (lead.FirstVisibleTick >= 0) lead.Lead = end - lead.FirstVisibleTick;
            return lead;
        }

        static IEnumerable<string> BetrayalErrors(string levelId, Betrayal betrayal, ReplayResult replay, LeadResult lead)
        {
            string name = $"{levelId}: betrayal '{betrayal.Name}'";
            if (replay.Kill == null) { yield return $"{name} does not die ({Why(replay)})."; yield break; }
            if (replay.Kill.Candidates.Count != 1) yield return $"{name}: ambiguous kill at t{replay.Kill.Tick} (candidates: {(replay.Kill.Candidates.Count == 0 ? "none" : string.Join(", ", replay.Kill.Candidates))}).";
            else if (replay.Kill.Killer != betrayal.Killer) yield return $"{name} is killed by {replay.Kill.Killer}, not {betrayal.Killer}.";
            if (!replay.Kill.CauseKnown) yield return $"{name}: death cause was never reported.";
            else if (replay.Kill.Cause != betrayal.Cause) yield return $"{name} dies of {replay.Kill.Cause}, not {betrayal.Cause}.";
            if (lead.FirstVisibleTick < 0) yield return $"{name}: {betrayal.RevealedBy} never changes visibly before the kill.";
            else if (lead.Lead < LeadTicks) yield return $"{name}: lead {lead.Lead} ticks is below {LeadTicks} ({lead}).";
        }

        // PAX-099 (D-106): the harness tick on which the killer's last fire before the kill showed, if it had fired before that
        // (a repeating trap); -1 for a trap that fired once, or never.
        static int KillingShotTick(ReplayResult replay, string killer)
        {
            int e = replay.Elements.IndexOf(killer), k = replay.Records.Count - 1;
            if (e < 0 || k < 1) return -1;
            int last = replay.Records[k].FireTick[e];
            if (last < 0) return -1;
            int i = k;
            while (i > 0 && replay.Records[i - 1].FireTick[e] == last) i--;
            bool earlier = false;
            for (int j = 0; j < i; j++) if (replay.Records[j].FireTick[e] >= 0) { earlier = true; break; }
            return earlier ? replay.Records[i].Tick : -1;
        }

        // PAX-090 (D-091): for a Route.FromSection replay the betrayal starts at the rewind, so its reveal is the first change
        // after it (against the room as restored); for every other route, ReplayResult.FirstVisibleChange as before.
        static int FirstVisibleChange(ReplayResult replay, Route route, string element)
        {
            int from = route != null && route.RewindSection != null ? RewindIndex(replay) : 0;
            if (from <= 0) return replay.FirstVisibleChange(element);
            int e = replay.Elements.IndexOf(element);
            if (e < 0) return -1;
            for (int i = from + 1; i < replay.Records.Count; i++) if (replay.Records[i].Signature[e] != replay.Records[from].Signature[e]) return replay.Records[i].Tick;
            return -1;
        }

        public static bool SameRecord(ReplayResult a, ReplayResult b)
        {
            if (a.Records.Count != b.Records.Count || a.Completed != b.Completed) return false;
            for (int i = 0; i < a.Records.Count; i++) if (!a.Records[i].SameAs(b.Records[i])) return false;
            return (a.Kill == null) == (b.Kill == null) && (a.Kill == null || (a.Kill.Tick == b.Kill.Tick && a.Kill.Killer == b.Kill.Killer));
        }

        static string Why(ReplayResult r)
        {
            if (r.AliveAtCap && r.Records.Count > 0) return $"ends alive at t{r.Records[r.Records.Count - 1].Tick} ({r.Failure}): possible soft-lock (D-053)";
            return r.Failure ?? (r.Kill != null ? $"died at t{r.Kill.Tick}, killer {r.Kill.Killer ?? "ambiguous: " + string.Join("/", r.Kill.Candidates)}" : "goal not reached");
        }
        // ---------- PAX-090 (D-091): checkpoint sections ----------

        public const int SectionBudgetTicks = 1000;   // R5: enforced (20 s)
        public const int SectionTargetTicks = 750;    // R5: the design target (15 s); over it needs a sentence in the sketch
        public const int RespawnSafeTicks = 50;       // standing still at a checkpoint survives 1 s
        public const int RewindDelayTicks = 10;       // the second rewind-equality replay dies this much later
        public const int RewindRecordTicks = 100;     // and both are compared for this many ticks after the rewind

        /// <summary>The record index of the rewind's reset tick: the first record after a death hold (0: none).</summary>
        public static int RewindIndex(ReplayResult replay)
        {
            for (int i = 1; i < replay.Records.Count; i++) if (replay.Records[i - 1].Holding && !replay.Records[i].Holding) return i;
            return 0;
        }

        /// <summary>D-079 (3)'s determinism, extended: replay `route` to the section's gate and die at once, and again dying
        /// delayTicks later; the recordTicks after the two rewinds (standing still) must match record for record, and at the
        /// rewind every element must look as it did at the gate, with the room clock back at the gate's room tick.</summary>
        public static RewindCheck CheckRewind(RouteSession session, SoloRoomDefinition room, Route route, string checkpointSection, int delayTicks, int recordTicks)
        {
            var check = new RewindCheck { Section = checkpointSection };
            string marker = null;
            foreach (CheckpointSection section in room.CheckpointSections ?? System.Array.Empty<CheckpointSection>())
                if (section.Name == checkpointSection && section.HasGate) marker = section.MarkerName;
            if (marker == null) { check.Difference = "no such section with a gate"; return check; }

            ReplayResult a = RouteHarness.Replay(session, room, Route.FromSection(route, checkpointSection, 0, "rewind at the gate", R.Release(), R.ReleaseClimb(), R.For(recordTicks)));
            ReplayResult b = RouteHarness.Replay(session, room, Route.FromSection(route, checkpointSection, delayTicks, "rewind later", R.Release(), R.ReleaseClimb(), R.For(recordTicks)));
            int ia = RewindIndex(a), ib = RewindIndex(b);
            check.Rewound = ia > 0 && ib > 0;
            if (!check.Rewound) { check.Difference = a.Failure ?? b.Failure ?? "no rewind happened"; return check; }
            check.RewindTickA = a.Records[ia].Tick; check.RewindTickB = b.Records[ib].Tick;

            check.GateTick = a.FirstVisibleChange(marker);
            int gate = a.Records.FindIndex(r => r.Tick == check.GateTick);
            if (gate < 0) { check.Difference = "the gate's marker never lit"; return check; }
            check.StateMatchesGate = a.Records[gate].RoomLifeTick == a.Records[ia].RoomLifeTick;
            if (!check.StateMatchesGate) check.Difference = $"room tick at the rewind {a.Records[ia].RoomLifeTick}, at the gate {a.Records[gate].RoomLifeTick}";
            int elements = a.Elements.Count - 2;   // not Cat.Gravity or Cat.Inverted: the cat itself respawns
            for (int e = 0; e < elements && check.StateMatchesGate; e++)
            {
                if (a.Records[gate].FireTick[e] == a.Records[ia].FireTick[e] && a.Records[gate].Signature[e] == a.Records[ia].Signature[e]) continue;
                check.StateMatchesGate = false;
                check.Difference = $"{a.Elements[e]} differs from the gate at the rewind (fire t{a.Records[gate].FireTick[e]} vs t{a.Records[ia].FireTick[e]})";
            }

            check.Equal = a.Records.Count - ia == b.Records.Count - ib;
            if (!check.Equal) check.Difference ??= $"{a.Records.Count - ia} vs {b.Records.Count - ib} records after the rewind";
            for (int i = 0; check.Equal && i < a.Records.Count - ia; i++)
            {
                TickRecord x = a.Records[ia + i], y = b.Records[ib + i];
                int tick = y.Tick;
                y.Tick = x.Tick;
                check.Equal = x.SameAs(y);
                y.Tick = tick;
                if (!check.Equal) check.Difference ??= $"{i} ticks after the rewind: {x} vs {y}{ElementDifference(a, x, y)}";
            }
            if (check.Equal && (a.Kill == null) != (b.Kill == null)) { check.Equal = false; check.Difference ??= "only one dies after the rewind"; }
            return check;
        }

        static string ElementDifference(ReplayResult replay, TickRecord x, TickRecord y)
        {
            for (int e = 0; e < x.FireTick.Length; e++)
                if (x.FireTick[e] != y.FireTick[e] || x.Signature[e] != y.Signature[e])
                    return $"; {(e < replay.Elements.Count ? replay.Elements[e] : "#" + e)} differs (fire t{x.FireTick[e]} vs t{y.FireTick[e]})";
            return $"; cat x {x.X:R}/{y.X:R} y {x.Y:R}/{y.Y:R} vx {x.Vx:R}/{y.Vx:R} vy {x.Vy:R}/{y.Vy:R} holding {x.Holding}/{y.Holding} climbing {x.IsClimbing}/{y.IsClimbing} complete {x.Complete}/{y.Complete}";
        }

        /// <summary>The route half of the section rules (LevelLayoutValidator.ValidateSections is the layout half): the
        /// solution passes every gate, in order; each section's time on the solution, from the gate (tick 0 for section 0)
        /// to the next gate or the door, is at most SectionBudgetTicks; standing still at each gate's checkpoint after a
        /// rewind survives RespawnSafeTicks; and each gate's rewind is exact (CheckRewind).</summary>
        public static SectionReport ValidateSections(RouteSession session, string levelId, SoloRoomDefinition room, RoomRoutes routes, ReplayResult solution = null)
        {
            var report = new SectionReport();
            CheckpointSection[] sections = room.CheckpointSections ?? System.Array.Empty<CheckpointSection>();
            if (sections.Length == 0) return report;
            solution ??= RouteHarness.Replay(session, room, routes.Solution);

            int from = 0;
            for (int i = 0; i < sections.Length; i++)
            {
                int to;
                if (i + 1 < sections.Length)
                {
                    to = solution.FirstVisibleChange(sections[i + 1].MarkerName);
                    if (to < 0) { report.Errors.Add($"{levelId}: the solution never passes the gate of section '{sections[i + 1].Name}'."); break; }
                    if (to <= from) report.Errors.Add($"{levelId}: the solution passes '{sections[i + 1].Name}' (t{to}) before '{sections[i].Name}' (t{from}).");
                }
                else if (solution.Completed) to = solution.Records[solution.Records.Count - 1].Tick;
                else { report.Errors.Add($"{levelId}: the solution doesn't complete, so section '{sections[i].Name}' has no time."); break; }
                var timing = new SectionTiming { Section = sections[i].Name, FromTick = from, ToTick = to };
                report.Timings.Add(timing);
                if (timing.Ticks > SectionBudgetTicks) report.Errors.Add($"{levelId}: section '{sections[i].Name}' takes {timing.Ticks} ticks on the solution, over {SectionBudgetTicks} (D-091 R5).");
                from = to;
            }

            for (int i = 1; i < sections.Length; i++)
            {
                string name = sections[i].Name;
                ReplayResult still = RouteHarness.Replay(session, room, Route.FromSection(routes.Solution, name, "stand still at " + name, R.Release(), R.ReleaseClimb(), R.For(RespawnSafeTicks)));
                int rewind = RewindIndex(still);
                var respawn = new RespawnResult { Section = name, Survived = -1 };
                if (rewind <= 0) report.Errors.Add($"{levelId}: section '{name}': no rewind to stand still after ({still.Failure ?? "no death hold"}).");
                else
                {
                    int start = still.Records[rewind].Tick;
                    respawn.Survived = still.Kill != null ? still.Kill.Tick - start : still.Records[still.Records.Count - 1].Tick - start;
                    if (still.Kill != null && respawn.Survived < RespawnSafeTicks)
                        report.Errors.Add($"{levelId}: standing still at section '{name}''s checkpoint dies {respawn.Survived} ticks after the respawn (killer {still.Kill.Killer ?? "ambiguous"}), under {RespawnSafeTicks}.");
                }
                report.Respawns.Add(respawn);

                RewindCheck check = CheckRewind(session, room, routes.Solution, name, RewindDelayTicks, RewindRecordTicks);
                report.Rewinds.Add(check);
                if (!check.Rewound || !check.StateMatchesGate || !check.Equal) report.Errors.Add($"{levelId}: the rewind to section '{name}' isn't exact ({check}).");
            }
            return report;
        }
    }

    public sealed class RewindCheck
    {
        public string Section, Difference;
        public bool Rewound, StateMatchesGate, Equal;
        public int GateTick = -1, RewindTickA = -1, RewindTickB = -1;
        public override string ToString() => $"{Section}: gate t{GateTick}, rewinds t{RewindTickA} and t{RewindTickB}, rewound {Rewound}, as at the gate {StateMatchesGate}, equal {Equal}{(Difference != null ? " (" + Difference + ")" : "")}";
    }

    public sealed class SectionTiming
    {
        public string Section; public int FromTick, ToTick;
        public int Ticks => ToTick - FromTick;
        public override string ToString() => $"{Section}: t{FromTick}-t{ToTick}, {Ticks} ticks ({Ticks * Parallax.Core.TickTime.SecondsPerTick:F1} s){(Ticks > RouteValidator.SectionTargetTicks ? ", over the 750-tick target" : "")}";
    }

    public sealed class RespawnResult
    {
        public string Section; public int Survived;
        public override string ToString() => $"{Section}: standing still survives {(Survived < 0 ? "?" : Survived.ToString())} ticks";
    }

    public sealed class SectionReport
    {
        public List<string> Errors = new();
        public List<SectionTiming> Timings = new();
        public List<RespawnResult> Respawns = new();
        public List<RewindCheck> Rewinds = new();
    }
}
