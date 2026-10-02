using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Reality;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A16 §3.1–3.5: what the A15 stack lacked. The sky plate, the sun's bloom halo and god rays, dust motes,
    /// the near-black foreground frame pinned to the view's corners and top, dark masonry outside the room (no void at
    /// the frame's edge), chains holding up thin slabs, and the post-processing volume on the level camera. All of it is
    /// presentation: no collider, trigger or tick is touched, and it lives outside the room (under Environment, or on
    /// the camera), so a saved room still matches a fresh build.</summary>
    public static partial class EnvironmentStackSetup
    {
        public const string FrameName = "EnvFrame";
        public const string VolumeProfilePath = "Assets/_Game/Data/LevelVolume.asset";
        public const string AdditiveShader = "Parallax/2D/Env-Sprite-Additive";
        static string AdditivePath(string name) => $"{EnvironmentKit.MaterialFolder}/ENV_Add{name}.mat";

        public delegate SpriteRenderer Placer(Transform layer, string name, string slot, Vector2 worldAtMid, Vector2 scale, Color color, int order, string band, bool flip = false, float z = 0f);

        public sealed class TierContext
        {
            public RealityRoot Root; public Camera Camera; public Bounds Frame; public SoloRoomDefinition? Room;
            public LevelLookConfig Config; public LevelLookConfig.Grade Grade; public Travel Travel; public Transform EnvRoot;
            public System.Func<string, Transform> Layer; public Placer Place;
            public Vector2 SunAt; public float SunSize, Horizon; public int LayerIndex;
            public string Back, Middle, Front; public List<string> Changes;
            public LevelPalettes.Palette Palette; public string LevelId;
        }

        // ---------- assets (the setup menu) ----------

        /// <summary>The cap's HDR boost, the additive materials, and the post-processing profile (idempotent).</summary>
        public static void EnsureTierAssets(LevelLookConfig config, List<string> changes)
        {
            foreach (string kind in new[] { "Cap", "CapWash" })
            {
                Material m = EnvironmentKit.TileMaterial(kind);
                if (m == null || m.GetColor("_Color") == config.CapBoost) continue;
                m.SetColor("_Color", config.CapBoost);
                EditorUtility.SetDirty(m);
                changes.Add("boosted ENV_" + kind + " (the lit lip)");
            }
            Shader additive = Shader.Find(AdditiveShader);
            if (additive == null) { Debug.LogError("Environment Stack: shader " + AdditiveShader + " not found."); return; }
            Additive("Glow", additive, config.SunHaloIntensity, null, changes);
            // Round 8: the god rays add light (alpha-blended cream over a bright sky was invisible); one more SetPass.
            Additive("Ray", additive, 0.55f, null, changes);
            // The motes draw on the same unlit shader as the background bands (no shader switch), with their texture.
            Sprite mote = EnvironmentKit.Sprite("ENV_Mote");
            Shader unlit = EnvironmentKit.UnlitMaterial != null ? EnvironmentKit.UnlitMaterial.shader : null;
            if (unlit != null && mote != null) Plain("Mote", unlit, mote.texture, changes);
            EnsureVolumeProfile(changes);
            SoftenCatOutline(changes);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Gauntlet (the brief: "the cat must read as the painted cat"): the cat's readability outline becomes a soft
        /// pale rim from above, faint, instead of a hard yellow line; the key light carries the cat's read.</summary>
        static void SoftenCatOutline(List<string> changes)
        {
            var outline = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Cat_OutlineUnlit.mat");
            // Phase 2 round 2: neutral pale and fainter (the warm rim read as a jagged yellow cut-out on the true-black cat).
            var soft = new Color(1f, 0.95f, 0.88f, 0.38f);
            // Round 3: the presenter overrides the material's colour from the cat's visual config (Reality A's solid orange
            // line, 1 texel, read as a jagged cut-out at the 1.8 zoom). Same warm hue, paler, at 15% (40% and 28% read as a
            // light dotted fringe).
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Game/Data/CatA_VisualConfig.asset");
            if (config != null)
            {
                var so = new SerializedObject(config);
                SerializedProperty colourA = so.FindProperty("outlineColorA");
                var rim = new Color(1f, 0.86f, 0.62f, 0.15f);
                if (colourA != null && colourA.colorValue != rim)
                {
                    colourA.colorValue = rim; so.ApplyModifiedPropertiesWithoutUndo();
                    changes.Add("the cat's Reality A outline is a paler warm rim at 15%");
                }
            }
            if (outline == null || !outline.HasProperty("_OutlineColor")) return;
            if (outline.GetColor("_OutlineColor") == soft && (!outline.HasProperty("_RimTop") || Mathf.Approximately(outline.GetFloat("_RimTop"), 0.9f))) return;
            outline.SetColor("_OutlineColor", soft);
            if (outline.HasProperty("_RimTop")) outline.SetFloat("_RimTop", 0.9f);
            EditorUtility.SetDirty(outline);
            changes.Add("the cat's outline is a warm rim lit from above");
        }

        static void Additive(string name, Shader shader, float intensity, Texture texture, List<string> changes)
        {
            string path = AdditivePath(name);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = m == null;
            if (created) { m = new Material(shader) { name = "ENV_Add" + name }; AssetDatabase.CreateAsset(m, path); }
            if (!created && m.shader == shader && Mathf.Approximately(m.GetFloat("_Intensity"), intensity) && m.GetTexture("_MainTex") == texture) return;
            m.shader = shader; m.SetFloat("_Intensity", intensity);
            if (texture != null) m.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(m);
            changes.Add((created ? "created " : "configured ") + path);
        }

        static Material AdditiveMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(AdditivePath(name));

        static void Plain(string name, Shader shader, Texture texture, List<string> changes)
        {
            string path = $"{EnvironmentKit.MaterialFolder}/ENV_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = m == null;
            if (created) { m = new Material(shader) { name = "ENV_" + name }; AssetDatabase.CreateAsset(m, path); }
            if (!created && m.shader == shader && m.mainTexture == texture) return;
            m.shader = shader; m.mainTexture = texture;
            EditorUtility.SetDirty(m);
            changes.Add((created ? "created " : "configured ") + path);
        }

        static Material PlainMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{EnvironmentKit.MaterialFolder}/ENV_{name}.mat");
        /// <summary>The warm additive glow (the sun's halo, the door's glow); null until the setup menu has made it.</summary>
        public static Material GlowMaterial => AdditiveMaterial("Glow");

        /// <summary>§3.2: one profile for every level (Neutral tonemapping, gentle contrast, warm split toning, bloom on
        /// HDR highlights, a dark warm vignette). Created once, then its values re-applied (idempotent, same asset).</summary>
        public static void EnsureVolumeProfile(List<string> changes) => EnsureVolumeProfile(changes, VolumeProfilePath, null);

        /// <summary>Gauntlet: a level's own profile from its palette (exposure, contrast, saturation, bloom); the shared one (no
        /// palette) keeps the round-3 constants. Round 2 (the developer): the grade never tints the cat or the hazards, so the
        /// profile carries no hue (white balance, split toning and the lift stay neutral; saturation within ±SaturationCap) and
        /// the level's colour lives in its environment; and no blown-out whites, so Neutral tonemapping rolls the highlights off
        /// (round 5: exposure capped at MaxPostExposure, so the cat keeps its true darks).</summary>
        public static VolumeProfile EnsureVolumeProfile(List<string> changes, string path, LevelPalettes.Palette p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                changes.Add("created " + path);
            }
            T Get<T>() where T : VolumeComponent
            {
                if (profile.TryGet(out T existing)) return existing;
                T c = profile.Add<T>(true);
                c.name = typeof(T).Name;
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profile);
                return c;
            }
            // Round 2: Neutral rolls the highlights off, so the brightest areas keep their detail.
            Get<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            ColorAdjustments color = Get<ColorAdjustments>();
            float saturation = Mathf.Clamp(p?.Saturation ?? PostSaturation, -SaturationCap, SaturationCap);
            color.contrast.Override(p?.Contrast ?? PostContrast); color.saturation.Override(saturation);
            // Round 5 (the cat's true colour): the post exposure lifted the cat with the scene (L005 +0.55 EV: its darkest 30 →
            // 53); at most +0.15 now, and no extra for the tonemapper. The scene's brightness lives in its own pieces.
            color.postExposure.Override(Mathf.Min(p?.Exposure ?? PostExposure, MaxPostExposure));
            color.colorFilter.Override(Color.white);
            // Hue-neutral (round 2): no lift (an equal pull on every channel, stretched by the contrast, crushed the cat's warm
            // near-black fur to red: (21,15,12) rendered (18,0,0) to (63,24,3)); no white balance or split toning.
            LiftGammaGain lgg = Get<LiftGammaGain>();
            lgg.lift.Override(new Vector4(1f, 1f, 1f, p != null ? 0f : PostLift));
            WhiteBalance wb = Get<WhiteBalance>();
            wb.temperature.Override(0f); wb.tint.Override(0f);
            SplitToning split = Get<SplitToning>();
            split.shadows.Override(Hex(PostShadows)); split.highlights.Override(Hex(PostHighlights)); split.balance.Override(PostBalance);
            Bloom bloom = Get<Bloom>();
            float bloomIntensity = p?.Bloom ?? PostBloom;
            bloom.active = bloomIntensity > 0f;   // gauntlet: on, quarter size, two steps (its SetPass cost is in the round's report)
            bloom.threshold.Override(0.92f); bloom.intensity.Override(bloomIntensity); bloom.scatter.Override(0.72f); bloom.tint.Override(Hex("#FFF2E0"));
            // Mobile: a quarter-size, three-step bloom (each step is a pass; SetPass ≤ 30 with post on, §2.5).
            bloom.downscale.Override(BloomDownscaleMode.Quarter); bloom.maxIterations.Override(2);
            Vignette vignette = Get<Vignette>();
            vignette.intensity.Override(PostVignette); vignette.smoothness.Override(0.45f); vignette.color.Override(Hex("#1A0F08"));
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // The grade's tunables (measured against the captures; §3.2's starting values were too strong on top of the
        // painted art's own saturation).
        // Round 3: more contrast, less exposure, a deeper vignette (the concept's dark frame against a bright core).
        const float PostContrast = 20f, PostSaturation = -3f, PostExposure = 0.45f, PostLift = -0.035f, PostBalance = 0f, PostBloom = 0f, PostVignette = 0.2f;
        /// <summary>Round 2: the most a level's grade may change saturation (it changes how strong the hazards' colours read).</summary>
        public const float SaturationCap = 5f;
        /// <summary>Round 5: the most a level's post exposure may lift (it lifts the cat with the scene).</summary>
        const float MaxPostExposure = 0.15f;
        // Split toning neutral (grey is no shift): it flattened every grade into one amber; the grades carry the colour.
        const string PostShadows = "#808080", PostHighlights = "#808080";

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        // ---------- the sky plate ----------

        static void SkyPlate(Transform sky, Vector2 mid, Vector2 size, LevelLookConfig.Grade grade, int order, string band, Placer place, string slot = "ENV_SkyPlate")
        {
            Sprite plate = EnvironmentKit.Sprite(slot) ?? EnvironmentKit.Sprite("ENV_SkyPlate"), fade = EnvironmentKit.Sprite("ENV_SkyFade");
            if (plate == null) return;
            float k = Mathf.Max(size.x / plate.bounds.size.x, size.y / plate.bounds.size.y);
            // Gauntlet: the palette's own sky plate (already mapped to its colours), untinted.
            place(sky, "Plate", plate.name, mid, Vector2.one * k, Color.white, order, band);
            if (fade == null) return;
            Color top = grade.SkyTop * 0.72f; top.a = 0.55f;
            place(sky, "PlateTop", "ENV_SkyFade", mid + new Vector2(0f, size.y * 0.25f), new Vector2(size.x / fade.bounds.size.x, size.y * 0.5f / fade.bounds.size.y), top, order + 1, band);
        }

        // ---------- the tiers ----------

        static void BuildTiers(TierContext c)
        {
            Transform oldFrame = c.Camera.transform.Find(FrameName);
            if (oldFrame != null) Object.DestroyImmediate(oldFrame.gameObject);
            SunHaloAndRays(c);
            IslandsBob(c);
            Motes(c);
            ForegroundFrame(c);
            if (c.Room.HasValue)
            {
                Compose(new ComposeContext { Tier = c, Palette = c.Palette, LevelId = c.LevelId, Signature = LevelPalettes.Signature(c.LevelId) });
                NearTier(c);
                Outside(c);
                Chains(c);
                // Phase 2 round 4: no arch fringe (Fringes). Critics, two rounds: "arcade strips hard-cut at their ends",
                // "ghost pillars under the bridge like a broken reflection" (L008, L020); a floating floor shows its underside.
            }
            PostVolume(c);
        }

        static void SunHaloAndRays(TierContext c)
        {
            // Plain alpha sprites on the background's unlit shader: the back of the scene draws without a shader switch
            // (SetPass ≤ 30 with post on, §2.5). The door's glow keeps the additive material.
            Material glow = EnvironmentKit.UnlitMaterial, rayMaterial = AdditiveMaterial("Ray") ?? EnvironmentKit.UnlitMaterial;
            Sprite disc = EnvironmentKit.Sprite("ENV_Glow"), ray = EnvironmentKit.Sprite("ENV_Ray");
            if (glow != null && disc != null)
            {
                // Round 3 (critics on L010: "a daytime yellow glow blob in a night sky"): the halo takes the disc's own colour,
                // and a moon's is half the size.
                bool moon = c.Palette != null && c.Palette.Moon;
                Color haloColour = c.Palette != null ? Color.Lerp(Color.white, c.Palette.Sun, 0.6f) : new Color(1f, 0.98f, 0.93f, 1f); haloColour.a = 1f;
                SpriteRenderer halo = c.Place(c.Layer("Sun"), "SunHalo", "ENV_Glow", c.SunAt, Vector2.one * (c.SunSize * c.Config.SunHaloScale * (moon ? 0.5f : 1f) / disc.bounds.size.x), haloColour, c.Config.GetLayer("Sun").Order + 1, c.Back);
                halo.sharedMaterial = glow;
                halo.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0f, 0f, 0.04f, 0.1f, 0.2f);
            }
            if (rayMaterial == null || ray == null || c.Config.Rays <= 0) return;
            if (c.Palette != null && c.Palette.Moon) return;   // round 3: no sunbeams at night
            Transform layer = c.Layer("Rays");
            // The beam is painted falling to the right; mirrored when the sun is right of the view's middle.
            bool flip = c.SunAt.x > c.Travel.Mid.x;
            Vector2 size = ray.bounds.size;
            float scale = c.Travel.ViewHeight * 1.05f / size.y;
            for (int i = 0; i < c.Config.Rays; i++)
            {
                float spread = (i - (c.Config.Rays - 1) * 0.5f) * size.x * scale * (0.45f + 0.3f * SoloRoomSkin.Hash(c.LevelId ?? "template", 700 + i))
                    + (SoloRoomSkin.Hash(c.LevelId ?? "template", 710) - 0.5f) * c.Travel.ViewHeight * 0.6f;   // Phase 2 round 2
                // The beams start near the view's top on the sun's side (a low sun's beams would fall under the floor).
                var origin = new Vector2(c.SunAt.x, Mathf.Max(c.SunAt.y, c.Travel.Mid.y + c.Travel.ViewHeight * 0.3f));
                Vector2 at = origin + new Vector2((flip ? -1f : 1f) * size.x * scale * 0.35f + spread, -size.y * scale * 0.45f);
                // Gauntlet: the sun's own colour (the ambient's grey vanished against the sky).
                Color tint = Color.Lerp(c.Palette != null ? c.Palette.SunLight : c.Grade.Light, Color.white, 0.35f); tint.a = c.Config.RayAlpha * (i == 1 ? 1f : 0.75f);
                SpriteRenderer r = c.Place(layer, "Ray_" + i, "ENV_Ray", at, new Vector2(scale * (0.8f + 0.15f * i), scale), tint, c.Config.GetLayer("Rays").Order, c.Middle, flip);
                r.sharedMaterial = rayMaterial;
                r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.6f, 0.05f, 0.12f, 0.125f, i * 0.31f);
            }
        }

        static void IslandsBob(TierContext c)
        {
            Transform far = c.EnvRoot.Find("Far");
            if (far == null) return;
            int i = 0;
            foreach (Transform piece in far)
                if (piece.name.Contains("Island"))
                    piece.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0.08f, 0.12f, 0f, 0f, 0f, 0f, SoloRoomSkin.Hash(piece.name, i++));
        }

        /// <summary>Dust motes over the view: a built-in ParticleSystem on the camera, simulated in world space, prewarmed,
        /// with a fixed seed (the same look on every run).</summary>
        static void Motes(TierContext c)
        {
            Material mote = PlainMaterial("Mote");
            if (mote == null || c.Config.Motes <= 0) return;
            Transform frame = FrameRoot(c);
            var go = new GameObject("Motes");
            go.transform.SetParent(frame, false);
            go.transform.localPosition = new Vector3(0f, 0f, 10f);
            go.layer = c.LayerIndex;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false; ps.randomSeed = 1601;
            ParticleSystem.MainModule main = ps.main;
            main.loop = true; main.prewarm = true; main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.7f, 0.35f), new Color(1f, 0.82f, 0.55f, 0.8f));
            main.maxParticles = c.Config.Motes;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = c.Config.Motes / 9f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(c.Travel.MaxViewWidth + 2f, c.Travel.ViewHeight, 0.1f);
            shape.rotation = Vector3.zero;
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.1f); velocity.y = new ParticleSystem.MinMaxCurve(0.04f, 0.1f); velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mote;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = c.Middle; renderer.sortingOrder = c.Config.GetLayer("BackWall").Order + 1;   // round 4: in front of the back walls (the rays went behind them)
            ps.Play();
        }

        static Transform FrameRoot(TierContext c)
        {
            Transform t = c.Camera.transform.Find(FrameName);
            if (t != null) return t;
            t = new GameObject(FrameName).transform;
            t.SetParent(c.Camera.transform, false);
            t.localPosition = Vector3.zero;
            t.gameObject.layer = c.LayerIndex;
            return t;
        }

        /// <summary>Tier 7: near-black foliage in the view's lower corners and a curtain of dark vines along its top, pinned
        /// to the view (ViewportAnchor) at every aspect. They cover only the frame's outer band.</summary>
        static void ForegroundFrame(TierContext c)
        {
            Transform frame = FrameRoot(c);
            foreach (string name in new[] { "FrameL", "FrameR", "FrameTop", "FrameHang", "FrameBanner", "FramePillar" })
            {
                Transform old = frame.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Sprite corner = EnvironmentKit.Sprite("ENV_FrameL"), cornerR = EnvironmentKit.Sprite("ENV_FrameR"), top = EnvironmentKit.Sprite("ENV_FrameTop");
            Material unlit = EnvironmentKit.UnlitMaterial;
            int order = c.Config.GetLayer("Foreground").Order + 5;
            // The frame's scales were set at D-100's 13.33 u view; it keeps its share of the screen at any zoom (D-102).
            float vk = c.Travel.ViewHeight / (16f / 1.2f);
            if (corner != null && c.Config.FrameCornerScale > 0f)
            {
                float k = c.Config.FrameCornerScale * vk;
                Vector2 size = (Vector2)corner.bounds.size * k;
                foreach (bool right in new[] { false, true })
                {
                    // Gauntlet: the right corner is its own composition (a mirror of the left read as a twin).
                    Sprite cs = right && cornerR != null ? cornerR : corner;
                    size = (Vector2)cs.bounds.size * k;
                    SpriteRenderer r = Pinned(frame, right ? "FrameR" : "FrameL", cs, unlit, c, order);
                    r.flipX = right;
                    r.transform.localScale = new Vector3(k, k, 1f);
                    r.GetComponent<ViewportAnchor>().Configure(c.Camera, new Vector2(right ? 1f : -1f, -1f),
                        new Vector2((right ? -1f : 1f) * (size.x * 0.5f - size.x * 0.18f), size.y * 0.5f - size.y * 0.22f), 0f);
                    r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.6f, 0.07f, 0f, 0f, right ? 0.5f : 0f);
                }
            }
            if (top != null && c.Config.FrameTopScale > 0f)
            {
                float k = c.Config.FrameTopScale * vk;
                SpriteRenderer r = Pinned(frame, "FrameTop", top, unlit, c, order);
                r.drawMode = SpriteDrawMode.Tiled;
                r.transform.localScale = new Vector3(k, k, 1f);
                r.size = new Vector2((c.Travel.MaxViewWidth + 2f) / k, top.bounds.size.y);
                r.GetComponent<ViewportAnchor>().Configure(c.Camera, new Vector2(0f, 1f), new Vector2(0f, -top.bounds.size.y * k * 0.5f + 0.05f), 0f);
            }
            // Round 3 (the concept's framing): roots hanging into the top-left corner, a banner and vines in the top right, and a
            // dark column at the left edge. Pinned to the view like the corners; they cover only its outer band.
            Sprite hang = EnvironmentKit.Sprite("ENV_FrameHang"), banner = EnvironmentKit.Sprite("ENV_FrameBanner"), pillar = EnvironmentKit.Sprite("ENV_FramePillar");
            float kh = c.Config.FrameHangScale * vk;
            foreach ((Sprite sprite, string name, float side) in new[] { (hang, "FrameHang", -1f), (banner, "FrameBanner", 1f) })
            {
                if (sprite == null || kh <= 0f) continue;
                Vector2 size = (Vector2)sprite.bounds.size * kh;
                SpriteRenderer r = Pinned(frame, name, sprite, unlit, c, order + 1);
                r.transform.localScale = new Vector3(kh, kh, 1f);
                r.GetComponent<ViewportAnchor>().Configure(c.Camera, new Vector2(side, 1f), new Vector2(-side * size.x * 0.42f, -size.y * 0.5f + 0.05f), 0f);
                r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.8f, 0.05f, 0f, 0f, side > 0f ? 0.3f : 0.7f);
            }
            if (pillar != null && c.Config.FramePillarScale > 0f)
            {
                float kp = c.Config.FramePillarScale * vk;
                Vector2 size = (Vector2)pillar.bounds.size * kp;
                SpriteRenderer r = Pinned(frame, "FramePillar", pillar, unlit, c, order + 2);
                r.transform.localScale = new Vector3(kp, kp, 1f);
                r.GetComponent<ViewportAnchor>().Configure(c.Camera, new Vector2(-1f, 0f), new Vector2(size.x * (c.Config.FramePillarShown - 0.5f), 0f), 0f);
            }
        }

        static SpriteRenderer Pinned(Transform frame, string name, Sprite sprite, Material material, TierContext c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(frame, false);
            go.transform.localPosition = new Vector3(0f, 0f, 10f);
            go.layer = c.LayerIndex;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sharedMaterial = material;
            r.sortingLayerName = c.Front; r.sortingOrder = order;
            go.AddComponent<ViewportAnchor>();
            return r;
        }

        // A16 round 2 (the developer: "missing intermediate to closer assets that look slightly bigger"): the near tier.
        static readonly string[] NearPalette = { "ENV_MidArch_0", "ENV_MidTowers", "ENV_MidTree_1", "ENV_MidArch_1", "ENV_MidColonnade", "ENV_MidTree_0", "ENV_MidPillar" };

        /// <summary>Ruins and trees at 1.3x, standing from below the lowest floor, spaced along the whole frame, on the Near
        /// layer (speed 0.6, lightly hazed). Each level starts the palette at its own place and alternates flips, so no two
        /// levels repeat the same row. A2 holds (FindLegalBase slides a piece whose flat top would line up with a floor),
        /// and nothing stands behind the door (A6).</summary>
        static void NearTier(TierContext c)
        {
            LevelLookConfig.Layer def = c.Config.GetLayer("Near");
            if (def == null) return;
            SoloRoomDefinition room = c.Room.Value;
            List<SoloRoomSkin.Edge> tops = SoloRoomSkin.WalkableTops(room);
            float ground = tops.Count > 0 ? tops.Min(t => t.Line) : c.Frame.min.y + 1f;
            var doors = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door)
                .Select(e => new Rect(room.Origin + e.Position - e.Size * 0.5f - Vector2.one * 2.5f, e.Size + Vector2.one * 5f)).ToList();
            string id = LevelLooks.LevelOf(room) ?? "default";
            int start = (int)(SoloRoomSkin.Hash(id, 7) * NearPalette.Length);
            Color tint = Color.Lerp(Color.white, c.Grade.Haze, def.HazeMix) * def.Value; tint.a = def.Alpha;
            float scale = c.Config.NearScale;
            Transform layer = null;
            float x = c.Frame.min.x + 1.5f + SoloRoomSkin.Hash(id, 8) * 3f;
            for (int k = 0; x < c.Frame.max.x + 2f && k < 12; k++)
            {
                if (k >= NearPalette.Length) break;   // gauntlet: each painting once per level
                string slot = NearPalette[(start + k) % NearPalette.Length];
                if (SignatureSlots(c.LevelId).Contains(slot)) { x += 2f; continue; }   // the mid distance already shows it
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null) continue;
                Vector2 size = (Vector2)sprite.bounds.size * scale;
                var bottom = new Vector2(x + size.x * 0.5f, ground - 1.4f - SoloRoomSkin.Hash(id, 20 + k) * 1.2f);
                x += size.x * 0.85f + 2f + SoloRoomSkin.Hash(id, 40 + k) * 3f;
                if (!FindLegalBase(slot, def.Speed, bottom, size, tops, c.Travel, out float legal)) { c.Changes.Add($"left out near {slot} (A2)"); continue; }
                bottom.y = legal;
                var box = new Rect(bottom.x - size.x * 0.5f, bottom.y, size.x, size.y);
                if (doors.Any(d => d.Overlaps(box))) continue;
                layer ??= c.Layer("Near");
                SpriteRenderer r = c.Place(layer, $"{slot}_near_{k}", slot, bottom + new Vector2(0f, size.y * 0.5f), Vector2.one * scale, tint, def.Order, c.Middle, k % 2 == 1, -0.001f * k);
                if (slot.Contains("Tree")) r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.4f, 0.09f, 0f, 0f, SoloRoomSkin.Hash(id, 60 + k));
            }
        }

        /// <summary>§3.1: everything from the room's sides out to the frame, under its floor and over its ceiling, is dark
        /// masonry (unreachable, so free to paint), so no void shows at the frame's edge.</summary>
        static void Outside(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            Material fill = EnvironmentKit.TileMaterial("Fill");
            Sprite stone = SoloRoomSkin.FillSprite(room);
            if (fill == null || stone == null) return;
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            Bounds frame = SoloRoomBuilder.ComputeRoomBounds(room, 20f);
            float x0 = room.Origin.x, x1 = room.Origin.x + room.Width;
            // Round 4 (critics: "a pale gap between the ceiling's end and the wall", L012): where a ceiling runs into a side,
            // that side's column is stone all the way up (no wall top there to read).
            List<SoloRoomSkin.Solid> ceilings = SoloRoomSkin.Solids(room).Where(s => s.Kind == SoloRoomElementKind.Ceiling).ToList();
            bool ceilingAtLeft = ceilings.Any(s => s.Rect.xMin <= x0 + 0.05f), ceilingAtRight = ceilings.Any(s => s.Rect.xMax >= x1 - 0.05f);
            var t = new GameObject("Outside").transform;
            t.SetParent(c.EnvRoot, false);
            t.gameObject.layer = c.LayerIndex;
            // The side bands start at the room walls' outer faces (1 u out): the strip over a side wall's top stays air, so the
            // top reads against it like any other (A1).
            var rects = new[]
            {
                Rect.MinMaxRect(frame.min.x, frame.min.y, x0 - 1f, frame.max.y),
                Rect.MinMaxRect(x1 + 1f, frame.min.y, frame.max.x, frame.max.y),
                Rect.MinMaxRect(x0, frame.min.y, x1, content.min.y),
                Rect.MinMaxRect(x0, content.max.y, x1, frame.max.y),
                // Gauntlet: the room's side-wall columns (1 u, no renderer of their own) up to the walls' tops: stone, not sky.
                Rect.MinMaxRect(x0 - 1f, frame.min.y, x0, ceilingAtLeft ? frame.max.y : room.Origin.y + 8f),
                Rect.MinMaxRect(x1, frame.min.y, x1 + 1f, ceilingAtRight ? frame.max.y : room.Origin.y + 8f),
            };
            // Round 3 (critics: "a smeared band and a pale strip above the ceiling", L012/L019): over each ceiling, stone from
            // its top up to where the top band starts (the room's bounds sit above it, and the sky showed between).
            rects = rects.Concat(SoloRoomSkin.Solids(room).Where(s => s.Kind == SoloRoomElementKind.Ceiling && s.Rect.yMax < content.max.y - 0.01f)
                .Select(s => Rect.MinMaxRect(s.Rect.xMin, s.Rect.yMax - 0.02f, s.Rect.xMax, content.max.y + 0.02f))).ToArray();
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                if (r.width <= 0.01f || r.height <= 0.01f) continue;
                var go = new GameObject("Outside_" + i);
                go.transform.SetParent(t, false);
                go.layer = c.LayerIndex;
                go.transform.position = new Vector3(r.center.x, r.center.y, c.EnvRoot.position.z);
                var sr = go.AddComponent<SpriteRenderer>();
                // Gauntlet: the room's own stone, world-UV (so it continues the wall beside it without a seam), in the fixed
                // stone's batch, under the same shadow as a block's face (a separate quad, the face shade's solid).
                sr.sprite = stone; sr.sharedMaterial = fill; sr.color = SoloRoomSkin.Body;
                sr.drawMode = SpriteDrawMode.Sliced; sr.size = r.size;
                sr.sortingLayerName = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay); sr.sortingOrder = -3;
                Sprite shadeSprite = EnvironmentKit.Sprite("ENV_FaceShadeSolid");
                if (shadeSprite != null)
                {
                    var sh = new GameObject("Outside_Shade_" + i);
                    sh.transform.SetParent(t, false);
                    sh.layer = c.LayerIndex;
                    sh.transform.position = new Vector3(r.center.x, r.center.y, c.EnvRoot.position.z - 0.01f);
                    sh.transform.localScale = new Vector3(r.width / shadeSprite.bounds.size.x, r.height / shadeSprite.bounds.size.y, 1f);
                    var shr = sh.AddComponent<SpriteRenderer>();
                    shr.sprite = shadeSprite; shr.sharedMaterial = EnvironmentKit.TileMaterial("Sprite") ?? EnvironmentKit.UnlitMaterial;
                    // Round 5 (critics, every round: "a navy/purple translucent overlay on the walls"): the shade sprite is navy
                    // (26,31,56); this tint makes it a neutral dark over the outside stone.
                    shr.color = new Color(0.9f, 0.66f, 0.37f, Mathf.Min(1f, c.Config.FaceShadeAlpha * 1.25f));
                    shr.sortingLayerName = sr.sortingLayerName; shr.sortingOrder = -3;
                }
            }
            // Gauntlet: the masonry over the room ends in a broken course with roots hanging, not a ruler-straight edge.
            Sprite under = EnvironmentKit.Sprite("ENV_Under");
            EnvironmentKit.Slot ud = EnvironmentKit.Get("ENV_Under");
            // Round 4 (critics: "a smeared band and a pale strip above the ceiling", L012/L019): where a ceiling closes the
            // room's top, the masonry sits on it and shows no underside (its edge, bounce and roots drew over the ceiling's top).
            float roofed = SoloRoomSkin.Solids(room).Where(s => s.Kind == SoloRoomElementKind.Ceiling && Mathf.Abs(s.Rect.yMax - content.max.y) < 0.05f).Sum(s => s.Rect.width);
            if (under != null && ud != null && content.max.y < frame.max.y - 0.5f && roofed < 0.8f * (x1 - x0))
            {
                var go = new GameObject("Outside_Edge");
                go.transform.SetParent(t, false);
                go.layer = c.LayerIndex;
                go.transform.position = new Vector3((x0 + x1) * 0.5f, content.max.y - ud.below + ud.height * 0.5f, c.EnvRoot.position.z);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = under; sr.sharedMaterial = EnvironmentKit.UnlitMaterial; sr.color = c.Config.OutsideTint * 1.15f;
                sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(x1 - x0 + 2f, ud.height);
                sr.sortingLayerName = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay); sr.sortingOrder = -3; go.transform.position += new Vector3(0f, 0f, -0.02f);
                // Its underside catches the bounce of the sun (a warm soffit, not a flat black bar).
                Sprite fade = EnvironmentKit.Sprite("ENV_SkyFade");
                if (fade != null && c.Palette != null)
                {
                    var b = new GameObject("Outside_Bounce");
                    b.transform.SetParent(t, false);
                    b.layer = c.LayerIndex;
                    b.transform.position = new Vector3((x0 + x1) * 0.5f, content.max.y + 0.6f, c.EnvRoot.position.z);
                    b.transform.localScale = new Vector3((x1 - x0 + 2f) / fade.bounds.size.x, 1.4f / fade.bounds.size.y, 1f);
                    var br = b.AddComponent<SpriteRenderer>();
                    br.sprite = fade; br.sharedMaterial = EnvironmentKit.UnlitMaterial; br.flipY = true;
                    Color warm = c.Palette.SunLight; warm.a = 0.28f; br.color = warm;
                    br.sortingLayerName = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay); br.sortingOrder = -3; b.transform.position += new Vector3(0f, 0f, -0.03f);
                }
                // Roots and ivy hanging from it one by one: six shapes, random lengths, flips and gaps.
                for (float x = x0 + 0.3f * SoloRoomSkin.Hash(c.LevelId ?? "t", 500); x < x1; )
                {
                    int i = Mathf.RoundToInt(x * 10f);
                    float pick = SoloRoomSkin.Hash(c.LevelId ?? "t", 510 + i);
                    Sprite d = EnvironmentKit.Sprite("ENV_Drape_" + (int)(SoloRoomSkin.Hash(c.LevelId ?? "t", 520 + i) * 3f));
                    if (d == null) { x += 0.6f; continue; }
                    float h = 0.35f + 1.1f * SoloRoomSkin.Hash(c.LevelId ?? "t", 530 + i) * SoloRoomSkin.Hash(c.LevelId ?? "t", 535 + i);
                    float w = d.bounds.size.x * h / d.bounds.size.y;
                    if (pick > 0.2f)
                    {
                        var go2 = new GameObject("Outside_Root_" + i);
                        go2.transform.SetParent(t, false);
                        go2.layer = c.LayerIndex;
                        go2.transform.position = new Vector3(x + w * 0.5f, content.max.y - h * 0.5f + 0.1f, c.EnvRoot.position.z);
                        go2.transform.localScale = new Vector3((pick > 0.6f ? -1f : 1f) * h / d.bounds.size.y, h / d.bounds.size.y, 1f);
                        var r2 = go2.AddComponent<SpriteRenderer>();
                        r2.sprite = d; r2.sharedMaterial = EnvironmentKit.UnlitMaterial; r2.color = Color.Lerp(c.Config.OutsideTint, Color.white, 0.35f);
                        r2.sortingLayerName = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay); r2.sortingOrder = -3; go2.transform.position += new Vector3(0f, 0f, -0.04f);
                    }
                    x += w * (0.5f + 0.8f * SoloRoomSkin.Hash(c.LevelId ?? "t", 540 + i)) + (pick < 0.3f ? 0.5f : 0.05f);
                }
            }
        }

        /// <summary>§3.3: a thin slab hangs on two chains from the solid above it (or from beyond the frame's top). The
        /// same rule for real and disguised slabs (P10); behind the play layer, dark, with no lip.</summary>
        static void Chains(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            Sprite chain = EnvironmentKit.Sprite("ENV_Chain");
            if (chain == null) return;
            // Unlit, like the door's pocket drawn right after them (one shader run); the support tint stands in for the light.
            Material lit = EnvironmentKit.UnlitMaterial;
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            List<Rect> keepOut = SoloRoomSkin.KeepOut(room);
            Bounds frame = SoloRoomBuilder.ComputeRoomBounds(room, 2f);
            string gameplay = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay);
            Transform t = null;
            float width = chain.bounds.size.x * 1.3f;
            foreach (SoloRoomSkin.Solid s in solids.Where(s => s.Shape == SoloRoomSkin.Shape.Slab && s.Rect.width >= 1f))
            {
                foreach (float x in new[] { s.Rect.xMin + 0.3f, s.Rect.xMax - 0.3f })
                {
                    float from = s.Rect.yMax - 0.05f, to = frame.max.y;
                    foreach (SoloRoomSkin.Solid o in solids)
                        if (o.Name != s.Name && o.Rect.xMin < x && o.Rect.xMax > x && o.Rect.yMin >= s.Rect.yMax - 0.01f) to = Mathf.Min(to, o.Rect.yMin);
                    var r = Rect.MinMaxRect(x - width * 0.5f, from, x + width * 0.5f, to);
                    if (r.height < 0.4f || keepOut.Any(k => k.Overlaps(r))) continue;
                    if (t == null) { t = new GameObject("Chains").transform; t.SetParent(c.EnvRoot, false); t.gameObject.layer = c.LayerIndex; }
                    var go = new GameObject($"Chain_{s.Name}_{(x < s.Rect.center.x ? "L" : "R")}");
                    go.transform.SetParent(t, false);
                    go.layer = c.LayerIndex;
                    go.transform.position = new Vector3(x, r.center.y, c.EnvRoot.position.z);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = chain; sr.sharedMaterial = lit; sr.color = c.Config.SupportTint;
                    sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(width, r.height);
                    sr.sortingLayerName = gameplay; sr.sortingOrder = -5;   // with the fringes (one shader run)
                }
            }
        }

        /// <summary>§3.3: a floating floor's underside carries a fringe of arch tops (a bridge span), behind the play layer,
        /// in the support tint. The same rule for real and disguised floors (P10). Only where at least 2.5 u is clear
        /// below, and at most 0.9 u deep, so it never reads as a lower ceiling.</summary>
        static void Fringes(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            Sprite fringe = EnvironmentKit.Sprite("ENV_ArchFringe");
            if (fringe == null) return;
            // Unlit, like the door's pocket drawn right after them (one shader run); the support tint stands in for the light.
            Material lit = EnvironmentKit.UnlitMaterial;
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<Rect> keepOut = SoloRoomSkin.KeepOut(room);
            string gameplay = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay);
            bool flips = room.Elements.Any(e => e.Kind == SoloRoomElementKind.GravityFlip);
            List<Rect> thickKeepOut = SoloRoomSkin.VisualKeepOut(room);
            Transform t = null;
            float h = fringe.bounds.size.y;
            foreach (SoloRoomSkin.Edge e in SoloRoomSkin.ExposedEdges(solids).Where(e => e.Side == SoloRoomSkin.Side.Bottom && e.Length >= 2.5f
                && e.Owner.Shape is SoloRoomSkin.Shape.Slab or SoloRoomSkin.Shape.Block && e.Owner.Kind != SoloRoomElementKind.Ceiling && e.Line > content.min.y + 0.5f))
            {
                // Clear space under the stretch (the nearest solid top below it).
                float clear = float.MaxValue;
                foreach (SoloRoomSkin.Solid o in solids)
                    if (o.Name != e.Owner.Name && o.Rect.xMax > e.From && o.Rect.xMin < e.To && o.Rect.yMax <= e.Line + 0.01f) clear = Mathf.Min(clear, e.Line - o.Rect.yMax);
                if (clear < (flips ? 3.5f : 3f)) continue;
                if (SoloRoomSkin.ThickUnderFits(e, solids, flips, thickKeepOut)) continue;   // its thick underside hangs there
                var r = Rect.MinMaxRect(e.From + 0.15f, e.Line - h + 0.08f, e.To - 0.15f, e.Line + 0.08f);
                if (keepOut.Any(k => k.Overlaps(r))) continue;
                if (t == null) { t = new GameObject("Fringes").transform; t.SetParent(c.EnvRoot, false); t.gameObject.layer = c.LayerIndex; }
                var go = new GameObject($"Fringe_{e.Owner.Name}_{Mathf.RoundToInt(e.From * 100f)}");
                go.transform.SetParent(t, false);
                go.layer = c.LayerIndex;
                go.transform.position = new Vector3(r.center.x, r.center.y, c.EnvRoot.position.z);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = fringe; sr.sharedMaterial = lit; sr.color = c.Config.SupportTint;
                sr.drawMode = SpriteDrawMode.Tiled; sr.size = r.size;
                sr.sortingLayerName = gameplay; sr.sortingOrder = -5;
            }
        }

        static void PostVolume(TierContext c)
        {
            // Gauntlet: a level grades by its own palette (its profile under Data/LevelVolumes); the template keeps the shared one.
            VolumeProfile profile = c.LevelId != null && c.Palette != null
                ? EnsureVolumeProfile(c.Changes, $"Assets/_Game/Data/LevelVolumes/{c.LevelId}.asset", c.Palette)
                : AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null) return;
            var go = new GameObject("PostVolume");
            go.transform.SetParent(c.EnvRoot, false);
            go.layer = c.LayerIndex;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 1f; volume.sharedProfile = profile;
            UniversalAdditionalCameraData data = c.Camera.GetUniversalAdditionalCameraData();
            if (data == null) return;
            if (!data.renderPostProcessing) { data.renderPostProcessing = true; c.Changes.Add("post-processing on for " + c.Camera.name); }
            // The camera reads volumes on its own layers; the volume lives on the reality's layer.
            if ((data.volumeLayerMask & (1 << go.layer)) == 0) { data.volumeLayerMask |= 1 << go.layer; c.Changes.Add("volume mask for " + c.Camera.name); }
        }
    }
}
