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

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Flip(receiver);
            }
        }

        void Flip(GravityReceiver receiver)
        {
            receiver.Flip();
            Debug.Log($"GravityDebug: gravity → {receiver.Side}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            GravityReceiver receiver = ActiveReceiver;
            if (receiver == null) return;

            const float buttonSize = 80f;
            const float margin = 16f;

            var flipRect = new Rect(Screen.width - margin - buttonSize, margin, buttonSize, buttonSize);

            var style = new GUIStyle(GUI.skin.button) { fontSize = 32 };

            if (GUI.Button(flipRect, "FLIP", style))
            {
                Flip(receiver);
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

            float left = Screen.width - margin - buttonSize;
            return new Rect(left, margin, buttonSize, buttonSize);
        }
    }
}
