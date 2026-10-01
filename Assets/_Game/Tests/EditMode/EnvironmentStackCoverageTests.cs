using System;
using System.Linq;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-A15 §2.3 coverage rule: in every saved level, the background fills the level camera's whole view at 4:3, 16:9
    // and 20:9 wherever the camera goes. The sky stays on the camera and is larger than the widest, tallest view; the far
    // haze spans the widest view. D-103: clouds are whole pieces placed once (no tiled band), so they carry no coverage
    // rule; the sky behind them covers. Reads scenes without saving and restores the Test Runner's scene.
    public sealed class EnvironmentStackCoverageTests
    {
        static readonly float[] Aspects = { 4f / 3f, 16f / 9f, 20f / 9f };
        static readonly string[] Bands = { "Haze" };

        [Test]
        public void EveryLevel_TheBackgroundCoversTheViewAtEveryAspect()
        {
            var failures = new System.Collections.Generic.List<string>();
            int checkedLevels = 0;
            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            try
            {
                for (int n = 1; n <= 20; n++)
                {
                    string id = "L" + n.ToString("000"), path = $"Assets/_Game/Scenes/Levels/Level_{n:000}.unity";
                    if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    RealityRoot rootA = scene.GetRootGameObjects().Select(g => g.GetComponent<RealityRoot>()).First(r => r != null && r.Id == Parallax.Core.ObserverId.A);
                    Transform env = rootA.transform.Find("Environment");
                    if (env == null) { failures.Add($"{id}: no Environment"); continue; }
                    checkedLevels++;
                    LevelCameraFollow follow = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
                    var so = new SerializedObject(follow);
                    Vector2 frame = so.FindProperty("frameSize").vector2Value;
                    // D-104: the level's own view (baked on its camera; 0 = the config's).
                    float baked = so.FindProperty("viewHeight").floatValue;
                    float maxView = baked > 0f ? baked : new SerializedObject(so.FindProperty("config").objectReferenceValue).FindProperty("maxViewHeight").floatValue;
                    float tallest = 0f, widest = 0f;
                    foreach (float a in Aspects)
                    {
                        float view = Mathf.Min(maxView, frame.y, frame.x / a);
                        tallest = Mathf.Max(tallest, view); widest = Mathf.Max(widest, view * a);
                    }

                    // The sky: on the camera (speed 0) and bigger than every view.
                    Transform sky = env.Find("Sky");
                    if (sky == null || Speed(sky) != 0f) { failures.Add($"{id}: the sky doesn't stay on the camera"); continue; }
                    Bounds horizon = sky.Find("Horizon").GetComponent<SpriteRenderer>().bounds;
                    if (horizon.size.x < widest || horizon.size.y < tallest) failures.Add($"{id}: sky {horizon.size} smaller than the view {widest}x{tallest}");

                    foreach (string band in Bands)
                    {
                        Transform layer = env.Find(band);
                        if (layer == null) { failures.Add($"{id}: no {band}"); continue; }
                        float span = layer.GetComponentsInChildren<SpriteRenderer>(true).Select(r => r.bounds.size.x).DefaultIfEmpty(0f).Max();
                        if (span < widest) failures.Add($"{id}: {band} spans {span:F1} u for a {widest:F1} u view");
                    }
                }
            }
            finally
            {
                if (setup.Length == 0) Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
                else EditorSceneManager.RestoreSceneManagerSetup(setup.Select((p, i) => new SceneSetup { path = p, isActive = i == 0, isLoaded = true }).ToArray());
            }
            Assert.Greater(checkedLevels, 0, "no level scene was checked");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static float Speed(Transform layer) => new SerializedObject(layer.GetComponent<ParallaxLayer>()).FindProperty("screenSpeed").floatValue;
    }
}
