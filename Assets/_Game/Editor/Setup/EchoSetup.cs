using System.Collections.Generic;
using Parallax.DebugTools;
using Parallax.Gameplay.Echo;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Transport;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class EchoSetup
    {
        const string ConfigPath = "Assets/_Game/Data/EchoConfig.asset";

        [MenuItem("PARALLAX/Setup/Echo (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();
            GameObject observersGo = GameObject.Find("Observers");
            GameObject inputGo = GameObject.Find("DeviceInput");
            if (observersGo == null || inputGo == null)
            {
                Debug.LogError("EchoSetup: Observers and DeviceInput are required.");
                return;
            }
            var observers = observersGo.GetComponent<ObserverSet>();
            var switchController = observersGo.GetComponent<SoloSwitchController>();
            var host = Object.FindAnyObjectByType<LocalTransportHost>();
            if (observers == null || switchController == null || host == null)
            {
                Debug.LogError("EchoSetup: ObserverSet, SoloSwitchController, and LocalTransportHost are required.");
                return;
            }
            EchoConfig config = EnsureConfig(changes);
            EchoSession session = Ensure<EchoSession>(observersGo, changes);
            Wire(session, "observers", observers, changes);
            Wire(session, "switchController", switchController, changes);
            Wire(session, "transportHost", host, changes);
            Wire(session, "config", config, changes);
            Wire(switchController, "echoSession", session, changes);
            var keyboard = Ensure<KeyboardEchoInput>(inputGo, changes);
            Wire(keyboard, "echoSession", session, changes);
            EchoRecordButton record = EnsureRecordButton(session, changes);
            var panel = observersGo.GetComponent<DebugPanel>();
            if (panel != null) Wire(panel, "echoSession", session, changes);
            var touch = inputGo.GetComponent<TouchStickCatInput>();
            if (touch != null) AddReserved(touch, record, changes);
            if (changes.Count == 0)
            {
                Debug.Log("EchoSetup: no changes.");
            }
            else
            {
                Debug.Log("EchoSetup: " + string.Join("; ", changes));
                EditorSceneManager.MarkSceneDirty(observersGo.scene);
            }
            RealityIsolationValidator.Validate();
            AnchorValidator.Validate();
        }

        static EchoConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<EchoConfig>(ConfigPath);
            if (config != null) return config;
            var created = ScriptableObject.CreateInstance<EchoConfig>();
            AssetDatabase.CreateAsset(created, ConfigPath);
            changes.Add("created EchoConfig asset");
            return created;
        }

        static EchoRecordButton EnsureRecordButton(EchoSession session, List<string> changes)
        {
            GameObject hud = GameObject.Find("HUD"); if (hud == null) { Debug.LogError("EchoSetup: HUD is missing. Run Switch + Debug setup first."); return null; }
            Transform existing = hud.transform.Find("RecordButton"); GameObject go;
            if (existing == null) { go = new GameObject("RecordButton", typeof(RectTransform)); go.transform.SetParent(hud.transform, false); changes.Add("created RecordButton"); } else go = existing.gameObject;
            var rect = go.GetComponent<RectTransform>(); Vector2 anchor = new Vector2(0.5f, 1f); Vector2 position = new Vector2(-250f, -40f); Vector2 size = new Vector2(180f, 110f);
            if (rect.anchorMin != anchor || rect.anchorMax != anchor || rect.pivot != anchor || rect.anchoredPosition != position || rect.sizeDelta != size) { rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; changes.Add("positioned RecordButton"); }
            Ensure<Image>(go, changes); var button = Ensure<Button>(go, changes); var nav = button.navigation; if (nav.mode != Navigation.Mode.None) { nav.mode = Navigation.Mode.None; button.navigation = nav; changes.Add("set RecordButton navigation none"); }
            Text label = go.GetComponentInChildren<Text>(true); if (label == null) { var labelGo = new GameObject("Label", typeof(RectTransform)); labelGo.transform.SetParent(go.transform, false); label = labelGo.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 36; label.alignment = TextAnchor.MiddleCenter; label.color = Color.black; var labelRect = (RectTransform)labelGo.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero; changes.Add("created RecordButton label"); }
            var record = Ensure<EchoRecordButton>(go, changes); Wire(record, "echoSession", session, changes); Wire(record, "button", button, changes); Wire(record, "label", label, changes); return record;
        }

        static void AddReserved(TouchStickCatInput touch, EchoRecordButton record, List<string> changes)
        {
            if (record == null) return;
            var serialized = new SerializedObject(touch); var property = serialized.FindProperty("reservedRegions");
            for (int i = 0; i < property.arraySize; i++) if (property.GetArrayElementAtIndex(i).objectReferenceValue == record) return;
            int oldSize = property.arraySize; property.arraySize++; property.GetArrayElementAtIndex(oldSize).objectReferenceValue = record; serialized.ApplyModifiedPropertiesWithoutUndo(); changes.Add("added RecordButton to TouchStickCatInput.reservedRegions");
        }

        static T Ensure<T>(GameObject go, List<string> changes) where T : Component { var c = go.GetComponent<T>(); if (c != null) return c; c = go.AddComponent<T>(); changes.Add("added " + typeof(T).Name + " to " + go.name); return c; }
        static void Wire(Object target, string field, Object value, List<string> changes) { if (target == null || value == null) return; var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); if (property == null || property.objectReferenceValue == value) return; property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); changes.Add("wired " + target.name + "." + field); }
    }
}
