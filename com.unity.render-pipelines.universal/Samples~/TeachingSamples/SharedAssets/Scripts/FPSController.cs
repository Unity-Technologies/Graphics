using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FPSController : MonoBehaviour
{
    [SerializeField]
    private bool m_MoveWithMouse = true;
    [SerializeField]
    private float m_MouseSensitivity = 1f;
    [SerializeField]
    private float m_ButtonSensitivity = 1f;
    [SerializeField]
    private float m_WalkSpeed = 1f;

    private Rigidbody m_RigidBody;
    private byte m_MovementFlags;

    // Start is called before the first frame update
    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        Debug.Log("The FPSController uses the legacy input system. Please set it in Project Settings");
        m_MoveWithMouse = false;
#endif
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            m_MoveWithMouse = false;
        }

        if (m_MoveWithMouse)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        m_RigidBody = GetComponent<Rigidbody>();

        m_MovementFlags = 0;
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (m_MoveWithMouse)
        {
            //Rotate based on mouse input
            float vert = Input.GetAxis("Mouse X") * Time.deltaTime * m_MouseSensitivity * 100f;
            float hori = Input.GetAxis("Mouse Y") * Time.deltaTime * m_MouseSensitivity * 100f;
            transform.Rotate(Vector3.up * vert, Space.World);
            transform.Rotate(-Vector3.right * hori, Space.Self);
        }
#endif
        Vector3 rotation = MovementMaskToRotation() * (Time.deltaTime * m_ButtonSensitivity * 100f);
        transform.Rotate(0f, rotation.y, 0f, Space.World);
        transform.Rotate(rotation.x, 0f, 0f, Space.Self);
    }

    void FixedUpdate()
    {
        //Move based on wasd input
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward = forward.normalized;

        Vector3 right = transform.right;
        right.y = 0;
        right = right.normalized;

        Vector3 direction = MovementMaskToWalkDirection();

#if ENABLE_LEGACY_INPUT_MANAGER
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
            direction -= right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += right;
        }
#endif
        direction = direction.normalized;

        m_RigidBody.AddForce((direction * m_WalkSpeed) - m_RigidBody.velocity, ForceMode.VelocityChange);
    }

    Vector3 MovementMaskToWalkDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward = forward.normalized;
        Vector3 right = transform.right;
        right.y = 0;
        right = right.normalized;
        Vector3 direction = Vector3.zero;

        direction += (m_MovementFlags & 1) * forward; //forward
        direction += ((m_MovementFlags & 2) >> 1) * -forward; //backwards
        direction += ((m_MovementFlags & 4) >> 2) * -right; //left
        direction += ((m_MovementFlags & 8) >> 3) * right; //right

        return direction;
    }

    Vector3 MovementMaskToRotation()
    {
        Vector3 direction = Vector3.zero;

        direction += ((m_MovementFlags & 16) >> 4) * -Vector3.right; //up
        direction += ((m_MovementFlags & 32) >> 5) * Vector3.right; //down
        direction += ((m_MovementFlags & 64) >> 6) * -Vector3.up; //left
        direction += ((m_MovementFlags & 128) >> 7) * Vector3.up; //right

        return direction;
    }

    public void ToggleWalkForward()
    {
        m_MovementFlags ^= 1;
    }

    public void ToggleWalkBackwards()
    {
        m_MovementFlags ^= 2;
    }

    public void ToggleWalkLeft()
    {
        m_MovementFlags ^= 4;
    }

    public void ToggleWalkRight()
    {
        m_MovementFlags ^= 8;
    }

    public void ToggleAimUp()
    {
        m_MovementFlags ^= 16;
    }

    public void ToggleAimDown()
    {
        m_MovementFlags ^= 32;
    }

    public void ToggleAimLeft()
    {
        m_MovementFlags ^= 64;
    }

    public void ToggleAimRight()
    {
        m_MovementFlags ^= 128;
    }
}
