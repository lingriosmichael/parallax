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
    // PAX-085 (D-087) §5: ValidateInverter's clauses, ValidateBand's inverter clause, and "an inverter's own box is never a
    // kill volume". Every room is Trap Lab room 7 with its Inverter element replaced.
    public sealed class InverterValidatorTests
    {
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        static Type EditorType(string name)
        {
            Type type = Type.GetType("Parallax.Editor.Setup." + name + ", Parallax.Editor");
            Assert.NotNull(type, name + " not found");
            return type;
        }

        internal static object Room7() => ((IList)EditorType("TrapLabLayout").GetField("Rooms").GetValue(null))[7];

        static object Timing(TrapRepeatMode repeat, int period = 1, int cooldown = 0)
        {
            ConstructorInfo ctor = EditorType("SoloRoomTrapSettings").GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            object[] values = ctor.GetParameters().Select(p => p.Name switch
            {
                "repeatMode" => (object)repeat, "periodTicks" => period, "cooldownTicks" => cooldown, _ => p.DefaultValue,
            }).ToArray();
            return ctor.Invoke(values);
        }

        static object InverterSettings(int duration, TrapRepeatMode repeat = TrapRepeatMode.Once, int period = 1, int cooldown = 0)
        {
            Type inverter = EditorType("InverterSettings");
            object settings = inverter.GetConstructors().Single(c => c.GetParameters().Length == 2).Invoke(new object[] { duration, false });
            ConstructorInfo ctor = EditorType("SoloRoomTrapSettings").GetConstructors().Single(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == inverter);
            return ctor.Invoke(new[] { settings, Timing(repeat, period, cooldown) });
        }

        // Room 7 with its Inverter replaced (same name); the rest of the room unchanged.
        internal static object WithInverter(object settings, Vector2? position = null, Vector2? size = null)
        {
            object room = Room7();
            Type elementType = EditorType("SoloRoomElement");
            ConstructorInfo elementCtor = elementType.GetConstructors().First(c => c.GetParameters().Length == 7);
            var elements = ((IEnumerable)F(room, "Elements")).Cast<object>().Select(e =>
            {
                if ((string)F(e, "Name") != "Inverter") return e;
                return elementCtor.Invoke(new[] { F(e, "Kind"), "Inverter", position ?? (Vector2)F(e, "Position"), size ?? (Vector2)F(e, "Size"), Vector2.zero, Vector2.zero, settings });
            }).ToArray();
            Array array = Array.CreateInstance(elementType, elements.Length);
            for (int i = 0; i < elements.Length; i++) array.SetValue(elements[i], i);
            ConstructorInfo roomCtor = EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            return roomCtor.Invoke(new[] { F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps") });
        }

        static List<string> Inverter(object room) => Rule("ValidateInverter", "FIX", room);

        static void AssertRejected(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            foreach (string fragment in fragments) StringAssert.Contains(fragment, errors[0]);
        }

        [Test] public void Room7sInverter_Passes() => CollectionAssert.IsEmpty(Inverter(Room7()));

        [TestCase(25)]
        [TestCase(150)]
        [TestCase(500)]
        public void ADurationInsideTheRange_Passes(int duration) => CollectionAssert.IsEmpty(Inverter(WithInverter(InverterSettings(duration))));

        [TestCase(24)]
        [TestCase(501)]
        public void ADurationOutsideTwentyFiveToFiveHundred_IsRejected(int duration) =>
            AssertRejected(Inverter(WithInverter(InverterSettings(duration))), "Inverter", duration.ToString(), "25-500");

        [Test] public void ARearmInverter_Passes() => CollectionAssert.IsEmpty(Inverter(WithInverter(InverterSettings(150, TrapRepeatMode.Rearm, cooldown: 60))));

        [Test] public void APeriodicInverter_IsRejected() =>
            AssertRejected(Inverter(WithInverter(InverterSettings(150, TrapRepeatMode.Periodic, period: 400, cooldown: 60))), "Inverter", "Periodic", "Once or Rearm");

        [Test] public void AnInverterOutsideTheRoomsFrame_IsRejected() =>
            AssertRejected(Inverter(WithInverter(InverterSettings(150), new Vector2(19.9f, 1.5f))), "Inverter", "outside the room's frame");

        [Test]
        public void AnInvertersBox_IsNeverAKillVolume_SoDoorClearanceIgnoresIt()
        {
            // Right against the door (x 17.7-18.3): an inverter there is fine; a hazard there would fail door clearance.
            object room = WithInverter(InverterSettings(150), new Vector2(17.2f, 1f), new Vector2(.4f, 2f));
            MethodInfo kill = Validator.GetMethod("KillVolumes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(kill, "KillVolumes not found");
            object inverter = ((IEnumerable)F(room, "Elements")).Cast<object>().Single(e => (string)F(e, "Name") == "Inverter");
            CollectionAssert.IsEmpty(((IEnumerable)kill.Invoke(null, new[] { inverter, (object)Vector2.zero })).Cast<object>().ToList());
            List<string> errors = Rule("Validate", "FIX", room);
            CollectionAssert.IsEmpty(errors.Where(e => e.Contains("door")).ToList(), string.Join("\n", errors));
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
        public void AnInverter_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Room7(), levels);
                Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
                StringAssert.Contains($"{id}: inverter 'Inverter' in level {int.Parse(id.Substring(1))}", errors[0]);
                StringAssert.Contains("D-087", errors[0]);
            }
        }

        [Test] public void TheSameInverter_InLevel11_Passes() => CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Room7(), ElevenLevels()));

        [Test] public void AnInverterInAnUnlistedRoom_IsExempt() => CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab7", Room7(), ElevenLevels()));
    }
}
