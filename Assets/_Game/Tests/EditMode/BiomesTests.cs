using System;
using NUnit.Framework;

namespace Parallax.Tests.EditMode
{
    // PAX-A20: the biome table (D-163 as amended by D-172): Golden Ruins L001-L030, Obsidian Void L031-L060, and anything that
    // isn't a level (the Trap Lab, fixtures) Golden Ruins. Read by reflection, as the other Editor-side tables' tests do.
    public sealed class BiomesTests
    {
        static readonly Type Table = Type.GetType("Parallax.Editor.Levels.Biomes, Parallax.Editor");

        static string For(string levelId) => Table.GetMethod("For", new[] { typeof(string) }).Invoke(null, new object[] { levelId }).ToString();

        [TestCase("L001", "GoldenRuins")]
        [TestCase("L030", "GoldenRuins")]
        [TestCase("L031", "ObsidianVoid")]
        [TestCase("L040", "ObsidianVoid")]
        [TestCase("L060", "ObsidianVoid")]
        [TestCase("L061", "GoldenRuins")]
        [TestCase(null, "GoldenRuins")]
        [TestCase("TrapLab", "GoldenRuins")]
        public void Biome_FollowsTheLevelRanges(string levelId, string biome)
        {
            Assert.IsNotNull(Table, "Parallax.Editor.Levels.Biomes is missing");
            Assert.AreEqual(biome, For(levelId));
        }
    }
}
