using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-088 (D-090) §5: the storm cloud as pure functions: follow step and clamp, the phases from the wake (ruling B),
    // the strike's bottom over a height profile (ruling A), the strict box test and the gravity-aware cat box (ruling C).
    public sealed class StormCloudMathTests
    {
        const float Speed = .08f, Hw = .4f;
        const int Delay = 50, Period = 100, Tell = 25, Strike = 6;

        static StormCloudPhase P(int s) => StormCloudMath.PhaseSince(s, Delay, Period, Tell, Strike);

        // ---------- follow ----------

        [Test]
        public void Follow_StepsTowardTheCat_AtMostTheSpeed_AndLandsOnItWhenCloser()
        {
            Assert.AreEqual(.08f, StormCloudMath.Follow(0f, 1f, Speed, -5f, 5f), 1e-6f);
            Assert.AreEqual(-.08f, StormCloudMath.Follow(0f, -1f, Speed, -5f, 5f), 1e-6f);
            Assert.AreEqual(.05f, StormCloudMath.Follow(0f, .05f, Speed, -5f, 5f), 1e-6f);
            Assert.AreEqual(2f, StormCloudMath.Follow(2f, 2f, Speed, -5f, 5f), 1e-6f);
        }

        [Test]
        public void Follow_IsClampedToTheRange()
        {
            Assert.AreEqual(5f, StormCloudMath.Follow(4.95f, 10f, Speed, -5f, 5f), 1e-6f);
            Assert.AreEqual(-5f, StormCloudMath.Follow(-4.95f, -10f, Speed, -5f, 5f), 1e-6f);
            Assert.AreEqual(5f, StormCloudMath.Follow(5f, 10f, Speed, -5f, 5f), 1e-6f);
        }

        // ---------- the cycle (ruling B): wake W, follow from W+1, first charge at W+50, strike at W+75 ----------

        [Test]
        public void Phase_IsDormant_BeforeTheWake_AndOnTheWakeTick()
        {
            Assert.AreEqual(StormCloudPhase.Dormant, P(-1));
            Assert.AreEqual(StormCloudPhase.Dormant, P(0));
        }

        [Test]
        public void Phase_Follows_FromWakePlusOne_UntilTheFirstCharge()
        {
            for (int s = 1; s < Delay; s++) Assert.AreEqual(StormCloudPhase.Follow, P(s), $"s {s}");
            Assert.AreEqual(StormCloudPhase.Charge, P(Delay));
        }

        [Test]
        public void Phase_ChargeThenStrikeThenFollow_EveryPeriod_CountedFromTheFirstCharge()
        {
            for (int cycle = 0; cycle < 3; cycle++)
            {
                int c = Delay + cycle * Period;
                for (int s = c; s < c + Tell; s++) Assert.AreEqual(StormCloudPhase.Charge, P(s), $"s {s}");
                for (int s = c + Tell; s < c + Tell + Strike; s++) Assert.AreEqual(StormCloudPhase.Strike, P(s), $"s {s}");
                for (int s = c + Tell + Strike; s < c + Period; s++) Assert.AreEqual(StormCloudPhase.Follow, P(s), $"s {s}");
            }
            Assert.AreEqual(StormCloudPhase.Strike, P(75));
            Assert.AreEqual(StormCloudPhase.Strike, P(80));
            Assert.AreEqual(StormCloudPhase.Follow, P(81));
            Assert.AreEqual(StormCloudPhase.Charge, P(150));
        }

        // ---------- the height profile (ruling A) ----------

        static readonly Vector3[] Floor = { new(-10f, 10f, 0f) };
        static readonly Vector3[] Pit = { new(-10f, -1f, 0f), new(1f, 10f, 0f), new(-1f, 1f, -3f) };
        static readonly Vector3[] Overhang = { new(-10f, 10f, 0f), new(2f, 4f, 1.9f) };

        static float Bottom(Vector3[] profile, float x, float cloudBottom = 5f, float halfWidth = Hw)
        {
            Assert.IsTrue(StormCloudMath.StrikeBottom(profile, x, halfWidth, cloudBottom, out float bottom), $"no top under x {x}");
            return bottom;
        }

        [Test] public void StrikeBottom_OverAFloor_IsTheFloorsTop() => Assert.AreEqual(0f, Bottom(Floor, 0f));

        [Test]
        public void StrikeBottom_OverAPit_IsThePitBottom_UnlessTheColumnTouchesAFloorEdge()
        {
            Assert.AreEqual(-3f, Bottom(Pit, 0f));
            Assert.AreEqual(0f, Bottom(Pit, .7f), "column x [0.3, 1.1] overlaps the floor from x 1: the highest top wins");
            // Exactly representable edges (half width 0.5): column x [0, 1] only touches the floor's edge at x 1.
            Assert.AreEqual(-3f, Bottom(Pit, .5f, halfWidth: .5f), "touching the floor's edge is not an overlap");
        }

        [Test]
        public void StrikeBottom_UnderAnOverhang_StopsOnIt_IfAnyPartOfTheWidthIsOver()
        {
            Assert.AreEqual(1.9f, Bottom(Overhang, 3f));
            Assert.AreEqual(1.9f, Bottom(Overhang, 1.7f), "column x [1.3, 2.1] overlaps the overhang's edge");
            Assert.AreEqual(0f, Bottom(Overhang, 1.5f, halfWidth: .5f), "column x [1, 2] only touches it");
        }

        [Test]
        public void StrikeBottom_IgnoresTopsAboveTheCloudsBottom_AndReportsNone()
        {
            Vector3[] ceilingOnly = { new(-10f, 10f, 8f) };
            Assert.IsFalse(StormCloudMath.StrikeBottom(ceilingOnly, 0f, Hw, 5f, out _));
            Vector3[] withCeiling = { new(-10f, 10f, 8f), new(-10f, 10f, 0f) };
            Assert.AreEqual(0f, Bottom(withCeiling, 0f));
            Assert.AreEqual(5f, Bottom(new[] { new Vector3(-10f, 10f, 5f) }, 0f), "a top exactly at the cloud's bottom counts");
        }

        [Test]
        public void Column_SpansTheWidth_FromTheBottomToTheCloud()
        {
            Rect c = StormCloudMath.Column(3f, Hw, 1.9f, 5f);
            Assert.AreEqual(2.6f, c.xMin, 1e-5f); Assert.AreEqual(3.4f, c.xMax, 1e-5f);
            Assert.AreEqual(1.9f, c.yMin, 1e-5f); Assert.AreEqual(5f, c.yMax, 1e-5f);
        }

        // ---------- the kill test (ruling C) ----------

        [Test]
        public void Hits_IsAStrictOverlap_TouchingIsSafe()
        {
            Rect column = StormCloudMath.Column(0f, Hw, 0f, 5f);
            Assert.IsTrue(StormCloudMath.Hits(column, new Rect(.3f, 0f, 1f, .56f)));
            Assert.IsFalse(StormCloudMath.Hits(column, new Rect(.4f, 0f, 1f, .56f)), "edge to edge");
            Assert.IsFalse(StormCloudMath.Hits(column, new Rect(-2f, 5f, 4f, .56f)), "resting on the cloud's bottom");
            Assert.IsFalse(StormCloudMath.Hits(column, new Rect(-2f, -.56f, 4f, .56f)), "under the column's bottom");
        }

        // Ruling C: the same body position, gravity down (rotation 0) and gravity up (rotation 180, D-052). The collider offset
        // (0, -0.12) turns with the body, so the up cat's box is 0.24 higher: y [-0.16, 0.40] against down's [-0.40, 0.16]. A
        // column ending at y -0.3 (the cloud's bottom) reaches the down cat and misses the up cat.
        [Test]
        public void CatBox_TurnsTheOffsetWithTheBody_SoTheSameStrikeHitsTheDownCatAndMissesTheUpCat()
        {
            Vector2 body = Vector2.zero, offset = new(0f, -.12f), size = new(1f, .56f);
            Rect down = StormCloudMath.CatBox(body, 0f, offset, size);
            Rect up = StormCloudMath.CatBox(body, 180f, offset, size);
            Assert.AreEqual(-.40f, down.yMin, 1e-4f); Assert.AreEqual(.16f, down.yMax, 1e-4f);
            Assert.AreEqual(-.16f, up.yMin, 1e-4f); Assert.AreEqual(.40f, up.yMax, 1e-4f);
            Assert.AreEqual(-.5f, up.xMin, 1e-4f); Assert.AreEqual(.5f, up.xMax, 1e-4f);
            Rect column = StormCloudMath.Column(0f, Hw, -5f, -.3f);
            Assert.IsTrue(StormCloudMath.Hits(column, down), "gravity down: the box reaches below -0.3");
            Assert.IsFalse(StormCloudMath.Hits(column, up), "gravity up: the box is wholly above the cloud's bottom");
        }

        [Test] public void ClearDistance_IsHalfTheStrikePlusHalfTheCollider() => Assert.AreEqual(.9f, StormCloudMath.ClearDistance(.8f, 1f), 1e-6f);
    }
}
