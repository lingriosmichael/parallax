using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-099 (D-106): LevelLayoutValidator.ValidateDoorStands. A level's door stands on a fixed solid whose top is its bottom
    // and covers its width, at its authored pose and, if it backs away, at its retreated pose. Red first: L004, L009 and L010
    // without the platforms PAX-099 added fail, at the poses that hung.
    public sealed class DoorStandsTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type Layouts = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        static readonly Type Definition = Type.GetType("Parallax.Editor.Setup.SoloRoomDefinition, Parallax.Editor");

        static object Level(string id) => ((IDictionary)Layouts.GetField("ById").GetValue(null))[id];

        static string[] Rule(string id, object room)
        {
            MethodInfo m = Validator.GetMethod("ValidateDoorStands", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "LevelLayoutValidator.ValidateDoorStands not found.");
            return ((IList)m.Invoke(null, new[] { id, room })).Cast<string>().ToArray();
        }

        // The level with one element left out (layout data only; the door rule reads nothing else).
        static object Without(object room, string name)
        {
            object Get(string field) => Definition.GetField(field)?.GetValue(room) ?? Definition.GetProperty(field)?.GetValue(room);
            Array elements = (Array)Get("Elements");
            Type elementType = elements.GetType().GetElementType();
            object[] kept = elements.Cast<object>().Where(e => (string)elementType.GetField("Name").GetValue(e) != name).ToArray();
            Assert.AreEqual(elements.Length - 1, kept.Length, $"the level has one '{name}'");
            Array copy = Array.CreateInstance(elementType, kept.Length);
            for (int i = 0; i < kept.Length; i++) copy.SetValue(kept[i], i);
            ConstructorInfo ctor = Definition.GetConstructors().First(c => c.GetParameters().Length == 7);
            return ctor.Invoke(new[] { Get("Id"), ((Vector2)Get("Origin")).x, Get("Width"), copy, Get("Openings"), Get("RequiredJumps"), Get("RequiredSteps") });
        }

        [Test]
        public void EveryLevel_HasItsDoorOnAPlatform([NUnit.Framework.Range(1, 20)] int level)
        {
            string id = "L" + level.ToString("000");
            string[] errors = Rule(id, Level(id));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        [TestCase("L004", "Door_Step", "its authored pose")]   // PAX-103: the door stands on the climb's top step (no retreat now)
        [TestCase("L009", "Slab_D", "its authored pose")]   // PAX-102: the door stands on Slab_D now
        [TestCase("L010", "Ledge_E3", "its authored pose")]   // PAX-102: the door stands on Ledge_E3 (no retreat now)
        [TestCase("L016", "Door_Ledge", "its authored pose")]
        [TestCase("L020", "Door_Ledge", "its retreated pose")]
        public void WithoutItsPlatform_TheDoorHangs_AndIsRejected(string id, string platform, string pose)
        {
            string[] errors = Rule(id, Without(Level(id), platform));
            Assert.IsTrue(errors.Any(e => e.StartsWith(id + ":") && e.Contains(pose) && e.Contains("doesn't stand")), $"expected '{pose}' to hang:\n" + string.Join("\n", errors));
        }
    }
}
