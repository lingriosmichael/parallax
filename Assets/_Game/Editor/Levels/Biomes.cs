namespace Parallax.Editor.Levels
{
    public enum Biome { GoldenRuins, ObsidianVoid }

    /// <summary>PAX-A20 (D-151, D-163 as amended by D-172): which world a level is in, by its number. A table in code: it
    /// changes only with a decision. Golden Ruins L001-L030, Obsidian Void L031-L060; anything else (the Trap Lab, test
    /// fixtures, later worlds not yet decided) is Golden Ruins.</summary>
    public static class Biomes
    {
        public const int ObsidianFirst = 31, ObsidianLast = 60;

        public static Biome For(int level) => level >= ObsidianFirst && level <= ObsidianLast ? Biome.ObsidianVoid : Biome.GoldenRuins;

        /// <summary>A level id ("L031"), or null for a room that isn't a level's.</summary>
        public static Biome For(string levelId) =>
            levelId != null && levelId.Length > 1 && levelId[0] == 'L' && int.TryParse(levelId.Substring(1), out int n) ? For(n) : Biome.GoldenRuins;

        public static Biome Of(Parallax.Editor.Setup.SoloRoomDefinition room) => For(LevelLooks.LevelOf(room));
    }
}
