using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SideScrollerPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpHeight = 2f;
    [SerializeField] float gravity = -20f;

    CharacterController characterController;
    Vector2 moveInput;
    float verticalVelocity;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        ReadKeyboardInput();

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        Vector3 movement = new Vector3(moveInput.x * moveSpeed, verticalVelocity, 0f);
        characterController.Move(movement * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;
    }

    void ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        float horizontalInput = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        moveInput = new Vector2(horizontalInput, 0f);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        moveInput.y = 0f;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
