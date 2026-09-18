using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Editor.Art
{
    // Idempotent scene/prefab wiring for PAX-A01. Safe to re-run after a new sprite export.
    public static class CatVisualSetup
    {
        const string PrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string SpriteSheetPath = "Assets/_Game/Art/Cats/CatA/CatA_Walk.png";
        const string ConfigPath = "Assets/_Game/Data/CatA_VisualConfig.asset";
        const string ShaderPath = "Assets/_Game/Art/Shaders/SpriteOutlineUnlit.shader";
        const string MaterialFolderPath = "Assets/_Game/Art/Materials";
        const string MaterialPath = MaterialFolderPath + "/Cat_OutlineUnlit.mat";
        static readonly string[] LegacyPlaceholderChildren = { "Body", "Ear_Front", "Ear_Back" };

        [MenuItem("PARALLAX/Setup/Cat Visual")]
        public static void Configure()
        {
            var changes = new List<string>();

            CatVisualConfig config = GetOrCreateConfig(changes);
            Material outlineMaterial = GetOrCreateOutlineMaterial(changes);

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, System.StringComparer.Ordinal)
                .ToArray();

            if (frames.Length == 0)
            {
                Debug.LogError($"CatVisualSetup: no sliced sprites found at '{SpriteSheetPath}'. Run 'PARALLAX/Art/Import Cat Sheet' first. Stopping without saving.");
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"CatVisualSetup: prefab not found at '{PrefabPath}'. Stopping without saving.");
                return;
            }

            int changesBeforePrefab = changes.Count;
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError($"CatVisualSetup: '{PrefabPath}' has no child named 'Visual'. Stopping without saving.");
                    return;
                }

                RemoveLegacyPlaceholderChildren(visual, changes);
                ApplyGroundContactOffset(root, visual, config, changes);

                var bodyRenderer = visual.GetComponent<SpriteRenderer>();
                if (bodyRenderer == null)
                {
                    bodyRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    changes.Add("added SpriteRenderer to Visual");
                }
                AssignIfChanged(bodyRenderer, bodyRenderer.sortingLayerName, RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay), changes, "set Visual SpriteRenderer sorting layer");
                if (bodyRenderer.sprite != frames[0])
                {
                    bodyRenderer.sprite = frames[0];
                    changes.Add("set Visual SpriteRenderer sprite");
                }

                SpriteRenderer outlineRenderer = GetOrCreateOutlineRenderer(visual, bodyRenderer.sortingLayerName, outlineMaterial, changes);

                var presenter = visual.GetComponent<CatVisualPresenter>();
                if (presenter == null)
                {
                    presenter = visual.gameObject.AddComponent<CatVisualPresenter>();
                    changes.Add("added CatVisualPresenter to Visual");
                }

                var presenterSO = new SerializedObject(presenter);
                AssignIfChanged(presenterSO, "config", config, changes, "CatVisualPresenter.config");
                AssignIfChanged(presenterSO, "bodyRenderer", bodyRenderer, changes, "CatVisualPresenter.bodyRenderer");
                AssignIfChanged(presenterSO, "outlineRenderer", outlineRenderer, changes, "CatVisualPresenter.outlineRenderer");

                SerializedProperty framesProp = presenterSO.FindProperty("frames");
                if (!SameSprites(framesProp, frames))
                {
                    framesProp.arraySize = frames.Length;
                    for (int i = 0; i < frames.Length; i++)
                        framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                    changes.Add($"assigned CatVisualPresenter.frames ({frames.Length} sprites)");
                }
                presenterSO.ApplyModifiedPropertiesWithoutUndo();

                if (changes.Count > changesBeforePrefab)
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            ConfigureSceneInstances(changes);

            if (changes.Count == 0)
                Debug.Log("CatVisualSetup: already configured.");
            else
                Debug.Log($"CatVisualSetup: {string.Join("; ", changes)}.");
        }

        static void ApplyGroundContactOffset(GameObject root, Transform visual, CatVisualConfig config, List<string> changes)
        {
            var collider = root.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                Debug.LogError($"CatVisualSetup: '{PrefabPath}' root has no CapsuleCollider2D. Cannot compute Visual's ground-contact position.");
                return;
            }

            var target = new Vector3(
                collider.offset.x,
                collider.offset.y - collider.size.y * 0.5f + config.GroundOffset,
                visual.localPosition.z);

            if (visual.localPosition != target)
            {
                visual.localPosition = target;
                changes.Add($"set Visual.localPosition = {target} (from collider offset/size + groundOffset)");
            }
        }

        static void RemoveLegacyPlaceholderChildren(Transform visual, List<string> changes)
        {
            foreach (string childName in LegacyPlaceholderChildren)
            {
                Transform child = visual.Find(childName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    changes.Add($"removed legacy placeholder '{childName}'");
                }
            }
        }

        static SpriteRenderer GetOrCreateOutlineRenderer(Transform visual, string sortingLayerName, Material material, List<string> changes)
        {
            Transform outline = visual.Find("Outline");
            GameObject outlineGO;
            if (outline == null)
            {
                outlineGO = new GameObject("Outline");
                outlineGO.transform.SetParent(visual, false);
                changes.Add("created Outline child");
            }
            else
            {
                outlineGO = outline.gameObject;
            }

            var renderer = outlineGO.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = outlineGO.AddComponent<SpriteRenderer>();
                changes.Add("added SpriteRenderer to Outline");
            }

            AssignIfChanged(renderer, renderer.sortingLayerName, sortingLayerName, changes, "set Outline sorting layer");
            if (renderer.sortingOrder != -1)
            {
                renderer.sortingOrder = -1;
                changes.Add("set Outline sorting order");
            }
            AssignMaterialIfChanged(renderer, material, changes, "assigned Outline material");

            return renderer;
        }

        static CatVisualConfig GetOrCreateConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<CatVisualConfig>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<CatVisualConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            changes.Add("created CatA_VisualConfig asset");
            return config;
        }

        static Material GetOrCreateOutlineMaterial(List<string> changes)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"CatVisualSetup: shader not found at '{ShaderPath}'.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolderPath))
                AssetDatabase.CreateFolder("Assets/_Game/Art", "Materials");

            material = new Material(shader) { name = "Cat_OutlineUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            changes.Add("created Cat_OutlineUnlit material asset");
            return material;
        }

        static void ConfigureSceneInstances(List<string> changes)
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;

                bool sceneChanged = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
                    {
                        RealityRoot reality = cat.GetComponentInParent<RealityRoot>();
                        Transform visual = cat.transform.Find("Visual");
                        if (reality == null || visual == null) continue;

                        SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
                        Transform outline = visual.Find("Outline");
                        SpriteRenderer outlineRenderer = outline != null ? outline.GetComponent<SpriteRenderer>() : null;
                        string sortingLayer = RealitySpace.SortingLayerName(reality.Id, SortingBand.Gameplay);
                        int rootLayer = reality.gameObject.layer;

                        sceneChanged |= ApplyInstanceRenderer(visual, visualRenderer, rootLayer, sortingLayer, changes);
                        sceneChanged |= ApplyInstanceRenderer(outline, outlineRenderer, rootLayer, sortingLayer, changes);
                        sceneChanged |= AssignObserver(cat, visual, changes);
                        sceneChanged |= RemoveUnusedOverrides(cat.gameObject, changes);
                    }
                }

                if (sceneChanged) EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static bool ApplyInstanceRenderer(Transform target, SpriteRenderer renderer, int layer, string sortingLayer, List<string> changes)
        {
            if (target == null || renderer == null) return false;
            bool changed = false;
            if (target.gameObject.layer != layer)
            {
                target.gameObject.layer = layer;
                changes.Add($"set {target.name}.layer");
                changed = true;
            }
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changes.Add($"set {target.name} sorting layer");
                changed = true;
            }
            return changed;
        }

        static bool AssignObserver(CatMotor2D cat, Transform visual, List<string> changes)
        {
            ObserverContext match = null;
            foreach (ObserverContext context in Object.FindObjectsByType<ObserverContext>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (context.Cat == cat)
                {
                    match = context;
                    break;
                }
            }

            var serialized = new SerializedObject(visual.GetComponent<CatVisualPresenter>());
            SerializedProperty property = serialized.FindProperty("observer");
            if (property.objectReferenceValue == match) return false;
            property.objectReferenceValue = match;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("assigned CatVisualPresenter observer");
            return true;
        }

        static bool RemoveUnusedOverrides(GameObject instanceRoot, List<string> changes)
        {
            var removed = new List<string>();
            foreach (PropertyModification modification in PrefabUtility.GetPropertyModifications(instanceRoot))
            {
                string targetName = TargetName(modification.target);
                if (targetName == "Body" || targetName == "Ear_Front" || targetName == "Ear_Back")
                    removed.Add(targetName + "." + modification.propertyPath);
            }

            PrefabUtility.RemoveUnusedOverrides(new[] { instanceRoot }, InteractionMode.AutomatedAction);
            if (removed.Count == 0) return false;
            changes.Add($"removed unused overrides from {instanceRoot.name}: {string.Join(", ", removed)}");
            return true;
        }

        static string TargetName(Object target)
        {
            if (target is Component component) return component.gameObject.name;
            if (target is GameObject gameObject) return gameObject.name;
            return target != null ? target.name : string.Empty;
        }

        static void AssignIfChanged(SerializedObject so, string propertyName, Object value, List<string> changes, string label)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop.objectReferenceValue == value) return;
            prop.objectReferenceValue = value;
            changes.Add($"assigned {label}");
        }

        static void AssignMaterialIfChanged(SpriteRenderer renderer, Material material, List<string> changes, string label)
        {
            if (renderer.sharedMaterial == material) return;
            renderer.sharedMaterial = material;
            changes.Add(label);
        }

        static void AssignIfChanged(SpriteRenderer renderer, string current, string value, List<string> changes, string label)
        {
            if (current == value) return;
            renderer.sortingLayerName = value;
            changes.Add(label);
        }

        static bool SameSprites(SerializedProperty framesProp, Sprite[] frames)
        {
            if (framesProp.arraySize != frames.Length) return false;
            for (int i = 0; i < frames.Length; i++)
                if (framesProp.GetArrayElementAtIndex(i).objectReferenceValue != frames[i]) return false;
            return true;
        }
    }
}
