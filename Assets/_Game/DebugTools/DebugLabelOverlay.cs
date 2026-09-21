using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Echo;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using UnityEngine;

namespace Parallax.DebugTools
{
    public sealed class DebugLabelOverlay : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] RealityRoot rootA;
        [SerializeField] RealityRoot rootB;
        [SerializeField] SoloSwitchController switchController;

        static readonly Rect LabelButtonRect = new Rect(90f, 45f, 70f, 30f);
        static readonly Rect LegendRect = new Rect(0f, 112f, 300f, 160f);

        bool labelsVisible = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (GUI.Button(LabelButtonRect, "LBL")) labelsVisible = !labelsVisible;
            if (!labelsVisible || observers == null) return;

            DrawLabels(rootA);
            DrawLabels(rootB);
            DrawLegend();
        }

        void DrawLabels(RealityRoot root)
        {
            if (root == null) return;

            ObserverContext observer = observers.Get(root.Id);
            if (observer == null || observer.Camera == null || !observer.Camera.enabled) return;

            Camera camera = observer.Camera;
            DrawCat(observer, camera);
            DebugLabel[] labels = root.GetComponentsInChildren<DebugLabel>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                DebugLabel label = labels[i];
                Vector3 screenPosition = camera.WorldToScreenPoint(label.transform.TransformPoint(label.Offset));
                if (screenPosition.z < 0f || !camera.pixelRect.Contains((Vector2)screenPosition)) continue;

                string text = label.Text;
                CheckpointMarker marker = label.GetComponent<CheckpointMarker>();
                if (marker != null) text = $"Checkpoint {marker.Id}";
                if (string.IsNullOrEmpty(text)) continue;

                GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y, 180f, 22f), text);
            }
        }

        void DrawCat(ObserverContext observer, Camera camera)
        {
            if (observer.Cat == null || switchController == null) return;

            Vector3 screenPosition = camera.WorldToScreenPoint(observer.Cat.transform.position);
            if (screenPosition.z < 0f || !camera.pixelRect.Contains((Vector2)screenPosition)) return;

            string state = switchController.Active == observer.Id ? "ACTIVE" : "INACTIVE";
            string text = $"CAT {observer.Id} ({state})";
            if (observer.Driver is EchoReplayDriver) text += "\nECHO";
            GUI.Label(new Rect(screenPosition.x, Screen.height - screenPosition.y - 22f, 180f, 40f), text);
        }

        static void DrawLegend()
        {
            Rect legendRect = LegendRect;
            legendRect.x = Screen.width - legendRect.width - 16f;
            GUI.Box(legendRect, "Controls");
            GUILayout.BeginArea(new Rect(legendRect.x + 10f, legendRect.y + 24f, legendRect.width - 20f, legendRect.height - 28f));
            GUILayout.Label("Move: A/D or stick    Jump: Space");
            GUILayout.Label("Interact: F    RECORD: Z    SWITCH: Tab");
            GUILayout.Label("Gravity: Q / FLIP");
            GUILayout.Label("Debug panel: DBG    Checkpoint row");
            GUILayout.Label("Respawn active cat: Debug panel button");
            GUILayout.EndArea();
        }
#endif
    }
}
