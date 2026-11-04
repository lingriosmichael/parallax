using System.Collections.Generic;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-049 (D-061). Idempotent find-or-create + rewire, like the other PARALLAX/Setup
    /// menus. PAX-050 (D-063): also creates/wires the Next level button and LevelListConfig
    /// reference. Run PARALLAX/Setup/Level List (PAX-050) first, or this logs an error and skips
    /// that part. PAX-053 (D-070) §2.4.1: retargeted to _LevelTemplate only - Level_Solo01 keeps
    /// its existing UI and is never touched by this menu again. Also creates/wires the Levels
    /// button (goes to LevelSelect, shown on every level, including the last).</summary>
    public static class LevelCompleteUISetup
    {
        const string RequiredSceneName = "_LevelTemplate";
        const string HudCanvasName = "HUD";
        const string DeviceInputName = "DeviceInput";
        const string PanelName = "LevelCompletePanel";
        const string TitleName = "Title";
        const string RowsContainerName = "Rows";
        const string RowTemplateName = "RowTemplate";
        const string TotalName = "Total";
        const string RestartButtonName = "RestartButton";
        const string NextLevelButtonName = "NextLevelButton";
        const string LevelsButtonName = "LevelsButton";
        const string ScreenControllerName = "LevelCompleteScreenController";
        const string LevelListConfigPath = "Assets/_Game/Data/LevelListConfig.asset";

        public static bool RefusesToRun(string activeSceneName) => activeSceneName != RequiredSceneName;

        [MenuItem("PARALLAX/Setup/Level Complete UI (PAX-049)")]
        public static void Configure()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (RefusesToRun(sceneName))
            {
                Debug.LogError($"LevelCompleteUISetup: refuses to run outside '{RequiredSceneName}' (active scene is '{sceneName}').");
                return;
            }

            var changes = new List<string>();

            var rooms = Object.FindAnyObjectByType<RoomManager>();
            var death = Object.FindAnyObjectByType<RoomDeath>();
            var checkpoints = Object.FindAnyObjectByType<CheckpointManager>();
            var observers = Object.FindAnyObjectByType<ObserverSet>();
            if (rooms == null || death == null || checkpoints == null || observers == null)
            {
                Debug.LogError("LevelCompleteUISetup: scene is missing RoomManager, RoomDeath, CheckpointManager or ObserverSet.");
                return;
            }

            GameObject canvasGO = GameObject.Find(HudCanvasName);
            if (canvasGO == null)
            {
                Debug.LogError($"LevelCompleteUISetup: no '{HudCanvasName}' canvas found. Run the switch/HUD setup first.");
                return;
            }

            var center = new Vector2(0.5f, 0.5f);
            GameObject panelGO = EnsureChild(canvasGO.transform, PanelName, changes);
            SetAnchors((RectTransform)panelGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, PanelName);
            var panelImage = EnsureComponent<Image>(panelGO, changes, "Image", PanelName);
            SetColor(panelImage, GameUiStyle.Dim, changes, $"{PanelName} Image");
            SetActive(panelGO, false, changes, PanelName); // hidden until LevelCompleteScreen shows it
            GameUiStyle.Plate(panelGO.transform, new Vector2(1240f, 620f), changes);

            GameObject titleGO = EnsureChild(panelGO.transform, TitleName, changes);
            GameUiStyle.Place((RectTransform)titleGO.transform, center, new Vector2(1120f, 100f), new Vector2(0f, 215f), changes, TitleName);
            var titleText = EnsureComponent<Text>(titleGO, changes, "Text", TitleName);
            ConfigureText(titleText, "Level complete", 64, TextAnchor.MiddleCenter, GameUiStyle.Gold, changes, TitleName);
            GameUiStyle.Type(titleText, GameUiStyle.Display, changes, TitleName);
            GameUiStyle.Flourish(panelGO.transform, center, 760f, new Vector2(0f, 150f), changes);
            ResultsCatOnRim(panelGO.transform, changes);

            GameObject rowsGO = EnsureChild(panelGO.transform, RowsContainerName, changes);
            // Every level has one room today, so this fits one line. A level with more would need a taller plate.
            GameUiStyle.Place((RectTransform)rowsGO.transform, center, new Vector2(1000f, 60f), new Vector2(0f, 80f), changes, RowsContainerName);
            var layout = EnsureComponent<VerticalLayoutGroup>(rowsGO, changes, "VerticalLayoutGroup", RowsContainerName);
            // Width too: a row is given no width of its own, and its text would wrap one letter to a line.
            if (layout.spacing != 8f || !layout.childControlHeight || layout.childForceExpandHeight || !layout.childControlWidth || !layout.childForceExpandWidth)
            {
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childAlignment = TextAnchor.UpperCenter;
                changes.Add($"configured {RowsContainerName} VerticalLayoutGroup");
            }

            GameObject rowTemplateGO = EnsureChild(rowsGO.transform, RowTemplateName, changes);
            var rowTemplateRect = (RectTransform)rowTemplateGO.transform;
            if (rowTemplateRect.sizeDelta != new Vector2(0f, 44f)) { rowTemplateRect.sizeDelta = new Vector2(0f, 44f); changes.Add($"sized {RowTemplateName}"); }
            var rowTemplateText = EnsureComponent<Text>(rowTemplateGO, changes, "Text", RowTemplateName);
            ConfigureText(rowTemplateText, "Room 0 — 0 deaths", 34, TextAnchor.MiddleCenter, GameUiStyle.Quiet, changes, RowTemplateName);
            GameUiStyle.Type(rowTemplateText, GameUiStyle.Body, changes, RowTemplateName);
            SetActive(rowTemplateGO, false, changes, RowTemplateName); // cloned per room at runtime, never shown itself

            GameObject totalGO = EnsureChild(panelGO.transform, TotalName, changes);
            GameUiStyle.Place((RectTransform)totalGO.transform, center, new Vector2(1000f, 70f), new Vector2(0f, 15f), changes, TotalName);
            var totalText = EnsureComponent<Text>(totalGO, changes, "Text", TotalName);
            ConfigureText(totalText, "Total: 0 deaths", 46, TextAnchor.MiddleCenter, GameUiStyle.Ink, changes, TotalName);
            GameUiStyle.Type(totalText, GameUiStyle.Italic, changes, TotalName);

            Button restartButtonComponent = EnsureFramedButton(panelGO.transform, RestartButtonName, "Restart", -390f, false, changes);
            var restartButton = EnsureComponent<RestartButton>(restartButtonComponent.gameObject, changes, "RestartButton", RestartButtonName);
            WireField(restartButton, "button", restartButtonComponent, changes, "RestartButton.button = Button");

            Button nextLevelButtonComponent = EnsureFramedButton(panelGO.transform, NextLevelButtonName, "Next level", 0f, true, changes);
            GameObject nextLevelGO = nextLevelButtonComponent.gameObject;
            var nextLevelButton = EnsureComponent<NextLevelButton>(nextLevelGO, changes, "NextLevelButton", NextLevelButtonName);
            WireField(nextLevelButton, "button", nextLevelButtonComponent, changes, "NextLevelButton.button = Button");
            SetActive(nextLevelGO, false, changes, NextLevelButtonName); // shown only when LevelCompleteScreen finds a next level

            Button levelsButtonComponent = EnsureFramedButton(panelGO.transform, LevelsButtonName, "Levels", 390f, false, changes);
            var levelsButton = EnsureComponent<LevelsButton>(levelsButtonComponent.gameObject, changes, "LevelsButton", LevelsButtonName);
            WireField(levelsButton, "button", levelsButtonComponent, changes, "LevelsButton.button = Button");
            // Shown on every level (including the last, which has no Next level button), so -
            // unlike NextLevelButton - it starts active, matching RestartButton's own pattern.

            LevelListConfig levelList = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListConfigPath);
            if (levelList == null)
            {
                Debug.LogError($"LevelCompleteUISetup: no LevelListConfig at '{LevelListConfigPath}'. Run PARALLAX/Setup/Level List (PAX-050) first.");
            }

            GameObject screenGO = GameObject.Find(ScreenControllerName);
            if (screenGO == null)
            {
                screenGO = new GameObject(ScreenControllerName);
                Undo.RegisterCreatedObjectUndo(screenGO, "Setup Level Complete UI");
                changes.Add($"created {ScreenControllerName}");
            }
            var screen = EnsureComponent<LevelCompleteScreen>(screenGO, changes, "LevelCompleteScreen", ScreenControllerName);
            WireField(screen, "rooms", rooms, changes, "LevelCompleteScreen.rooms = RoomManager");
            WireField(screen, "roomDeath", death, changes, "LevelCompleteScreen.roomDeath = RoomDeath");
            WireField(screen, "checkpoints", checkpoints, changes, "LevelCompleteScreen.checkpoints = CheckpointManager");
            WireField(screen, "observers", observers, changes, "LevelCompleteScreen.observers = ObserverSet");
            WireField(screen, "panel", panelGO, changes, "LevelCompleteScreen.panel = LevelCompletePanel");
            WireField(screen, "rowsContainer", (RectTransform)rowsGO.transform, changes, "LevelCompleteScreen.rowsContainer = Rows");
            WireField(screen, "rowTemplate", rowTemplateText, changes, "LevelCompleteScreen.rowTemplate = RowTemplate");
            WireField(screen, "totalText", totalText, changes, "LevelCompleteScreen.totalText = Total");
            WireField(screen, "restartButton", restartButton, changes, "LevelCompleteScreen.restartButton = RestartButton");
            WireField(screen, "nextLevelButton", nextLevelButton, changes, "LevelCompleteScreen.nextLevelButton = NextLevelButton");
            WireField(screen, "levelsButton", levelsButton, changes, "LevelCompleteScreen.levelsButton = LevelsButton");
            if (levelList != null) WireField(screen, "levelList", levelList, changes, "LevelCompleteScreen.levelList = LevelListConfig");

            // Reserved regions: append RestartButton, don't replace. TouchStickCatInput lives on
            // DeviceInput. Re-running SwitchSetup on this scene would still overwrite this array
            // (it hard-replaces with exactly [SwitchButton, DebugPanel, GravityDebugControl]) --
            // known limitation, reported in the PAX-049 review notes; SwitchSetup is untouched.
            GameObject deviceInputGO = GameObject.Find(DeviceInputName);
            var touchInput = deviceInputGO != null ? deviceInputGO.GetComponent<TouchStickCatInput>() : null;
            if (touchInput == null)
            {
                Debug.LogError($"LevelCompleteUISetup: no '{DeviceInputName}' GameObject with TouchStickCatInput found; RestartButton was not registered as a reserved region.");
            }
            else
            {
                AppendReservedRegion(touchInput, restartButton, changes);
                AppendReservedRegion(touchInput, nextLevelButton, changes);
                AppendReservedRegion(touchInput, levelsButton, changes);
            }

            if (changes.Count == 0)
            {
                Debug.Log("LevelCompleteUISetup: no changes.");
            }
            else
            {
                Debug.Log($"LevelCompleteUISetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            }
        }

        // PAX-A09: the three buttons along the plate's foot, Next level the strong one in the middle.
        static Button EnsureFramedButton(Transform panel, string name, string label, float x, bool primary, List<string> changes)
        {
            GameObject go = EnsureChild(panel, name, changes);
            GameUiStyle.Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), primary ? new Vector2(380f, 112f) : new Vector2(340f, 92f), new Vector2(x, -165f), changes, name);
            return GameUiStyle.Framed(go, label, primary ? 38 : 28, primary, changes);
        }

        // D-314: the results beat: the cat sitting on the plate's top rim, left of the title, over a soft warm halo (black on the
        // dimmed level it would vanish). Its paws sit 6 % up its cell, so the cell's foot is set that far under the rim.
        const float PlateTop = 310f, CatCell = 230f, CatX = -470f;
        static void ResultsCatOnRim(Transform panel, List<string> changes)
        {
            var center = new Vector2(0.5f, 0.5f);
            Sprite[] idle = CatFrames("CatA_Idle"), look = CatFrames("CatA_IdleLook");
            if (idle.Length == 0) { Debug.LogError("LevelCompleteUISetup: no CatA_Idle frames for the results cat; run PARALLAX/Setup/Cat Visual."); return; }
            // The halo is made first, so it draws behind the cat.
            GameUiStyle.Picture(panel, "CatHalo", center, new Vector2(380f, 300f), new Vector2(CatX, PlateTop + 70f),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/RealityA/Environment/Kit/ENV_Glow.png"), new Color(1f, 0.82f, 0.5f, 0.55f), changes);
            Image cat = GameUiStyle.Picture(panel, "Cat", center, new Vector2(CatCell, CatCell), new Vector2(CatX, PlateTop + CatCell * 0.5f - CatCell * 0.06f), idle[0], Color.white, changes);
            if (!cat.preserveAspect) { cat.preserveAspect = true; changes.Add("set Cat preserveAspect"); }
            ResultsCat flipbook = EnsureComponent<ResultsCat>(cat.gameObject, changes, "ResultsCat", "Cat");
            SetupUtility.SetArray(flipbook, "idle", idle, changes);
            SetupUtility.SetArray(flipbook, "look", look, changes);
        }

        static Sprite[] CatFrames(string sheet) =>
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.OfType<Sprite>(AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Cats/CatA/" + sheet + ".png")),
                s => int.TryParse(System.Text.RegularExpressions.Regex.Match(s.name, @"(\d+)$").Value, out int i) ? i : 0));

        static GameObject EnsureChild(Transform parent, string name, List<string> changes)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Setup Level Complete UI");
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

        // PAX-050 review fix: the color parameter used to be hardcoded to white here, while
        // RestartButton/Label and NextLevelButton/Label separately forced themselves to black
        // right after calling this -- so every re-run of the menu flipped each label white then
        // black again and never converged to "no changes". Taking the desired color as a
        // parameter removes that fight.
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
                Debug.LogError($"LevelCompleteUISetup: '{target.GetType().Name}' has no serialized field '{propertyName}'.");
                return;
            }
            if (prop.objectReferenceValue != value)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                changes.Add($"assigned {label}");
            }
        }

        static void AppendReservedRegion<T>(TouchStickCatInput touchInput, T region, List<string> changes) where T : Component
        {
            var so = new SerializedObject(touchInput);
            var prop = so.FindProperty("reservedRegions");

            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == (Object)region) return; // already present
            }

            int index = prop.arraySize;
            prop.arraySize++;
            prop.GetArrayElementAtIndex(index).objectReferenceValue = region;
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"appended {typeof(T).Name} to TouchStickCatInput.reservedRegions");
        }
    }
}
