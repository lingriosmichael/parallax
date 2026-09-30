using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0: turns a scenario's frame rows into checks.json (and one summary line). Every check
    /// is computed from the analytic measurements in FrameRow.M, never from the rendered images.</summary>
    static class CatCaptureReport
    {
        // State vs motor: one row per rule. A rule is broken on a frame when its test is true. Later items append rows
        // (e.g. no Run below the run threshold, TakeOff within a frame of the jump).
        public static readonly List<StateRule> Rules = new()
        {
            new StateRule("air-state-while-grounded", "Rise or Fall shown while the motor has been grounded for more than one frame",
                c => !c.Excluded && c.Air(c.Row.State) && c.Row.Grounded && c.GroundedFrames > 1),
            new StateRule("ground-state-while-airborne", "Idle or Walk shown while the motor has been airborne (not climbing) for more than one frame",
                c => !c.Excluded && (c.Row.State == CatAnimState.Idle || c.Row.State == CatAnimState.Walk) && !c.Row.Grounded && !c.Row.Climbing && c.AirborneFrames > 1),
            new StateRule("climb-without-climbing", "Climb shown while the motor isn't climbing",
                c => c.Row.State == CatAnimState.Climb && !c.Row.Climbing),
            new StateRule("land-late", "Land not shown by the frame after touchdown (after at least 3 airborne frames)",
                c => !c.Excluded && c.Row.Grounded && c.GroundedFrames == 2 && c.LastAirRun >= 3
                     && c.Row.State != CatAnimState.Land && c.Prev != null && c.Prev.State != CatAnimState.Land),
        };

        public sealed class Result { public string Json; public string Line; public bool Pass; }

        public static Result Evaluate(string scenario, List<FrameRow> rows, bool died, bool completed, bool expectDeath, string parity)
        {
            var j = new Json();
            j.Open().Str("scenario", scenario).Num("frames", rows.Count).Num("ticks", rows.Count > 0 ? rows[^1].Tick : 0)
                .Bool("died", died).Bool("expectDeath", expectDeath).Bool("completed", completed);
            bool pass = true;
            var line = new StringBuilder(scenario);

            // --- paws in floor ---
            var groundedTicks = new HashSet<int>(rows.Where(r => r.Grounded).Select(r => r.Tick));
            bool NearGround(FrameRow r)
            {
                for (int d = -CaptureThresholds.NearGroundTicks; d <= CaptureThresholds.NearGroundTicks; d++) if (groundedTicks.Contains(r.Tick + d)) return true;
                return false;
            }
            bool Checkable(FrameRow r) => r.M.HasSprite && r.M.SurfaceFound && !r.Holding && !r.Frozen && !r.Climbing;
            var pawFrames = rows.Where(r => Checkable(r) && (r.Grounded || NearGround(r))).ToList();
            FrameRow worstPaw = pawFrames.OrderByDescending(r => r.M.PenetrationSpritePx).FirstOrDefault();
            float worstPawPx = worstPaw != null ? worstPaw.M.PenetrationSpritePx : 0f;
            var pawFails = pawFrames.Where(r => r.M.PenetrationSpritePx > CaptureThresholds.PawsInFloorSpritePx).ToList();
            bool pawPass = pawFails.Count == 0;
            pass &= pawPass;
            j.Key("paws_in_floor").Open().Num("threshold_sprite_px", CaptureThresholds.PawsInFloorSpritePx).Num("frames_checked", pawFrames.Count)
                .Num("max_depth_sprite_px", worstPawPx).Num("worst_frame", worstPaw?.Frame ?? -1)
                .Num("max_depth_grounded_sprite_px", Max(pawFrames.Where(r => r.Grounded), r => r.M.PenetrationSpritePx))
                .Num("max_depth_air_near_ground_sprite_px", Max(pawFrames.Where(r => !r.Grounded), r => r.M.PenetrationSpritePx))
                .Num("frames_over", pawFails.Count).Ints("first_frames_over", pawFails.Take(20).Select(r => r.Frame)).Bool("pass", pawPass).Close();
            line.Append($" | paws max {worstPawPx:F1}sp ({pawFails.Count} over)");

            // --- float gap on grounded frames ---
            var ground = rows.Where(r => Checkable(r) && r.Grounded).ToList();
            float Gap(FrameRow r) => Mathf.Max(0f, -r.M.PenetrationSpritePx);
            float gapMax = ground.Count > 0 ? ground.Max(Gap) : 0f, gapMean = ground.Count > 0 ? ground.Average(Gap) : 0f;
            int gapOver = ground.Count(r => Gap(r) > CaptureThresholds.FloatGapSpritePx);
            j.Key("float_gap").Open().Num("threshold_sprite_px", CaptureThresholds.FloatGapSpritePx).Num("frames_checked", ground.Count)
                .Num("max_sprite_px", gapMax).Num("mean_sprite_px", gapMean).Num("frames_over", gapOver)
                .Ints("first_frames_over", ground.Where(r => Gap(r) > CaptureThresholds.FloatGapSpritePx).Take(20).Select(r => r.Frame))
                .Str("note", "reported, not pass/fail").Close();
            line.Append($" | gap max {gapMax:F1}sp mean {gapMean:F1}");

            // --- foot slide ---
            List<(int plant, int lift, float drift)> plants = FootSlide(rows);
            float slideMax = plants.Count > 0 ? plants.Max(p => p.drift) : 0f, slideMean = plants.Count > 0 ? plants.Average(p => p.drift) : 0f;
            int slideOver = plants.Count(p => p.drift > CaptureThresholds.FootSlidePhonePx);
            bool slidePass = slideOver == 0;
            pass &= slidePass;
            j.Key("foot_slide").Open().Num("threshold_phone_px", CaptureThresholds.FootSlidePhonePx).Num("planted_paws", plants.Count)
                .Num("max_phone_px", slideMax).Num("mean_phone_px", slideMean).Num("paws_over", slideOver)
                .Raw("worst", "[" + string.Join(",", plants.OrderByDescending(p => p.drift).Take(10).Select(p =>
                    $"{{\"plant_frame\":{p.plant},\"lift_frame\":{p.lift},\"drift_phone_px\":{F(p.drift)}}}")) + "]")
                .Bool("pass", slidePass).Close();
            line.Append($" | slide max {slideMax:F1}pp mean {slideMean:F1} ({slideOver} over)");

            // --- pose pop ---
            var changes = rows.Where(r => r.PopPhonePx >= 0f).ToList();
            var pops = changes.Where(r => r.PopPhonePx > CaptureThresholds.PosePopPhonePx).ToList();
            var boxPops = changes.Where(r => r.BoxPopPhonePx > CaptureThresholds.PoseBoxPhonePx).ToList();
            bool popPass = pops.Count == 0;
            pass &= popPass;
            string PopList(IEnumerable<FrameRow> list) => "[" + string.Join(",", list.Take(20).Select(r =>
                $"{{\"frame\":{r.Frame},\"sprite\":\"{r.Sprite}\",\"from\":\"{Prev(rows, r)?.Sprite}\",\"centroid_phone_px\":{F(r.PopPhonePx)},\"box_phone_px\":{F(r.BoxPopPhonePx)}}}")) + "]";
            j.Key("pose_pop").Open().Num("threshold_phone_px", CaptureThresholds.PosePopPhonePx).Num("box_threshold_phone_px", CaptureThresholds.PoseBoxPhonePx)
                .Num("sprite_changes", changes.Count).Num("centroid_max_phone_px", changes.Count > 0 ? changes.Max(r => r.PopPhonePx) : 0f)
                .Num("box_max_phone_px", changes.Count > 0 ? changes.Max(r => r.BoxPopPhonePx) : 0f)
                .Num("centroid_pops", pops.Count).Num("box_pops", boxPops.Count)
                .Raw("worst", PopList(changes.OrderByDescending(r => r.PopPhonePx))).Bool("pass", popPass).Close();
            line.Append($" | pop max {(changes.Count > 0 ? changes.Max(r => r.PopPhonePx) : 0f):F1}pp ({pops.Count} over)");

            // --- state vs motor ---
            var broken = StateRules(rows);
            bool rulesPass = broken.All(b => b.Value.Count == 0);
            pass &= rulesPass;
            j.Key("state_vs_motor").Open().Raw("rules", "[" + string.Join(",", Rules.Select(rule =>
                $"{{\"name\":\"{rule.Name}\",\"description\":\"{rule.Description}\",\"frames\":{broken[rule.Name].Count},\"first_frames\":[{string.Join(",", broken[rule.Name].Take(20))}]}}")) + "]")
                .Bool("pass", rulesPass).Close();
            line.Append($" | rules {broken.Sum(b => b.Value.Count)} frames broken");

            // --- hold timing (item 6) ---
            j.Key("hold_timing").Open().Str("status", "placeholder: item 6 (a death clip's held frame is shown by the end of the hold)").Close();

            // --- the harness itself: stepping frames between ticks must not change physics ---
            bool parityPass = parity == null;
            pass &= parityPass;
            j.Key("capture_parity").Open().Str("note", "ticks with 60 fps frames between them vs ticks alone: body position and velocity identical").Str("first_difference", parity ?? "none").Bool("pass", parityPass).Close();
            if (!parityPass) line.Append(" | PARITY BROKEN");
            if (died != expectDeath) { pass = false; line.Append(died ? " | UNEXPECTED DEATH" : " | EXPECTED DEATH MISSING"); }

            j.Bool("pass", pass).Close();
            line.Append(pass ? " | PASS" : " | FAIL");
            return new Result { Json = j.ToString(), Line = line.ToString(), Pass = pass };
        }

        static FrameRow Prev(List<FrameRow> rows, FrameRow r) => r.Frame > 0 && r.Frame - 1 < rows.Count ? rows[r.Frame - 1] : null;

        static float Max(IEnumerable<FrameRow> rows, Func<FrameRow, float> f) { float m = 0f; bool any = false; foreach (FrameRow r in rows) { m = any ? Mathf.Max(m, f(r)) : f(r); any = true; } return m; }

        static Dictionary<string, List<int>> StateRules(List<FrameRow> rows)
        {
            var broken = Rules.ToDictionary(r => r.Name, _ => new List<int>());
            var c = new StateContext();
            for (int i = 0; i < rows.Count; i++)
            {
                FrameRow r = rows[i];
                c.Prev = i > 0 ? rows[i - 1] : null;
                c.Row = r;
                // A death hold (frozen) or a respawn (a teleport) starts the counts again: the motor's flags are stale there.
                bool teleport = c.Prev != null && (r.Root - c.Prev.Root).magnitude >= 1f;
                if (c.Excluded || teleport) { c.GroundedFrames = c.AirborneFrames = c.LastAirRun = 0; if (c.Excluded) continue; }
                if (r.Grounded)
                {
                    if (c.GroundedFrames == 0) c.LastAirRun = c.AirborneFrames;
                    c.GroundedFrames++; c.AirborneFrames = 0;
                }
                else { c.AirborneFrames++; c.GroundedFrames = 0; }
                foreach (StateRule rule in Rules) if (rule.Broken(c)) broken[rule.Name].Add(r.Frame);
            }
            return broken;
        }

        // Planted paws: at each sprite change during ground locomotion, the contact pixels' clusters are matched to the
        // paws planted at the previous sample; a paw's drift is how far it moved (world, along the surface) from its first
        // sample to its last. Any frame off the ground (or not moving) ends every plant.
        static List<(int plant, int lift, float drift)> FootSlide(List<FrameRow> rows)
        {
            var done = new List<(int, int, float)>();
            var active = new List<(float plantAlong, float lastAlong, int plantFrame, int lastFrame, int samples, float drift)>();
            void EndAll()
            {
                foreach (var p in active) if (p.samples >= 2) done.Add((p.plantFrame, p.lastFrame, p.drift * CaptureThresholds.PhonePixelsPerUnit));
                active.Clear();
            }
            for (int i = 0; i < rows.Count; i++)
            {
                FrameRow r = rows[i];
                bool loco = r.Grounded && !r.Holding && !r.Frozen && !r.Climbing && r.M.SurfaceFound
                    && (r.State == CatAnimState.Walk || Mathf.Abs(r.VAlong) > 0.1f);
                if (!loco) { EndAll(); continue; }
                bool change = i == 0 || rows[i - 1].Sprite != r.Sprite || active.Count == 0;
                if (!change) continue;
                Vector2[] clusters = CatCaptureMath.Clusters(r.M.ContactAlong, CaptureThresholds.ClusterGapSpritePx / r.M.Ppu);
                var next = new List<(float, float, int, int, int, float)>();
                var used = new bool[clusters.Length];
                foreach (var p in active)
                {
                    int best = -1; float bestD = CaptureThresholds.PawMatchUnits;
                    for (int k = 0; k < clusters.Length; k++)
                    {
                        float d = Mathf.Abs(clusters[k].x - p.lastAlong);
                        if (!used[k] && d <= bestD) { best = k; bestD = d; }
                    }
                    if (best < 0) { if (p.samples >= 2) done.Add((p.plantFrame, p.lastFrame, p.drift * CaptureThresholds.PhonePixelsPerUnit)); continue; }
                    used[best] = true;
                    float along = clusters[best].x;
                    next.Add((p.plantAlong, along, p.plantFrame, r.Frame, p.samples + 1, Mathf.Max(p.drift, Mathf.Abs(along - p.plantAlong))));
                }
                for (int k = 0; k < clusters.Length; k++) if (!used[k]) next.Add((clusters[k].x, clusters[k].x, r.Frame, r.Frame, 1, 0f));
                active = next;
            }
            EndAll();
            return done;
        }

        public static string F(float v) => float.IsInfinity(v) || float.IsNaN(v) ? "null" : v.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>A tiny JSON writer (objects only; arrays come in as raw strings).</summary>
        public sealed class Json
        {
            readonly StringBuilder sb = new();
            bool first = true;
            public Json Open() { sb.Append('{'); first = true; return this; }
            public Json Close() { sb.Append('}'); first = false; return this; }
            public Json Key(string k) { if (!first) sb.Append(','); sb.Append('"').Append(k).Append("\":"); first = true; return this; }
            Json Put(string k, string v) { if (!first) sb.Append(','); sb.Append('"').Append(k).Append("\":").Append(v); first = false; return this; }
            public Json Str(string k, string v) => Put(k, "\"" + (v ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            public Json Num(string k, float v) => Put(k, F(v));
            public Json Num(string k, int v) => Put(k, v.ToString(CultureInfo.InvariantCulture));
            public Json Bool(string k, bool v) => Put(k, v ? "true" : "false");
            public Json Ints(string k, IEnumerable<int> v) => Put(k, "[" + string.Join(",", v) + "]");
            public Json Raw(string k, string raw) => Put(k, raw);
            public override string ToString() => sb.ToString();
        }
    }
}
