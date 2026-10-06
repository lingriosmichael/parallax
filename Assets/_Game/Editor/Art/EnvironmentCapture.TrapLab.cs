using System.IO;
using System.Linq;
using Parallax.Editor.Setup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-106 §5.3: a still of one Trap Lab room at the phone's scale (2400 × 1080) for the before/after sheets.
    /// The sandbox has no level camera, so the room is framed from its layout (every element's box and the ceiling).
    /// Batch mode on a clone: Unity -batchmode -projectPath clone -executeMethod
    /// Parallax.Editor.Art.EnvironmentCapture.RunTrapLabBatch with PARALLAX_CAPTURE_OUT (a folder) and
    /// PARALLAX_CAPTURE_ROOM (the room's index in TrapLabLayout.Rooms, e.g. 14).</summary>
    public static partial class EnvironmentCapture
    {
        const string TrapLabScene = "Assets/Sandbox_TrapLab.unity";

        public static void RunTrapLabBatch()
        {
            string output = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_OUT");
            string roomText = System.Environment.GetEnvironmentVariable("PARALLAX_CAPTURE_ROOM") ?? "14";
            if (string.IsNullOrEmpty(output) || !int.TryParse(roomText, out int index) || index < 0 || index >= TrapLabLayout.Rooms.Count)
            {
                Debug.LogError($"EnvironmentCapture: set PARALLAX_CAPTURE_OUT and PARALLAX_CAPTURE_ROOM (0-{TrapLabLayout.Rooms.Count - 1}).");
                EditorApplication.Exit(1);
                return;
            }
            Directory.CreateDirectory(output);
            bool ok = CaptureTrapLabRoom(index, Path.Combine(output, $"TrapLab{index}_full.png"));
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Opens the Trap Lab and renders room `index` framed whole; false (logged) when it can't.</summary>
        public static bool CaptureTrapLabRoom(int index, string path)
        {
            EditorSceneManager.OpenScene(TrapLabScene, OpenSceneMode.Single);
            SoloRoomDefinition room = TrapLabLayout.Rooms[index];
            Rect frame = new(room.Origin, Vector2.zero);
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Size == Vector2.zero) continue;
                Vector2 min = room.Origin + e.Position - e.Size * 0.5f, max = room.Origin + e.Position + e.Size * 0.5f;
                frame = Rect.MinMaxRect(Mathf.Min(frame.xMin, min.x), Mathf.Min(frame.yMin, min.y), Mathf.Max(frame.xMax, max.x), Mathf.Max(frame.yMax, max.y));
            }
            frame = Rect.MinMaxRect(Mathf.Min(frame.xMin, room.Origin.x - 1f), frame.yMin - 0.5f, Mathf.Max(frame.xMax, room.Origin.x + room.Width + 1f), frame.yMax + 0.5f);

            int reality = LayerMask.NameToLayer("RealityA");
            Camera cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.orthographic && (reality < 0 || (c.cullingMask & (1 << reality)) != 0))
                .OrderBy(c => c.name).FirstOrDefault();
            if (cam == null) { Debug.LogError("EnvironmentCapture: the Trap Lab has no orthographic camera that sees Reality A."); return false; }

            RenderTexture before = cam.targetTexture;
            Vector3 position = cam.transform.position;
            float size = cam.orthographicSize, aspect = cam.aspect;
            try
            {
                SetTarget(cam, W, H);
                cam.aspect = W / (float)H;
                cam.orthographicSize = Mathf.Max(frame.height * 0.5f, frame.width * 0.5f / cam.aspect);
                cam.transform.position = new Vector3(frame.center.x, frame.center.y, position.z);
                UpdateParallax();
                Texture2D shot = Render(cam);
                Save(shot, path);
                Object.DestroyImmediate(shot);
                return true;
            }
            finally
            {
                RenderTexture used = cam.targetTexture;
                cam.targetTexture = before;
                if (used != null && used != before) used.Release();
                cam.transform.position = position;
                cam.orthographicSize = size;
                cam.aspect = aspect;
            }
        }
    }
}
