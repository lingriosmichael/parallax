using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A15 §2.3: the background stack. PARALLAX/Setup/Levels/Environment Stack (on _LevelTemplate) creates
    /// LevelLookConfig with its defaults, attaches the kit's normal maps, removes the A02 strips and the stray FG_01 from
    /// Reality A, and builds the default stack; Rebuild All Levels then calls Build for each level with its recipe
    /// (LevelLooks): sky gradient, sun, three cloud bands, far pieces (scaled up to 1.5×), haze, mid pieces, back walls,
    /// fog and light shafts, foreground over solid ground, and the grade's global light. Everything lives under
    /// RealityRoot_A/Environment, outside the room, and moves with ParallaxLayer. Reality B is never touched.</summary>
    public static partial class EnvironmentStackSetup
    {
        public const string RootName = "Environment";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string CameraConfigPath = "Assets/_Game/Data/LevelCameraConfig.asset";
        public static readonly string[] OldLayerPaths = { "Background/BG_00_Sky", "Background/BG_01_Far", "Background/MG_01_Mid", "Foreground/FG_01" };
        // The template's camera frame is never baked (D-071): its stack is laid out for a typical one-room level.
        static readonly Bounds TemplateFrame = new(new Vector3(16f, 3.5f, 0f), new Vector3(35f, 16f, 0f));
        /// <summary>The widest and narrowest phone screens the coverage is built for (D-083: 4:3, 16:9, 20:9).</summary>
        public static readonly float[] Aspects = { 4f / 3f, 16f / 9f, 20f / 9f };
        const int BandTiles = 7;
        const float MinFlatClearance = 0.3f;

        [MenuItem("PARALLAX/Setup/Levels/Environment Stack")]
        public static void Run()
        {
            var changes = new List<string>();
            LevelLookConfig config = EnsureConfig(changes);
            ApplyCameraZoom(changes);
            AttachNormalMaps(changes);
            EnvironmentKit.EnsureMaterials(changes);
            EnvironmentKit.EnsureAtlas(changes);
            EnsureTierAssets(config, changes);
            if (!EnvironmentKit.Ready(out string missing)) { Debug.LogError("Environment Stack: the kit isn't imported (" + missing + "); run Tools/Art/env_kit.py and let Unity import it."); return; }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TemplatePath)
            {
                Debug.Log("Environment Stack: config and normal maps done; open _LevelTemplate and run this again to build its stack. " + string.Join("; ", changes));
                return;
            }
            RealityRoot root = scene.GetRootGameObjects().Select(g => g.GetComponent<RealityRoot>()).FirstOrDefault(r => r != null && r.Id == ObserverId.A);
            Camera camera = Object.FindObjectsByType<LevelCameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(f => f.GetComponent<Camera>()).FirstOrDefault();
            if (root == null || camera == null) { Debug.LogError("Environment Stack: _LevelTemplate needs RealityRoot_A and Camera_A's LevelCameraFollow."); return; }
            Build(root, camera, TemplateFrame, null, null, changes);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Environment Stack: " + string.Join("; ", changes) + ". Save the template (Cmd+S), then run Rebuild All Levels.");
        }

        // PAX-A16 §3.0 (D-100, D-102): the fixed 1.8x zoom. Every level shows 16 / 1.8 u and follows the cat inside the room;
        // the tighter look-ahead and horizontal dead zone keep D-083's reveals on screen at that view.
        public const float Zoom = 1.8f, ZoomedViewHeight = 16f / Zoom, ZoomedLookAhead = 1.5f;
        public static readonly Vector2 ZoomedDeadZone = new(1f, 1.6f);

        /// <summary>Writes D-100's camera constants into LevelCameraConfig (idempotent).</summary>
        public static void ApplyCameraZoom(List<string> changes)
        {
            LevelCameraConfig camera = LevelCameraBuilder.EnsureConfig(CameraConfigPath, changes);
            var so = new SerializedObject(camera);
            bool changed = false;
            void Set(string name, float value) { SerializedProperty p = so.FindProperty(name); if (!Mathf.Approximately(p.floatValue, value)) { p.floatValue = value; changed = true; } }
            Set("maxViewHeight", ZoomedViewHeight);
            Set("lookAhead", ZoomedLookAhead);
            SerializedProperty dz = so.FindProperty("deadZoneHalfExtents");
            if (dz.vector2Value != ZoomedDeadZone) { dz.vector2Value = ZoomedDeadZone; changed = true; }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(camera);
            AssetDatabase.SaveAssets();
            changes.Add($"LevelCameraConfig: view {ZoomedViewHeight:0.##}, look-ahead {ZoomedLookAhead}, dead zone {ZoomedDeadZone} (D-102)");
        }

        public static LevelLookConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
            if (config != null && config.Version >= LevelLookConfig.CurrentVersion) return config;
            if (config != null)
            {
                // PAX-A16: an older asset takes the new defaults (its layers and tiers changed).
                config.ResetToDefaults();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                changes.Add("reset " + LevelLookConfig.AssetPath + " to version " + LevelLookConfig.CurrentVersion);
                return config;
            }
            config = ScriptableObject.CreateInstance<LevelLookConfig>();
            config.ResetToDefaults();
            AssetDatabase.CreateAsset(config, LevelLookConfig.AssetPath);
            AssetDatabase.SaveAssets();
            changes.Add("created " + LevelLookConfig.AssetPath);
            return config;
        }

        /// <summary>Each kit sprite with a normal map names it as its _NormalMap secondary texture (as the trap kit does).</summary>
        public static void AttachNormalMaps(List<string> changes)
        {
            foreach (EnvironmentKit.Slot slot in EnvironmentKit.All.Where(s => s.normal))
            {
                var importer = AssetImporter.GetAtPath(EnvironmentKit.PathOf(slot)) as TextureImporter;
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentKit.NormalPathOf(slot));
                if (importer == null || normal == null) continue;
                // A normal map imported before the importer gave it its sprite's PPU is re-imported once.
                if (AssetImporter.GetAtPath(EnvironmentKit.NormalPathOf(slot)) is TextureImporter normalImporter
                    && !Mathf.Approximately(normalImporter.spritePixelsPerUnit, importer.spritePixelsPerUnit))
                { normalImporter.SaveAndReimport(); changes.Add("re-imported " + slot.name + "_n"); }
                SecondarySpriteTexture[] current = importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0];
                if (current.Length == 1 && current[0].name == "_NormalMap" && current[0].texture == normal) continue;
                importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = normal } };
                importer.SaveAndReimport();
                changes.Add("attached " + slot.name + " normal map");
            }
        }

        // ---------- the per-level stack ----------

        /// <summary>The camera's travel for a frame: the view height (LevelCameraFollow's rule: at most MaxViewHeight, never
        /// more than the frame) and the range of camera centres over every supported aspect.</summary>
        public readonly struct Travel
        {
            public readonly Vector2 Mid, Min, Max; public readonly float ViewHeight, MaxViewWidth;
            public Travel(Bounds frame, float maxViewHeight)
            {
                Mid = frame.center;
                Vector2 lo = Mid, hi = Mid;
                float tallest = 0f, widest = 0f;
                foreach (float aspect in Aspects)
                {
                    float view = Mathf.Min(maxViewHeight, frame.size.y, frame.size.x / aspect);
                    Vector2 half = new(view * aspect * 0.5f, view * 0.5f);
                    Vector2 min = (Vector2)frame.min + half, max = (Vector2)frame.max - half;
                    lo = Vector2.Min(lo, new Vector2(min.x > max.x ? Mid.x : min.x, min.y > max.y ? Mid.y : min.y));
                    hi = Vector2.Max(hi, new Vector2(min.x > max.x ? Mid.x : max.x, min.y > max.y ? Mid.y : max.y));
                    tallest = Mathf.Max(tallest, view); widest = Mathf.Max(widest, view * aspect);
                }
                Min = lo; Max = hi; ViewHeight = tallest; MaxViewWidth = widest;
            }
            /// <summary>Where a layer moving at `speed` puts a point placed at `atMid` (its place at mid travel) when the camera
            /// is at `camera`.</summary>
            public Vector2 Shifted(Vector2 atMid, float speed, Vector2 camera) => atMid + (1f - speed) * (camera - Mid);
        }

        public static float MaxViewHeight()
        {
            var cameraConfig = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(CameraConfigPath);
            return cameraConfig != null ? cameraConfig.MaxViewHeight : 16f;
        }

        public static void Build(RealityRoot root, Camera camera, Bounds worldFrame, string levelId, SoloRoomDefinition? room, List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
            if (config == null || !EnvironmentKit.Ready(out _)) return;
            foreach (string path in OldLayerPaths)
            {
                Transform old = root.transform.Find(path);
                if (old != null) { Object.DestroyImmediate(old.gameObject); changes.Add("removed A02 " + path); }
            }
            Transform existing = root.transform.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            LevelLooks.Look look = LevelLooks.For(levelId);
            LevelLookConfig.Grade grade = config.GetGrade(look.Grade) ?? config.GetGrade(LevelLooks.DefaultGrade) ?? config.Grades[0];
            // Gauntlet: the level's palette sets its sky, haze, sun and ambient light (LevelPalettes).
            LevelPalettes.Palette palette = LevelPalettes.For(levelId);
            grade = new LevelLookConfig.Grade
            {
                Name = palette.Name, SkyTop = palette.SkyTop, SkyHorizon = palette.Horizon, Sun = palette.SunAt, SunSize = palette.SunSize,
                Haze = palette.Haze, Light = palette.Ambient, LightIntensity = palette.AmbientIntensity,
            };
            // D-104: the level's own view height (its zoom), as its camera uses.
            var travel = new Travel(worldFrame, Parallax.Editor.Levels.LevelCameras.ViewHeight(levelId, AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(CameraConfigPath)));
            Vector2 mid = travel.Mid;
            float view = travel.ViewHeight, width = travel.MaxViewWidth;
            Material unlit = EnvironmentKit.UnlitMaterial;
            int layerIndex = root.gameObject.layer;
            var envRoot = new GameObject(RootName).transform;
            envRoot.SetParent(root.transform, false);
            envRoot.gameObject.layer = layerIndex;

            var layers = new Dictionary<string, Transform>();
            Transform Layer(string name)
            {
                if (layers.TryGetValue(name, out Transform t)) return t;
                LevelLookConfig.Layer def = config.GetLayer(name);
                t = new GameObject(name).transform;
                t.SetParent(envRoot, false);
                t.gameObject.layer = layerIndex;
                t.localPosition = ParallaxMath.LayerLocalOffset(mid, Vector2.zero, def.Speed);
                var parallax = t.gameObject.AddComponent<ParallaxLayer>();
                TrapKitSetup.Write(parallax, changes, ("realityCamera", camera), ("screenSpeed", def.Speed), ("tileWidth", 0f));
                layers[name] = t;
                return t;
            }

            Color Tint(string layerName, float alpha = 1f)
            {
                LevelLookConfig.Layer def = config.GetLayer(layerName);
                Color c = Color.Lerp(Color.white, grade.Haze, def.HazeMix) * def.Value;
                c.a = def.Alpha * alpha;
                return c;
            }

            SpriteRenderer Place(Transform layer, string name, string slot, Vector2 worldAtMid, Vector2 scale, Color color, int order, string band, bool flip = false, float z = 0f)
            {
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null) return null;
                var t = new GameObject(name).transform;
                t.SetParent(layer, false);
                t.gameObject.layer = layerIndex;
                Vector2 local = worldAtMid - (Vector2)layer.localPosition;
                t.localPosition = new Vector3(local.x, local.y, z);
                t.localScale = new Vector3(scale.x, scale.y, 1f);
                var r = t.gameObject.AddComponent<SpriteRenderer>();
                r.sprite = sprite; r.sharedMaterial = unlit; r.color = color; r.flipX = flip;
                r.sortingLayerName = band; r.sortingOrder = order;
                return r;
            }

            string Back = RealitySpace.SortingLayerName(root.Id, SortingBand.Background), Middle = RealitySpace.SortingLayerName(root.Id, SortingBand.Middle), Front = RealitySpace.SortingLayerName(root.Id, SortingBand.Foreground);
            int Order(string layerName) => config.GetLayer(layerName).Order;

            // 01 Sky: the horizon colour, the upper colour fading down over it. Stays on the camera, sized past the widest view.
            Transform sky = Layer("Sky");
            Vector2 skySize = new(width + 20f, view + 12f);
            Sprite white = EnvironmentKit.Sprite("ENV_White"), fade = EnvironmentKit.Sprite("ENV_SkyFade");
            Place(sky, "Horizon", "ENV_White", mid, skySize / (Vector2)white.bounds.size, grade.SkyHorizon, Order("Sky"), Back);
            Place(sky, "Upper", "ENV_SkyFade", mid + new Vector2(0f, view * 0.5f + 6f - view * 0.45f), new Vector2(skySize.x / fade.bounds.size.x, view * 0.9f / fade.bounds.size.y), grade.SkyTop, Order("Sky") + 1, Back);
            // PAX-A16 tier 0: the painted sky over the gradient, then the grade's upper colour deepening its top.
            // Gauntlet: the plate covers the widest view only (the sky layer stays on the camera), so it's stretched as little as can be.
            SkyPlate(sky, mid, new Vector2(width * 1.06f, view * 1.1f), grade, Order("Sky") + 2, Back, Place, "ENV_Sky_" + palette.Sky);

            // The sun (its painted glow included).
            Vector3 sunSpec = new(grade.Sun.x, grade.Sun.y, grade.SunSize);
            Sprite sunSprite = EnvironmentKit.Sprite("ENV_Sun");
            var sunAt = new Vector2(mid.x + (sunSpec.x - 0.5f) * width * 0.8f, mid.y + (sunSpec.y - 0.5f) * view);
            Place(Layer("Sun"), "Sun", "ENV_Sun", sunAt, Vector2.one * (sunSpec.z / sunSprite.bounds.size.x), palette.Sun, Order("Sun"), Back);

            float horizon = HorizonY(room, worldFrame);
            // Gauntlet: no tiled bands. The composer (Compose, from BuildTiers) places every background piece whole; an
            // interior level keeps its back wall from its recipe (rebuilt whole in Phase 2).
            List<SoloRoomSkin.Edge> tops = room.HasValue ? SoloRoomSkin.WalkableTops(room.Value) : new List<SoloRoomSkin.Edge>();
            int pieceIndex = 0;
            foreach (LevelLooks.Piece piece in look.Pieces.Where(q => q.Slot == "ENV_BackWall"))
            {
                Sprite sprite = EnvironmentKit.Sprite(piece.Slot);
                if (sprite == null) continue;
                LevelLookConfig.Layer def = config.GetLayer("BackWall");
                Vector2 bottom = piece.Relative ? new Vector2(worldFrame.min.x + piece.X * worldFrame.size.x, worldFrame.min.y + piece.BaseY * worldFrame.size.y) : new Vector2(piece.X, piece.BaseY);
                Vector2 size = new(piece.Span > 0f ? piece.Span : sprite.bounds.size.x, sprite.bounds.size.y);
                SpriteRenderer r = Place(Layer("BackWall"), $"{piece.Slot}_{pieceIndex}", piece.Slot, bottom + new Vector2(0f, size.y * 0.5f), Vector2.one, Tint("BackWall", piece.Alpha), def.Order, Middle, piece.Flip, -0.001f * pieceIndex);
                pieceIndex++;
                if (piece.Span > 0f) { r.drawMode = SpriteDrawMode.Tiled; r.transform.localScale = Vector3.one; r.size = size; }
            }

            // 07 Foreground: ferns and roots at the frame's lower corners, over solid ground only (never the room's air).
            AddForeground("ENV_FG_Ferns", worldFrame.min.x, false);
            AddForeground("ENV_FG_Roots", worldFrame.max.x, true);
            void AddForeground(string slot, float edgeX, bool right)
            {
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null || !room.HasValue) return;
                Vector2 size = sprite.bounds.size;
                var centre = new Vector2(edgeX + (right ? -size.x * 0.5f + 0.4f : size.x * 0.5f - 0.4f), worldFrame.min.y + size.y * 0.5f - 0.2f);
                if (!ForegroundClear(new Rect(centre - size * 0.5f, size), config.GetLayer("Foreground").Speed, room.Value, travel)) { changes.Add($"left out {slot} (it would cover the room's air)"); return; }
                Place(Layer("Foreground"), slot, slot, centre, Vector2.one, Tint("Foreground"), Order("Foreground"), Front, right);
            }

            // PAX-A16: light, atmosphere, the frame, the outside and the supports (EnvironmentStackSetup.Tiers.cs).
            BuildTiers(new TierContext
            {
                Palette = palette, LevelId = levelId,
                Root = root, Camera = camera, Frame = worldFrame, Room = room, Config = config, Grade = grade, Travel = travel, EnvRoot = envRoot,
                Layer = Layer, Place = Place, SunAt = sunAt, SunSize = sunSpec.z, Horizon = horizon, LayerIndex = layerIndex,
                Back = Back, Middle = Middle, Front = Front, Changes = changes,
            });

            // The grade's light.
            Light2D global = root.GetComponentsInChildren<Light2D>(true).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            if (global != null && (global.color != grade.Light || !Mathf.Approximately(global.intensity, grade.LightIntensity)))
            {
                global.color = grade.Light; global.intensity = grade.LightIntensity;
                changes.Add("graded " + global.name + " (" + grade.Name + ")");
            }
            changes.Add($"built {RootName} ({look.Grade}, {look.Pieces.Length} pieces)");
        }

        /// <summary>The horizon: just above the lowest walkable top, so the haze sits behind the ground line.</summary>
        static float HorizonY(SoloRoomDefinition? room, Bounds frame)
        {
            if (!room.HasValue) return frame.min.y + 5f;
            List<SoloRoomSkin.Edge> tops = SoloRoomSkin.WalkableTops(room.Value);
            return tops.Count > 0 ? tops.Min(t => t.Line) + 0.5f : frame.min.y + 5f;
        }

        /// <summary>A2 for one piece: false when no height (from the wanted one down, then up, in 0.25 u steps, up to 6 u)
        /// keeps every flat line of the piece at least 0.3 u from every walkable top it overlaps, over the camera's travel.</summary>
        public static bool FindLegalBase(string slot, float speed, Vector2 bottom, Vector2 size, List<SoloRoomSkin.Edge> tops, Travel travel, out float legal)
        {
            legal = bottom.y;
            float[] flats = LevelLooks.FlatTops(slot);
            if (flats.Length == 0) return true;
            for (int step = 0; step <= 48; step++)
            {
                float offset = (step % 2 == 0 ? -1f : 1f) * 0.25f * ((step + 1) / 2);
                float y = bottom.y + offset;
                if (FlatClear(flats, speed, new Vector2(bottom.x, y), size, tops, travel)) { legal = y; return true; }
            }
            return false;
        }

        public static bool FlatClear(float[] flats, float speed, Vector2 bottom, Vector2 size, List<SoloRoomSkin.Edge> tops, Travel travel)
        {
            Vector2[] cameras = { travel.Min, travel.Max, new(travel.Min.x, travel.Max.y), new(travel.Max.x, travel.Min.y), travel.Mid };
            foreach (Vector2 cam in cameras)
            {
                Vector2 at = travel.Shifted(bottom, speed, cam);
                foreach (float f in flats)
                {
                    float line = at.y + size.y * (1f - f);
                    foreach (SoloRoomSkin.Edge top in tops)
                        if (top.To > at.x - size.x * 0.5f && top.From < at.x + size.x * 0.5f && Mathf.Abs(line - top.Line) < MinFlatClearance) return false;
                }
            }
            return true;
        }

        /// <summary>The foreground rule (§3): over the camera's travel, a foreground piece's box lies only over solids or
        /// outside the room's content (the air a cat can reach is inside the content and not in a solid).</summary>
        public static bool ForegroundClear(Rect atMid, float speed, SoloRoomDefinition room, Travel travel)
        {
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<Rect> solids = SoloRoomSkin.Solids(room).Where(s => !s.Trap).Select(s => s.Rect).ToList();
            Vector2[] cameras = { travel.Min, travel.Max, new(travel.Min.x, travel.Max.y), new(travel.Max.x, travel.Min.y), travel.Mid };
            foreach (Vector2 cam in cameras)
            {
                Vector2 offset = travel.Shifted(atMid.center, speed, cam) - atMid.center;
                var r = new Rect(atMid.position + offset, atMid.size);
                for (float x = r.xMin; x <= r.xMax + 1e-3f; x += 0.25f)
                    for (float y = r.yMin; y <= r.yMax + 1e-3f; y += 0.25f)
                    {
                        var p = new Vector2(Mathf.Min(x, r.xMax), Mathf.Min(y, r.yMax));
                        bool inside = p.x > content.min.x && p.x < content.max.x && p.y > content.min.y && p.y < content.max.y;
                        if (inside && !solids.Any(s => s.Contains(p))) return false;
                    }
            }
            return true;
        }
    }
}
