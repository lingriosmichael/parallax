using Parallax.Core;
using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    // Dev-only view toggle. Swaps which Observer's Camera component is enabled.
    // Never touches drivers. Replaced by the real SWITCH in PAX-017.
    public sealed class RealityViewDebugToggle : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;

        ObserverId viewing = ObserverId.A;

        void Start()
        {
            ApplyView();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        void Toggle()
        {
            viewing = viewing.Other();
            ApplyView();
        }

        void ApplyView()
        {
            if (observers == null) return;

            SetCameraEnabled(ObserverId.A, viewing == ObserverId.A);
            SetCameraEnabled(ObserverId.B, viewing == ObserverId.B);
        }

        void SetCameraEnabled(ObserverId id, bool isEnabled)
        {
            ObserverContext observer = observers.Get(id);
            if (observer == null || observer.Camera == null) return;

            observer.Camera.enabled = isEnabled;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (observers == null) return;

            ObserverContext a = observers.Get(ObserverId.A);
            string drivingKind = a != null && a.Driver != null ? a.Driver.Kind.ToString() : "none";

            GUI.Label(new Rect(10f, 110f, 320f, 20f), $"View: {viewing} — driving: {drivingKind}");

            if (GUI.Button(new Rect(10f, 130f, 140f, 40f), "Toggle View (V)"))
            {
                Toggle();
            }
        }
#endif
    }
}
