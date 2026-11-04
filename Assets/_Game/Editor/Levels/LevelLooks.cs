using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Editor.Levels
{
    /// <summary>PAX-A15 §4 (approved 2026-10-01): each level's look, as data beside its layout. A grade (LevelLookConfig),
    /// one stone fill for the whole room (one fill per room keeps every trap's host skin identical to its neighbours,
    /// P10), a dressing profile, and a skyline: the far and mid pieces, placed where they appear when the camera is at the
    /// middle of its travel. L011–L020 (and any unlisted level) share Default: the kit with no polish (ruling 2).</summary>
    public static class LevelLooks
    {
        public enum Fill { A, A2, A3 }

        public readonly struct Dressing
        {
            public readonly float Moss; public readonly bool Drapes, Banners, Rubble, Glyphs;
            public Dressing(float moss, bool drapes = false, bool banners = false, bool rubble = false, bool glyphs = false)
            { Moss = moss; Drapes = drapes; Banners = banners; Rubble = rubble; Glyphs = glyphs; }
        }

        /// <summary>One background piece. X and BaseY are its bottom centre in world units (Relative: fractions of the
        /// camera frame); Span &gt; 0 tiles it sideways over that width (bands: the aqueduct, the back wall) or, for the
        /// waterfall, upward over that height.</summary>
        public readonly struct Piece
        {
            public readonly string Slot; public readonly float X, BaseY, Scale, Span, Alpha; public readonly bool Flip, Relative;
            public Piece(string slot, float x, float baseY, float scale = 1f, float span = 0f, bool flip = false, bool relative = false, float alpha = 1f)
            { Slot = slot; X = x; BaseY = baseY; Scale = scale; Span = span; Flip = flip; Relative = relative; Alpha = alpha; }
        }

        public sealed class Look
        {
            public string Grade; public Fill Stone; public Dressing Dress; public string Signature;
            public Piece[] Pieces = new Piece[0];
            /// <summary>Overrides the grade's sun: (x, y) in the view at mid travel, and its size in units; zero = the grade's.</summary>
            public Vector3 Sun;
            /// <summary>D-161: the level's spikes draw as stout steel blades with red tips (TrapArtConfig.SpikeBladesStout). Every
            /// level since amendment 1 (the developer, 2026-10-21); false keeps D-151's painted blades.</summary>
            public bool StoutSpikes = true;
        }

        public const string DefaultGrade = "Afternoon";

        public static readonly Look Default = new()
        {
            // Phase 2 round 2 (L011's blank walls): glyph panels on the biggest faces too.
            Grade = DefaultGrade, Stone = Fill.A, Dress = new Dressing(0.4f, drapes: true, glyphs: true), Signature = "none",
            Pieces = new[]
            {
                new Piece("ENV_FarIsland_0", 0.2f, 0.62f, 1.5f, relative: true),
                new Piece("ENV_FarIsland_2", 0.68f, 0.7f, 1.3f, relative: true),
                new Piece("ENV_FarSpire_3", 0.45f, 0.5f, 1.5f, relative: true),
                new Piece("ENV_MidTree_0", 0.08f, 0.2f, relative: true),
            },
        };

        public static readonly IReadOnlyDictionary<string, Look> ById = new Dictionary<string, Look>
        {
            ["L001"] = new()
            {
                Grade = "MorningGold", Stone = Fill.A, Dress = new Dressing(0.6f, drapes: true), Signature = "great arch framing the sun",
                Sun = new Vector3(0.3f, 0.74f, 3.4f),
                Pieces = new[]
                {
                    new Piece("ENV_MidArch_0", 6.8f, -0.6f),
                    new Piece("ENV_FarIsland_0", 18f, 4.6f, 1.5f),
                    new Piece("ENV_FarIsland_2", 27.5f, 5.4f, 1.2f),
                    new Piece("ENV_FarSpire_3", 12.5f, 3.6f, 1.5f),
                    new Piece("ENV_MidArch_1", 23.5f, 1.4f),
                    new Piece("ENV_MidTree_0", 31f, -1.2f),
                },
            },
            ["L002"] = new()
            {
                Grade = "MorningGold", Stone = Fill.A, Dress = new Dressing(0.5f, drapes: true, rubble: true), Signature = "aqueduct span, water in the pits",
                Pieces = new[]
                {
                    new Piece("ENV_MidAqueduct", 16f, 2.2f, span: 37f),
                    new Piece("ENV_Waterfall", 9.3f, -1f, span: 16f),
                    new Piece("ENV_Mist", 9.3f, -2.5f, alpha: 0.35f),
                    new Piece("ENV_FarIsland_1", 24f, 6.8f, 1.5f),
                    new Piece("ENV_FarSpire_0", 2.5f, 1.5f, 1.0f),
                },
            },
            ["L003"] = new()
            {
                Grade = "NoonGold", Stone = Fill.A3, Dress = new Dressing(1f, drapes: true), Signature = "waterfall and mist",
                Pieces = new[]
                {
                    new Piece("ENV_Waterfall", 23.6f, -1f, span: 17f, alpha: 0.8f),   // A16: clear of the treads (A1)
                    new Piece("ENV_Mist", 23.6f, -2.5f, alpha: 0.4f),
                    new Piece("ENV_MidTree_0", 5.5f, -1.2f),
                    new Piece("ENV_MidTree_1", 34.5f, 2.5f),   // A16: behind the right wall, clear of Tread_R3 (A1)
                    new Piece("ENV_FarSpire_0", 16f, 3f, 1.2f),
                    new Piece("ENV_FarIsland_1", 10f, 9f, 1.5f),
                },
            },
            ["L004"] = new()
            {
                Grade = "WarmAfternoon", Stone = Fill.A, Dress = new Dressing(0.4f, banners: true, glyphs: true), Signature = "enclosed hall",
                Pieces = new[]
                {
                    // Phase 2 round 2: a ruined hall of whole wall pieces (the tiled back wall repeated every 11 u).
                    // Round 3 (critics: "a pale strip between the back wall and the floor"): every foot well under the floor line.
                    // 2026-10-03 (the developer: "visuals are weird", a pale sky column between two pieces): 10.8 u apart, so
                    // each piece's feathered end (8% of its 12 u) overlaps the next and no gap shows.
                    new Piece("ENV_RuinWall", 2f, -1.5f), new Piece("ENV_RuinWall", 12.1f, -1.5f, flip: true), new Piece("ENV_RuinWall", 22.2f, -1.5f),
                    new Piece("ENV_RuinWall", 32.3f, -1.5f, flip: true),
                    new Piece("ENV_FarIsland_3", 8f, 15.5f, 1.5f),
                    new Piece("ENV_MidTowers", 27f, 9.5f),
                    new Piece("ENV_FarSpire_2", 18f, 11f, 1.3f),
                },
            },
            ["L005"] = new()
            {
                Grade = "Afternoon", Stone = Fill.A, Dress = new Dressing(0.4f, drapes: true, banners: true, glyphs: true), Signature = "banner hall",
                Pieces = new[]
                {
                    new Piece("ENV_MidColonnade", 20f, 9.5f),
                    new Piece("ENV_MidTowers", 5.5f, 5f),
                    new Piece("ENV_FarSpire_2", 26f, 9f, 1.5f),
                    new Piece("ENV_FarIsland_0", 12f, 10.5f, 1.3f),
                },
            },
            ["L006"] = new()
            {
                Grade = "LateAfternoon", Stone = Fill.A3, Dress = new Dressing(0.8f, drapes: true, rubble: true), Signature = "trees, broken pillars, low fog",
                Pieces = new[]
                {
                    new Piece("ENV_MidAqueduct", 16f, 0.9f, span: 37f),
                    new Piece("ENV_MidTree_1", 5f, -0.8f),
                    // 2026-10-18: the pillar (ENV_MidPillar, 27.5, -3.2) removed; it read as a platform (see NearPalette).
                    new Piece("ENV_FarIsland_2", 16f, 6.4f, 1.5f),
                    new Piece("ENV_Fog", 16f, -0.6f, span: 37f, alpha: 0.6f),
                },
            },
            ["L007"] = new()
            {
                // Phase 2 round 2: its big flat walls read blank; drapes, banners and glyph panels (two each at most, biggest faces).
                Grade = "GoldenHour", Stone = Fill.A2, Dress = new Dressing(0.35f, drapes: true, banners: true, rubble: true, glyphs: true), Signature = "collapsed ruins",
                Pieces = new[]
                {
                    // 2026-10-17 visual review ("the tall cracked column is drawn through the platform's lower course"): the near
                    // pillar (ENV_MidPillar, 7, -3.2) removed; on the near layer it slid behind the slabs wherever the camera went.
                    new Piece("ENV_MidArch_1", 25f, 4f),
                    new Piece("ENV_FarSpire_1", 16f, 3f, 1.5f),
                    new Piece("ENV_FarIsland_1", 29f, 10f, 1.4f),
                    new Piece("ENV_MidTree_0", 31.5f, -1.2f),
                },
            },
            ["L008"] = new()
            {
                Grade = "GoldenHour", Stone = Fill.A, Dress = new Dressing(0.4f, drapes: true, glyphs: true), Signature = "light shafts through the back wall",
                Pieces = new[]
                {
                    // D-300: 10.1 u apart, so each feathered end lies over the next wall's solid stone (at 10.8 they overlapped each other and stacked up into a pale pillar). 2026-10-17 visual review ("a strip of
                    // sky runs between the background ruin wall's straight bottom edge and the floor", "the floor dip shows a
                    // window of far sky", "a grey haze strip with a hard edge" where two pieces met at different heights): one
                    // base for all four, under the sump's floor (-3.6: -2 still stopped partway down the dip).
                    new Piece("ENV_RuinWall", 2f, -3.6f, flip: true), new Piece("ENV_RuinWall", 12.1f, -3.6f), new Piece("ENV_RuinWall", 22.2f, -3.6f, flip: true),
                    new Piece("ENV_RuinWall", 32.3f, -3.6f),
                    new Piece("ENV_MidTowers", 20f, 8.5f),
                    new Piece("ENV_Shaft", 11f, 1.5f, alpha: 0.45f),
                    new Piece("ENV_Shaft", 24.5f, 2.5f, alpha: 0.35f),
                },
            },
            ["L009"] = new()
            {
                Grade = "DuskAmber", Stone = Fill.A2, Dress = new Dressing(0.4f, rubble: true), Signature = "low sun, long haze, spires",
                Pieces = new[]
                {
                    new Piece("ENV_MidAqueduct", 16f, 9f, span: 37f),
                    new Piece("ENV_FarSpire_0", 6f, 2f, 1.5f),
                    new Piece("ENV_FarSpire_1", 26f, 3f, 1.5f),
                    new Piece("ENV_FarSpire_3", 16f, 6f, 1.5f),
                    new Piece("ENV_MidTree_1", 31f, 1f),
                },
            },
            ["L010"] = new()
            {
                Grade = "DuskRose", Stone = Fill.A3, Dress = new Dressing(0.5f, banners: true, glyphs: true), Signature = "water, fog and banners at dusk",
                Pieces = new[]
                {
                    // Round 4 (the cat vanished on them at night, contrast 2.0): hazed into the moonlit sky.
                    // D-300: 10.1 u apart, so each feathered end lies over the next wall's solid stone (at 10.8 they overlapped each other and stacked up into a pale pillar).
                    new Piece("ENV_RuinWall", 2f, 0.5f, alpha: 0.4f), new Piece("ENV_RuinWall", 12.1f, 1.2f, flip: true, alpha: 0.4f), new Piece("ENV_RuinWall", 22.2f, 0.2f, alpha: 0.4f),
                    new Piece("ENV_RuinWall", 32.3f, 0.8f, flip: true, alpha: 0.4f),
                    new Piece("ENV_Waterfall", 8f, 0f, span: 15f),
                    new Piece("ENV_MidTowers", 18f, 10f),
                    new Piece("ENV_Fog", 12f, 0.4f, span: 28f, alpha: 0.55f),
                },
            },
        };

        /// <summary>The grade table of §4, the acceptance check A5 compares against.</summary>
        public static readonly IReadOnlyDictionary<string, string> ApprovedGrades = new Dictionary<string, string>
        {
            ["L001"] = "MorningGold", ["L002"] = "MorningGold", ["L003"] = "NoonGold", ["L004"] = "WarmAfternoon", ["L005"] = "Afternoon",
            ["L006"] = "LateAfternoon", ["L007"] = "GoldenHour", ["L008"] = "GoldenHour", ["L009"] = "DuskAmber", ["L010"] = "DuskRose",
        };

        public static Look For(string levelId) => levelId != null && ById.TryGetValue(levelId, out Look look) ? look
            : levelId != null && StoneLooks.TryGetValue(levelId, out Look stone) ? stone
            : Biomes.For(levelId) == Biome.ObsidianVoid ? DefaultObsidian : Default;

        /// <summary>PAX-A20 stage 4: an unlisted Obsidian Void level's look: Default's, dressed with Obsidian's props (no moss;
        /// holo-banners, wreckage and circuit panels; the kit places fixtures and machines in place of ivy and ferns).</summary>
        public static readonly Look DefaultObsidian = new()
        {
            Grade = DefaultGrade, Stone = Fill.A, Dress = new Dressing(0f, drapes: true, banners: true, rubble: true, glyphs: true), Signature = "none",
            Pieces = Default.Pieces,
        };

        // D-281, D-282: levels that differ from their world's default look only in their stone.
        static Look Stone(Fill stone) => new() { Grade = DefaultGrade, Stone = stone, Dress = Default.Dress, Signature = "none", Pieces = Default.Pieces };
        static Look ObsidianStone(Fill stone) => new() { Grade = DefaultGrade, Stone = stone, Dress = DefaultObsidian.Dress, Signature = "none", Pieces = Default.Pieces };
        static readonly Dictionary<string, Look> StoneLooks = new()
        {
            ["L021"] = Stone(Fill.A3), ["L022"] = Stone(Fill.A2), ["L023"] = Stone(Fill.A), ["L024"] = Stone(Fill.A3), ["L025"] = Stone(Fill.A2),
            ["L026"] = Stone(Fill.A3), ["L027"] = Stone(Fill.A), ["L028"] = Stone(Fill.A2), ["L029"] = Stone(Fill.A3), ["L030"] = Stone(Fill.A2),
            ["L031"] = ObsidianStone(Fill.A3), ["L032"] = ObsidianStone(Fill.A2), ["L033"] = ObsidianStone(Fill.A3), ["L034"] = ObsidianStone(Fill.A),
            ["L035"] = ObsidianStone(Fill.A3), ["L036"] = ObsidianStone(Fill.A2), ["L037"] = ObsidianStone(Fill.A3), ["L038"] = ObsidianStone(Fill.A2),
            ["L039"] = ObsidianStone(Fill.A3), ["L040"] = ObsidianStone(Fill.A),
        };

        /// <summary>D-305: Golden Ruins' three districts: 0 The Gardens, 1 The Overgrown Quarter, 2 The High Citadel; -1 elsewhere.</summary>
        public static int District(string levelId)
        {
            int n = levelId != null && levelId.Length > 1 && int.TryParse(levelId.Substring(1), out int v) ? v : 0;
            return n >= 1 && n <= 30 ? (n - 1) / 10 : -1;
        }

        /// <summary>D-312: the exit door's kit slot: Pack E's own door in The Overgrown Quarter and The High Citadel, the Gardens door
        /// (and Obsidian's, from its own kit) elsewhere.</summary>
        public static string DoorSlot(string levelId) => District(levelId) switch { 1 => "ENV_DoorOvergrown", 2 => "ENV_DoorCitadel", _ => "ENV_Door" };

        /// <summary>D-307: hero walls may stand in any level, on a fixed wall big enough (a Trap Lab room or a fixture has none).</summary>
        public static bool HeroWalls(string levelId) => levelId != null;

        /// <summary>D-307: the levels with the collapsed wall (HW-02) behind the play, one every few levels so it stays a find.</summary>
        public static readonly HashSet<string> Breaches = new() { "L004", "L008", "L012", "L017", "L022", "L025", "L026", "L029" };

        /// <summary>D-310: levels whose thin tiles take depth on the face no cat reaches (L034: each floor tile is walked on top and each
        /// ceiling tile underneath; their other faces look out over the laser floor and roof).</summary>
        public static readonly HashSet<string> FarFaceDepth = new() { "L034" };


        /// <summary>D-307: how much of a surface's dressing is Pack B's plants, by district (The Gardens, The Overgrown Quarter, The High Citadel).</summary>
        public static float PlantShare(string levelId) => District(levelId) switch { 0 => 0.35f, 1 => 1f, 2 => 0.2f, _ => 0f };

        /// <summary>The level a room definition belongs to (the registry shares each layout's element array), or null for a
        /// room that isn't a level's (the Trap Lab, test fixtures).</summary>
        public static string LevelOf(Parallax.Editor.Setup.SoloRoomDefinition room)
        {
            foreach (KeyValuePair<string, Parallax.Editor.Setup.SoloRoomDefinition> pair in LevelLayouts.ById)
                if (ReferenceEquals(pair.Value.Elements, room.Elements)) return pair.Key;
            return null;
        }

        /// <summary>Which layer a piece's slot belongs to.</summary>
        public static string LayerOf(string slot) =>
            slot.StartsWith("ENV_Far") ? "Far" : slot == "ENV_BackWall" ? "BackWall" : slot is "ENV_Fog" or "ENV_Shaft" ? "Atmosphere"
            : slot.StartsWith("ENV_FG_") ? "Foreground" : "Mid";

        /// <summary>A2: the flat, walkable-looking lines of a piece, as fractions of its height down from its top (a deck, a
        /// lintel). Pieces with broken silhouettes (towers, trees, spires) have none.</summary>
        public static float[] FlatTops(string slot) => slot switch
        {
            "ENV_MidAqueduct" => new[] { 0.06f },
            "ENV_MidColonnade" => new[] { 0.1f },
            "ENV_MidArch_0" => new[] { 0.08f },
            // The broken pillar's plinth: two stepped tiers near its foot read as stairs at walking height.
            "ENV_MidPillar" => new[] { 0.86f, 0.92f },
            _ => new float[0],
        };
    }
}
