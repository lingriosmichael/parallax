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

            Rotate(dir.Value);
        }

        void Rotate(Vector2 dir)
        {
            receiver.SetTargetDirection(dir);
            Debug.Log($"GravityDebug: {gameObject.name} gravity → ({dir.x:0}, {dir.y:0})");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            const float buttonSize = 80f;
            const float margin = 16f;

            var ccwRect = new Rect(Screen.width - margin - buttonSize * 2f - margin, margin, buttonSize, buttonSize);
            var cwRect = new Rect(Screen.width - margin - buttonSize, margin, buttonSize, buttonSize);

            var style = new GUIStyle(GUI.skin.button) { fontSize = 32 };

            if (GUI.Button(ccwRect, "↺", style))
            {
                Rotate(GravityMath.Rotate90(receiver.TargetDirection, +1));
            }

            if (GUI.Button(cwRect, "↻", style))
            {
                Rotate(GravityMath.Rotate90(receiver.TargetDirection, -1));
            }
        }
#endif
    }
}
