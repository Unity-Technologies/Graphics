using UnityEngine;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] 
    private float m_MouseSensitivity = 100f;
    [SerializeField] 
    private float m_MovementSpeed = 5f;
    [SerializeField] 
    private Transform m_PlayerCamera = null;

    private CharacterController m_CharacterController;
    private float m_XRotation = 0f;
    private bool m_MoveWithMouse = true;
    
    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        Debug.Log("The FirstPersonController uses the legacy input system. Please set it in Project Settings");
        m_MoveWithMouse = false;
#endif
        
        Cursor.lockState = CursorLockMode.Locked;
        m_CharacterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (m_MoveWithMouse)
        {
            Look();
        }
        Move();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * m_MouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * m_MouseSensitivity * Time.deltaTime;

        m_XRotation -= mouseY;
        m_XRotation = Mathf.Clamp(m_XRotation, -90f, 90f);
        
        m_PlayerCamera.localRotation = Quaternion.Euler(m_XRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        m_CharacterController.Move(move * m_MovementSpeed * Time.deltaTime);
    }
}
