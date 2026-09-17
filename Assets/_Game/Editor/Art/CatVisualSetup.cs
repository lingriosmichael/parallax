using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // Idempotent scene/prefab wiring for PAX-A01. Safe to re-run after a new sprite export.
    public static class CatVisualSetup
    {
        const string PrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string SpriteSheetPath = "Assets/_Game/Art/Cats/CatA/CatA_Walk.png";
        const string ConfigPath = "Assets/_Game/Data/CatA_VisualConfig.asset";
        const string ShaderPath = "Assets/_Game/Art/Shaders/SpriteOutlineUnlit.shader";
        static readonly string[] LegacyPlaceholderChildren = { "Body", "Ear_Front", "Ear_Back" };

        [MenuItem("PARALLAX/Setup/Cat Visual")]
        public static void Configure()
        {
            var changes = new List<string>();

            CatVisualConfig config = GetOrCreateConfig(changes);

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

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError($"CatVisualSetup: '{PrefabPath}' has no child named 'Visual'. Stopping without saving.");
                    return;
                }

                DeactivateLegacyPlaceholders(visual, changes);
                ApplyGroundContactOffset(root, visual, config, changes);

                var bodyRenderer = visual.GetComponent<SpriteRenderer>();
                if (bodyRenderer == null)
                {
                    bodyRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    changes.Add("added SpriteRenderer to Visual");
                }
                bodyRenderer.sortingLayerName = RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay);
                bodyRenderer.sprite = frames[0];

                SpriteRenderer outlineRenderer = GetOrCreateOutlineRenderer(visual, bodyRenderer.sortingLayerName, changes);

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

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (changes.Count == 0)
                Debug.Log("CatVisualSetup: Cat A visual already configured.");
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

        static void DeactivateLegacyPlaceholders(Transform visual, List<string> changes)
        {
            foreach (string childName in LegacyPlaceholderChildren)
            {
                Transform child = visual.Find(childName);
                if (child != null && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    changes.Add($"deactivated legacy placeholder '{childName}'");
                }
            }
        }

        static SpriteRenderer GetOrCreateOutlineRenderer(Transform visual, string sortingLayerName, List<string> changes)
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

            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = -1;

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"CatVisualSetup: shader not found at '{ShaderPath}'.");
            }
            else if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != shader)
            {
                renderer.sharedMaterial = new Material(shader) { name = "CatA_OutlineUnlit" };
                changes.Add("assigned Outline material");
            }

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

        static void AssignIfChanged(SerializedObject so, string propertyName, Object value, List<string> changes, string label)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop.objectReferenceValue == value) return;
            prop.objectReferenceValue = value;
            changes.Add($"assigned {label}");
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
