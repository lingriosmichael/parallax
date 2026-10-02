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
            // Round 5: the cat's key light 0.9 (1.5 lifted the black cat to brown wherever the sun's light also reached it; a
            // stronger moon key at night lifted it to the grey of the haze behind it, so the night reads by its lighter haze).
            public float AmbientIntensity = 0.85f, SunIntensity = 0.9f, CatIntensity = 0.9f;
            /// <summary>The sun disc in the view at mid travel (0..1 across and up) and its size in units; a moon if Moon.</summary>
            public Vector2 SunAt = new(0.68f, 0.7f);
            public float SunSize = 3.2f;
            public bool Moon;
            /// <summary>Post: white balance, split toning (cool shadows, warm highlights), colour adjustments, bloom.</summary>
            public float Temperature, Tint, Saturation, Contrast = 18f, Exposure = 0.35f;
            /// <summary>Phase 2 round 5: no bloom. Its glow off the bright skies veiled the cat standing against them (the cat's
            /// darkest fifth 40-45 with it, 22-26 without; the sprite's own is 21,15,12), and it cost SetPass.</summary>
            public float Bloom = 0f;
            public Color Shadows = new(0.45f, 0.5f, 0.56f), Highlights = new(0.58f, 0.52f, 0.44f);
            /// <summary>How much of the far distance the haze swallows (0..1), and the low fog's strength.</summary>
            public float FarHaze = 0.55f, Fog = 0.35f;
        }

        /// <summary>Gauntlet round 2 (the developer: "colour grading must never tint the cat or the hazards"): the most a 2D
        /// light's colour may be saturated (HSV). The lights fall on the hazards' lit sprites, so a level's light keeps its warm
        /// or cool lean but never tints them; the level's colour lives in its sky, haze and background pieces.</summary>
        public const float LightSaturationCap = 0.15f;

        public static Color CapLight(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            Color capped = Color.HSVToRGB(h, Mathf.Min(s, LightSaturationCap), v);
            capped.a = c.a;
            return capped;
        }

        /// <summary>Gauntlet round 2: the level's warm/cool lean, which the post's white balance used to carry, as a gentle
        /// multiplier on the environment's own pieces (sky, haze, skyline, background), never on the cat or the hazards.
        /// From the palette's Temperature and Tint (±20 at most), normalised so it never brightens.</summary>
        public static Color EnvGrade(Palette p)
        {
            if (p == null) return Color.white;
            float t = Mathf.Clamp(p.Temperature, -20f, 20f) / 20f, n = Mathf.Clamp(p.Tint, -20f, 20f) / 20f;
            var c = new Color(1f + 0.16f * t + 0.04f * n, 1f - 0.1f * n, 1f - 0.16f * t + 0.04f * n);
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return new Color(c.r / m, c.g / m, c.b / m, 1f);
        }

        static Color H(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        static Palette P(string name, string sky, string top, string horizon, string haze, string clouds, string sun, string ambient, string sunLight,
            float ambientI, float sunI, float temp, float tint, float sat, string shadows, string highlights, float exposure = 0.35f, float farHaze = 0.55f, float fog = 0.35f) => new()
        {
            Name = name, Sky = sky, SkyTop = H(top), Horizon = H(horizon), Haze = H(haze), Clouds = H(clouds), Sun = H(sun),
            Ambient = H(ambient), SunLight = H(sunLight), CatLight = Color.Lerp(H(sunLight), Color.white, 0.35f),
            AmbientIntensity = ambientI, SunIntensity = sunI, Temperature = temp, Tint = tint, Saturation = sat,
            Shadows = H(shadows), Highlights = H(highlights), Exposure = exposure, FarHaze = farHaze, Fog = fog,
            Moon = name == "Moonlit",   // round 3: the night palette's disc is a moon (a smaller cool halo, no sunbeams)
        };

        public static readonly IReadOnlyDictionary<string, Palette> ByName = new Dictionary<string, Palette>
        {
            // The concept's own light: low gold sun behind the ruins, teal-grey shade.
            ["GoldenHour"] = P("GoldenHour", "Gold", "#C98B57", "#FFE2A8", "#E4DCCC", "#FFF0D8", "#FFF4D6", "#8E9AB2", "#FFC880", 0.85f, 1.15f, 0f, 0f, -4f, "#4E6A88", "#C8A070", 0.3f, 0.62f, 0.5f),
            // A cool, pearly morning: lilac shade, peach light, thick mist.
            ["MorningMist"] = P("MorningMist", "Mist", "#B8B4C8", "#F6E6DA", "#E8DCE0", "#FFF4EC", "#FFF8EC", "#9AA0B8", "#FFD8B0", 0.85f, 0.75f, -6f, 4f, -4f, "#6A6E92", "#D8B8A0", 0.3f, 0.6f, 0.42f),
            // Green-gold canopy light, deep jade shade.
            ["JadeCanopy"] = P("JadeCanopy", "Jade", "#8FA888", "#F2EDD4", "#DADCC6", "#F4F2D8", "#FFF6D0", "#7E9488", "#F2E2A0", 0.8f, 0.85f, 1f, -4f, 2f, "#3E6A5E", "#C8C080", 0.35f, 0.6f, 0.45f),
            // High clear noon over pale stone: the sky turns azure at the top.
            // Round 3 (critics: "washed blue-white sky, empty"): a deeper azure top and less haze.
            ["HighNoon"] = P("HighNoon", "Noon", "#4F7EB8", "#F4EAD2", "#DCE4EC", "#FFFFFF", "#FFFFF4", "#A8B0BC", "#FFF0D0", 0.95f, 0.75f, -4f, 0f, 4f, "#4A6A90", "#D8C8A0", 0.12f, 0.32f, 0.18f),
            // Rose dusk: pink-gold sky, violet shade.
            ["RoseDusk"] = P("RoseDusk", "Rose", "#8E7A88", "#FFD8BE", "#EECFC6", "#FFE0D8", "#FFE8D0", "#8A7898", "#FFB49A", 0.75f, 0.95f, 3f, 5f, 2f, "#5E4E7E", "#E0A088", 0.35f, 0.55f, 0.35f),
            // A storm breaking: slate clouds, one amber shaft of sun.
            ["AmberStorm"] = P("AmberStorm", "Storm", "#3E424E", "#E8B880", "#9C9490", "#D8D0C8", "#FFE0A8", "#6A7080", "#FFB060", 0.7f, 1.15f, 4f, 0f, -6f, "#3E4A5A", "#D89850", 0.25f, 0.45f, 0.5f),
            // Twilight: blue-violet sky, the last orange at the horizon.
            ["Twilight"] = P("Twilight", "Dusk", "#3A3660", "#F2A878", "#8A7898", "#C8B0C8", "#FFD0A0", "#6A6C9A", "#FFA070", 0.68f, 0.9f, -8f, 6f, 0f, "#3A4878", "#E09060", 0.25f, 0.5f, 0.4f),
            // Dawn: cool blue sky, a warm horizon.
            ["Dawn"] = P("Dawn", "Dawn", "#6A82B0", "#FFD8A8", "#C8CCD8", "#F0EEF4", "#FFF0D8", "#8C9AB8", "#FFCC98", 0.82f, 0.85f, -10f, 0f, 0f, "#4E6A9A", "#E0B080", 0.28f, 0.55f, 0.38f),
            // Ember sunset: deep red-orange, burnt shade.
            ["Ember"] = P("Ember", "Ember", "#7A2E24", "#FFB070", "#E89068", "#FFC8A0", "#FFE0B0", "#86707A", "#FF9050", 0.72f, 1.1f, 12f, 4f, 2f, "#4E3E56", "#E88848", 0.25f, 0.5f, 0.35f),
            // Phase 2 round 2: blue hour, deep teal over a warm horizon (L020; it shared Moonlit with L010).
            ["BlueHour"] = P("BlueHour", "Teal", "#16303C", "#E8B88C", "#5C7C88", "#9CB4BC", "#FFE0B8", "#5E7C8C", "#FFC898", 0.66f, 0.8f, -6f, 0f, -2f, "#26485A", "#E0A878", 0.25f, 0.55f, 0.4f),
            // Moonlit: deep blue night, silver rims; the door's glow is the warm light.
            // Round 5: a lighter horizon and haze (the black cat stood on dark ruins in a dark sky, contrast 2).
            ["Moonlit"] = P("Moonlit", "Moon", "#0E1630", "#4E6690", "#7486AC", "#8A9AB8", "#C4D2F2", "#5A6A98", "#B8C8F0", 0.62f, 0.8f, -18f, 0f, -10f, "#2A3E6A", "#A8B8D8", 0.3f, 0.6f, 0.3f),
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
            ["L020"] = ("BlueHour", "Aqueduct"),
        };

        public static Palette For(string levelId) =>
            levelId != null && Levels.TryGetValue(levelId, out var e) ? ByName[e.palette] : ByName["GoldenHour"];

        public static string Signature(string levelId) =>
            levelId != null && Levels.TryGetValue(levelId, out var e) ? e.signature : "Ruins";
    }
}
