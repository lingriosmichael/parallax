using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-074 (D-078) R3/R4: the arrow as a pure function of ticks since its fire, and the
    /// tunnelling cap. The cap tests use real Physics2D queries against a real 1 x 0.56 horizontal
    /// capsule, far from anything in the open scene.</summary>
    public sealed class ArrowMathTests
    {
        const float V = .3f, L = .8f, H = .16f;
        const int T = 6;
        static readonly Vector2 Far = new Vector2(10000f, 10000f);

        [TearDown] public void TearDown() => TickTime.ResetToDefault();

        // ---------- R3: pose, tell, flight, stop ----------

        [TestCase(ArrowDirection.Right, 8f, 14f)]
        [TestCase(ArrowDirection.Left, 22f, 14.5f)]
        public void Pose_PerTick_FollowsTheLaneInBothDirections_AndStopsAtTheEndFace(ArrowDirection direction, float mouth, float end)
        {
            float travel = ArrowMath.Travel(mouth, end, L);
            Assert.AreEqual(Mathf.Abs(end - mouth) - L, travel, 1e-5f, "travel = lane length - arrow length");
            int n = ArrowMath.FlightTicks(travel, V);
            float sign = direction == ArrowDirection.Right ? 1f : -1f;
            for (int s = 0; s <= T + n + 5; s++)
            {
                float expected = mouth + sign * (Mathf.Min(Mathf.Max(0, s - T) * V, travel) + L * .5f);
                Assert.AreEqual(expected, ArrowMath.CentreX(mouth, direction, L, ArrowMath.Offset(s, T, V, travel)), 1e-4f, $"centre x at s {s}");
            }
            float stopped = ArrowMath.CentreX(mouth, direction, L, ArrowMath.Offset(T + n + 3, T, V, travel));
            Assert.AreEqual(end - sign * L * .5f, stopped, 1e-4f, "a stopped arrow's front face is the lane end");
        }

        [Test]
        public void Tell_IsHarmlessForExactlyTellTicks_AndTheFirstLethalPoseIsTheTellPose()
        {
            const int n = 18;
            for (int s = 0; s < T; s++)
            {
                Assert.IsTrue(ArrowMath.IsTell(s, T), $"s {s} is tell");
                Assert.IsFalse(ArrowMath.IsLethal(s, T, n), $"s {s} must be harmless");
            }
            Assert.IsFalse(ArrowMath.IsTell(T, T), "the tell ends after T ticks");
            Assert.IsTrue(ArrowMath.IsLethal(T, T, n), "lethal on s = T");
            Assert.AreEqual(ArrowMath.Offset(0, T, V, 5.2f), ArrowMath.Offset(T, T, V, 5.2f), "the first lethal pose is the tell pose");
        }

        [Test]
        public void Flight_IsLethalThroughTPlusN_AndStoppedHarmlessFromTPlusNPlusOne()
        {
            float travel = ArrowMath.Travel(8f, 14f, L);
            int n = ArrowMath.FlightTicks(travel, V);
            Assert.AreEqual(18, n, "ceil(5.2 / 0.3) = 18");
            for (int s = T; s <= T + n; s++)
            {
                Assert.IsTrue(ArrowMath.IsLethal(s, T, n), $"s {s} lethal");
                Assert.IsFalse(ArrowMath.IsStopped(s, T, n), $"s {s} not stopped");
            }
            Assert.AreEqual(travel, ArrowMath.Offset(T + n, T, V, travel), 1e-5f, "the arrival tick is at the lane end");
            for (int s = T + n + 1; s <= T + n + 40; s++)
            {
                Assert.IsFalse(ArrowMath.IsLethal(s, T, n), $"s {s} harmless");
                Assert.IsTrue(ArrowMath.IsStopped(s, T, n), $"s {s} stopped");
            }
        }

        [TestCase(6f, .3f, 20)]
        [TestCase(3f, .3f, 10)]
        [TestCase(4.8f, .3f, 16)]
        [TestCase(6f, .25f, 24)]
        public void FlightTicks_WholeNumberTravelOverSpeed_IsStable(float travel, float v, int expected)
        {
            Assert.AreEqual(expected, ArrowMath.FlightTicks(travel, v));
            // The same travel reached through the lane subtraction, with its float noise.
            Assert.AreEqual(expected, ArrowMath.FlightTicks(ArrowMath.Travel(1.3f, 1.3f + travel + L, L), v));
        }

        // ---------- R3: fire tick per D-055 ----------

        [Test]
        public void FireTick_Overlap_IsTheTriggerTickPlusDelay_AndStartsTheTell()
        {
            var timing = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 4, 0, 1, 0);
            for (int tick = 0; tick <= 30; tick++) timing.Step(tick, overlapping: tick >= 10);
            Assert.AreEqual(14, timing.LatestFireTick, "overlap at 10 + delay 4");
            Assert.IsTrue(ArrowMath.IsTell(14 - timing.LatestFireTick, T), "the fire tick is s = 0, the first tell tick");
            Assert.IsTrue(ArrowMath.IsLethal(20 - timing.LatestFireTick, T, 18), "lethal from fire + T");
        }

        [Test]
        public void FireTick_Chain_IsTheSourceFirePlusDelay_AndStartsTheTell()
        {
            var source = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Once, 0, 0, 1, 0);
            var target = new TrapTiming(TrapTriggerSource.Chain, TrapRepeatMode.Once, 3, 0, 1, 0);
            for (int tick = 0; tick <= 30; tick++)
            {
                source.Step(tick, overlapping: tick >= 10);
                target.Step(tick, false, source.LatestFireTick);
            }
            Assert.AreEqual(13, target.LatestFireTick, "source fire 10 + chain delay 3");
            Assert.IsTrue(ArrowMath.IsTell(13 - target.LatestFireTick, T));
        }

        [Test]
        public void FireTick_Periodic_IsRoomStartPlusPhase_ThenEveryPeriod()
        {
            var timing = new TrapTiming(TrapTriggerSource.Overlap, TrapRepeatMode.Periodic, 0, 25, 120, 30);
            var fires = new List<int>();
            for (int tick = 0; tick <= 300; tick++) if (timing.Step(tick, false)) fires.Add(tick);
            CollectionAssert.AreEqual(new[] { 30, 150, 270 }, fires);
            foreach (int fire in fires) Assert.IsTrue(ArrowMath.IsTell(0, T) && ArrowMath.IsLethal(fire + T - fire, T, 18), $"fire at {fire} starts a tell, lethal at {fire + T}");
        }

        // ---------- R4: tunnelling ----------

        [Test]
        public void Cap_IsOneUnitPerTick_WithTheRealConfigAndTickRate()
        {
            CatMotorConfig config = RealConfig();
            float cap = ArrowMath.MaxUnitsPerTick(L, config.ColliderSize, config.MaxSpeed * TickTime.SecondsPerTick);
            Assert.AreEqual(1f, cap, 1e-4f, "0.8 + (1.0 - 0.56) - 2 x 0.12");
        }

        // The band grazing the capsule's top or bottom sees only the straight section, 1.0 - 0.56 wide:
        // moving the band from the centre line towards the graze, the capsule's width inside it only
        // shrinks, and never below the straight section. Physics2D adds its contact skin (~0.01 u),
        // which only widens what a query sees, so the real queries are never narrower than the model.
        [Test]
        public void TheBandAtTheCapsulesStraightSection_IsTheWorstCase()
        {
            GameObject cat = Capsule();
            try
            {
                var widths = new List<(float y, float width)>();
                for (float y = 0f; y < 1f; y += .005f)
                {
                    float width = WidthInBand(y);
                    if (width <= 0f) break;
                    widths.Add((y, width));
                }
                Assert.Greater(widths.Count, 10, "the band should overlap the capsule over many heights");
                for (int i = 1; i < widths.Count; i++)
                    Assert.LessOrEqual(widths[i].width, widths[i - 1].width + .004f, $"width grows again at band y {widths[i].y:F3}");
                float graze = widths[widths.Count - 1].width;
                Assert.GreaterOrEqual(graze, 1f - .56f - 1e-3f, "never narrower than the straight section");
                Assert.AreEqual(widths.Min(w => w.width), graze, 1e-3f, "the graze is the narrowest");
                Assert.AreEqual(1f, widths[0].width, .03f, "a band through the centre line sees the full 1.0 (plus skin)");
            }
            finally { Object.DestroyImmediate(cat); }

            float WidthInBand(float bandY)
            {
                float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
                for (float x = -.7f; x <= .7f; x += .002f)
                    if (Hits(new Vector2(x, bandY), new Vector2(.002f, H))) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
                return float.IsInfinity(minX) ? 0f : maxX - minX;
            }
        }

        [Test]
        public void AtTheCap_AnArrowAndACatRunningTowardsIt_CannotPassWithoutAnOverlapTick()
        {
            CatMotorConfig config = RealConfig();
            float run = config.MaxSpeed * TickTime.SecondsPerTick;
            float cap = ArrowMath.MaxUnitsPerTick(L, config.ColliderSize, run);
            Assert.Greater(cap, 0f, "the cap must be positive");
            GameObject cat = Capsule();
            try
            {
                // Band at the centre line, half way, and grazing the top by 0.001 u (the worst case).
                foreach (float bandY in new[] { 0f, .2f, .28f + H * .5f - .001f })
                    for (int phase = 0; phase < 100; phase++)
                        Assert.IsTrue(MeetsOnSomeTick(cat, cap, run, bandY, phase / 100f), $"passed through at v {cap:F3}, band y {bandY:F3}, phase {phase}");
            }
            finally { Object.DestroyImmediate(cat); }
        }

        // Sanity check that the sampling model can miss at all: well above the cap a grazing band is
        // skipped. 1.5 u/tick: 1.5 + 0.12 exceeds 0.8 + the ~0.6 u a grazing band sees through the skin.
        [Test]
        public void WellAboveTheCap_AGrazingBandCanBeSkipped()
        {
            CatMotorConfig config = RealConfig();
            float run = config.MaxSpeed * TickTime.SecondsPerTick;
            GameObject cat = Capsule();
            try
            {
                bool skipped = false;
                for (int phase = 0; phase < 100 && !skipped; phase++) skipped = !MeetsOnSomeTick(cat, 1.5f, run, .28f + H * .5f - .001f, phase / 100f);
                Assert.IsTrue(skipped, "at 1.5 u/tick some phase should skip a grazing band");
            }
            finally { Object.DestroyImmediate(cat); }
        }

        // The arrow flies right from far left of the cat; the cat runs left. Each tick pairs the
        // arrow's pose at k with the cat's pose at k - 1 (RoomManager's order). phase shifts the
        // arrow's start by a fraction of one relative step.
        static bool MeetsOnSomeTick(GameObject cat, float v, float run, float bandY, float phase)
        {
            float mouth = -6f + phase * (v + run);
            for (int k = 0; k < 200; k++)
            {
                float catX = -run * Mathf.Max(0, k - 1);
                cat.transform.position = Far + new Vector2(catX, 0f);
                Physics2D.SyncTransforms();
                float arrowX = ArrowMath.CentreX(mouth, ArrowDirection.Right, L, ArrowMath.Offset(T + k, T, v, 1000f));
                if (arrowX - L * .5f > catX + 1f) return false;
                if (Hits(new Vector2(arrowX, bandY), new Vector2(L, H))) return true;
            }
            return false;
        }

        // Positions are relative to Far, where the capsule lives.
        static bool Hits(Vector2 localCentre, Vector2 size) =>
            Physics2D.OverlapBoxAll(Far + localCentre, size, 0f).Any(h => h.name == "ArrowMathTests_Cat");

        static GameObject Capsule()
        {
            var cat = new GameObject("ArrowMathTests_Cat");
            cat.transform.position = Far;
            var capsule = cat.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Horizontal;
            capsule.size = new Vector2(1f, .56f);
            Physics2D.SyncTransforms();
            return cat;
        }

        static CatMotorConfig RealConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(config, "CatMotorConfig_Default.asset not found");
            return config;
        }
    }
}
