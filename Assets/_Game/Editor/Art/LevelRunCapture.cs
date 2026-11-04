using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Routes;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>Screenshots of a level mid-run, as the player sees it: every declared route (the solution and each betrayal) is
    /// replayed by the route harness to get its commands, then played through the real Level_NNN scene in Play Mode, with the
    /// backgrounds, lights and trap art, feeding the real input router one command a tick. A frame is rendered every
    /// PARALLAX_RUN_STEP ticks on the solution, and around the end of a betrayal. A replay that drifts from the harness only
    /// changes what's in the frame; this checks how levels look, never whether a route passes (that's ValidateRoutes).
    /// PARALLAX_RUN_SWEEP=1 plays no route: the camera walks the level's whole frame, for levels whose routes are stale.
    /// Gate 9 of the premium plan: PARALLAX_CAPTURE_ASPECT ("4:3", "16:9", default "20:9") sets the frame's shape at 1080 px tall;
    /// PARALLAX_CAPTURE_HOLES=1 clears the camera to pure magenta, so any spot no layer covers shows up in the frame; and
    /// PARALLAX_RUN_ROUTES=all plays the betrayals too when PARALLAX_RUN_DUMP=1 (a dump run is the solution only otherwise).
    /// Batch mode on a clone: Unity -batchmode -projectPath clone -executeMethod Parallax.Editor.Art.LevelRunCapture.RunBatch
    /// with PARALLAX_CAPTURE_OUT (a folder) and PARALLAX_CAPTURE_LEVELS (e.g. "1-10"). Domain reload is off on entering Play
    /// Mode, so the job list lives in statics.</summary>
    public static class LevelRunCapture
    {
        const int H = 1080, BetrayalLead = 70, BetrayalStep = 20, AfterEnd = 45, StallFrames = 900;

        sealed class Job { public string Level, Route, Folder; public bool Solution; public List<TickRecord> Records; public int LastTick; }

        sealed class Playback : ICatCommandSource
        {
            public Job Job; public ObserverSet Observers; public GravityReceiver Gravity;
            public void ResetTransientState() { }
            public CatCommand Read()
            {
                int tick = Observers.Tick;
                if (tick < 1 || tick >= Job.Records.Count) return CatCommand.None;
                TickRecord r = Job.Records[tick];
                Vector2 up = -Gravity.Direction;
                return new CatCommand
                {
                    Move = VirtualStick.ToMove(new Vector2(r.Move, 0f), new Vector2(up.y, -up.x), StickProjection.ScreenRelative),
                    Climb = r.Climb, JumpPressed = r.JumpPressed, JumpHeld = r.JumpHeld,
                };
            }
        }

        static readonly Queue<Job> jobs = new();
        static string output;
        static int step, W = 2400;
        static Job job;
        static Camera cam;
        static ObserverSet observers;
        static int nextShot, idle, lastTick;
        static bool opened;
        static readonly bool Sweep = System.Environment.GetEnvironmentVariable("PARALLAX_RUN_SWEEP") == "1";
        static List<Vector2> points;
        static int point;

        public static void RunBatch()
        {
            output = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_OUT");
            string range = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_LEVELS") ?? "1-10";
            step = int.TryParse(System.Environment.GetEnvironmentVariable("PARALLAX_RUN_STEP"), out int s) ? s : 30;
            string[] aspect = (System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_ASPECT") ?? "20:9").Split(':');
            W = Mathf.RoundToInt(H * float.Parse(aspect[0]) / float.Parse(aspect[1]));
            if (string.IsNullOrEmpty(output)) { Debug.LogError("LevelRunCapture: set PARALLAX_CAPTURE_OUT."); EditorApplication.Exit(1); return; }
            string[] parts = range.Split('-');
            int first = int.Parse(parts[0]), last = int.Parse(parts[parts.Length - 1]);
            using (var session = new RouteSession())
                for (int n = first; n <= last; n++) Plan("L" + n.ToString("000"), session);
            Debug.LogWarning($"LevelRunCapture: {jobs.Count} routes to play.");
            EditorApplication.update += Tick;
        }

        static void Plan(string id, RouteSession session)
        {
            if (!LevelLayouts.ById.TryGetValue(id, out SoloRoomDefinition room) || !LevelRoutes.TryGet(id, out RoomRoutes routes)) { Debug.LogError($"LevelRunCapture: no {id}."); return; }
            if (Sweep) { jobs.Enqueue(new Job { Level = id, Route = "sweep", Solution = true, Records = new List<TickRecord>(), Folder = Path.Combine(output, id, "00_sweep") }); return; }
            void Add(Route route, bool solution, int index)
            {
                ReplayResult result = RouteHarness.Replay(session, room, route);
                string name = new string(route.Name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                jobs.Enqueue(new Job { Level = id, Route = route.Name, Solution = solution, Records = result.Records, LastTick = result.Records.Count - 1, Folder = Path.Combine(output, id, $"{index:00}_{(name.Length > 40 ? name.Substring(0, 40) : name)}") });
            }
            Add(routes.Solution, true, 0);
            if (System.Environment.GetEnvironmentVariable("PARALLAX_RUN_DUMP") == "1" && System.Environment.GetEnvironmentVariable("PARALLAX_RUN_ROUTES") != "all") return;
            for (int i = 0; i < routes.Betrayals.Count; i++) Add(routes.Betrayals[i].Route, false, i + 1);
        }

        // Runs before the level's first tick, so the replay and the level's clocks start together.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (job == null) return;
            LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsSortMode.None).FirstOrDefault();
            cam = follow != null ? follow.GetComponent<Camera>() : null;
            observers = Object.FindFirstObjectByType<ObserverSet>();
            CatInputRouter router = Object.FindFirstObjectByType<CatInputRouter>();
            var cat = follow != null ? new SerializedObject(follow).FindProperty("target").objectReferenceValue as Transform : null;
            if (cam == null || observers == null || router == null || cat == null) { Debug.LogError($"LevelRunCapture: {job.Level} is missing its camera, observers, router or cat."); return; }
            var sources = (List<ICatCommandSource>)typeof(CatInputRouter).GetField("validSources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(router);
            sources.Add(new Playback { Job = job, Observers = observers, Gravity = cat.GetComponent<GravityReceiver>() });
            cam.targetTexture = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            if (System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_HOLES") == "1") { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.magenta; }
            Time.captureDeltaTime = TickTime.SecondsPerTick;
            AudioListener.volume = 0f;   // a capture plays every level: silently
        }

        static void Tick()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    if (!opened || cam == null || observers == null) { if (++idle > StallFrames) { Debug.LogError($"LevelRunCapture: {job?.Level} never attached."); Leave(); } return; }
                    int tick = observers.Tick;
                    if (Sweep) { SweepStep(tick); return; }
                    idle = tick == lastTick ? idle + 1 : 0;
                    lastTick = tick;
                    if (tick >= nextShot) { Shot(tick); nextShot = Next(tick); }
                    if (tick >= job.LastTick + AfterEnd || idle > StallFrames) Leave();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (jobs.Count == 0) { EditorApplication.update -= Tick; EditorApplication.Exit(0); return; }
                job = jobs.Dequeue();
                Directory.CreateDirectory(job.Folder);
                EditorSceneManager.OpenScene($"Assets/_Game/Scenes/Levels/Level_{job.Level.Substring(1)}.unity", OpenSceneMode.Single);
                nextShot = job.Solution ? step : Mathf.Max(1, job.LastTick - BetrayalLead);
                idle = 0; lastTick = -1; cam = null; observers = null; opened = true;
                EditorApplication.EnterPlaymode();
            }
            catch (System.Exception e) { Debug.LogError($"LevelRunCapture: {job?.Level} {job?.Route} failed: {e}"); Leave(); }
        }

        // A frame's worth of the level at a time, each shot one update after the camera moves, so the parallax layers have followed.
        static void SweepStep(int tick)
        {
            if (tick < 10) return;
            if (points == null)
            {
                LevelCameraFollow follow = cam.GetComponent<LevelCameraFollow>();
                var so = new SerializedObject(follow);
                Vector2 c = so.FindProperty("frameCenter").vector2Value, size = so.FindProperty("frameSize").vector2Value;
                follow.enabled = false;
                float h = cam.orthographicSize * 2f, w = h * W / H;
                points = new List<Vector2>();
                int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / (w * 0.8f))), rows = Mathf.Max(1, Mathf.CeilToInt(size.y / (h * 0.8f)));
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols; i++)
                        points.Add(new Vector2(
                            cols == 1 ? c.x : Mathf.Lerp(c.x - size.x * 0.5f + w * 0.5f, c.x + size.x * 0.5f - w * 0.5f, i / (cols - 1f)),
                            rows == 1 ? c.y : Mathf.Lerp(c.y - size.y * 0.5f + h * 0.5f, c.y + size.y * 0.5f - h * 0.5f, j / (rows - 1f))));
                point = 0; idle = 0;
            }
            if (idle++ % 2 == 0)
            {
                if (point >= points.Count) { points = null; Leave(); return; }
                cam.transform.position = new Vector3(points[point].x, points[point].y, cam.transform.position.z);
                return;
            }
            Shot(point++);
        }

        static int Next(int tick) => tick + (job.Solution ? step : BetrayalStep);

        static void Leave()
        {
            Time.captureDeltaTime = 0f;
            opened = false;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        static void Shot(int tick)
        {
            RenderTexture rt = cam.targetTexture;
            EnvironmentCapture.PrepareLights();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(job.Folder, $"t{tick:0000}.jpg"), tex.EncodeToJPG(88));
            Object.DestroyImmediate(tex);
            if (System.Environment.GetEnvironmentVariable("PARALLAX_RUN_DUMP") == "1") Dump(tick);
        }

        // What's in the frame, for tracing a mark on a screenshot to its object: each renderer's frame rectangle, in pixels from the top left.
        static void Dump(int tick)
        {
            float h = cam.orthographicSize * 2f, w = h * W / H;
            Vector2 c = cam.transform.position;
            var lines = new List<string>();
            foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.sprite == null || !r.gameObject.activeInHierarchy) continue;
                Bounds b = r.bounds;
                float x0 = (b.min.x - c.x + w * 0.5f) / w * W, x1 = (b.max.x - c.x + w * 0.5f) / w * W, y0 = (c.y + h * 0.5f - b.max.y) / h * H, y1 = (c.y + h * 0.5f - b.min.y) / h * H;
                if (x1 < 0f || x0 > W || y1 < 0f || y0 > H) continue;
                Transform t = r.transform;
                string path = t.parent != null ? t.parent.name + "/" + t.name : t.name;
                lines.Add($"{r.sortingLayerName}\t{r.sortingOrder}\t{path}\t{r.sprite.name}\t{r.drawMode}\tx {x0:0}-{x1:0}\ty {y0:0}-{y1:0}\t{r.color}\t{r.sharedMaterial?.name}");
            }
            File.WriteAllLines(Path.Combine(job.Folder, $"t{tick:0000}.txt"), lines);
        }
    }
}
