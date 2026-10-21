using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    // PAX-108 (D-157): the level the title's PLAY button loads.
    public sealed class TitleContinueTests
    {
        static readonly string[] Ids = { "L001", "L002", "L003" };

        [Test]
        public void FreshInstall_PlaysLevelOne_LabelledPlay()
        {
            var progress = new LevelProgress(Ids);
            Assert.AreEqual("L001", TitleContinue.Target(Ids, progress));
            Assert.IsFalse(TitleContinue.HasProgress(Ids, progress));
        }

        [Test]
        public void AfterCompletingLevels_PlaysTheFirstUnlockedLevelNotYetCompleted()
        {
            var progress = new LevelProgress(Ids);
            progress.RecordCompletion("L001", 12);
            Assert.AreEqual("L002", TitleContinue.Target(Ids, progress));
            Assert.IsTrue(TitleContinue.HasProgress(Ids, progress));
        }

        [Test]
        public void WithEveryLevelCompleted_PlaysTheLastOne()
        {
            var progress = new LevelProgress(Ids);
            foreach (string id in Ids) progress.RecordCompletion(id, 3);
            Assert.AreEqual("L003", TitleContinue.Target(Ids, progress));
        }

        [Test]
        public void ALockedLevel_IsNeverTheTarget()
        {
            // L002 completed from an old save without L003 unlocked: the target is the first unlocked, uncompleted one.
            var progress = new LevelProgress(Ids, new[] { "L001", "L002" }, new[] { new KeyValuePair<string, int>("L001", 1), new KeyValuePair<string, int>("L002", 1) });
            Assert.AreEqual("L002", TitleContinue.Target(Ids, progress), "every unlocked level is completed, so the last unlocked one");
        }

        [Test]
        public void NoLevels_NoTarget()
        {
            var none = new string[0];
            Assert.IsNull(TitleContinue.Target(none, new LevelProgress(none)));
        }
    }
}
