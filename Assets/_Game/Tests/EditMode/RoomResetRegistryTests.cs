using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    public sealed class RoomResetRegistryTests
    {
        sealed class Resettable : IRoomResettable
        {
            public int RoomId { get; set; }
            public int ResetCount { get; private set; }
            public void ResetToInitial() => ResetCount++;
        }

        [Test]
        public void Reset_OnlyResetsRequestedRoomAndCarriesDeclaredAnchorValue()
        {
            var registry = new RoomResetRegistry();
            var room0 = new Resettable { RoomId = 0 };
            var room1 = new Resettable { RoomId = 1 };
            registry.Register(room0);
            registry.Register(room1);
            registry.RegisterAnchor(0, new AnchorId(10), 0.25f);
            registry.RegisterAnchor(1, new AnchorId(11), 0.75f);
            var resets = new List<AnchorReset>();

            RoomResetResult result = registry.Reset(0, resets);

            Assert.AreEqual(1, room0.ResetCount);
            Assert.AreEqual(0, room1.ResetCount);
            Assert.AreEqual(1, result.TrapsReset);
            Assert.AreEqual(1, result.AnchorsReset);
            Assert.AreEqual(new AnchorId(10), resets[0].AnchorId);
            Assert.AreEqual(0.25f, resets[0].InitialValue);
        }

        [Test]
        public void Registration_RejectsDuplicatesAndUnregisterRemovesItem()
        {
            var registry = new RoomResetRegistry();
            var item = new Resettable { RoomId = 3 };
            Assert.IsTrue(registry.Register(item));
            Assert.IsFalse(registry.Register(item));
            Assert.IsTrue(registry.RegisterAnchor(3, new AnchorId(7), 0f));
            Assert.IsFalse(registry.RegisterAnchor(4, new AnchorId(7), 1f));
            Assert.IsTrue(registry.Unregister(item));
            Assert.IsFalse(registry.Unregister(item));
            Assert.AreEqual(0, registry.Reset(3, new List<AnchorReset>()).TrapsReset);
        }

        [Test]
        public void Reset_EmptyRoomReturnsZeroAndRepeatedResetIsSafe()
        {
            var registry = new RoomResetRegistry();
            Assert.AreEqual(0, registry.Reset(99, new List<AnchorReset>()).TrapsReset);
            Assert.AreEqual(0, registry.Reset(99, new List<AnchorReset>()).AnchorsReset);
            var item = new Resettable { RoomId = 2 };
            registry.Register(item);
            registry.Reset(2, new List<AnchorReset>());
            registry.Reset(2, new List<AnchorReset>());
            Assert.AreEqual(2, item.ResetCount);
        }
    }
}
