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
        /// <summary>Backgrounds and materials use half (98.333).</summary>
        public const float BackgroundPixelsPerUnit = WorldPixelsPerUnit * 0.5f;

        public static float PixelsPerUnitFor(string assetPath) =>
            IsBackground(assetPath) || IsMaterial(assetPath) ? BackgroundPixelsPerUnit : WorldPixelsPerUnit;

        void OnPreprocessTexture()
        {
            if (!IsEnvironmentArt(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = System.Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            bool background = IsBackground(assetPath);
            importer.spritePixelsPerUnit = PixelsPerUnitFor(assetPath);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = background ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 50;
            importer.wrapMode = IsTileable(assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            if (background)
            {
                var android = new TextureImporterPlatformSettings
                {
                    name = "Android", overridden = true, maxTextureSize = 4096,
                    format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50
                };
                importer.SetPlatformTextureSettings(android);
            }
        }

        static bool IsEnvironmentArt(string path) =>
            path.StartsWith("Assets/_Game/Art/RealityA/Environment/") || path.StartsWith("Assets/_Game/Art/RealityB/Environment/");

        static bool IsBackground(string path) => path.Contains("/Environment/Backgrounds/");
        static bool IsMaterial(string path)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.Contains("_GAME_Platform_Fill") || file.Contains("_GAME_Wall");
        }
        static bool IsTileable(string path)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            return file.Contains("_BG_") || file.Contains("_MG_") || file.Contains("_GAME_") || file.Contains("_OBJ_Hazard");
        }
    }
}
