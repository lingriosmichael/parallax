using System.Collections.Generic;
using Parallax.DebugTools;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Transport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    public static class TransportSetup
    {
        const string SystemsName = "Systems";

        [MenuItem("PARALLAX/Setup/Transport (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            var observerSet = Object.FindAnyObjectByType<ObserverSet>();
            if (observerSet == null)
            {
                Debug.LogError("TransportSetup: no ObserverSet found in the scene. Run 'PARALLAX/Setup/Observers (Sandbox)' first.");
                return;
            }

            GameObject systemsGO = GameObject.Find(SystemsName);
            if (systemsGO == null)
            {
                systemsGO = new GameObject(SystemsName);
                Undo.RegisterCreatedObjectUndo(systemsGO, "Setup Transport");
                changes.Add($"created {SystemsName}");
            }

            var host = EnsureComponent<LocalTransportHost>(systemsGO, changes, "LocalTransportHost", SystemsName);
            WireField(host, "observers", observerSet, changes, "LocalTransportHost.observers = ObserverSet");

            var debugPanel = Object.FindAnyObjectByType<DebugPanel>();
            if (debugPanel == null)
            {
                Debug.LogError("TransportSetup: no DebugPanel found in the scene. Run 'PARALLAX/Setup/Switch + Debug (Sandbox)' first.");
            }
            else
            {
                WireField(debugPanel, "transportHost", host, changes, "DebugPanel.transportHost = LocalTransportHost");
            }

            if (changes.Count == 0)
            {
                Debug.Log("TransportSetup: no changes.");
            }
            else
            {
                Debug.Log($"TransportSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(systemsGO.scene);
            }

            RealityIsolationValidator.Validate();
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

        static void WireField(Object target, string propertyName, Object value, List<string> changes, string label)
        {
            if (target == null || value == null) return;

            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"TransportSetup: '{target.GetType().Name}' has no serialized field '{propertyName}'.");
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
