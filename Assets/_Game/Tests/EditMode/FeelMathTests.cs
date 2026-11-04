using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-V06: game feel's curves (FeelMath): presentation only, but deterministic and bounded.</summary>
    public sealed class FeelMathTests
    {
        [Test]
        public void Squash_StartsAtItsAmount_SettlesAtZero_AndReboundsLess()
        {
            Assert.AreEqual(0.2f, FeelMath.Squash(0.2f, 0f, 0.16f), 1e-5f);
            Assert.AreEqual(0f, FeelMath.Squash(0.2f, 0.16f, 0.16f));
            Assert.AreEqual(0f, FeelMath.Squash(0.2f, 1f, 0.16f));
            float lowest = 0f;
            for (float t = 0f; t < 0.16f; t += 0.001f) lowest = Mathf.Min(lowest, FeelMath.Squash(0.2f, t, 0.16f));
            Assert.Less(lowest, 0f, "it rebounds past rest");
            Assert.Greater(lowest, -0.2f * 0.25f, "the rebound is small");
        }

        [Test]
        public void ScaleOf_WidensAndLowers_ForASquash()
        {
            Assert.AreEqual(new Vector2(1.1f, 0.9f), FeelMath.ScaleOf(0.1f));
            Assert.AreEqual(Vector2.one, FeelMath.ScaleOf(0f));
        }

        [Test]
        public void Shake_IsBounded_Deterministic_AndEndsAtRest()
        {
            for (float t = 0f; t < 0.3f; t += 0.01f)
                Assert.LessOrEqual(FeelMath.Shake(0.16f, t, 0.3f, 3, 22f).magnitude, 0.16f * Mathf.Sqrt(2f) + 1e-5f);
            Assert.AreEqual(FeelMath.Shake(0.16f, 0.05f, 0.3f, 3, 22f), FeelMath.Shake(0.16f, 0.05f, 0.3f, 3, 22f));
            Assert.AreEqual(Vector2.zero, FeelMath.Shake(0.16f, 0.3f, 0.3f, 3, 22f));
            Assert.AreEqual(Vector2.zero, FeelMath.Shake(0f, 0.05f, 0.3f, 3, 22f));
        }

        [Test]
        public void Falloff_IsFullBesideTheCat_AndNoneFarAway()
        {
            Assert.AreEqual(1f, FeelMath.Falloff(0f, 9f));
            Assert.AreEqual(0.5f, FeelMath.Falloff(4.5f, 9f), 1e-5f);
            Assert.AreEqual(0f, FeelMath.Falloff(12f, 9f));
        }

        [Test]
        public void Impact_IsASuddenStop_OfAFastMover_Only()
        {
            Assert.IsTrue(FeelMath.Impact(12f, 0f, 6f), "a falling block landing");
            Assert.IsFalse(FeelMath.Impact(3f, 0f, 6f), "a slow platform stopping");
            Assert.IsFalse(FeelMath.Impact(12f, 8f, 6f), "slowing, not stopping");
        }
    }
}
