using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    [RequireComponent(typeof(GravityReceiver))]
    public sealed class GravityDebugControl : MonoBehaviour
    {
        GravityReceiver receiver;

        void Awake()
        {
            receiver = GetComponent<GravityReceiver>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2? dir = null;

            if (keyboard.qKey.wasPressedThisFrame)
            {
                dir = GravityMath.Rotate90(receiver.TargetDirection, +1);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                dir = GravityMath.Rotate90(receiver.TargetDirection, -1);
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                dir = Vector2.down;
            }

            if (dir == null) return;

            receiver.SetTargetDirection(dir.Value);
            Debug.Log($"GravityDebug: {gameObject.name} gravity → ({dir.Value.x:0}, {dir.Value.y:0})");
        }
    }
}
