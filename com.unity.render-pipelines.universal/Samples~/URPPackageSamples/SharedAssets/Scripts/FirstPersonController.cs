using UnityEngine;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] 
    private float m_MouseSensitivity = 100f;
    [SerializeField]
    private float m_ButtonSensitivity = 100f;
    [SerializeField] 
    private float m_MovementSpeed = 5f;
    [SerializeField] 
    private Transform m_PlayerCamera = null;
    [SerializeField]
    private bool m_MoveWithMouse = true;

    private CharacterController m_CharacterController;
    private float m_XRotation = 0f;
    [SerializeField]
    private byte m_ButtonMovementFlags;
    
    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        Debug.Log("The FirstPersonController uses the legacy input system. Please set it in Project Settings");
        m_MoveWithMouse = false;
#endif
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            m_MoveWithMouse = false;
        }

        if (m_MoveWithMouse)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        m_CharacterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        Vector2 lookInput = GetLookInput();

        m_XRotation -= lookInput.y;
        m_XRotation = Mathf.Clamp(m_XRotation, -90f, 90f);
        
        m_PlayerCamera.localRotation = Quaternion.Euler(m_XRotation, 0, 0);
        transform.Rotate(Vector3.up * lookInput.x, Space.World);
    }

    private void Move()
    {
        Vector3 movementInput = GetMovementInput();

        Vector3 move = transform.right * movementInput.x + transform.forward * movementInput.z;

        m_CharacterController.Move(move * m_MovementSpeed * Time.deltaTime);
    }

    private Vector2 GetLookInput()
    {
        float mouseX = 0;
        float mouseY = 0;
        if (m_MoveWithMouse)
        {
            mouseX = Input.GetAxis("Mouse X") * m_MouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * m_MouseSensitivity * Time.deltaTime;
        }
        else
        {
            mouseY = (((m_ButtonMovementFlags & 16) >> 4) - ((m_ButtonMovementFlags & 32) >> 5)) * m_ButtonSensitivity * Time.deltaTime;
            mouseX = (((m_ButtonMovementFlags & 64) >> 6) - ((m_ButtonMovementFlags & 128) >> 7)) * m_ButtonSensitivity * Time.deltaTime;
        }
        return new Vector2(mouseX,mouseY);
    }

    private Vector3 GetMovementInput()
    {
        float x = 0;
        float z = 0;
        if (m_MoveWithMouse)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
        }
        else
        {
            z = (m_ButtonMovementFlags & 1) - ((m_ButtonMovementFlags & 2) >> 1);
            x = ((m_ButtonMovementFlags & 4) >> 2) - ((m_ButtonMovementFlags & 8) >> 3);
        }
        
        return new Vector3(x,0, z);
    }

    #region ControllerToggles
    public void ToggleWalkForward()
    {
        m_ButtonMovementFlags ^= 1;
    }

    public void ToggleWalkBackwards()
    {
        m_ButtonMovementFlags ^= 2;
    }
    
    public void ToggleWalkRight()
    {
        m_ButtonMovementFlags ^= 4;
    }

    public void ToggleWalkLeft()
    {
        m_ButtonMovementFlags ^= 8;
    }

    public void ToggleAimUp()
    {
        m_ButtonMovementFlags ^= 16;
    }

    public void ToggleAimDown()
    {
        m_ButtonMovementFlags ^= 32;
    }

    public void ToggleAimRight()
    {
        m_ButtonMovementFlags ^= 64;
    }

    public void ToggleAimLeft()
    {
        m_ButtonMovementFlags ^= 128;
    }
    #endregion
}
