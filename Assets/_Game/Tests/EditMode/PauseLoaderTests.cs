using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073) §5.4-5.5: every scene load restores the running state, through
    /// LevelSceneLoader's existing CanLoad/LoadScene seam and the RunningState seam; a refused load
    /// leaves it untouched. Restart and Levels on the pause panel load through the loader and never
    /// write level progress.</summary>
    public sealed class PauseLoaderTests : PauseTestBase
    {
        [Test]
        public void Load_Accepted_RestoresRunningStateBeforeLoading()
        {
            bool result = LevelSceneLoader.Load("Level_002");

            Assert.IsTrue(result);
            CollectionAssert.AreEqual(new[] { "timeScale=1", "load Level_002" }, CallOrder,
                "the loader must restore the running state, then load");
        }

        [Test]
        public void Load_Refused_LeavesRunningStateUntouched()
        {
            CanLoadResult = false;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Level_003.*"));

            bool result = LevelSceneLoader.Load("Level_003");

            Assert.IsFalse(result);
            CollectionAssert.IsEmpty(CallOrder, "a refused load must neither restore the running state nor load");
        }

        [Test]
        public void PausePanel_Restart_ReloadsThisLevelThroughTheLoader_AndRecordsNothing()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            var sentinels = new ProgressSentinels();
            try
            {
                rig.Pause.Pause();
                rig.Panel.Restart();

                CollectionAssert.AreEqual(new[] { SceneManager.GetActiveScene().name }, LoadedScenes, "Restart must reload the active scene through LevelSceneLoader");
                Assert.AreEqual(1f, TimeScaleWrites[TimeScaleWrites.Count - 1], "the reload must leave the running state restored");
                sentinels.AssertUntouched("Restart");
            }
            finally
            {
                sentinels.Restore();
                rig.Dispose();
            }
        }

        [Test]
        public void PausePanel_Levels_LoadsLevelSelectThroughTheLoader_AndRecordsNothing()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            var sentinels = new ProgressSentinels();
            try
            {
                rig.Pause.Pause();
                rig.Panel.GoToLevelSelect();

                CollectionAssert.AreEqual(new[] { LevelSceneLoader.LevelSelectSceneName }, LoadedScenes, "Levels must load LevelSelect through LevelSceneLoader");
                Assert.AreEqual(1f, TimeScaleWrites[TimeScaleWrites.Count - 1], "LevelSelect must not start frozen");
                sentinels.AssertUntouched("Levels");
            }
            finally
            {
                sentinels.Restore();
                rig.Dispose();
            }
        }

        /// <summary>Overwrites every listed level's two LevelProgressStore keys with values
        /// LevelProgressStore.Save would never write (it writes UNLOCKED as 0/1 for every id), so
        /// any save during the test shows up; the developer's real values are put back afterwards.</summary>
        sealed class ProgressSentinels
        {
            const string UnlockedPrefix = "PARALLAX_LEVEL_UNLOCKED_";
            const string BestPrefix = "PARALLAX_LEVEL_BEST_";
            const int UnlockedSentinel = 7;
            const int BestSentinel = -4242;

            readonly List<(string key, bool had, int value)> originals = new List<(string, bool, int)>();
            readonly List<string> ids = new List<string>();

            public ProgressSentinels()
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
                Assert.NotNull(config, "LevelListConfig asset must exist.");
                ids.AddRange(config.OrderedIds());
                Assert.IsNotEmpty(ids, "LevelListConfig lists no levels.");
                foreach (string id in ids)
                {
                    Save(UnlockedPrefix + id, UnlockedSentinel);
                    Save(BestPrefix + id, BestSentinel);
                }
            }

            void Save(string key, int sentinel)
            {
                originals.Add((key, PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key, 0)));
                PlayerPrefs.SetInt(key, sentinel);
            }

            public void AssertUntouched(string action)
            {
                foreach (string id in ids)
                {
                    Assert.AreEqual(UnlockedSentinel, PlayerPrefs.GetInt(UnlockedPrefix + id, 0), $"{action} wrote level progress (unlocked, {id})");
                    Assert.AreEqual(BestSentinel, PlayerPrefs.GetInt(BestPrefix + id, 0), $"{action} wrote level progress (best deaths, {id})");
                }
            }

            public void Restore()
            {
                foreach ((string key, bool had, int value) in originals)
                {
                    if (had) PlayerPrefs.SetInt(key, value);
                    else PlayerPrefs.DeleteKey(key);
                }
            }
        }
    }
}
