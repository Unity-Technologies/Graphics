using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode]
public class LensFlareSamplesInputAndControl : MonoBehaviour
{
    [Header("References")]
    public GameObject cameraGameObject;
    public GameObject[] environments;
    public LensFlareDataSRP[] lensFlares;

    [Header("Light Settings")]
    public float lightDistance = 100.0f;

    [Header("Camera Movement")]
    public float cameraRotationSpeed = 1.0f;
    public bool useMouseDragInsteadOfFPSControl;

    [Header("Camera Shake")]
    public bool enableCameraShake;
    [Range(0, 10)]
    public float cameraShakeSpeed = 3.0f;
    [Range(0, 1)]
    public float cameraShakeAmplitude = 0.3f;

    private Camera cameraComponent;
    private LensFlareComponentSRP lensFlareComponent;
    private GameObject lensFlareLight;

    private Vector3 vectorNoise = Vector3.zero;

    void Start()
    {
        cameraComponent = cameraGameObject.GetComponent<Camera>();
    }

    void Update()
    {
        if (Application.isFocused)
        {
            lensFlareLight = this.transform.GetChild(0).gameObject;
            lensFlareComponent = lensFlareLight.GetComponent<LensFlareComponentSRP>();
            SetSkyFromInput();
            MoveLightWithMouse();
            CameraMovementWithMouse();
        }
        CameraShake();
    }

    private void SetSkyFromInput()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            SetSky(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            SetSky(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            SetSky(2);
        }
    }

    void SetSky(int inputNumber)
    {
        if (inputNumber < environments.Length)
        {
            for (int i = 0; i < environments.Length; i++)
            {
                environments[i].SetActive(false);
            }

            environments[inputNumber].SetActive(true);
        }
    }

    private void MoveLightWithMouse()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            var mousePosition = Mouse.current.position.ReadValue();
            lensFlareLight.transform.position = cameraComponent.ScreenToWorldPoint(
                new Vector3(mousePosition.x, mousePosition.y, lightDistance));
        }
    }

    private void CameraMovementWithMouse()
    {
        LockCursorWhileMouseButtonDown();

        if (Mouse.current.rightButton.isPressed)
        {
            var mouseMovement = Mouse.current.delta.ReadValue() * cameraRotationSpeed / 30f;

            if (useMouseDragInsteadOfFPSControl)
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.y, mouseMovement.x * -1f, 0f);
            }
            else
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.y * -1f, mouseMovement.x, 0f);
            }
        }
    }

    private void LockCursorWhileMouseButtonDown()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void CameraShake()
    {
        if (enableCameraShake)
        {
            cameraGameObject.transform.localEulerAngles -= vectorNoise;

            vectorNoise = new Vector3(
                Mathf.PerlinNoise(0, Time.time * cameraShakeSpeed),
                Mathf.PerlinNoise(1, Time.time * cameraShakeSpeed),
                Mathf.PerlinNoise(2, Time.time * cameraShakeSpeed)) * cameraShakeAmplitude;

            cameraGameObject.transform.localEulerAngles += vectorNoise;
        }
    }
}