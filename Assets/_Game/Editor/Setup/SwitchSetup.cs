using System.Collections.Generic;
using System.Linq;
using Parallax.App;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class SwitchSetup
    {
        const string CatAName = "Cat_A";
        const string DeviceInputName = "DeviceInput";
        const string ObserversName = "Observers";
        const string HudCanvasName = "HUD";
        const string SwitchButtonName = "SwitchButton";

        [MenuItem("PARALLAX/Setup/Switch + Debug (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            GameObject observersGO = GameObject.Find(ObserversName);
            if (observersGO == null)
            {
                Debug.LogError($"SwitchSetup: no '{ObserversName}' GameObject found. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            var observerSet = observersGO.GetComponent<ObserverSet>();
            if (observerSet == null)
            {
                Debug.LogError($"SwitchSetup: '{ObserversName}' has no ObserverSet. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            GameObject deviceInputGO = GameObject.Find(DeviceInputName);
            if (deviceInputGO == null)
            {
                Debug.LogError($"SwitchSetup: no '{DeviceInputName}' GameObject found. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            var router = deviceInputGO.GetComponent<CatInputRouter>();
            if (router == null)
            {
                Debug.LogError($"SwitchSetup: '{DeviceInputName}' has no CatInputRouter. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            // Step 1: strip the leftover ObserverDriverDebugToggle / RealityViewDebugToggle
            // components. Their source is already deleted, so the scene holds them as
            // missing-script data by this point.
            RemoveMissingScripts(observersGO, changes);

            // Step 2: SoloSwitchController on Observers.
            var switchController = EnsureComponent<SoloSwitchController>(observersGO, changes, "SoloSwitchController", "Observers");
            WireField(switchController, "observers", observerSet, changes, "SoloSwitchController.observers = ObserverSet");
            WireField(switchController, "router", router, changes, "SoloSwitchController.router = CatInputRouter");

            var bootstrap = observersGO.GetComponent<ObserverBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError($"SwitchSetup: '{ObserversName}' has no ObserverBootstrap. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }
            WireField(bootstrap, "switchController", switchController, changes, "ObserverBootstrap.switchController = SoloSwitchController");

            // Step 3: EventSystem + InputSystemUIInputModule.
            EnsureEventSystem(changes);

            // Step 4: HUD canvas + SwitchButton.
            SwitchButton switchButton = EnsureHud(switchController, changes);

            // Step 5: KeyboardSwitchInput on DeviceInput.
            var keyboardSwitch = EnsureComponent<KeyboardSwitchInput>(deviceInputGO, changes, "KeyboardSwitchInput", DeviceInputName);
            WireField(keyboardSwitch, "switchController", switchController, changes, "KeyboardSwitchInput.switchController = SoloSwitchController");

            // Step 6: move GravityDebugControl from Cat_A to Observers.
            GravityDebugControl gravityDebug = MoveGravityDebugControl(observersGO, changes);
            WireField(gravityDebug, "observers", observerSet, changes, "GravityDebugControl.observers = ObserverSet");
            WireField(gravityDebug, "switchController", switchController, changes, "GravityDebugControl.switchController = SoloSwitchController");

            // Step 7: DebugPanel on Observers.
            var debugPanel = EnsureComponent<DebugPanel>(observersGO, changes, "DebugPanel", "Observers");
            WireField(debugPanel, "observers", observerSet, changes, "DebugPanel.observers = ObserverSet");
            WireField(debugPanel, "switchController", switchController, changes, "DebugPanel.switchController = SoloSwitchController");

            // Step 8: reserved regions on TouchStickCatInput.
            var touchInput = deviceInputGO.GetComponent<TouchStickCatInput>();
            if (touchInput == null)
            {
                Debug.LogError($"SwitchSetup: '{DeviceInputName}' has no TouchStickCatInput. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
            }
            else
            {
                SetReservedRegions(touchInput, switchButton, debugPanel, gravityDebug, changes);
            }

            if (changes.Count == 0)
            {
                Debug.Log("SwitchSetup: no changes.");
            }
            else
            {
                Debug.Log($"SwitchSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(observersGO.scene);
            }

            // Step 9: re-tint Cat_B and re-validate isolation.
            RealitySetup.Configure();
        }

        // ---- Step 1 ----

        static void RemoveMissingScripts(GameObject go, List<string> changes)
        {
            int before = CountMissingScripts(go);
            if (before == 0) return;

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            changes.Add($"removed {before} missing-script component(s) from {go.name} (superseded ObserverDriverDebugToggle/RealityViewDebugToggle)");
        }

        static int CountMissingScripts(GameObject go)
        {
            return go.GetComponents<Component>().Count(c => c == null);
        }

        // ---- Step 2 ----

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

        static void WireField(Object target, string propertyName, Object value, List<string> changes, string label)
        {
            if (target == null || value == null) return;

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"SwitchSetup: '{target.GetType().Name}' has no serialized field '{propertyName}'.");
                return;
            }

            if (prop.objectReferenceValue != value)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                changes.Add($"assigned {label}");
            }
        }

        // ---- Step 3 ----

        static void EnsureEventSystem(List<string> changes)
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            GameObject go;
            if (eventSystem == null)
            {
                go = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(go, "Setup Switch + Debug");
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

        // ---- Step 4 ----

        static SwitchButton EnsureHud(SoloSwitchController switchController, List<string> changes)
        {
            GameObject canvasGO = GameObject.Find(HudCanvasName);
            bool createdCanvas = canvasGO == null;
            if (createdCanvas)
            {
                canvasGO = new GameObject(HudCanvasName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Switch + Debug");
                changes.Add($"created {HudCanvasName} canvas");
            }

            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasGO.AddComponent<Canvas>();
                changes.Add($"added Canvas to {HudCanvasName}");
            }
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                changes.Add($"set {HudCanvasName}.renderMode = ScreenSpaceOverlay");
            }

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasGO.AddComponent<CanvasScaler>();
                changes.Add($"added CanvasScaler to {HudCanvasName}");
            }
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != new Vector2(1920f, 1080f)
                || !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                changes.Add($"configured {HudCanvasName} CanvasScaler (1920x1080, match 0.5)");
            }

            if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGO.AddComponent<GraphicRaycaster>();
                changes.Add($"added GraphicRaycaster to {HudCanvasName}");
            }

            Transform buttonT = canvasGO.transform.Find(SwitchButtonName);
            GameObject buttonGO;
            bool createdButton = buttonT == null;
            if (createdButton)
            {
                buttonGO = new GameObject(SwitchButtonName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(buttonGO, "Setup Switch + Debug");
                buttonGO.transform.SetParent(canvasGO.transform, false);
                changes.Add($"created {SwitchButtonName} under {HudCanvasName}");
            }
            else
            {
                buttonGO = buttonT.gameObject;
            }

            var rect = buttonGO.GetComponent<RectTransform>();
            Vector2 wantAnchor = new Vector2(0.5f, 1f);
            Vector2 wantSize = new Vector2(220f, 110f);
            Vector2 wantPos = new Vector2(0f, -40f);
            if (rect.anchorMin != wantAnchor || rect.anchorMax != wantAnchor || rect.pivot != wantAnchor
                || rect.sizeDelta != wantSize || rect.anchoredPosition != wantPos)
            {
                rect.anchorMin = wantAnchor;
                rect.anchorMax = wantAnchor;
                rect.pivot = wantAnchor;
                rect.sizeDelta = wantSize;
                rect.anchoredPosition = wantPos;
                changes.Add($"positioned {SwitchButtonName} (top-center, 220x110 @ reference, inset 40px from top)");
            }

            var image = buttonGO.GetComponent<Image>();
            if (image == null)
            {
                image = buttonGO.AddComponent<Image>();
                changes.Add($"added Image to {SwitchButtonName}");
            }

            var button = buttonGO.GetComponent<Button>();
            if (button == null)
            {
                button = buttonGO.AddComponent<Button>();
                changes.Add($"added Button to {SwitchButtonName}");
            }
            if (button.navigation.mode != Navigation.Mode.None)
            {
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                changes.Add($"set {SwitchButtonName} Button.navigation.mode = None");
            }

            Text label = buttonGO.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                var labelGO = new GameObject("Label", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelGO, "Setup Switch + Debug");
                labelGO.transform.SetParent(buttonGO.transform, false);

                label = labelGO.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 36;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.black;

                var labelRect = (RectTransform)labelGO.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                changes.Add($"created Label under {SwitchButtonName}");
            }

            var switchButton = EnsureComponent<SwitchButton>(buttonGO, changes, "SwitchButton", SwitchButtonName);
            WireField(switchButton, "switchController", switchController, changes, "SwitchButton.switchController = SoloSwitchController");
            WireField(switchButton, "button", button, changes, "SwitchButton.button = Button");
            WireField(switchButton, "label", label, changes, "SwitchButton.label = Label");

            return switchButton;
        }

        // ---- Step 6 ----

        static GravityDebugControl MoveGravityDebugControl(GameObject observersGO, List<string> changes)
        {
            var existingOnObservers = observersGO.GetComponent<GravityDebugControl>();
            if (existingOnObservers != null) return existingOnObservers;

            GameObject catAGO = GameObject.Find(CatAName);
            var onCatA = catAGO != null ? catAGO.GetComponent<GravityDebugControl>() : null;
            if (onCatA != null)
            {
                Object.DestroyImmediate(onCatA);
                changes.Add($"removed GravityDebugControl from {CatAName}");
            }

            var added = observersGO.AddComponent<GravityDebugControl>();
            changes.Add("added GravityDebugControl to Observers");
            return added;
        }

        // ---- Step 8 ----

        static void SetReservedRegions(TouchStickCatInput touchInput, SwitchButton switchButton, DebugPanel debugPanel, GravityDebugControl gravityDebug, List<string> changes)
        {
            var expected = new MonoBehaviour[] { switchButton, debugPanel, gravityDebug };

            var so = new SerializedObject(touchInput);
            var prop = so.FindProperty("reservedRegions");

            bool matches = prop.arraySize == expected.Length;
            if (matches)
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    if (prop.GetArrayElementAtIndex(i).objectReferenceValue != (Object)expected[i])
                    {
                        matches = false;
                        break;
                    }
                }
            }

            if (matches) return;

            prop.arraySize = expected.Length;
            for (int i = 0; i < expected.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = expected[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            changes.Add("set TouchStickCatInput.reservedRegions = [SwitchButton, DebugPanel, GravityDebugControl]");
        }
    }
}
