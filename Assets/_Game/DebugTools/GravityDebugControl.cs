using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.DebugTools
{
    public sealed class GravityDebugControl : MonoBehaviour, ITouchReservedRegion
    {
        [SerializeField] ObserverSet observers;
        [SerializeField] SoloSwitchController switchController;

        GravityReceiver ActiveReceiver
        {
            get
            {
                if (observers == null || switchController == null) return null;
                ObserverContext active = observers.Get(switchController.Active);
                return active != null ? active.Gravity : null;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            GravityReceiver receiver = ActiveReceiver;
            if (receiver == null) return;

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

            Rotate(receiver, dir.Value);
        }

        void Rotate(GravityReceiver receiver, Vector2 dir)
        {
            receiver.SetTargetDirection(dir);
            Debug.Log($"GravityDebug: gravity → ({dir.x:0}, {dir.y:0})");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            GravityReceiver receiver = ActiveReceiver;
            if (receiver == null) return;

            const float buttonSize = 80f;
            const float margin = 16f;

            var ccwRect = new Rect(Screen.width - margin - buttonSize * 2f - margin, margin, buttonSize, buttonSize);
            var cwRect = new Rect(Screen.width - margin - buttonSize, margin, buttonSize, buttonSize);

            var style = new GUIStyle(GUI.skin.button) { fontSize = 32 };

            if (GUI.Button(ccwRect, "↺", style))
            {
                Rotate(receiver, GravityMath.Rotate90(receiver.TargetDirection, +1));
            }

            if (GUI.Button(cwRect, "↻", style))
            {
                Rotate(receiver, GravityMath.Rotate90(receiver.TargetDirection, -1));
            }
        }
#endif

        public bool ContainsScreenPoint(Vector2 screenPos)
        {
            Vector2 guiPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);
            return ButtonsRect().Contains(guiPoint);
        }

        static Rect ButtonsRect()
        {
            const float buttonSize = 80f;
            const float margin = 16f;

            float left = Screen.width - margin - buttonSize * 2f - margin;
            return new Rect(left, margin, buttonSize * 2f + margin, buttonSize);
        }
    }
}
