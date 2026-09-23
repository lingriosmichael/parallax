using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-073 (D-074): LevelLayoutValidator.ValidateTriggerCoverage. Everything under test lives in
    // the Editor assembly, which this test assembly does not reference, so access goes through
    // reflection (the LevelLayoutTests pattern). Synthetic rooms come from TriggerCoverageFixtures
    // and use a CreateInstance CatMotorConfig; only the real-levels check loads assets.
    public sealed class TriggerCoverageTests
    {
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.TriggerCoverageFixtures, Parallax.Editor");
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        static readonly Type SoloLayoutType = Type.GetType("Parallax.Editor.Setup.SoloRoomsLayout, Parallax.Editor");
        const float SyntheticGravity = 30f;

        CatMotorConfig syntheticMotor;

        [SetUp] public void SetUp() => syntheticMotor = ScriptableObject.CreateInstance<CatMotorConfig>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(syntheticMotor);

        static object Fixture(string name, params object[] args)
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Setup.TriggerCoverageFixtures not found.");
            MethodInfo method = FixturesType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "TriggerCoverageFixtures." + name + " not found.");
            return method.Invoke(null, args);
        }

        static string[] Coverage(string levelId, object room, CatMotorConfig motor, float gravity, List<string> learnedBypasses)
        {
            Assert.NotNull(ValidatorType, "Parallax.Editor.Setup.LevelLayoutValidator not found.");
            MethodInfo method = ValidatorType.GetMethod("ValidateTriggerCoverage", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator.ValidateTriggerCoverage not found.");
            return ((IList)method.Invoke(null, new[] { levelId, room, motor, gravity, (object)learnedBypasses })).Cast<string>().ToArray();
        }

        string[] Synthetic(string fixture, params object[] args) => Coverage("FIX", Fixture(fixture, args), syntheticMotor, SyntheticGravity, new List<string>());

        static void AssertRejected(string[] errors, params string[] mentions)
        {
            Assert.IsNotEmpty(errors, "expected a trigger-coverage rejection.");
            Assert.IsTrue(errors.All(e => e.StartsWith("FIX:")), "every error must name the level id:\n" + string.Join("\n", errors));
            foreach (string mention in mentions)
                Assert.IsTrue(errors.Any(e => e.Contains(mention)), "expected an error mentioning '" + mention + "':\n" + string.Join("\n", errors));
        }

        // ---------- §5.1 synthetic layouts ----------

        [Test]
        public void FallingCeiling_TriggerAtFloorHeightOnly_IsRejectedNamingTheTrapAndTheUncoveredBand() =>
            AssertRejected(Synthetic("FloorHeightTrigger"), "Ceiling_Block", "uncovered y [1.00, 7.00]");

        [Test]
        public void FallingCeiling_FullCurtain_Passes() => Assert.IsEmpty(Synthetic("FullCurtain"));

        [Test]
        public void ApproachFromLeft_DangerBeforeTheCurtain_IsRejected() =>
            AssertRejected(Synthetic("CheckpointLeftDangerBeforeCurtain"), "Ceiling_Block", "before");

        [Test]
        public void ApproachFromRight_DangerBeyondTheCurtain_Passes() => Assert.IsEmpty(Synthetic("CheckpointRightDangerBeyondCurtain"));

        [Test]
        public void ApproachFromRight_DangerBeforeTheCurtain_IsRejected() =>
            AssertRejected(Synthetic("CheckpointRightDangerBeforeCurtain"), "Ceiling_Block", "before");

        [Test]
        public void TriggerCoveringOnlyTheFloorSideJumpBand_IsRejectedForTheCeilingSideBand() =>
            AssertRejected(Synthetic("GravityUpBandUncovered"), "Ceiling_Block", "uncovered y [3.76, 7.00]");

        [Test]
        public void TriggerCoveringOnlyTheCeilingSideBand_IsRejectedForTheFloorSideBand() =>
            AssertRejected(Synthetic("CeilingSideBandOnly"), "Ceiling_Block", "uncovered y [0.00, 3.24]");

        [Test]
        public void LearnedBypass_WithAReason_PassesAndIsListed()
        {
            var bypasses = new List<string>();
            string[] errors = Coverage("FIX", Fixture("LearnedBypass", "jump the trigger"), syntheticMotor, SyntheticGravity, bypasses);
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.AreEqual(1, bypasses.Count, "the learned bypass must be listed.");
            StringAssert.Contains("Ceiling_Block", bypasses[0]);
            StringAssert.Contains("jump the trigger", bypasses[0]);
        }

        [Test]
        public void LearnedBypass_WithAnEmptyReason_IsRejected() =>
            AssertRejected(Synthetic("LearnedBypass", " "), "Ceiling_Block", "learned bypass");

        // ---------- R5 additions ----------

        [Test]
        public void ChainDescendant_WhoseDangerLiesBeforeTheRootsCurtain_IsRejectedNamingTheDescendant() =>
            AssertRejected(Synthetic("ChainDescendantBeforeRootCurtain"), "Block_Early", "Spikes_Root");

        [Test]
        public void FlipEntry_TriggerContainsTheOnlyFlipIntoTheStretch_Passes() => Assert.IsEmpty(Synthetic("FlipEntry", true));

        [Test]
        public void FlipEntry_TriggerMissesTheFlipIntoTheStretch_IsRejected() =>
            AssertRejected(Synthetic("FlipEntry", false), "Drop_Spikes");

        [Test]
        public void FlipEntry_DangerWithinGravityUpReach_IsRejected() =>
            AssertRejected(Synthetic("FlipEntryCeilingDanger"), "Ceiling_Drop");

        [Test]
        public void SunkPlatformInsideAPit_LowersTheBand() =>
            AssertRejected(Synthetic("SunkPlatformInPit"), "Ceiling_Block", "uncovered y [-1.00, 0.00]");

        [Test]
        public void NoCeilingOverTheTrap_IsRejectedWithBandUndefined() =>
            AssertRejected(Synthetic("NoCeiling"), "no ceiling over Ceiling_Block: coverage band undefined");

        // ---------- §5.2 real levels ----------

        [Test]
        public void EveryLevelLayoutsEntry_PassesTriggerCoverage_WithNoLearnedBypass()
        {
            CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(motor, "CatMotorConfig_Default asset missing.");
            GameObject cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab");
            GravityReceiver gravity = cat != null ? cat.GetComponent<GravityReceiver>() : null;
            Assert.NotNull(gravity, "Cat_Player prefab with a GravityReceiver is required.");

            var failures = new List<string>();
            var bypasses = new List<string>();
            foreach (DictionaryEntry entry in Registry())
                failures.AddRange(Coverage((string)entry.Key, entry.Value, motor, gravity.Strength, bypasses));
            Assert.IsEmpty(failures, "Trigger coverage (D-074):\n" + string.Join("\n", failures));
            Assert.IsEmpty(bypasses, "No trap in L001-L004 may be a learned bypass without the developer's approval by name:\n" + string.Join("\n", bypasses));
        }

        // ---------- R6: the extended Lift trigger still catches a standing cat (D-076) ----------

        [Test]
        public void L002Lift_ExtendedTrigger_StillCatchesAStandingCatOnTheLift()
        {
            object level = ((IDictionary)Registry())["L002"];
            object solo = ((IEnumerable)SoloLayoutType.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null)).Cast<object>().ElementAt(1);
            Rect levelTrigger = Trigger(level, "Lift"), soloTrigger = Trigger(solo, "Lift");
            Rect liftBody = Body(level, "Lift");
            CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(motor, "CatMotorConfig_Default asset missing.");
            // A cat standing on the Lift occupies y [lift top, lift top + collider height].
            float standTop = liftBody.yMax + motor.ColliderSize.y;
            float ceilingUnderside = Body(level, "Ceiling").yMin;
            Assert.IsTrue(Mathf.Abs(levelTrigger.xMin - 19.65f) <= 1e-4f && Mathf.Abs(levelTrigger.xMax - 19.95f) <= 1e-4f && levelTrigger.xMax <= liftBody.xMax,
                $"D-076: the Lift trigger moved +0.25 u for 50 Hz landing slack; fires 2 ticks later than SoloRoomsLayout. Expected x [19.65, 19.95] inside the Lift's top (xMax {liftBody.xMax:F2}), was x [{levelTrigger.xMin:F2}, {levelTrigger.xMax:F2}].");
            Assert.AreEqual(soloTrigger.yMin, levelTrigger.yMin, 1e-4f, "the trigger's bottom must not change.");
            Assert.Greater(soloTrigger.yMax, standTop, "the original trigger already covered a standing cat.");
            Assert.Greater(levelTrigger.yMax, standTop, "the extended trigger still covers a standing cat.");
            Assert.AreEqual(ceilingUnderside, levelTrigger.yMax, 1e-3f, "the extended trigger reaches the ceiling underside.");
        }

        static IDictionary Registry()
        {
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            return (IDictionary)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        static object Element(object room, string name) =>
            ((IEnumerable)Field(room, "Elements")).Cast<object>().Single(e => (string)Field(e, "Name") == name);

        static Rect Trigger(object room, string name)
        {
            object e = Element(room, name);
            Vector2 centre = (Vector2)Field(e, "SecondaryPosition"), size = (Vector2)Field(e, "SecondarySize");
            return new Rect(centre - size * .5f, size);
        }

        static Rect Body(object room, string name)
        {
            object e = Element(room, name);
            Vector2 centre = (Vector2)Field(e, "Position"), size = (Vector2)Field(e, "Size");
            return new Rect(centre - size * .5f, size);
        }

        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
