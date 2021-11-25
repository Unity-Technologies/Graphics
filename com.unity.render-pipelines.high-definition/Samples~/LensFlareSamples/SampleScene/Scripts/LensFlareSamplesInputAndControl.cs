using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Script that allows easy navigation of provided lens flare assets and ability to add custom lens flare assets for development
public class LensFlareSamplesInputAndControl : MonoBehaviour
{
    [Header("References")]
    public GameObject cameraGameObject;
    public LensFlareComponentSRP lensFlareComponent;
    public Text lensFlareUIText;
    public GameObject[] environments;
    public LensFlareDataSRP[] lensFlares;

    [Header("Light Settings")]
    public GameObject lensFlareLight;
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
    private int flareNumber;
    private Vector3 vectorNoise = Vector3.zero;

    void Start()
    {
        cameraComponent = cameraGameObject.GetComponent<Camera>();
    }

    void Update()
    {
        if (Application.isFocused)
        {
            SetSkyFromInput();
            MoveLightWithMouse();
            ChangeLensFlare();
            CameraMovementWithMouse();
        }
        CameraShake();
    }

    private void SetSkyFromInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetSky(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SetSky(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SetSky(2);
        }
#else
        if (Input.GetKeyDown("1"))
        {
            SetSky(0);
        }
        else if (Input.GetKeyDown("2"))
        {
            SetSky(1);
        }
        else if (Input.GetKeyDown("3"))
        {
            SetSky(2);
        }
#endif
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
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current.leftButton.IsPressed())
        {
            var mousePosition = Mouse.current.position.ReadValue();
            lensFlareLight.transform.position = cameraComponent.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, lightDistance));
        }
#else
        if (Input.GetMouseButton(0))
        {
            lensFlareLight.transform.position = cameraComponent.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, lightDistance));
        }
#endif
    }

    private void ChangeLensFlare()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasReleasedThisFrame)
        {
            IncrementFlare();
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasReleasedThisFrame)
        {
            DecrementFlare();
        }
#else
        if (Input.GetKeyDown("d") || Input.GetKeyDown("left"))
        {
            IncrementFlare();
        }
        else if (Input.GetKeyDown("a") || Input.GetKeyDown("right"))
        {
            DecrementFlare();
        }
#endif
    }

    private void IncrementFlare()
    {
        flareNumber += 1;

        if (flareNumber == lensFlares.Length)
        {
            flareNumber = 0;
        }

        lensFlareComponent.lensFlareData = lensFlares[flareNumber];
        UpdateFlareNameUI();
    }

    private void DecrementFlare()
    {
        flareNumber -= 1;

        if (flareNumber < 0)
        {
            flareNumber = lensFlares.Length - 1;
        }

        lensFlareComponent.lensFlareData = lensFlares[flareNumber];
        UpdateFlareNameUI();
    }

    private void UpdateFlareNameUI()
    {
        // set the flare name in the UI but only include the name information
        lensFlareUIText.text = lensFlares[flareNumber].ToString().Split(char.Parse("("))[0];
    }

    private void CameraMovementWithMouse()
    {
        LockCursorWhileMouseButtonDown();

#if ENABLE_INPUT_SYSTEM
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
#else
        if (Input.GetMouseButton(1))
        {
            var mouseMovement = new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * Time.deltaTime * cameraRotationSpeed * 200.0f;

            if (useMouseDragInsteadOfFPSControl)
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.x, mouseMovement.y * -1f, 0f);
            }
            else
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.x * -1f, mouseMovement.y, 0f);
            }
        }
#endif
    }

    private void LockCursorWhileMouseButtonDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
#else
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
#endif
    }

    private void CameraShake()
    {
        if (enableCameraShake)
        {
            cameraGameObject.transform.localEulerAngles -= vectorNoise;

            vectorNoise = new Vector3(Mathf.PerlinNoise(0, Time.time * cameraShakeSpeed), Mathf.PerlinNoise(1, Time.time * cameraShakeSpeed), Mathf.PerlinNoise(2, Time.time * cameraShakeSpeed)) * cameraShakeAmplitude;

            cameraGameObject.transform.localEulerAngles += vectorNoise;
        }
    }
}
