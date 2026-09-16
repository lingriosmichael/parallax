using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    // Dev-only. Never assigns drivers — only reads Observer state and toggles the
    // inactive Observer's Camera component for picture-in-picture.
    public sealed class DebugPanel : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;

        static readonly Rect DbgButtonRect = new Rect(10f, 10f, 70f, 30f);
        static readonly Rect PanelRect = new Rect(10f, 45f, 340f, 190f);

        bool open;
        bool pipOn;

        float fpsTimer;
        int fpsFrames;
        float fps;

        void OnEnable()
        {
            if (switchController != null) switchController.Switched += OnSwitched;
        }

        void OnDisable()
        {
            if (switchController != null) switchController.Switched -= OnSwitched;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
            {
                open = !open;
            }

            fpsTimer += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                fpsTimer = 0f;
                fpsFrames = 0;
            }
        }

        void OnSwitched(ObserverId from, ObserverId to)
        {
            ApplyPiP();
        }

        void ApplyPiP()
        {
            if (observers == null || switchController == null) return;

            ObserverContext inactive = observers.Get(switchController.Active.Other());
            Camera cam = inactive != null ? inactive.Camera : null;
            if (cam == null) return;

            cam.enabled = pipOn;
            if (pipOn)
            {
                cam.rect = new Rect(0.70f, 0.70f, 0.28f, 0.28f);
                cam.depth = 1f;
            }
        }

        void TogglePiP()
        {
            pipOn = !pipOn;
            ApplyPiP();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (GUI.Button(DbgButtonRect, "DBG"))
            {
                open = !open;
            }

            if (!open || observers == null || switchController == null) return;

            GUILayout.BeginArea(PanelRect, GUI.skin.box);

            GUILayout.Label($"Active: {switchController.Active}   Tick: {observers.Tick}   FPS: {fps:F0}");

            DrawObserverRow(ObserverId.A);
            DrawObserverRow(ObserverId.B);

            bool nextPiP = GUILayout.Toggle(pipOn, "PiP (inactive reality, top-right)");
            if (nextPiP != pipOn)
            {
                TogglePiP();
            }

            GUILayout.EndArea();
        }

        void DrawObserverRow(ObserverId id)
        {
            ObserverContext observer = observers.Get(id);
            if (observer == null)
            {
                GUILayout.Label($"{id}: (missing)");
                return;
            }

            string kind = observer.Driver != null ? observer.Driver.Kind.ToString() : "none";
            GravityReceiverSummary(observer, out float angleDeg, out bool grounded, out float speed);

            GUILayout.Label($"{id}: {kind}  grav {angleDeg:F0}°  grounded {grounded}  speed {speed:F1}");
        }

        static void GravityReceiverSummary(ObserverContext observer, out float angleDeg, out bool grounded, out float speed)
        {
            angleDeg = 0f;
            grounded = false;
            speed = 0f;

            if (observer.Gravity != null)
            {
                angleDeg = Vector2.SignedAngle(Vector2.down, observer.Gravity.Direction);
            }

            if (observer.Cat != null)
            {
                grounded = observer.Cat.IsGrounded;

                var body = observer.Cat.GetComponent<Rigidbody2D>();
                if (body != null) speed = body.linearVelocity.magnitude;
            }
        }
#endif

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            Vector2 guiPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);

            if (DbgButtonRect.Contains(guiPoint)) return true;
            if (open && PanelRect.Contains(guiPoint)) return true;

            return false;
        }
    }
}
