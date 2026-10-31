using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Editor.Levels;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A15: the environment kit as the builders see it. Tools/Art/env_kit.py writes the sprites and
    /// Kit/env_kit.json (each slot's size in units and the offsets a strip is aligned by); this reads them, so no art
    /// measurement lives in C#.
    /// PAX-A20: a biome's kit is an overlay on Golden Ruins'. Tools/Art/obsidian_env.py writes Obsidian Void's slots, the same
    /// names and sizes, into ObsidianKitFolder with their own env_kit.json; while Current is Obsidian Void (LevelSetup sets it
    /// for an L031+ build), a slot it lists resolves there and every other slot (the edge masks, the backgrounds until stage
    /// 3) falls back to Golden Ruins'. The play layer's tile materials and atlases are the biome's own. Stage 3 (D-274) adds the
    /// background pieces, in ObsidianLayerFolder.</summary>
    public static class EnvironmentKit
    {
        public const string KitFolder = "Assets/_Game/Art/RealityA/Environment/Kit";
        public const string ObsidianKitFolder = "Assets/_Game/Art/ObsidianVoid/Environment/Kit";
        public const string ObsidianLayerFolder = "Assets/_Game/Art/ObsidianVoid/Environment/Backgrounds/Kit";

        /// <summary>The biome whose kit the builders read. Golden Ruins unless a level build says otherwise.</summary>
        public static Biome Current { get; set; } = Biome.GoldenRuins;

        /// <summary>The folder holding the current biome's play-layer overrides, tile materials and atlases.</summary>
        public static string BiomeKitFolder => Current == Biome.ObsidianVoid ? ObsidianKitFolder : KitFolder;
        public const string LayerFolder = "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit";
        public const string ManifestPath = KitFolder + "/env_kit.json";
        public const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [System.Serializable]
        public sealed class Slot
        {
            public string name, file, folder, kind;
            public float width, height, above, below, outside;
            public bool normal;
            [System.NonSerialized] public string root;   // PAX-A20: the folder of a biome override (null: Golden Ruins')
        }

        [System.Serializable] sealed class Manifest { public float worldPpu, halfPpu; public Slot[] slots; }

        static Dictionary<string, Slot> slots;
        static readonly Dictionary<Biome, (Dictionary<string, Slot> slots, string stamp)> loaded = new();

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

        public static string PathOf(Slot slot) => slot.root != null ? slot.root + "/" + slot.file : (slot.folder == "Kit" ? KitFolder : LayerFolder) + "/" + slot.file;
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
            if (readyCache.TryGetValue(Current, out (bool ready, string missing) cached)) { missing = cached.missing; return cached.ready; }
            Load();
            missing = null;
            if (slots == null) missing = ManifestPath;
            else foreach (Slot s in slots.Values)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(s)) == null) { missing = PathOf(s); break; }
            readyCache[Current] = (missing == null, missing);
            return missing == null;
        }

        static readonly Dictionary<Biome, (bool ready, string missing)> readyCache = new();
        static readonly Dictionary<Biome, bool> materialsCache = new();

        [InitializeOnLoadMethod]
        static void ClearCachesOnChange() => EditorApplication.projectChanged += () => { readyCache.Clear(); materialsCache.Clear(); loaded.Clear(); };

        /// <summary>Runs `action` once per biome whose kit is there (Golden Ruins always; Obsidian once obsidian_env.py has run),
        /// with Current set to it, then puts Current back.</summary>
        public static void ForEachBiome(System.Action action)
        {
            Biome was = Current;
            try
            {
                foreach (Biome b in new[] { Biome.GoldenRuins, Biome.ObsidianVoid })
                {
                    if (b == Biome.ObsidianVoid && !File.Exists(ObsidianKitFolder + "/env_kit.json")) continue;
                    Current = b;
                    action();
                }
            }
            finally { Current = was; }
        }

        public static Material UnlitMaterial => AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

        // ---------- the world-UV materials and the dressing atlas (approved 2026-10-01) ----------

        public const string WorldUVShader = "Parallax/2D/Env-Sprite-Lit-WorldUV";
        /// <summary>The background tiers' materials (Golden Ruins' until stage 3 gives Obsidian its own backgrounds).</summary>
        public const string MaterialFolder = KitFolder + "/Materials";
        /// <summary>PAX-A20: the play layer's tile and edge materials, per biome (each samples its biome's sprites).</summary>
        public static string TileMaterialFolder => BiomeKitFolder + "/Materials";
        public static string AtlasPath => BiomeKitFolder + "/Atlas_EnvKit.spriteatlasv2";

        /// <summary>The fixed play layer's materials: each tile kind's world tile size and anchored axes (0 both, 1
        /// across, 2 down). Renderers sharing one batch.</summary>
        public static readonly (string kind, string slot, int axis)[] TileKinds =
        {
            ("Fill", "ENV_Fill_A", 0), ("Cap", "ENV_Cap", 1), ("CapWash", "ENV_CapWash", 1), ("LipVines", "ENV_LipVines", 1), ("ArchShade", "ENV_ArchShade", 1), ("ThickUnder", "ENV_ThickUnder", 1), ("Under", "ENV_Under", 1), ("Slab", "ENV_Slab", 1), ("Water", "ENV_Water", 1),
            ("Side", "ENV_Side", 2), ("Post", "ENV_Post", 2), ("SlimPost", "ENV_SlimPost", 2),
            // PAX-106 (D-114): the broken-stone side strips (every variant shares ENV_SideChip_A_0's size) and the cut thick
            // underside. Optional: without their materials the builder keeps ENV_Side and ENV_ThickUnder.
            ("ThickUnderShort", "ENV_ThickUnderShort", 1),
            // PAX-A18 W3: the walkable underside of flip rooms, and D-114's own strips (beside hazards, and on moving traps).
            ("UnderWalk", "ENV_UnderWalk", 1), ("SideIn", "ENV_SideIn_A_0", 2),
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
            "ENV_Fern_0", "ENV_Fern_1", "ENV_Fern_2",      // gauntlet: foot ferns, fixed in the world
            "ENV_CapEnd_0", "ENV_CapEnd_1", "ENV_CapEnd_2", "ENV_CapEnd_3", "ENV_SlabEndThick",   // PAX-A18 W3: broken corner and thick ends
            "ENV_ThickUnderEnd", "ENV_ThickUnderShortEnd", "ENV_LipVinesEnd",   // 2026-10-16: broken and ragged ends
            "ENV_GripTuft_0", "ENV_GripTuft_1", "ENV_GripTuft_2",   // PAX-A20: Obsidian's grip panels (no Golden Ruins twin)
            "ENV_Fixture_0", "ENV_Fixture_1", "ENV_Fixture_2", "ENV_Fixture_3", "ENV_Fixture_4", "ENV_Fixture_5",   // and its props
            "ENV_Prop_0", "ENV_Prop_1", "ENV_Prop_2", "ENV_Prop_3", "ENV_Prop_4", "ENV_Prop_5",
        };

        public static string MaterialPath(string kind) => $"{TileMaterialFolder}/ENV_{kind}.mat";
        public static Material TileMaterial(string kind) => AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(kind));
        /// <summary>PAX-106: tile kinds the builder falls back from when their material is missing (made by Environment Stack).</summary>
        static readonly string[] OptionalKinds = { "ThickUnderShort", "UnderWalk", "SideIn" };

        /// <summary>PAX-A18 W3: the broken-stone edge strips (band and deep) are masks on Env-Sprite-Lit-EdgeMask: their colour is
        /// the room's fill at the same world position, so the edge is cut from the stone it borders. One material per strip kind
        /// and fill stone (ENV_SideChip_A, ENV_SideDeep_A2, …).</summary>
        public const string EdgeMaskShader = "Parallax/2D/Env-Sprite-Lit-EdgeMask";
        public static readonly string[] EdgeFills = { "A", "A2", "A3" };
        public static readonly (string kind, string prefix)[] EdgeKinds = { ("SideChip", "ENV_SideChip_"), ("SideDeep", "ENV_SideDeep_") };
        public static string EdgeKind(string kind, string fill) => kind + "_" + fill;
        public static bool MaterialsReady
        {
            get
            {
                if (materialsCache.TryGetValue(Current, out bool ready)) return ready;
                return materialsCache[Current] = TileKinds.Where(k => System.Array.IndexOf(OptionalKinds, k.kind) < 0).All(k => TileMaterial(k.kind) != null);
            }
        }

        /// <summary>Every biome's tile materials (PAX-A20).</summary>
        public static void EnsureMaterials(List<string> changes) => ForEachBiome(() => EnsureBiomeMaterials(changes));

        static void EnsureBiomeMaterials(List<string> changes)
        {
            Shader shader = Shader.Find(WorldUVShader);
            if (shader == null) { Debug.LogError("EnvironmentKit: shader " + WorldUVShader + " not found."); return; }
            if (!AssetDatabase.IsValidFolder(TileMaterialFolder)) { AssetDatabase.CreateFolder(BiomeKitFolder, "Materials"); changes.Add("created " + TileMaterialFolder); }
            foreach ((string kind, string slot, int axis) in TileKinds)
            {
                Sprite sprite = Sprite(slot);
                if (sprite == null) continue;
                Material m = TileMaterial(kind);
                bool created = m == null;
                if (created) { m = new Material(shader) { name = "ENV_" + kind }; AssetDatabase.CreateAsset(m, MaterialPath(kind)); materialsCache.Remove(Current); }
                Vector4 tile = new(sprite.bounds.size.x, sprite.bounds.size.y, 0f, 0f);
                if (created || m.shader != shader || m.GetVector("_Tile") != tile || !Mathf.Approximately(m.GetFloat("_Axis"), axis))
                {
                    m.shader = shader; m.SetVector("_Tile", tile); m.SetFloat("_Axis", axis);
                    EditorUtility.SetDirty(m);
                    changes.Add((created ? "created " : "configured ") + MaterialPath(kind));
                }
            }
            EnsureEdgeMaterials(changes);
            AssetDatabase.SaveAssets();
        }

        /// <summary>2026-10-17: a fallen block's heap (ENV_RubbleHeap_*): a mask on the same shader in its sprite's own UV (_Axis
        /// 3), so the heap is cut from the room's stone. One material per fill stone (ENV_Rubble_A, …).</summary>
        public const string RubbleKind = "Rubble";
        public static readonly string[] RubbleShapes = { "Sq", "Wide", "Slab" };

        static void EnsureEdgeMaterials(List<string> changes)
        {
            Shader shader = Shader.Find(EdgeMaskShader);
            if (shader == null) { Debug.LogError("EnvironmentKit: shader " + EdgeMaskShader + " not found."); return; }
            foreach ((string kind, string prefix) in EdgeKinds.Append((RubbleKind, "ENV_RubbleHeap_")))
                foreach (string fill in EdgeFills)
                {
                    bool rubble = kind == RubbleKind;
                    Sprite strip = Sprite(rubble ? prefix + "Sq_0" : prefix + fill + "_0"), stone = Sprite("ENV_Fill_" + fill);
                    Slot fillSlot = Get("ENV_Fill_" + fill);
                    if (strip == null || stone == null || fillSlot == null) continue;
                    var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPathOf(fillSlot));
                    string name = EdgeKind(kind, fill);
                    Material m = TileMaterial(name);
                    bool created = m == null;
                    if (created) { m = new Material(shader) { name = "ENV_" + name }; AssetDatabase.CreateAsset(m, MaterialPath(name)); materialsCache.Remove(Current); }
                    Vector4 tile = new(strip.bounds.size.x, strip.bounds.size.y, 0f, 0f), fillTile = new(stone.bounds.size.x, stone.bounds.size.y, 0f, 0f);
                    if (created || m.shader != shader || m.GetVector("_Tile") != tile || m.GetFloat("_Axis") != (rubble ? 3f : 2f) || m.GetVector("_FillTile") != fillTile || m.GetTexture("_FillTex") != stone.texture || m.GetTexture("_FillNormal") != normal)
                    {
                        m.shader = shader; m.SetVector("_Tile", tile); m.SetFloat("_Axis", rubble ? 3f : 2f);
                        m.SetTexture("_FillTex", stone.texture); m.SetVector("_FillTile", fillTile); m.SetTexture("_FillNormal", normal);
                        EditorUtility.SetDirty(m);
                        changes.Add((created ? "created " : "configured ") + MaterialPath(name));
                    }
                }
        }

        /// <summary>Gauntlet: the face shade's sprites, added after the first atlas was made, in a second atlas so they batch
        /// with each other without touching the first one's contents. (Dressing stays in the first atlas: two atlases
        /// alternating at one depth broke every batch.)</summary>
        public static string Atlas2Path => BiomeKitFolder + "/Atlas_EnvKit2.spriteatlasv2";
        public static readonly string[] Atlas2Slots =
        {
            "ENV_FaceShade", "ENV_FaceShadeSolid",
        };

        public static void EnsureAtlas(List<string> changes) => ForEachBiome(() =>
        {
            EnsureAtlas(AtlasPath, AtlasSlots, changes);
            EnsureAtlas(Atlas2Path, Atlas2Slots, changes);
        });

        static void EnsureAtlas(string atlasPath, string[] slotNames, List<string> changes)
        {
            // PAX-A20: a biome's atlas packs its own sprites only (a fallback is in Golden Ruins' atlas already).
            slotNames = slotNames.Where(n => Get(n) is Slot slot && (Current == Biome.GoldenRuins ? slot.root == null : slot.root != null)).ToArray();
            if (slotNames.Length == 0) return;
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
            string overlay = Current == Biome.ObsidianVoid ? ObsidianKitFolder + "/env_kit.json" : null;
            if (!File.Exists(ManifestPath)) { slots = null; loaded.Remove(Current); return; }
            string stamp = File.GetLastWriteTimeUtc(ManifestPath).Ticks + (overlay != null && File.Exists(overlay) ? "/" + File.GetLastWriteTimeUtc(overlay).Ticks : "");
            if (loaded.TryGetValue(Current, out var cached) && cached.stamp == stamp) { slots = cached.slots; return; }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            slots = new Dictionary<string, Slot>();
            if (manifest?.slots != null) foreach (Slot s in manifest.slots) slots[s.name] = s;
            // PAX-A20: the biome's overrides, each resolving in its own folder. An override whose sprite isn't imported (yet) is
            // left out, so its Golden Ruins twin stands in (or, for an Obsidian-only slot, nothing): one missing sprite must
            // never fail the whole kit (Ready), which skipped the level's background and left the template's (2026-10-31).
            if (overlay != null && File.Exists(overlay))
            {
                var over = JsonUtility.FromJson<Manifest>(File.ReadAllText(overlay));
                var missing = new List<string>();
                if (over?.slots != null) foreach (Slot s in over.slots)
                {
                    s.root = s.folder == "Kit" ? ObsidianKitFolder : ObsidianLayerFolder;
                    if (AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(s)) == null) { missing.Add(s.name); continue; }
                    slots[s.name] = s;
                }
                if (missing.Count > 0)
                    Debug.LogWarning($"EnvironmentKit: {missing.Count} Obsidian Void sprites aren't imported as sprites ({string.Join(", ", missing.Take(8))}{(missing.Count > 8 ? ", ..." : "")}); Golden Ruins' stand in. Let Unity finish importing Assets/_Game/Art/ObsidianVoid (or reimport it), then rebuild the level.");
            }
            loaded[Current] = (slots, stamp);
        }
    }
}
