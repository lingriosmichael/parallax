using System.Collections.Generic;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-054 (D-073). Idempotent find-or-create + rewire, run on _LevelTemplate only
    /// (D-070); Rebuild All Levels then carries it into every Level_NNN (regeneration copies the
    /// template's bytes, so every in-scene reference below travels with it). Builds the top-right
    /// pause button and the full-screen pause panel (Resume, Restart, Levels) under HUD, a root
    /// LevelPause, and wires: LevelPause (rooms, router, panel, EventSystem), ObserverSet's pause
    /// gate, LevelCompleteScreen's pause-button hook, and both pause UI regions into
    /// TouchStickCatInput.reservedRegions. Every pause button uses Navigation None, as on the
    /// complete screen, so a click never selects it. Doesn't touch LevelCompleteUISetup.</summary>
    public static class PauseMenuSetup
    {
        const string RequiredSceneName = "_LevelTemplate";
        const string HudCanvasName = "HUD";
        const string DeviceInputName = "DeviceInput";
        const string LevelPauseName = "LevelPause";
        const string PauseButtonName = "PauseButton";
        const string PanelName = "PausePanel";
        const string TitleName = "Title";
        const string ResumeButtonName = "PauseResumeButton";
        const string RestartButtonName = "PauseRestartButton";
        const string LevelsButtonName = "PauseLevelsButton";

        // HUD reference resolution is 1920x1080, match 0.5 (SwitchSetup). The dev-only
        // GravityDebugControl FLIP button draws 16-96 px from the top-right in screen pixels, so
        // the 120-unit inset clears it only while the HUD scale factor is at least 0.8 (screens
        // roughly 900 px tall or more, e.g. the 2400x1080 target). On smaller screens the two can
        // overlap - debug builds only, since FLIP doesn't exist in release.
        static readonly Vector2 PauseButtonSize = new Vector2(110f, 110f);
        static readonly Vector2 PauseButtonInset = new Vector2(-16f, -120f);

        public static bool RefusesToRun(string activeSceneName) => activeSceneName != RequiredSceneName;

        [MenuItem("PARALLAX/Setup/Levels/Pause Menu")]
        public static void Configure()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (RefusesToRun(sceneName))
            {
                Debug.LogError($"PauseMenuSetup: refuses to run outside '{RequiredSceneName}' (active scene is '{sceneName}').");
                return;
            }

            var rooms = Object.FindAnyObjectByType<RoomManager>();
            var observers = Object.FindAnyObjectByType<ObserverSet>();
            var router = Object.FindAnyObjectByType<CatInputRouter>();
            var screen = Object.FindAnyObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (rooms == null || observers == null || router == null || screen == null || eventSystem == null)
            {
                Debug.LogError("PauseMenuSetup: scene is missing RoomManager, ObserverSet, CatInputRouter, LevelCompleteScreen or EventSystem. Run the HUD and Level Complete UI setup first.");
                return;
            }

            GameObject canvasGO = GameObject.Find(HudCanvasName);
            if (canvasGO == null)
            {
                Debug.LogError($"PauseMenuSetup: no '{HudCanvasName}' canvas found. Run the switch/HUD setup first.");
                return;
            }

            GameObject deviceInputGO = GameObject.Find(DeviceInputName);
            var touchInput = deviceInputGO != null ? deviceInputGO.GetComponent<TouchStickCatInput>() : null;
            if (touchInput == null)
            {
                Debug.LogError($"PauseMenuSetup: no '{DeviceInputName}' GameObject with TouchStickCatInput found; nothing was changed.");
                return;
            }

            var changes = new List<string>();
            BuildPauseUI(canvasGO.transform, changes);
            var pauseButton = canvasGO.transform.Find(PauseButtonName).GetComponent<PauseButton>();
            var pausePanel = canvasGO.transform.Find(PanelName).GetComponent<PausePanel>();

            GameObject levelPauseGO = SetupUtility.EnsureSceneRoot(LevelPauseName, changes);
            var levelPause = EnsureComponent<LevelPause>(levelPauseGO, changes, LevelPauseName);

            Wire(levelPause, "rooms", rooms, changes);
            Wire(levelPause, "router", router, changes);
            Wire(levelPause, "panel", pausePanel.gameObject, changes);
            Wire(levelPause, "eventSystem", eventSystem, changes);
            Wire(pauseButton, "levelPause", levelPause, changes);
            Wire(pausePanel, "levelPause", levelPause, changes);
            Wire(observers, "pauseGate", levelPause, changes);
            Wire(screen, "pauseButton", pauseButton.gameObject, changes);

            AppendReservedRegion(touchInput, pauseButton, changes);
            AppendReservedRegion(touchInput, pausePanel, changes);

            if (changes.Count == 0)
            {
                Debug.Log("PauseMenuSetup: no changes.");
            }
            else
            {
                Debug.Log($"PauseMenuSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            }
        }

        /// <summary>Builds (or re-checks) the pause button and pause panel under `hud` and wires
        /// the buttons into PauseButton/PausePanel. No scene lookups, so tests can call it on a
        /// throwaway parent. Idempotent: a second call reports no changes.</summary>
        public static void BuildPauseUI(Transform hud, List<string> changes)
        {
            GameObject pauseGO = EnsureChild(hud, PauseButtonName, changes);
            var pauseRect = (RectTransform)pauseGO.transform;
            if (pauseRect.anchorMin != Vector2.one || pauseRect.anchorMax != Vector2.one || pauseRect.pivot != Vector2.one
                || pauseRect.sizeDelta != PauseButtonSize || pauseRect.anchoredPosition != PauseButtonInset)
            {
                pauseRect.anchorMin = Vector2.one;
                pauseRect.anchorMax = Vector2.one;
                pauseRect.pivot = Vector2.one;
                pauseRect.sizeDelta = PauseButtonSize;
                pauseRect.anchoredPosition = PauseButtonInset;
                changes.Add($"positioned {PauseButtonName} (top-right, below the dev FLIP button)");
            }
            Button pauseButton = EnsureButton(pauseGO, "II", changes);
            var pauseComponent = EnsureComponent<PauseButton>(pauseGO, changes, PauseButtonName);
            Wire(pauseComponent, "button", pauseButton, changes);

            GameObject panelGO = EnsureChild(hud, PanelName, changes);
            SetAnchors((RectTransform)panelGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, PanelName);
            var panelImage = EnsureComponent<Image>(panelGO, changes, PanelName);
            if (panelImage.color != new Color(0f, 0f, 0f, 0.85f)) { panelImage.color = new Color(0f, 0f, 0f, 0.85f); changes.Add($"set {PanelName} Image.color"); }
            var panelComponent = EnsureComponent<PausePanel>(panelGO, changes, PanelName);
            // Drawn over (and blocking) every other HUD control while shown.
            if (panelGO.transform.GetSiblingIndex() != hud.childCount - 1)
            {
                panelGO.transform.SetAsLastSibling();
                changes.Add($"moved {PanelName} to the top of {hud.name}");
            }

            GameObject titleGO = EnsureChild(panelGO.transform, TitleName, changes);
            SetAnchors((RectTransform)titleGO.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, 80f), new Vector2(-20f, 160f), changes, PanelName + "/" + TitleName);
            ConfigureText(EnsureComponent<Text>(titleGO, changes, TitleName), "Paused", 56, Color.white, changes, PanelName + "/" + TitleName);

            Button resume = EnsurePanelButton(panelGO.transform, ResumeButtonName, "Resume", -360f, changes);
            Button restart = EnsurePanelButton(panelGO.transform, RestartButtonName, "Restart", -110f, changes);
            Button levels = EnsurePanelButton(panelGO.transform, LevelsButtonName, "Levels", 140f, changes);
            Wire(panelComponent, "resumeButton", resume, changes);
            Wire(panelComponent, "restartButton", restart, changes);
            Wire(panelComponent, "levelsButton", levels, changes);

            if (panelGO.activeSelf) { panelGO.SetActive(false); changes.Add($"set {PanelName} active = False"); } // shown by LevelPause
        }

        static Button EnsurePanelButton(Transform panel, string name, string label, float left, List<string> changes)
        {
            GameObject go = EnsureChild(panel, name, changes);
            var anchor = new Vector2(0.5f, 0.5f);
            SetAnchors((RectTransform)go.transform, anchor, anchor, new Vector2(left, -40f), new Vector2(left + 220f, 40f), changes, name);
            return EnsureButton(go, label, changes);
        }

        static Button EnsureButton(GameObject go, string label, List<string> changes)
        {
            var image = EnsureComponent<Image>(go, changes, go.name);
            if (image.color != Color.white) { image.color = Color.white; changes.Add($"set {go.name} Image.color"); }
            var button = EnsureComponent<Button>(go, changes, go.name);
            if (button.navigation.mode != Navigation.Mode.None)
            {
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                changes.Add($"set {go.name} Button.navigation.mode = None");
            }

            GameObject labelGO = EnsureChild(go.transform, "Label", changes);
            SetAnchors((RectTransform)labelGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, go.name + "/Label");
            ConfigureText(EnsureComponent<Text>(labelGO, changes, go.name + "/Label"), label, 36, Color.black, changes, go.name + "/Label");
            return button;
        }

        static GameObject EnsureChild(Transform parent, string name, List<string> changes)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Setup Pause Menu");
            go.transform.SetParent(parent, false);
            changes.Add($"created {name} under {parent.name}");
            return go;
        }

        static T EnsureComponent<T>(GameObject go, List<string> changes, string ownerLabel) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null) return component;
            component = go.AddComponent<T>();
            changes.Add($"added {typeof(T).Name} to {ownerLabel}");
            return component;
        }

        static void ConfigureText(Text text, string content, int fontSize, Color color, List<string> changes, string label)
        {
            bool changed = false;
            if (text.font == null) { text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); changed = true; }
            if (text.text != content) { text.text = content; changed = true; }
            if (text.fontSize != fontSize) { text.fontSize = fontSize; changed = true; }
            if (text.alignment != TextAnchor.MiddleCenter) { text.alignment = TextAnchor.MiddleCenter; changed = true; }
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

        static void Wire(Object target, string propertyName, Object value, List<string> changes)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"PauseMenuSetup: '{target.GetType().Name}' has no serialized field '{propertyName}'.");
                return;
            }
            if (value == null)
            {
                Debug.LogError($"PauseMenuSetup: nothing to wire into '{target.GetType().Name}.{propertyName}'.");
                return;
            }
            if (prop.objectReferenceValue == value) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"wired {target.GetType().Name}.{propertyName} = {value.name}");
        }

        static void AppendReservedRegion(TouchStickCatInput touchInput, Component region, List<string> changes)
        {
            var so = new SerializedObject(touchInput);
            SerializedProperty prop = so.FindProperty("reservedRegions");
            if (prop == null)
            {
                Debug.LogError("PauseMenuSetup: TouchStickCatInput has no serialized field 'reservedRegions'.");
                return;
            }
            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == (Object)region) return;

            int index = prop.arraySize;
            prop.arraySize++;
            prop.GetArrayElementAtIndex(index).objectReferenceValue = region;
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add($"appended {region.GetType().Name} to TouchStickCatInput.reservedRegions");
        }
    }
}
