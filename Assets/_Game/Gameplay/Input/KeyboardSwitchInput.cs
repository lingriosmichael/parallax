using Parallax.Gameplay.Observers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    // Editor convenience: Tab toggles the active Observer. Lives on DeviceInput.
    public sealed class KeyboardSwitchInput : MonoBehaviour
    {
        [SerializeField] SoloSwitchController switchController;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && switchController != null)
            {
                switchController.Toggle();
            }
        }
    }
}
