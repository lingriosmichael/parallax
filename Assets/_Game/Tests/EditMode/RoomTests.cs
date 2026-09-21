using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public class RoomTests
    {
        [Test]
        public void Policy_LocalHumanCompletes()
        {
            Assert.IsTrue(RoomPolicy.CompletesRoom(InputSourceKind.LocalHuman));
        }

        [TestCase(InputSourceKind.Inactive)]
        [TestCase(InputSourceKind.RemoteHuman)]
        [TestCase(InputSourceKind.EchoReplay)]
        public void Policy_OthersDoNotComplete(InputSourceKind kind)
        {
            Assert.IsFalse(RoomPolicy.CompletesRoom(kind));
        }

        [Test]
        public void Progress_StartsEmpty()
        {
            var progress = new RoomProgress();
            Assert.AreEqual(-1, progress.LastCompleted);
            Assert.IsFalse(progress.LevelComplete);
        }

        [Test]
        public void Progress_SameRoomCompletesOnce()
        {
            var progress = new RoomProgress();
            Assert.IsTrue(progress.TryComplete(0));
            Assert.IsFalse(progress.TryComplete(0));
        }

        [Test]
        public void Progress_NextRoomCompletes()
        {
            var progress = new RoomProgress();
            progress.TryComplete(0);
            Assert.IsTrue(progress.TryComplete(1));
        }

        [Test]
        public void Progress_NothingCompletesAfterLevelComplete()
        {
            var progress = new RoomProgress();
            progress.MarkLevelComplete();
            Assert.IsFalse(progress.TryComplete(5));
        }

        [Test]
        public void HasNext_TrueWhenNextIdExists()
        {
            Assert.IsTrue(RoomSequence.HasNext(0, new[] { 0, 1 }));
        }

        [Test]
        public void HasNext_FalseAtLastRoom()
        {
            Assert.IsFalse(RoomSequence.HasNext(1, new[] { 0, 1 }));
        }

        [Test]
        public void HasNext_FalseForEmptySet()
        {
            Assert.IsFalse(RoomSequence.HasNext(0, new int[0]));
        }
    }
}
