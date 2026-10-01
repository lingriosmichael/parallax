using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Player;
using Parallax.Presentation;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A16 §2: the "better than the concept" measures, on the phone shot. Value spread (pixels under 20% and
    /// over 90% brightness), the featureless share (64 px tiles with almost no variation), the cat's contrast against the
    /// 1 u around it (WCAG ratio on linear luminance, at its checkpoint), each walkable lip against the air above it and the
    /// body below it, and the ambient motions (AmbientMotion and particle systems) and which thirds of the view they reach.</summary>
    public static partial class EnvironmentCapture
    {
        static float Linear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        static float LinLum(Color c) => 0.2126f * Linear(c.r) + 0.7152f * Linear(c.g) + 0.0722f * Linear(c.b);

        static void MeasureValues(Metrics m, Texture2D tex)
        {
            Color32[] px = tex.GetPixels32();
            int dark = 0, bright = 0;
            var lum = new float[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                float l = (0.2126f * px[i].r + 0.7152f * px[i].g + 0.0722f * px[i].b) / 255f;
                lum[i] = l;
                if (l < 0.2f) dark++; else if (l > 0.9f) bright++;
            }
            m.darkFraction = dark / (float)px.Length;
            m.brightFraction = bright / (float)px.Length;
            int tiles = 0, flat = 0;
            for (int ty = 0; ty + 64 <= tex.height; ty += 64)
                for (int tx = 0; tx + 64 <= tex.width; tx += 64)
                {
                    double sum = 0, sq = 0; int n = 0;
                    for (int y = ty; y < ty + 64; y += 2)
                        for (int x = tx; x < tx + 64; x += 2) { float l = lum[y * tex.width + x]; sum += l; sq += l * l; n++; }
                    double mean = sum / n, sd = System.Math.Sqrt(System.Math.Max(0, sq / n - mean * mean));
                    tiles++;
                    if (sd < 0.025) flat++;
                }
            m.featurelessFraction = flat / (float)tiles;
        }

        static void MeasureTiers(Metrics m, Texture2D tex, Camera cam, SoloRoomDefinition room)
        {
            // The cat against the 1 u around it (its own box and the solids skipped).
            CatMotor2D cat = Object.FindObjectsByType<CatMotor2D>(FindObjectsSortMode.None).FirstOrDefault();
            List<Rect> solids = SoloRoomSkin.Solids(room).Select(s => s.Rect).ToList();
            // The cat's body sprite (CatVisualPresenter.bodyRenderer), the visible one.
            SpriteRenderer catArt = Object.FindObjectsByType<Parallax.Gameplay.Presentation.CatVisualPresenter>(FindObjectsSortMode.None)
                .Select(p => new UnityEditor.SerializedObject(p).FindProperty("bodyRenderer").objectReferenceValue as SpriteRenderer)
                .FirstOrDefault(r => r != null && r.enabled && r.sprite != null && r.isVisible);
            if (catArt != null)
            {
                Bounds b = catArt.bounds;
                float catLum = DarkFifthLinLum(tex, cam, new Rect(b.min, b.size));
                var around = Rect.MinMaxRect(b.min.x - 1f, b.min.y - 0.2f, b.max.x + 1f, b.max.y + 1f);
                var skip = new List<Rect>(solids) { new(b.min, b.size) };
                float bg = MeanLinLum(tex, cam, around, skip);
                if (!float.IsNaN(catLum) && !float.IsNaN(bg)) m.catContrast = (Mathf.Max(catLum, bg) + 0.05f) / (Mathf.Min(catLum, bg) + 0.05f);
            }
            // Lip (the cap band), body (1.2–1.6 u down, blocks tall enough), air (0.35–0.65 u up), per walkable top in view.
            Rect view = ViewRect(cam, tex);
            List<Rect> traps = room.Elements.Where(e => e.Kind is not (SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall
                or SoloRoomElementKind.PitBottom or SoloRoomElementKind.Checkpoint or SoloRoomElementKind.Door or SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.FakePlatform))
                .Select(e => new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size)).ToList();
            var airSkip = new List<Rect>(solids); airSkip.AddRange(traps);
            if (cat != null) airSkip.Add(new Rect((Vector2)cat.transform.position - new Vector2(0.9f, 0.9f), new Vector2(1.8f, 1.8f)));
            float lipMin = float.MaxValue, bodyMin = float.MaxValue;
            foreach (SoloRoomSkin.Edge top in SoloRoomSkin.WalkableTops(room))
            {
                float from = Mathf.Max(top.From + 0.2f, view.xMin), to = Mathf.Min(top.To - 0.2f, view.xMax);
                if (to - from < 0.3f || top.Line < view.yMin + 1.7f || top.Line > view.yMax - 0.7f) continue;
                float lip = MeanLum(tex, cam, Rect.MinMaxRect(from, top.Line - 0.25f, to, top.Line - 0.05f), traps);
                float air = MeanLum(tex, cam, Rect.MinMaxRect(from, top.Line + 0.35f, to, top.Line + 0.65f), airSkip);
                if (float.IsNaN(lip) || float.IsNaN(air)) continue;
                float body = top.Owner.Rect.height >= 1.7f ? MeanLum(tex, cam, Rect.MinMaxRect(from, top.Line - 1.6f, to, top.Line - 1.2f), traps) : float.NaN;
                m.lips.Add(new Step { name = top.Owner.Name, value = lip - air, from = air - (float.IsNaN(body) ? air : body), to = air, line = top.Line });
                // §2.2 as revised (A16 results): the lip separates from the air by at least 0.15 either way.
                lipMin = Mathf.Min(lipMin, Mathf.Abs(lip - air));
                if (!float.IsNaN(body)) bodyMin = Mathf.Min(bodyMin, air - body);
            }
            m.lipOverAirMin = lipMin == float.MaxValue ? float.NaN : lipMin;
            m.airOverBodyMin = bodyMin == float.MaxValue ? float.NaN : bodyMin;
        }

        static float DarkFifthLinLum(Texture2D tex, Camera cam, Rect world)
        {
            // The sprite's box is mostly air: the cat is its darkest fifth.
            var lums = Pixels(tex, cam, world, null).Select(LinLum).OrderBy(v => v).ToList();
            return lums.Count < 8 ? float.NaN : lums.Take(System.Math.Max(4, lums.Count / 5)).Average();
        }

        static float MeanLinLum(Texture2D tex, Camera cam, Rect world, List<Rect> skip)
        {
            var lums = Pixels(tex, cam, world, skip).Select(LinLum).ToList();
            return lums.Count < 8 ? float.NaN : lums.Average();
        }

        static IEnumerable<Color> Pixels(Texture2D tex, Camera cam, Rect world, List<Rect> skip)
        {
            float viewH = cam.orthographicSize * 2f, viewW = viewH * tex.width / tex.height;
            Vector2 c = cam.transform.position;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((world.xMin - c.x) / viewW * tex.width + tex.width * 0.5f), 0, tex.width), x1 = Mathf.Clamp(Mathf.CeilToInt((world.xMax - c.x) / viewW * tex.width + tex.width * 0.5f), 0, tex.width);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((world.yMin - c.y) / viewH * tex.height + tex.height * 0.5f), 0, tex.height), y1 = Mathf.Clamp(Mathf.CeilToInt((world.yMax - c.y) / viewH * tex.height + tex.height * 0.5f), 0, tex.height);
            for (int y = y0; y < y1; y += 2)
                for (int x = x0; x < x1; x += 2)
                {
                    if (skip != null)
                    {
                        var p = new Vector2(c.x + (x + 0.5f - tex.width * 0.5f) / tex.width * viewW, c.y + (y + 0.5f - tex.height * 0.5f) / tex.height * viewH);
                        if (skip.Any(s => s.Contains(p))) continue;
                    }
                    yield return tex.GetPixel(x, y);
                }
        }

        static void MeasureMotion(Metrics m, Texture2D tex, Camera cam)
        {
            Rect view = ViewRect(cam, tex);
            var thirds = new bool[3];
            int count = 0;
            foreach (AmbientMotion a in Object.FindObjectsByType<AmbientMotion>(FindObjectsSortMode.None))
            {
                Renderer r = a.GetComponentInChildren<Renderer>();
                if (r == null || !r.enabled) continue;
                Rect b = new(r.bounds.min, r.bounds.size);
                if (!b.Overlaps(view)) continue;
                count++;
                for (int i = 0; i < 3; i++) if (b.Overlaps(new Rect(view.xMin + view.width * i / 3f, view.yMin, view.width / 3f, view.height))) thirds[i] = true;
            }
            foreach (ParticleSystem p in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (!p.isPlaying || p.particleCount == 0) continue;
                count++;
                thirds[0] = thirds[1] = thirds[2] = true;
            }
            m.motions = count;
            m.motionThirds = string.Join("", thirds.Select(t => t ? "1" : "0"));
        }
    

        static void MeasureDrawOrder(Metrics m, Texture2D tex, Camera cam)
        {
            Rect view = ViewRect(cam, tex);
            var visible = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.sharedMaterial != null && (cam.cullingMask & (1 << r.gameObject.layer)) != 0
                    && new Rect(r.bounds.min, r.bounds.size).Overlaps(view) && !(r is SpriteRenderer sr && sr.sprite == null))
                .OrderBy(r => SortingLayer.GetLayerValueFromID(r.sortingLayerID)).ThenBy(r => r.sortingOrder).ThenByDescending(r => r.transform.position.z).ToList();
            string last = null; int switches = 0;
            foreach (Renderer r in visible)
            {
                string shader = r.sharedMaterial.shader.name;
                if (shader != last) { switches++; m.drawOrder.Add($"{SortingLayer.IDToName(r.sortingLayerID)}:{r.sortingOrder} {shader} <- {r.name}"); }
                last = shader;
            }
            m.shaderSwitches = switches;
        }
    }
}
