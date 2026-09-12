using System.Collections.Generic;
using Parallax.Gameplay;
using Parallax.Gameplay.Controls;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor
{
    public static class TouchInputSetup
    {
        const string CatPlayerName = "Cat_Player";

        [MenuItem("PARALLAX/Setup/Configure Touch Input")]
        public static void Configure()
        {
            GameObject catGO = GameObject.Find(CatPlayerName);
            if (catGO == null)
            {
                Debug.LogError($"TouchInputSetup: no GameObject named '{CatPlayerName}' found in the active scene. Stopping.");
                return;
            }

            var motor = catGO.GetComponent<CatMotor2D>();
            if (motor == null)
            {
                Debug.LogError($"TouchInputSetup: '{CatPlayerName}' has no CatMotor2D. Stopping.");
                return;
            }

            var changes = new List<string>();

            var touchInput = catGO.GetComponent<TouchCatInput>();
            if (touchInput == null)
            {
                touchInput = catGO.AddComponent<TouchCatInput>();
                changes.Add("added TouchCatInput");
            }

            var router = catGO.GetComponent<CatInputRouter>();
            if (router == null)
            {
                router = catGO.AddComponent<CatInputRouter>();
                changes.Add("added CatInputRouter");
            }

            var keyboardInput = catGO.GetComponent<KeyboardCatInput>();

            var routerSO = new SerializedObject(router);
            var sourcesProp = routerSO.FindProperty("sources");

            if (keyboardInput != null && !ContainsReference(sourcesProp, keyboardInput))
            {
                AppendSource(sourcesProp, keyboardInput);
                changes.Add("added KeyboardCatInput to CatInputRouter.sources");
            }

            if (!ContainsReference(sourcesProp, touchInput))
            {
                AppendSource(sourcesProp, touchInput);
                changes.Add("added TouchCatInput to CatInputRouter.sources");
            }

            routerSO.ApplyModifiedPropertiesWithoutUndo();

            var motorSO = new SerializedObject(motor);
            var commandSourceProp = motorSO.FindProperty("commandSource");
            if (commandSourceProp.objectReferenceValue != router)
            {
                commandSourceProp.objectReferenceValue = router;
                motorSO.ApplyModifiedPropertiesWithoutUndo();
                changes.Add("assigned CatMotor2D.commandSource = CatInputRouter");
            }

            if (changes.Count == 0)
            {
                Debug.Log("TouchInputSetup: already configured.");
            }
            else
            {
                Debug.Log($"TouchInputSetup: {string.Join("; ", changes)}.");
                EditorSceneManager.MarkSceneDirty(catGO.scene);
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
