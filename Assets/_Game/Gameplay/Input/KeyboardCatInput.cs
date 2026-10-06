using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    public sealed class KeyboardCatInput : MonoBehaviour, ICatCommandSource
    {
        float move;
        float climb;
        GravityReceiver gravityReceiver;
        bool warnedNoGravityFrame;
        bool jumpHeld;
        bool jumpPressedLatched;
        bool interactHeld;
        bool interactPressedLatched;

        void Update()
        {
            var keyboard = Keyboard.current;

            move = 0f;
            climb = 0f;
            jumpHeld = false;
            interactHeld = false;

            if (keyboard != null)
            {
                bool left  = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
                bool up    = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                bool down  = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;

                move = ProjectScreenMove(Axis(left, right));
                climb = Axis(down, up);

                // PAX-087 (D-089) R1: Space is the only jump key; W/Up and S/Down climb.
                jumpPressedLatched |= keyboard.spaceKey.wasPressedThisFrame;
                jumpHeld = keyboard.spaceKey.isPressed;
                interactPressedLatched |= keyboard.fKey.wasPressedThisFrame;
                interactHeld = keyboard.fKey.isPressed;
            }
        }

        /// <summary>PAX-087 (D-089) R1: two opposing keys to a screen axis: -1, 0 or +1, and 0 while both are held.
        /// Move is Axis(left, right) before the gravity projection; Climb is Axis(down, up), screen-up positive.</summary>
        public static float Axis(bool negative, bool positive)
        {
            if (negative && !positive) return -1f;
            if (positive && !negative) return 1f;
            return 0f;
        }

        public void SetGravityFrame(GravityReceiver gravity)
        {
            gravityReceiver = gravity;
        }

        float ProjectScreenMove(float screenMove)
        {
            if (gravityReceiver == null)
            {
                if (!warnedNoGravityFrame)
                {
                    Debug.LogWarning($"KeyboardCatInput on '{gameObject.name}' has no gravity frame bound. Move will read 0 until SetGravityFrame is called.", this);
                    warnedNoGravityFrame = true;
                }
                return 0f;
            }

            Vector2 up = -gravityReceiver.Direction;
            Vector2 catRight = new Vector2(up.y, -up.x);
            return VirtualStick.ToMove(new Vector2(screenMove, 0f), catRight, StickProjection.ScreenRelative);
        }

        public CatCommand Read()
        {
            var cmd = CatCommand.None;
            cmd.Move = move;
            cmd.Climb = climb;
            cmd.JumpPressed = jumpPressedLatched;
            cmd.JumpHeld = jumpHeld;
            cmd.InteractPressed = interactPressedLatched;
            cmd.InteractHeld = interactHeld;

            jumpPressedLatched = false;
            interactPressedLatched = false;

            return cmd;
        }

        public void ResetTransientState()
        {
            move = 0f;
            climb = 0f;
            jumpHeld = false;
            jumpPressedLatched = false;
            interactHeld = false;
            interactPressedLatched = false;
        }
    }
}
