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
        int steps;

        void Awake()
        {
            receiver = GetComponent<GravityReceiver>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            bool changed = false;

            if (keyboard.qKey.wasPressedThisFrame)
            {
                steps += 1;
                changed = true;
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                steps -= 1;
                changed = true;
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                steps = 0;
                changed = true;
            }

            if (!changed) return;

            Vector2 dir = GravityMath.Rotate90(Vector2.down, steps);
            receiver.SetTargetDirection(dir);
            Debug.Log($"GravityDebug: {gameObject.name} gravity → ({dir.x:0}, {dir.y:0})");
        }
    }
}
