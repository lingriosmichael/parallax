using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-V08 (the developer: "arrows are super hard to see … make the arrow more visible regardless of the level it is in";
    // ruling: "Measure the 3:1 contrast in luminance, with 2D lights on, at every camera pose"). For every saved level scene,
    // every lethal sprite (spikes from their reveal, an arrow or spear in flight at three points of its lane, a launcher:
    // an honest one's body, a disguised one's slot) is posed as the game draws it, and the level camera is snapped to each
    // pose of its own that keeps the hazard in view (its target set around the hazard; SnapToTarget, the phone's 20:9 at
    // 2400 x 1080). Each pose is rendered with the level's 2D lights, global and point (prepared as EnvironmentCapture
    // does), the grade and the parallax, twice: with the hazard and without it. Over the pixels the hazard changes, the
    // per-pixel contrast ratio (relative luminance, (L1 + 0.05) / (L2 + 0.05)) is measured against what's behind it; a
    // hazard reads when at least MinShare of its pixels reach MinRatio (its outline or its rim: one of the two must stand
    // out, on any background). Reads scenes without saving and restores the Test Runner's scene (PAX-075 R21/R22).
    public sealed class HazardContrastTests
    {
        public const float MinRatio = 3f;
        public const float MinShare = 0.3f;
        const int W = 2400, H = 1080;
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        static readonly Vector2[] Offsets = { new(0f, 0f), new(-4f, 0f), new(4f, 0f), new(0f, -2f), new(0f, 2f), new(-4f, -2f), new(4f, 2f), new(-4f, 2f), new(4f, -2f) };

        public static IEnumerable<TestCaseData> Levels()
        {
            foreach (int n in Enumerable.Range(1, 20)) yield return new TestCaseData("L" + n.ToString("000")).SetName("Contrast:L" + n.ToString("000"));
        }

        [TestCaseSource(nameof(Levels))]
        [Timeout(900000)]
        public void EveryHazard_ReadsAgainstItsBackground_AtEveryCameraPose(string id)
        {
            string path = $"{LevelsFolder}/Level_{id.Substring(1)}.unity";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) Assert.Ignore($"{path} doesn't exist");
            var failures = new List<string>();
            var report = new List<string>();
            int measured = 0;
            WithScene(path, () => measured = Measure(id, failures, report));
            string outDir = Environment.GetEnvironmentVariable("PARALLAX_CONTRAST_OUT");
            if (!string.IsNullOrEmpty(outDir)) { Directory.CreateDirectory(outDir); File.WriteAllLines(Path.Combine(outDir, id + ".tsv"), report); }
            TestContext.WriteLine($"{id}: {measured} hazard poses measured");
            Assert.IsEmpty(failures, $"{id}: hazards that don't read (need {MinShare:P0} of their pixels at {MinRatio}:1 or more):\n" + string.Join("\n", failures.Take(40)));
        }

        // ---------- one level ----------

        sealed class Hazard
        {
            public string Name;
            public Action Pose;
            public SpriteRenderer[] Renderers;
        }

        static int Measure(string id, List<string> failures, List<string> report)
        {
            LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            Assert.NotNull(follow, $"{id}: no LevelCameraFollow");
            Camera cam = follow.GetComponent<Camera>();
            var target = (Transform)new SerializedObject(follow).FindProperty("target").objectReferenceValue;
            Assert.NotNull(target, $"{id}: the level camera has no target");
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = W / (float)H;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            int measured = 0;
            try
            {
                // Nothing but the room and its backdrop: no cat, no HUD, no effects, no grey-box.
                foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = true;
                foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) c.enabled = false;
                TrapArt[] arts = Object.FindObjectsByType<TrapArt>(FindObjectsSortMode.None);
                foreach (TrapArt art in arts)
                {
                    art.HideGreybox();
                    foreach (TrapArt.Effect e in art.Effects) if (e.Renderer != null) e.Renderer.forceRenderingOff = true;
                }
                List<Hazard> hazards = Hazards(arts);
                foreach (Hazard hazard in hazards)
                {
                    Unfire(arts);
                    hazard.Pose();
                    SpriteRenderer[] shown = hazard.Renderers.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy && r.sprite != null).ToArray();
                    if (shown.Length == 0) continue;
                    Bounds b = shown[0].bounds;
                    foreach (SpriteRenderer r in shown) b.Encapsulate(r.bounds);
                    float worst = float.PositiveInfinity; string worstAt = ""; (Color[] with, Color[] without, int w, int h) worstCrop = default;
                    foreach (Vector2 centre in Poses(follow, cam, target, b))
                    {
                        cam.transform.position = new Vector3(centre.x, centre.y, cam.transform.position.z);
                        RectInt crop = PixelRect(cam, b);
                        if (crop.width <= 0 || crop.height <= 0) continue;
                        Color[] with = Render(cam, rt, tex, crop);
                        foreach (SpriteRenderer r in shown) r.forceRenderingOff = true;
                        Color[] without = Render(cam, rt, tex, crop);
                        foreach (SpriteRenderer r in shown) r.forceRenderingOff = false;
                        (float share, int pixels) = Share(with, without);
                        if (pixels < 12) continue;
                        measured++;
                        report.Add($"{id}\t{hazard.Name}\t{centre.x:F2}\t{centre.y:F2}\t{share:F3}\t{pixels}");
                        if (share < worst) { worst = share; worstAt = $"camera ({centre.x:F1}, {centre.y:F1})"; worstCrop = (with, without, crop.width, crop.height); }
                    }
                    if (!float.IsPositiveInfinity(worst) && worst < MinShare)
                        failures.Add($"{id} {hazard.Name}: at {worstAt}, {worst:P0} of its pixels reach {MinRatio}:1");
                    SaveCrop(id, hazard.Name, worstCrop);
                }
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            return measured;
        }

        // Every lethal sprite, with how to pose it as the game draws it.
        static List<Hazard> Hazards(TrapArt[] arts)
        {
            var list = new List<Hazard>();
            foreach (TrapArt art in arts)
            {
                var so = new SerializedObject(art);
                if (art is SpikeArt spikes)
                {
                    var greybox = (SpriteRenderer)so.FindProperty("greyboxBody").objectReferenceValue;
                    var body = (SpriteRenderer)so.FindProperty("body").objectReferenceValue;
                    if (greybox == null || body == null) continue;
                    list.Add(new Hazard { Name = art.name, Renderers = new[] { body }, Pose = () => { greybox.enabled = true; spikes.Apply(); } });
                }
                else if (art is ArrowArt arrow && arrow.Trap is ArrowTrap trap)
                {
                    var greyboxArrow = (SpriteRenderer)so.FindProperty("greyboxArrow").objectReferenceValue;
                    var arrowArt = (SpriteRenderer)so.FindProperty("arrowArt").objectReferenceValue;
                    var shaftArt = (SpriteRenderer)so.FindProperty("shaftArt").objectReferenceValue;
                    var slot = (SpriteRenderer)so.FindProperty("slot").objectReferenceValue;
                    if (greyboxArrow == null || arrowArt == null) continue;
                    var ts = new SerializedObject(trap);
                    int tell = ts.FindProperty("tellTicks").intValue;
                    int flight = ArrowMath.FlightTicks(ts.FindProperty("travel").floatValue, ts.FindProperty("unitsPerTick").floatValue);
                    MethodInfo localPose = typeof(ArrowTrap).GetMethod("LocalPose", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(localPose, "ArrowTrap.LocalPose not found");
                    void Fly(int s)
                    {
                        greyboxArrow.transform.localPosition = (Vector2)localPose.Invoke(trap, new object[] { s });
                        greyboxArrow.transform.localRotation = Quaternion.Euler(0f, 0f, trap.KillAngle);
                        greyboxArrow.enabled = true;
                        arrow.Apply();
                    }
                    string what = arrow.Spear ? "spear" : "arrow";
                    foreach ((string label, int s) in new[] { ("leaving", tell + 1), ("mid-lane", tell + Mathf.Max(1, flight / 2)), ("arriving", tell + Mathf.Max(1, flight - 1)) })
                        list.Add(new Hazard { Name = $"{art.name} ({what} {label})", Renderers = new[] { arrowArt, shaftArt }, Pose = () => Fly(s) });
                    // The launcher: an honest one's body; a disguised one's slot, shown from its fire tick.
                    SpriteRenderer launcher = arrow.Disguised ? slot : arrow.LauncherArt;
                    if (launcher != null)
                        list.Add(new Hazard { Name = $"{art.name} (launcher)", Renderers = new[] { launcher }, Pose = () => Fly(tell + 1) });
                }
            }
            return list;
        }

        // Every trap back to its unfired look: arrows away, hidden and periodic spikes down (static spikes stay).
        static void Unfire(TrapArt[] arts)
        {
            foreach (TrapArt art in arts)
            {
                var so = new SerializedObject(art);
                if (art is ArrowArt)
                {
                    if (so.FindProperty("greyboxArrow").objectReferenceValue is SpriteRenderer a) a.enabled = false;
                }
                else if (art is SpikeArt && art.Trap != null)
                {
                    if (so.FindProperty("greyboxBody").objectReferenceValue is SpriteRenderer g) g.enabled = false;
                }
                else continue;
                art.Apply();
            }
        }

        // The level camera's own poses that keep `b` in view: its target set around the hazard, snapped (deduplicated).
        static IEnumerable<Vector2> Poses(LevelCameraFollow follow, Camera cam, Transform target, Bounds b)
        {
            var seen = new HashSet<Vector2Int>();
            Vector3 home = target.position;
            var poses = new List<Vector2>();
            foreach (Vector2 o in Offsets)
            {
                target.position = new Vector3(b.center.x + o.x, b.center.y + o.y, home.z);
                follow.SnapToTarget();
                Vector2 c = cam.transform.position;
                float h = cam.orthographicSize, w = h * cam.aspect;
                if (b.min.x < c.x - w || b.max.x > c.x + w || b.min.y < c.y - h || b.max.y > c.y + h) continue;
                if (seen.Add(new Vector2Int(Mathf.RoundToInt(c.x * 4f), Mathf.RoundToInt(c.y * 4f)))) poses.Add(c);
            }
            target.position = home;
            return poses;
        }

        // With PARALLAX_CONTRAST_OUT set: each hazard's worst pose, with and without it side by side, for review.
        static void SaveCrop(string id, string name, (Color[] with, Color[] without, int w, int h) crop)
        {
            string outDir = Environment.GetEnvironmentVariable("PARALLAX_CONTRAST_OUT");
            if (string.IsNullOrEmpty(outDir) || crop.with == null) return;
            var t = new Texture2D(crop.w * 2 + 4, crop.h, TextureFormat.RGBA32, false);
            t.SetPixels(0, 0, crop.w, crop.h, crop.with);
            t.SetPixels(crop.w + 4, 0, crop.w, crop.h, crop.without);
            t.Apply();
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, $"{id}_{string.Concat(name.Where(char.IsLetterOrDigit))}.png"), t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static RectInt PixelRect(Camera cam, Bounds b)
        {
            Vector3 lo = cam.WorldToViewportPoint(b.min), hi = cam.WorldToViewportPoint(b.max);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(lo.x * W) - 3, 0, W), x1 = Mathf.Clamp(Mathf.CeilToInt(hi.x * W) + 3, 0, W);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(lo.y * H) - 3, 0, H), y1 = Mathf.Clamp(Mathf.CeilToInt(hi.y * H) + 3, 0, H);
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        static Color[] Render(Camera cam, RenderTexture rt, Texture2D tex, RectInt crop)
        {
            UpdateParallax();
            PrepareLights();
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(crop.x, crop.y, crop.width, crop.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            return tex.GetPixels(0, 0, crop.width, crop.height);
        }

        /// <summary>The share of the hazard's pixels (those it changes by more than 4/255) whose contrast with what's behind
        /// them reaches MinRatio, and how many pixels it changes.</summary>
        public static (float share, int pixels) Share(Color[] with, Color[] without)
        {
            int changed = 0, reading = 0;
            for (int i = 0; i < with.Length; i++)
            {
                Color a = with[i], b = without[i];
                if (Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)) <= 4f / 255f) continue;
                changed++;
                if (Ratio(Luminance(a), Luminance(b)) >= MinRatio) reading++;
            }
            return (changed > 0 ? reading / (float)changed : 1f, changed);
        }

        /// <summary>Relative luminance of an sRGB colour (WCAG: linearised, 0.2126 R + 0.7152 G + 0.0722 B).</summary>
        public static float Luminance(Color c) =>
            0.2126f * Mathf.GammaToLinearSpace(c.r) + 0.7152f * Mathf.GammaToLinearSpace(c.g) + 0.0722f * Mathf.GammaToLinearSpace(c.b);

        public static float Ratio(float l1, float l2) => (Mathf.Max(l1, l2) + 0.05f) / (Mathf.Min(l1, l2) + 0.05f);

        // As the game's LateUpdate would: the parallax layers follow the camera, and every 2D point and shape light builds its
        // mesh and culling sphere (URP does that only in its LateUpdate; EnvironmentCapture, round 4).
        static readonly MethodInfo ParallaxLate = typeof(ParallaxLayer).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo FollowLate = typeof(FollowTransform).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        // The test assembly doesn't reference URP: Light2D by name.
        static readonly Type Light2D = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.Rendering.Universal.Light2D")).FirstOrDefault(t => t != null);
        static readonly MethodInfo LightMesh = Light2D?.GetMethod("UpdateMesh", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo LightSphere = Light2D?.GetMethod("UpdateBoundingSphere", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly PropertyInfo LightType = Light2D?.GetProperty("lightType");

        static void UpdateParallax()
        {
            foreach (ParallaxLayer p in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) ParallaxLate.Invoke(p, null);
        }

        static void PrepareLights()
        {
            foreach (FollowTransform f in Object.FindObjectsByType<FollowTransform>(FindObjectsSortMode.None)) FollowLate.Invoke(f, null);
            Assert.NotNull(LightMesh, "Light2D.UpdateMesh not found: point lights wouldn't render");
            Assert.NotNull(LightSphere, "Light2D.UpdateBoundingSphere not found: point lights wouldn't render");
            foreach (Object light in Object.FindObjectsByType(Light2D, FindObjectsSortMode.None))
            {
                if (LightType.GetValue(light).ToString() == "Global") continue;
                LightMesh.Invoke(light, new object[] { true });
                LightSphere.Invoke(light, null);
            }
        }

        static void WithScene(string path, Action body)
        {
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try { EditorSceneManager.OpenScene(path, OpenSceneMode.Single); body(); }
            finally
            {
                if (setup.Length == 0) Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                else
                {
                    var restore = new SceneSetup[setup.Length];
                    for (int i = 0; i < setup.Length; i++) restore[i] = new SceneSetup { path = setup[i], isActive = i == 0, isLoaded = true };
                    EditorSceneManager.RestoreSceneManagerSetup(restore);
                }
            }
        }

        // ---------- the measure itself ----------

        [Test]
        public void Ratio_MatchesWcag_BlackOnWhiteIs21() => Assert.AreEqual(21f, Ratio(Luminance(Color.white), Luminance(Color.black)), 0.01f);

        [Test]
        public void Share_CountsOnlyChangedPixels_AndThoseThatReachTheRatio()
        {
            var grey = new Color(0.5f, 0.5f, 0.5f);
            Color[] without = { grey, grey, grey, grey };
            Color[] with = { grey, Color.black, new Color(0.52f, 0.52f, 0.52f), new Color(0.75f, 0.75f, 0.75f) };   // unchanged, reads, too close, under 3:1
            (float share, int pixels) = Share(with, without);
            Assert.AreEqual(3, pixels);
            Assert.AreEqual(1f / 3f, share, 1e-4f);
        }
    }
}
