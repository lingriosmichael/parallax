using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-050 (D-063) §5 tests 1-4. Pure Core logic, no Unity dependency.</summary>
    public sealed class LevelProgressTests
    {
        static readonly string[] ThreeLevels = { "L1", "L2", "L3" };

        [Test]
        public void FirstLevel_IsUnlocked_WithNoSaveData()
        {
            var progress = new LevelProgress(ThreeLevels);

            Assert.IsTrue(progress.IsUnlocked("L1"));
            Assert.IsFalse(progress.IsUnlocked("L2"));
            Assert.IsFalse(progress.IsUnlocked("L3"));
        }

        [Test]
        public void LaterLevel_UnlocksOnlyAfterThePreviousOneCompletes()
        {
            var progress = new LevelProgress(ThreeLevels);
            Assert.IsFalse(progress.IsUnlocked("L2"));

            progress.RecordCompletion("L1", deaths: 3);

            Assert.IsTrue(progress.IsUnlocked("L2"));
            Assert.IsFalse(progress.IsUnlocked("L3"));
        }

        [Test]
        public void RecordCompletion_KeepsMinimumDeathCount_RegardlessOfOrder()
        {
            var worseFirst = new LevelProgress(ThreeLevels);
            worseFirst.RecordCompletion("L1", deaths: 5);
            worseFirst.RecordCompletion("L1", deaths: 2);
            Assert.AreEqual(2, worseFirst.BestDeaths("L1"));

            var betterFirst = new LevelProgress(ThreeLevels);
            betterFirst.RecordCompletion("L1", deaths: 2);
            betterFirst.RecordCompletion("L1", deaths: 5);
            Assert.AreEqual(2, betterFirst.BestDeaths("L1"));
        }

        [Test]
        public void RecordCompletion_UnlocksExactlyTheNextId_AndNoOther()
        {
            var progress = new LevelProgress(ThreeLevels);

            progress.RecordCompletion("L1", deaths: 0);

            Assert.IsTrue(progress.IsUnlocked("L2"));
            Assert.IsFalse(progress.IsUnlocked("L3"));

            progress.RecordCompletion("L2", deaths: 0);

            Assert.IsTrue(progress.IsUnlocked("L3"));
        }

        [Test]
        public void BestDeaths_ForANeverCompletedLevel_ReturnsNeverCompletedSentinel()
        {
            var progress = new LevelProgress(ThreeLevels);

            Assert.AreEqual(LevelProgress.NeverCompleted, progress.BestDeaths("L2"));
        }

        [Test]
        public void SavedData_SeedsUnlockedAndBestDeaths()
        {
            var savedUnlocked = new List<string> { "L1", "L2" };
            var savedBest = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("L1", 4) };

            var progress = new LevelProgress(ThreeLevels, savedUnlocked, savedBest);

            Assert.IsTrue(progress.IsUnlocked("L2"));
            Assert.IsFalse(progress.IsUnlocked("L3"));
            Assert.AreEqual(4, progress.BestDeaths("L1"));
        }
    }
}
