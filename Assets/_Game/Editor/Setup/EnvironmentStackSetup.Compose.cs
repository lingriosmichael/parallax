using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Reality;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A16 gauntlet (the developer's brief): the composer. Every background element is a whole piece placed once
    /// (no band repeats across the view): clouds, the far skyline, the level's signature at mid distance, haze between the
    /// depths (aerial perspective), low fog and the water. Placement is seeded by the level's id, so a rebuild reproduces it
    /// and no two levels share a skyline. A piece goes only where its layer can show it over the camera's travel, and A2
    /// (FindLegalBase) still keeps flat tops off the walkable lines. Then the 2D lights: the sun's backlight following the
    /// camera and the cat's key light following the cat. Presentation only; nothing here touches a collider or a tick.</summary>
    public static partial class EnvironmentStackSetup
    {
        public sealed class ComposeContext
        {
            public TierContext Tier; public LevelPalettes.Palette Palette; public string LevelId, Signature;
            public System.Func<string, Color> Tint;
        }

        // A_BG_01's cluster is out (triage: low resolution, magenta baked into its mist).
        static readonly string[] FarSet = { "ENV_FarSpire_0", "ENV_FarIsland_1", "ENV_FarSpire_2", "ENV_FarIsland_2", "ENV_FarSpire_1", "ENV_FarIsland_3", "ENV_FarSpire_3", "ENV_FarIsland_0" };

        static readonly Dictionary<string, string[]> Signatures = new()
        {
            ["Aqueduct"] = new[] { "ENV_MidAqueduct", "ENV_MidTree_0" },
            ["Ruins"] = new[] { "ENV_MidRuins", "ENV_MidPillar" },
            ["Colonnade"] = new[] { "ENV_MidColonnade", "ENV_MidArch_1", "ENV_MidTree_1" },
            ["Towers"] = new[] { "ENV_MidTowers", "ENV_MidArch_0" },
            ["GreatTree"] = new[] { "ENV_MidTree_1", "ENV_MidRuins" },
            ["Temple"] = new[] { "ENV_MidRuins", "ENV_MidColonnade" },
            ["Waterfalls"] = new[] { "ENV_MidArch_1", "ENV_MidTowers" },
            ["FloatingIsles"] = new[] { "ENV_MidArch_0", "ENV_MidTowers" },
        };

        /// <summary>The paintings a level's signature shows at mid distance (the near tier skips them: no painting twice).</summary>
        static HashSet<string> SignatureSlots(string levelId) =>
            new(Signatures.TryGetValue(LevelPalettes.Signature(levelId), out string[] s) ? s : Signatures["Ruins"]);

        static float R(string id, int salt) => SoloRoomSkin.Hash(id ?? "template", salt);

        /// <summary>The x range (at mid travel) a layer at `speed` can show over the camera's travel, plus a margin.</summary>
        static (float from, float to) Visible(TierContext c, float speed, float margin = 1f)
        {
            float half = c.Travel.MaxViewWidth * 0.5f, travel = (c.Travel.Max.x - c.Travel.Min.x) * 0.5f;
            float reach = half + speed * travel + margin;
            return (c.Travel.Mid.x - reach, c.Travel.Mid.x + reach);
        }

        static void Compose(ComposeContext k)
        {
            TierContext c = k.Tier;
            LevelPalettes.Palette p = k.Palette;
            string id = k.LevelId;
            float view = c.Travel.ViewHeight;

            // Clouds: three whole cloud banks, soft enough to scale, at three depths.
            string[] cloudLayers = { "CloudsFar", "CloudsMid", "CloudsNear" };
            for (int i = 0; i < 3; i++)
            {
                LevelLookConfig.Layer def = c.Config.GetLayer(cloudLayers[i]);
                var (from, to) = Visible(c, def.Speed);
                float x = Mathf.Lerp(from, to, 0.15f + 0.7f * R(id, 100 + i));
                float y = c.Horizon + view * (0.18f + 0.14f * i + 0.08f * R(id, 110 + i));
                float s = 1.3f + 0.6f * R(id, 120 + i);
                Color tint = p.Clouds; tint.a = 0.55f + 0.15f * i;
                c.Place(c.Layer(cloudLayers[i]), "Cloud_" + i, "ENV_Cloud_" + ((i + (int)(R(id, 130) * 3)) % 3), new Vector2(x, y), Vector2.one * s, tint, def.Order, c.Back, R(id, 140 + i) > 0.5f);
            }

            // The far skyline: whole pieces, never larger than baked, swallowed by the haze toward the horizon.
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("Far");
                var (from, to) = Visible(c, def.Speed, 2f);
                Color tint = Color.Lerp(Color.white, p.Haze, p.FarHaze) * 0.98f; tint.a = 1f;
                int start = (int)(R(id, 200) * FarSet.Length);
                float x = from + R(id, 201) * 2f;
                for (int n = 0; x < to && n < FarSet.Length; n++)   // each far piece once
                {
                    string slot = FarSet[(start + n) % FarSet.Length];
                    Sprite sprite = EnvironmentKit.Sprite(slot);
                    if (sprite == null) continue;
                    float s = 0.55f + 0.4f * R(id, 210 + n);
                    Vector2 size = (Vector2)sprite.bounds.size * s;
                    bool island = slot.Contains("Island") || slot.Contains("Cluster");
                    float baseY = c.Horizon + (island ? 1.5f + 2.5f * R(id, 220 + n) : -1.2f + 1.0f * R(id, 230 + n));
                    c.Place(c.Layer("Far"), $"{slot}_far_{n}", slot, new Vector2(x + size.x * 0.5f, baseY + size.y * 0.5f), Vector2.one * s, tint, def.Order, c.Back, R(id, 240 + n) > 0.5f, -0.001f * n);
                    x += size.x * (0.6f + 0.5f * R(id, 250 + n)) + 0.8f;
                }
            }

            // Haze over the far skyline: the palette's haze rising from below the horizon, thinning upward (no texture).
            Gradient(c, "Haze", "FarHaze", c.Horizon - 3f, c.Horizon + view * 0.42f, p.Haze, p.FarHaze * 0.85f, up: true);

            // The signature at mid distance: its pieces, whole, standing from below the ground line.
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("Mid");
                var (from, to) = Visible(c, def.Speed, 0f);
                string[] set = Signatures.TryGetValue(k.Signature, out string[] s) ? s : Signatures["Ruins"];
                List<SoloRoomSkin.Edge> tops = c.Room.HasValue ? SoloRoomSkin.WalkableTops(c.Room.Value) : new List<SoloRoomSkin.Edge>();
                // P1-R4: the signature is the mid plane's subject: lightly hazed, standing well above the walk line.
                Color tint = Color.Lerp(Color.white, p.Haze, p.FarHaze * 0.55f) * 0.95f; tint.a = 1f;   // back enough not to read as a platform
                float span = to - from, x = from + span * (0.05f + 0.15f * R(id, 300));
                for (int n = 0; n < set.Length && x < to; n++)   // each signature piece once
                {
                    string slot = set[n % set.Length];
                    Sprite sprite = EnvironmentKit.Sprite(slot);
                    if (sprite == null) continue;
                    Vector2 size = sprite.bounds.size;
                    var bottom = new Vector2(x + size.x * 0.5f, c.Horizon - 1.2f - 0.8f * R(id, 310 + n));
                    x += size.x * (0.75f + 0.35f * R(id, 320 + n)) + 1.5f;
                    if (!FindLegalBase(slot, def.Speed, bottom, size, tops, c.Travel, out float legal)) { c.Changes.Add($"left out mid {slot} (A2)"); continue; }
                    bottom.y = legal;
                    SpriteRenderer r = c.Place(c.Layer("Mid"), $"{slot}_mid_{n}", slot, bottom + new Vector2(0f, size.y * 0.5f), Vector2.one, tint, def.Order, c.Middle, R(id, 330 + n) > 0.5f, -0.001f * n);
                    if (slot.Contains("Tree") && r != null) r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.4f, 0.08f, 0f, 0f, R(id, 340 + n));
                }
                if (k.Signature == "Waterfalls")
                    foreach (float f in new[] { 0.3f, 0.72f })
                        WaterfallAt(c, Mathf.Lerp(from, to, f), tint);
            }

            // A pale veil in front of the mid distance, behind the near tier: a third step of distance.
            Gradient(c, "Mid", "MidVeil", c.Horizon - 2f, c.Horizon + view * 0.4f, p.Haze, p.FarHaze * 0.25f, up: true);
            // Mist rising behind the walk line: the floor's lip stands against haze, not hard against the sky.
            Gradient(c, "Atmosphere", "MidMist", c.Horizon - 1.2f, c.Horizon + 2.2f, p.Haze, Mathf.Min(0.85f, p.Fog * 1.4f), up: true);

            // The water far below, wherever the room is open under its floors.
            {
                Sprite water = EnvironmentKit.Sprite("ENV_WaterPlane");
                if (water != null)
                {
                    LevelLookConfig.Layer def = c.Config.GetLayer("Mid");
                    var (from, to) = Visible(c, def.Speed, c.Travel.MaxViewWidth);
                    float sx = (to - from) / water.bounds.size.x;
                    Color tint = Color.Lerp(Color.white, p.Horizon, 0.35f); tint.a = 1f;
                    c.Place(c.Layer("Mid"), "Water", "ENV_WaterPlane", new Vector2((from + to) * 0.5f, c.Frame.min.y + 1.9f), new Vector2(sx, 1f), tint, def.Order + 1, c.Middle);
                }
            }

            // A low warm mist over the bottom of the view (the wall's foot): one quad on the camera, in front of the play layer,
            // too faint to hide anything (alpha 0.22 at the frame's edge, clear by a fifth of the view up).
            {
                Sprite fade = EnvironmentKit.Sprite("ENV_SkyFade");
                if (fade != null)
                {
                    var go = new GameObject("FootMist");
                    go.transform.SetParent(c.Camera.transform, false);
                    go.layer = c.LayerIndex;
                    go.transform.localPosition = new Vector3(0f, -view * 0.5f + view * 0.11f, 10f);
                    go.transform.localScale = new Vector3((c.Travel.MaxViewWidth + 2f) / fade.bounds.size.x, view * 0.22f / fade.bounds.size.y, 1f);
                    var r = go.AddComponent<SpriteRenderer>();
                    r.sprite = fade; r.sharedMaterial = EnvironmentKit.UnlitMaterial; r.flipY = true;
                    Color warm = Color.Lerp(p.Haze, p.SunLight, 0.4f); warm.a = 0.06f; r.color = warm;
                    r.sortingLayerName = c.Front; r.sortingOrder = c.Config.GetLayer("Foreground").Order + 1;
                }
            }

            PitVoids(c);
            Lights(k);
        }

        /// <summary>Round 9 (readability): every pit reads as a drop. From its floor's line down, the opening fades into the
        /// face shade's cool dark over 1.4 u (the backdrop behind it sinks into a void), then stays dark to the frame's bottom.
        /// Drawn behind the play layer (the pit's floor, spikes and walls draw over it), on the face shade's sprites.</summary>
        static void PitVoids(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            Sprite ramp = EnvironmentKit.Sprite("ENV_FaceShade"), solid = EnvironmentKit.Sprite("ENV_FaceShadeSolid");
            if (ramp == null || solid == null) return;
            Material m = EnvironmentKit.TileMaterial("Sprite") ?? EnvironmentKit.UnlitMaterial;
            string gameplay = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay);
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            int n = 0;
            foreach (SoloRoomElement e in room.Elements.Where(x => x.Kind == SoloRoomElementKind.Hazard && x.HazardRole == SoloRoomHazardRole.OpeningBottom))
            {
                var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                // The pit's lip: the lower of the floors at its two sides (a tall neighbour must not pull the void up the sky).
                float top = solids.Where(s => s.Rect.yMax > r.yMax && s.Rect.yMin < r.yMax + 0.5f && (Mathf.Abs(s.Rect.xMax - r.xMin) < 0.3f || Mathf.Abs(s.Rect.xMin - r.xMax) < 0.3f)).Select(s => s.Rect.yMax).DefaultIfEmpty(r.yMax + 2f).Min();
                // Down to the pit's own floor (its kill plane), never past it into open air below a raised floor (L012).
                float fadeTop = top - 0.1f, fadeH = 0.9f, bottom = r.yMin;
                void Quad(string name, Sprite s, float y0, float y1)
                {
                    var go = new GameObject(name);
                    go.transform.SetParent(c.EnvRoot, false);
                    go.layer = c.LayerIndex;
                    go.transform.position = new Vector3(r.center.x, (y0 + y1) * 0.5f, c.EnvRoot.position.z);
                    go.transform.localScale = new Vector3((r.width + 0.1f) / s.bounds.size.x, (y1 - y0) / s.bounds.size.y, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = s; sr.sharedMaterial = m; sr.color = new Color(0.62f, 0.42f, 0.3f, 0.97f);   // round 10: warm near-black (the sprite is cool; this neutralises it)
                    sr.sortingLayerName = gameplay; sr.sortingOrder = -4;
                }
                Quad($"PitVoid_{n}_Fade", ramp, fadeTop - fadeH, fadeTop);
                // A warm glow at the pit's lip, falling into the dark: the drop reads as depth, not fog.
                Sprite glowFade = EnvironmentKit.Sprite("ENV_SkyFade");
                if (glowFade != null)
                {
                    var g = new GameObject($"PitVoid_{n}_Warm");
                    g.transform.SetParent(c.EnvRoot, false);
                    g.layer = c.LayerIndex;
                    g.transform.position = new Vector3(r.center.x, fadeTop - 0.35f, c.EnvRoot.position.z - 0.001f);
                    g.transform.localScale = new Vector3((r.width + 0.1f) / glowFade.bounds.size.x, 0.7f / glowFade.bounds.size.y, 1f);
                    var gr = g.AddComponent<SpriteRenderer>();
                    gr.sprite = glowFade; gr.sharedMaterial = EnvironmentKit.UnlitMaterial;
                    Color warm = c.Palette != null ? c.Palette.SunLight : new Color(1f, 0.75f, 0.45f); warm.a = 0.35f; gr.color = warm;
                    gr.sortingLayerName = gameplay; gr.sortingOrder = -4;
                }
                if (fadeTop - fadeH > bottom) Quad($"PitVoid_{n}", solid, bottom, fadeTop - fadeH + 0.02f);
                n++;
            }
        }

        static void WaterfallAt(TierContext c, float x, Color tint)
        {
            Sprite fall = EnvironmentKit.Sprite("ENV_Waterfall");
            if (fall == null) return;
            LevelLookConfig.Layer def = c.Config.GetLayer("Mid");
            SpriteRenderer r = c.Place(c.Layer("Mid"), "Waterfall_" + Mathf.RoundToInt(x), "ENV_Waterfall", new Vector2(x, c.Horizon + 2f), Vector2.one, tint, def.Order - 1, c.Middle);
            if (r == null) return;
            r.drawMode = SpriteDrawMode.Tiled;   // the falling water scrolls down its own height (it's water: it moves)
            r.transform.localScale = Vector3.one;
            r.size = new Vector2(fall.bounds.size.x, c.Travel.ViewHeight + 6f);
        }

        /// <summary>A soft vertical gradient of `colour` between two heights over a layer's whole visible range: one quad, no
        /// texture to repeat. `up`: opaque at the bottom, clear at the top.</summary>
        static void Gradient(TierContext c, string layerName, string name, float y0, float y1, Color colour, float alpha, bool up)
        {
            Sprite fade = EnvironmentKit.Sprite("ENV_SkyFade");
            if (fade == null || alpha <= 0f) return;
            LevelLookConfig.Layer def = c.Config.GetLayer(layerName);
            // One cheap quad: a whole view wider than it can ever be seen on each side, so its ends never show.
            var (from, to) = Visible(c, def.Speed, c.Travel.MaxViewWidth);
            Color tint = colour; tint.a = alpha;
            SpriteRenderer r = c.Place(c.Layer(layerName), name, "ENV_SkyFade", new Vector2((from + to) * 0.5f, (y0 + y1) * 0.5f),
                new Vector2((to - from) / fade.bounds.size.x, (y1 - y0) / fade.bounds.size.y), tint, def.Order + 2, def.Speed < 0.3f ? c.Back : c.Middle);
            if (r == null) return;
            r.flipY = up;
            // Round 8: its opaque end continues into a soft fade (2 u), so a pit or a gap never shows the gradient's cut edge.
            float fadeY = up ? y0 - 1f : y1 + 1f;
            SpriteRenderer tail = c.Place(c.Layer(layerName), name + "_Fade", "ENV_SkyFade", new Vector2((from + to) * 0.5f, fadeY),
                new Vector2((to - from) / fade.bounds.size.x, 2f / fade.bounds.size.y), tint, def.Order + 2, def.Speed < 0.3f ? c.Back : c.Middle);
            if (tail != null) tail.flipY = !up;
        }

        // ---------- the 2D lights ----------

        static void Lights(ComposeContext k)
        {
            TierContext c = k.Tier;
            LevelPalettes.Palette p = k.Palette;
            int[] targets =
            {
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Background)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Middle)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Foreground)),
            };
            Light2D Point(string name, Color colour, float intensity, float inner, float outer, float falloff)
            {
                var go = new GameObject(name);
                go.transform.SetParent(c.EnvRoot, false);
                go.layer = c.LayerIndex;
                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = colour; light.intensity = intensity;
                light.pointLightInnerRadius = inner; light.pointLightOuterRadius = outer; light.falloffIntensity = falloff;
                light.targetSortingLayers = targets;
                return light;
            }

            // The sun's backlight: from the sun's side of the view, warm, wide, following the camera.
            float view = c.Travel.ViewHeight, width = view * 20f / 9f;
            var sunOffset = new Vector2((p.SunAt.x - 0.5f) * width, (p.SunAt.y - 0.5f) * view);
            Light2D sun = Point("SunLight", p.SunLight, p.SunIntensity, view * 0.15f, width * 0.85f, 0.45f);
            sun.gameObject.AddComponent<FollowTransform>().Configure(c.Camera.transform, sunOffset);

            // The cat's key light: a small warm light over its shoulder, so the painted cat reads (not a black shape).
            var follow = c.Camera.GetComponent<LevelCameraFollow>();
            Transform cat = follow != null ? new SerializedObject(follow).FindProperty("target").objectReferenceValue as Transform : null;
            if (cat == null) { c.Changes.Add("no cat light (the camera follows no target)"); return; }
            Light2D key = Point("CatLight", p.CatLight, p.CatIntensity, 0.4f, 2.2f, 0.5f);
            key.gameObject.AddComponent<FollowTransform>().Configure(cat, new Vector2(-0.35f, 0.9f));
        }
    }
}
