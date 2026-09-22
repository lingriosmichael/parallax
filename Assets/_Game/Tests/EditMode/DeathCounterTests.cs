using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public sealed class DeathCounterTests
    {
        [Test]
        public void Record_CountsOncePerKillPerRoom()
        {
            var counter = new DeathCounter();
            Assert.AreEqual(0, counter.DeathsIn(0));

            counter.Record(0);
            Assert.AreEqual(1, counter.DeathsIn(0));

            counter.Record(0);
            Assert.AreEqual(2, counter.DeathsIn(0));
        }

        [Test]
        public void Record_MultipleRoomsAreIndependent()
        {
            var counter = new DeathCounter();
            counter.Record(0);
            counter.Record(1);
            counter.Record(1);

            Assert.AreEqual(1, counter.DeathsIn(0));
            Assert.AreEqual(2, counter.DeathsIn(1));
            Assert.AreEqual(0, counter.DeathsIn(2));
        }

        [Test]
        public void DeathsIn_SurvivesRoomReset()
        {
            var counter = new DeathCounter();
            counter.Record(0);
            counter.Record(0);

            // DeathCounter is deliberately never IRoomResettable, so a room reset — which only
            // ever touches items registered with RoomResetRegistry — structurally cannot
            // reach it.
            var registry = new RoomResetRegistry();
            registry.Reset(0, new List<AnchorReset>());

            Assert.AreEqual(2, counter.DeathsIn(0));
        }
    }
}
