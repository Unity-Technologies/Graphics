using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FPSController : MonoBehaviour
{
    public bool moveWithMouse = true;
    public float mouseSensitivity = 1;
    public float buttonSensitivity = 1;
    public float walkSpeed = 1;

    private Rigidbody rb;
    private byte movementFlags;

    // Start is called before the first frame update
    void Start()
    {
        if (moveWithMouse)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        rb = GetComponent<Rigidbody>();

        movementFlags = 0;
    }

    private void Update()
    {
        if (moveWithMouse)
        {
            //Rotate based on mouse input
            float vert = Input.GetAxis("Mouse X") * Time.deltaTime * mouseSensitivity * 100;
            float hori = Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSensitivity * 100;
            transform.Rotate(Vector3.up * vert, Space.World);
            transform.Rotate(-Vector3.right * hori, Space.Self);
        }

        Vector3 rotation = MovementMaskToRotation() * (Time.deltaTime * buttonSensitivity * 100);
        transform.Rotate(0, rotation.y, 0, Space.World);
        transform.Rotate(rotation.x, 0, 0, Space.Self);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Move based on wasd input
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward = forward.normalized;

        Vector3 direction = Vector3.zero;


        direction = MovementMaskToWalkDirection();


        if (Input.GetKey(KeyCode.W))
        {
            direction += forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction -= forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction -= transform.right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += transform.right;
        }
        direction = direction.normalized;

        rb.AddForce((direction * walkSpeed) - rb.velocity, ForceMode.VelocityChange);
    }

    private Vector3 MovementMaskToWalkDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward = forward.normalized;
        Vector3 direction = Vector3.zero;

        direction += (movementFlags & 1) * forward; //forward
        direction += ((movementFlags & 2) >> 1) * -forward; //backwards
        direction += ((movementFlags & 4) >> 2) * -transform.right; //left
        direction += ((movementFlags & 8) >> 3) * transform.right; //right

        return direction;
    }

    private Vector3 MovementMaskToRotation()
    {
        Vector3 direction = Vector3.zero;

        direction += ((movementFlags & 16) >> 4) * -Vector3.right; //up
        direction += ((movementFlags & 32) >> 5) * Vector3.right; //down
        direction += ((movementFlags & 64) >> 6) * -Vector3.up; //left
        direction += ((movementFlags & 128) >> 7) * Vector3.up; //right

        return direction;
    }

    public void ToggleWalkForward()
    {
        movementFlags ^= 1;
    }

    public void ToggleWalkBackwards()
    {
        movementFlags ^= 2;
    }

    public void ToggleWalkLeft()
    {
        movementFlags ^= 4;
    }

    public void ToggleWalkRight()
    {
        movementFlags ^= 8;
    }

    public void ToggleAimUp()
    {
        movementFlags ^= 16;
    }

    public void ToggleAimDown()
    {
        movementFlags ^= 32;
    }

    public void ToggleAimLeft()
    {
        movementFlags ^= 64;
    }

    public void ToggleAimRight()
    {
        movementFlags ^= 128;
    }
}
