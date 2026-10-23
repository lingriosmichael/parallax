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

        // D-162: the geyser's steam (a sprite per tick, both directions) is complete and packed in its own atlas, at most
        // 2048 x 2048 a page, beside the kit's.
        [Test]
        public void GeyserSteam_IsCompleteAndInItsOwnAtlasOfAtMost2048()
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(ConfigPath);
            Assert.NotNull(config, $"no TrapArtConfig at {ConfigPath} (PARALLAX/Art/Import Trap Kit writes it)");
            const string steamFolder = "Assets/_Game/Art/Geyser";
            string[] atlases = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { steamFolder });
            Assert.AreEqual(1, atlases.Length, $"steam atlases under {steamFolder}");
            string atlasPath = AssetDatabase.GUIDToAssetPath(atlases[0]);
            var importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
            Assert.NotNull(importer, atlasPath + " is not a Sprite Atlas V2");
            Assert.That(importer.GetPlatformSettings("DefaultTexturePlatform").maxTextureSize, Is.LessThanOrEqualTo(MaxAtlasSize), "the steam atlas's maximum size");
            var packed = new HashSet<string>(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath).GetPackables().Select(AssetDatabase.GetAssetPath));

            var problems = new List<string>();
            foreach ((string dir, GeyserSteamFrames f) in new[] { ("Up", config.GeyserSteamUp), ("Down", config.GeyserSteamDown) })
            {
                if (f.Tell.Length != Parallax.Core.GeyserMath.DefaultTellTicks) problems.Add($"{dir}: {f.Tell.Length} tell frames (one per tell tick)");
                if (f.Core.Length != Parallax.Core.GeyserMath.DefaultEruptTicks || f.Plume.Length != f.Core.Length) problems.Add($"{dir}: {f.Core.Length} core and {f.Plume.Length} plume frames (one per erupt tick)");
                if (f.Fade.Length != Parallax.Core.Presentation.TrapArtMath.GeyserFadeTicks) problems.Add($"{dir}: {f.Fade.Length} fade frames");
                foreach (Sprite sprite in f.Tell.Concat(f.Core).Concat(f.Plume).Concat(f.Fade))
                {
                    string path = sprite != null ? AssetDatabase.GetAssetPath(sprite) : "";
                    if (sprite == null) problems.Add($"{dir}: a missing frame");
                    else if (!packed.Any(p => path == p || path.StartsWith(p.TrimEnd('/') + "/"))) problems.Add($"{sprite.name} ({path}) outside the steam atlas");
                }
                // The body never reaches past the column art's box: 1.1 x 1.5 u at most.
                foreach (Sprite core in f.Core.Where(c => c != null))
                    if (core.bounds.size.x > 1.1f + 0.02f || core.bounds.size.y > 1.5f + 0.02f) problems.Add($"{core.name} is {core.bounds.size}, past the column's 1.1 x 1.5");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        static IEnumerable<Sprite> Sprites(TrapArtConfig c)
        {
            var all = new List<Sprite> { c.ArrowLauncher, c.Arrow, c.Glint, c.GeyserVent };
            all.AddRange(c.GeyserColumnFrames); all.AddRange(c.Dust); all.AddRange(c.Spray);
            return all.Where(s => s != null);
        }
    }
}
