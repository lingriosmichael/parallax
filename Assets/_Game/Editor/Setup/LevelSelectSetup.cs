using System.Collections.Generic;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-053 (D-063) §2.3: builds/configures the LevelSelect scene. Idempotent
    /// find-or-create + rewire, like the other PARALLAX/Setup menus. Its guard accepts only the
    /// "LevelSelect" scene - except on the very first run, when the scene file doesn't exist yet
    /// and there's nothing else to be active. Also syncs Build Settings (BuildSceneList.Sync,
    /// D-072) so the newly-built scene is registered immediately.</summary>
    public static class LevelSelectSetup
    {
        public const string RequiredSceneName = LevelSceneLoader.LevelSelectSceneName;
        const string ScenePath = "Assets/_Game/Scenes/" + LevelSceneLoader.LevelSelectSceneName + ".unity";
        const string LevelListConfigPath = "Assets/_Game/Data/LevelListConfig.asset";
        const string CanvasName = "Canvas";
        const string RowsContainerName = "Rows";
        const string RowTemplateName = "RowTemplate";

        public static bool RefusesToRun(string activeSceneName) => activeSceneName != RequiredSceneName;

        // Review fix: the bootstrap path below replaces the active scene in memory
        // (EditorSceneManager.NewScene(..., Single)) without saving it first - unlike the
        // File -> New Scene menu, the programmatic call never prompts. Same rule LevelSetup.
        // RegenerateScene already enforces for its own scene-replacing menus (LevelSetup.cs).
        public static bool RefusesToCreate(bool activeSceneIsDirty) => activeSceneIsDirty;

        [MenuItem("PARALLAX/Setup/Levels/Level Select")]
        public static void Configure()
        {
            Scene scene;
            if (AssetDatabase.LoadAssetAtPath<Object>(ScenePath) == null)
            {
                Scene active = EditorSceneManager.GetActiveScene();
                if (RefusesToCreate(active.isDirty))
                {
                    Debug.LogError("LevelSelectSetup: '" + active.name + "' has unsaved changes; save or discard them first.");
                    return;
                }

                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    Debug.LogError("LevelSelectSetup: failed to create " + ScenePath + ".");
                    return;
                }
                scene = EditorSceneManager.GetActiveScene();
            }
            else
            {
                scene = EditorSceneManager.GetActiveScene();
                if (RefusesToRun(scene.name))
                {
                    Debug.LogError($"LevelSelectSetup: refuses to run outside '{RequiredSceneName}' (active scene is '{scene.name}'); open it first.");
                    return;
                }
            }

            LevelListConfig levelList = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (levelList == null)
            {
                Debug.LogError("LevelSelectSetup: no LevelListConfig at '" + LevelListConfigPath + "'. Run PARALLAX/Setup/Level List (PAX-050) first.");
                return;
            }

            var changes = new List<string>();
            EnsureEventSystem(changes);
            BuildUI(levelList, changes);

            if (changes.Count == 0)
            {
                Debug.Log("LevelSelectSetup: no changes.");
            }
            else
            {
                Debug.Log("LevelSelectSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(scene);
            }

            BuildSceneList.Sync();
        }

        // Minimum EventSystem + InputSystemUIInputModule, matching SwitchSetup's pattern
        // (that helper is private and this scene has no HUD to share it with, so it's
        // duplicated here rather than making SwitchSetup's helpers public - CLAUDE.md's ticket
        // rule not to touch SwitchSetup.cs).
        static void EnsureEventSystem(List<string> changes)
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            GameObject go;
            if (eventSystem == null)
            {
                go = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(go, "Setup Level Select");
                eventSystem = go.AddComponent<EventSystem>();
                changes.Add("created EventSystem");
            }
            else
            {
                go = eventSystem.gameObject;
            }

            var legacyModule = go.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.DestroyImmediate(legacyModule);
                changes.Add("removed StandaloneInputModule from EventSystem");
            }

            if (go.GetComponent<InputSystemUIInputModule>() == null)
            {
                go.AddComponent<InputSystemUIInputModule>();
                changes.Add("added InputSystemUIInputModule to EventSystem");
            }
        }

        static void BuildUI(LevelListConfig levelList, List<string> changes)
        {
            GameObject canvasGO = GameObject.Find(CanvasName);
            bool createdCanvas = canvasGO == null;
            if (createdCanvas)
            {
                canvasGO = new GameObject(CanvasName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Level Select");
                changes.Add($"created {CanvasName}");
            }

            var canvas = EnsureComponent<Canvas>(canvasGO, changes, "Canvas", CanvasName);
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                changes.Add($"set {CanvasName}.renderMode = ScreenSpaceOverlay");
            }

            var scaler = EnsureComponent<CanvasScaler>(canvasGO, changes, "CanvasScaler", CanvasName);
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != new Vector2(1920f, 1080f)
                || !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                changes.Add($"configured {CanvasName} CanvasScaler (1920x1080, match 0.5)");
            }

            EnsureComponent<GraphicRaycaster>(canvasGO, changes, "GraphicRaycaster", CanvasName);

            GameObject rowsGO = EnsureChild(canvasGO.transform, RowsContainerName, changes);
            SetAnchors((RectTransform)rowsGO.transform, Vector2.zero, Vector2.one, new Vector2(80f, 40f), new Vector2(-80f, -40f), changes, RowsContainerName);
            var scrollRect = EnsureComponent<ScrollRect>(rowsGO, changes, "ScrollRect", RowsContainerName);

            GameObject viewportGO = EnsureChild(rowsGO.transform, "Viewport", changes);
            SetAnchors((RectTransform)viewportGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, "Viewport");
            EnsureComponent<RectMask2D>(viewportGO, changes, "RectMask2D", "Viewport");

            GameObject contentGO = EnsureChild(viewportGO.transform, "Content", changes);
            var contentRect = (RectTransform)contentGO.transform;
            SetAnchors(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, changes, "Content");
            // A ScrollRect's content rect must pivot from its top edge (anchors are top-stretch
            // above) or ContentSizeFitter's PreferredSize growth pushes half of it above the
            // viewport instead of down into it.
            if (contentRect.pivot != new Vector2(0.5f, 1f)) { contentRect.pivot = new Vector2(0.5f, 1f); changes.Add("set Content pivot = (0.5, 1)"); }
            ConfigureContentLayout(contentGO, changes);
            var sizeFitter = EnsureComponent<ContentSizeFitter>(contentGO, changes, "ContentSizeFitter", "Content");
            if (sizeFitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
            {
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                changes.Add("set Content ContentSizeFitter.verticalFit = PreferredSize");
            }

            if (scrollRect.viewport != (RectTransform)viewportGO.transform) { scrollRect.viewport = (RectTransform)viewportGO.transform; changes.Add("wired ScrollRect.viewport"); }
            if (scrollRect.content != contentRect) { scrollRect.content = contentRect; changes.Add("wired ScrollRect.content"); }
            if (scrollRect.horizontal) { scrollRect.horizontal = false; changes.Add("set ScrollRect.horizontal = false"); }

            LevelSelectRow row = BuildRowTemplate(contentGO.transform, changes);

            var controller = EnsureComponent<LevelSelectController>(canvasGO, changes, "LevelSelectController", CanvasName);
            WireField(controller, "levelList", levelList, changes, "LevelSelectController.levelList = LevelListConfig");
            WireField(controller, "rowsContainer", contentRect, changes, "LevelSelectController.rowsContainer = Content");
            WireField(controller, "rowTemplate", row, changes, "LevelSelectController.rowTemplate = RowTemplate");
        }

        // Builder seam: Content's VerticalLayoutGroup. It controls both axes: height from each row's LayoutElement,
        // and width stretched to Content's. Without childControlWidth, the rows kept the template's 0 width and the
        // list drew nothing (PAX-053 bug, fixed after PAX-059a).
        public static VerticalLayoutGroup ConfigureContentLayout(GameObject contentGO, List<string> changes)
        {
            var layout = EnsureComponent<VerticalLayoutGroup>(contentGO, changes, "VerticalLayoutGroup", "Content");
            if (layout.spacing != 12f || !layout.childControlHeight || layout.childForceExpandHeight || !layout.childControlWidth || !layout.childForceExpandWidth)
            {
                layout.spacing = 12f;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childAlignment = TextAnchor.UpperCenter;
                changes.Add("configured Content VerticalLayoutGroup");
            }
            return layout;
        }

        // Builder seam (review fix): pulled out of BuildUI so a test can build exactly the row
        // LevelSelectSetup builds - LayoutElement included - without running the whole menu or
        // needing a LevelListConfig/scene. Idempotent like every other Ensure* helper here.
        public static LevelSelectRow BuildRowTemplate(Transform contentParent, List<string> changes)
        {
            GameObject rowTemplateGO = EnsureChild(contentParent, RowTemplateName, changes);
            var rowRect = (RectTransform)rowTemplateGO.transform;
            if (rowRect.sizeDelta != new Vector2(0f, 100f)) { rowRect.sizeDelta = new Vector2(0f, 100f); changes.Add($"sized {RowTemplateName}"); }
            // Content's VerticalLayoutGroup has childControlHeight = true, so it sizes every row
            // from LayoutUtility.GetPreferredHeight - without a LayoutElement, a row with no
            // Text/Image content of its own (an Image with a null sprite has preferred height 0)
            // collapses to zero height regardless of rowRect.sizeDelta above.
            var rowLayoutElement = EnsureComponent<LayoutElement>(rowTemplateGO, changes, "LayoutElement", RowTemplateName);
            if (!Mathf.Approximately(rowLayoutElement.preferredHeight, 100f)) { rowLayoutElement.preferredHeight = 100f; changes.Add($"set {RowTemplateName} LayoutElement.preferredHeight = 100"); }

            var rowImage = EnsureComponent<Image>(rowTemplateGO, changes, "Image", RowTemplateName);
            SetColor(rowImage, Color.white, changes, $"{RowTemplateName} Image");
            var rowButton = EnsureComponent<Button>(rowTemplateGO, changes, "Button", RowTemplateName);

            GameObject nameGO = EnsureChild(rowTemplateGO.transform, "Name", changes);
            SetAnchors((RectTransform)nameGO.transform, new Vector2(0f, 0f), new Vector2(0.7f, 1f), new Vector2(20f, 0f), new Vector2(0f, 0f), changes, "Name");
            var nameText = EnsureComponent<Text>(nameGO, changes, "Text", "Name");
            ConfigureText(nameText, "Level", 40, TextAnchor.MiddleLeft, Color.black, changes, "Name");

            GameObject bestGO = EnsureChild(rowTemplateGO.transform, "Best", changes);
            SetAnchors((RectTransform)bestGO.transform, new Vector2(0.7f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-20f, 0f), changes, "Best");
            var bestText = EnsureComponent<Text>(bestGO, changes, "Text", "Best");
            ConfigureText(bestText, "—", 36, TextAnchor.MiddleRight, Color.black, changes, "Best");

            var row = EnsureComponent<LevelSelectRow>(rowTemplateGO, changes, "LevelSelectRow", RowTemplateName);
            WireField(row, "button", rowButton, changes, "LevelSelectRow.button = Button");
            WireField(row, "nameText", nameText, changes, "LevelSelectRow.nameText = Name");
            WireField(row, "bestDeathsText", bestText, changes, "LevelSelectRow.bestDeathsText = Best");
            SetActive(rowTemplateGO, false, changes, RowTemplateName); // cloned per level at runtime, never shown itself
            return row;
        }

        static GameObject EnsureChild(Transform parent, string name, List<string> changes)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Setup Level Select");
            go.transform.SetParent(parent, false);
            changes.Add($"created {name} under {parent.name}");
            return go;
        }

        static T EnsureComponent<T>(GameObject go, List<string> changes, string label, string ownerLabel) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
                changes.Add($"added {label} to {ownerLabel}");
            }
            return component;
        }

        static void SetActive(GameObject go, bool active, List<string> changes, string label)
        {
            if (go.activeSelf == active) return;
            go.SetActive(active);
            changes.Add($"set {label} active = {active}");
        }

        static void SetColor(Image image, Color color, List<string> changes, string label)
        {
            if (image.color == color) return;
            image.color = color;
            changes.Add($"set {label}.color");
        }

        static void ConfigureText(Text text, string content, int fontSize, TextAnchor alignment, Color color, List<string> changes, string label)
        {
            bool changed = false;
            if (text.font == null) { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); changed = true; }
            if (text.text != content) { text.text = content; changed = true; }
            if (text.fontSize != fontSize) { text.fontSize = fontSize; changed = true; }
            if (text.alignment != alignment) { text.alignment = alignment; changed = true; }
            if (text.color != color) { text.color = color; changed = true; }
            if (changed) changes.Add($"configured {label} Text");
        }

        static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, List<string> changes, string label)
        {
            if (rect.anchorMin == anchorMin && rect.anchorMax == anchorMax && rect.offsetMin == offsetMin && rect.offsetMax == offsetMax) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            changes.Add($"positioned {label}");
        }

        static void WireField(Object target, string propertyName, Object value, List<string> changes, string label)
        {
            if (target == null || value == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"LevelSelectSetup: '{target.GetType().Name}' has no serialized field '{propertyName}'.");
                return;
            }
            if (prop.objectReferenceValue != value)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                changes.Add($"assigned {label}");
            }
        }
    }
}
