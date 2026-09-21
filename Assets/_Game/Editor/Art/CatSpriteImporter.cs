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
    // Slices an AutoSprite cat export into named, pivoted sprites. Pure import-time
    // tooling: never runs at play time, never touches gameplay or collider data.
    public static class CatSpriteImporter
    {
        const int CellSize = 256;
        const byte OpaqueAlphaThreshold = 25; // ~10% alpha
        const string CatWalkPath = "Assets/_Game/Art/Cats/CatA/CatA_Walk.png";
        const string CatPlayerPrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        static readonly string[] CatASheetPaths =
        {
            CatWalkPath,
            "Assets/_Game/Art/Cats/CatA/CatA_Idle.png",
            "Assets/_Game/Art/Cats/CatA/CatA_Rise.png",
            "Assets/_Game/Art/Cats/CatA/CatA_Fall.png",
            "Assets/_Game/Art/Cats/CatA/CatA_Land.png",
        };

        [MenuItem("PARALLAX/Art/Import Cat Sheet")]
        public static void ImportSelected()
        {
            Texture2D selected = Selection.activeObject as Texture2D;
            if (selected == null)
            {
                Debug.LogError("CatSpriteImporter: select a cat sprite-sheet texture asset first.");
                return;
            }

            Import(AssetDatabase.GetAssetPath(selected));
        }

        [MenuItem("PARALLAX/Art/Import Cat A Walk")]
        public static void ImportCatAWalk() => Import(CatWalkPath);

        [MenuItem("PARALLAX/Art/Import Cat A Sheets")]
        public static void ImportCatASheets()
        {
            foreach (string path in CatASheetPaths)
                Import(path);
        }

        static void Import(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"CatSpriteImporter: '{path}' has no TextureImporter.");
                return;
            }

            // Temporarily force readable + uncompressed so we can analyze raw alpha.
            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogError($"CatSpriteImporter: could not load texture at '{path}' after reimport.");
                return;
            }

            int columns = Mathf.Max(1, texture.width / CellSize);
            int rows = Mathf.Max(1, texture.height / CellSize);
            int frameCount = columns * rows;
            string spriteNamePrefix = Path.GetFileNameWithoutExtension(path) + "_";

            Color32[] pixels = texture.GetPixels32();

            float colliderLength = GetCatColliderLength();
            if (colliderLength <= 0f)
            {
                Debug.LogError("CatSpriteImporter: could not read Cat_Player's collider length. Aborting before writing import settings.");
                return;
            }

            var frameRects = new Rect[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns; // row 0 = top row of the sheet
                int x = col * CellSize;
                int y = texture.height - (row + 1) * CellSize;
                frameRects[i] = new Rect(x, y, CellSize, CellSize);
            }

            (int frame0MinX, int frame0MaxX) = OpaquePixelBounds(pixels, texture.width, frameRects[0]);
            int frame0OpaqueWidth = frame0MaxX >= frame0MinX ? frame0MaxX - frame0MinX + 1 : 0;
            if (frame0OpaqueWidth <= 0)
            {
                Debug.LogError("CatSpriteImporter: frame 0 has no opaque pixels. Aborting.");
                return;
            }

            int pawRowLocal = LowestOpaqueRow(pixels, texture.width, frameRects);
            float pixelsPerUnit;
            Vector2 pivot;
            if (path == CatWalkPath)
            {
                pixelsPerUnit = frame0OpaqueWidth / colliderLength;
                float pivotXNormalized = (frame0MinX + frame0MaxX + 1) * 0.5f / CellSize;
                float pivotYNormalized = (pawRowLocal + 0.5f) / CellSize;
                pivot = new Vector2(pivotXNormalized, pivotYNormalized);
            }
            else if (!TryGetWalkImportSettings(out pixelsPerUnit, out pivot))
            {
                Debug.LogError(
                    $"CatSpriteImporter: import '{CatWalkPath}' before '{path}' so every cat frame can share its PPU and pivot.");
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>(frameCount);
            var nameFileIdPairs = new List<SpriteNameFileIdPair>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                string name = $"{spriteNamePrefix}{i:00}";
                GUID spriteID = DeterministicGuid(name);

                spriteRects.Add(new SpriteRect
                {
                    name = name,
                    rect = frameRects[i],
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = spriteID,
                });
                nameFileIdPairs.Add(new SpriteNameFileIdPair(name, spriteID));
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            var nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
            dataProvider.Apply();

            importer.isReadable = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            string[] importedSpriteNames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Select(sprite => sprite.name)
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToArray();

            Debug.Log(
                $"CatSpriteImporter: sliced {frameCount} frames ({columns}x{rows}) from '{path}'. " +
                $"PixelsPerUnit = {pixelsPerUnit:F3}. " +
                $"Pivot = ({pivot.x:F4}, {pivot.y:F4}) normalized. " +
                $"Source frame0 opaque width {frame0OpaqueWidth}px, bounds [{frame0MinX},{frame0MaxX}]px, baseline {pawRowLocal}px. " +
                $"spriteSheet.sprites = {importedSpriteNames.Length}: [{string.Join(", ", importedSpriteNames)}].");
        }

        static bool TryGetWalkImportSettings(out float pixelsPerUnit, out Vector2 pivot)
        {
            pixelsPerUnit = 0f;
            pivot = default;

            var importer = AssetImporter.GetAtPath(CatWalkPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                return false;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            foreach (SpriteRect spriteRect in dataProvider.GetSpriteRects())
            {
                if (spriteRect.name != "CatA_Walk_00") continue;
                pixelsPerUnit = importer.spritePixelsPerUnit;
                pivot = spriteRect.pivot;
                return pixelsPerUnit > 0f;
            }

            return false;
        }

        static float GetCatColliderLength()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"CatSpriteImporter: prefab not found at '{CatPlayerPrefabPath}'.");
                return 0f;
            }

            var collider = prefab.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                Debug.LogError($"CatSpriteImporter: '{CatPlayerPrefabPath}' has no CapsuleCollider2D.");
                return 0f;
            }

            return collider.direction == CapsuleDirection2D.Horizontal ? collider.size.x : collider.size.y;
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

        static bool IsOpaque(Color32[] pixels, int textureWidth, int px, int py) =>
            pixels[py * textureWidth + px].a > OpaqueAlphaThreshold;

        static (int minX, int maxX) OpaquePixelBounds(Color32[] pixels, int textureWidth, Rect frame)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int x0 = (int)frame.x, y0 = (int)frame.y;
            int size = (int)frame.width;

            for (int ly = 0; ly < size; ly++)
            {
                for (int lx = 0; lx < size; lx++)
                {
                    if (!IsOpaque(pixels, textureWidth, x0 + lx, y0 + ly)) continue;
                    if (lx < minX) minX = lx;
                    if (lx > maxX) maxX = lx;
                }
            }

            return (minX, maxX);
        }

        static int LowestOpaqueRow(Color32[] pixels, int textureWidth, Rect[] frames)
        {
            int lowest = int.MaxValue;

            foreach (Rect frame in frames)
            {
                int x0 = (int)frame.x, y0 = (int)frame.y;
                int size = (int)frame.width;

                for (int ly = 0; ly < size; ly++)
                {
                    bool rowHasOpaque = false;
                    for (int lx = 0; lx < size; lx++)
                    {
                        if (IsOpaque(pixels, textureWidth, x0 + lx, y0 + ly)) { rowHasOpaque = true; break; }
                    }
                    if (rowHasOpaque)
                    {
                        if (ly < lowest) lowest = ly;
                        break;
                    }
                }
            }

            return lowest == int.MaxValue ? 0 : lowest;
        }

    }
}
