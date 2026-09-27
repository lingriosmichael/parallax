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
    // PAX-091 (D-074, D-080): the approach search sees a geyser launch, a vine climb and a stuck spear as ways up, surface
    // coverage works per storey, and a drop can't pass through a closed pit. In every fixture the checkpoint's side (the
    // old fallback) isn't the side the cat really arrives from, so each case was wrong before PAX-091: the "before its
    // trigger" cases passed although the cat walks into the danger unwarned, and the "beyond" cases were false errors
    // (L013's Spikes_D2). Rooms come from TriggerCoverageFixtures and are sized for the real cat (D-082: jump 1.6), so the
    // real CatMotorConfig and the cat prefab's gravity are used (ValidateSurfaceCoverage reads the prefab's gravity too).
    public sealed class CoverageEdgesTests
    {
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.TriggerCoverageFixtures, Parallax.Editor");

        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static float Gravity() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;

        static object Fixture(string name, bool variant)
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Setup.TriggerCoverageFixtures not found.");
            MethodInfo method = FixturesType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "TriggerCoverageFixtures." + name + " not found.");
            return method.Invoke(null, new object[] { variant });
        }

        static string[] TriggerCoverage(object room)
        {
            MethodInfo method = ValidatorType.GetMethod("ValidateTriggerCoverage", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator.ValidateTriggerCoverage not found.");
            return ((IList)method.Invoke(null, new[] { "FIX", room, Motor(), Gravity(), (object)new List<string>() })).Cast<string>().ToArray();
        }

        static string[] SurfaceCoverage(object room)
        {
            MethodInfo method = ValidatorType.GetMethod("ValidateSurfaceCoverage", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator.ValidateSurfaceCoverage not found.");
            return ((IList)method.Invoke(null, new[] { "FIX", room, (object)Motor() })).Cast<string>().ToArray();
        }

        static void AssertMentioned(string[] errors, string name, string phrase) =>
            Assert.IsTrue(errors.Any(e => e.Contains(name) && e.Contains(phrase)), $"expected an error naming {name} with '{phrase}':\n" + string.Join("\n", errors));

        static void AssertNotMentioned(string[] errors, string name) =>
            Assert.IsFalse(errors.Any(e => e.Contains(name)), $"expected no error naming {name}:\n" + string.Join("\n", errors));

        // ---------- the three ways up ----------

        [TestCase("LaunchOnlyLedge")]
        [TestCase("ClimbOnlyLedge")]
        [TestCase("SpearOnlyLedge")]
        [TestCase("MoverOnlyLedge")]   // PAX-093 (D-095)
        public void DangerBeyondItsTrigger_SeenFromTheOnlyWayUp_Passes(string fixture) =>
            AssertNotMentioned(TriggerCoverage(Fixture(fixture, true)), "Ledge_Spikes");

        [TestCase("LaunchOnlyLedge")]
        [TestCase("ClimbOnlyLedge")]
        [TestCase("SpearOnlyLedge")]
        [TestCase("MoverOnlyLedge")]   // PAX-093 (D-095)
        public void DangerBeforeItsTrigger_SeenFromTheOnlyWayUp_IsRejected(string fixture) =>
            AssertMentioned(TriggerCoverage(Fixture(fixture, false)), "Ledge_Spikes", "lies before Ledge_Spikes's trigger near edge");

        // ---------- surface coverage per storey ----------

        [Test]
        public void SurfaceOnASecondStorey_BeyondAnUpperStoreyCut_Passes() =>
            AssertNotMentioned(SurfaceCoverage(Fixture("SecondStoreySurface", true)), "Upper_Collapse");

        [Test]
        public void SurfaceOnASecondStorey_BeforeTheCut_IsRejected() =>
            AssertMentioned(SurfaceCoverage(Fixture("SecondStoreySurface", false)), "Upper_Collapse", "lies before Upper_Spikes's trigger near edge");

        // ---------- closed pits ----------

        [Test]
        public void DropThroughAPitClosedByAFixedFloor_IsNotAWayIn_SoTheCutPasses() =>
            AssertNotMentioned(TriggerCoverage(Fixture("ClosedPitDrop", true)), "Low_Spikes");

        // PAX-093 (D-095): the vertical wall. Before it, the drop off the slab's west end down the Slot counted as a way into
        // the hall from the west, so Low_Spikes were "reached from both sides".
        [Test]
        public void ADropDownASlotPastAFullHeightWall_IsNotAWayIn_SoTheCutPasses()
        {
            MethodInfo method = FixturesType.GetMethod("VerticalWallDrop", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "TriggerCoverageFixtures.VerticalWallDrop not found.");
            AssertNotMentioned(TriggerCoverage(method.Invoke(null, null)), "Low_Spikes");
        }

        [Test]
        public void DropThroughAPitClosedByACollapsingFloor_IsAWayIn_SoTheCutIsRejectedFromBothSides() =>
            AssertMentioned(TriggerCoverage(Fixture("ClosedPitDrop", false)), "Low_Spikes", "reached from both sides");
    }
}
