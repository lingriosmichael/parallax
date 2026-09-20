using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace Parallax.Editor.Setup
{
    /// <summary>Idempotent PAX-A02 presentation wiring. It never creates or changes physics components.</summary>
    public static class EnvironmentArtSetup
    {
        const string ArtRoot = "Assets/_Game/Art";
        const string AtlasAPath = ArtRoot + "/RealityA/Atlas_RealityA_Env.spriteatlas";
        const string AtlasBPath = ArtRoot + "/RealityB/Atlas_RealityB_Env.spriteatlas";
        const string AtlasAPathV2 = ArtRoot + "/RealityA/Atlas_RealityA_Env.spriteatlasv2";
        const string AtlasBPathV2 = ArtRoot + "/RealityB/Atlas_RealityB_Env.spriteatlasv2";
        // PAX-A02_Stage2.md: final-palette globals must not darken Reality B to 35%.
        static readonly Color GlobalLightAColor = new Color(1f, .95f, .88f);
        const float GlobalLightAIntensity = 1f;
        static readonly Color GlobalLightBColor = new Color(.78f, .86f, 1f);
        const float GlobalLightBIntensity = .9f;
        static readonly string[] GeometryA = { "Ground", "Wall_Left", "Wall_Right", "Ceiling", "Platform_Left", "Platform_Right" };
        static readonly string[] GeometryB = { "Ground", "Wall_Left", "Wall_Right", "Ceiling", "Pillar_BOnly", "Ledge_B" };
        static readonly string[] SlotsA = { "BG_00_Sky", "BG_01_Far", "MG_01_Mid", "GAME_Platform_Top", "GAME_Platform_Fill", "GAME_Wall", "OBJ_Vine", "OBJ_Plate", "OBJ_Station", "OBJ_Checkpoint", "OBJ_Hazard", "FG_01" };
        static readonly string[] SlotsB = { "BG_00_Sky", "BG_01_Far", "MG_01_Mid", "GAME_Platform_Top", "GAME_Platform_Fill", "GAME_Wall", "OBJ_Elevator", "OBJ_Gate", "OBJ_Checkpoint", "OBJ_Hazard", "FG_01" };

        [MenuItem("PARALLAX/Setup/Environment Art")]
        public static void Configure()
        {
            var changes = new List<string>();
            RealityRoot rootA = GetRoot("RealityRoot_A");
            RealityRoot rootB = GetRoot("RealityRoot_B");
            if (rootA == null || rootB == null) return;

            EnsureFolder(ArtRoot + "/RealityA/Environment/Backgrounds", changes);
            EnsureFolder(ArtRoot + "/RealityB/Environment/Backgrounds", changes);
            bool useV2Atlas = UsesV2SpritePacker();
            EnsureAtlas(AtlasAPath, AtlasAPathV2, "A", useV2Atlas, changes);
            EnsureAtlas(AtlasBPath, AtlasBPathV2, "B", useV2Atlas, changes);
            int filledA = ConfigureReality(rootA, GeometryA, "A", CameraNamed("Camera_A"), changes);
            int filledB = ConfigureReality(rootB, GeometryB, "B", CameraNamed("Camera_B"), changes);
            ValidateGlobalLight(rootA);
            ValidateGlobalLight(rootB);
            ConfigureGlobalLight(rootA, GlobalLightAColor, GlobalLightAIntensity, changes);
            ConfigureGlobalLight(rootB, GlobalLightBColor, GlobalLightBIntensity, changes);
            ConfigureAccentShadows(rootB, changes);

            if (changes.Count == 0) Debug.Log("EnvironmentArtSetup: no changes.");
            else
            {
                Debug.Log("EnvironmentArtSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(rootA.gameObject.scene);
            }
            LogSlotFiles("A", SlotsA);
            LogSlotFiles("B", SlotsB);
            Debug.Log("EnvironmentArtSetup: sorting orders — geometry Art=-10; Platform_Top ArtTop=-9; puzzle/Hazard Art=-2; cat body=0; cat outline=-1; background/middle/foreground=0.");
            RealityIsolationValidator.Validate();
        }

        static RealityRoot GetRoot(string name)
        {
            GameObject go = GameObject.Find(name);
            RealityRoot root = go == null ? null : go.GetComponent<RealityRoot>();
            if (root == null) Debug.LogError("EnvironmentArtSetup: " + name + " is required.");
            return root;
        }

        static Camera CameraNamed(string name)
        {
            GameObject go = GameObject.Find(name);
            Camera camera = go == null ? null : go.GetComponent<Camera>();
            if (camera == null) Debug.LogError("EnvironmentArtSetup: " + name + " is required.");
            return camera;
        }

        static int ConfigureReality(RealityRoot root, string[] geometryNames, string prefix, Camera camera, List<string> changes)
        {
            int filled = 0;
            for (int i = 0; i < geometryNames.Length; i++)
            {
                string name = geometryNames[i];
                string slot = name.StartsWith("Wall") || name == "Ceiling" || name == "Pillar_BOnly" ? "GAME_Wall" : "GAME_Platform_Fill";
                Sprite sprite = Load(prefix, slot);
                bool topGeometry = IsTopGeometry(prefix, name);
                Sprite top = topGeometry ? Load(prefix, "GAME_Platform_Top") : null;
                if (sprite == null) Missing(prefix, slot);
                if (topGeometry && top == null) Missing(prefix, "GAME_Platform_Top");
                if (sprite == null && top == null) continue;
                Transform geometry = root.transform.Find("Geometry/" + name);
                BoxCollider2D collider = geometry == null ? null : geometry.GetComponent<BoxCollider2D>();
                SpriteRenderer greybox = geometry == null ? null : geometry.GetComponent<SpriteRenderer>();
                if (collider == null || greybox == null) { Debug.LogError("EnvironmentArtSetup: missing inventoried geometry renderer/collider " + name); continue; }
                bool hasArt = false;
                if (sprite != null) { ConfigureGeometryArt(geometry, collider, root, "Art", sprite, -10, changes); hasArt = true; }
                if (topGeometry)
                {
                    if (top != null) { ConfigureTopArt(geometry, collider, root, top, changes); hasArt = true; }
                }
                if (hasArt && greybox.enabled) { greybox.enabled = false; changes.Add("disabled greybox " + RealityIsolationValidator.Path(greybox.transform)); }
                if (hasArt) filled++;
            }
            filled += ConfigurePuzzleArt(root, prefix, changes);
            filled += ConfigureHazard(root, prefix, changes);
            filled += ConfigureBackgrounds(root, prefix, camera, changes);
            return filled;
        }

        static bool IsTopGeometry(string prefix, string name) =>
            name == "Ground" || (prefix == "A" && name.StartsWith("Platform_")) || (prefix == "B" && name == "Ledge_B");

        static void ConfigureGeometryArt(Transform geometry, BoxCollider2D collider, RealityRoot root, string childName, Sprite sprite, int order, List<string> changes)
        {
            Transform art = SetupUtility.EnsureChild(geometry, childName, root.gameObject.layer, changes);
            SetArtScale(art, changes);
            SetupUtility.SetLocalPosition(art, collider.offset, changes);
            SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
            ConfigureRenderer(renderer, sprite, RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay), collider.bounds.size, SpriteDrawMode.Tiled, order, changes);
        }

        static void ConfigureTopArt(Transform geometry, BoxCollider2D collider, RealityRoot root, Sprite sprite, List<string> changes)
        {
            Transform art = SetupUtility.EnsureChild(geometry, "ArtTop", root.gameObject.layer, changes);
            SetArtScale(art, changes);
            float pivotY = sprite.rect.height == 0f ? 1f : sprite.pivot.y / sprite.rect.height;
            float topFromPivotInWorldUnits = (1f - pivotY) * sprite.bounds.size.y;
            float topFromPivotInParentUnits = topFromPivotInWorldUnits / geometry.lossyScale.y;
            SetupUtility.SetLocalPosition(art, collider.offset + Vector2.up * (collider.size.y * 0.5f - topFromPivotInParentUnits), changes);
            SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
            ConfigureRenderer(renderer, sprite, RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay), new Vector2(collider.bounds.size.x, sprite.bounds.size.y), SpriteDrawMode.Tiled, -9, changes);
        }

        static void SetArtScale(Transform art, List<string> changes)
        {
            Vector3 parentScale = art.parent.lossyScale;
            Vector3 cancellation = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
            if (art.localScale != cancellation) { art.localScale = cancellation; changes.Add("set " + RealityIsolationValidator.Path(art) + ".localScale"); }
        }

        static int ConfigurePuzzleArt(RealityRoot root, string prefix, List<string> changes)
        {
            int filled = 0;
            if (prefix == "A")
            {
                filled += AddPuzzle(root, "Anchors/Vine_A/Knot", prefix, "OBJ_Vine", changes);
                filled += AddPuzzle(root, "Anchors/Plate_A", prefix, "OBJ_Plate", changes);
                filled += AddPuzzle(root, "Interactables/Station_A", prefix, "OBJ_Station", changes);
                filled += AddPuzzle(root, "Checkpoints/Checkpoint_0", prefix, "OBJ_Checkpoint", changes);
            }
            else
            {
                filled += AddPuzzle(root, "Anchors/Elevator_B", prefix, "OBJ_Elevator", changes);
                filled += AddPuzzle(root, "Anchors/Gate_B", prefix, "OBJ_Gate", changes);
                filled += AddPuzzle(root, "Checkpoints/Checkpoint_0", prefix, "OBJ_Checkpoint", changes);
            }
            return filled;
        }

        static int AddPuzzle(RealityRoot root, string path, string prefix, string slot, List<string> changes)
        {
            Sprite sprite = Load(prefix, slot);
            if (sprite == null) { Missing(prefix, slot); return 0; }
            Transform target = root.transform.Find(path);
            if (target == null) { Debug.LogError("EnvironmentArtSetup: missing manifestation " + path); return 0; }
            Transform art = SetupUtility.EnsureChild(target, "Art", root.gameObject.layer, changes);
            SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
            ConfigureRenderer(renderer, sprite, RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay), sprite.bounds.size, SpriteDrawMode.Simple, -2, changes);
            return 1;
        }

        static int ConfigureHazard(RealityRoot root, string prefix, List<string> changes)
        {
            Sprite sprite = Load(prefix, "OBJ_Hazard");
            if (sprite == null) { Missing(prefix, "OBJ_Hazard"); return 0; }
            Transform reset = root.transform.Find("FallResetTest");
            if (reset == null) { Debug.LogError("EnvironmentArtSetup: missing FallResetTest for " + root.Id); return 0; }
            Transform art = SetupUtility.EnsureChild(reset, "HazardArt", root.gameObject.layer, changes);
            SetupUtility.SetLocalPosition(art, new Vector2(0f, -3.5f), changes);
            SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
            ConfigureRenderer(renderer, sprite, RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay), new Vector2(18f, 1f), SpriteDrawMode.Tiled, -2, changes);
            return 1;
        }

        static int ConfigureBackgrounds(RealityRoot root, string prefix, Camera camera, List<string> changes)
        {
            int filled = 0;
            filled += AddBackground(root, prefix, camera, "BG_00_Sky", SortingBand.Background, 0.05f, changes);
            filled += AddBackground(root, prefix, camera, "BG_01_Far", SortingBand.Background, 0.15f, changes);
            filled += AddBackground(root, prefix, camera, "MG_01_Mid", SortingBand.Middle, 0.35f, changes);
            Sprite fg = Load(prefix, "FG_01");
            if (fg == null) Missing(prefix, "FG_01");
            else
            {
                Transform parent = SetupUtility.EnsureChild(root.transform, "Foreground", root.gameObject.layer, changes);
                Transform art = SetupUtility.EnsureChild(parent, "FG_01", root.gameObject.layer, changes);
                SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(art.gameObject, changes);
                ConfigureRenderer(renderer, fg, RealitySpace.SortingLayerName(root.Id, SortingBand.Foreground), fg.bounds.size, SpriteDrawMode.Simple, 0, changes);
                filled++;
            }
            return filled;
        }

        static int AddBackground(RealityRoot root, string prefix, Camera camera, string slot, SortingBand band, float screenSpeed, List<string> changes)
        {
            Sprite sprite = Load(prefix, slot);
            if (sprite == null) { Missing(prefix, slot); return 0; }
            Transform parent = SetupUtility.EnsureChild(root.transform, "Background", root.gameObject.layer, changes);
            Transform layer = SetupUtility.EnsureChild(parent, slot, root.gameObject.layer, changes);
            ParallaxLayer parallax = SetupUtility.Ensure<ParallaxLayer>(layer.gameObject, changes);
            SetupUtility.SetObject(parallax, "realityCamera", camera, changes);
            SetupUtility.SetFloat(parallax, "screenSpeed", screenSpeed, changes);
            float width = sprite.bounds.size.x;
            SetupUtility.SetFloat(parallax, "tileWidth", width, changes);
            var tiles = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                Transform tile = SetupUtility.EnsureChild(layer, "Tile_" + (i - 1), root.gameObject.layer, changes);
                SpriteRenderer renderer = SetupUtility.Ensure<SpriteRenderer>(tile.gameObject, changes);
                ConfigureRenderer(renderer, sprite, RealitySpace.SortingLayerName(root.Id, band), sprite.bounds.size, SpriteDrawMode.Tiled, 0, changes);
                tiles[i] = tile;
            }
            SetupUtility.SetArray(parallax, "tiles", tiles, changes);
            return 1;
        }

        static void ValidateGlobalLight(RealityRoot root)
        {
            int count = 0;
            foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true)) if (light.lightType == Light2D.LightType.Global) count++;
            if (count != 1) Debug.LogError("EnvironmentArtSetup: expected exactly one Global Light2D under " + root.name + ", found " + count + ".");
        }

        static void ConfigureGlobalLight(RealityRoot root, Color color, float intensity, List<string> changes)
        {
            foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true))
            {
                if (light.lightType != Light2D.LightType.Global) continue;
                if (light.color != color) { light.color = color; changes.Add("set " + light.name + ".color"); }
                if (!Mathf.Approximately(light.intensity, intensity)) { light.intensity = intensity; changes.Add("set " + light.name + ".intensity"); }
                return;
            }
        }

        static void ConfigureAccentShadows(RealityRoot root, List<string> changes)
        {
            string[] names = { "CyanAccent_Platform", "CyanAccent_Gate" };
            for (int i = 0; i < names.Length; i++)
            {
                Light2D light = FindLight(root, names[i]);
                Transform accent = light == null ? null : light.transform;
                if (light == null) { Debug.LogError("EnvironmentArtSetup: missing cyan accent Light2D " + names[i]); continue; }
                if (light.shadowsEnabled) { light.shadowsEnabled = false; changes.Add("disabled shadows " + RealityIsolationValidator.Path(accent)); }
            }
        }

        static Light2D FindLight(RealityRoot root, string name)
        {
            Light2D[] lights = root.GetComponentsInChildren<Light2D>(true);
            for (int i = 0; i < lights.Length; i++) if (lights[i].name == name) return lights[i];
            return null;
        }

        static void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite, string sortingLayer, Vector2 size, SpriteDrawMode drawMode, int sortingOrder, List<string> changes)
        {
            if (renderer.sprite != sprite) { renderer.sprite = sprite; changes.Add("set " + renderer.name + ".sprite"); }
            if (renderer.drawMode != drawMode) { renderer.drawMode = drawMode; changes.Add("set " + renderer.name + ".drawMode"); }
            if (renderer.size != size) { renderer.size = size; changes.Add("set " + renderer.name + ".size"); }
            if (renderer.sortingLayerName != sortingLayer) { renderer.sortingLayerName = sortingLayer; changes.Add("set " + renderer.name + ".sortingLayer"); }
            if (renderer.sortingOrder != sortingOrder) { renderer.sortingOrder = sortingOrder; changes.Add("set " + renderer.name + ".sortingOrder"); }
        }

        static Sprite Load(string prefix, string slot) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Reality" + prefix + "/Environment/" + (slot.StartsWith("BG_") || slot.StartsWith("MG_") ? "Backgrounds/" : "") + prefix + "_" + slot + ".png");
        static void Missing(string prefix, string slot) => Debug.Log("EnvironmentArtSetup: missing slot " + prefix + "_" + slot + ".png");
        static void LogSlotFiles(string prefix, string[] slots)
        {
            int found = 0;
            for (int i = 0; i < slots.Length; i++) if (Load(prefix, slots[i]) != null) found++;
            Debug.Log($"EnvironmentArtSetup: {prefix} slot files found {found}/{slots.Length}, missing {slots.Length - found}.");
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot + "/Reality" + prefix + "/Environment" });
            for (int i = 0; i < guids.Length; i++)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guids[i]));
                bool used = false;
                for (int j = 0; j < slots.Length; j++) if (file == prefix + "_" + slots[j]) used = true;
                if (!used) Debug.Log("EnvironmentArtSetup: unused slot file " + file + ".png");
            }
        }
        static void EnsureFolder(string path, List<string> changes)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent, changes);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
            changes.Add("created " + path);
        }

        static bool UsesV2SpritePacker() => EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2 || EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2Build;

        static void EnsureAtlas(string v1Path, string v2Path, string prefix, bool useV2, List<string> changes)
        {
            if (useV2) { EnsureAtlasV2(v1Path, v2Path, prefix, changes); return; }
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(v1Path);
            if (atlas == null) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, v1Path); changes.Add("created " + v1Path); }
            Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
            string[] slots = { "GAME_Platform_Top", "GAME_Platform_Fill", "GAME_Wall", "OBJ_Vine", "OBJ_Elevator", "OBJ_Plate", "OBJ_Gate", "OBJ_Station", "OBJ_Checkpoint", "OBJ_Hazard", "FG_01" };
            for (int i = 0; i < slots.Length; i++)
            {
                Sprite sprite = Load(prefix, slots[i]);
                bool contains = false;
                for (int j = 0; j < packables.Length; j++) if (packables[j] == sprite) contains = true;
                if (sprite != null && !contains) { SpriteAtlasExtensions.Add(atlas, new Object[] { sprite }); changes.Add("added " + sprite.name + " to " + atlas.name); }
            }
            if (!SpriteAtlasExtensions.IsIncludeInBuild(atlas)) { SpriteAtlasExtensions.SetIncludeInBuild(atlas, true); changes.Add("set " + atlas.name + ".includeInBuild"); }
        }

        static void EnsureAtlasV2(string v1Path, string v2Path, string prefix, List<string> changes)
        {
            if (AssetDatabase.LoadMainAssetAtPath(v1Path) != null)
            {
                AssetDatabase.DeleteAsset(v1Path);
                changes.Add("removed V1 atlas " + v1Path);
            }
            SpriteAtlasAsset atlas = SpriteAtlasAsset.Load(v2Path);
            if (atlas == null)
            {
                atlas = new SpriteAtlasAsset();
                SpriteAtlasAsset.Save(atlas, v2Path);
                changes.Add("created " + v2Path);
            }
            string[] dependencies = AssetDatabase.GetDependencies(v2Path, false);
            string[] slots = { "GAME_Platform_Top", "GAME_Platform_Fill", "GAME_Wall", "OBJ_Vine", "OBJ_Elevator", "OBJ_Plate", "OBJ_Gate", "OBJ_Station", "OBJ_Checkpoint", "OBJ_Hazard", "FG_01" };
            for (int i = 0; i < slots.Length; i++)
            {
                Sprite sprite = Load(prefix, slots[i]);
                if (sprite == null || Contains(dependencies, AssetDatabase.GetAssetPath(sprite))) continue;
                atlas.Add(new Object[] { sprite });
                SpriteAtlasAsset.Save(atlas, v2Path);
                changes.Add("added " + sprite.name + " to " + atlas.name);
            }
            SpriteAtlasImporter importer = AssetImporter.GetAtPath(v2Path) as SpriteAtlasImporter;
            if (importer != null && !importer.includeInBuild) { importer.includeInBuild = true; importer.SaveAndReimport(); changes.Add("set " + atlas.name + ".includeInBuild"); }
        }

        static bool Contains(string[] values, string candidate)
        {
            for (int i = 0; i < values.Length; i++) if (values[i] == candidate) return true;
            return false;
        }
    }
}
