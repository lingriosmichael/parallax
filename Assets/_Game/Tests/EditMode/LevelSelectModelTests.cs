using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-053 §5.5: pure level-select model logic (Parallax.Core, no UnityEngine dependency
    // beyond what LevelProgress already needs). Given the level list order and a LevelProgress,
    // it produces one entry per level, in list order, with locked/unlocked state and a
    // best-deaths display string - never re-deriving unlock rules (those stay in LevelProgress,
    // PAX-050/D-063).
    public sealed class LevelSelectModelTests
    {
        static (string Id, string SceneName, string DisplayName)[] Levels(int count)
        {
            var levels = new (string, string, string)[count];
            for (int i = 0; i < count; i++)
            {
                string id = "L" + (i + 1).ToString("000");
                levels[i] = (id, "Level_" + (i + 1).ToString("000"), "Level " + (i + 1));
            }
            return levels;
        }

        [Test]
        public void Build_ProducesEntriesInListOrder_MatchingIdsSceneNamesAndDisplayNames()
        {
            var levels = Levels(3);
            var progress = new LevelProgress(new[] { "L001", "L002", "L003" });

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);

            Assert.AreEqual(3, entries.Length);
            for (int i = 0; i < levels.Length; i++)
            {
                Assert.AreEqual(levels[i].Id, entries[i].Id);
                Assert.AreEqual(levels[i].SceneName, entries[i].SceneName);
                Assert.AreEqual(levels[i].DisplayName, entries[i].DisplayName);
            }
        }

        [Test]
        public void Build_FirstLevelAlwaysUnlocked_RestLockedWithNoSavedProgress()
        {
            var levels = Levels(3);
            var progress = new LevelProgress(new[] { "L001", "L002", "L003" });

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);

            Assert.IsTrue(entries[0].IsUnlocked, "the first level must always be unlocked");
            Assert.IsFalse(entries[1].IsUnlocked);
            Assert.IsFalse(entries[2].IsUnlocked);
        }

        [Test]
        public void Build_NeverCompleted_BestDeathsTextIsEmDash()
        {
            var levels = Levels(1);
            var progress = new LevelProgress(new[] { "L001" });

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);

            Assert.AreEqual("—", entries[0].BestDeathsText);
        }

        [Test]
        public void Build_AfterRecordCompletion_UnlocksNextAndShowsBestDeaths()
        {
            var levels = Levels(2);
            var progress = new LevelProgress(new[] { "L001", "L002" });
            progress.RecordCompletion("L001", 4);

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);

            Assert.AreEqual("4", entries[0].BestDeathsText);
            Assert.IsTrue(entries[1].IsUnlocked, "completing L001 must unlock L002");
        }

        [Test]
        public void Build_FiftyEntryConfig_ProducesFiftyEntriesInOrder()
        {
            var levels = Levels(50);
            var ids = new List<string>();
            foreach (var l in levels) ids.Add(l.Id);
            var progress = new LevelProgress(ids);

            LevelSelectEntry[] entries = LevelSelectModel.Build(levels, progress);

            Assert.AreEqual(50, entries.Length);
            for (int i = 0; i < 50; i++) Assert.AreEqual(levels[i].Id, entries[i].Id);
            Assert.IsTrue(entries[0].IsUnlocked);
            for (int i = 1; i < 50; i++) Assert.IsFalse(entries[i].IsUnlocked, "id " + entries[i].Id);
        }
    }
}
