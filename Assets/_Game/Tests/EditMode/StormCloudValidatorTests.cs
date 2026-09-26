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
    // PAX-088 (D-090) §2.4, §2.5, §11: ValidateStormCloud's clauses (one per room, the cycle, the dodge rule, the range in the
    // frame and clear of static elements, a static top under every x, door clearance over the whole range), ValidateBand's
    // cloud clause, and the baked strike profile. Every room is Trap Lab room 10 with its StormCloud replaced (or one element
    // added or removed). Room 10: width 28, the cloud 2 x 0.8 at (4, 5.5) (bottom 5.1), range x 1.5-24, the Door at x 26.
    public sealed class StormCloudValidatorTests
    {
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        static Type E(string name) => GeyserValidatorTests.EditorType(name);
        internal static object Room10() => ((IList)E("TrapLabLayout").GetField("Rooms").GetValue(null))[10];

        internal static object CloudSettings(float minX = 1.5f, float maxX = 24f, float follow = .08f, int delay = 50, int period = 100, int tell = 25, int strike = 6,
            float width = .8f, TrapRepeatMode repeat = TrapRepeatMode.Once, TrapTriggerSource source = TrapTriggerSource.Overlap)
        {
            Type cloud = E("StormCloudSettings");
            object settings = cloud.GetConstructors().Single(c => c.GetParameters().Length == 8).Invoke(new object[] { minX, maxX, follow, delay, period, tell, strike, width });
            ConstructorInfo timingCtor = E("SoloRoomTrapSettings").GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            object timing = timingCtor.Invoke(timingCtor.GetParameters().Select(p => p.Name switch
            {
                "repeatMode" => (object)repeat, "triggerSource" => source, "periodTicks" => repeat == TrapRepeatMode.Periodic ? 100 : 1, _ => p.DefaultValue,
            }).ToArray());
            ConstructorInfo ctor = E("SoloRoomTrapSettings").GetConstructors().Single(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == cloud);
            return ctor.Invoke(new[] { settings, timing });
        }

        static object Rebuild(object room, IEnumerable<object> elements, object sections = null)
        {
            Type elementType = E("SoloRoomElement");
            object[] items = elements.ToArray();
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++) array.SetValue(items[i], i);
            if (sections == null)
            {
                ConstructorInfo roomCtor = E("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
                return roomCtor.Invoke(new[] { F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps") });
            }
            ConstructorInfo full = E("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 9);
            return full.Invoke(new[] { F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps"), null, sections, null });
        }

        static IEnumerable<object> Elements(object room) => ((IEnumerable)F(room, "Elements")).Cast<object>();
        static object Named(object room, string name) => Elements(room).Single(e => (string)F(e, "Name") == name);

        static object WithCloud(object settings, Vector2? position = null, object sections = null)
        {
            object room = Room10();
            object old = Named(room, "StormCloud");
            object cloud = GeyserValidatorTests.Element("StormCloud", "StormCloud", position ?? (Vector2)F(old, "Position"), (Vector2)F(old, "Size"),
                (Vector2)F(old, "SecondaryPosition"), (Vector2)F(old, "SecondarySize"), settings);
            return Rebuild(room, Elements(room).Select(e => (string)F(e, "Name") == "StormCloud" ? cloud : e), sections);
        }

        static List<string> Cloud(object room) => Rule("ValidateStormCloud", "FIX", room, Motor());

        static void AssertRejected(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            foreach (string fragment in fragments) StringAssert.Contains(fragment, errors[0]);
        }

        static void AssertAnyContains(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => fragments.All(e.Contains)), $"no error containing [{string.Join(", ", fragments)}]:\n" + string.Join("\n", errors));
        }

        [Test] public void Room10sCloud_PassesValidateStormCloud() => CollectionAssert.IsEmpty(Cloud(Room10()));
        [Test] public void Room10sCloud_WithItsSettingsRebuilt_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings())));

        // ---------- one per room ----------

        [Test]
        public void TwoClouds_AreRejected()
        {
            object room = Room10();
            object second = GeyserValidatorTests.Element("StormCloud", "StormCloud_2", new Vector2(12f, 5.5f), new Vector2(2f, .8f), new Vector2(12f, 3.5f), new Vector2(.5f, 7f), CloudSettings(10f, 14f));
            AssertAnyContains(Cloud(Rebuild(room, Elements(room).Append(second))), "2 storm clouds", "at most one");
        }

        // ---------- the cycle (ruling B) and the trigger ----------

        [Test] public void APeriodOfTellPlusStrike_IsRejected() => AssertRejected(Cloud(WithCloud(CloudSettings(period: 31))), "StormCloud", "period 31", "31");
        [Test] public void APeriodOfTellPlusStrikePlusOne_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings(period: 32))));
        [Test] public void AFirstStrikeDelayOfZero_IsRejected() => AssertRejected(Cloud(WithCloud(CloudSettings(delay: 0))), "StormCloud", "first strike delay 0");
        [Test] public void AFirstStrikeDelayOfOne_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings(delay: 1))));
        [Test] public void AStrikeOfZeroTicks_IsRejected() => AssertRejected(Cloud(WithCloud(CloudSettings(strike: 0))), "StormCloud", "strike 0");
        [Test] public void APeriodicCloud_IsRejected() => AssertAnyContains(Cloud(WithCloud(CloudSettings(repeat: TrapRepeatMode.Periodic))), "StormCloud", "Once");
        [Test] public void ARearmingCloud_IsRejected() => AssertAnyContains(Cloud(WithCloud(CloudSettings(repeat: TrapRepeatMode.Rearm))), "StormCloud", "Once");

        // ---------- the dodge rule (§2.4): tell >= from-rest ticks to clear 0.9 u (10) + 12, or + 8 in a precision section ----------

        [Test] public void ATellOf22_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings(tell: 22))));
        [Test] public void ATellOf21_IsRejected() => AssertRejected(Cloud(WithCloud(CloudSettings(tell: 21))), "StormCloud", "tell 21", "22", "D-056");
        [Test] public void AWiderStrike_NeedsALongerTell() => AssertRejected(Cloud(WithCloud(CloudSettings(tell: 22, width: 1.6f))), "StormCloud", "tell 22");

        static object SectionOverTheRange()
        {
            Type section = E("PrecisionSection");
            object s = Activator.CreateInstance(section, "Chase", Rect.MinMaxRect(0f, -1f, 28f, 7f));
            Array array = Array.CreateInstance(section, 1);
            array.SetValue(s, 0);
            return array;
        }

        [Test] public void InsideAPrecisionSection_ATellOf18_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings(tell: 18), sections: SectionOverTheRange())));
        [Test] public void InsideAPrecisionSection_ATellOf17_IsRejected() => AssertRejected(Cloud(WithCloud(CloudSettings(tell: 17), sections: SectionOverTheRange())), "StormCloud", "tell 17", "18");

        // ---------- the range: in the frame, clear of static elements, a static top under every x ----------

        [Test] public void AnAuthoredXOutsideTheRange_IsRejected() => AssertAnyContains(Cloud(WithCloud(CloudSettings(minX: 5f))), "StormCloud", "outside its range");
        [Test] public void ARangePastTheFramesEdge_IsRejected() => AssertAnyContains(Cloud(WithCloud(CloudSettings(minX: .5f))), "StormCloud", "outside the room's frame");

        [Test]
        public void ACloudInsideTheCeiling_IsRejected() =>
            AssertAnyContains(Cloud(WithCloud(CloudSettings(), new Vector2(4f, 7f))), "StormCloud", "overlaps Ceiling");

        [Test]
        public void ACloudOverlappingAStaticElementAnywhereInItsRange_IsRejected()
        {
            object room = WithCloud(CloudSettings());
            object pillar = GeyserValidatorTests.Element("Wall", "Pillar", new Vector2(15f, 3f), new Vector2(1f, 6f));
            AssertAnyContains(Cloud(Rebuild(room, Elements(room).Append(pillar))), "StormCloud", "overlaps Pillar");
        }

        [Test]
        public void ARangeWithNoStaticTopUnderPartOfIt_IsRejected()
        {
            // The Floor replaced by one that stops at x 10: past x 10.4 nothing static is under the column.
            object room = Room10();
            object shortFloor = GeyserValidatorTests.Element("Floor", "Floor", new Vector2(5f, -.5f), new Vector2(10f, 1f));
            AssertAnyContains(Cloud(Rebuild(room, Elements(room).Select(e => (string)F(e, "Name") == "Floor" ? shortFloor : e))), "StormCloud", "no static top");
        }

        // ---------- door clearance over the whole range (D-060) ----------

        [Test] public void ARangeReachingTheDoor_IsRejected() => AssertAnyContains(Cloud(WithCloud(CloudSettings(maxX: 25.2f))), "StormCloud", "door", "D-060");
        [Test] public void ARangeOneRunTickShortOfTheDoor_Passes() => CollectionAssert.IsEmpty(Cloud(WithCloud(CloudSettings(maxX: 25.1f))));

        // ---------- the baked profile (ruling A: static tops only) ----------

        static float StrikeBottomAt(object room, float x)
        {
            var profile = (Vector3[])Invoke(Validator, "StrikeProfile", room);
            Assert.IsTrue(StormCloudMath.StrikeBottom(profile, x, .4f, 5.1f, out float bottom), $"no top under x {x}");
            return bottom;
        }

        [Test]
        public void TheProfile_HasTheFloorTheRiseAndTheRealOverhang_ButNotTheFakeOne()
        {
            object room = Room10();
            Assert.AreEqual(0f, StrikeBottomAt(room, 3f), 1e-4f, "the Floor");
            Assert.AreEqual(0f, StrikeBottomAt(room, 7.5f), 1e-4f, "under Fake_Overhang: fakes don't block lightning");
            Assert.AreEqual(.8f, StrikeBottomAt(room, 14f), 1e-4f, "the Rise");
            Assert.AreEqual(1.9f, StrikeBottomAt(room, 21f), 1e-4f, "the real Overhang");
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
        public void ACloud_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Room10(), levels);
                Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
                StringAssert.Contains($"{id}: storm cloud 'StormCloud' in level {int.Parse(id.Substring(1))}", errors[0]);
                StringAssert.Contains("D-090", errors[0]);
            }
        }

        [Test] public void TheSameCloud_InLevel11_Passes() => CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Room10(), ElevenLevels()));
        [Test] public void ACloudInAnUnlistedRoom_IsExempt() => CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab10", Room10(), ElevenLevels()));
    }
}
