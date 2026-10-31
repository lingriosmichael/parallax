using System.Globalization;
using System.IO;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-108 amendment 2: renders the title as a player sees it, at chosen seconds of the intro, in Play Mode. The overlay canvas
    /// can't draw into a texture, so for the capture only it's moved onto the camera; nothing is saved. Batch mode on a clone:
    /// Unity -batchmode -projectPath clone -executeMethod Parallax.Editor.Art.TitleCapture.RunBatch with PARALLAX_CAPTURE_OUT (a folder)
    /// and PARALLAX_CAPTURE_TIMES (seconds, comma-separated). A frame also takes the width and height from PARALLAX_CAPTURE_SIZE (2400x1080).</summary>
    [InitializeOnLoad]
    public static class TitleCapture
    {
        const string KeyOut = "pax.titlecap.out", KeyTimes = "pax.titlecap.times", KeySize = "pax.titlecap.size";
        const string ScenePath = "Assets/_Game/Scenes/Title.unity";
        static float[] times;
        static int next, settle, width, height;
        static RenderTexture target;

        static TitleCapture() { EditorApplication.update += Tick; }

        public static void RunBatch()
        {
            string output = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_OUT");
            if (string.IsNullOrEmpty(output)) { Debug.LogError("TitleCapture: set PARALLAX_CAPTURE_OUT."); EditorApplication.Exit(1); return; }
            Directory.CreateDirectory(output);
            SessionState.SetString(KeyOut, output);
            SessionState.SetString(KeyTimes, System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_TIMES") ?? "9");
            SessionState.SetString(KeySize, System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_SIZE") ?? "2400x1080");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            string output = SessionState.GetString(KeyOut, "");
            if (output.Length == 0 || !EditorApplication.isPlaying) return;
            TitleScreen screen = Object.FindAnyObjectByType<TitleScreen>();
            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (screen == null || camera == null) return;
            if (times == null) Begin(screen, camera);
            if (settle-- > 0) return;

            float at = times[next];
            screen.Seek(at);
            EnvironmentCapture.PrepareLights();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(output, "title_" + width + "_" + at.ToString("00.00", CultureInfo.InvariantCulture) + "s.png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
            settle = 2;
            if (++next < times.Length) return;
            SessionState.SetString(KeyOut, "");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        static void Begin(TitleScreen screen, Camera camera)
        {
            string[] seconds = SessionState.GetString(KeyTimes, "9").Split(',');
            times = new float[seconds.Length];
            for (int i = 0; i < seconds.Length; i++) times[i] = float.Parse(seconds[i], CultureInfo.InvariantCulture);
            string[] size = SessionState.GetString(KeySize, "2400x1080").Split('x');
            width = int.Parse(size[0]);
            height = int.Parse(size[1]);
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            Canvas canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.5f;
            canvas.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;   // on a camera it sorts with the world
            canvas.sortingOrder = 1000;
            camera.cullingMask |= 1 << canvas.gameObject.layer;
            settle = 30;
        }
    }
}
