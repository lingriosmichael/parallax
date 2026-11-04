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
        const string SkyPath = "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ENV_SkyPlate.png";
        // PAX-A09: 75 by 56 dp on the Pixel 8a, and five lines of nine fill a 16:9 screen with no scrolling.
        static readonly Vector2 CellSize = new Vector2(176f, 136f), CellGap = new Vector2(16f, 14f);

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

            BuildBackdrop(canvasGO.transform, changes);
            GameObject rowsGO = EnsureChild(canvasGO.transform, RowsContainerName, changes);
            // D-314: under the chapter tabs.
            SetAnchors((RectTransform)rowsGO.transform, Vector2.zero, Vector2.one, new Vector2(80f, 40f), new Vector2(-80f, -292f), changes, RowsContainerName);
            KeepAt(rowsGO.transform, 9, changes);
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
            WireField(controller, "chapters", BuildChapters(canvasGO.transform, levelList, changes), changes, "LevelSelectController.chapters = LevelChapters");

            // D-314: the screen opens out of black (ScreenReveal), over everything; it never takes a tap and turns itself off.
            Image reveal = GameUiStyle.Picture(canvasGO.transform, "Reveal", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, null, new Color(0.03f, 0.02f, 0.01f, 1f), changes);
            SetAnchors((RectTransform)reveal.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, "Reveal");
            EnsureComponent<ScreenReveal>(reveal.gameObject, changes, "ScreenReveal", "Reveal");
            if (reveal.transform.GetSiblingIndex() != canvasGO.transform.childCount - 1) { reveal.transform.SetAsLastSibling(); changes.Add("moved Reveal on top"); }
        }

        // D-314: Level Select in chapters, one per district of ten levels: the tabs under the heading, and each chapter's landmark
        // rising from the foot of the screen over the sky, tinted to the chapter's time of day (as its levels are lit).
        static readonly (string name, string art, Color sky)[] ChapterLooks =
        {
            ("The Gardens", "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ENV_Landmark_0.png", new Color(1f, 0.95f, 0.86f, 1f)),
            ("The Overgrown Quarter", "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ENV_Landmark_1.png", new Color(1f, 0.8f, 0.64f, 1f)),
            ("The High Citadel", "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ENV_Landmark_2.png", new Color(0.78f, 0.86f, 1f, 1f)),
            ("Obsidian Void", "Assets/_Game/Art/ObsidianVoid/Environment/Backgrounds/Kit/ENV_VoidHeart.png", new Color(0.62f, 0.56f, 0.92f, 1f)),
        };
        const int ChapterSize = 10;
        const float TabWidth = 400f, TabGap = 16f;

        static LevelChapters BuildChapters(Transform canvas, LevelListConfig levelList, List<string> changes)
        {
            int levels = levelList != null ? levelList.Levels.Count : 0;
            int count = Mathf.Max(1, Mathf.CeilToInt(levels / (float)ChapterSize));
            GameObject tabsGO = EnsureChild(canvas, "Chapters", changes);
            GameUiStyle.Place((RectTransform)tabsGO.transform, new Vector2(0.5f, 1f), new Vector2(count * (TabWidth + TabGap), 76f), new Vector2(0f, -226f), changes, "Chapters");
            var tabs = new Button[count];
            for (int i = 0; i < count; i++)
            {
                string name = i < ChapterLooks.Length ? ChapterLooks[i].name : "Chapter " + (i + 1);
                GameObject tab = EnsureChild(tabsGO.transform, "Chapter_" + i, changes);
                GameUiStyle.Place((RectTransform)tab.transform, new Vector2(0.5f, 0.5f), new Vector2(TabWidth, 76f), new Vector2((i - (count - 1) * 0.5f) * (TabWidth + TabGap), 0f), changes, tab.name);
                tabs[i] = GameUiStyle.Framed(tab, name, 26, false, changes);
            }
            Transform background = canvas.Find("Background");
            LevelChapters chapters = EnsureComponent<LevelChapters>(canvas.gameObject, changes, "LevelChapters", CanvasName);
            var serialized = new SerializedObject(chapters);
            SerializedProperty list = serialized.FindProperty("chapters"), tabList = serialized.FindProperty("tabs");
            list.arraySize = count;
            tabList.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty c = list.GetArrayElementAtIndex(i);
                bool known = i < ChapterLooks.Length;
                c.FindPropertyRelative("Name").stringValue = known ? ChapterLooks[i].name : "Chapter " + (i + 1);
                c.FindPropertyRelative("Count").intValue = Mathf.Min(ChapterSize, levels - i * ChapterSize);
                c.FindPropertyRelative("Art").objectReferenceValue = known ? AssetDatabase.LoadAssetAtPath<Sprite>(ChapterLooks[i].art) : null;
                c.FindPropertyRelative("Sky").colorValue = known ? ChapterLooks[i].sky : Color.white;
                tabList.GetArrayElementAtIndex(i).objectReferenceValue = tabs[i];
            }
            serialized.FindProperty("art").objectReferenceValue = canvas.Find("ChapterArt")?.GetComponent<Image>();
            serialized.FindProperty("sky").objectReferenceValue = background != null ? background.GetComponent<Image>() : null;
            if (serialized.ApplyModifiedPropertiesWithoutUndo()) changes.Add($"set LevelChapters ({count} chapters)");
            return chapters;
        }

        // PAX-A09: the sky of the title's world, darkened so the gold stands on it, with the title's leaves in the corners and a heading. Behind the
        // cells, and behind the unlock panel, which stays on top.
        static void BuildBackdrop(Transform canvas, List<string> changes)
        {
            var center = new Vector2(0.5f, 0.5f);
            Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>(SkyPath);
            Image back = GameUiStyle.Picture(canvas, "Background", center, Vector2.zero, Vector2.zero, sky, Color.white, changes);
            var fitter = EnsureComponent<AspectRatioFitter>(back.gameObject, changes, "AspectRatioFitter", "Background");
            float aspect = sky != null ? sky.rect.width / sky.rect.height : 1.5f;
            if (fitter.aspectMode != AspectRatioFitter.AspectMode.EnvelopeParent || !Mathf.Approximately(fitter.aspectRatio, aspect))
            {
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = aspect;
                changes.Add("set Background to fill the screen");
            }
            Image shade = GameUiStyle.Picture(canvas, "Shade", center, Vector2.zero, Vector2.zero, null, new Color(0.09f, 0.05f, 0.03f, 0.56f), changes);
            SetAnchors((RectTransform)shade.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, "Shade");
            // D-314: the chapter's landmark, rising from the foot of the screen behind the cells (LevelChapters sets its sprite).
            Image chapterArt = GameUiStyle.Picture(canvas, "ChapterArt", new Vector2(0.5f, 0f), new Vector2(900f, 560f), new Vector2(0f, 300f), null, new Color(1f, 1f, 1f, 0.9f), changes);
            Image vignette = GameUiStyle.Picture(canvas, "Vignette", center, Vector2.zero, Vector2.zero, GameUiStyle.Title("TUI_Vignette"), Color.white, changes);
            SetAnchors((RectTransform)vignette.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, "Vignette");
            Image left = GameUiStyle.Picture(canvas, "Foliage_L", Vector2.zero, new Vector2(720f, 272f), new Vector2(320f, 120f), GameUiStyle.Title("TUI_FoliageR"), Color.white, changes);
            Image right = GameUiStyle.Picture(canvas, "Foliage_R", new Vector2(1f, 0f), new Vector2(720f, 272f), new Vector2(-320f, 120f), GameUiStyle.Title("TUI_FoliageR"), Color.white, changes);
            if (right.transform.localScale.x > 0f) { right.transform.localScale = new Vector3(-1f, 1f, 1f); changes.Add("mirrored Foliage_R"); }

            GameObject heading = EnsureChild(canvas, "Heading", changes);
            GameUiStyle.Place((RectTransform)heading.transform, new Vector2(0.5f, 1f), new Vector2(1000f, 100f), new Vector2(0f, -86f), changes, "Heading");
            var headingText = EnsureComponent<Text>(heading, changes, "Text", "Heading");
            ConfigureText(headingText, "Levels", 66, TextAnchor.MiddleCenter, GameUiStyle.Gold, changes, "Heading");
            GameUiStyle.Type(headingText, GameUiStyle.Display, changes, "Heading");
            GameUiStyle.Shade(heading, 0.65f, new Vector2(0f, -3f), changes, "Heading");
            Image rule = GameUiStyle.Flourish(canvas, new Vector2(0.5f, 1f), 620f, new Vector2(0f, -150f), changes);

            Transform chapterTabs = canvas.Find("Chapters");
            Transform[] order = chapterTabs != null
                ? new[] { back.transform, shade.transform, chapterArt.transform, vignette.transform, left.transform, right.transform, heading.transform, rule.transform, chapterTabs }
                : new[] { back.transform, shade.transform, chapterArt.transform, vignette.transform, left.transform, right.transform, heading.transform, rule.transform };
            for (int i = 0; i < order.Length; i++) KeepAt(order[i], i, changes);
        }

        static void KeepAt(Transform child, int index, List<string> changes)
        {
            if (child.GetSiblingIndex() == index) return;
            child.SetSiblingIndex(index);
            changes.Add($"moved {child.name} to {index}");
        }

        // Builder seam: PAX-A09: Content's grid of cells. It wraps to the screen's width, so a 20:9 phone fits a column more than 16:9, and Content's
        // ContentSizeFitter takes its height from however many lines that makes.
        public static GridLayoutGroup ConfigureContentLayout(GameObject contentGO, List<string> changes)
        {
            var oldList = contentGO.GetComponent<VerticalLayoutGroup>();
            if (oldList != null) { Object.DestroyImmediate(oldList); changes.Add("removed Content VerticalLayoutGroup"); }
            var layout = EnsureComponent<GridLayoutGroup>(contentGO, changes, "GridLayoutGroup", "Content");
            if (layout.cellSize != CellSize || layout.spacing != CellGap || layout.constraint != GridLayoutGroup.Constraint.Flexible || layout.childAlignment != TextAnchor.UpperCenter)
            {
                layout.cellSize = CellSize;
                layout.spacing = CellGap;
                layout.constraint = GridLayoutGroup.Constraint.Flexible;
                layout.childAlignment = TextAnchor.UpperCenter;
                changes.Add("configured Content GridLayoutGroup");
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
            if (rowRect.sizeDelta != CellSize) { rowRect.sizeDelta = CellSize; changes.Add($"sized {RowTemplateName}"); }
            var rowLayoutElement = EnsureComponent<LayoutElement>(rowTemplateGO, changes, "LayoutElement", RowTemplateName);
            if (!Mathf.Approximately(rowLayoutElement.preferredHeight, CellSize.y)) { rowLayoutElement.preferredHeight = CellSize.y; changes.Add($"set {RowTemplateName} LayoutElement.preferredHeight"); }

            var center = new Vector2(0.5f, 0.5f);
            // The glow goes first, so it stays under the cell's type: only the level that's next has one.
            Image glow = GameUiStyle.Picture(rowTemplateGO.transform, "Glow", center, CellSize * 1.5f, Vector2.zero, GameUiStyle.Title("TUI_Soft"), new Color(1f, 0.72f, 0.34f, 0.3f), changes);
            var rowImage = EnsureComponent<Image>(rowTemplateGO, changes, "Image", RowTemplateName);
            GameUiStyle.Paint(rowImage, GameUiStyle.Title("TUI_Plate"), Color.white, true, changes, RowTemplateName);
            GameUiStyle.Slice(rowImage, changes, RowTemplateName);
            if (!Mathf.Approximately(rowImage.pixelsPerUnitMultiplier, 1.6f)) { rowImage.pixelsPerUnitMultiplier = 1.6f; changes.Add($"set {RowTemplateName} corner size"); }
            var rowButton = GameUiStyle.Tap(rowTemplateGO, rowImage, changes);

            GameObject nameGO = EnsureChild(rowTemplateGO.transform, "Name", changes);
            SetAnchors((RectTransform)nameGO.transform, new Vector2(0f, 0.3f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, changes, "Name");
            var nameText = EnsureComponent<Text>(nameGO, changes, "Text", "Name");
            ConfigureText(nameText, "1", 50, TextAnchor.MiddleCenter, GameUiStyle.Gold, changes, "Name");
            GameUiStyle.Type(nameText, GameUiStyle.Body, changes, "Name");

            GameObject bestGO = EnsureChild(rowTemplateGO.transform, "Best", changes);
            SetAnchors((RectTransform)bestGO.transform, new Vector2(0f, 0f), new Vector2(1f, 0.42f), Vector2.zero, Vector2.zero, changes, "Best");
            var bestText = EnsureComponent<Text>(bestGO, changes, "Text", "Best");
            ConfigureText(bestText, "", 26, TextAnchor.MiddleCenter, GameUiStyle.Quiet, changes, "Best");
            GameUiStyle.Type(bestText, GameUiStyle.Italic, changes, "Best");

            Image lockIcon = GameUiStyle.Picture(rowTemplateGO.transform, "Lock", center, new Vector2(60f, 60f), Vector2.zero, GameUiStyle.Art("GUI_Lock"), Color.white, changes);
            Image doneMark = GameUiStyle.Picture(rowTemplateGO.transform, "Done", new Vector2(1f, 1f), new Vector2(26f, 26f), new Vector2(-26f, -24f), GameUiStyle.Art("GUI_Diamond"), GameUiStyle.Gold, changes);

            var row = EnsureComponent<LevelSelectRow>(rowTemplateGO, changes, "LevelSelectRow", RowTemplateName);
            WireField(row, "button", rowButton, changes, "LevelSelectRow.button = Button");
            WireField(row, "nameText", nameText, changes, "LevelSelectRow.nameText = Name");
            WireField(row, "bestDeathsText", bestText, changes, "LevelSelectRow.bestDeathsText = Best");
            WireField(row, "lockIcon", lockIcon, changes, "LevelSelectRow.lockIcon = Lock");
            WireField(row, "doneMark", doneMark, changes, "LevelSelectRow.doneMark = Done");
            WireField(row, "glow", glow, changes, "LevelSelectRow.glow = Glow");
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
