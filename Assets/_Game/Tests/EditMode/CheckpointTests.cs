using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    public class CheckpointTests
    {
        [Test]
        public void Progress_AdvancesForwardOnly()
        {
            var progress = new CheckpointProgress();
            Assert.AreEqual(0, progress.Current);
            Assert.IsTrue(progress.TryAdvance(1));
            Assert.IsFalse(progress.TryAdvance(1));
            Assert.IsFalse(progress.TryAdvance(0));
            Assert.IsTrue(progress.TryAdvance(3));
            Assert.AreEqual(3, progress.Current);
        }

        [Test]
        public void SpawnTable_ReturnsHighestIdLessOrEqualCurrent()
        {
            var table = new CheckpointSpawnTable();
            table.Set(0, ObserverId.A, new Vector2(0f, 0f), Vector2.down);
            table.Set(2, ObserverId.A, new Vector2(5f, 0f), Vector2.up);

            Assert.IsTrue(table.TryGet(1, ObserverId.A, out Vector2 pos1, out Vector2 grav1));
            Assert.AreEqual(new Vector2(0f, 0f), pos1);
            Assert.AreEqual(Vector2.down, grav1);

            Assert.IsTrue(table.TryGet(2, ObserverId.A, out Vector2 pos2, out Vector2 grav2));
            Assert.AreEqual(new Vector2(5f, 0f), pos2);
            Assert.AreEqual(Vector2.up, grav2);

            Assert.IsTrue(table.TryGet(5, ObserverId.A, out Vector2 pos5, out _));
            Assert.AreEqual(new Vector2(5f, 0f), pos5);
        }

        [Test]
        public void SpawnTable_EntriesArePerObserver()
        {
            var table = new CheckpointSpawnTable();
            table.Set(0, ObserverId.B, new Vector2(1f, 1f), Vector2.down);

            Assert.IsTrue(table.TryGet(2, ObserverId.B, out Vector2 pos, out _));
            Assert.AreEqual(new Vector2(1f, 1f), pos);
        }

        [Test]
        public void SpawnTable_NoEntryForObserver_ReturnsFalse()
        {
            var table = new CheckpointSpawnTable();
            Assert.IsFalse(table.TryGet(0, ObserverId.A, out _, out _));
        }

        [Test]
        public void SpawnTable_RespawnWithNoCheckpointReached_UsesCheckpointZero()
        {
            var spawnObject = new GameObject("Spawn");
            SpawnPoint spawn = spawnObject.AddComponent<SpawnPoint>();
            var table = new CheckpointSpawnTable();
            table.Set(0, ObserverId.A, spawn.Position, spawn.GravityDirection);

            Assert.IsTrue(table.TryGet(new CheckpointProgress().Current, ObserverId.A, out Vector2 position, out Vector2 gravity));
            Assert.AreEqual(spawn.Position, position);
            Assert.AreEqual(spawn.GravityDirection, gravity);
            Object.DestroyImmediate(spawnObject);
        }

        [TestCase(InputSourceKind.LocalHuman, true)]
        [TestCase(InputSourceKind.Inactive, false)]
        [TestCase(InputSourceKind.RemoteHuman, false)]
        [TestCase(InputSourceKind.EchoReplay, false)]
        public void Policy_ActivatesOnlyLocalHuman(InputSourceKind kind, bool expected)
        {
            Assert.AreEqual(expected, CheckpointPolicy.Activates(kind));
        }

        [TestCase(InputSourceKind.LocalHuman, true)]
        [TestCase(InputSourceKind.Inactive, true)]
        [TestCase(InputSourceKind.RemoteHuman, true)]
        [TestCase(InputSourceKind.EchoReplay, false)]
        public void Policy_FallResetsEveryoneExceptEchoReplay(InputSourceKind kind, bool expected)
        {
            Assert.AreEqual(expected, CheckpointPolicy.FallResets(kind));
        }
    }
}
