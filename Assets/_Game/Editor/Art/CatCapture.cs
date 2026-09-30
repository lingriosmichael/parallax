using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Parallax.Core;
using Parallax.Editor.Routes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0 (§11 R8, R9, R14): the cat capture harness. Plays each scenario through the real
    /// game code (CatCaptureRig), and between the 50 Hz ticks draws 60 fps frames the way a phone shows them: the cat root
    /// at the pose Rigidbody2D interpolation would give, CatVisualPresenter.Present(1/60), then a render. The body's exact
    /// pose is restored before the next tick, and a second, frame-less run proves physics never saw the difference.
    /// Per scenario it writes frames/fNNNNN.png (160 px/u, a 4 × 2.5 u window that follows the cat), frames.jsonl,
    /// checks.json and transitions.json; then summary.json / summary.txt; then (optionally) the contact sheets through
    /// Tools/Art/cat_capture_sheet.py. Nothing is saved to a scene or asset; the open scenes are restored.
    /// Batch: -executeMethod Parallax.Editor.Art.CatCapture.Run -captureOut &lt;dir&gt; [-captureFilter &lt;regex&gt;]
    /// [-captureNoImages] [-captureSheets]. See HARNESS.md.</summary>
    public static partial class CatCapture
    {
        public const float FrameRate = 60f;             // the phone's display rate the frames emulate
        public const float RenderPixelsPerUnit = 160f;  // twice phone scale (80 px/u), so details show
        public const float ViewWidthUnits = 4.8f, ViewHeightUnits = 3.6f;   // tall enough for a full jump and the whole cat
        const int DeathSheetAfter = 44;                 // a death sheet runs this many frames on: the whole 0.6 s hold and the respawn
        const int WalkOffJumpLookback = 3;              // frames: leaving the ground this soon after a jump is a takeoff, not a walk-off
        const float CameraFrameMarginUnits = 0.12f;     // the cat's drawn pixels are kept at least this far inside the window
        const int SheetBefore = 8, SheetAfter = 24, SheetMaxPerKind = 8;   // a transition sheet's frames, and how many of each kind
        const float InterpolationTeleportUnits = 1f;  // a tick move this long (a respawn) shows the new pose at once
        const float CameraSmoothTime = 0.1f;            // seconds; the follow is a critically damped spring
        const float CameraSnapUnits = 2f;               // a jump this far (a respawn) snaps the view
        const float CameraKeepInX = 0.9f, CameraKeepInY = 0.6f;    // the cat's centre never gets closer than this to the window's edges
        const float CameraLookRoomUnits = 1.0f;        // the view sits this far above (against gravity) the standing cat: the floor line 0.5 u from the edge
        static readonly Color Backdrop = new(0.93f, 0.82f, 0.62f, 1f);   // the trap sheet's warm haze

        [MenuItem("PARALLAX/Art/Cat Capture")]
        public static void Menu()
        {
            string outDir = Path.GetFullPath("Logs/CatCapture");
            if (Capture(outDir, ".*", images: true)) RunSheets(outDir);
            Debug.LogWarning($"CAPTURE: output in {outDir}");
        }

        /// <summary>Batch entry point. Arguments come from the command line (see the class summary).</summary>
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            string outDir = Arg("-captureOut");
            string filter = Arg("-captureFilter") ?? ".*";
            try
            {
                if (string.IsNullOrEmpty(outDir)) throw new ArgumentException("CatCapture.Run needs -captureOut <dir>.");
                bool ok = Capture(Path.GetFullPath(outDir), filter, images: Array.IndexOf(args, "-captureNoImages") < 0);
                if (ok && Array.IndexOf(args, "-captureSheets") >= 0) RunSheets(outDir);
                Debug.Log($"CAPTURE: done, output in {outDir}");
                if (!ok) EditorApplication.Exit(2);
            }
            catch (Exception e)
            {
                Debug.LogError($"CAPTURE: failed: {e}");
                EditorApplication.Exit(1);
            }
        }

        public static bool Capture(string outDir, string filter, bool images)
        {
            var regex = new Regex(filter);
            List<CaptureScenario> scenarios = CatCaptureScenarios.All.Where(s => regex.IsMatch(s.Name)).ToList();
            if (scenarios.Count == 0) { Debug.LogError($"CAPTURE: no scenario matches '{filter}'."); return false; }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("CAPTURE: save or discard the open scene first (the capture restores it from disk)."); return false; }
            Directory.CreateDirectory(outDir);
            var lines = new List<string>(); var jsons = new List<string>();
            using (var session = new RouteSession())
            using (var alpha = new SpriteAlphaCache())
            {
                foreach (CaptureScenario scenario in scenarios)
                {
                    CatCaptureReport.Result result = CaptureScenario(session, alpha, scenario, Path.Combine(outDir, scenario.Name), images);
                    lines.Add(result.Line); jsons.Add(result.Json);
                    Debug.LogWarning("CAPTURE: " + result.Line);
                }
            }
            File.WriteAllText(Path.Combine(outDir, "summary.json"), "[" + string.Join(",\n", jsons) + "]\n");
            File.WriteAllText(Path.Combine(outDir, "summary.txt"), string.Join("\n", lines) + "\n");
            return true;
        }

        sealed class Runner
        {
            readonly List<CaptureStep> steps; int index, inStep;
            public Runner(List<CaptureStep> steps) => this.steps = steps;
            public string Label => index < steps.Count ? steps[index].Label : "end";
            public bool Setup => index < steps.Count && steps[index].Setup;
            public bool Next(CatCaptureRig rig, out CatCommand command)
            {
                while (index < steps.Count)
                {
                    if (steps[index].Next(rig, inStep, out command)) { inStep++; return true; }
                    index++; inStep = 0;
                }
                command = default;
                return false;
            }
        }

        static string TickState(CatCaptureRig rig) =>
            $"{rig.Tick}:{rig.Body.position.x:R},{rig.Body.position.y:R},{rig.Body.linearVelocity.x:R},{rig.Body.linearVelocity.y:R}";

        // The scenario's ticks alone (no frames): what the game's physics does with these inputs.
        static List<string> Headless(RouteSession session, CaptureScenario scenario, List<CaptureStep> steps)
        {
            session.Clear();
            var log = new List<string>();
            CatCaptureRig rig = CatCaptureRig.Build(scenario.Room(session), scenario.StartX);
            var runner = new Runner(steps);
            while (runner.Next(rig, out CatCommand c)) { rig.Step(c); log.Add(TickState(rig)); }
            session.Clear();
            return log;
        }

        static CatCaptureReport.Result CaptureScenario(RouteSession session, SpriteAlphaCache alpha, CaptureScenario scenario, string dir, bool images)
        {
            List<CaptureStep> steps = scenario.Steps(session);   // before any rig: a route replay clears the session's scene
            List<string> reference = Headless(session, scenario, steps);
            string framesDir = Path.Combine(dir, "frames");
            if (Directory.Exists(framesDir)) Directory.Delete(framesDir, true);
            Directory.CreateDirectory(framesDir);

            session.Clear();
            CatCaptureRig rig = CatCaptureRig.Build(scenario.Room(session), scenario.StartX);
            CaptureSpeeds speeds = Speeds(rig);
            var shots = images ? new TrapShots.Rig() : null;
            var grid = new CatCaptureGrid(rig, RenderPixelsPerUnit);
            string[] aboveCat = DrawnAboveCat(rig);   // at the start; replaced by the first death-hold frame's list
            var rows = new List<FrameRow>();
            var ticks = new List<string>();
            var jsonl = new StringBuilder();
            bool died = false, completed = false;
            try
            {
                shots?.Ensure(Backdrop, 1f);
                var runner = new Runner(steps);
                Transform root = rig.Cat.transform;
                float tickLength = TickTime.SecondsPerTick, frameDt = 1f / FrameRate;
                Vector2 prevPos = rig.Body.position, curPos = prevPos; float prevRot = rig.Body.rotation, curRot = prevRot;
                Vector3 tickPosition = root.position; Quaternion tickRotation = root.rotation;
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = rig.Root.PhysicsMask, useTriggers = false };
                Vector2 cam = rig.ColliderCentre, camVelocity = Vector2.zero;
                float standY = cam.y;
                float maxWriteBack = 0f;
                bool finished = false;
                for (int n = 0; !finished; n++)
                {
                    // Unity's player loop runs the first FixedUpdate in the first frame: tick k's physics time is (k - 1) ticks,
                    // so the frame at t sees every tick due by t + one tick, and blends from the last one's start.
                    double t = n / (double)FrameRate, loopTime = t + tickLength;
                    int due = CatCaptureMath.TicksDueBy(loopTime, tickLength);
                    while (rig.Tick < due)
                    {
                        // The body's exact pose back before physics runs again (§11 R14).
                        root.SetPositionAndRotation(tickPosition, tickRotation);
                        Physics2D.SyncTransforms();
                        if (!runner.Next(rig, out CatCommand command)) { finished = true; break; }
                        rig.Step(command);
                        ticks.Add(TickState(rig));
                        died |= rig.Death.IsHolding;
                        completed |= rig.Rooms.LevelComplete;
                        prevPos = curPos; prevRot = curRot;
                        curPos = rig.Body.position; curRot = rig.Body.rotation;
                        tickPosition = root.position; tickRotation = root.rotation;
                        maxWriteBack = Mathf.Max(maxWriteBack, ((Vector2)tickPosition - curPos).magnitude);
                    }
                    if (finished) break;

                    // The frame: the interpolated pose, the presenter, the room's art, then the measurements and the render.
                    float a = CatCaptureMath.InterpolationAlpha(loopTime, rig.Tick, tickLength);
                    CatCaptureMath.InterpolatedPose(prevPos, prevRot, curPos, curRot, a, InterpolationTeleportUnits, out Vector2 pos, out float rot);
                    root.SetPositionAndRotation(new Vector3(pos.x, pos.y, tickPosition.z), Quaternion.Euler(0f, 0f, rot));
                    rig.Presenter.Present(frameDt);
                    TrapShots.ApplyArt();

                    Vector2 down = rig.Gravity.Direction;
                    Vector2 centre = root.TransformPoint(rig.CatCollider.offset);
                    FrameRow row = Row(rig, runner, n, t, down, centre);
                    row.M = CatCaptureMeasure.Measure(alpha, rig.BodyRenderer, down, centre, filter, rig.CatCollider);
                    if (rows.Count > 0) Pop(rows[^1], row);
                    // A platformer camera: horizontally the cat, vertically where it last stood (so a jump doesn't move the
                    // floor), kept inside the window.
                    if (row.Grounded || n == 0) standY = centre.y - down.y * CameraLookRoomUnits;
                    cam = Follow(cam, ref camVelocity, new Vector2(centre.x, standY), centre, frameDt, n == 0);
                    if (row.M.HasSprite) cam = KeepInView(cam, row.M.BoxMin, row.M.BoxMax);
                    row.Cam = cam;
                    Vector2 paw = rig.BodyRenderer.transform.position;
                    row.Paw = paw;
                    row.PawImg = new Vector2((paw.x - cam.x) * RenderPixelsPerUnit + ViewWidthUnits * RenderPixelsPerUnit * 0.5f,
                        ViewHeightUnits * RenderPixelsPerUnit * 0.5f - (paw.y - cam.y) * RenderPixelsPerUnit);
                    if (shots != null)
                    {
                        row.Image = $"frames/f{n:D5}.png";
                        Texture2D tex = shots.Shot(cam, ViewHeightUnits, Mathf.RoundToInt(ViewWidthUnits * RenderPixelsPerUnit), Mathf.RoundToInt(ViewHeightUnits * RenderPixelsPerUnit));
                        grid.DrawWorldReference(tex, alpha, rig.BodyRenderer, cam);
                        if (row.Holding) grid.DrawSilhouette(tex, alpha, rig.BodyRenderer, cam);
                        if (row.Holding && (rows.Count == 0 || !rows[^1].Holding)) aboveCat = DrawnAboveCat(rig);
                        TrapShots.SavePng(tex, Path.Combine(dir, row.Image));
                        Object.DestroyImmediate(tex);
                    }
                    rows.Add(row);
                    jsonl.Append(Jsonl(row)).Append('\n');
                }
                root.SetPositionAndRotation(tickPosition, tickRotation);
                if (maxWriteBack > 1e-5f) Debug.LogWarning($"CAPTURE: {scenario.Name}: the transform after Simulate differs from the body by {maxWriteBack} u.");
            }
            finally
            {
                shots?.Dispose();
                session.Clear();
            }

            string parity = null;
            for (int i = 0; i < Mathf.Max(ticks.Count, reference.Count) && parity == null; i++)
            {
                string got = i < ticks.Count ? ticks[i] : "(none)", want = i < reference.Count ? reference[i] : "(none)";
                if (got != want) parity = $"tick {i + 1}: captured {got} vs ticks alone {want}";
            }
            File.WriteAllText(Path.Combine(dir, "frames.jsonl"), jsonl.ToString());
            CaptureTransition[] transitions = CatCaptureMath.SelectTransitions(rows.Select(r => r.State.ToString()).ToArray(), rows.Select(r => r.Grounded).ToArray(), SheetBefore, SheetAfter, SheetMaxPerKind)
                .Concat(CatCaptureMath.SelectStepCuts(rows.Select(r => r.Step).ToArray(), steps.Where(st => st.Cut).Select(st => st.Label).ToArray(), SheetBefore, SheetAfter))
                .Concat(CatCaptureMath.SelectEventCuts(rows.Select(r => r.Facing).ToArray(), rows.Select(r => r.Grounded).ToArray(), rows.Select(r => r.Jumped).ToArray(),
                    rows.Select(r => r.Holding).ToArray(), SheetBefore, SheetAfter, DeathSheetAfter, WalkOffJumpLookback, SheetMaxPerKind))
                // A motor flag flipping on the respawn frames (stale for one tick after RespawnAt) is not a takeoff or a landing.
                .Where(tr => !((tr.Kind == "takeoff" || tr.Kind == "landing") && rows.Skip(Mathf.Max(0, tr.Frame - 3)).Take(4).Any(r => r.Holding)))
                .OrderBy(tr => tr.Frame).ToArray();
            // PAX-V07 item 1: every ground change (Idle / Walk / Run / Turn, a turn, a scripted start or stop) also gets a
            // paw strip.
            foreach (CaptureTransition tr in transitions)
                if (tr.Kind == "turn" || tr.Kind.StartsWith("step: ") || (GroundState(tr.From) && GroundState(tr.To))) tr.Strip = true;
            File.WriteAllText(Path.Combine(dir, "transitions.json"), "[" + string.Join(",\n", transitions.Select(tr =>
                $"{{\"kind\":\"{tr.Kind}\",\"from\":\"{tr.From}\",\"to\":\"{tr.To}\",\"frame\":{tr.Frame},\"start\":{tr.Start},\"end\":{tr.End},\"strip\":{(tr.Strip ? "true" : "false")}}}")) + "]\n");
            File.WriteAllText(Path.Combine(dir, "scenario.json"),
                $"{{\"name\":\"{scenario.Name}\",\"description\":\"{scenario.Description}\",\"gravity\":\"{(scenario.Name.EndsWith("_up") ? "up" : "down")}\",\"frame_rate\":{FrameRate},\"render_ppu\":{RenderPixelsPerUnit},\"phone_ppu\":{CaptureThresholds.PhonePixelsPerUnit},\"view_units\":[{ViewWidthUnits},{ViewHeightUnits}],\"images\":{(images ? "true" : "false")},\"drawn_above_cat\":[{string.Join(",", aboveCat.Select(a => "\"" + a + "\""))}],\"grid\":{{\"step\":{CatCaptureGrid.GridStep},\"tick\":{CatCaptureGrid.TickStep}}}}}\n");
            CatCaptureReport.Result result = CatCaptureReport.Evaluate(scenario.Name, rows, died, completed, scenario.ExpectDeath, parity, speeds);
            File.WriteAllText(Path.Combine(dir, "checks.json"), result.Json + "\n");
            return result;
        }

        static FrameRow Row(CatCaptureRig rig, Runner runner, int n, double t, Vector2 down, Vector2 centre)
        {
            Vector2 v = rig.Body.linearVelocity;
            return new FrameRow
            {
                Frame = n, Time = t, Tick = rig.Tick, Step = runner.Label, Setup = runner.Setup,
                State = rig.Presenter.State, Clip = rig.Presenter.ClipName, ClipFrame = rig.Presenter.FrameIndex, Facing = rig.Presenter.Facing,
                Sprite = rig.BodyRenderer.sprite != null ? rig.BodyRenderer.sprite.name : "",
                Grounded = rig.Cat.IsGrounded, Climbing = rig.Cat.IsClimbing, Jumped = rig.Cat.JumpedThisStep, Frozen = rig.Cat.IsFrozen,
                Holding = rig.Death.IsHolding, GravityDown = down.y < 0f,
                VAlong = GravityFrame.Along(v, down), VGravity = Vector2.Dot(v, down),
                Root = rig.Cat.transform.position, ColliderCentre = centre,
            };
        }

        // Pose pop at a sprite change: the silhouette's centroid (and box edges) move beyond what the root moved.
        static void Pop(FrameRow prev, FrameRow row)
        {
            if (!row.M.HasSprite || !prev.M.HasSprite || row.Sprite == prev.Sprite || row.Holding || prev.Holding) return;
            Vector2 rootMove = row.Root - prev.Root;
            if (rootMove.magnitude >= 1f) return;   // a respawn
            float s = CaptureThresholds.PhonePixelsPerUnit;
            row.PopPhonePx = ((row.M.Centroid - prev.M.Centroid) - rootMove).magnitude * s;
            Vector2 dMin = row.M.BoxMin - prev.M.BoxMin - rootMove, dMax = row.M.BoxMax - prev.M.BoxMax - rootMove;
            row.BoxPopPhonePx = Mathf.Max(Mathf.Max(Mathf.Abs(dMin.x), Mathf.Abs(dMin.y)), Mathf.Max(Mathf.Abs(dMax.x), Mathf.Abs(dMax.y))) * s;
        }

        // The cat's drawn pixels (world box) kept inside the window with a margin: nothing of the cat is ever cropped.
        static Vector2 KeepInView(Vector2 cam, Vector2 boxMin, Vector2 boxMax)
        {
            float hx = ViewWidthUnits * 0.5f - CameraFrameMarginUnits, hy = ViewHeightUnits * 0.5f - CameraFrameMarginUnits;
            cam.x = Mathf.Clamp(cam.x, boxMax.x - hx, boxMin.x + hx);
            cam.y = Mathf.Clamp(cam.y, boxMax.y - hy, boxMin.y + hy);
            return cam;
        }

        // Every visible room sprite the game sorts above the cat's body (sorting layer, then order): what can hide the cat.
        static string[] DrawnAboveCat(CatCaptureRig rig)
        {
            SpriteRenderer body = rig.BodyRenderer;
            int layer = SortingLayer.GetLayerValueFromID(body.sortingLayerID);
            return Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(r => r.enabled && r.sprite != null && r.color.a > 0f && !r.transform.IsChildOf(rig.Cat.transform) && !r.name.StartsWith("__"))
                .Where(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID) > layer
                            || (SortingLayer.GetLayerValueFromID(r.sortingLayerID) == layer && r.sortingOrder > body.sortingOrder))
                .Select(r => r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name).Distinct().OrderBy(n => n).Take(40).ToArray();
        }

        // The view: a critically damped follow of `target`, with the cat's centre kept inside the window, snapping on a
        // respawn.
        static Vector2 Follow(Vector2 cam, ref Vector2 velocity, Vector2 target, Vector2 cat, float dt, bool first)
        {
            if (first || (cat - cam).magnitude > CameraSnapUnits + ViewWidthUnits * 0.5f) { velocity = Vector2.zero; return target; }
            cam = Vector2.SmoothDamp(cam, target, ref velocity, CameraSmoothTime, float.PositiveInfinity, dt);
            float hx = ViewWidthUnits * 0.5f - CameraKeepInX, hy = ViewHeightUnits * 0.5f - CameraKeepInY;
            cam.x = Mathf.Clamp(cam.x, cat.x - hx, cat.x + hx);
            cam.y = Mathf.Clamp(cam.y, cat.y - hy, cat.y + hy);
            return cam;
        }

        static string Jsonl(FrameRow r)
        {
            string F(float v) => CatCaptureReport.F(v);
            var sb = new StringBuilder();
            sb.Append("{\"frame\":").Append(r.Frame).Append(",\"time\":").Append(r.Time.ToString("0.#####", CultureInfo.InvariantCulture))
              .Append(",\"tick\":").Append(r.Tick).Append(",\"step\":\"").Append(r.Step).Append('"')
              .Append(",\"state\":\"").Append(r.State).Append("\",\"clip\":\"").Append(r.Clip).Append("\",\"clip_frame\":").Append(r.ClipFrame)
              .Append(",\"sprite\":\"").Append(r.Sprite).Append("\",\"facing\":").Append(r.Facing)
              .Append(",\"grounded\":").Append(B(r.Grounded)).Append(",\"climbing\":").Append(B(r.Climbing)).Append(",\"jumped\":").Append(B(r.Jumped))
              .Append(",\"frozen\":").Append(B(r.Frozen)).Append(",\"holding\":").Append(B(r.Holding)).Append(",\"gravity\":\"").Append(r.GravityDown ? "down" : "up").Append('"')
              .Append(",\"v_along\":").Append(F(r.VAlong)).Append(",\"v_gravity\":").Append(F(r.VGravity))
              .Append(",\"root\":[").Append(F(r.Root.x)).Append(',').Append(F(r.Root.y)).Append(']')
              .Append(",\"cam\":[").Append(F(r.Cam.x)).Append(',').Append(F(r.Cam.y)).Append(']')
              .Append(",\"paw\":[").Append(F(r.Paw.x)).Append(',').Append(F(r.Paw.y)).Append(']')
              .Append(",\"paw_img\":[").Append(F(r.PawImg.x)).Append(',').Append(F(r.PawImg.y)).Append(']')
              .Append(",\"depth_sprite_px\":").Append(r.M.SurfaceFound ? F(r.M.PenetrationSpritePx) : "null")
              .Append(",\"cat_box\":").Append(r.M.HasSprite ? $"[{F(r.M.BoxMin.x)},{F(r.M.BoxMin.y)},{F(r.M.BoxMax.x)},{F(r.M.BoxMax.y)}]" : "null")
              .Append(",\"surface\":\"").Append(r.M.Surface).Append('"')
              .Append(",\"contact_px\":").Append(r.M.ContactAlong.Length)
              .Append(",\"contacts\":[").Append(string.Join(",", CatCaptureMath.ClusterRanges(r.M.ContactAlong, CaptureThresholds.ClusterGapSpritePx / Mathf.Max(1f, r.M.Ppu))
                  .Select(c => $"[{F(c.x)},{F(c.y)}]"))).Append(']')
              .Append(",\"pop_phone_px\":").Append(r.PopPhonePx >= 0f ? F(r.PopPhonePx) : "null")
              .Append(",\"box_pop_phone_px\":").Append(r.BoxPopPhonePx >= 0f ? F(r.BoxPopPhonePx) : "null")
              .Append(",\"image\":").Append(r.Image != null ? "\"" + r.Image + "\"" : "null").Append('}');
            return sb.ToString();
        }

        static string B(bool b) => b ? "true" : "false";

        /// <summary>Tools/Art/cat_capture_sheet.py over the output (python3 on PATH, or PARALLAX_PYTHON).</summary>
        static void RunSheets(string outDir)
        {
            string python = Environment.GetEnvironmentVariable("PARALLAX_PYTHON") ?? "python3";
            string script = Path.GetFullPath("Tools/Art/cat_capture_sheet.py");
            try
            {
                var info = new ProcessStartInfo(python, $"\"{script}\" \"{outDir}\"") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                using Process p = Process.Start(info);
                string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) Debug.LogError($"CAPTURE: {script} exited {p.ExitCode}:\n{output}");
                else Debug.Log($"CAPTURE: sheets written.\n{output}");
            }
            catch (Exception e)
            {
                Debug.LogError($"CAPTURE: couldn't run '{python} {script}' ({e.Message}). Run it by hand: python3 Tools/Art/cat_capture_sheet.py \"{outDir}\"");
            }
        }
    }
}
