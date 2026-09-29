using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13: rendering helpers for the contact sheet and the level screenshots. A camera, lights and an
    /// optional backdrop in the route harness's scratch scene; renders go to a RenderTexture and come back as textures.
    /// Nothing here is saved: the route session throws its scene away.</summary>
    static class TrapShots
    {
        public const float PhonePixelsPerUnit = 90f;   // a Pixel 8a landscape (1080 px tall) showing about 12 units

        public sealed class Rig : IDisposable
        {
            Camera camera;
            readonly List<GameObject> owned = new();

            /// <summary>The camera, and a global light of `globalIntensity` unless that's negative (the level screenshots copy
            /// the level's own lights instead: two global lights on one sorting layer conflict).</summary>
            public void Ensure(Color background, float globalIntensity)
            {
                if (camera != null) return;
                var go = new GameObject("__TrapShotCamera");
                owned.Add(go);
                camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.enabled = false;
                if (globalIntensity >= 0f) Light(Light2D.LightType.Global, Vector2.zero, globalIntensity, new Color(1f, 0.95f, 0.88f), 0f);
            }

            /// <summary>A 2D light, configured while its object is inactive (type, sorting layers and normal-map settings are
            /// read when it's enabled), then switched on.</summary>
            public Light2D Light(Light2D.LightType type, Vector2 at, float intensity, Color color, float radius, bool normals = false, float normalDistance = 1.5f, int[] targets = null)
            {
                var go = new GameObject("__TrapShotLight_" + type);
                go.SetActive(false);
                owned.Add(go);
                go.transform.position = new Vector3(at.x, at.y, 0f);
                var light = go.AddComponent<Light2D>();
                var so = new SerializedObject(light);
                so.FindProperty("m_LightType").intValue = (int)type;
                so.FindProperty("m_Intensity").floatValue = intensity;
                so.FindProperty("m_Color").colorValue = color;
                if (type == Light2D.LightType.Point) { so.FindProperty("m_PointLightOuterRadius").floatValue = radius; so.FindProperty("m_PointLightInnerRadius").floatValue = 0f; }
                so.FindProperty("m_NormalMapQuality").intValue = (int)(normals ? Light2D.NormalMapQuality.Accurate : Light2D.NormalMapQuality.Disabled);
                so.FindProperty("m_NormalMapDistance").floatValue = normalDistance;
                so.FindProperty("m_UseNormalMap").boolValue = normals;
                SerializedProperty layers = so.FindProperty("m_ApplyToSortingLayers");
                int[] ids = targets ?? SortingLayer.layers.Select(l => l.id).ToArray();
                layers.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++) layers.GetArrayElementAtIndex(i).intValue = ids[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                go.SetActive(true);
                return light;
            }

            public void Own(GameObject go) => owned.Add(go);

            public Texture2D Shot(Vector2 centre, float viewHeight, int width, int height)
            {
                camera.transform.position = new Vector3(centre.x, centre.y, -20f);
                camera.orthographicSize = viewHeight * 0.5f;
                camera.aspect = width / (float)height;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = rt;
                    camera.Render();
                    RenderTexture.active = rt;
                    // Kept alive past the route session, whose scene restore unloads unreferenced assets; freed by the caller.
                    var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    return tex;
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }

            public void Dispose()
            {
                foreach (GameObject go in owned) if (go != null) Object.DestroyImmediate(go);
                owned.Clear();
            }
        }

        /// <summary>Applies every trap's art in the open scenes, as the game's LateUpdate would.</summary>
        public static void ApplyArt()
        {
            foreach (TrapArt art in Object.FindObjectsByType<TrapArt>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) art.Apply();
        }

        public static RoomTrap FindTrap(string name) =>
            Object.FindObjectsByType<RoomTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);

        public static int TicksSinceFire(RoomTrap trap)
        {
            if (trap is GravityFlipTrap flip && flip.RearmTicksSinceFire >= 0) return flip.RearmTicksSinceFire;
            var rooms = Object.FindFirstObjectByType<RoomManager>();
            return trap == null || trap.LatestFireTick < 0 || rooms == null ? -1 : rooms.RoomLifeTick - trap.LatestFireTick;
        }

        public static void SavePng(Texture2D tex, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        // ---------- a small 5x7 bitmap font for labels ----------

        static readonly Dictionary<char, string> Glyphs = new()
        {
            ['A'] = "01110100011000111111100011000110001", ['B'] = "11110100011000111110100011000111110", ['C'] = "01110100011000010000100001000101110",
            ['D'] = "11110100011000110001100011000111110", ['E'] = "11111100001000011110100001000011111", ['F'] = "11111100001000011110100001000010000",
            ['G'] = "01110100011000010111100011000101111", ['H'] = "10001100011000111111100011000110001", ['I'] = "01110001000010000100001000010001110",
            ['J'] = "00111000100001000010000101001001100", ['K'] = "10001100101010011000101001001010001", ['L'] = "10000100001000010000100001000011111",
            ['M'] = "10001110111010110101100011000110001", ['N'] = "10001100011100110101100111000110001", ['O'] = "01110100011000110001100011000101110",
            ['P'] = "11110100011000111110100001000010000", ['Q'] = "01110100011000110001101011001001101", ['R'] = "11110100011000111110101001001010001",
            ['S'] = "01111100001000001110000010000111110", ['T'] = "11111001000010000100001000010000100", ['U'] = "10001100011000110001100011000101110",
            ['V'] = "10001100011000110001100010101000100", ['W'] = "10001100011000110101101011010101010", ['X'] = "10001100010101000100010101000110001",
            ['Y'] = "10001100010101000100001000010000100", ['Z'] = "11111000010001000100010001000011111",
            ['0'] = "01110100011001110101110011000101110", ['1'] = "00100011000010000100001000010001110", ['2'] = "01110100010000100010001000100011111",
            ['3'] = "11111000100010000010000011000101110", ['4'] = "00010001100101010010111110001000010", ['5'] = "11111100001111000001000011000101110",
            ['6'] = "00110010001000011110100011000101110", ['7'] = "11111000010001000100010000100001000", ['8'] = "01110100011000101110100011000101110",
            ['9'] = "01110100011000101111000010001001100", [' '] = "00000000000000000000000000000000000", ['-'] = "00000000000000011111000000000000000",
            ['.'] = "00000000000000000000000000110001100", [':'] = "00000011000110000000011000110000000", ['/'] = "00000000010001000100010001000000000",
            ['('] = "00010001000100001000010000010000010", [')'] = "01000001000001000010000100010001000", ['+'] = "00000001000010011111001000010000000",
            ['='] = "00000000001111100000111110000000000", [','] = "00000000000000000000000000110000100", ['_'] = "00000000000000000000000000000011111",
        };

        /// <summary>Draws `text` (upper-cased) at (x, y) from the top-left, scale px per font pixel.</summary>
        public static void Text(Texture2D tex, string text, int x, int y, int scale, Color color)
        {
            int cx = x;
            foreach (char raw in text.ToUpperInvariant())
            {
                if (!Glyphs.TryGetValue(raw, out string g)) g = Glyphs[' '];
                for (int row = 0; row < 7; row++)
                    for (int col = 0; col < 5; col++)
                        if (g[row * 5 + col] == '1')
                            for (int dy = 0; dy < scale; dy++)
                                for (int dx = 0; dx < scale; dx++)
                                {
                                    int px = cx + col * scale + dx, py = tex.height - 1 - (y + row * scale + dy);
                                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height) tex.SetPixel(px, py, color);
                                }
                cx += 6 * scale;
            }
        }

        public static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color32[tex.width * tex.height];
            Color32 c = color;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels32(pixels);
        }

        /// <summary>Copies `tile` into `sheet` with its top-left at (x, y) from the sheet's top-left.</summary>
        public static void Blit(Texture2D sheet, Texture2D tile, int x, int y) =>
            sheet.SetPixels(x, sheet.height - y - tile.height, tile.width, tile.height, tile.GetPixels());
    }
}
