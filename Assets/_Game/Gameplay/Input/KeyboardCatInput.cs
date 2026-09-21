using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Input
{
    public sealed class KeyboardCatInput : MonoBehaviour, ICatCommandSource
    {
        float move;
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
            jumpHeld = false;
            interactHeld = false;

            if (keyboard != null)
            {
                bool left  = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

                float screenMove = 0f;
                if (left && !right) screenMove = -1f;
                else if (right && !left) screenMove = 1f;
                move = ProjectScreenMove(screenMove);

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
