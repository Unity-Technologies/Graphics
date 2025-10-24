using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float lookSensitivity = 0.5f;

    [Header("Camera Reference")]
    public Camera baseCamera;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        var actions = playerInput.actions;

        moveAction = actions["Move"];
        lookAction = actions["Look"];
        
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;

        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => lookInput = Vector2.zero;

        actions.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled -= ctx => moveInput = Vector2.zero;
        }
        if (lookAction != null)
        {
            lookAction.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
            lookAction.canceled -= ctx => lookInput = Vector2.zero;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        cameraPitch -= lookInput.y * lookSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        baseCamera.transform.localEulerAngles = Vector3.right * cameraPitch;
    }
}