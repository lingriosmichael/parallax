using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Parallax.Gameplay.Controls
{
    [RequireComponent(typeof(CatMotor2D))]
    public sealed class KeyboardCatInput : MonoBehaviour
    {
        CatMotor2D motor;

        void Awake()
        {
            motor = GetComponent<CatMotor2D>();
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            float move = 0f;
            if (keyboard != null)
            {
                bool left  = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

                if (left && !right) move = -1f;
                else if (right && !left) move = 1f;
            }

            var cmd = CatCommand.None;
            cmd.Move = move;
            motor.SetCommand(cmd);
        }
    }
}
