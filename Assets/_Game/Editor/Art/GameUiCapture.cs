using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Parallax.Core;
using Parallax.Gameplay;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A09: renders the UI as a player sees it, in Play Mode, as TitleCapture does the title. The overlay canvas moves onto a second camera for
    /// the capture only, without post-processing, as on a phone, where an overlay isn't graded, and is laid over the world's frame; nothing is saved. Dev-only HUD pieces are hidden, as in a
    /// release build. Level select's cells get made-up states; PlayerPrefs is never written. Batch mode on a clone:
    /// Unity -batchmode -projectPath clone -executeMethod Parallax.Editor.Art.GameUiCapture.RunBatch with PARALLAX_CAPTURE_OUT (a folder),
    /// PARALLAX_CAPTURE_SCENE (a scene path) and PARALLAX_CAPTURE_SIZES (2400x1080,1920x1080).</summary>
    [InitializeOnLoad]
    public static class GameUiCapture
    {
        const string KeyOut = "pax.uicap.out", KeySizes = "pax.uicap.sizes", KeyScene = "pax.uicap.scene";
        static readonly List<System.Action> stages = new List<System.Action>();
        static readonly List<string> names = new List<string>();
        static readonly List<Vector2Int> sizes = new List<Vector2Int>();
        static int stage, size, settle = -1;
        static bool shown;
        static Camera world, ui;
        static Canvas canvas;
        static RenderTexture target, uiTarget;

        static GameUiCapture() { EditorApplication.update += Tick; }

        public static void RunBatch()
        {
            string output = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_OUT");
            string scene = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_SCENE");
            if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(scene)) { Debug.LogError("GameUiCapture: set PARALLAX_CAPTURE_OUT and PARALLAX_CAPTURE_SCENE."); EditorApplication.Exit(1); return; }
            Directory.CreateDirectory(output);
            SessionState.SetString(KeyOut, output);
            SessionState.SetString(KeyScene, Path.GetFileNameWithoutExtension(scene));
            SessionState.SetString(KeySizes, System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_SIZES") ?? "2400x1080");
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            string output = SessionState.GetString(KeyOut, "");
            if (output.Length == 0 || !EditorApplication.isPlaying) return;
            if (settle < 0) { if (!Begin()) return; settle = 40; return; }
            if (settle-- > 0) return;

            Vector2Int at = sizes[size];
            Aim(at);
            if (!shown) { stages[stage](); shown = true; settle = 40; return; }   // a Button's tint fades, so it gets a moment
            shown = false;
            Canvas.ForceUpdateCanvases();
            foreach (LayoutGroup group in Object.FindObjectsByType<LayoutGroup>(FindObjectsSortMode.None)) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            Canvas.ForceUpdateCanvases();
            ui.Render();
            Texture2D image = Read(uiTarget, at);
            if (world != null)
            {
                EnvironmentCapture.PrepareLights();
                world.Render();
                Texture2D back = Read(target, at);
                Color32[] over = image.GetPixels32(), under = back.GetPixels32();
                for (int i = 0; i < over.Length; i++)     // uGUI writes premultiplied colour
                {
                    int keep = 255 - over[i].a;
                    over[i] = new Color32((byte)Mathf.Min(255, over[i].r + under[i].r * keep / 255), (byte)Mathf.Min(255, over[i].g + under[i].g * keep / 255), (byte)Mathf.Min(255, over[i].b + under[i].b * keep / 255), 255);
                }
                image.SetPixels32(over);
                Object.DestroyImmediate(back);
            }
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, SessionState.GetString(KeyScene, "ui") + "_" + names[stage] + "_" + at.x + "x" + at.y + ".png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
            settle = 2;
            if (++stage < stages.Count) return;
            stage = 0;
            if (++size < sizes.Count) return;
            SessionState.SetString(KeyOut, "");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        static bool Begin()
        {
            foreach (string pair in SessionState.GetString(KeySizes, "2400x1080").Split(','))
            {
                string[] parts = pair.Split('x');
                sizes.Add(new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1])));
            }
            GameObject levelCamera = GameObject.Find("Camera_A");
            world = levelCamera != null ? levelCamera.GetComponent<Camera>() : Camera.main;
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (canvas == null || c.name == "HUD" || c.name == "Canvas") canvas = c;
            if (canvas == null) return false;
            foreach (DevOnly dev in Object.FindObjectsByType<DevOnly>(FindObjectsSortMode.None)) dev.gameObject.SetActive(false);
            // A batch Editor's screen is small, and the stick's size comes from the screen: keep the size the menu built.
            foreach (TouchControlsView view in Object.FindObjectsByType<TouchControlsView>(FindObjectsSortMode.None)) view.enabled = false;

            var go = new GameObject("CaptureUiCamera");
            ui = go.AddComponent<Camera>();
            ui.clearFlags = CameraClearFlags.SolidColor;
            ui.backgroundColor = world != null ? Color.clear : Color.black;
            ui.allowHDR = false;
            ui.cullingMask = 1 << 5;
            ui.depth = 100;
            ui.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            ui.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = ui;
            canvas.planeDistance = ui.nearClipPlane + 0.5f;
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;

            var hudPanels = new Dictionary<string, GameObject>();
            foreach (RectTransform t in canvas.GetComponentsInChildren<RectTransform>(true))
                if (t.parent == canvas.transform) hudPanels[t.name] = t.gameObject;
            System.Action hideAll = () =>
            {
                foreach (string panel in new[] { "PausePanel", "LevelCompletePanel", "UnlockPanel" })
                    if (hudPanels.TryGetValue(panel, out GameObject go2)) go2.SetActive(false);
            };

            if (hudPanels.ContainsKey("PlayHud"))
            {
                Add("hud", () => { hideAll(); hudPanels["PlayHud"].SetActive(true); });
                Add("pause", () => { hideAll(); hudPanels["PausePanel"].SetActive(true); });
                Add("complete", () =>
                {
                    hideAll();
                    var screen = Object.FindAnyObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
                    typeof(LevelCompleteScreen).GetMethod("OnLevelCompleted", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
                });
                Add("unlock", () => { hideAll(); hudPanels["UnlockPanel"].SetActive(true); });
            }
            else
            {
                Add("grid", () => { hideAll(); States(); });
                Add("unlock", () => { hideAll(); hudPanels["UnlockPanel"].SetActive(true); });
            }
            return true;
        }

        static void Add(string name, System.Action show) { names.Add(name); stages.Add(show); }

        // Fourteen done, the next open, the rest locked past a paywall at 11 for a player who hasn't paid.
        static void States()
        {
            LevelSelectRow[] rows = Object.FindObjectsByType<LevelSelectRow>(FindObjectsSortMode.None);
            System.Array.Sort(rows, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            for (int i = 0; i < rows.Length; i++)
            {
                int n = i + 1;
                bool done = n <= 14;
                rows[i].Configure(new LevelSelectEntry("L" + n, "Level_" + n, "Level " + n, n <= 15, done ? (n * 7 % 23).ToString() : "—", n > 24 && n <= 30, done), _ => { }, n);
            }
        }

        static void Aim(Vector2Int at)
        {
            if (target != null && target.width == at.x && target.height == at.y) return;
            target = new RenderTexture(at.x, at.y, 24, RenderTextureFormat.ARGB32);
            uiTarget = new RenderTexture(at.x, at.y, 24, RenderTextureFormat.ARGB32);
            if (world != null) world.targetTexture = target;
            ui.targetTexture = uiTarget;
        }

        static Texture2D Read(RenderTexture from, Vector2Int at)
        {
            var image = new Texture2D(at.x, at.y, TextureFormat.RGBA32, false);
            RenderTexture.active = from;
            image.ReadPixels(new Rect(0, 0, at.x, at.y), 0, 0);
            RenderTexture.active = null;
            return image;
        }
    }
}
