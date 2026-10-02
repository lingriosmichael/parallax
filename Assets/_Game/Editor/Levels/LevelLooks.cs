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
                    new Piece("ENV_RuinWall", 3f, -1.5f), new Piece("ENV_RuinWall", 16.5f, -1.5f, 0.9f, flip: true), new Piece("ENV_RuinWall", 29.5f, -1.5f),
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
                    new Piece("ENV_MidPillar", 27.5f, -3.2f),
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
                    new Piece("ENV_MidPillar", 7f, -3.2f),
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
                    new Piece("ENV_RuinWall", 4f, -0.5f, flip: true), new Piece("ENV_RuinWall", 17f, 0.2f), new Piece("ENV_RuinWall", 30f, -0.6f, 0.92f, flip: true),
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
                    new Piece("ENV_RuinWall", 2f, 0.5f, 0.95f, alpha: 0.4f), new Piece("ENV_RuinWall", 14f, 1.2f, flip: true, alpha: 0.4f), new Piece("ENV_RuinWall", 25f, 0.2f, alpha: 0.4f),
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

        public static Look For(string levelId) => levelId != null && ById.TryGetValue(levelId, out Look look) ? look : Default;

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
