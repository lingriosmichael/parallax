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
        bool interactHeld;
        bool interactPressedLatched;

        void Update()
        {
            var keyboard = Keyboard.current;

            move = 0f;
            jumpHeld = false;
            interactHeld = false;

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
                interactPressedLatched |= keyboard.fKey.wasPressedThisFrame;
                interactHeld = keyboard.fKey.isPressed;
            }
        }

        public CatCommand Read()
        {
            var cmd = CatCommand.None;
            cmd.Move = move;
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
            jumpHeld = false;
            jumpPressedLatched = false;
            interactHeld = false;
            interactPressedLatched = false;
        }
    }
}
