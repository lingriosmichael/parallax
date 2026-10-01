using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A15: the environment kit as the builders see it. Tools/Art/env_kit.py writes the sprites and
    /// Kit/env_kit.json (each slot's size in units and the offsets a strip is aligned by); this reads them, so no art
    /// measurement lives in C#.</summary>
    public static class EnvironmentKit
    {
        public const string KitFolder = "Assets/_Game/Art/RealityA/Environment/Kit";
        public const string LayerFolder = "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit";
        public const string ManifestPath = KitFolder + "/env_kit.json";
        public const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [System.Serializable]
        public sealed class Slot
        {
            public string name, file, folder, kind;
            public float width, height, above, below, outside;
            public bool normal;
        }

        [System.Serializable] sealed class Manifest { public float worldPpu, halfPpu; public Slot[] slots; }

        static Dictionary<string, Slot> slots;
        static string loadedStamp;

        /// <summary>The slot named `name`, or null when the kit (or that slot) isn't there.</summary>
        public static Slot Get(string name)
        {
            Load();
            return slots != null && slots.TryGetValue(name, out Slot s) ? s : null;
        }

        public static IEnumerable<Slot> All
        {
            get { Load(); return slots != null ? slots.Values : new Slot[0]; }
        }

        public static string PathOf(Slot slot) => (slot.folder == "Kit" ? KitFolder : LayerFolder) + "/" + slot.file;
        public static string NormalPathOf(Slot slot) => PathOf(slot).Replace(".png", "_n.png");

        public static Sprite Sprite(string name)
        {
            Slot s = Get(name);
            return s == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(s));
        }

        /// <summary>True when the manifest and every sprite it lists are imported. Cached until the project's assets change
        /// (the room builder asks for every room, and the route harness builds rooms hundreds of times).</summary>
        public static bool Ready(out string missing)
        {
            if (readyCache.HasValue) { missing = missingCache; return readyCache.Value; }
            Load();
            missing = null;
            if (slots == null) missing = ManifestPath;
            else foreach (Slot s in slots.Values)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(s)) == null) { missing = PathOf(s); break; }
            readyCache = missing == null; missingCache = missing;
            return readyCache.Value;
        }

        static bool? readyCache, materialsCache;
        static string missingCache;

        [InitializeOnLoadMethod]
        static void ClearCachesOnChange() => EditorApplication.projectChanged += () => { readyCache = null; materialsCache = null; };

        public static Material UnlitMaterial => AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

        // ---------- the world-UV materials and the dressing atlas (approved 2026-10-01) ----------

        public const string WorldUVShader = "Parallax/2D/Env-Sprite-Lit-WorldUV";
        public const string MaterialFolder = KitFolder + "/Materials";
        public const string AtlasPath = KitFolder + "/Atlas_EnvKit.spriteatlasv2";

        /// <summary>The fixed play layer's materials: each tile kind's world tile size and anchored axes (0 both, 1
        /// across, 2 down). Renderers sharing one batch.</summary>
        public static readonly (string kind, string slot, int axis)[] TileKinds =
        {
            ("Fill", "ENV_Fill_A", 0), ("Cap", "ENV_Cap", 1), ("CapWash", "ENV_CapWash", 1), ("LipVines", "ENV_LipVines", 1), ("ArchShade", "ENV_ArchShade", 1), ("ThickUnder", "ENV_ThickUnder", 1), ("Under", "ENV_Under", 1), ("Slab", "ENV_Slab", 1), ("Water", "ENV_Water", 1),
            ("Side", "ENV_Side", 2), ("Post", "ENV_Post", 2), ("SlimPost", "ENV_SlimPost", 2),
            // PAX-A16: plain sprites (dressing, ends, capitals, chains, fringes) on the same shader, sprite UV.
            ("Sprite", "ENV_Moss_0", 3),
        };

        /// <summary>The sprites drawn as plain sprites (not tiled in world space), packed into one atlas so they batch.</summary>
        public static readonly string[] AtlasSlots =
        {
            "ENV_Moss_0", "ENV_Moss_1", "ENV_Moss_2", "ENV_Drape_0", "ENV_Drape_1", "ENV_Drape_2", "ENV_Banner_0", "ENV_Banner_1",
            "ENV_Rubble_0", "ENV_Rubble_1", "ENV_Glyph_0", "ENV_Glyph_1", "ENV_Door", "ENV_Checkpoint", "ENV_CheckpointLit",
            "ENV_Glow", "ENV_Halo", "ENV_SlabEnd", "ENV_PostCap", "ENV_PostBase", "ENV_VineAnchor", "ENV_VineTip", "ENV_VineMid",
            "ENV_Drape_3", "ENV_Drape_4", "ENV_Drape_5",   // gauntlet round 9: more ivy shapes (same atlas, so they batch)
        };

        public static string MaterialPath(string kind) => $"{MaterialFolder}/ENV_{kind}.mat";
        public static Material TileMaterial(string kind) => AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(kind));
        public static bool MaterialsReady => materialsCache ??= TileKinds.All(k => TileMaterial(k.kind) != null);

        public static void EnsureMaterials(List<string> changes)
        {
            Shader shader = Shader.Find(WorldUVShader);
            if (shader == null) { Debug.LogError("EnvironmentKit: shader " + WorldUVShader + " not found."); return; }
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) { AssetDatabase.CreateFolder(KitFolder, "Materials"); changes.Add("created " + MaterialFolder); }
            foreach ((string kind, string slot, int axis) in TileKinds)
            {
                Sprite sprite = Sprite(slot);
                if (sprite == null) continue;
                Material m = TileMaterial(kind);
                bool created = m == null;
                if (created) { m = new Material(shader) { name = "ENV_" + kind }; AssetDatabase.CreateAsset(m, MaterialPath(kind)); materialsCache = null; }
                Vector4 tile = new(sprite.bounds.size.x, sprite.bounds.size.y, 0f, 0f);
                if (created || m.shader != shader || m.GetVector("_Tile") != tile || !Mathf.Approximately(m.GetFloat("_Axis"), axis))
                {
                    m.shader = shader; m.SetVector("_Tile", tile); m.SetFloat("_Axis", axis);
                    EditorUtility.SetDirty(m);
                    changes.Add((created ? "created " : "configured ") + MaterialPath(kind));
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Gauntlet: the face shade's sprites, added after the first atlas was made, in a second atlas so they batch
        /// with each other without touching the first one's contents. (Dressing stays in the first atlas: two atlases
        /// alternating at one depth broke every batch.)</summary>
        public const string Atlas2Path = KitFolder + "/Atlas_EnvKit2.spriteatlasv2";
        public static readonly string[] Atlas2Slots =
        {
            "ENV_FaceShade", "ENV_FaceShadeSolid",
        };

        public static void EnsureAtlas(List<string> changes)
        {
            EnsureAtlas(AtlasPath, AtlasSlots, changes);
            EnsureAtlas(Atlas2Path, Atlas2Slots, changes);
        }

        static void EnsureAtlas(string atlasPath, string[] slotNames, List<string> changes)
        {
            if (AssetDatabase.LoadMainAssetAtPath(atlasPath) == null)
            {
                var asset = new UnityEditor.U2D.SpriteAtlasAsset();
                asset.Add(slotNames.Select(n => (Object)Sprite(n)).Where(o => o != null).ToArray());
                UnityEditor.U2D.SpriteAtlasAsset.Save(asset, atlasPath);
                AssetDatabase.ImportAsset(atlasPath);
                changes.Add("created " + atlasPath);
            }
            else
            {
                // An existing atlas takes the slots added since it was made (idempotent).
                var packed = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlasPath);
                Object[] missing = slotNames.Select(n => Sprite(n)).Where(sp => sp != null && packed != null && !packed.CanBindTo(sp)).Cast<Object>().ToArray();
                if (missing.Length > 0)
                {
                    UnityEditor.U2D.SpriteAtlasAsset asset = UnityEditor.U2D.SpriteAtlasAsset.Load(atlasPath);
                    asset.Add(missing);
                    UnityEditor.U2D.SpriteAtlasAsset.Save(asset, atlasPath);
                    AssetDatabase.ImportAsset(atlasPath);
                    changes.Add($"added {missing.Length} sprites to {atlasPath}");
                }
            }
            var importer = AssetImporter.GetAtPath(atlasPath) as UnityEditor.U2D.SpriteAtlasImporter;
            if (importer == null) { Debug.LogError("EnvironmentKit: " + atlasPath + " isn't a Sprite Atlas V2."); return; }
            bool dirty = false;
            UnityEditor.U2D.SpriteAtlasPackingSettings packing = importer.packingSettings;
            if (packing.enableRotation || packing.enableTightPacking || packing.padding != 4)
            {
                packing.enableRotation = false; packing.enableTightPacking = false; packing.padding = 4;
                importer.packingSettings = packing; dirty = true;
            }
            UnityEditor.U2D.SpriteAtlasTextureSettings texture = importer.textureSettings;
            // Gauntlet: mipmapped (drawn smaller than packed on a phone; without mips the moss and drapes sparkled).
            if (!texture.generateMipMaps || texture.filterMode != FilterMode.Bilinear)
            {
                texture.generateMipMaps = true; texture.filterMode = FilterMode.Bilinear;
                importer.textureSettings = texture; dirty = true;
            }
            TextureImporterPlatformSettings android = importer.GetPlatformSettings("Android");
            if (!android.overridden || android.format != TextureImporterFormat.ASTC_4x4 || android.maxTextureSize != 2048)
            {
                android.overridden = true; android.format = TextureImporterFormat.ASTC_4x4; android.maxTextureSize = 2048;
                importer.SetPlatformSettings(android); dirty = true;
            }
            TextureImporterPlatformSettings standalone = importer.GetPlatformSettings("DefaultTexturePlatform");
            if (standalone.maxTextureSize != 2048) { standalone.maxTextureSize = 2048; importer.SetPlatformSettings(standalone); dirty = true; }
            if (!dirty) return;
            importer.SaveAndReimport();
            changes.Add("configured " + atlasPath);
        }

        static void Load()
        {
            if (!File.Exists(ManifestPath)) { slots = null; loadedStamp = null; return; }
            string stamp = File.GetLastWriteTimeUtc(ManifestPath).Ticks.ToString();
            if (slots != null && stamp == loadedStamp) return;
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            slots = new Dictionary<string, Slot>();
            if (manifest?.slots != null) foreach (Slot s in manifest.slots) slots[s.name] = s;
            loadedStamp = stamp;
        }
    }
}
