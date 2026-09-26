using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using static Parallax.Tests.EditMode.RouteTestApi;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-087 (D-089) §5, R5, R6: ValidateVine's clauses, ValidateVineRoutes, ValidateBand's vine clause, and "a vine is
    // never a kill volume". Every room is Trap Lab room 9 with one vine replaced or one element added.
    public sealed class VineValidatorTests
    {
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        static object Room9() => TrapLabRoom9Tests.Room();
        static IEnumerable<object> Elements(object room) => ((IEnumerable)F(room, "Elements")).Cast<object>();

        static object Rebuild(object room, IEnumerable<object> elements)
        {
            Type elementType = GeyserValidatorTests.EditorType("SoloRoomElement");
            object[] items = elements.ToArray();
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            ConstructorInfo roomCtor = GeyserValidatorTests.EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            return roomCtor.Invoke(new[] { F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps") });
        }

        // Room 9 with Vine_Real replaced (same name).
        static object WithReal(Vector2 position, Vector2 size, object settings = null, Vector2 secondaryPosition = default, Vector2 secondarySize = default) =>
            Rebuild(Room9(), Elements(Room9()).Select(e => (string)F(e, "Name") != "Vine_Real" ? e
                : GeyserValidatorTests.Element("Vine", "Vine_Real", position, size, secondaryPosition, secondarySize, settings)));

        static object WithExtra(object element) => Rebuild(Room9(), Elements(Room9()).Append(element));

        static List<string> Vine(object room) => Rule("ValidateVine", "FIX", room, Motor());

        static void AssertRejected(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            foreach (string fragment in fragments) StringAssert.Contains(fragment, errors[0]);
        }

        [Test] public void Room9sVines_PassValidateVine() => CollectionAssert.IsEmpty(Vine(Room9()));
        [Test] public void Room9sSnapVine_HasADeclaredBetrayal() => CollectionAssert.IsEmpty(Rule("ValidateVineRoutes", "FIX", Room9(), TrapLabRoom9Tests.Routes()));

        // ---------- size ----------

        [Test] public void AVineNotPoint6Wide_IsRejected() =>
            AssertRejected(Vine(WithReal(new Vector2(7.9f, 2.25f), new Vector2(.8f, 4.5f))), "Vine_Real", "0.80 u wide", "0.60");

        [Test] public void AVineShorterThan1Point5_IsRejected() =>
            AssertRejected(Vine(WithReal(new Vector2(7.9f, .7f), new Vector2(.6f, 1.4f))), "Vine_Real", "1.40 u tall", "1.50");

        [Test] public void AVine1Point5Tall_Passes() => CollectionAssert.IsEmpty(Vine(WithReal(new Vector2(7.9f, .75f), new Vector2(.6f, 1.5f))));

        // ---------- the frame, solids and the snapped cat ----------

        [Test] public void AVineOutsideTheFrame_IsRejected()
        {
            // x [-0.2, 0.4]: left of the room's frame (x 0).
            List<string> errors = Vine(WithReal(new Vector2(.1f, 2.25f), new Vector2(.6f, 4.5f)));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.StartsWith("FIX: Vine_Real lies outside the room's frame")), string.Join("\n", errors));
        }

        [Test] public void AVineIntoTheCeiling_IsRejected() =>
            // y [0, 7.5] reaches into the Ceiling (y [7, 8]).
            Assert.IsTrue(Vine(WithReal(new Vector2(7.9f, 3.75f), new Vector2(.6f, 7.5f))).Any(e => e.Contains("Vine_Real") && e.Contains("overlaps Ceiling")));

        [Test] public void AVineOverlappingASolid_IsRejected() =>
            // y [4, 6] at x [10.9, 11.5]: into the Cliff's top corner (x 11, y 4.5).
            AssertRejected(Vine(WithReal(new Vector2(11.2f, 5f), new Vector2(.6f, 2f))), "Vine_Real", "overlaps Cliff", "never overlaps a solid");

        [Test]
        public void AVineWhoseSnappedCatHitsTheCliff_IsRejected()
        {
            // Vine x [10.4, 11.0] touches the Cliff's face (x 11) without overlapping it; the cat at x 10.7 spans [10.2, 11.2].
            // (It hangs over the pit out of Floor_Left's reach too, so that error comes with it.)
            List<string> errors = Vine(WithReal(new Vector2(10.7f, 2.25f), new Vector2(.6f, 4.5f)));
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsFalse(errors.Any(e => e.Contains("never overlaps a solid")), "the vine itself is clear");
            Assert.IsTrue(errors.Any(e => e.Contains("snapped to Vine_Real") && e.Contains("overlaps Cliff") && e.Contains("R6")), string.Join("\n", errors));
        }

        // ---------- the bottom is reachable (R5) ----------

        [Test] public void AVineHangingOutOfAJumpsReach_IsRejected() =>
            // Bottom y 2.5 over Floor_Left (top 0): 0 + 0.56 + 1.6 = 2.16 < 2.5.
            AssertRejected(Vine(WithReal(new Vector2(7.9f, 4.25f), new Vector2(.6f, 3.5f))), "Vine_Real", "bottom (y 2.50)", "R5");

        [Test] public void AVineWithinAJumpsReach_Passes() =>
            CollectionAssert.IsEmpty(Vine(WithReal(new Vector2(7.9f, 3.9f), new Vector2(.6f, 4.2f))));

        // ---------- snap vines ----------

        [Test] public void ASnapVineSetToRearm_IsRejected() =>
            AssertRejected(Vine(WithReal(new Vector2(7.9f, 2.25f), new Vector2(.6f, 4.5f), GeyserValidatorTests.Timing(TrapRepeatMode.Rearm, 1, 0))), "Vine_Real", "Rearm", "Once");

        [Test] public void ASnapTriggerAwayFromTheVine_IsRejected() =>
            AssertRejected(Vine(WithReal(new Vector2(7.9f, 2.25f), new Vector2(.6f, 4.5f), GeyserValidatorTests.Timing(TrapRepeatMode.Once, 1, 0), new Vector2(3f, 1f), new Vector2(.5f, .5f))),
                "Vine_Real", "snap trigger", "doesn't overlap");

        [Test] public void ASnapVine_WithNoBetrayalRevealedByIt_IsRejected()
        {
            object room = WithReal(new Vector2(7.9f, 2.25f), new Vector2(.6f, 4.5f), GeyserValidatorTests.Timing(TrapRepeatMode.Once, 1, 0));
            AssertRejected(Rule("ValidateVineRoutes", "FIX", room, TrapLabRoom9Tests.Routes()), "Vine_Real", "snap vine", "revealed by it");
        }

        [Test]
        public void AVine_IsNeverAKillVolume()
        {
            MethodInfo kill = Validator.GetMethod("KillVolumes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(kill, "KillVolumes not found");
            foreach (object vine in Elements(Room9()).Where(e => F(e, "Kind").ToString() == "Vine"))
                CollectionAssert.IsEmpty(((IEnumerable)kill.Invoke(null, new[] { vine, (object)Vector2.zero })).Cast<object>().ToList());
        }

        // ---------- ValidateBand (levels 11+ only) ----------

        LevelListConfig ElevenLevels()
        {
            list = ScriptableObject.CreateInstance<LevelListConfig>();
            LevelEntry[] entries = Enumerable.Range(1, 11).Select(i => new LevelEntry { Id = $"B{i:00}", SceneName = $"Level_B{i:00}", DisplayName = i.ToString() }).ToArray();
            typeof(LevelListConfig).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(list, entries);
            return list;
        }

        [Test]
        public void AVine_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Room9(), levels);
                Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
                StringAssert.Contains($"{id}: vine 'Vine_Real' in level {int.Parse(id.Substring(1))}", errors[0]);
                StringAssert.Contains("D-089", errors[0]);
                StringAssert.Contains("Vine_Obvious", errors[1]);
            }
        }

        [Test] public void TheSameVines_InLevel11_Pass() => CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Room9(), ElevenLevels()));

        [Test] public void VinesInAnUnlistedRoom_AreExempt() => CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab9", Room9(), ElevenLevels()));
    }
}
