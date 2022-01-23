using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour
{
    public event Action onJumpPressed;
    public event Action onJumpReleased;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }

    private void Start()
    {
        inputActions.Player.Jump.performed += JumpPerformed;
        inputActions.Player.Jump.canceled += JumpCanceled;
    }

    private void JumpCanceled(InputAction.CallbackContext context)
    {
        onJumpReleased?.Invoke();
    }

    private void JumpPerformed(InputAction.CallbackContext context)
    {
        onJumpPressed?.Invoke();
    }

    public float GetHorizontalInput()
    {
        return inputActions.Player.Horizontal.ReadValue<float>();
    }

    public float GetVerticalInput()
    {
        return inputActions.Player.Vertical.ReadValue<float>();
    }

    public bool IsJumpPressed()
    {
        return inputActions.Player.Jump.inProgress;
    }

    public bool IsGrabPressed()
    {
        return inputActions.Player.WallGrab.inProgress;
    }

    private void OnDestroy()
    {
        inputActions.Player.Jump.performed -= JumpPerformed;
        inputActions.Player.Jump.canceled -= JumpCanceled;
    }
}
