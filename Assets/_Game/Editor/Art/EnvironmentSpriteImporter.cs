using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>Applies the environment importer baseline to environment PNGs as they arrive through LFS.</summary>
    public sealed class EnvironmentSpriteImporter : AssetPostprocessor
    {
        /// <summary>PAX-A08 §4.4 (ruled 2026-09-30): the environment's PPU is fixed at the value A02 records (the old Cat A
        /// PPU, 196.667), no longer read from CatA_Walk's importer: the cat's PPU changed with its new art, and following
        /// it would have rescaled every later environment import.</summary>
        public const float WorldPixelsPerUnit = 196.66666f;
        /// <summary>Gauntlet: backgrounds at 128 px/u, finer than a 20:9 phone at the 1.8x camera (122), so no layer is drawn
        /// upsampled; fills (baked at the play layer's PPU) use WorldPixelsPerUnit.</summary>
        public const float BackgroundPixelsPerUnit = 128f;

        // Gauntlet: bumped when the import rules change, so every environment texture reimports under them.
        public override uint GetVersion() => 4;

        /// <summary>The A02 material tiles keep their original half PPU (98.333); the kit's fills are baked at the play layer's.</summary>
        public const float MaterialPixelsPerUnit = WorldPixelsPerUnit * 0.5f;

        public static float PixelsPerUnitFor(string assetPath) =>
            IsBackground(assetPath) ? BackgroundPixelsPerUnit
            : IsMaterial(assetPath) && !System.IO.Path.GetFileNameWithoutExtension(assetPath).StartsWith("ENV_Fill_") ? MaterialPixelsPerUnit
            : WorldPixelsPerUnit;

        void OnPreprocessTexture()
        {
            if (!IsEnvironmentArt(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            // PAX-A15: a kit normal map (<slot>_n.png) is the sprite's _NormalMap secondary texture, not a sprite.
            if (IsKitNormal(assetPath))
            {
                importer.textureType = TextureImporterType.NormalMap;
                // Its sprite's PPU too (unused by a normal map, but every environment texture carries the fixed PPU).
                importer.spritePixelsPerUnit = PixelsPerUnitFor(assetPath.Replace("_n.png", ".png"));
                importer.mipmapEnabled = true;   // gauntlet: drawn smaller than baked, so mipmapped (no sparkle, no crunch)
                importer.filterMode = FilterMode.Bilinear;
                ApplyWrap(importer, assetPath.Replace("_n.png", ".png"));
                SetAndroid(importer, TextureImporterFormat.ASTC_6x6);
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = System.Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            bool background = IsBackground(assetPath);
            importer.spritePixelsPerUnit = PixelsPerUnitFor(assetPath);
            importer.filterMode = FilterMode.Bilinear;
            // Gauntlet: every environment texture is drawn at or below its baked resolution on a phone; without mipmaps the
            // minified stone sparkled and read as crunchy. Except the material tiles (A02's and the kit's stone fills): a
            // disguise samples them through the trap shader, which wraps an atlased sprite with frac(), and with mipmaps the
            // wrap's derivative jump draws a 1 px seam (P10, the reveal frame).
            importer.mipmapEnabled = !IsMaterial(assetPath);
            importer.textureCompression = background ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 50;
            importer.wrapMode = IsTileable(assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (IsKit(assetPath)) ApplyWrap(importer, assetPath);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            if (background) SetAndroid(importer, TextureImporterFormat.ASTC_6x6);
            // PAX-A15: the play kit stays lossless in the Editor and is ASTC 4x4 on the device (the 32 MB budget).
            else if (IsKit(assetPath)) SetAndroid(importer, TextureImporterFormat.ASTC_4x4);
        }

        static void SetAndroid(TextureImporter importer, TextureImporterFormat format)
        {
            var android = new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 4096,
                format = format, compressionQuality = 50
            };
            importer.SetPlatformTextureSettings(android);
        }

        /// <summary>PAX-A15: a kit strip repeats along its tiling axis only (a cap repeats sideways, a wall face down), so
        /// bilinear filtering never bleeds its top row into its bottom one; a fill repeats both ways.</summary>
        static void ApplyWrap(TextureImporter importer, string spritePath)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(spritePath);
            bool horizontal = System.Array.IndexOf(KitHorizontal, file) >= 0, vertical = System.Array.IndexOf(KitVertical, file) >= 0
                // PAX-106 (D-114): the broken-stone side strips tile down a face, as ENV_Side does.
                || file.StartsWith("ENV_SideChip_") && !file.EndsWith("_n");
            bool fill = file.StartsWith("ENV_Fill_");
            importer.wrapModeU = fill || horizontal ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.wrapModeV = fill || vertical ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        }

        static readonly string[] KitHorizontal = { "ENV_Cap", "ENV_Under", "ENV_Slab", "ENV_Water", "ENV_CloudsFar", "ENV_CloudsMid", "ENV_CloudsNear", "ENV_Haze", "ENV_Fog", "ENV_MidAqueduct", "ENV_BackWall", "ENV_CapWash", "ENV_FarCity", "ENV_FrameTop", "ENV_ArchFringe", "ENV_LipVines", "ENV_ArchShade", "ENV_ThickUnder", "ENV_ThickUnderShort" };
        static readonly string[] KitVertical = { "ENV_Side", "ENV_Post", "ENV_SlimPost", "ENV_VineMid", "ENV_Waterfall", "ENV_Chain" };

        static bool IsKit(string path) => path.Contains("/Environment/Kit/") || path.Contains("/Environment/Backgrounds/Kit/");
        static bool IsKitNormal(string path) => IsKit(path) && path.EndsWith("_n.png");

        static bool IsEnvironmentArt(string path) =>
            path.StartsWith("Assets/_Game/Art/RealityA/Environment/") || path.StartsWith("Assets/_Game/Art/RealityB/Environment/");

        static bool IsBackground(string path) => path.Contains("/Environment/Backgrounds/");
        static bool IsMaterial(string path)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.Contains("_GAME_Platform_Fill") || file.Contains("_GAME_Wall") || file.StartsWith("ENV_Fill_");
        }
        static bool IsTileable(string path)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.Contains("_BG_") || file.Contains("_MG_") || file.Contains("_GAME_") || file.Contains("_OBJ_Hazard");
        }
    }
}
