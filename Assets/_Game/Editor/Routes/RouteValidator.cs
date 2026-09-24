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
                FirstVisibleTick = replay.FirstVisibleChange(betrayal.RevealedBy), Failure = replay.Failure,
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
            lead.FirstVisibleTick = replay.FirstVisibleChange(betrayal.RevealedBy);
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
    }
}
