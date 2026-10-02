using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-A16 gauntlet round 2 (the developer: "colour grading must never tint the cat or the hazards … no blown-out
    // whites"). Each level's post profile carries no hue: white balance and split toning neutral, the lift uncoloured, no
    // colour filter, saturation within the cap; Neutral tonemapping rolls the highlights off. The level's colour lives in its
    // environment pieces. Reads the profiles the Environment Stack setup writes (Data/LevelVolumes/Lnnn.asset).
    public sealed class EnvironmentGradeTests
    {
        static readonly Type Palettes = Type.GetType("Parallax.Editor.Levels.LevelPalettes, Parallax.Editor");
        const float Eps = 1e-3f;

        [Test]
        public void EveryLevelsProfile_IsHueNeutral_AndRollsTheHighlightsOff([NUnit.Framework.Range(1, 20)] int level)
        {
            string path = $"Assets/_Game/Data/LevelVolumes/L{level:000}.asset";
            UnityEngine.Object profile = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.NotNull(profile, path + " is missing: run the Environment Stack setup and Rebuild All Levels.");

            Assert.AreEqual("Neutral", Value(profile, "Tonemapping", "mode")?.ToString(), "Neutral tonemapping (no blown-out whites)");
            if (Has(profile, "WhiteBalance"))
            {
                Assert.AreEqual(0f, (float)Value(profile, "WhiteBalance", "temperature"), Eps, "white balance temperature");
                Assert.AreEqual(0f, (float)Value(profile, "WhiteBalance", "tint"), Eps, "white balance tint");
            }
            if (Has(profile, "SplitToning"))
                foreach (string field in new[] { "shadows", "highlights" })
                {
                    var c = (Color)Value(profile, "SplitToning", field);
                    Assert.AreEqual(c.r, c.g, Eps, $"split toning {field} are grey");
                    Assert.AreEqual(c.g, c.b, Eps, $"split toning {field} are grey");
                }
            if (Has(profile, "LiftGammaGain"))
            {
                var lift = (Vector4)Value(profile, "LiftGammaGain", "lift");
                Assert.AreEqual(lift.x, lift.y, Eps, "the lift is uncoloured");
                Assert.AreEqual(lift.y, lift.z, Eps, "the lift is uncoloured");
            }
            if (Has(profile, "ColorAdjustments"))
            {
                Assert.AreEqual(Color.white, (Color)Value(profile, "ColorAdjustments", "colorFilter"), "no colour filter");
                Assert.LessOrEqual(Mathf.Abs((float)Value(profile, "ColorAdjustments", "saturation")), 5f + Eps, "saturation within the cap");
            }
        }

        // The profile's components by type name, read by reflection (the test assembly doesn't reference URP).
        static object Component(UnityEngine.Object profile, string type) =>
            ((IEnumerable)profile.GetType().GetField("components").GetValue(profile)).Cast<object>().FirstOrDefault(c => c.GetType().Name == type);

        static bool Has(UnityEngine.Object profile, string type) => Component(profile, type) != null;

        static object Value(UnityEngine.Object profile, string type, string parameter)
        {
            object c = Component(profile, type);
            if (c == null) return null;
            object p = c.GetType().GetField(parameter, BindingFlags.Public | BindingFlags.Instance).GetValue(c);
            return p.GetType().GetProperty("value").GetValue(p);
        }

        [Test]
        public void TheLightsColour_IsCapped_SoItNeverTintsTheHazards()
        {
            var cap = (float)Palettes.GetField("LightSaturationCap").GetValue(null);
            var capLight = Palettes.GetMethod("CapLight");
            foreach (Color c in new[] { new Color(1f, 0.56f, 0.31f), new Color(0.3f, 0.4f, 1f), Color.white, new Color(0.5f, 0.5f, 0.5f) })
            {
                var capped = (Color)capLight.Invoke(null, new object[] { c });
                Color.RGBToHSV(capped, out float h0, out float s, out float v);
                Color.RGBToHSV(c, out float h1, out float s1, out float v1);
                Assert.LessOrEqual(s, cap + Eps, $"{c} capped to saturation {s}");
                Assert.AreEqual(v1, v, Eps, "the cap keeps the light's brightness");
            }
        }
    }
}
