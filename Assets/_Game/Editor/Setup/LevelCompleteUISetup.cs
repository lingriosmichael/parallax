using System.Collections.Generic;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Input;
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
    /// menus. The first setup menu with a scene guard: it refuses to run outside Level_Solo01,
    /// since this screen is player-facing (not Sandbox_Realities/frozen-co-op tooling).</summary>
    public static class LevelCompleteUISetup
    {
        const string RequiredSceneName = "Level_Solo01";
        const string HudCanvasName = "HUD";
        const string DeviceInputName = "DeviceInput";
        const string PanelName = "LevelCompletePanel";
        const string TitleName = "Title";
        const string RowsContainerName = "Rows";
        const string RowTemplateName = "RowTemplate";
        const string TotalName = "Total";
        const string RestartButtonName = "RestartButton";
        const string ScreenControllerName = "LevelCompleteScreenController";

        [MenuItem("PARALLAX/Setup/Level Complete UI (PAX-049)")]
        public static void Configure()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (sceneName != RequiredSceneName)
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

            GameObject panelGO = EnsureChild(canvasGO.transform, PanelName, changes);
            SetAnchors((RectTransform)panelGO.transform, Vector2.zero, Vector2.one, new Vector2(80f, 80f), new Vector2(-80f, -80f), changes, PanelName);
            var panelImage = EnsureComponent<Image>(panelGO, changes, "Image", PanelName);
            SetColor(panelImage, new Color(0f, 0f, 0f, 0.85f), changes, $"{PanelName} Image");
            SetActive(panelGO, false, changes, PanelName); // hidden until LevelCompleteScreen shows it

            GameObject titleGO = EnsureChild(panelGO.transform, TitleName, changes);
            SetAnchors((RectTransform)titleGO.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -80f), new Vector2(-20f, -20f), changes, TitleName);
            var titleText = EnsureComponent<Text>(titleGO, changes, "Text", TitleName);
            ConfigureText(titleText, "Level complete", 48, TextAnchor.MiddleCenter, changes, TitleName);

            GameObject rowsGO = EnsureChild(panelGO.transform, RowsContainerName, changes);
            SetAnchors((RectTransform)rowsGO.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 120f), new Vector2(-40f, -140f), changes, RowsContainerName);
            var layout = EnsureComponent<VerticalLayoutGroup>(rowsGO, changes, "VerticalLayoutGroup", RowsContainerName);
            if (layout.spacing != 8f || !layout.childControlHeight || layout.childForceExpandHeight)
            {
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.UpperCenter;
                changes.Add($"configured {RowsContainerName} VerticalLayoutGroup");
            }

            GameObject rowTemplateGO = EnsureChild(rowsGO.transform, RowTemplateName, changes);
            var rowTemplateRect = (RectTransform)rowTemplateGO.transform;
            if (rowTemplateRect.sizeDelta != new Vector2(0f, 44f)) { rowTemplateRect.sizeDelta = new Vector2(0f, 44f); changes.Add($"sized {RowTemplateName}"); }
            var rowTemplateText = EnsureComponent<Text>(rowTemplateGO, changes, "Text", RowTemplateName);
            ConfigureText(rowTemplateText, "Room 0 — 0 deaths", 32, TextAnchor.MiddleCenter, changes, RowTemplateName);
            SetActive(rowTemplateGO, false, changes, RowTemplateName); // cloned per room at runtime, never shown itself

            GameObject totalGO = EnsureChild(panelGO.transform, TotalName, changes);
            SetAnchors((RectTransform)totalGO.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 70f), new Vector2(-20f, 120f), changes, TotalName);
            var totalText = EnsureComponent<Text>(totalGO, changes, "Text", TotalName);
            ConfigureText(totalText, "Total: 0 deaths", 36, TextAnchor.MiddleCenter, changes, TotalName);

            GameObject restartGO = EnsureChild(panelGO.transform, RestartButtonName, changes);
            SetAnchors((RectTransform)restartGO.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, 10f), new Vector2(110f, 65f), changes, RestartButtonName);
            var restartImage = EnsureComponent<Image>(restartGO, changes, "Image", RestartButtonName);
            SetColor(restartImage, Color.white, changes, $"{RestartButtonName} Image");
            var restartButtonComponent = EnsureComponent<Button>(restartGO, changes, "Button", RestartButtonName);
            if (restartButtonComponent.navigation.mode != Navigation.Mode.None)
            {
                var navigation = restartButtonComponent.navigation;
                navigation.mode = Navigation.Mode.None;
                restartButtonComponent.navigation = navigation;
                changes.Add($"set {RestartButtonName} Button.navigation.mode = None");
            }
            GameObject restartLabelGO = EnsureChild(restartGO.transform, "Label", changes);
            SetAnchors((RectTransform)restartLabelGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, "RestartButton/Label");
            var restartLabel = EnsureComponent<Text>(restartLabelGO, changes, "Text", "RestartButton/Label");
            ConfigureText(restartLabel, "Restart", 32, TextAnchor.MiddleCenter, changes, "RestartButton/Label");
            if (restartLabel.color != Color.black) { restartLabel.color = Color.black; changes.Add("set RestartButton/Label color = black"); }

            var restartButton = EnsureComponent<RestartButton>(restartGO, changes, "RestartButton", RestartButtonName);
            WireField(restartButton, "button", restartButtonComponent, changes, "RestartButton.button = Button");

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

        static void ConfigureText(Text text, string content, int fontSize, TextAnchor alignment, List<string> changes, string label)
        {
            bool changed = false;
            if (text.font == null) { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); changed = true; }
            if (text.text != content) { text.text = content; changed = true; }
            if (text.fontSize != fontSize) { text.fontSize = fontSize; changed = true; }
            if (text.alignment != alignment) { text.alignment = alignment; changed = true; }
            if (text.color != Color.white) { text.color = Color.white; changed = true; }
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

        static void AppendReservedRegion(TouchStickCatInput touchInput, RestartButton restartButton, List<string> changes)
        {
            var so = new SerializedObject(touchInput);
            var prop = so.FindProperty("reservedRegions");

            for (int i = 0; i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == (Object)restartButton) return; // already present
            }

            int index = prop.arraySize;
            prop.arraySize++;
            prop.GetArrayElementAtIndex(index).objectReferenceValue = restartButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("appended RestartButton to TouchStickCatInput.reservedRegions");
        }
    }
}
