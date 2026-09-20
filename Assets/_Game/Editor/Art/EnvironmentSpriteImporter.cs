using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>Applies the Cat A importer baseline to environment PNGs as they arrive through LFS.</summary>
    public sealed class EnvironmentSpriteImporter : AssetPostprocessor
    {
        const string CatPath = "Assets/_Game/Art/Cats/CatA/CatA_Walk.png";

        void OnPreprocessTexture()
        {
            if (!IsEnvironmentArt(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = System.Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            var catImporter = AssetImporter.GetAtPath(CatPath) as TextureImporter;
            if (catImporter == null)
            {
                Debug.LogError("EnvironmentSpriteImporter: CatA_Walk TextureImporter is required.");
                return;
            }
            bool background = IsBackground(assetPath);
            bool material = IsMaterial(assetPath);
            importer.spritePixelsPerUnit = background || material ? catImporter.spritePixelsPerUnit * 0.5f : catImporter.spritePixelsPerUnit;
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
