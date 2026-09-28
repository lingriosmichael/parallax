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
        [TestCase("CeilingVineSlab")]   // PAX-095 (D-098)
        [TestCase("CeilingStartVineSlab")]   // PAX-095 (D-098)
        public void DangerBeyondItsTrigger_SeenFromTheOnlyWayUp_Passes(string fixture) =>
            AssertNotMentioned(TriggerCoverage(Fixture(fixture, true)), "Ledge_Spikes");

        [TestCase("LaunchOnlyLedge")]
        [TestCase("ClimbOnlyLedge")]
        [TestCase("SpearOnlyLedge")]
        [TestCase("MoverOnlyLedge")]   // PAX-093 (D-095)
        [TestCase("CeilingVineSlab")]   // PAX-095 (D-098)
        [TestCase("CeilingStartVineSlab")]   // PAX-095 (D-098)
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

        // PAX-060 (L014 finding): before, the cat on the slab "grabbed" Vine under it, so the hall counted as reached from the
        // west and Low_Spikes were "reached from both sides".
        // The grab from the slab itself, and (upperVine) a leap onto it from a vine standing on the slab.
        [TestCase(false)] [TestCase(true)]
        public void AGrabOntoAVineUnderASlab_IsNotAWayIn_SoTheCutPasses(bool upperVine) =>
            AssertNotMentioned(TriggerCoverage(Fixture("GrabThroughSlab", upperVine)), "Low_Spikes");

        // A cat in a trough in the slab, right over the vine: the slab beside the trough rises above its floor and still closes the drop.
        [Test]
        public void AGrabFromATroughOntoAVineUnderTheSlab_IsNotAWayIn_SoTheCutPasses()
        {
            MethodInfo method = FixturesType.GetMethod("GrabFromTrough", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "TriggerCoverageFixtures.GrabFromTrough not found.");
            AssertNotMentioned(TriggerCoverage(method.Invoke(null, null)), "Low_Spikes");
        }

        [Test]
        public void DropThroughAPitClosedByACollapsingFloor_IsAWayIn_SoTheCutIsRejectedFromBothSides() =>
            AssertMentioned(TriggerCoverage(Fixture("ClosedPitDrop", false)), "Low_Spikes", "reached from both sides");

        // ---------- PAX-095 (D-098): a launch from a vine ----------

        // The search's own launch edge, on hand-made pieces in VineLaunchRoom: a cat climbing a vine inside an erupting column
        // is launched (the launch releases the climb, D-089 (3)), and reaches a surface within the envelope. A room can't
        // isolate this edge: the search always lets the cat stand on the vent itself.
        static readonly Type CoverageType = Type.GetType("Parallax.Editor.Setup.TriggerCoverage, Parallax.Editor");
        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static object Piece(float xMin, float xMax, float y, bool up, bool vine = false, float p0 = 0f, float p1 = 0f)
        {
            Type type = CoverageType.GetNestedType("Piece", BindingFlags.NonPublic);
            object piece = Activator.CreateInstance(type, true);
            foreach ((string field, object value) in new (string, object)[] { ("XMin", xMin), ("XMax", xMax), ("Y", y), ("Up", up), ("Vine", vine), ("P0", p0), ("P1", p1), ("InBand", true) })
                type.GetField(field).SetValue(piece, value);
            return piece;
        }

        static bool Launches(object a, object b)
        {
            object room = FixturesType.GetMethod("VineLaunchRoom", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            object vents = CoverageType.GetMethod("Vents", AnyStatic).Invoke(null, new[] { room, Motor(), (object)Gravity() });
            object reach = Activator.CreateInstance(CoverageType.GetNestedType("Reach", BindingFlags.NonPublic), Motor(), Gravity());
            var farTrigger = new Rect(0f, 0f, .4f, 7f);
            return (bool)CoverageType.GetMethod("Launches", AnyStatic).Invoke(null, new[] { a, b, vents, farTrigger, (object)0f, 7f, reach });
        }

        // A vine at the vent (x 20, paws 0.5-1.0, inside the column y 0-1.5), to a floor piece at y 3 beside the column.
        [Test]
        public void ACatOnAVineInsideAnUpVentsColumn_IsLaunched() =>
            Assert.IsTrue(Launches(Piece(19.5f, 20.5f, .5f, false, true, .5f, 1f), Piece(20f, 22f, 3f, false)));

        [Test]
        public void ACatOnAVineOutsideTheColumn_IsNotLaunched() =>
            Assert.IsFalse(Launches(Piece(24.5f, 25.5f, .5f, false, true, .5f, 1f), Piece(20f, 22f, 3f, false)));

        // Mirrored: a gravity-up cat on a vine inside Vent_Down's column (y 5.5-7; the cat's top 5.8-6.3) to an underside at 4.
        [Test]
        public void AGravityUpCatOnAVineInsideADownVentsColumn_IsLaunched() =>
            Assert.IsTrue(Launches(Piece(19.5f, 20.5f, 6.3f, true, true, 5.8f, 6.3f), Piece(20f, 22f, 4f, true)));
    }
}
