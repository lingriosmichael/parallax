using System.Collections.Generic;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class ControlStationSetup
    {
        [MenuItem("PARALLAX/Setup/Control Station (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject aObject = GameObject.Find("RealityRoot_A");
            GameObject observersObject = GameObject.Find("Observers");
            GameObject hud = GameObject.Find("HUD");
            RealityRoot root = aObject == null ? null : aObject.GetComponent<RealityRoot>();
            ObserverSet observers = observersObject == null ? null : observersObject.GetComponent<ObserverSet>();
            TransportHost host = Object.FindAnyObjectByType<LocalTransportHost>(FindObjectsInactive.Include);
            if (root == null || observers == null || host == null || hud == null) { Debug.LogError("ControlStationSetup: RealityRoot_A, Observers, HUD, and LocalTransportHost are required."); return; }
            float groundTop = GroundTop(root);
            float spawnX = SpawnX(root);
            float stationX = spawnX + 6f;
            BoxCollider2D ground = root.transform.Find("Geometry/Ground").GetComponent<BoxCollider2D>();
            float right = root.ToLocal((Vector2)ground.bounds.max).x;
            if (stationX + 0.6f > right) { stationX = right - 0.6f; Debug.Log($"ControlStationSetup: shifted station left to {stationX:F2}."); }
            DialGravityInput dial = BuildDial(hud, changes);
            BuildStation(root, observers, dial, stationX, groundTop, changes);
            WireCats(host, changes);
            WireDebug(observers, root, changes);
            if (changes.Count == 0) Debug.Log("ControlStationSetup: no changes.");
            else { Debug.Log("ControlStationSetup: station=(" + stationX.ToString("F2") + "," + (groundTop + 0.8f).ToString("F2") + "); dial=(right-middle,-220,0); " + string.Join("; ", changes)); EditorSceneManager.MarkSceneDirty(root.gameObject.scene); }
            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
            ControlValidator.Validate();
        }

        static ControlStation BuildStation(RealityRoot root, ObserverSet observers, DialGravityInput dial, float x, float groundTop, List<string> changes)
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(root.Id));
            Transform folder = SetupUtility.EnsureChild(root.transform, "Interactables", layer, changes);
            GameObject station = SetupUtility.EnsureChild(folder, "Station_A", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(station.transform, new Vector2(x, groundTop + 0.8f), changes);
            BoxCollider2D collider = SetupUtility.Ensure<BoxCollider2D>(station, changes);
            if (!collider.isTrigger) { collider.isTrigger = true; changes.Add("set Station_A trigger"); }
            SetupUtility.SetColliderSize(collider, new Vector2(1.2f, 1.6f), changes);
            GameObject baseObject = SetupUtility.EnsureChild(station.transform, "Base", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(baseObject.transform, new Vector2(0f, -0.4f), changes);
            SetupUtility.SetVisual(baseObject, root, new Vector2(1f, 0.8f), Color.white, changes);
            GameObject glowObject = SetupUtility.EnsureChild(station.transform, "Glow", layer, changes).gameObject;
            SetupUtility.SetLocalPosition(glowObject.transform, new Vector2(0f, 0.1f), changes);
            SpriteRenderer glow = SetupUtility.SetVisual(glowObject, root, new Vector2(1.2f, 0.2f), new Color(1f, 1f, 1f, 0.3f), changes);
            GameObject pulseObject = SetupUtility.EnsureChild(station.transform, "Pulse", layer, changes).gameObject;
            SpriteRenderer pulse = SetupUtility.SetVisual(pulseObject, root, new Vector2(0.3f, 0.3f), new Color(1f, 1f, 1f, 0f), changes);
            ControlStation control = SetupUtility.Ensure<ControlStation>(station, changes);
            SetupUtility.SetObject(control, "observers", observers, changes);
            SetupUtility.SetObject(control, "inputSource", dial, changes);
            SetupUtility.SetObject(control, "dial", dial, changes);
            SetupUtility.SetObject(control, "glow", glow, changes);
            SetupUtility.SetObject(control, "transitPulse", pulse, changes);
            return control;
        }

        static DialGravityInput BuildDial(GameObject hud, List<string> changes)
        {
            GameObject dialObject = SetupUtility.EnsureChild(hud.transform, "GravityDial", hud.layer, changes).gameObject;
            RectTransform rect = dialObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f); rect.anchorMax = new Vector2(1f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = new Vector2(-220f, 0f); rect.sizeDelta = new Vector2(260f, 260f);
            Image rootImage = dialObject.GetComponent<Image>();
            if (rootImage != null) { Object.DestroyImmediate(rootImage); changes.Add("removed GravityDial root Image"); }
            GameObject visual = SetupUtility.EnsureChild(dialObject.transform, "Visual", hud.layer, changes).gameObject;
            GameObject backgroundObject = SetupUtility.EnsureChild(visual.transform, "Background", hud.layer, changes).gameObject;
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            SetRect(backgroundRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, changes);
            Image image = SetupUtility.Ensure<Image>(backgroundObject, changes);
            SetImage(image, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), new Color(0f, 0f, 0f, 0.55f), true, changes);
            GameObject knobObject = SetupUtility.EnsureChild(visual.transform, "Knob", hud.layer, changes).gameObject;
            RectTransform knob = knobObject.GetComponent<RectTransform>();
            knob.anchorMin = new Vector2(0.5f, 0.5f); knob.anchorMax = new Vector2(0.5f, 0.5f); knob.pivot = new Vector2(0.5f, 0f); knob.anchoredPosition = Vector2.zero; knob.sizeDelta = new Vector2(24f, 110f);
            Image knobImage = SetupUtility.Ensure<Image>(knobObject, changes);
            SetImage(knobImage, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), Color.white, false, changes);
            DialGravityInput dial = SetupUtility.Ensure<DialGravityInput>(dialObject, changes);
            SetupUtility.SetObject(dial, "knob", knob, changes); SetupUtility.SetObject(dial, "visualRoot", visual, changes);
            if (visual.activeSelf) { visual.SetActive(false); changes.Add("hid GravityDial Visual"); }
            return dial;
        }

        static void WireCats(TransportHost host, List<string> changes)
        {
            foreach (CatMotor2D cat in Object.FindObjectsByType<CatMotor2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                CatSeat seat = SetupUtility.Ensure<CatSeat>(cat.gameObject, changes);
                GravityControlReceiver receiver = SetupUtility.Ensure<GravityControlReceiver>(cat.gameObject, changes);
                SetupUtility.SetObject(receiver, "transportHost", host, changes);
            }
            TouchStickCatInput touch = Object.FindAnyObjectByType<TouchStickCatInput>(FindObjectsInactive.Include);
            DialGravityInput dial = Object.FindAnyObjectByType<DialGravityInput>(FindObjectsInactive.Include);
            if (touch != null && dial != null) AppendReserved(touch, dial, changes);
        }

        static void WireDebug(ObserverSet observers, RealityRoot root, List<string> changes)
        {
            DebugPanel panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (panel == null) return;
            CatMotor2D[] cats = Object.FindObjectsByType<CatMotor2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CatMotor2D cat in cats)
            {
                RealityRoot catRoot = cat.GetComponentInParent<RealityRoot>();
                if (catRoot == null) continue;
                string suffix = catRoot.Id == ObserverId.A ? "A" : "B";
                SetupUtility.SetObject(panel, "seat" + suffix, cat.GetComponent<CatSeat>(), changes);
                SetupUtility.SetObject(panel, "receiver" + suffix, cat.GetComponent<GravityControlReceiver>(), changes);
            }
        }

        static void AppendReserved(TouchStickCatInput touch, DialGravityInput dial, List<string> changes)
        {
            var serialized = new SerializedObject(touch); SerializedProperty array = serialized.FindProperty("reservedRegions");
            for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == dial) return;
            int index = array.arraySize; array.arraySize++; array.GetArrayElementAtIndex(index).objectReferenceValue = dial; serialized.ApplyModifiedPropertiesWithoutUndo(); changes.Add("added GravityDial to reservedRegions");
        }

        static float GroundTop(RealityRoot root) => root.ToLocal((Vector2)root.transform.Find("Geometry/Ground").GetComponent<BoxCollider2D>().bounds.max).y;
        static float SpawnX(RealityRoot root) => root.ToLocal(root.GetComponentInChildren<CatMotor2D>(true).transform.position).x;

        static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size, List<string> changes)
        {
            if (rect.anchorMin == min && rect.anchorMax == max && rect.pivot == pivot && rect.anchoredPosition == position && rect.sizeDelta == size) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            changes.Add("positioned " + rect.name);
        }

        static void SetImage(Image image, Sprite sprite, Color color, bool raycastTarget, List<string> changes)
        {
            if (image.sprite != sprite) { image.sprite = sprite; changes.Add("set " + image.name + ".sprite"); }
            if (image.color != color) { image.color = color; changes.Add("set " + image.name + ".color"); }
            if (image.raycastTarget != raycastTarget) { image.raycastTarget = raycastTarget; changes.Add("set " + image.name + ".raycastTarget"); }
        }
    }
}
