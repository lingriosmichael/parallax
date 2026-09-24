using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-074 (D-078) R5/R6/R8: the six arrow rules, arrow trigger coverage, and arrows as chain
    // sources. Everything under test lives in the Editor assembly, which this test assembly doesn't
    // reference, so access goes through reflection (the TriggerCoverageTests pattern). Synthetic
    // rooms come from ArrowFixtures; coverage uses a CreateInstance CatMotorConfig (1 x 0.56).
    public sealed class ArrowValidatorTests
    {
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type ChainValidatorType = Type.GetType("Parallax.Editor.Setup.TrapLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.ArrowFixtures, Parallax.Editor");
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        static readonly Type LabType = Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor");
        static readonly string[] Rules = { "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" };
        const float SyntheticGravity = 30f;

        CatMotorConfig syntheticMotor;

        [SetUp] public void SetUp() => syntheticMotor = ScriptableObject.CreateInstance<CatMotorConfig>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(syntheticMotor);

        static object Fixture(string name, params object[] args)
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Setup.ArrowFixtures not found.");
            MethodInfo method = FixturesType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "ArrowFixtures." + name + " not found.");
            return method.Invoke(null, args);
        }

        static string[] Rule(string rule, object room, string levelId = "FIX")
        {
            Assert.NotNull(ValidatorType, "LevelLayoutValidator not found.");
            MethodInfo method = ValidatorType.GetMethod(rule, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator." + rule + " not found.");
            return ((IList)method.Invoke(null, new[] { levelId, room })).Cast<string>().ToArray();
        }

        string[] Coverage(object room, CatMotorConfig motor = null, string levelId = "FIX")
        {
            MethodInfo method = ValidatorType.GetMethod("ValidateTriggerCoverage", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator.ValidateTriggerCoverage not found.");
            return ((IList)method.Invoke(null, new[] { levelId, room, motor ?? syntheticMotor, SyntheticGravity, (object)new List<string>() })).Cast<string>().ToArray();
        }

        static void AssertRejected(string[] errors, params string[] mentions)
        {
            Assert.IsNotEmpty(errors, "expected a rejection.");
            Assert.IsTrue(errors.All(e => e.StartsWith("FIX:")), "every error must name the level id:\n" + string.Join("\n", errors));
            foreach (string mention in mentions)
                Assert.IsTrue(errors.Any(e => e.Contains(mention)), "expected an error mentioning '" + mention + "':\n" + string.Join("\n", errors));
        }

        static void AssertPasses(string[] errors) => Assert.IsEmpty(errors, string.Join("\n", errors));

        // ---------- R6: one passing and one failing fixture per rule ----------

        [Test] public void EveryRule_PassesTheWellFormedRoom([ValueSource(nameof(Rules))] string rule) => AssertPasses(Rule(rule, Fixture("AllRulesPass")));

        [Test] public void Tell_BelowSixTicks_IsRejected() => AssertRejected(Rule("ValidateArrowTell", Fixture("TellBelowSix")), "ArrowB", "tell 5");

        [Test] public void Speed_AtTheCap_Passes() => AssertPasses(Rule("ValidateArrowSpeed", Fixture("SpeedAt", 1.00f)));

        [Test] public void Speed_FirstSpeedAboveTheCap_IsRejected() => AssertRejected(Rule("ValidateArrowSpeed", Fixture("SpeedAt", 1.01f)), "ArrowB", "cap");

        [Test] public void Lane_BlockedByFixedGeometry_IsRejectedNamingTheBlocker() => AssertRejected(Rule("ValidateArrowLane", Fixture("LaneBlocked")), "ArrowB", "Crate");

        [Test] public void Lane_EndingInOpenAir_IsRejected() => AssertRejected(Rule("ValidateArrowLane", Fixture("LaneEndInOpenAir")), "ArrowB", "end face");

        [Test] public void DoorClearance_LaneThroughTheDoor_IsRejected() => AssertRejected(Rule("ValidateArrowDoorClearance", Fixture("LaneThroughTheDoor")), "ArrowD", "door");

        [Test] public void Cooldown_EndingBeforeTheArrowStops_IsRejected() => AssertRejected(Rule("ValidateArrowCooldown", Fixture("CooldownBeforeTheArrowStops")), "ArrowA", "cooldown 20");

        [Test] public void PeriodicSlack_BelowTwelveTicks_IsRejected() => AssertRejected(Rule("ValidateArrowPeriodicSlack", Fixture("PeriodicSlackBelowTwelve")), "ArrowA", "slack");

        // ---------- R5: coverage fixtures ----------

        [Test] public void CoverageF1_CutWithTheLaneBeyondItsNearEdge_Passes() => AssertPasses(Coverage(Fixture("CoverageF1_CutWithLaneBeyond")));

        [Test] public void CoverageF2_CutBeyondTheWholeLane_IsRejected() => AssertRejected(Coverage(Fixture("CoverageF2_CutBeyondTheLane")), "ArrowB", "lane");

        [Test] public void CoverageF3_NotACutAndNotContainingTheLane_IsRejected() => AssertRejected(Coverage(Fixture("CoverageF3_NotACut")), "ArrowB", "uncovered y [1.00, 7.00]");

        [Test] public void CoverageF4_JumpArcTriggerContainingTheLane_Passes() => AssertPasses(Coverage(Fixture("CoverageF4_JumpArcContainsLane")));

        [Test] public void CoverageF5_JumpArcTriggerMissingTheLane_IsRejected() => AssertRejected(Coverage(Fixture("CoverageF5_JumpArcMissesLane")), "ArrowC", "lane");

        [Test] public void CoverageF6_CutLessThanOneColliderWidthBeforeTheMouth_IsRejected() => AssertRejected(Coverage(Fixture("CoverageF6_CutTooCloseToTheMouth")), "ArrowB", "0.50 u beyond");

        [Test] public void CoverageF7_JumpArcContainingTheLane_GravityUpMirror_Passes() => AssertPasses(Coverage(Fixture("CoverageF7_JumpArcContainsLaneGravityUp")));

        // ---------- R8: an arrow as a chain source ----------

        [Test]
        public void ArrowAsChainSource_PassesTheChainChecks()
        {
            Assert.NotNull(ChainValidatorType, "TrapLayoutValidator not found.");
            MethodInfo method = ChainValidatorType.GetMethod("TryValidate", BindingFlags.Public | BindingFlags.Static);
            object[] args = { Fixture("ArrowChainSource"), null };
            Assert.IsTrue((bool)method.Invoke(null, args), "TrapLayoutValidator: " + args[1]);
        }

        // ---------- every real layout and every Trap Lab room ----------

        [Test]
        public void EveryLevelLayoutsEntry_PassesEveryArrowRuleAndCoverage()
        {
            Assert.NotNull(LayoutsType, "LevelLayouts not found.");
            var byId = (IEnumerable)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var errors = new List<string>();
            CatMotorConfig real = UnityEditor.AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            int count = 0;
            foreach (object pair in byId)
            {
                string id = (string)pair.GetType().GetProperty("Key").GetValue(pair);
                object room = pair.GetType().GetProperty("Value").GetValue(pair);
                foreach (string rule in Rules) errors.AddRange(Rule(rule, room, id));
                errors.AddRange(Coverage(room, real, id));
                count++;
            }
            Assert.AreEqual(4, count, "L001-L004");
            AssertPasses(errors.ToArray());
        }

        [Test]
        public void EveryTrapLabRoom_PassesEveryArrowRule_AndTheArrowRoomPassesCoverage()
        {
            Assert.NotNull(LabType, "TrapLabLayout not found.");
            var rooms = ((IEnumerable)LabType.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null)).Cast<object>().ToArray();
            var errors = new List<string>();
            foreach (object room in rooms)
                foreach (string rule in Rules) errors.AddRange(Rule(rule, room, "Lab" + Get(room, "Id")));

            // D-074: rooms 0-2 predate coverage and aren't checked for it; the arrow room is.
            object arrowRoom = rooms.Single(r => (int)Get(r, "Id") == 3);
            string[] arrows = ((IEnumerable)Get(arrowRoom, "Elements")).Cast<object>().Where(e => Get(e, "Kind").ToString() == "Arrow").Select(e => (string)Get(e, "Name")).ToArray();
            CollectionAssert.AreEquivalent(new[] { "ArrowA", "ArrowB", "ArrowC" }, arrows);
            CatMotorConfig real = UnityEditor.AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            errors.AddRange(Coverage(arrowRoom, real, "Lab3"));
            AssertPasses(errors.ToArray());
        }

        static object Get(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
