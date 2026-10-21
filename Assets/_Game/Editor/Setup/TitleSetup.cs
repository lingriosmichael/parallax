using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Parallax.Core;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-108 (D-157): PARALLAX/Setup/Title Screen. Creates Assets/_Game/Scenes/Title.unity on its first run (the
    /// open scene must have no unsaved changes), then builds it, idempotently, in the open Title scene: Reality A's
    /// environment stack (the levels' own builder, with LookLevel's palette) under a slowly panning camera, a mossy ledge
    /// with the idle cat, and the intro/menu canvas (TitleSetup.UI.cs). Ends with BuildSceneList.Sync(), which puts Title
    /// first in Build Settings. The scene still has to be saved (Cmd+S).</summary>
    public static partial class TitleSetup
    {
        public const string SceneName = "Title";
        public const string ScenePath = "Assets/_Game/Scenes/" + SceneName + ".unity";
        /// <summary>The level whose palette and look the title borrows (L002's look was the first one approved, D-107).</summary>
        public const string LookLevel = "L002";
        const string LevelListConfigPath = "Assets/_Game/Data/LevelListConfig.asset";
        const string CameraConfigPath = "Assets/_Game/Data/LevelCameraConfig.asset";
        const string CatArtPath = "Assets/_Game/Art/Cats/CatA/";
        const string CatMaterialPath = "Assets/_Game/Art/Materials/Cat_OutlineUnlit.mat";
        const string LitSpritePath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        const string WorldName = "RealityRoot_A", CameraName = "Camera_Title", LedgeName = "Ledge", CatName = "Cat";
        // The world the stack is built for (the same frame _LevelTemplate's stack uses) and the camera's rest point in it.
        static readonly Bounds Frame = new(new Vector3(16f, 3.5f, 0f), new Vector3(35f, 16f, 0f));
        static readonly Vector2 CameraCentre = new(16f, 2.6f);
        // The ledge: from off screen on the left to its broken end, its top a little above the frame's horizon.
        const float LedgeLeft = -6f, LedgeRight = 12.6f, LedgeTop = 0.35f, LedgeBottom = -6f;
        const float CatFromEnd = 1.5f;
        const float SlabLift = 0.16f;

        public static bool RefusesToRun(string activeSceneName) => activeSceneName != SceneName;

        [MenuItem("PARALLAX/Setup/Title Screen")]
        public static void Configure()
        {
            Scene scene;
            if (AssetDatabase.LoadAssetAtPath<Object>(ScenePath) == null)
            {
                Scene active = EditorSceneManager.GetActiveScene();
                if (active.isDirty)
                {
                    Debug.LogError("TitleSetup: '" + active.name + "' has unsaved changes; save or discard them first.");
                    return;
                }
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    Debug.LogError("TitleSetup: failed to create " + ScenePath + ".");
                    return;
                }
                scene = EditorSceneManager.GetActiveScene();
            }
            else
            {
                scene = EditorSceneManager.GetActiveScene();
                if (RefusesToRun(scene.name))
                {
                    Debug.LogError($"TitleSetup: refuses to run outside '{SceneName}' (active scene is '{scene.name}'); open {ScenePath} first.");
                    return;
                }
            }

            var levelList = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (levelList == null) { Debug.LogError("TitleSetup: no LevelListConfig at '" + LevelListConfigPath + "'."); return; }
            if (!EnvironmentKit.Ready(out string missing)) { Debug.LogError("TitleSetup: the environment kit isn't imported (" + missing + ")."); return; }

            var changes = new List<string>();
            TitleScreenConfig config = EnsureConfig(changes);
            BuildWorld(config, changes);
            BuildCanvas(config, levelList, changes);
            AudioSetup.AddTo(scene, AudioSetup.EnsureBank(changes), changes);   // PAX-064
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("TitleSetup: " + string.Join("; ", changes) + ". Save the scene (Cmd+S).");
            BuildSceneList.Sync();
        }

        public static TitleScreenConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<TitleScreenConfig>(TitleScreenConfig.AssetPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<TitleScreenConfig>();
            AssetDatabase.CreateAsset(config, TitleScreenConfig.AssetPath);
            AssetDatabase.SaveAssets();
            changes.Add("created " + TitleScreenConfig.AssetPath);
            return config;
        }

        static void BuildWorld(TitleScreenConfig config, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A));
            GameObject world = SetupUtility.EnsureSceneRoot(WorldName, changes);
            if (world.layer != layer) { world.layer = layer; changes.Add("set " + WorldName + ".layer"); }
            RealityRoot root = SetupUtility.Ensure<RealityRoot>(world, changes);
            TrapKitSetup.Write(root, changes, ("id", (int)ObserverId.A));

            // The grade's light: Build colours the Global Light2D it finds under the root.
            Transform lightT = SetupUtility.EnsureChild(world.transform, "GlobalLight_A", layer, changes);
            Light2D light = SetupUtility.Ensure<Light2D>(lightT.gameObject, changes);
            if (light.lightType != Light2D.LightType.Global) light.lightType = Light2D.LightType.Global;
            light.targetSortingLayers = new[] { SortingBand.Background, SortingBand.Middle, SortingBand.Gameplay, SortingBand.Foreground }
                .Select(b => SortingLayer.NameToID(RealitySpace.SortingLayerName(ObserverId.A, b))).ToArray();

            GameObject cameraGO = SetupUtility.EnsureSceneRoot(CameraName, changes);
            if (!cameraGO.CompareTag("MainCamera")) cameraGO.tag = "MainCamera";
            Camera camera = SetupUtility.Ensure<Camera>(cameraGO, changes);
            SetupUtility.Ensure<AudioListener>(cameraGO, changes);
            SetupUtility.Ensure<UniversalAdditionalCameraData>(cameraGO, changes);
            float view = LevelCameras.ViewHeight(LookLevel, AssetDatabase.LoadAssetAtPath<LevelCameraConfig>(CameraConfigPath));
            camera.orthographic = true;
            camera.orthographicSize = view * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(CameraCentre.x, CameraCentre.y, -10f);
            TitleCameraDrift drift = SetupUtility.Ensure<TitleCameraDrift>(cameraGO, changes);
            TrapKitSetup.Write(drift, changes, ("config", config));
            SetupUtility.SetVector2(drift, "centre", CameraCentre, changes);

            // The levels' own stack builder, as Rebuild All runs it. Its room is only the ledge, so the composer sets the
            // horizon, the far ruins and the haze from the ledge's top, as it does from a level's floors.
            EnvironmentStackSetup.Build(root, camera, Frame, LookLevel, Room(), changes);
            BuildLedge(world.transform, layer, changes);
            BuildCat(world.transform, layer, config, changes);
        }

        /// <summary>The title's "room" for the stack builder: the ledge, and a ceiling far above the view whose only job is
        /// to make the room's bounds hold the whole sky (wide enough for the pan) (the builder walls in everything outside them). It is never built as
        /// a room (no colliders, no checkpoint, no art of its own); the builder reads its bounds and the ledge's top.</summary>
        static SoloRoomDefinition Room() => new(0, 0f, 32f,
            new[]
            {
                new SoloRoomElement(SoloRoomElementKind.Floor, LedgeName, new Vector2((LedgeLeft + LedgeRight) * 0.5f, (LedgeTop + LedgeBottom) * 0.5f),
                    new Vector2(LedgeRight - LedgeLeft, LedgeTop - LedgeBottom)),
                new SoloRoomElement(SoloRoomElementKind.Ceiling, "Sky_Bound", new Vector2(16f, 14f), new Vector2(46f, 1f)),
            },
            new SoloRoomOpening[0], new RequiredJump[0]);

        static void BuildLedge(Transform world, int layer, List<string> changes)
        {
            Transform old = world.Find(LedgeName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform ledge = SetupUtility.EnsureChild(world, LedgeName, layer, changes);
            string sorting = RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay);
            float width = LedgeRight - LedgeLeft, height = LedgeTop - LedgeBottom;

            void Tiled(string name, string slot, string kind, Vector2 centre, Vector2 size, int order)
            {
                Sprite sprite = EnvironmentKit.Sprite(slot);
                Material material = EnvironmentKit.TileMaterial(kind);
                if (sprite == null || material == null) { changes.Add("ledge: no " + slot + " or ENV_" + kind + " material, skipped"); return; }
                Transform t = SetupUtility.EnsureChild(ledge, name, layer, changes);
                t.position = new Vector3(centre.x, centre.y, 0f);
                var r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                r.sprite = sprite; r.sharedMaterial = material; r.drawMode = SpriteDrawMode.Sliced; r.size = size;
                t.localScale = Vector3.one;
                r.sortingLayerName = sorting; r.sortingOrder = order;
            }

            void Plain(string name, string slot, Vector2 centre, int order, bool flip = false, float scale = 1f)
            {
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null) return;
                Transform t = SetupUtility.EnsureChild(ledge, name, layer, changes);
                t.position = new Vector3(centre.x, centre.y, 0f);
                var r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                r.sprite = sprite; r.flipX = flip;
                t.localScale = new Vector3(scale, scale, 1f);
                if (EnvironmentKit.TileMaterial("Sprite") is Material plain) r.sharedMaterial = plain;
                r.sortingLayerName = sorting; r.sortingOrder = order;
            }

            Tiled("Fill", "ENV_Fill_A", "Fill", new Vector2(LedgeLeft + width * 0.5f, LedgeBottom + height * 0.5f), new Vector2(width, height), 0);
            Sprite side = EnvironmentKit.Sprite("ENV_Side");
            if (side != null)
                Tiled("Side", "ENV_Side", "Side", new Vector2(LedgeRight - side.bounds.size.x * 0.5f, LedgeBottom + height * 0.5f), new Vector2(side.bounds.size.x, height), 1);
            Sprite slab = EnvironmentKit.Sprite("ENV_Slab");
            float slabH = slab != null ? slab.bounds.size.y : 0.3f;
            Sprite end = EnvironmentKit.Sprite("ENV_SlabEnd");
            float endW = end != null ? end.bounds.size.x : 0f;
            // The slab's painted top sits SlabLift below its sprite's edge: raised by that much, its stone is the walk line.
            Tiled("Slab", "ENV_Slab", "Slab", new Vector2(LedgeLeft + (width - endW) * 0.5f, LedgeTop + SlabLift - slabH * 0.5f), new Vector2(width - endW, slabH), 2);
            if (end != null) Plain("SlabEnd", "ENV_SlabEnd", new Vector2(LedgeRight - endW * 0.5f, LedgeTop - end.bounds.size.y * 0.5f), 3);
            Sprite lip = EnvironmentKit.Sprite("ENV_LipVines");
            if (lip != null)
                Tiled("LipVines", "ENV_LipVines", "LipVines", new Vector2(LedgeLeft + (width - endW) * 0.5f, LedgeTop - slabH - lip.bounds.size.y * 0.5f + 0.05f),
                    new Vector2(width - endW, lip.bounds.size.y), 4);
            Plain("Moss", "ENV_Moss_1", new Vector2(LedgeRight - 3.6f, LedgeTop + 0.05f), 5);
            // The ledge's end: a broken cap over the corner and ivy down the cut face, so it reads as ruin, not a cut.
            if (EnvironmentKit.Sprite("ENV_CapEnd_0") is Sprite cap)
                Plain("CapEnd", "ENV_CapEnd_0", new Vector2(LedgeRight - cap.bounds.size.x * 0.35f, LedgeTop - cap.bounds.size.y * 0.5f), 6, flip: true);
            if (EnvironmentKit.Sprite("ENV_Drape_3") is Sprite drape)
                Plain("Drape_0", "ENV_Drape_3", new Vector2(LedgeRight - drape.bounds.size.x * 0.4f, LedgeTop - 0.2f - drape.bounds.size.y * 0.8f), 7, scale: 1.6f);
            if (EnvironmentKit.Sprite("ENV_Drape_0") is Sprite drape2)
                Plain("Drape_1", "ENV_Drape_0", new Vector2(LedgeRight - 0.15f, LedgeTop - 1.2f - drape2.bounds.size.y * 0.7f), 7, flip: true, scale: 1.4f);
        }

        static void BuildCat(Transform world, int layer, TitleScreenConfig config, List<string> changes)
        {
            Transform t = SetupUtility.EnsureChild(world, CatName, layer, changes);
            t.position = new Vector3(LedgeRight - CatFromEnd, LedgeTop, 0f);
            t.localScale = Vector3.one;
            string sorting = RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay);
            // As the player cat: the body on the lit sprite material, a soft rim behind it on the outline material.
            var r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
            r.sortingLayerName = sorting;
            r.sortingOrder = 20;
            if (AssetDatabase.LoadAssetAtPath<Material>(LitSpritePath) is Material lit) r.sharedMaterial = lit;
            Transform outlineT = SetupUtility.EnsureChild(t, "Outline", layer, changes);
            outlineT.localPosition = Vector3.zero;
            var outline = SetupUtility.Ensure<SpriteRenderer>(outlineT.gameObject, changes);
            outline.sortingLayerName = sorting;
            outline.sortingOrder = 19;
            var material = AssetDatabase.LoadAssetAtPath<Material>(CatMaterialPath);
            if (material != null) outline.sharedMaterial = material;
            Sprite[] idle = Frames("CatA_Idle"), look = Frames("CatA_IdleLook");
            if (idle.Length == 0) { changes.Add("cat: no CatA_Idle frames (run PARALLAX/Setup/Cat Visual)"); return; }
            r.sprite = idle[0];
            outline.sprite = idle[0];
            var cat = SetupUtility.Ensure<TitleCatIdle>(t.gameObject, changes);
            TrapKitSetup.Write(cat, changes, ("config", config), ("outline", outline));
            SetupUtility.SetArray(cat, "idle", idle, changes);
            SetupUtility.SetArray(cat, "fidget", look, changes);
        }

        /// <summary>A cat sheet's frames in order (CatA_Idle_0, CatA_Idle_1, …).</summary>
        static Sprite[] Frames(string sheet) =>
            AssetDatabase.LoadAllAssetsAtPath(CatArtPath + sheet + ".png").OfType<Sprite>()
                .OrderBy(s => int.TryParse(Regex.Match(s.name, @"(\d+)$").Value, out int i) ? i : 0).ToArray();
    }
}
