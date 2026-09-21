using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Presentation;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Editor.Art
{
    // Idempotent prefab and scene wiring for PAX-A03 cat animation clips.
    public static class CatVisualSetup
    {
        readonly struct ClipSpec
        {
            public ClipSpec(string label, string property, string path, string prefix, float fps, bool loop)
            {
                Label = label;
                Property = property;
                Path = path;
                Prefix = prefix;
                Fps = fps;
                Loop = loop;
            }

            public readonly string Label;
            public readonly string Property;
            public readonly string Path;
            public readonly string Prefix;
            public readonly float Fps;
            public readonly bool Loop;
        }

        const string PrefabPath = "Assets/_Game/Gameplay/Player/Cat_Player.prefab";
        const string ConfigPath = "Assets/_Game/Data/CatA_VisualConfig.asset";
        const string CatArtPath = "Assets/_Game/Art/Cats/CatA/";
        const string ShaderPath = "Assets/_Game/Art/Shaders/SpriteOutlineUnlit.shader";
        const string MaterialFolderPath = "Assets/_Game/Art/Materials";
        const string MaterialPath = MaterialFolderPath + "/Cat_OutlineUnlit.mat";
        static readonly string[] LegacyPlaceholderChildren = { "Body", "Ear_Front", "Ear_Back" };
        static readonly ClipSpec[] Clips =
        {
            new ClipSpec("Idle", "idleClip", CatArtPath + "CatA_Idle.png", "CatA_Idle_", 7f, true),
            new ClipSpec("Walk", "walkClip", CatArtPath + "CatA_Walk.png", "CatA_Walk_", 10f, true),
            new ClipSpec("Rise", "riseClip", CatArtPath + "CatA_Rise.png", "CatA_Rise_", 12f, false),
            new ClipSpec("Fall", "fallClip", CatArtPath + "CatA_Fall.png", "CatA_Fall_", 12f, false),
            new ClipSpec("Land", "landClip", CatArtPath + "CatA_Land.png", "CatA_Land_", 12f, false)
        };

        [MenuItem("PARALLAX/Setup/Cat Visual")]
        public static void Configure()
        {
            var changes = new List<string>();
            CatVisualConfig config = GetOrCreateConfig(changes);
            Material outlineMaterial = GetOrCreateOutlineMaterial(changes);
            Dictionary<string, Sprite[]> framesByClip = LoadFrames();

            if (!ValidateSheets(config, framesByClip))
            {
                Debug.LogError("CatVisualSetup: sheet validation failed. Stopping without saving.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"CatVisualSetup: prefab not found at '{PrefabPath}'. Stopping without saving.");
                return;
            }

            int changesBeforePrefab = changes.Count;
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                CatMotor2D motor = root.GetComponent<CatMotor2D>();
                if (visual == null || motor == null)
                {
                    Debug.LogError($"CatVisualSetup: '{PrefabPath}' needs a Visual child and root CatMotor2D. Stopping without saving.");
                    return;
                }

                RemoveLegacyPlaceholderChildren(visual, changes);
                ApplyGroundContactOffset(root, visual, config, changes);

                Sprite initialSprite = framesByClip["idleClip"][0];
                SpriteRenderer body = visual.GetComponent<SpriteRenderer>();
                if (body == null)
                {
                    body = visual.gameObject.AddComponent<SpriteRenderer>();
                    changes.Add("added SpriteRenderer to Visual");
                }
                SetSortingLayer(body, RealitySpace.SortingLayerName(ObserverId.A, SortingBand.Gameplay), changes, "Visual");
                SetSprite(body, initialSprite, changes, "Visual");

                SpriteRenderer outline = GetOrCreateOutlineRenderer(visual, body.sortingLayerName, outlineMaterial, changes);
                SetSprite(outline, initialSprite, changes, "Outline");

                CatVisualPresenter presenter = visual.GetComponent<CatVisualPresenter>();
                if (presenter == null)
                {
                    presenter = visual.gameObject.AddComponent<CatVisualPresenter>();
                    changes.Add("added CatVisualPresenter to Visual");
                }

                var presenterSO = new SerializedObject(presenter);
                AssignObject(presenterSO, "config", config, changes, "CatVisualPresenter.config");
                AssignObject(presenterSO, "bodyRenderer", body, changes, "CatVisualPresenter.bodyRenderer");
                AssignObject(presenterSO, "outlineRenderer", outline, changes, "CatVisualPresenter.outlineRenderer");
                AssignObject(presenterSO, "motor", motor, changes, "CatVisualPresenter.motor");
                foreach (ClipSpec clip in Clips)
                    AssignClip(presenterSO, clip, framesByClip[clip.Property], changes);
                presenterSO.ApplyModifiedPropertiesWithoutUndo();

                if (changes.Count > changesBeforePrefab)
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Rewrites the existing config through Unity so removed serialized fields do not linger in YAML.
            AssetDatabase.ForceReserializeAssets(new[] { ConfigPath });
            ConfigureSceneInstances(changes);
            ReportAssignments();

            Debug.Log(changes.Count == 0
                ? "CatVisualSetup: already configured."
                : $"CatVisualSetup: {string.Join("; ", changes)}.");
        }

        static Dictionary<string, Sprite[]> LoadFrames()
        {
            var result = new Dictionary<string, Sprite[]>();
            foreach (ClipSpec clip in Clips)
            {
                result[clip.Property] = AssetDatabase.LoadAllAssetsAtPath(clip.Path)
                    .OfType<Sprite>()
                    .Where(sprite => sprite.name.StartsWith(clip.Prefix, System.StringComparison.Ordinal))
                    .OrderBy(sprite => sprite.name, System.StringComparer.Ordinal)
                    .ToArray();
            }
            return result;
        }

        static bool ValidateSheets(CatVisualConfig config, Dictionary<string, Sprite[]> framesByClip)
        {
            TextureImporter walkImporter = AssetImporter.GetAtPath(Clips[1].Path) as TextureImporter;
            Sprite[] walkFrames = framesByClip[Clips[1].Property];
            if (walkImporter == null || walkFrames.Length == 0) return false;

            float expectedPpu = walkImporter.spritePixelsPerUnit;
            Vector2 expectedPivot = walkFrames[0].pivot;
            bool valid = true;
            foreach (ClipSpec clip in Clips)
            {
                TextureImporter importer = AssetImporter.GetAtPath(clip.Path) as TextureImporter;
                Sprite[] frames = framesByClip[clip.Property];
                if (importer == null || frames.Length == 0)
                {
                    Debug.LogError($"CatVisualSetup: {clip.Label} has no imported sprites at '{clip.Path}'.");
                    valid = false;
                    continue;
                }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, expectedPpu))
                {
                    Debug.LogError($"CatVisualSetup: {clip.Label} PPU {importer.spritePixelsPerUnit} != Walk PPU {expectedPpu}.");
                    valid = false;
                }
                foreach (Sprite frame in frames)
                {
                    if (!Mathf.Approximately(frame.rect.width, config.CellSize)
                        || !Mathf.Approximately(frame.rect.height, config.CellSize)
                        || Vector2.Distance(frame.pivot, expectedPivot) > 0.01f)
                    {
                        Debug.LogError($"CatVisualSetup: '{frame.name}' does not match the {config.CellSize}px cell and Walk pivot.");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static void AssignClip(SerializedObject presenter, ClipSpec clip, Sprite[] frames, List<string> changes)
        {
            SerializedProperty clipProperty = presenter.FindProperty(clip.Property);
            SerializedProperty framesProperty = clipProperty.FindPropertyRelative("frames");
            bool changed = false;
            if (!SameSprites(framesProperty, frames))
            {
                framesProperty.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++)
                    framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                changed = true;
            }

            SerializedProperty fpsProperty = clipProperty.FindPropertyRelative("fps");
            SerializedProperty loopProperty = clipProperty.FindPropertyRelative("loop");
            if (!Mathf.Approximately(fpsProperty.floatValue, clip.Fps))
            {
                fpsProperty.floatValue = clip.Fps;
                changed = true;
            }
            if (loopProperty.boolValue != clip.Loop)
            {
                loopProperty.boolValue = clip.Loop;
                changed = true;
            }
            if (changed) changes.Add($"assigned {clip.Label} clip ({frames.Length} sprites)");
        }

        static void ConfigureSceneInstances(List<string> changes)
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;
                bool sceneChanged = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (CatMotor2D cat in root.GetComponentsInChildren<CatMotor2D>(true))
                {
                    RealityRoot reality = cat.GetComponentInParent<RealityRoot>();
                    Transform visual = cat.transform.Find("Visual");
                    if (reality == null || visual == null) continue;

                    SpriteRenderer body = visual.GetComponent<SpriteRenderer>();
                    Transform outlineTransform = visual.Find("Outline");
                    SpriteRenderer outline = outlineTransform != null ? outlineTransform.GetComponent<SpriteRenderer>() : null;
                    string sortingLayer = RealitySpace.SortingLayerName(reality.Id, SortingBand.Gameplay);
                    sceneChanged |= ApplyInstanceRenderer(visual, body, reality.gameObject.layer, sortingLayer, changes);
                    sceneChanged |= ApplyInstanceRenderer(outlineTransform, outline, reality.gameObject.layer, sortingLayer, changes);
                    sceneChanged |= AssignSceneReferences(cat, visual, changes);
                    sceneChanged |= RemoveUnusedOverrides(cat.gameObject, changes);
                }
                if (sceneChanged) EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static bool AssignSceneReferences(CatMotor2D cat, Transform visual, List<string> changes)
        {
            CatVisualPresenter presenter = visual.GetComponent<CatVisualPresenter>();
            if (presenter == null) return false;
            ObserverContext match = Object.FindObjectsByType<ObserverContext>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(context => context.Cat == cat);
            var serialized = new SerializedObject(presenter);
            bool changed = AssignObject(serialized, "motor", cat)
                | AssignObject(serialized, "observer", match);
            if (!changed) return false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"assigned {cat.name} presenter references");
            return true;
        }

        static void ReportAssignments()
        {
            var reported = new HashSet<ObserverId>();
            foreach (CatMotor2D cat in Object.FindObjectsByType<CatMotor2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                RealityRoot reality = cat.GetComponentInParent<RealityRoot>();
                CatVisualPresenter presenter = cat.transform.Find("Visual")?.GetComponent<CatVisualPresenter>();
                if (reality == null || presenter == null || !reported.Add(reality.Id)) continue;

                var serialized = new SerializedObject(presenter);
                var missing = new List<string>();
                foreach (ClipSpec clip in Clips)
                {
                    SerializedProperty frames = serialized.FindProperty(clip.Property)?.FindPropertyRelative("frames");
                    if (frames == null || frames.arraySize == 0) missing.Add(clip.Label);
                }
                int missingRenderers = serialized.FindProperty("bodyRenderer").objectReferenceValue == null ? 1 : 0;
                if (serialized.FindProperty("outlineRenderer").objectReferenceValue == null) missingRenderers++;
                Debug.Log($"CatVisualSetup: Reality {reality.Id}: {Clips.Length - missing.Count}/{Clips.Length} clips assigned; unassigned slots: {(missing.Count == 0 ? "none" : string.Join(", ", missing))}; unassigned renderers: {missingRenderers}.");
            }
            foreach (ObserverId id in new[] { ObserverId.A, ObserverId.B })
                if (!reported.Contains(id)) Debug.LogWarning($"CatVisualSetup: Reality {id}: 0/{Clips.Length} clips assigned; cat not found in loaded scenes.");
        }

        static void ApplyGroundContactOffset(GameObject root, Transform visual, CatVisualConfig config, List<string> changes)
        {
            CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
            if (collider == null)
            {
                Debug.LogError($"CatVisualSetup: '{PrefabPath}' has no CapsuleCollider2D; cannot place Visual at ground contact.");
                return;
            }
            var target = new Vector3(collider.offset.x, collider.offset.y - collider.size.y * 0.5f + config.GroundOffset, visual.localPosition.z);
            if (visual.localPosition == target) return;
            visual.localPosition = target;
            changes.Add($"set Visual.localPosition = {target}");
        }

        static void RemoveLegacyPlaceholderChildren(Transform visual, List<string> changes)
        {
            foreach (string childName in LegacyPlaceholderChildren)
            {
                Transform child = visual.Find(childName);
                if (child == null) continue;
                Object.DestroyImmediate(child.gameObject);
                changes.Add($"removed legacy placeholder '{childName}'");
            }
        }

        static SpriteRenderer GetOrCreateOutlineRenderer(Transform visual, string sortingLayer, Material material, List<string> changes)
        {
            Transform child = visual.Find("Outline");
            if (child == null)
            {
                var childObject = new GameObject("Outline");
                childObject.transform.SetParent(visual, false);
                child = childObject.transform;
                changes.Add("created Outline child");
            }
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
                changes.Add("added SpriteRenderer to Outline");
            }
            SetSortingLayer(renderer, sortingLayer, changes, "Outline");
            if (renderer.sortingOrder != -1)
            {
                renderer.sortingOrder = -1;
                changes.Add("set Outline sorting order");
            }
            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
                changes.Add("assigned Outline material");
            }
            return renderer;
        }

        static CatVisualConfig GetOrCreateConfig(List<string> changes)
        {
            CatVisualConfig config = AssetDatabase.LoadAssetAtPath<CatVisualConfig>(ConfigPath);
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
            if (!AssetDatabase.IsValidFolder(MaterialFolderPath)) AssetDatabase.CreateFolder("Assets/_Game/Art", "Materials");
            material = new Material(shader) { name = "Cat_OutlineUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            changes.Add("created Cat_OutlineUnlit material asset");
            return material;
        }

        static bool ApplyInstanceRenderer(Transform target, SpriteRenderer renderer, int layer, string sortingLayer, List<string> changes)
        {
            if (target == null || renderer == null) return false;
            bool changed = false;
            if (target.gameObject.layer != layer)
            {
                target.gameObject.layer = layer;
                changed = true;
            }
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
                changed = true;
            }
            if (changed) changes.Add($"set {target.name} reality layer/sorting");
            return changed;
        }

        static bool RemoveUnusedOverrides(GameObject instanceRoot, List<string> changes)
        {
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instanceRoot);
            int legacyCount = modifications == null ? 0 : modifications.Count(modification =>
                TargetName(modification.target) == "Body" || TargetName(modification.target) == "Ear_Front" || TargetName(modification.target) == "Ear_Back");
            PrefabUtility.RemoveUnusedOverrides(new[] { instanceRoot }, InteractionMode.AutomatedAction);
            if (legacyCount == 0) return false;
            changes.Add($"removed {legacyCount} unused overrides from {instanceRoot.name}");
            return true;
        }

        static string TargetName(Object target)
        {
            if (target is Component component) return component.gameObject.name;
            if (target is GameObject gameObject) return gameObject.name;
            return target != null ? target.name : string.Empty;
        }

        static void AssignObject(SerializedObject serialized, string propertyName, Object value, List<string> changes, string label)
        {
            if (!AssignObject(serialized, propertyName, value)) return;
            changes.Add($"assigned {label}");
        }

        static bool AssignObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        static void SetSortingLayer(SpriteRenderer renderer, string value, List<string> changes, string label)
        {
            if (renderer.sortingLayerName == value) return;
            renderer.sortingLayerName = value;
            changes.Add($"set {label} sorting layer");
        }

        static void SetSprite(SpriteRenderer renderer, Sprite value, List<string> changes, string label)
        {
            if (renderer.sprite == value) return;
            renderer.sprite = value;
            changes.Add($"set {label} sprite");
        }

        static bool SameSprites(SerializedProperty property, Sprite[] frames)
        {
            if (property.arraySize != frames.Length) return false;
            for (int i = 0; i < frames.Length; i++)
                if (property.GetArrayElementAtIndex(i).objectReferenceValue != frames[i]) return false;
            return true;
        }
    }
}
