using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace Parallax.Tests.EditMode
{
    // PAX-053 §2.1/§5.1-5.3 (D-072). BuildSceneList.Compute is a pure function of
    // (currentEntries, configScenePaths, levelsFolder) - it never touches EditorBuildSettings or
    // the real project, so these tests build fixture EditorBuildSettingsScene[] in memory rather
    // than writing the real settings (per pax-ticket review correction). Compute lives in the
    // Editor assembly, which this test assembly does not reference, so access goes through
    // reflection, matching LevelSceneTests.cs/LevelLayoutTests.cs's existing pattern.
    public sealed class BuildSceneListTests
    {
        const string LevelSelectPath = "Assets/_Game/Scenes/LevelSelect.unity";
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";

        static readonly Type BuildSceneListType = Type.GetType("Parallax.Editor.Setup.BuildSceneList, Parallax.Editor");

        static EditorBuildSettingsScene[] Compute(EditorBuildSettingsScene[] current, IReadOnlyList<string> configScenePaths, string levelsFolder)
        {
            Assert.NotNull(BuildSceneListType, "Parallax.Editor.Setup.BuildSceneList not found.");
            MethodInfo method = BuildSceneListType.GetMethod("Compute", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "BuildSceneList.Compute not found.");
            return (EditorBuildSettingsScene[])method.Invoke(null, new object[] { current, configScenePaths, levelsFolder });
        }

        static EditorBuildSettingsScene Scene(string path, bool enabled) => new EditorBuildSettingsScene(path, enabled);

        // ---------- 5.1 Scene list shape ----------

        [Test]
        public void Compute_PutsLevelSelectFirst_ThenListedLevelsInOrder_ThenOtherScenes_DropsTemplateAndUnlistedLevelScenes()
        {
            var current = new[]
            {
                Scene("Assets/Scenes/SampleScene.unity", true),
                Scene(LevelsFolder + "/_LevelTemplate.unity", true),
                Scene(LevelsFolder + "/Level_999_Unlisted.unity", true),
                Scene("Assets/_Game/Scenes/Sandbox_Realities.unity", true),
                Scene("Assets/_Game/Scenes/Bootstrap.unity", false),
            };
            var configScenePaths = new[] { LevelsFolder + "/Level_001.unity", LevelsFolder + "/Level_002.unity" };

            EditorBuildSettingsScene[] result = Compute(current, configScenePaths, LevelsFolder);

            var paths = result.Select(s => s.path).ToArray();
            CollectionAssert.AreEqual(
                new[]
                {
                    LevelSelectPath,
                    LevelsFolder + "/Level_001.unity",
                    LevelsFolder + "/Level_002.unity",
                    "Assets/Scenes/SampleScene.unity",
                    "Assets/_Game/Scenes/Sandbox_Realities.unity",
                    "Assets/_Game/Scenes/Bootstrap.unity",
                },
                paths);

            Assert.IsTrue(result[0].enabled, "LevelSelect must be enabled");
            Assert.IsTrue(result[1].enabled && result[2].enabled, "listed levels must be enabled");
            Assert.IsFalse(result.Any(s => s.path == LevelsFolder + "/Bootstrap.unity"));
            Assert.IsFalse(result.Any(s => s.path.Contains("_LevelTemplate")), "the template must never be in the list");
            Assert.IsFalse(result.Any(s => s.path.Contains("Level_999_Unlisted")), "an unlisted Scenes/Levels scene must never be in the list");

            EditorBuildSettingsScene bootstrap = result.First(s => s.path == "Assets/_Game/Scenes/Bootstrap.unity");
            Assert.IsFalse(bootstrap.enabled, "an existing scene's enabled state must be kept");
        }

        [Test]
        public void Compute_RemovesDuplicatePaths_WhenLevelSelectOrAListedLevelAlreadyAppearsAmongOtherScenes()
        {
            var current = new[]
            {
                Scene(LevelSelectPath, false), // stray duplicate of the always-first entry, wrong enabled state
                Scene(LevelsFolder + "/Level_001.unity", false), // stray duplicate of a listed level
                Scene("Assets/_Game/Scenes/Sandbox_Realities.unity", true),
            };
            var configScenePaths = new[] { LevelsFolder + "/Level_001.unity" };

            EditorBuildSettingsScene[] result = Compute(current, configScenePaths, LevelsFolder);

            Assert.AreEqual(1, result.Count(s => s.path == LevelSelectPath), "LevelSelect must appear exactly once");
            Assert.AreEqual(1, result.Count(s => s.path == LevelsFolder + "/Level_001.unity"), "a listed level must appear exactly once");
            Assert.IsTrue(result.First(s => s.path == LevelSelectPath).enabled, "the canonical (first) placement wins, not the stray duplicate's state");
            CollectionAssert.AreEqual(
                new[] { LevelSelectPath, LevelsFolder + "/Level_001.unity", "Assets/_Game/Scenes/Sandbox_Realities.unity" },
                result.Select(s => s.path).ToArray());
        }

        // ---------- 5.2 Idempotent ----------

        [Test]
        public void Compute_RunTwice_SecondRunChangesNothing()
        {
            var current = new[]
            {
                Scene("Assets/Scenes/SampleScene.unity", true),
                Scene("Assets/_Game/Scenes/Sandbox_Realities.unity", true),
            };
            var configScenePaths = new[] { LevelsFolder + "/Level_001.unity", LevelsFolder + "/Level_002.unity" };

            EditorBuildSettingsScene[] first = Compute(current, configScenePaths, LevelsFolder);
            EditorBuildSettingsScene[] second = Compute(first, configScenePaths, LevelsFolder);

            CollectionAssert.AreEqual(first.Select(s => s.path), second.Select(s => s.path));
            CollectionAssert.AreEqual(first.Select(s => s.enabled), second.Select(s => s.enabled));
        }

        // ---------- 5.3 Real project: no drift ----------

        [Test]
        public void RealProject_CurrentBuildSettingsOnDisk_IsAFixedPointOfCompute()
        {
            string assetPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "EditorBuildSettings.asset");
            Assert.IsTrue(File.Exists(assetPath), "ProjectSettings/EditorBuildSettings.asset must exist.");
            string text = File.ReadAllText(assetPath);

            var onDisk = new List<(bool enabled, string path)>();
            foreach (Match m in Regex.Matches(text, @"- enabled: (\d)\s*\r?\n\s*path: (.+?)\s*\r?\n\s*guid:", RegexOptions.Singleline))
                onDisk.Add((m.Groups[1].Value == "1", m.Groups[2].Value.Trim()));
            Assert.Greater(onDisk.Count, 0, "could not parse any scene entries from EditorBuildSettings.asset");

            var config = AssetDatabase.LoadAssetAtPath<Parallax.Gameplay.Levels.LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist.");
            var configScenePaths = config.Levels.Select(e => $"{LevelsFolder}/{e.SceneName}.unity").ToArray();

            EditorBuildSettingsScene[] currentAsEntries = onDisk.Select(e => Scene(e.path, e.enabled)).ToArray();
            EditorBuildSettingsScene[] expected = Compute(currentAsEntries, configScenePaths, LevelsFolder);

            CollectionAssert.AreEqual(expected.Select(s => s.path), onDisk.Select(e => e.path),
                "ProjectSettings/EditorBuildSettings.asset has drifted from what BuildSceneList.Sync() would produce; run PARALLAX/Setup/Levels/Sync Build Scene List.");
            CollectionAssert.AreEqual(expected.Select(s => s.enabled), onDisk.Select(e => e.enabled),
                "ProjectSettings/EditorBuildSettings.asset's enabled flags have drifted from what BuildSceneList.Sync() would produce.");
        }

        // ---------- Guards (review fix) ----------

        // Sync() is impure - it reads/writes the real project's LevelListConfig/Build Settings,
        // so it is never invoked here (that would risk mutating real settings from a test run).
        // Instead this checks the typed contract callers (LevelSetup.NewLevel) rely on: Sync
        // reports success/failure rather than assuming it always wrote.
        [Test]
        public void Sync_ReturnsBool_SoCallersCanTellWhetherItActuallyWrote()
        {
            Assert.NotNull(BuildSceneListType, "Parallax.Editor.Setup.BuildSceneList not found.");
            MethodInfo method = BuildSceneListType.GetMethod("Sync", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "BuildSceneList.Sync not found.");
            Assert.AreEqual(typeof(bool), method.ReturnType, "Sync() must report whether it actually wrote Build Settings.");
        }
    }
}
