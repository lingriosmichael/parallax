using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Editor.Levels
{
    /// <summary>PAX-A15: the environment's tunables. The grades (time of day: sky, sun, haze, light), how each background
    /// layer moves and how far it is pushed back into the haze (A1), and the far layers' scale (ruling 4: at most 1.5).
    /// Created with these defaults by PARALLAX/Setup/Levels/Environment Stack; the per-level recipes are LevelLooks.
    /// Its existence also switches the play-layer kit on in the room builder (as TrapArtConfig does for trap art).</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Level Look Config")]
    public sealed class LevelLookConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/LevelLookConfig.asset";
        public const float MaxFarScale = 1.5f;
        /// <summary>PAX-A16: bumped when the defaults change; the setup menu resets an older asset to them.</summary>
        public const int CurrentVersion = 22;

        [System.Serializable]
        public sealed class Grade
        {
            public string Name;
            public Color SkyTop, SkyHorizon;
            [Tooltip("The sun's centre in the view, 0..1 across and up, at the camera's mid travel.")]
            public Vector2 Sun;
            public float SunSize = 5f;
            public Color Haze;
            public Color Light;
            public float LightIntensity = 1f;
        }

        [System.Serializable]
        public sealed class Layer
        {
            public string Name;
            [Tooltip("ParallaxLayer.screenSpeed: 0 stays on the camera, 1 moves with the playfield.")]
            public float Speed;
            [Tooltip("How far the layer's colours go toward the grade's haze (0..1).")]
            public float HazeMix;
            [Tooltip("Brightness multiplier after the haze mix (A1: separation from the sandstone play layer).")]
            public float Value = 1f;
            public float Alpha = 1f;
            public int Order;
        }

        public List<Grade> Grades = new();
        public List<Layer> Layers = new();
        [Range(1f, MaxFarScale)] public float FarScale = MaxFarScale;
        [Tooltip("The door's warm glow (around its lit opening) and the soft dark pocket behind it that makes the door the most visible thing in the level (A6): sizes in units, opacities.")]
        public float DoorGlowSize = 2.2f;
        public float DoorGlowAlpha = 0.55f;
        public float DoorShadeSize = 5f;
        public float DoorShadeAlpha = 0.55f;
        [Tooltip("The flip ring's dark backing, as a multiple of the ring's size, and its opacity (A3).")]
        public float FlipHaloScale = 1.35f;
        public float FlipHaloAlpha = 1f;

        [Header("PAX-A16 value tiers")]
        public int Version;
        [Tooltip("The play layer's stone body (fill, wall faces, undersides): renderer tint, so a disguised trap copies it (P10).")]
        public Color BodyTint = new(0.9f, 0.87f, 0.82f, 1f);   // gauntlet: the painterly stone is darker than the old tiles
        [Tooltip("The walkable cap's material colour (HDR above 1 lifts the lip into the bloom).")]
        public Color CapBoost = new(0.98f, 0.92f, 0.84f, 1f);
        [Tooltip("Outside the room (walls out to the frame, under the ground, over the ceiling): dark masonry tint.")]
        public Color OutsideTint = new(0.3f, 0.31f, 0.37f, 1f);   // gauntlet: cool shade, not warm black
        [Tooltip("Hangers and supports: tint (dark, desaturated, no lip).")]
        public Color SupportTint = new(0.56f, 0.5f, 0.44f, 1f);
        [Tooltip("God rays: count, additive intensity, alpha.")]
        public int Rays = 3;
        public float RayIntensity = 1.1f;
        public float RayAlpha = 0.7f;
        [Tooltip("The sun's additive halo: size as a multiple of the sun, intensity (above 1 blooms).")]
        public float SunHaloScale = 3.2f;
        public float SunHaloIntensity = 1.3f;
        [Tooltip("Dust motes alive at once.")]
        public int Motes = 90;
        [Tooltip("The foreground frame: corner scale and the top curtain's scale (of their sprites), at a 13.33 u view (scaled with the view).")]
        public float FrameCornerScale = 1.7f;
        public float FrameTopScale = 0.16f;
        [Tooltip("Round 3: the hanging roots (top left) and banner (top right) pinned to the view, as a fraction of their painted size.")]
        public float FrameHangScale = 0.75f;
        [Tooltip("Round 3: the dark column at the view's left edge (scale, and how much of its width is in view).")]
        public float FramePillarScale = 0f, FramePillarShown = 0.46f;   // gauntlet: 0 = off (its lit edge read as a gap beside the floor)
        [Tooltip("The near tier's pieces: their scale (the paintings stay sharp up to about 1.4 at the phone's 81 px/u).")]
        public float NearScale = 1.0f;   // gauntlet: never above baked resolution
        [Tooltip("Round 3: how dark every block's face gets below its lit lip (the same for every block, real or disguised).")]
        [Range(0f, 1f)] public float FaceShadeAlpha = 0.5f;

        public Grade GetGrade(string name) => Grades.Find(g => g.Name == name);
        public Layer GetLayer(string name) => Layers.Find(l => l.Name == name);

        public static string[] LayerNames => new[] { "Sky", "Sun", "CloudsFar", "CloudsMid", "CloudsNear", "Far", "Haze", "Mid", "Veil", "BackWall", "Atmosphere", "Foreground", "FarCity", "Rays", "Near" };

        /// <summary>The defaults the setup menu writes. Colours follow LOOK_AND_FEEL §4: warm gold, shadows warm brown,
        /// never black, no cyan.</summary>
        public void ResetToDefaults()
        {
            // Every scalar back to its initializer (a fresh instance's), then the tables.
            var fresh = CreateInstance<LevelLookConfig>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh), this);
            DestroyImmediate(fresh);
            Grades = new List<Grade>
            {
                G("MorningGold", "#E7CFA0", "#FFF0CC", new Vector2(0.22f, 0.78f), "#F6E2B8", "#FFF4E2", 1.0f),
                G("NoonGold", "#DDC79C", "#FFF2D2", new Vector2(0.55f, 0.86f), "#F4E4C0", "#FFF6E8", 1.02f),
                G("WarmAfternoon", "#E2B57A", "#FFDDA6", new Vector2(0.74f, 0.74f), "#F2CF98", "#FFEFD6", 1.0f),
                G("Afternoon", "#D9A86C", "#FFD59A", new Vector2(0.80f, 0.66f), "#F0C68A", "#FFEACC", 0.98f),
                G("LateAfternoon", "#D2955A", "#FFC888", new Vector2(0.82f, 0.56f), "#EEB878", "#FFE2BC", 0.96f),
                G("GoldenHour", "#C27D4C", "#FFB66C", new Vector2(0.70f, 0.42f), "#EAA466", "#FFD9A8", 0.94f),
                G("DuskAmber", "#8E5A4C", "#F3A35C", new Vector2(0.62f, 0.30f), "#D9925E", "#FFCC9A", 0.9f),
                G("DuskRose", "#6A4A5C", "#E98E78", new Vector2(0.30f, 0.27f), "#C98476", "#FFC4A6", 0.88f),
            };
            Layers = new List<Layer>
            {
                L("Sky", 0f, 0f, 1f, 1f, -100),
                L("Sun", 0.02f, 0f, 1f, 1f, -95),
                L("CloudsFar", 0.04f, 0.45f, 1f, 0.85f, -90),
                L("CloudsMid", 0.07f, 0.3f, 0.97f, 0.9f, -88),
                L("CloudsNear", 0.10f, 0.2f, 0.93f, 0.95f, -86),
                // PAX-A16 tier 2: the far city, pale in the haze.
                L("FarCity", 0.10f, 0.3f, 0.98f, 0.95f, -84),
                L("Far", 0.15f, 0.3f, 0.92f, 1f, -80),
                L("Haze", 0.20f, 0f, 1f, 0.3f, -70),   // round 3: 0.55 washed the 1.8x view out
                // PAX-A16 §3.1: value falls toward the viewer. The mid layer is the air behind the play space (mid-tone).
                L("Mid", 0.35f, 0.32f, 0.9f, 1f, -50),
                // A16: the A15 veil is gone (alpha 0 skips it).
                L("Veil", 0f, 1f, 1f, 0f, -45),
                // A16 round 2: the near tier, ruins and trees between the mid layer and the play space, bigger and less hazed
                // (behind a back wall, so an interior shows it only through its windows).
                L("Near", 0.6f, 0.4f, 0.82f, 1f, -47),   // gauntlet: in front of the mid veil (Mid order + 2)
                L("BackWall", 1f, 0.18f, 0.78f, 1f, -46),   // round 3: deeper, so interiors aren't a pale flat wall
                L("Rays", 0.04f, 0f, 1f, 1f, -30),   // P1-R5: with the sun (on screen at every camera position)
                L("Atmosphere", 0.6f, 0f, 1f, 0.25f, -20),
                L("Foreground", 1.1f, 0f, 0.14f, 1f, 10),
            };
            Version = CurrentVersion;
            FarScale = MaxFarScale;
        }

        static Grade G(string name, string top, string horizon, Vector2 sun, string haze, string light, float intensity) =>
            new() { Name = name, SkyTop = Hex(top), SkyHorizon = Hex(horizon), Sun = sun, Haze = Hex(haze), Light = Hex(light), LightIntensity = intensity };

        static Layer L(string name, float speed, float haze, float value, float alpha, int order) =>
            new() { Name = name, Speed = speed, HazeMix = haze, Value = value, Alpha = alpha, Order = order };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }
}
