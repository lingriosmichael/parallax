using System.Collections.Generic;
using Parallax.App;
using Parallax.Core;
using Parallax.DebugTools;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static class ObserverSetup
    {
        const string CatPlayerName = "Cat_Player";

        [MenuItem("PARALLAX/Setup/Observers (Sandbox)")]
        public static void Configure()
        {
            var changes = new List<string>();

            GameObject catGO = GameObject.Find(CatPlayerName);
            if (catGO == null)
            {
                Debug.LogError($"ObserverSetup: no GameObject named '{CatPlayerName}' found in the active scene. Stopping.");
                return;
            }

            var motor = catGO.GetComponent<CatMotor2D>();
            if (motor == null)
            {
                Debug.LogError($"ObserverSetup: '{CatPlayerName}' has no CatMotor2D. Stopping.");
                return;
            }

            // Step 1: DeviceInput rig.
            GameObject deviceInputGO = GameObject.Find("DeviceInput");
            if (deviceInputGO == null)
            {
                deviceInputGO = new GameObject("DeviceInput");
                Undo.RegisterCreatedObjectUndo(deviceInputGO, "Setup Observers");
                changes.Add("created DeviceInput");
            }

            var keyboardInput = deviceInputGO.GetComponent<KeyboardCatInput>();
            if (keyboardInput == null)
            {
                keyboardInput = deviceInputGO.AddComponent<KeyboardCatInput>();
                changes.Add("added KeyboardCatInput to DeviceInput");
            }

            var touchInput = deviceInputGO.GetComponent<TouchStickCatInput>();
            if (touchInput == null)
            {
                touchInput = deviceInputGO.AddComponent<TouchStickCatInput>();
                changes.Add("added TouchStickCatInput to DeviceInput");
            }

            var router = deviceInputGO.GetComponent<CatInputRouter>();
            if (router == null)
            {
                router = deviceInputGO.AddComponent<CatInputRouter>();
                changes.Add("added CatInputRouter to DeviceInput");
            }

            var routerSO = new SerializedObject(router);
            var sourcesProp = routerSO.FindProperty("sources");
            if (!ContainsReference(sourcesProp, keyboardInput))
            {
                AppendSource(sourcesProp, keyboardInput);
                changes.Add("added KeyboardCatInput to CatInputRouter.sources");
            }
            if (!ContainsReference(sourcesProp, touchInput))
            {
                AppendSource(sourcesProp, touchInput);
                changes.Add("added TouchStickCatInput to CatInputRouter.sources");
            }
            routerSO.ApplyModifiedPropertiesWithoutUndo();

            // Step 2: strip input components from Cat_Player (prefab asset, then scene instance).
            StripPrefabInputComponents(catGO, changes);
            RemoveInputComponents(catGO, changes, "Cat_Player scene instance");

            // Step 3: Observers + Observer_A.
            GameObject observersGO = GameObject.Find("Observers");
            if (observersGO == null)
            {
                observersGO = new GameObject("Observers");
                Undo.RegisterCreatedObjectUndo(observersGO, "Setup Observers");
                changes.Add("created Observers");
            }

            var observerSet = observersGO.GetComponent<ObserverSet>();
            if (observerSet == null)
            {
                observerSet = observersGO.AddComponent<ObserverSet>();
                changes.Add("added ObserverSet to Observers");
            }

            var bootstrap = observersGO.GetComponent<ObserverBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = observersGO.AddComponent<ObserverBootstrap>();
                changes.Add("added ObserverBootstrap to Observers");
            }

            GameObject observerAGO = GameObject.Find("Observer_A");
            if (observerAGO == null)
            {
                observerAGO = new GameObject("Observer_A");
                Undo.RegisterCreatedObjectUndo(observerAGO, "Setup Observers");
                observerAGO.transform.SetParent(observersGO.transform);
                changes.Add("created Observer_A");
            }

            var observerA = observerAGO.GetComponent<ObserverContext>();
            if (observerA == null)
            {
                observerA = observerAGO.AddComponent<ObserverContext>();
                changes.Add("added ObserverContext to Observer_A");
            }

            var observerASO = new SerializedObject(observerA);
            var idProp = observerASO.FindProperty("id");
            if ((ObserverId)idProp.enumValueIndex != ObserverId.A)
            {
                idProp.enumValueIndex = (int)ObserverId.A;
                changes.Add("set Observer_A.id = A");
            }
            var catProp = observerASO.FindProperty("cat");
            if (catProp.objectReferenceValue != motor)
            {
                catProp.objectReferenceValue = motor;
                changes.Add("assigned Observer_A.cat = Cat_Player");
            }
            observerASO.ApplyModifiedPropertiesWithoutUndo();

            var observerSetSO = new SerializedObject(observerSet);
            var observerAProp = observerSetSO.FindProperty("observerA");
            if (observerAProp.objectReferenceValue != observerA)
            {
                observerAProp.objectReferenceValue = observerA;
                changes.Add("assigned ObserverSet.observerA = Observer_A");
            }
            observerSetSO.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapSO = new SerializedObject(bootstrap);
            var observersProp = bootstrapSO.FindProperty("observers");
            if (observersProp.objectReferenceValue != observerSet)
            {
                observersProp.objectReferenceValue = observerSet;
                changes.Add("assigned ObserverBootstrap.observers = ObserverSet");
            }
            var routerProp = bootstrapSO.FindProperty("router");
            if (routerProp.objectReferenceValue != router)
            {
                routerProp.objectReferenceValue = router;
                changes.Add("assigned ObserverBootstrap.router = CatInputRouter");
            }
            bootstrapSO.ApplyModifiedPropertiesWithoutUndo();

            // Step 4: debug toggle.
            var toggle = observersGO.GetComponent<ObserverDriverDebugToggle>();
            if (toggle == null)
            {
                toggle = observersGO.AddComponent<ObserverDriverDebugToggle>();
                changes.Add("added ObserverDriverDebugToggle to Observers");
            }

            var toggleSO = new SerializedObject(toggle);
            var toggleObserversProp = toggleSO.FindProperty("observers");
            if (toggleObserversProp.objectReferenceValue != observerSet)
            {
                toggleObserversProp.objectReferenceValue = observerSet;
                changes.Add("assigned ObserverDriverDebugToggle.observers = ObserverSet");
            }
            var toggleRouterProp = toggleSO.FindProperty("router");
            if (toggleRouterProp.objectReferenceValue != router)
            {
                toggleRouterProp.objectReferenceValue = router;
                changes.Add("assigned ObserverDriverDebugToggle.router = CatInputRouter");
            }
            toggleSO.ApplyModifiedPropertiesWithoutUndo();

            // Step 5: log summary.
            if (changes.Count == 0)
            {
                Debug.Log("ObserverSetup: no changes.");
            }
            else
            {
                Debug.Log($"ObserverSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(catGO.scene);
            }
        }

        static void RemoveInputComponents(GameObject go, List<string> changes, string label)
        {
            var keyboard = go.GetComponent<KeyboardCatInput>();
            if (keyboard != null)
            {
                Object.DestroyImmediate(keyboard);
                changes.Add($"removed KeyboardCatInput from {label}");
            }

            var touch = go.GetComponent<TouchStickCatInput>();
            if (touch != null)
            {
                Object.DestroyImmediate(touch);
                changes.Add($"removed TouchStickCatInput from {label}");
            }

            var router = go.GetComponent<CatInputRouter>();
            if (router != null)
            {
                Object.DestroyImmediate(router);
                changes.Add($"removed CatInputRouter from {label}");
            }
        }

        static void StripPrefabInputComponents(GameObject catGO, List<string> changes)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(catGO);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogWarning($"ObserverSetup: '{catGO.name}' is not a prefab instance (no prefab asset path resolved). Skipping prefab-asset strip.", catGO);
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"ObserverSetup: could not load prefab asset at '{prefabPath}' resolved from '{catGO.name}'. Skipping prefab-asset strip.", catGO);
                return;
            }

            bool hasInput = prefabAsset.GetComponent<KeyboardCatInput>() != null
                            || prefabAsset.GetComponent<TouchStickCatInput>() != null
                            || prefabAsset.GetComponent<CatInputRouter>() != null;
            if (!hasInput) return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var prefabChanges = new List<string>();
                RemoveInputComponents(root, prefabChanges, "Cat_Player prefab asset");
                if (prefabChanges.Count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    changes.AddRange(prefabChanges);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool ContainsReference(SerializedProperty arrayProp, Object value)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == value) return true;
            }
            return false;
        }

        static void AppendSource(SerializedProperty arrayProp, Object value)
        {
            int index = arrayProp.arraySize;
            arrayProp.arraySize++;
            arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = value;
        }
    }
}
