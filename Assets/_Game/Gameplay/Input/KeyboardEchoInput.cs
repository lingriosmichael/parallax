using Parallax.Gameplay.Echo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    public sealed class KeyboardEchoInput : MonoBehaviour
    {
        [SerializeField] EchoSession echoSession;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame && echoSession != null)
                echoSession.ToggleRecord();
        }
    }
}
