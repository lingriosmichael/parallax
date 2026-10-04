using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // The developer (2026-10-04): the critics' captures must include the 2D point lights. URP builds a point or shape light's
    // mesh and culling sphere only in its LateUpdate, so a render straight after moving the camera culled them all; captures
    // before b08d0cc (PAX-A13, A16 Phase 1 rounds 1-3) had none. EnvironmentCapture.Render prepares them (PrepareLights)
    // before each render. This renders L002 through that same step, with the level camera at its saved pose, once with the
    // level's point lights on and once with them off, and fails if the two images match: the lights didn't render.
    public sealed class EnvironmentCaptureLightsTests
    {
        const string ScenePath = "Assets/_Game/Scenes/Levels/Level_002.unity";
        const int W = 1200, H = 540;
        const float MinChangedShare = .05f;   // measured 2026-10-04 through the Play Mode capture: 53% of the play frame
        static readonly Type Capture = Type.GetType("Parallax.Editor.Art.EnvironmentCapture, Parallax.Editor");
        static readonly Type Light2D = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.Rendering.Universal.Light2D")).FirstOrDefault(t => t != null);

        [Test]
        public void ACapture_ChangesWhenThePointLightsAreSwitchedOff()
        {
            Assert.NotNull(Capture, "EnvironmentCapture not found");
            Assert.NotNull(Light2D, "Light2D not found");
            MethodInfo render = Capture.GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Camera) }, null);
            Assert.NotNull(render, "EnvironmentCapture.Render(Camera) not found");
            PropertyInfo lightType = Light2D.GetProperty("lightType");

            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsSortMode.None).FirstOrDefault();
                Assert.NotNull(follow, "L002 has no level camera");
                var cam = follow.GetComponent<Camera>();
                var points = Object.FindObjectsByType(Light2D, FindObjectsSortMode.None).Cast<Behaviour>().Where(l => lightType.GetValue(l).ToString() == "Point").ToList();
                Assert.IsNotEmpty(points, "L002 has no 2D point light");

                RenderTexture previous = cam.targetTexture;
                var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                try
                {
                    Color32[] on = Shot(render, cam);
                    foreach (Behaviour l in points) l.enabled = false;
                    Color32[] off = Shot(render, cam);
                    int changed = 0;
                    for (int i = 0; i < on.Length; i++)
                        if (Mathf.Max(Mathf.Abs(on[i].r - off[i].r), Mathf.Abs(on[i].g - off[i].g), Mathf.Abs(on[i].b - off[i].b)) > 3) changed++;
                    float share = changed / (float)on.Length;
                    TestContext.WriteLine($"L002: {points.Count} point lights; switching them off changes {share:P1} of the capture");
                    Assert.GreaterOrEqual(share, MinChangedShare, $"the capture barely changes without its {points.Count} point lights ({share:P1}): they don't render in captures");
                }
                finally
                {
                    cam.targetTexture = previous;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
            finally
            {
                // PAX-075 R21/R22: never leave a Level_NNN scene loaded; recreate the Test Runner's untitled scene.
                if (setup.Length == 0) Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                else EditorSceneManager.RestoreSceneManagerSetup(setup.Select((p, i) => new SceneSetup { path = p, isActive = i == 0, isLoaded = true }).ToArray());
            }
        }

        static Color32[] Shot(MethodInfo render, Camera cam)
        {
            var tex = (Texture2D)render.Invoke(null, new object[] { cam });
            Color32[] pixels = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            return pixels;
        }
    }
}
