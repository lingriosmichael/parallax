using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Parallax.Tests.EditMode
{
    // PAX-A13 (§9.1, §11 R6): the interim budget until PAX-V03. The trap kit's sprites are packed in one atlas, at most
    // 2048 x 2048; every sprite the config hands the builder is in it. (The live-effect cap, at most 150 at once in the
    // busiest moment, is measured tick by tick in TrapArtParityTests.)
    public sealed class TrapArtBudgetTests
    {
        const string ConfigPath = "Assets/_Game/Data/TrapArtConfig.asset";
        const string ArtFolder = "Assets/_Game/Art/Traps";
        const int MaxAtlasSize = 2048;

        [Test]
        public void TrapKit_IsOneAtlasOfAtMost2048_HoldingEveryConfiguredSprite()
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(ConfigPath);
            Assert.NotNull(config, $"no TrapArtConfig at {ConfigPath} (PARALLAX/Art/Import Trap Kit writes it)");
            string[] atlases = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { ArtFolder });
            Assert.AreEqual(1, atlases.Length, $"trap atlases under {ArtFolder}");
            string atlasPath = AssetDatabase.GUIDToAssetPath(atlases[0]);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            // Sprite Atlas V2 (the project's packer mode): the settings live on its importer.
            var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
            Assert.NotNull(importer, atlasPath + " is not a Sprite Atlas V2");
            TextureImporterPlatformSettings settings = importer.GetPlatformSettings("DefaultTexturePlatform");
            Assert.That(settings.maxTextureSize, Is.LessThanOrEqualTo(MaxAtlasSize), "the atlas's maximum size");

            var packed = new HashSet<string>(atlas.GetPackables().Select(AssetDatabase.GetAssetPath));
            var missing = new List<string>();
            foreach (Sprite sprite in Sprites(config))
            {
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!packed.Any(p => path == p || path.StartsWith(p.TrimEnd('/') + "/"))) missing.Add(sprite.name + " (" + path + ")");
            }
            Assert.IsEmpty(missing, "configured sprites outside the trap atlas:\n" + string.Join("\n", missing));
        }

        static IEnumerable<Sprite> Sprites(TrapArtConfig c)
        {
            var all = new List<Sprite> { c.ArrowLauncher, c.Arrow, c.Glint, c.GeyserVent };
            all.AddRange(c.GeyserColumnFrames); all.AddRange(c.Dust); all.AddRange(c.Spray);
            return all.Where(s => s != null);
        }
    }
}
