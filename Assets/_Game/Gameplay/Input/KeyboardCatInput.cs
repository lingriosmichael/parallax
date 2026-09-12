using Parallax.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    public sealed class KeyboardCatInput : MonoBehaviour, ICatCommandSource
    {
        float move;
        bool jumpHeld;
        bool jumpPressedLatched;

        void Update()
        {
            var keyboard = Keyboard.current;

            move = 0f;
            jumpHeld = false;

            if (keyboard != null)
            {
                bool left  = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

                if (left && !right) move = -1f;
                else if (right && !left) move = 1f;

                jumpPressedLatched |= keyboard.spaceKey.wasPressedThisFrame
                                      || keyboard.wKey.wasPressedThisFrame
                                      || keyboard.upArrowKey.wasPressedThisFrame;

                jumpHeld = keyboard.spaceKey.isPressed
                           || keyboard.wKey.isPressed
                           || keyboard.upArrowKey.isPressed;
            }
        }

        public CatCommand Read()
        {
            var cmd = CatCommand.None;
            cmd.Move = move;
            cmd.JumpPressed = jumpPressedLatched;
            cmd.JumpHeld = jumpHeld;

            jumpPressedLatched = false;

            return cmd;
        }
    }
}
