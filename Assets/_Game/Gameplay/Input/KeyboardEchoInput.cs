using Parallax.Gameplay.Echo;
using Parallax.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    public sealed class KeyboardEchoInput : MonoBehaviour
    {
        [SerializeField] EchoSession echoSession;

        void Awake()
        {
            if (DevOnly.ShouldRemainInBuild(Debug.isDebugBuild)) return;
            Destroy(this);
            return;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame && echoSession != null)
                echoSession.ToggleRecord();
        }
    }
}
