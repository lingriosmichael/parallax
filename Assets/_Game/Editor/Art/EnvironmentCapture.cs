using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Editor.Levels;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A15 §5, §13: captures and measures levels in Play Mode, as the player sees them. For each level it
    /// enters Play Mode, lets the level start (the cat idles at its checkpoint), and renders the level camera at phone
    /// scale (2400 × 1080, the Pixel 8a in landscape), then 16:9 and 4:3 (coverage), then the whole camera frame. It
    /// measures the readability checks (A1 surface steps, A3 flip rings, A6 the door's rank), the render counters
    /// (batches, draw calls, SetPass) and the texture memory on Android, and writes PNGs and metrics.json.
    /// Batch mode on a clone: Unity -batchmode -projectPath clone -executeMethod Parallax.Editor.Art.EnvironmentCapture.RunBatch
    /// with PARALLAX_CAPTURE_OUT (a folder) and PARALLAX_CAPTURE_LEVELS (e.g. "1-20"). 2D point and shape lights are
    /// prepared before each render (PrepareLights); no HUD is drawn.</summary>
    [InitializeOnLoad]
    public static partial class EnvironmentCapture
    {
        const int W = 2400, H = 1080, SettleFrames = 60;
        const string KeyLevel = "pax.envcap.level", KeyLast = "pax.envcap.last", KeyPhase = "pax.envcap.phase", KeyOut = "pax.envcap.out";
        static int frames;
        static readonly Dictionary<string, ProfilerRecorder> recorders = new();
        static readonly Dictionary<string, long> peaks = new();

        static EnvironmentCapture() { EditorApplication.update += Tick; }

        public static void RunBatch()
        {
            string output = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_OUT");
            string range = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_LEVELS") ?? "1-10";
            if (string.IsNullOrEmpty(output)) { Debug.LogError("EnvironmentCapture: set PARALLAX_CAPTURE_OUT."); EditorApplication.Exit(1); return; }
            string[] parts = range.Split('-');
            Directory.CreateDirectory(output);
            SessionState.SetString(KeyOut, output);
            SessionState.SetInt(KeyLevel, int.Parse(parts[0]));
            SessionState.SetInt(KeyLast, int.Parse(parts[parts.Length - 1]));
            SessionState.SetInt(KeyPhase, 0);
        }

        [System.Serializable] public sealed class Step { public string name; public float value, from, to, line; }
        [System.Serializable]
        public sealed class Metrics
        {
            public string level, grade;
            public float viewHeight;
            public List<Step> surfaces = new();
            public float surfaceMin, surfaceMedian;
            public List<Step> rings = new();
            public float doorContrast; public int doorRank; public string doorRankedBelow;
            /// <summary>A6, second measure: the brightest tenth of each element (a lit doorway) against its surround.</summary>
            public float doorPeakContrast; public int doorPeakRank; public string doorPeakRankedBelow;
            public float clearFraction169, clearFraction43, clearFractionPhone;
            public long batches, drawCalls, setPass, batchesPeak, drawCallsPeak, setPassPeak;
            public float envTextureMB, realityBTextureMB, otherTextureMB;
            public List<string> textures = new();
            // PAX-A16 §2, on the phone shot (2400 x 1080).
            public float darkFraction, brightFraction, featurelessFraction;
            public float catContrast;
            public List<Step> lips = new();
            public float lipOverAirMin, airOverBodyMin;
            public int motions; public string motionThirds;
            /// <summary>Diagnostic: the visible renderers in draw order and the shader switches between them (SetPass's scene share).</summary>
            public int shaderSwitches; public List<string> drawOrder = new();
        }

        static void Tick()
        {
            int level = SessionState.GetInt(KeyLevel, 0);
            if (level == 0) return;
            int phase = SessionState.GetInt(KeyPhase, 0);
            string output = SessionState.GetString(KeyOut, "");
            string id = "L" + level.ToString("000");
            try
            {
                if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (level > SessionState.GetInt(KeyLast, 0)) { SessionState.SetInt(KeyLevel, 0); EditorApplication.Exit(0); return; }
                    EditorSceneManager.OpenScene($"Assets/_Game/Scenes/Levels/Level_{level:000}.unity", OpenSceneMode.Single);
                    SessionState.SetInt(KeyPhase, 1);
                    frames = 0;
                    EditorApplication.EnterPlaymode();
                }
                else if (phase == 1 && EditorApplication.isPlaying)
                {
                    LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsSortMode.None).FirstOrDefault();
                    Camera cam = follow != null ? follow.GetComponent<Camera>() : null;
                    if (cam == null) { Debug.LogError($"EnvironmentCapture: {id} has no level camera."); Next(level); return; }
                    if (frames == 0)
                    {
                        cam.targetTexture = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
                        StartRecorders();
                    }
                    frames++;
                    SamplePeaks();
                    if (frames < SettleFrames) return;
                    Metrics m = Measure(id, cam, follow, output);
                    File.WriteAllText(Path.Combine(output, id + "_metrics.json"), JsonUtility.ToJson(m, true));
                    StopRecorders();
                    Next(level);
                }
            }
            catch (System.Exception e) { Debug.LogError($"EnvironmentCapture: {id} failed: {e}"); StopRecorders(); Next(level); }
        }

        static void Next(int level)
        {
            SessionState.SetInt(KeyLevel, level + 1);
            SessionState.SetInt(KeyPhase, 0);
            EditorApplication.ExitPlaymode();
        }

        static readonly string[] Counters = { "Batches Count", "Draw Calls Count", "SetPass Calls Count" };
        static void StartRecorders()
        {
            StopRecorders();
            foreach (string c in Counters) { recorders[c] = ProfilerRecorder.StartNew(ProfilerCategory.Render, c); peaks[c] = 0; }
        }
        static void SamplePeaks() { foreach (var pair in recorders) if (pair.Value.Valid) peaks[pair.Key] = System.Math.Max(peaks[pair.Key], pair.Value.LastValue); }
        static void StopRecorders() { foreach (var r in recorders.Values) r.Dispose(); recorders.Clear(); }
        static long Last(string c) => recorders.TryGetValue(c, out var r) && r.Valid ? r.LastValue : -1;

        // ---------- measuring ----------

        static Metrics Measure(string id, Camera cam, LevelCameraFollow follow, string output)
        {
            var m = new Metrics { level = id, grade = LevelLooks.For(id).Grade, viewHeight = cam.orthographicSize * 2f };
            // PAX-A16: clear to magenta for the captures, so a coverage gap can't be confused with the frame's near-black art.
            cam.backgroundColor = new Color(1f, 0f, 1f, 1f);
            m.batches = Last("Batches Count"); m.drawCalls = Last("Draw Calls Count"); m.setPass = Last("SetPass Calls Count");
            m.batchesPeak = peaks.GetValueOrDefault("Batches Count"); m.drawCallsPeak = peaks.GetValueOrDefault("Draw Calls Count"); m.setPassPeak = peaks.GetValueOrDefault("SetPass Calls Count");

            Texture2D phone = Render(cam);
            Save(phone, Path.Combine(output, id + "_play.png"));
            m.clearFractionPhone = ClearFraction(phone, cam.backgroundColor);
            if (LevelLayouts.ById.TryGetValue(id, out SoloRoomDefinition room))
            {
                MeasureSurfaces(m, phone, cam, room);
                MeasureRings(m, phone, cam, room);
                MeasureTiers(m, phone, cam, room);
            }
            MeasureValues(m, phone);
            MeasureMotion(m, phone, cam);
            MeasureDrawOrder(m, phone, cam);
            Object.DestroyImmediate(phone);
            MeasureTextures(m);

            // Coverage at 16:9 and 4:3, then the whole frame.
            m.clearFraction169 = Coverage(cam, follow, 1920, Path.Combine(output, id + "_169.png"));
            m.clearFraction43 = Coverage(cam, follow, 1440, Path.Combine(output, id + "_43.png"));
            var so = new SerializedObject(follow);
            Vector2 fc = so.FindProperty("frameCenter").vector2Value, fs = so.FindProperty("frameSize").vector2Value;
            // Gauntlet Phase 2: a mid-level shot at the phone's frame. The cat stands on the walkable top nearest the frame's
            // middle and the level camera snaps to it, as it would there in play.
            if (LevelLayouts.ById.TryGetValue(id, out SoloRoomDefinition midRoom) && so.FindProperty("target").objectReferenceValue is Transform cat)
            {
                var tops = SoloRoomSkin.WalkableTops(midRoom);
                if (tops.Count > 0)
                {
                    SoloRoomSkin.Edge best = tops.OrderBy(t => Mathf.Abs(Mathf.Clamp(fc.x, t.From + 0.5f, t.To - 0.5f) - fc.x) + 0.02f * Mathf.Abs(t.Line - fc.y)).First();
                    float x = Mathf.Clamp(fc.x, best.From + 0.5f, best.To - 0.5f);
                    Vector3 home = cat.position;
                    cat.position = new Vector3(x, best.Line + 0.3f, cat.position.z);
                    SetTarget(cam, W, H);
                    follow.SnapToTarget();
                    UpdateParallax();
                    Texture2D mid = Render(cam);
                    Save(mid, Path.Combine(output, id + "_mid.png"));
                    Object.DestroyImmediate(mid);
                    cat.position = home;
                }
            }
            follow.enabled = false;
            SetTarget(cam, W, H);
            cam.orthographicSize = Mathf.Max(fs.y, fs.x / (W / (float)H)) * 0.5f;
            cam.transform.position = new Vector3(fc.x, fc.y, cam.transform.position.z);
            UpdateParallax();
            Texture2D full = Render(cam);
            Save(full, Path.Combine(output, id + "_full.png"));
            // A6 on the whole frame: the door is often off screen when the level starts.
            MeasureDoor(m, full, cam);
            Object.DestroyImmediate(full);
            return m;
        }

        static float Coverage(Camera cam, LevelCameraFollow follow, int width, string path)
        {
            SetTarget(cam, width, H);
            follow.SnapToTarget();
            UpdateParallax();
            Texture2D shot = Render(cam);
            Save(shot, path);
            float f = ClearFraction(shot, cam.backgroundColor);
            Object.DestroyImmediate(shot);
            return f;
        }

        static void SetTarget(Camera cam, int w, int h)
        {
            RenderTexture old = cam.targetTexture;
            cam.targetTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            if (old != null) old.Release();
        }

        static void UpdateParallax()
        {
            var late = typeof(ParallaxLayer).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (ParallaxLayer p in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) late.Invoke(p, null);
        }

        // URP builds a 2D light's mesh and culling sphere only in its LateUpdate. The capture moves the camera and renders
        // straight away, so without this every point and shape light kept its default sphere (radius 0 at the origin) and was
        // culled: captures had no point lights (round 4: "captures must include 2D lights").
        static readonly System.Reflection.MethodInfo LightMesh = typeof(UnityEngine.Rendering.Universal.Light2D).GetMethod("UpdateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        static readonly System.Reflection.MethodInfo LightSphere = typeof(UnityEngine.Rendering.Universal.Light2D).GetMethod("UpdateBoundingSphere", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        static void PrepareLights()
        {
            // The followers (the cat's key light, the sun's backlight) move to their targets first, as their LateUpdate would.
            var follow = typeof(Parallax.Presentation.FollowTransform).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var f in Object.FindObjectsByType<Parallax.Presentation.FollowTransform>(FindObjectsSortMode.None)) follow.Invoke(f, null);
            if (LightMesh == null || LightSphere == null) { Debug.LogError("EnvironmentCapture: Light2D.UpdateMesh/UpdateBoundingSphere not found; point lights won't render."); return; }
            foreach (var light in Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None))
            {
                if (light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) continue;
                LightMesh.Invoke(light, new object[] { true });
                LightSphere.Invoke(light, null);
            }
        }

        static Texture2D Render(Camera cam)
        {
            RenderTexture rt = cam.targetTexture;
            PrepareLights();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        static void Save(Texture2D tex, string path) => File.WriteAllBytes(path, tex.EncodeToPNG());

        static float ClearFraction(Texture2D tex, Color clear)
        {
            Color32[] px = tex.GetPixels32();
            Color32 c = clear;
            // ±24 per channel: the post-processing grade shifts the clear colour slightly (magenta never occurs in the art).
            int n = px.Count(p => Mathf.Abs(p.r - c.r) <= 24 && Mathf.Abs(p.g - c.g) <= 24 && Mathf.Abs(p.b - c.b) <= 24);
            return n / (float)px.Length;
        }

        static float Lum(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        /// <summary>Mean luminance over a world rectangle, skipping pixels inside `skip` rectangles; NaN with too few pixels.</summary>
        static float MeanLum(Texture2D tex, Camera cam, Rect world, List<Rect> skip, int minPixels = 30)
        {
            float viewH = cam.orthographicSize * 2f, viewW = viewH * tex.width / tex.height;
            Vector2 c = cam.transform.position;
            int x0 = Mathf.FloorToInt((world.xMin - c.x) / viewW * tex.width + tex.width * 0.5f), x1 = Mathf.CeilToInt((world.xMax - c.x) / viewW * tex.width + tex.width * 0.5f);
            int y0 = Mathf.FloorToInt((world.yMin - c.y) / viewH * tex.height + tex.height * 0.5f), y1 = Mathf.CeilToInt((world.yMax - c.y) / viewH * tex.height + tex.height * 0.5f);
            x0 = Mathf.Clamp(x0, 0, tex.width); x1 = Mathf.Clamp(x1, 0, tex.width); y0 = Mathf.Clamp(y0, 0, tex.height); y1 = Mathf.Clamp(y1, 0, tex.height);
            double sum = 0; int n = 0;
            for (int y = y0; y < y1; y += 2)
                for (int x = x0; x < x1; x += 2)
                {
                    if (skip != null && skip.Count > 0)
                    {
                        var p = new Vector2(c.x + (x + 0.5f - tex.width * 0.5f) / tex.width * viewW, c.y + (y + 0.5f - tex.height * 0.5f) / tex.height * viewH);
                        if (skip.Any(s => s.Contains(p))) continue;
                    }
                    sum += Lum(tex.GetPixel(x, y)); n++;
                }
            return n * 4 >= minPixels ? (float)(sum / n) : float.NaN;
        }

        static Rect ViewRect(Camera cam, Texture2D tex)
        {
            float h = cam.orthographicSize * 2f, w = h * tex.width / tex.height;
            return new Rect((Vector2)cam.transform.position - new Vector2(w, h) * 0.5f, new Vector2(w, h));
        }

        /// <summary>A1: for each walkable top in view, the step between the surface band (0.05–0.25 u below the line) and
        /// the air above it (0.35–0.65 u up), air pixels inside solids and around the cat skipped.</summary>
        static void MeasureSurfaces(Metrics m, Texture2D tex, Camera cam, SoloRoomDefinition room)
        {
            Rect view = ViewRect(cam, tex);
            List<Rect> solids = SoloRoomSkin.Solids(room).Select(s => s.Rect).ToList();
            CatMotor2D cat = Object.FindObjectsByType<CatMotor2D>(FindObjectsSortMode.None).FirstOrDefault();
            var skip = new List<Rect>(solids);
            if (cat != null) skip.Add(new Rect((Vector2)cat.transform.position - new Vector2(0.9f, 0.9f), new Vector2(1.8f, 1.8f)));
            // The door and its pocket are the door, not the background behind a surface.
            foreach (Parallax.Gameplay.Rooms.RoomDoor door in Object.FindObjectsByType<Parallax.Gameplay.Rooms.RoomDoor>(FindObjectsSortMode.None))
                if (door.transform.Find("Art")?.GetComponent<SpriteRenderer>() is SpriteRenderer art)
                    skip.Add(Rect.MinMaxRect(art.bounds.min.x - 1.5f, art.bounds.min.y - 1.5f, art.bounds.max.x + 1.5f, art.bounds.max.y + 1.5f));
            // Traps and hazards (both poses for anything that moves) aren't background either, and spikes over a surface
            // aren't the surface.
            var traps = new List<Rect>();
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Kind is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.PitBottom
                    or SoloRoomElementKind.Checkpoint or SoloRoomElementKind.Door or SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.FakePlatform) continue;
                var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                traps.Add(r);
                if (e.Kind == SoloRoomElementKind.MovingTrap) traps.Add(new Rect(r.position + e.Settings.Offset, r.size));
            }
            skip.AddRange(traps);
            foreach (SoloRoomSkin.Edge top in SoloRoomSkin.WalkableTops(room))
            {
                float from = Mathf.Max(top.From + 0.2f, view.xMin), to = Mathf.Min(top.To - 0.2f, view.xMax);
                if (to - from < 0.3f || top.Line < view.yMin + 0.3f || top.Line > view.yMax - 0.7f) continue;
                float surface = MeanLum(tex, cam, Rect.MinMaxRect(from, top.Line - 0.25f, to, top.Line - 0.05f), traps);
                float air = MeanLum(tex, cam, Rect.MinMaxRect(from, top.Line + 0.35f, to, top.Line + 0.65f), skip);
                if (float.IsNaN(surface) || float.IsNaN(air)) continue;
                m.surfaces.Add(new Step { name = top.Owner.Name, value = Mathf.Abs(surface - air), from = from, to = to, line = top.Line });
            }
            if (m.surfaces.Count == 0) return;
            List<float> values = m.surfaces.Select(s => s.value).OrderBy(v => v).ToList();
            m.surfaceMin = values[0];
            m.surfaceMedian = values[values.Count / 2];
        }

        /// <summary>A3: in a disc around each visible flip zone, the brightest 15% of pixels (the gold ring) against the
        /// darker half (what's behind it).</summary>
        static void MeasureRings(Metrics m, Texture2D tex, Camera cam, SoloRoomDefinition room)
        {
            Rect view = ViewRect(cam, tex);
            float viewH = cam.orthographicSize * 2f, ppu = tex.height / viewH;
            Vector2 c = cam.transform.position;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.GravityFlip && e.Settings.RendererEnabled))
            {
                Vector2 centre = room.Origin + e.Position;
                float radius = Mathf.Min(e.Size.x, e.Size.y) * 0.5f * 1.3f;
                if (!view.Contains(centre)) continue;
                var lums = new List<float>();
                int cx = Mathf.RoundToInt((centre.x - c.x) * ppu + tex.width * 0.5f), cy = Mathf.RoundToInt((centre.y - c.y) * ppu + tex.height * 0.5f), r = Mathf.RoundToInt(radius * ppu);
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (x * x + y * y <= r * r && cx + x >= 0 && cx + x < tex.width && cy + y >= 0 && cy + y < tex.height)
                            lums.Add(Lum(tex.GetPixel(cx + x, cy + y)));
                if (lums.Count < 50) continue;
                lums.Sort();
                float bright = lums.Skip((int)(lums.Count * 0.85f)).Average(), dark = lums.Take(lums.Count / 2).Average();
                m.rings.Add(new Step { name = e.Name, value = bright - dark, line = centre.y, from = centre.x });
            }
        }

        /// <summary>A6: the door's local contrast (its art against the 1 u around it) and its rank among the environment's
        /// pieces in view (dressing, checkpoint markers, background pieces).</summary>
        static void MeasureDoor(Metrics m, Texture2D tex, Camera cam)
        {
            Rect view = ViewRect(cam, tex);
            float Contrast(Bounds b, bool peak)
            {
                var inner = new Rect(b.center.x - b.extents.x * 0.8f, b.center.y - b.extents.y * 0.8f, b.size.x * 0.8f, b.size.y * 0.8f);
                var ring = Rect.MinMaxRect(b.min.x - 1f, b.min.y - 1f, b.max.x + 1f, b.max.y + 1f);
                float a = peak ? PeakLum(tex, cam, inner) : MeanLum(tex, cam, inner, null, 10), around = MeanLum(tex, cam, ring, new List<Rect> { new(b.min, b.size) }, 10);
                return float.IsNaN(a) || float.IsNaN(around) ? float.NaN : Mathf.Abs(a - around);
            }
            SpriteRenderer door = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .FirstOrDefault(r => r.name == "Art" && r.transform.parent != null && r.transform.parent.GetComponent<Parallax.Gameplay.Rooms.RoomDoor>() != null && r.enabled);
            if (door == null || !view.Overlaps(new Rect(door.bounds.min, door.bounds.size))) { m.doorRank = -1; return; }
            m.doorContrast = Contrast(door.bounds, false);
            m.doorPeakContrast = Contrast(door.bounds, true);
            var others = new List<(string, float, float)>();
            foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.sprite == null || r == door) continue;
                string path = PathOf(r.transform);
                bool dressing = path.Contains("/" + SoloRoomSkin.EnvRootName + "/") && (r.name.StartsWith("Moss") || r.name.StartsWith("Drape") || r.name.StartsWith("Banner") || r.name.StartsWith("Glyph") || r.name.StartsWith("Rubble"));
                bool piece = path.Contains("/" + EnvironmentStackSetup.RootName + "/") && r.name.StartsWith("ENV_");
                bool marker = r.name.EndsWith("_Marker") || r.name.Contains("Checkpoint");
                if (!dressing && !piece && !marker) continue;
                if (!view.Overlaps(new Rect(r.bounds.min, r.bounds.size))) continue;
                float c = Contrast(r.bounds, false), p = Contrast(r.bounds, true);
                if (!float.IsNaN(c)) others.Add((r.name, c, float.IsNaN(p) ? 0f : p));
            }
            var above = others.Where(o => o.Item2 > m.doorContrast).OrderByDescending(o => o.Item2).ToList();
            m.doorRank = above.Count + 1;
            m.doorRankedBelow = string.Join(", ", above.Take(5).Select(o => $"{o.Item1} {o.Item2:F3}"));
            var abovePeak = others.Where(o => o.Item3 > m.doorPeakContrast).OrderByDescending(o => o.Item3).ToList();
            m.doorPeakRank = abovePeak.Count + 1;
            m.doorPeakRankedBelow = string.Join(", ", abovePeak.Take(5).Select(o => $"{o.Item1} {o.Item3:F3}"));
        }

        /// <summary>The 90th-percentile luminance over a world rectangle (NaN when off screen).</summary>
        static float PeakLum(Texture2D tex, Camera cam, Rect world)
        {
            float viewH = cam.orthographicSize * 2f, viewW = viewH * tex.width / tex.height;
            Vector2 c = cam.transform.position;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((world.xMin - c.x) / viewW * tex.width + tex.width * 0.5f), 0, tex.width), x1 = Mathf.Clamp(Mathf.CeilToInt((world.xMax - c.x) / viewW * tex.width + tex.width * 0.5f), 0, tex.width);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((world.yMin - c.y) / viewH * tex.height + tex.height * 0.5f), 0, tex.height), y1 = Mathf.Clamp(Mathf.CeilToInt((world.yMax - c.y) / viewH * tex.height + tex.height * 0.5f), 0, tex.height);
            var lums = new List<float>();
            for (int y = y0; y < y1; y += 2) for (int x = x0; x < x1; x += 2) lums.Add(Lum(tex.GetPixel(x, y)));
            if (lums.Count < 4) return float.NaN;
            lums.Sort();
            return lums[(int)(lums.Count * 0.9f)];
        }

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        // ---------- texture memory (Android formats) ----------

        static void MeasureTextures(Metrics m)
        {
            var atlases = AssetDatabase.FindAssets("t:SpriteAtlas").Select(g => AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(g))).Where(a => a != null).ToList();
            var counted = new Dictionary<string, (long bytes, string category)>();
            void Count(string key, long bytes, string category) { if (!counted.ContainsKey(key)) counted[key] = (bytes, category); }
            string Category(string path) => path.Contains("/RealityA/Environment/") || path.Contains("/Art/Doors/") ? "env" : path.Contains("/RealityB/") ? "realityB" : "other";

            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer is SpriteRenderer sr && sr.sprite != null)
                {
                    string path = AssetDatabase.GetAssetPath(sr.sprite);
                    SpriteAtlas atlas = atlases.FirstOrDefault(a => a.CanBindTo(sr.sprite));
                    if (atlas != null && sr.sprite.texture != null && sr.sprite.texture.name.StartsWith("sactx"))
                    {
                        TextureImporterPlatformSettings ps = atlas.GetPlatformSettings("Android");
                        TextureImporterFormat f = ps.overridden ? ps.format : TextureImporterFormat.ASTC_6x6;
                        Count("atlas:" + atlas.name + ":" + sr.sprite.texture.name, Bytes(sr.sprite.texture.width, sr.sprite.texture.height, f, false), Category(path));
                    }
                    else CountTexture(path, Category(path), Count);
                    var secondary = new SecondarySpriteTexture[sr.sprite.GetSecondaryTextureCount()];
                    sr.sprite.GetSecondaryTextures(secondary);
                    foreach (SecondarySpriteTexture s in secondary) if (s.texture != null) { string p = AssetDatabase.GetAssetPath(s.texture); CountTexture(p, Category(p), Count); }
                }
                else if (renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null && !(renderer is SpriteRenderer))
                {
                    string p = AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture);
                    if (!string.IsNullOrEmpty(p)) CountTexture(p, Category(p), Count);
                }
            }
            m.envTextureMB = counted.Values.Where(v => v.category == "env").Sum(v => v.bytes) / 1048576f;
            m.realityBTextureMB = counted.Values.Where(v => v.category == "realityB").Sum(v => v.bytes) / 1048576f;
            m.otherTextureMB = counted.Values.Where(v => v.category == "other").Sum(v => v.bytes) / 1048576f;
            m.textures = counted.OrderByDescending(p => p.Value.bytes).Select(p => $"{p.Value.category} {p.Value.bytes / 1024f:F0} KB {p.Key}").ToList();
        }

        static void CountTexture(string path, string category, System.Action<string, long, string> count)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/")) return;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || tex == null) return;
            TextureImporterPlatformSettings ps = importer.GetPlatformTextureSettings("Android");
            TextureImporterFormat f = ps.overridden ? ps.format : importer.GetAutomaticFormat("Android");
            count(path, Bytes(tex.width, tex.height, f, importer.mipmapEnabled), category);
        }

        /// <summary>Bytes on the device for a texture of this size and Android format.</summary>
        public static long Bytes(int w, int h, TextureImporterFormat f, bool mips)
        {
            double bits = f switch
            {
                TextureImporterFormat.ASTC_4x4 => 8, TextureImporterFormat.ASTC_5x5 => 5.12, TextureImporterFormat.ASTC_6x6 => 128.0 / 36.0,
                TextureImporterFormat.ASTC_8x8 => 2, TextureImporterFormat.ASTC_10x10 => 1.28, TextureImporterFormat.ASTC_12x12 => 128.0 / 144.0,
                TextureImporterFormat.ETC2_RGBA8 or TextureImporterFormat.ETC2_RGBA8Crunched => 8, TextureImporterFormat.ETC2_RGB4 or TextureImporterFormat.ETC_RGB4 or TextureImporterFormat.ETC_RGB4Crunched => 4,
                TextureImporterFormat.Alpha8 or TextureImporterFormat.R8 => 8, TextureImporterFormat.RGBA16 or TextureImporterFormat.RGB16 or TextureImporterFormat.RG16 => 16,
                _ => 32,
            };
            double bytes = w * (double)h * bits / 8.0;
            return (long)(mips ? bytes * 4.0 / 3.0 : bytes);
        }
    }
}
