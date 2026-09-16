using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    public sealed class ObserverDriverDebugToggle : MonoBehaviour
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] CatInputRouter router;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        void Toggle()
        {
            if (observers == null) return;

            ObserverContext a = observers.Get(ObserverId.A);
            if (a == null) return;

            if (a.Driver != null && a.Driver.Kind == InputSourceKind.LocalHuman)
            {
                a.SetDriver(new InactiveDriver());
            }
            else if (router != null)
            {
                a.SetDriver(new LocalHumanDriver(router));
            }
            else
            {
                Debug.LogError($"ObserverDriverDebugToggle '{gameObject.name}' has no CatInputRouter assigned; cannot reactivate LocalHuman.", this);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (observers == null) return;

            ObserverContext a = observers.Get(ObserverId.A);
            string kind = a != null && a.Driver != null ? a.Driver.Kind.ToString() : "none";

            GUI.Label(new Rect(10f, 40f, 300f, 20f), $"Observer A: {kind}   Tick: {observers.Tick}");

            if (GUI.Button(new Rect(10f, 60f, 120f, 40f), "Toggle A (T)"))
            {
                Toggle();
            }
        }
#endif
    }
}
