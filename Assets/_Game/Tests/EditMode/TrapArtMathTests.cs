using NUnit.Framework;
using Parallax.Core.Presentation;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§9.1, §11 R4): the trap presentation's pure functions. Frames, shards and puffs are functions of ticks and
    // seeds only, so the game, the route harness and a checkpoint rewind all draw the same thing.
    public sealed class TrapArtMathTests
    {
        [Test]
        public void Seed_IsStableAndSeparatesNamesAndEvents()
        {
            Assert.AreEqual(TrapArtMath.Seed("Collapse_2", 0), TrapArtMath.Seed("Collapse_2", 0));
            Assert.AreNotEqual(TrapArtMath.Seed("Collapse_2", 0), TrapArtMath.Seed("Collapse_2", 1));
            Assert.AreNotEqual(TrapArtMath.Seed("Collapse_2", 0), TrapArtMath.Seed("Collapse_3", 0));
            // Event 0 is the name's FNV-1a ("a" = 0xE40C292C): pinned, so a platform or runtime change can't move a variant.
            Assert.AreEqual(0xE40C292Cu, TrapArtMath.Seed("a", 0));
        }

        [Test]
        public void Hash01_IsInRangeAndRepeatable()
        {
            uint seed = TrapArtMath.Seed("G_1", 3);
            bool anyDifferent = false;
            for (int i = 0; i < 64; i++)
            {
                float v = TrapArtMath.Hash01(seed, i);
                Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f), $"index {i}");
                Assert.AreEqual(v, TrapArtMath.Hash01(seed, i));
                if (i > 0 && v != TrapArtMath.Hash01(seed, 0)) anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "every index hashed to the same value");
        }

        [Test]
        public void Frame_PlaysEvenlyThenHoldsOrWraps()
        {
            Assert.AreEqual(-1, TrapArtMath.Frame(-1, 25, 5, false));
            Assert.AreEqual(0, TrapArtMath.Frame(0, 25, 5, false));
            Assert.AreEqual(0, TrapArtMath.Frame(4, 25, 5, false));
            Assert.AreEqual(1, TrapArtMath.Frame(5, 25, 5, false));
            Assert.AreEqual(4, TrapArtMath.Frame(24, 25, 5, false));
            Assert.AreEqual(4, TrapArtMath.Frame(100, 25, 5, false), "held on the last frame");
            Assert.AreEqual(0, TrapArtMath.Frame(25, 25, 5, true), "wrapped");
            Assert.AreEqual(2, TrapArtMath.Frame(36, 25, 5, true));
        }

        [TestCase(6)] [TestCase(10)] [TestCase(40)]
        public void ArrowGlint_IsOnExactlyOverTheTell(int tell)
        {
            Assert.IsFalse(TrapArtMath.ArrowGlint(-1, tell), "before the fire");
            for (int s = 0; s < tell; s++) Assert.IsTrue(TrapArtMath.ArrowGlint(s, tell), $"tick {s} of the tell");
            Assert.IsFalse(TrapArtMath.ArrowGlint(tell, tell), "the first flight tick");
        }

        [TestCase(1f, 0.5f, 7u)] [TestCase(3.5f, 0.5f, 11u)] [TestCase(0.6f, 0.4f, 3u)]
        public void ShardRects_TileTheFloorExactly(float width, float height, uint seed)
        {
            var bounds = new Rect(2f, -0.5f, width, height);
            int columns = TrapArtMath.ShardColumns(width), rows = 2;
            Assert.That(columns, Is.GreaterThanOrEqualTo(1));
            float area = 0f;
            for (int i = 0; i < columns * rows; i++)
            {
                Rect r = TrapArtMath.ShardRect(bounds, seed, i, columns, rows);
                Assert.That(r.width, Is.GreaterThan(0f)); Assert.That(r.height, Is.GreaterThan(0f));
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1e-5f)); Assert.That(r.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1e-5f));
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1e-5f)); Assert.That(r.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1e-5f));
                for (int j = 0; j < i; j++)
                {
                    Rect o = TrapArtMath.ShardRect(bounds, seed, j, columns, rows);
                    float ox = Mathf.Min(r.xMax, o.xMax) - Mathf.Max(r.xMin, o.xMin), oy = Mathf.Min(r.yMax, o.yMax) - Mathf.Max(r.yMin, o.yMin);
                    Assert.IsFalse(ox > 1e-5f && oy > 1e-5f, $"shards {j} and {i} overlap");
                }
                area += r.width * r.height;
            }
            Assert.AreEqual(width * height, area, 1e-4f, "the shards cover the floor");
        }

        [Test]
        public void Shard_StartsInPlaceFallsAndIsGoneByTheEnd()
        {
            uint seed = TrapArtMath.Seed("Collapse_2", 0);
            for (int i = 0; i < 6; i++)
            {
                ShardPose start = TrapArtMath.Shard(seed, i, 0);
                Assert.IsTrue(start.Visible);
                Assert.AreEqual(Vector2.zero, start.Offset, $"shard {i} starts in place (the host's own pixels, P10)");
                Assert.AreEqual(0f, start.Rotation);
                Assert.AreEqual(1f, start.Alpha);
                ShardPose mid = TrapArtMath.Shard(seed, i, 20);
                Assert.That(mid.Offset.y, Is.LessThan(-0.2f), $"shard {i} has fallen by tick 20");
                Assert.IsFalse(TrapArtMath.Shard(seed, i, TrapArtMath.CrumbleTicks).Visible, $"shard {i} is gone at the end");
                Assert.AreEqual(mid.Offset, TrapArtMath.Shard(seed, i, 20).Offset, "repeatable");
            }
        }

        [Test]
        public void Puff_GrowsFadesAndEnds()
        {
            uint seed = TrapArtMath.Seed("Arrow_3", 0);
            Assert.IsFalse(TrapArtMath.Puff(seed, 0, -1, 20, 0.5f).Visible, "nothing before the event");
            PuffPose first = TrapArtMath.Puff(seed, 0, 0, 20, 0.5f);
            Assert.IsTrue(first.Visible);
            PuffPose later = TrapArtMath.Puff(seed, 0, 15, 20, 0.5f);
            Assert.That(later.Scale, Is.GreaterThan(first.Scale));
            Assert.That(later.Alpha, Is.LessThan(first.Alpha));
            Assert.That(later.Offset.magnitude, Is.LessThanOrEqualTo(0.5f + 1e-5f));
            Assert.IsFalse(TrapArtMath.Puff(seed, 0, 20, 20, 0.5f).Visible, "gone at the end of its life");
        }
    }
}
