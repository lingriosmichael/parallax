using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-A15 §2.2, as amended by D-103 (the gauntlet): every kit slot env_kit.json lists is imported at the fixed
    // environment PPU (196.667 for the play layer and its fills; 128 for layers, finer than a phone at the 1.8x camera),
    // Full Rect, repeating only along its tiling axis, compressed for Android within the budget's formats, mipmapped (drawn
    // smaller than baked, so no sparkle; not the stone fills, whose disguised shards must match their skin exactly), and (after PARALLAX/Setup/Levels/Environment Stack) carries its normal map as the
    // _NormalMap secondary texture.
    public sealed class EnvironmentKitImportTests
    {
        const string KitFolder = "Assets/_Game/Art/RealityA/Environment/Kit";
        const string LayerFolder = "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit";
        const float WorldPpu = 196.66666f, LayerPpu = 128f;
        static readonly string[] Horizontal = { "ENV_Cap", "ENV_Under", "ENV_Slab", "ENV_Water", "ENV_CloudsFar", "ENV_CloudsMid", "ENV_CloudsNear", "ENV_Haze", "ENV_Fog", "ENV_MidAqueduct", "ENV_BackWall",
            "ENV_CapWash", "ENV_FarCity", "ENV_FrameTop", "ENV_ArchFringe", "ENV_LipVines", "ENV_ArchShade", "ENV_ThickUnder" };   // PAX-A16 tiers
        static readonly string[] Vertical = { "ENV_Side", "ENV_Post", "ENV_SlimPost", "ENV_VineMid", "ENV_Waterfall", "ENV_Chain" };

        [System.Serializable] sealed class Slot { public string name, file, folder; public bool normal; }
        [System.Serializable] sealed class Manifest { public float worldPpu; public Slot[] slots; }

        static Manifest Load()
        {
            string path = KitFolder + "/env_kit.json";
            Assert.IsTrue(File.Exists(path), path + " is missing; run Tools/Art/env_kit.py.");
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        }

        static string PathOf(Slot s) => (s.folder == "Kit" ? KitFolder : LayerFolder) + "/" + s.file;

        [Test]
        public void EverySlot_IsImportedWithTheFixedPpuAndItsTilingAxis()
        {
            Manifest manifest = Load();
            Assert.AreEqual(WorldPpu, manifest.worldPpu, 1e-3f, "the kit was cut for a different PPU");
            var failures = new List<string>();
            foreach (Slot s in manifest.slots)
            {
                var importer = AssetImporter.GetAtPath(PathOf(s)) as TextureImporter;
                if (importer == null) { failures.Add(s.name + ": not imported"); continue; }
                bool layer = s.folder != "Kit";
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, layer ? LayerPpu : WorldPpu)) failures.Add($"{s.name}: PPU {importer.spritePixelsPerUnit}");
                if (importer.textureType != TextureImporterType.Sprite) failures.Add($"{s.name}: not a sprite");
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect) failures.Add($"{s.name}: mesh {settings.spriteMeshType}");
                // PAX-106 (D-114): the broken-stone side strips tile down a face; the cut thick underside across.
                bool fill = s.name.StartsWith("ENV_Fill_"), u = fill || Horizontal.Contains(s.name) || s.name == "ENV_ThickUnderShort",
                    v = fill || Vertical.Contains(s.name) || s.name.StartsWith("ENV_SideChip_");
                if ((importer.wrapModeU == TextureWrapMode.Repeat) != u || (importer.wrapModeV == TextureWrapMode.Repeat) != v)
                    failures.Add($"{s.name}: wrap {importer.wrapModeU}/{importer.wrapModeV}");
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                TextureImporterFormat wanted = s.folder == "Kit" ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
                if (!android.overridden || android.format != wanted) failures.Add($"{s.name}: Android {android.format}");
                bool wantMips = !s.name.StartsWith("ENV_Fill_");   // D-103: fills stay unmipmapped (P10, the reveal frame)
                if (importer.mipmapEnabled != wantMips) failures.Add($"{s.name}: mipmaps {importer.mipmapEnabled}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void EveryNormalMap_IsANormalMapAndItsSpritesSecondaryTexture()
        {
            var failures = new List<string>();
            foreach (Slot s in Load().slots.Where(s => s.normal))
            {
                string normalPath = PathOf(s).Replace(".png", "_n.png");
                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (normalImporter == null) { failures.Add(s.name + ": no normal map"); continue; }
                if (normalImporter.textureType != TextureImporterType.NormalMap) failures.Add($"{s.name}_n: type {normalImporter.textureType}");
                var importer = (TextureImporter)AssetImporter.GetAtPath(PathOf(s));
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                SecondarySpriteTexture[] secondary = importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0];
                if (!secondary.Any(t => t.name == "_NormalMap" && t.texture == normal)) failures.Add($"{s.name}: _NormalMap not attached (run PARALLAX/Setup/Levels/Environment Stack)");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
