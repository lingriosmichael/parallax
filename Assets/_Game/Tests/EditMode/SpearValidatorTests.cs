using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using static Parallax.Tests.EditMode.RouteTestApi;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-084 (D-086) §5, R1, R5: ValidateSpear's clauses, ValidateBand's spear clause, the spear defaults and the speed
    // cap at the committed collider.
    public sealed class SpearValidatorTests
    {
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.SpearFixtures, Parallax.Editor");
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        static object Room(string fixture)
        {
            Assert.NotNull(Fixtures, "SpearFixtures not found");
            return Invoke(Fixtures, fixture);
        }

        static PlatformSizeConfig Sizes() => AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
        static List<string> Spear(string fixture) => Rule("ValidateSpear", "FIX", Room(fixture), Sizes());

        static void AssertRejected(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            foreach (string fragment in fragments) StringAssert.Contains(fragment, errors[0]);
        }

        [Test]
        public void AWellFormedSpear_PassesEveryArrowRule_AndValidateSpear()
        {
            var errors = new List<string>(Spear("AllRulesPass"));
            foreach (string rule in new[] { "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" })
                errors.AddRange(Rule(rule, "FIX", Room("AllRulesPass")));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        [TestCase("Rearm")]
        [TestCase("Periodic")]
        public void ASpearThatRefires_IsRejected(string fixture) => AssertRejected(Spear(fixture), "Spear_A", fixture, "Once");

        [Test] public void AShaftShorterThanTheMinimumWidth_IsRejected() => AssertRejected(Spear("ShortShaft"), "Spear_A", "0.80", "1.00");

        [Test] public void AShaftThinnerThanTheSpearMinimum_IsRejected() => AssertRejected(Spear("ThinShaft"), "Spear_A", "0.30", "0.40");

        [TestCase("EndsInCollapsingFloor", "Crumble", "CollapsingFloor")]
        [TestCase("EndsInMovingSolid", "Slider", "MovingTrap")]
        [TestCase("EndsInFakePlatform", "Fake", "FakePlatform")]
        public void ASpearStuckInAnythingButFixedGeometry_IsRejectedNamingIt(string fixture, string host, string kind) =>
            AssertRejected(Spear(fixture), "Spear_A", host, kind);

        [Test] public void AStuckShaftOverlappingAnElement_IsRejectedNamingIt() => AssertRejected(Spear("ShaftOverlapsHazard"), "Spear_A", "Spikes");

        // Each spear's stuck shaft lies in the other's lane, so both are named.
        [Test]
        public void AStuckShaftOverlappingAnotherLane_IsRejectedNamingIt()
        {
            List<string> errors = Spear("ShaftOverlapsAnotherLane");
            TestContext.Out.WriteLine(string.Join("\n", errors));
            CollectionAssert.AreEquivalent(new[] { "Spear_A/Spear_B", "Spear_B/Spear_A" },
                errors.Select(e => e.Contains("FIX: Spear_A stuck shaft") && e.Contains("Spear_B's lane") ? "Spear_A/Spear_B" : e.Contains("FIX: Spear_B stuck shaft") && e.Contains("Spear_A's lane") ? "Spear_B/Spear_A" : e).ToArray());
        }

        [Test]
        public void ARoomWithoutSpears_PassesValidateSpear() =>
            CollectionAssert.IsEmpty(Rule("ValidateSpear", "FIX", Type.GetType("Parallax.Editor.Setup.ArrowFixtures, Parallax.Editor").GetMethod("AllRulesPass").Invoke(null, null), Sizes()));

        // R1: the committed asset keeps floors at 0.5 and has no serialized spear field yet, so the class default applies.
        [Test]
        public void TheSpearMinimum_IsOneMaxFallTick_AndFloorsKeepTheirs()
        {
            PlatformSizeConfig sizes = Sizes();
            Assert.AreEqual(.4f, sizes.SpearMinThickness, 1e-6f);
            Assert.AreEqual(.5f, sizes.MinThickness, 1e-6f);
            Assert.AreEqual(Motor().MaxFallSpeed * TickTime.SecondsPerTick, sizes.SpearMinThickness, 1e-4f, "one max-fall tick (D-074 (6))");
        }

        // R5: the committed collider (1.0 x 0.56) gives D-078's cap 1.4 + 0.44 - 0.24 = 1.60 at the default length.
        [Test]
        public void TheSpearDefaults_FitTheSpeedCap_AtTheCommittedCollider()
        {
            var motor = Motor();
            Assert.AreEqual(new Vector2(1f, .56f), motor.ColliderSize, "the collider R5's arithmetic used");
            object lane = Type.GetType("Parallax.Editor.Setup.ArrowLane, Parallax.Editor").GetMethod("SpearLane").Invoke(null, new object[] { ArrowDirection.Right, 1f, 10f, 1.4f, .4f, 1.2f, 8, false });
            float length = (float)F(lane, "Length"), speed = (float)F(lane, "UnitsPerTick");
            Assert.IsTrue((bool)F(lane, "Spear"));
            Assert.AreEqual(8, (int)F(lane, "TellTicks"));
            Assert.AreEqual(.4f, (float)F(lane, "Thickness"), 1e-6f);
            float cap = ArrowMath.MaxUnitsPerTick(length, motor.ColliderSize, motor.MaxSpeed * TickTime.SecondsPerTick);
            TestContext.Out.WriteLine($"cap {cap:F3} u/tick at length {length}, default speed {speed}");
            Assert.AreEqual(1.6f, cap, 1e-4f);
            Assert.LessOrEqual(speed, cap);
            Assert.GreaterOrEqual(speed, 1f, "extremely fast: at least 1.0 u/tick (§4)");
        }

        // ---------- ValidateBand (the shared levels-11+ check) ----------

        LevelListConfig ElevenLevels()
        {
            list = ScriptableObject.CreateInstance<LevelListConfig>();
            LevelEntry[] entries = Enumerable.Range(1, 11).Select(i => new LevelEntry { Id = $"B{i:00}", SceneName = $"Level_B{i:00}", DisplayName = i.ToString() }).ToArray();
            typeof(LevelListConfig).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(list, entries);
            return list;
        }

        [Test]
        public void ASpear_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Room("AllRulesPass"), levels);
                Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
                StringAssert.Contains($"{id}: spear 'Spear_A' in level {int.Parse(id.Substring(1))}", errors[0]);
            }
        }

        [Test] public void TheSameSpear_InLevel11_Passes() => CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Room("AllRulesPass"), ElevenLevels()));

        [Test] public void ASpearInAnUnlistedRoom_IsExempt() => CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab6", Room("AllRulesPass"), ElevenLevels()));
    }
}
