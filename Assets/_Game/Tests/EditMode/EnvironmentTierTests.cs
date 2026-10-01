using System;
using System.Reflection;
using NUnit.Framework;
using Parallax.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-A16: the tier pieces' rules. Decor motion wraps a drifting band inside one tile and stops with render
    /// time; the frame pins to the view's corners at any aspect; the look config's defaults carry the A16 tiers (no veil,
    /// the far city and rays layers) and D-100's camera constants are the ones the setup menu writes.</summary>
    public sealed class EnvironmentTierTests
    {
        static readonly Type Stack = Type.GetType("Parallax.Editor.Setup.EnvironmentStackSetup, Parallax.Editor");
        static readonly Type ConfigType = Type.GetType("Parallax.Editor.Levels.LevelLookConfig, Parallax.Editor");
        static object Static(string name) => (object)Stack.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance).Invoke(o, args);
        static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(o);
        static object Layer(object config, string name) => Call(config, "GetLayer", name);
        [Test]
        public void AmbientMotion_DriftWrapsInsideOneTile_AndHoldsWhenTimeStops()
        {
            var go = new GameObject("drift");
            try
            {
                var motion = go.AddComponent<AmbientMotion>();
                motion.Configure(0.25f, 4f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
                for (int i = 0; i < 1000; i++)
                {
                    motion.Step(0.05f);
                    Assert.That(go.transform.localPosition.x, Is.InRange(-4f, 0f), "a drifting band stays within one tile of its place");
                }
                float x = go.transform.localPosition.x;
                for (int i = 0; i < 10; i++) motion.Step(0f);
                Assert.AreEqual(x, go.transform.localPosition.x, 1e-6f, "no render time (the pause), no motion");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void AmbientMotion_BobAndBreathe_StayWithinTheirAmplitude()
        {
            var go = new GameObject("bob");
            try
            {
                var sprite = go.AddComponent<SpriteRenderer>();
                sprite.color = new Color(1f, 1f, 1f, 0.5f);
                var motion = go.AddComponent<AmbientMotion>();
                motion.Configure(0f, 0f, 0.08f, 0.12f, 0f, 0f, 0.1f, 0.125f, 0.3f);
                for (int i = 0; i < 500; i++)
                {
                    motion.Step(0.04f);
                    Assert.That(go.transform.localPosition.y, Is.InRange(-0.0801f, 0.0801f));
                    Assert.That(sprite.color.a, Is.InRange(0.5f * 0.9f - 1e-4f, 0.5f * 1.1f + 1e-4f));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ViewportAnchor_PinsToTheViewsCorner_AtEveryAspect()
        {
            var camGo = new GameObject("cam");
            var child = new GameObject("corner");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true; cam.orthographicSize = (float)Static("ZoomedViewHeight") * 0.5f;
                child.transform.SetParent(camGo.transform, false);
                var anchor = child.AddComponent<ViewportAnchor>();
                foreach (float aspect in (float[])Static("Aspects"))
                {
                    cam.aspect = aspect;
                    anchor.Configure(cam, new Vector2(-1f, -1f), new Vector2(0.5f, 0.25f), 0f);
                    float h = cam.orthographicSize, w = h * aspect;
                    Assert.AreEqual(-w + 0.5f, child.transform.localPosition.x, 1e-4f, $"x at {aspect:F2}");
                    Assert.AreEqual(-h + 0.25f, child.transform.localPosition.y, 1e-4f, $"y at {aspect:F2}");
                }
            }
            finally { Object.DestroyImmediate(child); Object.DestroyImmediate(camGo); }
        }

        [Test]
        public void LookConfigDefaults_CarryTheTiers()
        {
            Assert.NotNull(ConfigType, "LevelLookConfig not found");
            var config = ScriptableObject.CreateInstance(ConfigType);
            try
            {
                Call(config, "ResetToDefaults");
                Assert.AreEqual(ConfigType.GetField("CurrentVersion").GetValue(null), Field(config, "Version"));
                Assert.AreEqual(0f, (float)Field(Layer(config, "Veil"), "Alpha"), "A16 removes A15's haze veil");
                foreach (string name in (string[])ConfigType.GetProperty("LayerNames").GetValue(null)) Assert.NotNull(Layer(config, name), name);
                Assert.NotNull(Layer(config, "FarCity")); Assert.NotNull(Layer(config, "Rays"));
                // §3.1: value falls toward the viewer (the foreground darkest, the far layers palest).
                float Value(string n) => (float)Field(Layer(config, n), "Value");
                Assert.Less(Value("Foreground"), Value("Mid"));
                Assert.LessOrEqual(Value("Mid"), Value("FarCity"));
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void CameraZoom_IsD102s()
        {
            Assert.NotNull(Stack, "EnvironmentStackSetup not found");
            Assert.AreEqual(16f / 1.8f, (float)Stack.GetField("ZoomedViewHeight").GetValue(null), 1e-5f);
            Assert.AreEqual(1.5f, (float)Stack.GetField("ZoomedLookAhead").GetValue(null), 1e-5f);
            Assert.AreEqual(new Vector2(1f, 1.6f), (Vector2)Static("ZoomedDeadZone"));
        }
    }
}
