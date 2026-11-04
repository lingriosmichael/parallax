using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Parallax.Tests.EditMode
{
    // D-281: L031-L040 have their own light and rows, and a portal's ring (drawn bigger than its box) pierces no stone at any place
    // its end stands. The rules live beside the composer; Golden Ruins' palettes stay as they were.
    public sealed class ObsidianLookTests
    {
        static readonly Type Stack = Type.GetType("Parallax.Editor.Setup.EnvironmentStackSetup, Parallax.Editor");
        static readonly Type Palettes = Type.GetType("Parallax.Editor.Levels.LevelPalettes, Parallax.Editor");

        [Test]
        public void ObsidianLevels_FollowTheirLookRules()
        {
            var problems = (List<string>)Stack.GetMethod("ObsidianLookProblems").Invoke(null, null);
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [TestCase("L001", "Colonnade")]
        [TestCase("L020", "Aqueduct")]
        [TestCase("L030", "Clear")]
        public void GoldenRuinsLevels_KeepTheirRows(string level, string row) =>
            Assert.AreEqual(row, Palettes.GetMethod("Signature").Invoke(null, new object[] { level }));
    }
}
