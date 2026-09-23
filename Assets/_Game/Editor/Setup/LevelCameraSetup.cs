using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    // PAX-052 (D-071). Configures the level camera on Camera_A. Scaffolding menu: refuses
    // everywhere except _LevelTemplate (§2.1.5/§2.2.6) - LevelSetup's regeneration step is what
    // wires each level's copy to that scene's own RoomManager/CatRespawn.
    public static class LevelCameraSetup
    {
        public const string RequiredSceneName = "_LevelTemplate";
        // The Cat_Player prefab instance for reality A is renamed "Cat_A" by RealitySetup/the
        // level pipeline (never "Cat_Player" - that name only exists on the source prefab asset).
        const string CatAName = "Cat_A";
        const string ConfigPath = "Assets/_Game/Data/LevelCameraConfig.asset";

        public static bool RefusesToRun(string activeSceneName) => activeSceneName != RequiredSceneName;

        [MenuItem("PARALLAX/Setup/Levels/Level Camera")]
        public static void Configure()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (RefusesToRun(sceneName))
            {
                Debug.LogError($"LevelCameraSetup: refuses to run outside '{RequiredSceneName}' (active scene is '{sceneName}').");
                return;
            }

            GameObject cameraGO = GameObject.Find("Camera_A");
            if (cameraGO == null) { Debug.LogError("LevelCameraSetup: 'Camera_A' not found. Stopping."); return; }
            var cam = cameraGO.GetComponent<Camera>();
            if (cam == null || !cam.orthographic) { Debug.LogError("LevelCameraSetup: 'Camera_A' has no orthographic Camera component. Stopping."); return; }

            GameObject catGO = GameObject.Find(CatAName);
            if (catGO == null) { Debug.LogError($"LevelCameraSetup: '{CatAName}' not found. Stopping."); return; }

            GameObject rootGO = GameObject.Find("RealityRoot_A");
            RealityRoot root = rootGO == null ? null : rootGO.GetComponent<RealityRoot>();
            if (root == null) { Debug.LogError("LevelCameraSetup: 'RealityRoot_A' not found. Stopping."); return; }

            var changes = new List<string>();

            var oldFollow = cameraGO.GetComponent<CatCameraFollow>();
            if (oldFollow != null)
            {
                Object.DestroyImmediate(oldFollow);
                changes.Add("removed CatCameraFollow from Camera_A");
            }

            var follow = cameraGO.GetComponent<LevelCameraFollow>();
            if (follow == null)
            {
                follow = cameraGO.AddComponent<LevelCameraFollow>();
                changes.Add("added LevelCameraFollow");
            }

            LevelCameraConfig config = LevelCameraBuilder.EnsureConfig(ConfigPath, changes);
            SoloRoomBuilder.Wire(follow, "config", config, changes);
            SoloRoomBuilder.Wire(follow, "target", catGO.transform, changes);

            var respawn = catGO.GetComponent<CatRespawn>();
            if (respawn != null) SoloRoomBuilder.Wire(follow, "respawn", respawn, changes);
            else Debug.LogError("LevelCameraSetup: '" + CatAName + "' has no CatRespawn; LevelCameraFollow.respawn was not wired.");

            RoomDeath death = Object.FindAnyObjectByType<RoomDeath>();
            if (death != null) SoloRoomBuilder.Wire(follow, "roomDeath", death, changes);
            else Debug.LogError("LevelCameraSetup: no RoomDeath found in the active scene; LevelCameraFollow.roomDeath was not wired.");

            ConfigureBackgroundCoverage(root, config, changes);

            if (changes.Count == 0)
            {
                Debug.Log("LevelCameraSetup: Level Camera already configured.");
            }
            else
            {
                Debug.Log("LevelCameraSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(cameraGO.scene);
            }
        }

        // §2.3: sizes and offsets the three tiled background layers once, in the template, for
        // the tallest view the level camera ever uses (MaxViewHeight). No new art - the same
        // sprite, tiled taller (DrawMode.Tiled already supports this). maxAbsCameraY is 0 because
        // none of today's rooms trigger vertical follow (§2.2.2: only when the room is taller
        // than the view); revisit this if a future room does.
        static void ConfigureBackgroundCoverage(RealityRoot root, LevelCameraConfig config, List<string> changes)
        {
            float orthoSize = config.MaxViewHeight * 0.5f;
            ConfigureLayer(root, "Background/BG_00_Sky", orthoSize, changes);
            ConfigureLayer(root, "Background/BG_01_Far", orthoSize, changes);
            ConfigureLayer(root, "Background/MG_01_Mid", orthoSize, changes);
        }

        const float BackgroundMaxAbsCameraY = 0f;
        const float BackgroundVerticalMargin = .25f;

        static void ConfigureLayer(RealityRoot root, string path, float orthoSize, List<string> changes)
        {
            Transform layer = root.transform.Find(path);
            if (layer == null) { Debug.LogError("LevelCameraSetup: missing background layer '" + path + "'. Run PARALLAX/Setup/Environment Art first."); return; }
            var parallax = layer.GetComponent<ParallaxLayer>();
            if (parallax == null) { Debug.LogError("LevelCameraSetup: '" + path + "' has no ParallaxLayer."); return; }

            var serialized = new SerializedObject(parallax);
            float screenSpeed = serialized.FindProperty("screenSpeed").floatValue;
            SerializedProperty tilesProp = serialized.FindProperty("tiles");

            float requiredHeight = 2f * (orthoSize + screenSpeed * BackgroundMaxAbsCameraY + BackgroundVerticalMargin);

            for (int i = 0; i < tilesProp.arraySize; i++)
            {
                var tileTransform = tilesProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (tileTransform == null) continue;
                var renderer = tileTransform.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                var size = new Vector2(renderer.size.x, requiredHeight);
                if (renderer.size != size)
                {
                    renderer.size = size;
                    changes.Add("set " + tileTransform.name + " (" + path + ") size");
                }
            }

            float offsetY = ParallaxMath.BackgroundTileOffsetY(requiredHeight, BackgroundMaxAbsCameraY, orthoSize, screenSpeed, BackgroundVerticalMargin, out float shortfall);
            SerializedProperty offsetProp = serialized.FindProperty("tileOffsetY");
            if (!Mathf.Approximately(offsetProp.floatValue, offsetY))
            {
                offsetProp.floatValue = offsetY;
                changes.Add("set " + path + " tileOffsetY");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (shortfall > 0f) Debug.LogWarning("LevelCameraSetup: " + path + " vertical coverage shortfall " + shortfall + "u.");
        }
    }
}
