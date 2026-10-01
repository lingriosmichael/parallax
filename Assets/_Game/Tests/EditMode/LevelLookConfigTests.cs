using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-A15 §4 / A5: the approved time-of-day plan. L001–L010 carry exactly the grades the developer approved (band 1
    // plays as one day passing), every grade a recipe names exists in LevelLookConfig, L011–L020 use the default look
    // (ruling 2), the far layers never scale past 1.5 (ruling 4), and every recipe piece is a kit slot.
    public sealed class LevelLookConfigTests
    {
        static readonly Type Looks = Type.GetType("Parallax.Editor.Levels.LevelLooks, Parallax.Editor");
        static readonly Type ConfigType = Type.GetType("Parallax.Editor.Levels.LevelLookConfig, Parallax.Editor");
        const string ConfigPath = "Assets/_Game/Data/LevelLookConfig.asset";

        static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(o);
        static object Static(string name) => Looks.GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        static object For(string id) => Looks.GetMethod("For").Invoke(null, new object[] { id });

        [Test]
        public void BandOne_Grades_MatchTheApprovedPlan()
        {
            Assert.NotNull(Looks, "LevelLooks not found");
            var approved = (IDictionary)Static("ApprovedGrades");
            var expected = new Dictionary<string, string>
            {
                ["L001"] = "MorningGold", ["L002"] = "MorningGold", ["L003"] = "NoonGold", ["L004"] = "WarmAfternoon", ["L005"] = "Afternoon",
                ["L006"] = "LateAfternoon", ["L007"] = "GoldenHour", ["L008"] = "GoldenHour", ["L009"] = "DuskAmber", ["L010"] = "DuskRose",
            };
            foreach (KeyValuePair<string, string> pair in expected)
            {
                Assert.AreEqual(pair.Value, approved[pair.Key], pair.Key + " approved grade");
                Assert.AreEqual(pair.Value, Field(For(pair.Key), "Grade"), pair.Key + " recipe grade");
            }
        }

        [Test]
        public void BandTwo_UsesTheDefaultLook()
        {
            object fallback = Static("Default");
            for (int n = 11; n <= 20; n++) Assert.AreSame(fallback, For("L" + n.ToString("000")), $"L0{n}");
        }

        [Test]
        public void EveryRecipeGrade_ExistsInTheConfig_AndFarScaleIsCapped()
        {
            var config = AssetDatabase.LoadAssetAtPath(ConfigPath, ConfigType) ?? ScriptableObject.CreateInstance(ConfigType);
            if (AssetDatabase.LoadAssetAtPath(ConfigPath, ConfigType) == null) ConfigType.GetMethod("ResetToDefaults").Invoke(config, null);
            var grades = ((IEnumerable)Field(config, "Grades")).Cast<object>().Select(g => (string)Field(g, "Name")).ToList();
            var ids = ((IDictionary)Static("ById")).Keys.Cast<string>().ToList();
            foreach (string id in ids.Append("L015")) CollectionAssert.Contains(grades, Field(For(id), "Grade"), id);
            Assert.LessOrEqual((float)Field(config, "FarScale"), 1.5f);
            if (!AssetDatabase.Contains((UnityEngine.Object)config)) UnityEngine.Object.DestroyImmediate((UnityEngine.Object)config);
        }

        [Test]
        public void EveryRecipePiece_IsAKitSlot()
        {
            string json = System.IO.File.ReadAllText("Assets/_Game/Art/RealityA/Environment/Kit/env_kit.json");
            var looks = ((IDictionary)Static("ById")).Values.Cast<object>().Append(Static("Default"));
            var failures = new List<string>();
            foreach (object look in looks)
                foreach (object piece in (IEnumerable)Field(look, "Pieces"))
                {
                    string slot = (string)Field(piece, "Slot");
                    if (!json.Contains("\"name\": \"" + slot + "\"")) failures.Add(slot);
                }
            Assert.IsEmpty(failures, "recipe pieces with no kit slot: " + string.Join(", ", failures));
        }
    }
}
