using System.Collections.Generic;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-066 (D-279). Idempotent: makes the IapConfig asset if it's missing, then gives LevelSelect and _LevelTemplate a root
    /// "Store" (StoreHost) and an UnlockPanel, wiring it into the level select, Next level and the pause panel's Restore button, and
    /// saves both scenes. Rebuild All Levels then carries the template's into every Level_NNN. It opens the scenes itself, so it
    /// refuses while the open scene has unsaved changes.</summary>
    public static class PaidUnlockSetup
    {
        public const string RootName = "Store", PanelName = "UnlockPanel";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string LevelSelectPath = "Assets/_Game/Scenes/" + LevelSceneLoader.LevelSelectSceneName + ".unity";
        const string LevelListPath = "Assets/_Game/Data/LevelListConfig.asset";
        const string HudCanvasName = "HUD", SelectCanvasName = "Canvas", DeviceInputName = "DeviceInput";

        [MenuItem("PARALLAX/Setup/Paid Unlock (PAX-066)")]
        public static void Configure()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError("PaidUnlockSetup: '" + active.name + "' has unsaved changes; save or discard them first."); return; }
            string originalPath = active.path;

            var changes = new List<string>();
            EnsureConfig(changes);
            Run(LevelSelectPath, SelectCanvasName, false, changes);
            Run(TemplatePath, HudCanvasName, true, changes);

            if (!string.IsNullOrEmpty(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) != null)
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            Debug.Log("PaidUnlockSetup:\n" + string.Join("\n", changes) + "\nRun PARALLAX/Setup/Levels/Rebuild All Levels next, so every level gets the template's.");
        }

        public static IapConfig EnsureConfig(List<string> changes)
        {
            var config = AssetDatabase.LoadAssetAtPath<IapConfig>(IapConfig.AssetPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<IapConfig>();
            AssetDatabase.CreateAsset(config, IapConfig.AssetPath);
            AssetDatabase.SaveAssets();
            changes.Add("created " + IapConfig.AssetPath);
            return config;
        }

        static void Run(string path, string canvasName, bool level, List<string> changes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var sceneChanges = new List<string>();
            // Loaded after the open: opening a scene unloads unreferenced assets.
            IapConfig config = EnsureConfig(changes);
            var levelList = AssetDatabase.LoadAssetAtPath<LevelListConfig>(LevelListPath);
            GameObject canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == canvasName) canvas = root;
            if (canvas == null || levelList == null) { Debug.LogError("PaidUnlockSetup: '" + scene.name + "' has no '" + canvasName + "' canvas, or there's no LevelListConfig; skipped."); return; }

            StoreHost store = SetupUtility.Ensure<StoreHost>(SetupUtility.EnsureSceneRoot(RootName, sceneChanges), sceneChanges);
            TrapKitSetup.Write(store, sceneChanges, ("config", config));
            if (level) PauseMenuSetup.BuildPauseUI(canvas.transform, sceneChanges);
            UnlockPanel panel = BuildPanel(canvas.transform, level, sceneChanges);
            TrapKitSetup.Write(panel, sceneChanges, ("store", store), ("levelList", levelList));

            if (level)
            {
                var next = Object.FindAnyObjectByType<NextLevelButton>(FindObjectsInactive.Include);
                var pause = Object.FindAnyObjectByType<PausePanel>(FindObjectsInactive.Include);
                var touch = GameObject.Find(DeviceInputName)?.GetComponent<Parallax.Gameplay.Input.TouchStickCatInput>();
                if (next == null || pause == null || touch == null) { Debug.LogError("PaidUnlockSetup: the template has no NextLevelButton, PausePanel or TouchStickCatInput; run the level UI menus first."); return; }
                TrapKitSetup.Write(next, sceneChanges, ("unlockPanel", panel));
                TrapKitSetup.Write(pause, sceneChanges, ("store", store));
                PauseMenuSetup.AppendReservedRegion(touch, panel, sceneChanges);
            }
            else
            {
                var select = Object.FindAnyObjectByType<LevelSelectController>(FindObjectsInactive.Include);
                if (select == null) { Debug.LogError("PaidUnlockSetup: LevelSelect has no LevelSelectController; run its menu first."); return; }
                TrapKitSetup.Write(select, sceneChanges, ("unlockPanel", panel), ("store", store));
            }

            if (sceneChanges.Count == 0) { changes.Add(scene.name + ": no changes"); return; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            changes.Add(scene.name + ": " + string.Join(", ", sceneChanges) + " (saved)");
        }

        /// <summary>Builds (or re-checks) the panel under `canvas`. On a level's HUD it goes under the pause panel, which stays on top.</summary>
        public static UnlockPanel BuildPanel(Transform canvas, bool underPause, List<string> changes)
        {
            bool isNew = canvas.Find(PanelName) == null;
            GameObject go = PauseMenuSetup.EnsureChild(canvas, PanelName, changes);
            if (isNew) go.transform.SetSiblingIndex(Mathf.Max(0, canvas.childCount - (underPause ? 2 : 1)));
            PauseMenuSetup.SetAnchors((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, changes, PanelName);
            var image = PauseMenuSetup.EnsureComponent<Image>(go, changes, PanelName);
            if (image.color != new Color(0f, 0f, 0f, 0.92f)) { image.color = new Color(0f, 0f, 0f, 0.92f); changes.Add("set " + PanelName + " Image.color"); }
            var panel = PauseMenuSetup.EnsureComponent<UnlockPanel>(go, changes, PanelName);

            Text title = Line(go.transform, "Title", 160f, 250f, changes);
            PauseMenuSetup.ConfigureText(title, "Keep going?", 56, Color.white, changes, PanelName + "/Title");
            Text status = Line(go.transform, "Status", 20f, 150f, changes);
            PauseMenuSetup.ConfigureText(status, "", 34, Color.white, changes, PanelName + "/Status");

            Button unlock = Button(go.transform, "UnlockButton", "Unlock all levels", -340f, 680f, -70f, changes);
            Button restore = Button(go.transform, "RestoreButton", "Restore purchase", -340f, 330f, -180f, changes);
            Button close = Button(go.transform, "CloseButton", "Not now", 10f, 330f, -180f, changes);
            TrapKitSetup.Write(panel, changes, ("statusText", status), ("unlockLabel", unlock.transform.Find("Label").GetComponent<Text>()),
                ("unlockButton", unlock), ("restoreButton", restore), ("closeButton", close));

            if (go.activeSelf) { go.SetActive(false); changes.Add("set " + PanelName + " active = False"); }   // shown by UnlockPanel.Show
            return panel;
        }

        static Text Line(Transform panel, string name, float bottom, float top, List<string> changes)
        {
            GameObject go = PauseMenuSetup.EnsureChild(panel, name, changes);
            PauseMenuSetup.SetAnchors((RectTransform)go.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(40f, bottom), new Vector2(-40f, top), changes, PanelName + "/" + name);
            return PauseMenuSetup.EnsureComponent<Text>(go, changes, PanelName + "/" + name);
        }

        static Button Button(Transform panel, string name, string label, float left, float width, float y, List<string> changes) =>
            PauseMenuSetup.EnsurePanelButton(panel, name, label, left, changes, width, y);
    }
}
