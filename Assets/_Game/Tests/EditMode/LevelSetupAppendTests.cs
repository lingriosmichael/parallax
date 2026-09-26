using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-059b (the developer's review): New Level... built the scene, then threw "Object at index 0 is null" in
    // AppendToLevelList, because the LevelListConfig it loaded before rebuilding the scene was gone by then. The append now
    // reloads the config from its path when the reference it was handed is dead. A temporary config asset stands in for
    // the real one, which the test never touches; it is deleted in TearDown.
    public sealed class LevelSetupAppendTests
    {
        const string TempFolder = "Assets/ZZ_Pax059bTemp";
        const string TempPath = TempFolder + "/TempLevelList.asset";

        [SetUp]
        public void CreateTempConfig()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.CreateFolder("Assets", "ZZ_Pax059bTemp");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LevelListConfig>(), TempPath);
        }

        [TearDown]
        public void DeleteTempConfig() => AssetDatabase.DeleteAsset(TempFolder);

        [Test]
        public void AppendToLevelList_WithADeadConfigReference_ReloadsItFromItsPathAndAppends()
        {
            var dead = ScriptableObject.CreateInstance<LevelListConfig>();
            Object.DestroyImmediate(dead);
            MethodInfo append = Type.GetType("Parallax.Editor.Setup.LevelSetup, Parallax.Editor")
                .GetMethod("AppendToLevelList", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(append, "LevelSetup.AppendToLevelList not found.");

            var changes = new List<string>();
            append.Invoke(null, new object[] { dead, TempPath, "L099", "Level_099", "Level 99", changes });

            LevelListConfig reloaded = AssetDatabase.LoadAssetAtPath<LevelListConfig>(TempPath);
            Assert.AreEqual(1, reloaded.Levels.Count, string.Join("; ", changes));
            Assert.AreEqual("L099", reloaded.Levels[0].Id);
            Assert.AreEqual("Level_099", reloaded.Levels[0].SceneName);
        }
    }
}
