using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§6.5): PARALLAX/Art/Import Trap Kit. Takes the post-processed sprites (Tools/Art/trap_process.py and
    /// its manifest, Assets/_Game/Art/Traps/trap_kit.json) into the game: each sprite's importer (128 px per unit, its pivot,
    /// a full-rect mesh, no mipmaps, its normal map as the _NormalMap secondary texture), the trap material
    /// (Parallax/2D/Sprite-Lit-Flip), one Sprite Atlas V2 of at most 2048 x 2048, and the TrapArtConfig the level builder
    /// reads. Idempotent: running it again changes nothing. Then Rebuild All Levels puts the art in the scenes.</summary>
    public static class TrapKitImport
    {
        public const string Folder = "Assets/_Game/Art/Traps";
        const string ManifestPath = Folder + "/trap_kit.json";
        const string MaterialPath = Folder + "/TrapArt.mat";
        const string AtlasPath = Folder + "/TrapKit.spriteatlasv2";
        const string ShaderName = "Parallax/2D/Sprite-Lit-Flip";
        const string WorldTileMaterialPath = Folder + "/TrapArtWorldTile.mat";
        const int MaxAtlas = 2048;

        [System.Serializable] class Entry { public string path; public float pivotX, pivotY; public bool tiled; public string normal; public string placeholder; }
        const int KitVersion = 2;
        [System.Serializable] class Manifest { public float ppu; public Entry[] entries; }

        [MenuItem("PARALLAX/Art/Import Trap Kit")]
        public static void Import()
        {
            var changes = new List<string>();
            if (!File.Exists(ManifestPath)) { Debug.LogError($"Import Trap Kit: no {ManifestPath}; run Tools/Art/trap_process.py first."); return; }
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest?.entries == null || manifest.entries.Length == 0) { Debug.LogError("Import Trap Kit: the manifest lists no sprites."); return; }

            // Normal maps first, so the sprites can name them as secondary textures.
            foreach (Entry e in manifest.entries) if (!string.IsNullOrEmpty(e.normal)) ImportNormal(e.normal, changes);
            foreach (Entry e in manifest.entries) ImportSprite(e.path, new Vector2(e.pivotX, e.pivotY), e.tiled, string.IsNullOrEmpty(e.normal) ? null : e.normal, manifest.ppu, changes);

            Material material = EnsureMaterial(MaterialPath, ShaderName, "TrapArt", changes);
            Material worldTile = EnsureMaterial(WorldTileMaterialPath, Parallax.Presentation.HostSkin.WorldTileShader, "TrapArtWorldTile", changes);
            EnsureAtlas(changes);
            EnsureConfig(material, worldTile, manifest, changes);
            AssetDatabase.SaveAssets();
            Debug.Log($"Import Trap Kit: {manifest.entries.Length} sprites; {(changes.Count == 0 ? "no changes" : string.Join("; ", changes))}");
        }

        static void ImportNormal(string path, List<string> changes)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("Import Trap Kit: no texture at " + path); return; }
            bool dirty = false;
            if (importer.textureType != TextureImporterType.NormalMap) { importer.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            // The atlas compresses; its sources stay lossless.
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (!dirty) return;
            importer.SaveAndReimport();
            changes.Add("imported " + Path.GetFileName(path) + " as a normal map");
        }

        static void ImportSprite(string path, Vector2 pivot, bool tiled, string normalPath, float ppu, List<string> changes)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("Import Trap Kit: no texture at " + path); return; }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool dirty = importer.textureType != TextureImporterType.Sprite || settings.spriteMode != (int)SpriteImportMode.Single
                || !Mathf.Approximately(settings.spritePixelsPerUnit, ppu) || settings.spriteAlignment != (int)SpriteAlignment.Custom
                || settings.spritePivot != pivot || settings.spriteMeshType != SpriteMeshType.FullRect || importer.mipmapEnabled
                || !importer.alphaIsTransparency || importer.filterMode != FilterMode.Bilinear
                || importer.textureCompression != TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Sprite;
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = ppu;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;   // Tiled draw mode needs it; small sprites lose nothing by it
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;   // the atlas compresses; its sources stay lossless

            Texture2D normal = normalPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) : null;
            SecondarySpriteTexture[] wanted = normal != null ? new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } } : new SecondarySpriteTexture[0];
            SecondarySpriteTexture[] current = importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0];
            if (current.Length != wanted.Length || current.Where((c, i) => c.name != wanted[i].name || c.texture != wanted[i].texture).Any())
            {
                importer.secondarySpriteTextures = wanted;
                dirty = true;
            }
            if (!dirty) return;
            importer.SaveAndReimport();
            changes.Add("imported " + Path.GetFileName(path));
        }

        static Material EnsureMaterial(string path, string shaderName, string name, List<string> changes)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError($"Import Trap Kit: shader '{shaderName}' not found (Assets/_Game/Art/Traps/Shaders)."); return null; }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
                changes.Add("created " + path);
            }
            else if (material.shader != shader) { material.shader = shader; EditorUtility.SetDirty(material); changes.Add("set " + name + ".mat's shader"); }
            return material;
        }

        static void EnsureAtlas(List<string> changes)
        {
            Object[] folders = { AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder + "/Bodies"), AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder + "/Effects") };
            if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) == null)
            {
                var asset = new SpriteAtlasAsset();
                asset.Add(folders);
                SpriteAtlasAsset.Save(asset, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
                changes.Add("created " + AtlasPath);
            }
            var importer = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
            if (importer == null) { Debug.LogError("Import Trap Kit: " + AtlasPath + " isn't a Sprite Atlas V2."); return; }
            bool dirty = false;
            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            if (platform.maxTextureSize != MaxAtlas) { platform.maxTextureSize = MaxAtlas; importer.SetPlatformSettings(platform); dirty = true; }
            SpriteAtlasPackingSettings packing = importer.packingSettings;
            if (packing.enableRotation || packing.enableTightPacking || packing.padding != 4)
            {
                packing.enableRotation = false; packing.enableTightPacking = false; packing.padding = 4;
                importer.packingSettings = packing; dirty = true;
            }
            SpriteAtlasTextureSettings texture = importer.textureSettings;
            if (texture.generateMipMaps || texture.filterMode != FilterMode.Bilinear)
            {
                texture.generateMipMaps = false; texture.filterMode = FilterMode.Bilinear;
                importer.textureSettings = texture; dirty = true;
            }
            if (!dirty) return;
            importer.SaveAndReimport();
            changes.Add("configured the trap atlas");
        }

        static void EnsureConfig(Material material, Material worldTile, Manifest manifest, List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TrapArtConfig>();
                AssetDatabase.CreateAsset(config, TrapArtSetup.ConfigPath);
                changes.Add("created " + TrapArtSetup.ConfigPath);
            }
            Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
            Sprite[] Many(string prefix) => AssetDatabase.FindAssets("t:Sprite", new[] { Folder + "/Effects" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p).StartsWith(prefix)).OrderBy(p => p, System.StringComparer.Ordinal)
                .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray();

            var so = new SerializedObject(config);
            void Ref(string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
            void Refs(string field, Object[] values)
            {
                SerializedProperty p = so.FindProperty(field);
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            Ref("TrapMaterial", material);
            Ref("WorldTileMaterial", worldTile);
            Ref("ArrowLauncher", S("Bodies/TRAP-01_arrow_launcher"));
            Ref("Arrow", S("Bodies/TRAP-02_arrow"));
            Ref("Glint", S("Effects/glint"));
            Ref("GeyserVent", S("Bodies/TRAP-03_geyser_vent"));
            Refs("GeyserColumnFrames", Many("geyser_column_"));
            Refs("Dust", Many("dust_"));
            Refs("Spray", Many("spray_"));
            Ref("SpikeStrip", S("Bodies/TRAP-04_spike_strip"));
            Ref("SpearHead", S("Bodies/TRAP-05_spear_head"));
            Ref("SpearShaft", S("Bodies/TRAP-05_spear_shaft"));
            Ref("InverterOrb", S("Bodies/TRAP-06_inverter_orb"));
            Ref("CueRing", S("Bodies/TRAP-07_cue_ring"));
            Ref("CueMark", S("Bodies/TRAP-07_cue_mark"));
            Ref("GlyphRing", S("Bodies/TRAP-08_glyph_ring"));
            Ref("StormCloud", S("Bodies/TRAP-09_storm_cloud"));
            Ref("Scorch", S("Effects/TRAP-10_scorch"));
            Ref("BlockCrack", S("Effects/TRAP-11_block_crack"));
            Refs("Steam", Many("TRAP-12_steam_"));
            Refs("Leaves", Many("TRAP-13_leaf_"));
            Ref("BoltSegment", S("Effects/bolt_segment"));
            Ref("Flash", S("Effects/flash"));
            Ref("Ring", S("Effects/ring"));
            Ref("Mote", S("Effects/mote"));
            string[] placeholders = manifest.entries.Where(e => !string.IsNullOrEmpty(e.placeholder)).Select(e => Path.GetFileNameWithoutExtension(e.path)).OrderBy(n => n, System.StringComparer.Ordinal).ToArray();
            SerializedProperty ph = so.FindProperty("Placeholders");
            bool samePh = ph.arraySize == placeholders.Length;
            for (int i = 0; samePh && i < placeholders.Length; i++) samePh = ph.GetArrayElementAtIndex(i).stringValue == placeholders[i];
            if (!samePh) { ph.arraySize = placeholders.Length; for (int i = 0; i < placeholders.Length; i++) ph.GetArrayElementAtIndex(i).stringValue = placeholders[i]; }
            // New count defaults, once per kit version (later tuning in the asset survives re-imports).
            if (so.FindProperty("KitVersion").intValue < KitVersion)
            {
                so.FindProperty("SprayPuffs").intValue = 8;
                so.FindProperty("KitVersion").intValue = KitVersion;
            }
            if (so.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(config); changes.Add("wrote TrapArtConfig"); }
        }
    }
}
