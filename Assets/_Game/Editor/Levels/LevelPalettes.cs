using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Editor.Levels
{
    /// <summary>PAX-A16 gauntlet (the developer's brief: "each level gets its own time of day, palette, skyline composition and
    /// one signature element, so no two levels look alike"): a level's light and colour. The composer
    /// (EnvironmentStackSetup.Compose) reads it for the sky plate, the tints of every depth, the 2D lights and the level's own
    /// post-processing profile. Data only; the numbers were set against the captures.</summary>
    public static class LevelPalettes
    {
        public sealed class Palette
        {
            public string Name;
            /// <summary>The baked sky plate (ENV_Sky_&lt;Sky&gt;, env_v2.py's ramp of the same name).</summary>
            public string Sky;
            public Color SkyTop, Horizon, Haze, Clouds, Sun;
            /// <summary>The 2D lights: the ambient (global) light, the sun's backlight over the play layer, the cat's key light.</summary>
            public Color Ambient, SunLight, CatLight;
            public float AmbientIntensity = 0.85f, SunIntensity = 0.9f, CatIntensity = 1.5f;
            /// <summary>The sun disc in the view at mid travel (0..1 across and up) and its size in units; a moon if Moon.</summary>
            public Vector2 SunAt = new(0.68f, 0.7f);
            public float SunSize = 3.2f;
            public bool Moon;
            /// <summary>Post: white balance, split toning (cool shadows, warm highlights), colour adjustments, bloom.</summary>
            public float Temperature, Tint, Saturation, Contrast = 18f, Exposure = 0.35f, Bloom = 0.45f;
            public Color Shadows = new(0.45f, 0.5f, 0.56f), Highlights = new(0.58f, 0.52f, 0.44f);
            /// <summary>How much of the far distance the haze swallows (0..1), and the low fog's strength.</summary>
            public float FarHaze = 0.55f, Fog = 0.35f;
        }

        static Color H(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        static Palette P(string name, string sky, string top, string horizon, string haze, string clouds, string sun, string ambient, string sunLight,
            float ambientI, float sunI, float temp, float tint, float sat, string shadows, string highlights, float exposure = 0.35f, float farHaze = 0.55f, float fog = 0.35f) => new()
        {
            Name = name, Sky = sky, SkyTop = H(top), Horizon = H(horizon), Haze = H(haze), Clouds = H(clouds), Sun = H(sun),
            Ambient = H(ambient), SunLight = H(sunLight), CatLight = Color.Lerp(H(sunLight), Color.white, 0.35f),
            AmbientIntensity = ambientI, SunIntensity = sunI, Temperature = temp, Tint = tint, Saturation = sat,
            Shadows = H(shadows), Highlights = H(highlights), Exposure = exposure, FarHaze = farHaze, Fog = fog,
        };

        public static readonly IReadOnlyDictionary<string, Palette> ByName = new Dictionary<string, Palette>
        {
            // The concept's own light: low gold sun behind the ruins, teal-grey shade.
            ["GoldenHour"] = P("GoldenHour", "Gold", "#C98B57", "#FFE2A8", "#E4DCCC", "#FFF0D8", "#FFF4D6", "#8E9AB2", "#FFC880", 0.85f, 1.15f, 0f, 0f, -4f, "#4E6A88", "#C8A070", 0.3f, 0.62f, 0.5f),
            // A cool, pearly morning: lilac shade, peach light, thick mist.
            ["MorningMist"] = P("MorningMist", "Mist", "#B8B4C8", "#F6E6DA", "#E8DCE0", "#FFF4EC", "#FFF8EC", "#9AA0B8", "#FFD8B0", 0.85f, 0.75f, -6f, 4f, -4f, "#6A6E92", "#D8B8A0", 0.45f, 0.7f, 0.55f),
            // Green-gold canopy light, deep jade shade.
            ["JadeCanopy"] = P("JadeCanopy", "Jade", "#8FA888", "#F2EBC0", "#D8DDB0", "#F4F2D8", "#FFF6D0", "#7E9488", "#F2E2A0", 0.8f, 0.85f, 2f, -8f, 4f, "#3E6A5E", "#C8C080", 0.35f, 0.6f, 0.45f),
            // High clear noon over pale stone: the sky turns azure at the top.
            ["HighNoon"] = P("HighNoon", "Noon", "#7FA4C8", "#F4F0E2", "#E6EAEA", "#FFFFFF", "#FFFFF4", "#A8B0BC", "#FFF0D0", 0.95f, 0.7f, -4f, 0f, 2f, "#5A7898", "#D8C8A0", 0.3f, 0.5f, 0.2f),
            // Rose dusk: pink-gold sky, violet shade.
            ["RoseDusk"] = P("RoseDusk", "Rose", "#9A6A86", "#FFD2B0", "#F0C4B8", "#FFE0D8", "#FFE8D0", "#8A7898", "#FFB49A", 0.75f, 0.95f, 6f, 10f, 4f, "#5E4E7E", "#E0A088", 0.35f, 0.55f, 0.35f),
            // A storm breaking: slate clouds, one amber shaft of sun.
            ["AmberStorm"] = P("AmberStorm", "Storm", "#3E424E", "#E8B880", "#9C9490", "#D8D0C8", "#FFE0A8", "#6A7080", "#FFB060", 0.7f, 1.15f, 4f, 0f, -6f, "#3E4A5A", "#D89850", 0.25f, 0.45f, 0.5f),
            // Twilight: blue-violet sky, the last orange at the horizon.
            ["Twilight"] = P("Twilight", "Dusk", "#3A3660", "#F2A878", "#8A7898", "#C8B0C8", "#FFD0A0", "#6A6C9A", "#FFA070", 0.68f, 0.9f, -8f, 6f, 0f, "#3A4878", "#E09060", 0.25f, 0.5f, 0.4f),
            // Dawn: cool blue sky, a warm horizon.
            ["Dawn"] = P("Dawn", "Dawn", "#6A82B0", "#FFD8A8", "#C8CCD8", "#F0EEF4", "#FFF0D8", "#8C9AB8", "#FFCC98", 0.82f, 0.85f, -10f, 0f, 0f, "#4E6A9A", "#E0B080", 0.4f, 0.6f, 0.45f),
            // Ember sunset: deep red-orange, burnt shade.
            ["Ember"] = P("Ember", "Ember", "#7A2E24", "#FFB070", "#E89068", "#FFC8A0", "#FFE0B0", "#86707A", "#FF9050", 0.72f, 1.1f, 12f, 4f, 2f, "#4E3E56", "#E88848", 0.25f, 0.5f, 0.35f),
            // Moonlit: deep blue night, silver rims; the door's glow is the warm light.
            ["Moonlit"] = P("Moonlit", "Moon", "#0E1630", "#3A5078", "#4A5A7A", "#8A9AB8", "#E8F0FF", "#5A6A98", "#B8C8F0", 0.62f, 0.8f, -18f, 0f, -10f, "#2A3E6A", "#A8B8D8", 0.3f, 0.6f, 0.3f),
        };

        /// <summary>Each level's palette and signature (L001–L010 the full pass; L011–L020 the same system, its own variants).</summary>
        static readonly Dictionary<string, (string palette, string signature)> Levels = new()
        {
            ["L001"] = ("Dawn", "Colonnade"), ["L002"] = ("GoldenHour", "Aqueduct"), ["L003"] = ("JadeCanopy", "GreatTree"),
            ["L004"] = ("HighNoon", "Temple"), ["L005"] = ("RoseDusk", "FloatingIsles"), ["L006"] = ("MorningMist", "Waterfalls"),
            ["L007"] = ("AmberStorm", "Towers"), ["L008"] = ("Twilight", "Temple"), ["L009"] = ("Ember", "Ruins"),
            ["L010"] = ("Moonlit", "FloatingIsles"),
            ["L011"] = ("GoldenHour", "Ruins"), ["L012"] = ("MorningMist", "Colonnade"), ["L013"] = ("Dawn", "Waterfalls"),
            ["L014"] = ("RoseDusk", "FloatingIsles"), ["L015"] = ("JadeCanopy", "Aqueduct"), ["L016"] = ("HighNoon", "Towers"),
            ["L017"] = ("Twilight", "Ruins"), ["L018"] = ("AmberStorm", "GreatTree"), ["L019"] = ("Ember", "Temple"),
            ["L020"] = ("Moonlit", "Aqueduct"),
        };

        public static Palette For(string levelId) =>
            levelId != null && Levels.TryGetValue(levelId, out var e) ? ByName[e.palette] : ByName["GoldenHour"];

        public static string Signature(string levelId) =>
            levelId != null && Levels.TryGetValue(levelId, out var e) ? e.signature : "Ruins";
    }
}
