using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // Imports a cat's sprite sheets from its Stage 0 manifest (PAX-A08 §4): copies the registered sheets and normal maps
    // into the cat's art folder, then slices each sheet with the manifest's PPU and that sheet's own cell and pivot (one
    // world pivot, per-sheet cells). Pure import-time tooling: never runs at play time, never touches gameplay or
    // collider data. The paths are data, so Cat B reuses this with its own manifest.
    public static class CatSpriteImporter
    {
        const string CatAManifestPath = "Art_Source/AutoSprite/Cats/A/_import/manifest.json";
        const string CatASheetFolder = "Assets/_Game/Art/Cats/CatA/";
        const int AstcBlock = 6;
        const int AstcBlockBytes = 16;

        [Serializable] sealed class Slot { public string slot; public string file; public int frames; public int[] cell; public float[] pivotNormalized; public int[] sheetSize; public int astc6x6Bytes; }
        [Serializable] sealed class Manifest { public float ppu; public Slot[] slots; }

        [MenuItem("PARALLAX/Art/Import Cat Sheet")]
        public static void ImportSelected()
        {
            Texture2D selected = Selection.activeObject as Texture2D;
            if (selected == null)
            {
                Debug.LogError("CatSpriteImporter: select a cat sprite-sheet texture asset first.");
                return;
            }
            string path = AssetDatabase.GetAssetPath(selected);
            Manifest manifest = LoadManifest(CatAManifestPath);
            Slot slot = manifest?.slots.FirstOrDefault(s => CatASheetFolder + s.file == path);
            if (slot == null)
            {
                Debug.LogError($"CatSpriteImporter: '{path}' isn't a sheet in {CatAManifestPath}.");
                return;
            }
            ImportSheets(manifest, new[] { slot }, CatASheetFolder, CatAManifestPath);
        }

        [MenuItem("PARALLAX/Art/Import Cat A Sheets")]
        public static void ImportCatASheets()
        {
            Manifest manifest = LoadManifest(CatAManifestPath);
            if (manifest != null) ImportSheets(manifest, manifest.slots, CatASheetFolder, CatAManifestPath);
        }

        static Manifest LoadManifest(string manifestPath)
        {
            string full = Path.Combine(ProjectRoot, manifestPath);
            if (!File.Exists(full))
            {
                Debug.LogError($"CatSpriteImporter: the manifest '{manifestPath}' is missing. Run Tools/Art/cat_register.py first.");
                return null;
            }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(full));
            if (manifest == null || manifest.ppu <= 0f || manifest.slots == null || manifest.slots.Length == 0)
            {
                Debug.LogError($"CatSpriteImporter: '{manifestPath}' has no PPU or no slots.");
                return null;
            }
            return manifest;
        }

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        static void ImportSheets(Manifest manifest, IReadOnlyList<Slot> slots, string folder, string manifestPath)
        {
            string sourceFolder = Path.Combine(ProjectRoot, Path.GetDirectoryName(manifestPath), "sheets");
            foreach (Slot s in slots)
            {
                string problem = CheckEntry(s, Path.Combine(sourceFolder, s.file));
                if (problem == null) continue;
                Debug.LogError($"CatSpriteImporter: {s.slot}: {problem}. Stopping before any import.");
                return;
            }

            int copied = 0;
            foreach (Slot s in slots)
            {
                if (CopyIfChanged(Path.Combine(sourceFolder, s.file), folder + s.file)) copied++;
                if (CopyIfChanged(Path.Combine(sourceFolder, NormalName(s.file)), folder + NormalName(s.file))) copied++;
            }
            AssetDatabase.Refresh();

            var log = new StringBuilder();
            long colour = 0, normals = 0;
            foreach (Slot s in slots)
            {
                string sheet = folder + s.file, normal = folder + NormalName(s.file);
                if (!ImportNormal(normal) || !ImportSheet(sheet, normal, s, manifest.ppu)) return;
                long bytes = AstcBytes(s.sheetSize[0], s.sheetSize[1]);
                if (s.astc6x6Bytes > 0 && s.astc6x6Bytes != bytes)
                    Debug.LogWarning($"CatSpriteImporter: {s.slot}: ASTC 6x6 is {bytes} B here, {s.astc6x6Bytes} B in the manifest.");
                colour += bytes;
                normals += bytes;
                log.AppendLine($"  {s.slot}: {s.frames} frames, cell {s.cell[0]}x{s.cell[1]}, pivot ({s.pivotNormalized[0]:F5}, " +
                               $"{s.pivotNormalized[1]:F5}) = ({s.pivotNormalized[0] * s.cell[0]:F2}, {s.pivotNormalized[1] * s.cell[1]:F2}) px, " +
                               $"sheet {s.sheetSize[0]}x{s.sheetSize[1]}, ASTC 6x6 {bytes / 1024f:F1} KB (+ normal {bytes / 1024f:F1} KB)");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"CatSpriteImporter: imported {slots.Count} sheets from {manifestPath} ({copied} files copied). " +
                      $"PPU {manifest.ppu:F4}. Android ASTC 6x6: colour {colour / 1048576f:F2} MB, normals {normals / 1048576f:F2} MB, " +
                      $"total {(colour + normals) / 1048576f:F2} MB.\n{log}");
        }

        // Every sheet is one row of whole cells, and its normal map is the same size.
        static string CheckEntry(Slot s, string sourceSheet)
        {
            if (s.cell == null || s.cell.Length != 2 || s.pivotNormalized == null || s.pivotNormalized.Length != 2
                || s.sheetSize == null || s.sheetSize.Length != 2 || s.frames <= 0)
                return "the manifest entry is incomplete";
            if (s.sheetSize[0] != s.frames * s.cell[0] || s.sheetSize[1] != s.cell[1])
                return $"sheet {s.sheetSize[0]}x{s.sheetSize[1]} isn't {s.frames} cells of {s.cell[0]}x{s.cell[1]} in one row";
            if (!File.Exists(sourceSheet)) return "missing " + sourceSheet;
            string sourceNormal = Path.Combine(Path.GetDirectoryName(sourceSheet), NormalName(s.file));
            if (!File.Exists(sourceNormal)) return "missing " + sourceNormal;
            foreach (string file in new[] { sourceSheet, sourceNormal })
            {
                Vector2Int size = PngSize(file);
                if (size.x != s.sheetSize[0] || size.y != s.sheetSize[1])
                    return $"{Path.GetFileName(file)} is {size.x}x{size.y}, the manifest says {s.sheetSize[0]}x{s.sheetSize[1]}";
            }
            return null;
        }

        static string NormalName(string file) => Path.GetFileNameWithoutExtension(file) + "_n.png";

        // The width and height from a PNG's IHDR chunk (big-endian, bytes 16-23).
        static Vector2Int PngSize(string file)
        {
            var header = new byte[24];
            using (FileStream stream = File.OpenRead(file))
                if (stream.Read(header, 0, header.Length) != header.Length) return Vector2Int.zero;
            int Read(int at) => (header[at] << 24) | (header[at + 1] << 16) | (header[at + 2] << 8) | header[at + 3];
            return new Vector2Int(Read(16), Read(20));
        }

        static bool CopyIfChanged(string source, string target)
        {
            string full = Path.Combine(ProjectRoot, target);
            if (File.Exists(full) && File.ReadAllBytes(full).SequenceEqual(File.ReadAllBytes(source))) return false;
            File.Copy(source, full, true);   // an existing sheet is replaced in place: its .meta and GUID stay
            return true;
        }

        static long AstcBytes(int width, int height) =>
            (long)((width + AstcBlock - 1) / AstcBlock) * ((height + AstcBlock - 1) / AstcBlock) * AstcBlockBytes;

        static TextureImporterPlatformSettings AndroidAstc() => new TextureImporterPlatformSettings
        {
            name = "Android", overridden = true, maxTextureSize = 8192,
            format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50,
        };

        static bool ImportNormal(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"CatSpriteImporter: '{path}' has no TextureImporter.");
                return false;
            }
            importer.textureType = TextureImporterType.NormalMap;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SetPlatformTextureSettings(AndroidAstc());
            importer.SaveAndReimport();
            return true;
        }

        static bool ImportSheet(string path, string normalPath, Slot s, float ppu)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"CatSpriteImporter: '{path}' has no TextureImporter.");
                return false;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.maxTextureSize = 8192;   // Climb is 4800 px wide; the 2048 default would downscale it off Android
            importer.SetPlatformTextureSettings(AndroidAstc());
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal == null)
            {
                Debug.LogError($"CatSpriteImporter: no normal map at '{normalPath}'.");
                return false;
            }
            importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            string prefix = Path.GetFileNameWithoutExtension(path) + "_";
            var pivot = new Vector2(s.pivotNormalized[0], s.pivotNormalized[1]);
            var rects = new SpriteRect[s.frames];
            var pairs = new List<SpriteNameFileIdPair>(s.frames);
            for (int i = 0; i < s.frames; i++)
            {
                string name = $"{prefix}{i:00}";
                GUID spriteID = DeterministicGuid(name);
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * s.cell[0], 0, s.cell[0], s.cell[1]),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = spriteID,
                };
                pairs.Add(new SpriteNameFileIdPair(name, spriteID));
            }
            dataProvider.SetSpriteRects(rects);
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            dataProvider.Apply();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            int imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count();
            if (imported == s.frames) return true;
            Debug.LogError($"CatSpriteImporter: '{path}' has {imported} sprites after import, the manifest says {s.frames}.");
            return false;
        }

        // Stable per-name GUID so re-slicing the same sprite name keeps the same
        // internal fileID across reimports, preserving asset references.
        static GUID DeterministicGuid(string name)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{nameof(CatSpriteImporter)}:{name}"));
            var sb = new StringBuilder(32);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return new GUID(sb.ToString());
        }
    }
}
