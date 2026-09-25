using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-059 (D-085): the rules band 1 adds or changes: the frame height from room geometry (C1), storey-aware trigger
    // coverage (C3), ValidateBand1Content and ValidateBand1Duration, the tell checks, and the builder's sorting for the
    // disguised trap floors. Fixtures live in RouteFixtures (the Editor assembly), reached by reflection.
    public sealed class Band1RulesTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        static object Fixture(string name, params object[] args) => Call(T("RouteFixtures"), name, args);

        static string[] Invoke(string method, params object[] args)
        {
            Assert.NotNull(Validator, "LevelLayoutValidator not found.");
            MethodInfo m = Validator.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "LevelLayoutValidator." + method + " not found.");
            return ((IList)m.Invoke(null, args)).Cast<string>().ToArray();
        }

        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static float Gravity() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;

        static string[] Coverage(object room) => Invoke("ValidateTriggerCoverage", "FIX", room, Motor(), Gravity(), new List<string>());
        static string[] Content(int level, object room, object routes) => Invoke("ValidateBand1Content", "FIX", level, room, routes);
        static object Routes(int chain, int side, int recovers, bool sequential = true) => Fixture("Band1Routes", chain, side, recovers, sequential);
        static object Room(float width = 30f, float doorX = 28f) => Fixture("Band1Room", width, doorX);

        static void AssertMentions(string[] errors, string mention)
        {
            Assert.IsNotEmpty(errors, "expected an error mentioning '" + mention + "'.");
            Assert.IsTrue(errors.Any(e => e.Contains(mention)), "expected an error mentioning '" + mention + "':\n" + string.Join("\n", errors));
        }

        // ---------- C1: frame height ----------

        [Test]
        public void TallRoom_DoorAboveTheOldFrameTop_PassesTheFrameRule() =>   // seen red: "Door lies outside the room's frame (... of [-4.00,8.00])"
            Assert.IsFalse(Invoke("Validate", "FIX", Fixture("TallRoom", false)).Any(e => e.Contains("frame")),
                string.Join("\n", Invoke("Validate", "FIX", Fixture("TallRoom", false))));

        [Test]
        public void TallRoom_AnElementAboveAllOfItsGeometry_FailsTheFrameRule() =>
            AssertMentions(Invoke("Validate", "FIX", Fixture("TallRoom", true)), "Stray lies outside the room's frame");

        // The frame's floor is still the lowest floor top (or pit bottom): a slab is not play space.
        [Test]
        public void AnElementSunkInsideTheFloorSlab_FailsTheFrameRule() =>
            AssertMentions(Invoke("Validate", "FIX", Fixture("SlabHazardRoom")), "Sunk lies outside the room's frame");

        // ---------- C3: storey-aware coverage ----------

        [Test]
        public void TwoStoreyRoom_EachTriggerCutsItsOwnStorey_IsCovered() =>   // seen red: both triggers "do not cut the cat's band"
            Assert.IsEmpty(Coverage(Fixture("TwoStoreyRoom", false)), string.Join("\n", Coverage(Fixture("TwoStoreyRoom", false))));

        [Test]
        public void TwoStoreyRoom_ATriggerThatMissesTheTopOfItsStorey_IsRejected() =>
            AssertMentions(Coverage(Fixture("TwoStoreyRoom", true)), "Spikes_S1's trigger");

        // ---------- ValidateBand1Content ----------

        [Test]
        public void Level3_FourLethalBetrayals_IsRejected() =>
            AssertMentions(Content(3, Room(), Routes(4, 0, 1)), "lethal");

        [Test]
        public void Level3_FiveLethal_FourInSequence_AndADeadEnd_Passes()
        {
            string[] errors = Content(3, Room(), Routes(4, 1, 0));
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Level8_SixLethalBetrayals_IsRejected() =>
            AssertMentions(Content(8, Room(), Routes(5, 1, 1)), "lethal");

        [Test]
        public void ABetrayalThatDoesntSurviveTheEarlierOnes_BreaksTheSequence() =>
            AssertMentions(Content(3, Room(), Routes(5, 0, 1, false)), "in sequence");

        [Test]
        public void ALevelWithNoDeadEnd_IsRejected() =>
            AssertMentions(Content(3, Room(), Routes(5, 0, 0)), "dead end");

        [Test]
        public void ARoomWiderThanTheFrame_IsRejected() =>
            AssertMentions(Content(3, Room(40f, 38f), Routes(4, 1, 0)), "wider than");

        [Test]
        public void ADoorCloserThanHalfTheRoomToTheStart_IsRejected() =>
            AssertMentions(Content(3, Room(30f, 10f), Routes(4, 1, 0)), "door");

        [TestCase(1, 279, false)] [TestCase(1, 280, true)]
        [TestCase(3, 499, false)] [TestCase(3, 500, true)] [TestCase(3, 1500, true)] [TestCase(3, 1501, false)]
        public void Duration_TenToThirtySeconds_AndLevelOneFromSix(int level, int ticks, bool passes)
        {
            string[] errors = Invoke("ValidateBand1Duration", "FIX", level, ticks);
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        // ---------- tells ----------

        [Test]
        public void AFallCatcherThatPointsAtAFakeSection_IsRejected() =>
            AssertMentions(Invoke("ValidateBand1Tells", "FIX", Fixture("SingledOutHazardRoom", true)), "Strip");

        [Test]
        public void AFakeSectionWithNothingUnderItThatPointsAtIt_Passes() =>
            Assert.IsEmpty(Invoke("ValidateBand1Tells", "FIX", Fixture("SingledOutHazardRoom", false)));

        [Test]
        public void AnUpsideDownTrapOverAnOpenHazardRecess_IsRejected() =>
            AssertMentions(Invoke("ValidateBand1Tells", "FIX", Fixture("RoofRecessRoom", false)), "Recess_Hazard");

        [Test]
        public void AnUpsideDownTrapThatFillsItsRecess_Passes() =>
            Assert.IsEmpty(Invoke("ValidateBand1Tells", "FIX", Fixture("RoofRecessRoom", true)));

        [Test]
        public void AFallingBlockHangingUnderTheCeiling_IsRejected() =>
            AssertMentions(Invoke("ValidateBand1Tells", "FIX", Fixture("HangingBlockRoom", false)), "Block");

        [Test]
        public void AFallingBlockFlushInsideTheCeiling_Passes() =>
            Assert.IsEmpty(Invoke("ValidateBand1Tells", "FIX", Fixture("HangingBlockRoom", true)));

        // ---------- the falling-block landing kill (a kit quirk) ----------

        [TestCase(8.5f, false)]   // seen in the harness: pins the standing cat without a kill (L001, PAX-059)
        [TestCase(8.4f, true)]
        [TestCase(4f, true)]
        public void AFallingBlock_KillsACatWhereItLands_OnlyWithAShortLastStep(float travel, bool passes)
        {
            string[] errors = Invoke("ValidateFallingBlockLanding", "FIX", Fixture("BlockTravelRoom", travel), Motor());
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        // ---------- a trap floor only goes when the cat gets onto it (found in play: L003's Tread_R4) ----------

        [TestCase(2.25f, false)]   // a jump from the floor (head 2.16) is within 0.15 of the underside: it would set it off
        [TestCase(2.35f, true)]
        public void ATouchTrapFloor_IsOutOfReachOfAJumpFromTheFloorUnderIt(float underside, bool passes)
        {
            string[] errors = Invoke("ValidateTrapFloorHeadroom", "FIX", Fixture("TrapLedgeRoom", underside), Motor());
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        // A trap floor triggered from a box over its top: the box must hold the whole top strip (a standing cat).
        [TestCase(4.5f, true)]    // the top to the ceiling
        [TestCase(.3f, false)]    // shorter than the cat: seen red before the rule accepted over-top triggers, too (band)
        public void ATrapFloorTriggeredFromOverItsTop_IsCoveredOnlyIfTheTriggerHoldsItsTopStrip(float triggerHeight, bool passes)
        {
            string[] errors = Invoke("ValidateSurfaceCoverage", "FIX", Fixture("TopTriggerLedgeRoom", triggerHeight), Motor());
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        // A falling block is set off right at it, never from afar (found in play: L001's block, 8.5 u from its trigger).
        [TestCase(11f, false)]   // 3.25 u away: seen red in L001 and L002 before their fix
        [TestCase(12f, true)]    // 2.25 u away
        public void AFallingBlock_IsSetOffWithinThreeUnitsOfIt(float triggerX, bool passes)
        {
            string[] errors = Invoke("ValidateTriggerNearTrap", "FIX", Fixture("BlockTriggerRoom", triggerX));
            Assert.AreEqual(passes, errors.Length == 0, string.Join("\n", errors));
        }

        // ---------- the level scene's cat starts on the room's checkpoint (found in play: L005 started at its door) ----------

        [TestCase("L001")] [TestCase("L002")] [TestCase("L003")] [TestCase("L004")] [TestCase("L005")]
        public void TheCatStartsOnTheCheckpoint(string id)
        {
            object room = RouteValidatorTests.Room(id);
            Vector2 checkpoint = ((System.Collections.IEnumerable)room.GetType().GetField("Elements").GetValue(room)).Cast<object>()
                .Where(e => e.GetType().GetField("Kind").GetValue(e).ToString() == "Checkpoint")
                .Select(e => (Vector2)e.GetType().GetField("Position").GetValue(e)).First();
            var start = (Vector2)Type.GetType("Parallax.Editor.Setup.LevelSetup, Parallax.Editor").GetMethod("CatStart").Invoke(null, new[] { room, (object)Motor() });
            Assert.AreEqual(checkpoint.x, start.x, 1e-4f, id + ": the cat must start at the checkpoint's x");
            Assert.AreEqual(checkpoint.y - Motor().ColliderBottom, start.y, 1e-4f, id + ": the cat's feet must be on the checkpoint");
        }

        // ---------- builder: the disguised trap floors draw over the pit, in the floor's colour ----------

        [TestCase("CollapsingFloor")]
        [TestCase("FakePlatform")]
        public void TrapFloor_HasTheFloorsColour_AndDrawsOverHazardsAndFloors(string kind)   // seen red: sortingOrder -2
        {
            Type builder = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");
            MethodInfo buildElement = builder.GetMethod("BuildElement", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(buildElement, "SoloRoomBuilder.BuildElement not found.");
            object room = Fixture("FlatRoom");
            var rootGo = new GameObject("PAX059_TellRoot");
            try
            {
                RealityRoot root = rootGo.AddComponent<RealityRoot>();
                foreach ((string k, string name) in new[] { ("Floor", "Floor_Built"), ("Hazard", "Hazard_Built"), (kind, "Trap_Built") })
                    buildElement.Invoke(null, new object[] { rootGo.transform, root, room, Element(k, name, new Vector2(11f, -.5f), new Vector2(2f, 1f)), null, null, null, null, null, new List<string>() });
                SpriteRenderer floor = rootGo.transform.Find("Floor_Built").GetComponent<SpriteRenderer>();
                SpriteRenderer hazard = rootGo.transform.Find("Hazard_Built").GetComponent<SpriteRenderer>();
                SpriteRenderer trap = rootGo.transform.Find("Trap_Built").GetComponent<SpriteRenderer>();
                Assert.AreEqual(floor.color, trap.color, "colour");
                Assert.AreEqual(floor.sortingLayerID, trap.sortingLayerID, "sorting layer");
                Assert.Greater(trap.sortingOrder, hazard.sortingOrder, "the trap floor must draw over a pit's hazard");
                Assert.Greater(trap.sortingOrder, floor.sortingOrder, "the trap floor must draw over the ground it fills");
            }
            finally { UnityEngine.Object.DestroyImmediate(rootGo); }
        }

        static object Element(string kind, string name, Vector2 position, Vector2 size)
        {
            Type kindType = Type.GetType("Parallax.Editor.Setup.SoloRoomElementKind, Parallax.Editor");
            Type elementType = Type.GetType("Parallax.Editor.Setup.SoloRoomElement, Parallax.Editor");
            Type settingsType = Type.GetType("Parallax.Editor.Setup.SoloRoomTrapSettings, Parallax.Editor");
            Type roleType = Type.GetType("Parallax.Editor.Setup.SoloRoomHazardRole, Parallax.Editor");
            ConstructorInfo ctor = elementType.GetConstructor(new[] { kindType, typeof(string), typeof(Vector2), typeof(Vector2), typeof(Vector2), typeof(Vector2), settingsType, roleType });
            object settings = kind == "CollapsingFloor" ? Activator.CreateInstance(settingsType, settingsType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First().GetParameters().Select(p => p.Name == "delayTicks" ? (object)12 : p.DefaultValue).ToArray()) : Activator.CreateInstance(settingsType);
            return ctor.Invoke(new[] { Enum.Parse(kindType, kind), name, position, size, Vector2.zero, Vector2.zero, settings, Enum.Parse(roleType, "Normal") });
        }
    }
}
