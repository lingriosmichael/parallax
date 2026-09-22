using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-050 (D-063) §5 tests 7-8. Uses unique per-test level ids so PlayerPrefs
    /// writes from one test can never leak into another, and cleans up the keys it wrote.</summary>
    public sealed class LevelProgressStoreTests
    {
        static string[] Ids(string prefix) => new[] { prefix + "_A", prefix + "_B", prefix + "_C" };

        static void CleanUp(string[] ids)
        {
            foreach (string id in ids)
            {
                PlayerPrefs.DeleteKey("PARALLAX_LEVEL_UNLOCKED_" + id);
                PlayerPrefs.DeleteKey("PARALLAX_LEVEL_BEST_" + id);
            }
        }

        [Test]
        public void SaveThenLoad_RoundTripsUnlockAndBestDeaths()
        {
            string[] ids = Ids(nameof(SaveThenLoad_RoundTripsUnlockAndBestDeaths));
            try
            {
                var progress = new LevelProgress(ids);
                progress.RecordCompletion(ids[0], deaths: 7);
                progress.RecordCompletion(ids[0], deaths: 3); // best should be 3
                LevelProgressStore.Save(progress, ids);

                LevelProgress loaded = LevelProgressStore.Load(ids);

                Assert.IsTrue(loaded.IsUnlocked(ids[0]));
                Assert.IsTrue(loaded.IsUnlocked(ids[1]), "completing the first level must unlock the second");
                Assert.IsFalse(loaded.IsUnlocked(ids[2]));
                Assert.AreEqual(3, loaded.BestDeaths(ids[0]));
                Assert.AreEqual(LevelProgress.NeverCompleted, loaded.BestDeaths(ids[1]));
            }
            finally { CleanUp(ids); }
        }

        [Test]
        public void Load_WithNoSavedData_ReturnsEmptyFirstLevelUnlockedState_WithoutThrowing()
        {
            string[] ids = Ids(nameof(Load_WithNoSavedData_ReturnsEmptyFirstLevelUnlockedState_WithoutThrowing));
            CleanUp(ids); // ensure a clean slate, then don't save anything
            try
            {
                LevelProgress loaded = null;
                Assert.DoesNotThrow(() => loaded = LevelProgressStore.Load(ids));

                Assert.IsTrue(loaded.IsUnlocked(ids[0]));
                Assert.IsFalse(loaded.IsUnlocked(ids[1]));
                Assert.AreEqual(LevelProgress.NeverCompleted, loaded.BestDeaths(ids[0]));
            }
            finally { CleanUp(ids); }
        }

        [Test]
        public void Load_WithCorruptBestDeathsValue_FallsBackWithoutThrowing()
        {
            string[] ids = Ids(nameof(Load_WithCorruptBestDeathsValue_FallsBackWithoutThrowing));
            try
            {
                // A negative "best deaths" is not producible by Save; simulate corrupt/foreign
                // data written under the same key by something else.
                PlayerPrefs.SetInt("PARALLAX_LEVEL_BEST_" + ids[0], -999);

                LevelProgress loaded = null;
                Assert.DoesNotThrow(() => loaded = LevelProgressStore.Load(ids));

                Assert.AreEqual(LevelProgress.NeverCompleted, loaded.BestDeaths(ids[0]),
                    "an out-of-range stored value must not be surfaced as a real best-deaths count");
            }
            finally { CleanUp(ids); }
        }
    }
}
