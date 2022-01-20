using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour
{
    public event Action onJumpPressed;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }

    private void Start()
    {
        inputActions.Player.Jump.performed += Jump_performed;
    }

    private void Jump_performed(InputAction.CallbackContext context)
    {
        onJumpPressed?.Invoke();
    }

    public float GetHorizontalInput()
    {
        return inputActions.Player.Horizontal.ReadValue<float>();
    }

    private void OnDestroy()
    {
        inputActions.Player.Jump.performed -= Jump_performed;
    }
}
