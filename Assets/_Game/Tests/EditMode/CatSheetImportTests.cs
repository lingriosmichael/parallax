using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-A08 Stage 1 (§4, §10): the Cat A sheets as imported match the Stage 0 manifest: its PPU, each sheet's own cell
    // and pivot (one world pivot, per-sheet cells), ASTC 6x6 on Android, no mipmaps, bilinear, and the normal map as the
    // _NormalMap secondary texture. The environment's PPU is fixed (§4.4). Cat_Player's sprite references still resolve to
    // non-blank frames of the new art (§4.3: Cat Visual isn't re-run until V07).
    public sealed class CatSheetImportTests
    {
        const string ManifestPath = "Art_Source/AutoSprite/Cats/A/_import/manifest.json";
        const string SheetFolder = "Assets/_Game/Art/Cats/CatA/";
        const string CatPlayerPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";

        [Serializable] sealed class Slot { public string slot; public string file; public int frames; public int[] cell; public float[] pivotNormalized; public int[] sheetSize; }
        [Serializable] sealed class Manifest { public float ppu; public Slot[] slots; }

        static Manifest Load()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), ManifestPath);
            Assert.IsTrue(File.Exists(path), "the Stage 0 manifest is missing: " + ManifestPath);
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        }

        public static IEnumerable<TestCaseData> Slots() => Load().slots.Select(s => new TestCaseData(s.slot).SetName("CatSheet:" + s.slot));

        [TestCaseSource(nameof(Slots))]
        public void EverySheet_MatchesTheManifest(string slotName)
        {
            Manifest m = Load();
            Slot s = m.slots.Single(x => x.slot == slotName);
            string path = SheetFolder + s.file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer, path + " isn't imported");
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, path);
            Assert.AreEqual(SpriteImportMode.Multiple, importer.spriteImportMode, path);
            Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(m.ppu).Within(0.01f), path + " PPU");
            Assert.IsFalse(importer.mipmapEnabled, path + " mipmaps");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, path);
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden, path + " has no Android override");
            Assert.AreEqual(TextureImporterFormat.ASTC_6x6, android.format, path + " Android format");

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(x => x.name).ToArray();
            Assert.AreEqual(s.frames, sprites.Length, path + " frame count");
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sp = sprites[i];
                Assert.AreEqual(Path.GetFileNameWithoutExtension(s.file) + $"_{i:00}", sp.name);
                Assert.AreEqual(i * s.cell[0], sp.rect.x, 0.01f, sp.name + " cell x");
                Assert.AreEqual(0f, sp.rect.y, 0.01f, sp.name + " cell y");
                Assert.AreEqual(s.cell[0], sp.rect.width, 0.01f, sp.name + " cell width");
                Assert.AreEqual(s.cell[1], sp.rect.height, 0.01f, sp.name + " cell height");
                Vector2 pivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
                Assert.That(Vector2.Distance(pivot, new Vector2(s.pivotNormalized[0], s.pivotNormalized[1])), Is.LessThan(0.002f),
                    $"{sp.name} pivot {pivot}, manifest ({s.pivotNormalized[0]}, {s.pivotNormalized[1]})");
            }

            string normalPath = SheetFolder + Path.GetFileNameWithoutExtension(s.file) + "_n.png";
            SecondarySpriteTexture[] secondary = importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0];
            Assert.IsTrue(secondary.Any(t => t.name == "_NormalMap" && t.texture != null && AssetDatabase.GetAssetPath(t.texture) == normalPath),
                path + " has no _NormalMap secondary texture from " + normalPath);
            var normal = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            Assert.NotNull(normal, normalPath + " isn't imported");
            Assert.AreEqual(TextureImporterType.NormalMap, normal.textureType, normalPath);
            Assert.IsFalse(normal.mipmapEnabled, normalPath + " mipmaps");
            TextureImporterPlatformSettings normalAndroid = normal.GetPlatformTextureSettings("Android");
            Assert.IsTrue(normalAndroid.overridden && normalAndroid.format == TextureImporterFormat.ASTC_6x6,
                normalPath + " isn't ASTC 6x6 on Android");
            Assert.GreaterOrEqual(importer.maxTextureSize, s.sheetSize[0], path + " default max size downscales the sheet");
        }

        [Test]
        public void TheEnvironmentPpu_IsFixed_NotReadFromTheCat()
        {
            Type env = Type.GetType("Parallax.Editor.Art.EnvironmentSpriteImporter, Parallax.Editor");
            Assert.NotNull(env, "EnvironmentSpriteImporter not found");
            FieldInfo world = env.GetField("WorldPixelsPerUnit", BindingFlags.Public | BindingFlags.Static);
            FieldInfo background = env.GetField("BackgroundPixelsPerUnit", BindingFlags.Public | BindingFlags.Static);
            if (world == null || background == null) Assert.Fail("the environment PPU isn't pinned (it's read from CatA_Walk's importer)");
            Assert.That((float)world.GetValue(null), Is.EqualTo(196.66666f).Within(0.0001f));
            Assert.That((float)background.GetValue(null), Is.EqualTo(98.33333f).Within(0.0001f));
        }

        static float EnvironmentPpuFor(string path)
        {
            MethodInfo ppuFor = Type.GetType("Parallax.Editor.Art.EnvironmentSpriteImporter, Parallax.Editor")
                ?.GetMethod("PixelsPerUnitFor", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(ppuFor, "EnvironmentSpriteImporter.PixelsPerUnitFor not found");
            return (float)ppuFor.Invoke(null, new object[] { path });
        }

        [TestCase("Assets/_Game/Art/RealityA/Environment/A_OBJ_Door.png", 196.66666f)]
        [TestCase("Assets/_Game/Art/RealityA/Environment/A_GAME_Platform_Top.png", 196.66666f)]
        [TestCase("Assets/_Game/Art/RealityA/Environment/Backgrounds/A_BG_00_Sky.png", 98.33333f)]
        [TestCase("Assets/_Game/Art/RealityA/Environment/A_GAME_Wall.png", 98.33333f)]
        [TestCase("Assets/_Game/Art/RealityA/Environment/A_GAME_Platform_Fill.png", 98.33333f)]
        public void TheEnvironmentPpu_ByPath(string path, float expected)
        {
            Assert.That(EnvironmentPpuFor(path), Is.EqualTo(expected).Within(0.0001f), path);
        }

        // Every Reality A environment sprite already imported keeps the PPU the importer gives it (the old behaviour, §4.4).
        // Reality B is frozen and left out: its Platform_Fill and Wall carry 196.667 against the rule, from before A08.
        [Test]
        public void EveryRealityAEnvironmentSprite_HasTheImportersPpu()
        {
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Game/Art/RealityA/Environment" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".png")).ToArray();
            Assert.IsNotEmpty(paths, "no Reality A environment sprites found");
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(EnvironmentPpuFor(path)).Within(0.0001f), path);
            }
        }

        [Test]
        public void TheManifest_HasAllTwentySixSlots()
        {
            Assert.AreEqual(26, Load().slots.Length);
        }

        [Test]
        public void CatPlayer_EverySpriteReference_ResolvesToANonBlankFrameOfTheNewArt()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPlayerPath);
            Assert.NotNull(prefab, CatPlayerPath);
            var sprites = new List<(string where, Sprite sprite)>();
            foreach (Component c in prefab.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceInstanceIDValue != 0
                        && p.objectReferenceValue is Sprite sp)
                        sprites.Add(($"{c.GetType().Name}.{p.propertyPath}", sp));
                    else if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null
                             && p.objectReferenceInstanceIDValue != 0)
                        Assert.Fail($"{c.GetType().Name}.{p.propertyPath} references a missing object");
            }
            Assert.IsNotEmpty(sprites, "Cat_Player references no sprites");
            var pixels = new Dictionary<string, Texture2D>();
            foreach ((string where, Sprite sp) in sprites)
            {
                string path = AssetDatabase.GetAssetPath(sp);
                StringAssert.StartsWith(SheetFolder, path, where);
                if (!pixels.TryGetValue(path, out Texture2D source))
                {
                    source = new Texture2D(2, 2);
                    source.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), path)));
                    pixels[path] = source;
                }
                Slot slot = Load().slots.SingleOrDefault(x => SheetFolder + x.file == path);
                Assert.NotNull(slot, $"{where} -> {path} isn't a Stage 0 sheet");
                Assert.AreEqual(new Vector2Int(slot.sheetSize[0], slot.sheetSize[1]), new Vector2Int(source.width, source.height),
                    $"{where} -> {path} isn't the new art (sheet size)");
                Rect r = sp.rect;
                Color32[] block = source.GetPixels32();
                int opaque = 0;
                for (int y = (int)r.y; y < (int)(r.y + r.height); y++)
                    for (int x = (int)r.x; x < (int)(r.x + r.width); x++)
                        if (block[y * source.width + x].a > 25) opaque++;
                Assert.Greater(opaque, 500, $"{where} -> {sp.name} is blank");
            }
            foreach (Texture2D t in pixels.Values) UnityEngine.Object.DestroyImmediate(t);
        }
    }
}
