using UnityEngine;
using UnityEngine.InputSystem;

namespace Flipside
{
    /// <summary>Reads the keyboard and forwards intent to the motor. Keeps input separate from physics.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class KeyboardPlayerInput : MonoBehaviour
    {
        PlayerMotor motor;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                motor.SetMoveInput(0f);
                motor.SetJumpHeld(false);
                return;
            }

            float horizontal = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            motor.SetMoveInput(horizontal);

            if (keyboard.spaceKey.wasPressedThisFrame) motor.RequestJump();
            motor.SetJumpHeld(keyboard.spaceKey.isPressed);

            bool flipPressed = keyboard.wKey.wasPressedThisFrame
                || keyboard.upArrowKey.wasPressedThisFrame
                || keyboard.leftShiftKey.wasPressedThisFrame
                || keyboard.rightShiftKey.wasPressedThisFrame;
            if (flipPressed) motor.RequestFlip();
        }
    }
}
