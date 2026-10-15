using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-107 (D-131): a falling block leaves empty space where it was. One inside a floor's walkable top fails (the floor's
    // collider and stone stay behind it); one hung in a slab's underside, or one that is a section of a split slab, passes;
    // every level passes.
    public sealed class CleanFallsTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");

        static string[] Clean(string id, object room)
        {
            Assert.NotNull(Validator, "LevelLayoutValidator not found.");
            MethodInfo m = Validator.GetMethod("ValidateCleanFalls", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, "LevelLayoutValidator.ValidateCleanFalls not found.");
            return ((IList)m.Invoke(null, new[] { id, room })).Cast<string>().ToArray();
        }

        static object Fixture(string name, params object[] args) => Call(T("RouteFixtures"), name, args);

        [Test]
        public void BlockInAFloorsTop_Fails()
        {
            string[] errors = Clean("FIX", Fixture("FloorBlockRoom", false));
            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("Block", errors[0]);
            StringAssert.Contains("Slab", errors[0]);
        }

        [Test]
        public void BlockInASlabsUnderside_Passes() => Assert.IsEmpty(Clean("FIX", Fixture("FloorBlockRoom", true)));

        [Test]
        public void BlockThatIsASectionOfASplitSlab_Passes() => Assert.IsEmpty(Clean("FIX", Fixture("SlabBlockRoom", false)));

        [Test]
        public void EveryLevel_Passes([Range(1, 20)] int level)
        {
            string id = $"L{level:000}";
            Assert.IsEmpty(Clean(id, RouteValidatorTests.Room(id)));
        }
    }
}
