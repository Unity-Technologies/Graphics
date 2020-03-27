using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FPSController : MonoBehaviour
{
    public float rotationSpeed = 1;
    public float walkSpeed = 1;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        transform.eulerAngles = Vector3.zero;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Rotate based on mouse input
        float vert = Input.GetAxis("Mouse X") * Time.deltaTime * rotationSpeed * 100;
        float hori = Input.GetAxis("Mouse Y") * Time.deltaTime * rotationSpeed * 100;

        Vector3 rotation = new Vector3(hori, vert, 0);
        transform.Rotate(Vector3.up * vert, Space.World);
        transform.Rotate(-Vector3.right * hori, Space.Self);

        //Move based on wasd input
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward = forward.normalized;

        Vector3 direction = Vector3.zero;

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

}
