#if (ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_INSTALLED)
#define USE_INPUT_SYSTEM
#endif

#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
	
    public Transform modelTransform;
    public Transform foamGeneratorParent;
	
    public float rotationSpeed = 5f;

    public float speed = 12f;
    public float gravity = -10f;
    public float jumpHeight = 2f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;


    Vector3 velocity;
    bool isGrounded;

#if USE_INPUT_SYSTEM
    InputAction movement;
    InputAction jump;

    void Start()
    {
        movement = new InputAction("PlayerMovement", binding: "<Gamepad>/leftStick");
        movement.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        jump = new InputAction("PlayerJump", binding: "<Gamepad>/a");
        jump.AddBinding("<Keyboard>/space");

        movement.Enable();
        jump.Enable();
    }

#endif

    // Update is called once per frame
    void Update()
    {
        float x;
        float z;
        bool jumpPressed = false;

#if USE_INPUT_SYSTEM
        var delta = movement.ReadValue<Vector2>();
        x = -delta.x;
        z = -delta.y;
        jumpPressed = Mathf.Approximately(jump.ReadValue<float>(), 1);
#else
        x = -Input.GetAxis("Horizontal");
        z = -Input.GetAxis("Vertical");
        jumpPressed = Input.GetButtonDown("Jump");
#endif

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
		
        Vector3 move = transform.right * x + transform.forward * z;
		move = Vector3.Normalize(move);

        controller.Move(move * speed * Time.deltaTime);

        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
		
		// Rotate controller based on where it's going. 
		if (move != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
			modelTransform.rotation = Quaternion.RotateTowards(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}
		
		foamGeneratorParent.localScale = Vector3.one * move.magnitude;

    }
}
