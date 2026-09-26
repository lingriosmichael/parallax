using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-089 A (R1, R2): LevelLayoutValidator.ValidateKit, the KIT-5-KIT-9 rules in one fixed order, over every LevelLayouts
    // entry and Trap Lab rooms 6-10, each with its declared routes (so GeyserEnvelope and VineRoutes run too).
    public sealed class LevelKitRulesTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        static PlatformSizeConfig Sizes() => AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
        static LevelListConfig Levels() => AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");

        static List<string> Kit(string id, object room, object routes)
        {
            MethodInfo kit = Validator.GetMethod("ValidateKit", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(kit, "LevelLayoutValidator.ValidateKit not found");
            return (List<string>)kit.Invoke(null, new[] { id, room, Motor(), Gravity(), Sizes(), Levels(), routes });
        }

        static IEnumerable<string> LevelIds() =>
            ((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null)).Keys.Cast<string>().OrderBy(id => id);

        static object TrapLabRoom(int n) => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[n];
        static object TrapLabRoutes(int n) => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room" + n);

        [TestCaseSource(nameof(LevelIds))]
        public void EveryLevelLayoutsEntry_PassesEveryKitRule(string id)
        {
            object routes = RouteValidatorTests.Routes(id);
            Assert.NotNull(routes, id + " has no routes");
            List<string> errors = Kit(id, RouteValidatorTests.Room(id), routes);
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void TrapLabRoom_PassesEveryKitRule(int n)
        {
            List<string> errors = Kit("TrapLab" + n, TrapLabRoom(n), TrapLabRoutes(n));
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        // The ticket's red check: a broken spear (0.3 u thick, below the spear minimum) added to level 1. ValidateSpear and
        // ValidateBand both report it, every error is kept, each prefixed with its rule, in the fixed order.
        [Test]
        public void ABrokenSpearInLevel1_IsReportedBySpearThenBand_EachPrefixedWithItsRule()
        {
            object thin = Invoke(Type.GetType("Parallax.Editor.Setup.SpearFixtures, Parallax.Editor"), "ThinShaft");
            object spear = ((IEnumerable)F(thin, "Elements")).Cast<object>().Single(e => (string)F(e, "Name") == "Spear_A");
            object room = WithExtra(RouteValidatorTests.Room("L001"), spear);
            List<string> errors = Kit("L001", room, RouteValidatorTests.Routes("L001"));
            TestContext.Out.WriteLine(string.Join("\n", errors));

            int thickness = errors.FindIndex(e => e.StartsWith("ValidateSpear: L001: Spear_A shaft thickness 0.30"));
            int band = errors.FindIndex(e => e.StartsWith("ValidateBand: L001: spear 'Spear_A' in level 1"));
            Assert.GreaterOrEqual(thickness, 0, "ValidateSpear's thickness error is missing");
            Assert.GreaterOrEqual(band, 0, "ValidateBand's spear error is missing");
            Assert.Less(thickness, band, "Spear runs before Band");
            Assert.IsTrue(errors.All(e => e.StartsWith("Validate")), "every error is prefixed with its rule's name");
        }

        // The route rules run only when routes are given: room 9's snap vine checked against room 8's routes (no betrayal is
        // revealed by it) fails VineRoutes; with no routes, it isn't checked.
        [Test]
        public void TheRouteRules_RunOnlyWhenRoutesAreGiven()
        {
            List<string> without = Kit("TrapLab9", TrapLabRoom(9), null);
            Assert.IsEmpty(without, string.Join("\n", without));
            List<string> wrong = Kit("TrapLab9", TrapLabRoom(9), TrapLabRoutes(8));
            TestContext.Out.WriteLine(string.Join("\n", wrong));
            Assert.IsNotEmpty(wrong);
            Assert.IsTrue(wrong.All(e => e.StartsWith("ValidateVineRoutes: TrapLab9: ")), string.Join("\n", wrong));
        }

        static object WithExtra(object room, object element)
        {
            Type elementType = GeyserValidatorTests.EditorType("SoloRoomElement");
            object[] list = ((IEnumerable)F(room, "Elements")).Cast<object>().Append(element).ToArray();
            Array array = Array.CreateInstance(elementType, list.Length);
            for (int i = 0; i < list.Length; i++) array.SetValue(list[i], i);
            ConstructorInfo ctor = GeyserValidatorTests.EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            return ctor.Invoke(new[] { F(room, "Id"), ((UnityEngine.Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps") });
        }
    }
}
