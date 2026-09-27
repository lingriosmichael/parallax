using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEditor;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-093 (D-095): LevelLayoutValidator.ValidateMovingFloors on MovingFloorFixtures' rooms. Q1: a sideways MovingTrap Solid
    // in a level numbered 14+ or Trap Lab room 12 declares Carry or Slip. Q4: a drop-and-return floor's cooldown covers its
    // motion. Q5: a shrinker's settings. Q6: a push path ends against its named partner or in open space. D-056 (3): a Solid's
    // swept path overlaps no fixed geometry. D-065: movers, shrinkers and push walls are levels 11+; slide-away and
    // drop-and-return floors are allowed in levels 1-10.
    public sealed class MovingFloorValidatorTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.MovingFloorFixtures, Parallax.Editor");
        static readonly Type Motion = Type.GetType("Parallax.Gameplay.Rooms.SurfaceMotion, Parallax.Gameplay");
        static LevelListConfig Levels() => AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");

        static object Room(string fixture, params object[] args) => Call(Fixtures, fixture, args);
        static object M(string name) => Enum.Parse(Motion, name);

        static string[] Rule(string levelId, object room)
        {
            MethodInfo m = Validator.GetMethod("ValidateMovingFloors", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "LevelLayoutValidator.ValidateMovingFloors not found.");
            return ((IList)m.Invoke(null, new[] { levelId, room, (object)Levels() })).Cast<string>().ToArray();
        }

        static void AssertNone(string[] errors) => CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        static void AssertMentions(string[] errors, string text) =>
            Assert.IsTrue(errors.Any(e => e.Contains(text)), $"expected an error mentioning '{text}':\n" + string.Join("\n", errors));

        // ---------- Q1 ----------

        [TestCase("Carry")] [TestCase("Slip")]
        public void ASidewaysMover_DeclaringItsMotion_PassesInTrapLabRoom12(string motion) => AssertNone(Rule("TrapLab12", Room("SidewaysMover", M(motion))));

        [Test] public void ASidewaysMover_LeftLegacy_IsRejectedInTrapLabRoom12() => AssertMentions(Rule("TrapLab12", Room("SidewaysMover", M("Legacy"))), "SurfaceMotion");
        [Test] public void ASidewaysMover_LeftLegacy_IsAllowedElsewhereInTheLab() => AssertNone(Rule("TrapLab4", Room("SidewaysMover", M("Legacy"))));

        // ---------- Q4 ----------

        [Test] public void ADropAndReturnFloor_WhoseCooldownCoversItsMotion_Passes() => AssertNone(Rule("TrapLab12", Room("DropCooldown", 80)));
        [Test] public void ADropAndReturnFloor_WhoseCooldownEndsBeforeItsReturn_IsRejected() => AssertMentions(Rule("TrapLab12", Room("DropCooldown", 79)), "cooldown");

        // ---------- Q5 ----------

        [Test] public void AShrinker_ToNothingOver40Ticks_Passes() => AssertNone(Rule("TrapLab12", Room("ShrinkerSettings", 40, 0f)));
        [Test] public void AShrinker_WithNoShrinkSettings_IsRejected() => AssertMentions(Rule("TrapLab12", Room("ShrinkerSettings", 0, 0f)), "shrink settings");
        [Test] public void AShrinker_WithANegativeMinimum_IsRejected() => AssertMentions(Rule("TrapLab12", Room("ShrinkerSettings", 40, -.5f)), "minimum width");
        [Test] public void AShrinker_ThatDoesntShrink_IsRejected() => AssertMentions(Rule("TrapLab12", Room("ShrinkerSettings", 40, 4f)), "minimum width");

        // ---------- Q6 ----------

        [Test] public void APushIntoOpenSpace_Passes() => AssertNone(Rule("TrapLab12", Room("PushIntoOpenSpace")));
        [Test] public void APushIntoItsNamedPartner_Passes() => AssertNone(Rule("TrapLab12", Room("PushIntoAnvil")));
        [Test] public void APushPath_ThroughAnotherFixedSolid_IsRejected() => AssertMentions(Rule("TrapLab12", Room("PushPathBlocked")), "Post");
        [Test] public void APushPath_EndingShortOfItsNamedPartner_IsRejected() => AssertMentions(Rule("TrapLab12", Room("PushPartnerNotAtTheEnd")), "Far_Anvil");

        // ---------- D-056 (3) ----------

        [Test] public void AMoverWhoseSweptPathRunsIntoAWall_IsRejected() => AssertMentions(Rule("TrapLab12", Room("MoverIntoAWall")), "Block");

        // ---------- D-065 ----------

        [TestCase("BandMover", "mover")] [TestCase("BandShrinker", "shrinking floor")] [TestCase("BandPushWall", "push wall")]
        public void InALevel1To10_TheHardPatterns_AreRejected(string fixture, string what) => AssertMentions(Rule("L005", Room(fixture)), what);

        [TestCase("BandSlideAway")] [TestCase("BandDropAndReturn")]
        public void InALevel1To10_SlideAwayAndDropAndReturn_AreAllowed(string fixture) => AssertNone(Rule("L005", Room(fixture)));
    }
}
