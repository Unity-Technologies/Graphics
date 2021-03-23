using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LensFlareInput : MonoBehaviour
{
    [Header("References")]

    public GameObject cameraGameObject;
    public GameObject lensFlareLight;

    public GameObject[] skies;

    [Header("Camera Movement")]
    public float cameraRotationSpeed;
    public bool useMouseDragInsteadOfFPSControl;

    [Header("Camera Shake")]
    public bool enableCameraShake;
    [Range(0,10)]
    public float cameraShakeSpeed;
    [Range(0, 1)]
    public float cameraShakeAmplitude;

    private Camera cameraComponent;

    private int skyNumber;

    private Vector2 mousePosition;
    private Vector3 vectorNoise = Vector3.zero;

    void Start()
    {
        cameraComponent = cameraGameObject.GetComponent<Camera>();
    }

    void Update()
    {
        SetSkyFromInput();
        MoveLightWithMouse();
        CameraMovementWithMouse();
        CameraShake();
    }
    private void SetSkyFromInput()
    {
        for (int i = 0; i < skies.Length; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                skyNumber = i;
                SetSky();
            }
        }
    }

    void SetSky()
    {
        if (skyNumber < skies.Length)
        {
            for (int i = 0; i < skies.Length; i++)
            {
                skies[i].SetActive(false);
            }

            skies[skyNumber].SetActive(true);
        }
    }

    private void MoveLightWithMouse()
    {
        if (Input.GetMouseButton(0))
        {
            mousePosition.x = Input.mousePosition.x / Screen.width;
            mousePosition.y = Input.mousePosition.y / Screen.height;

            lensFlareLight.transform.position = cameraComponent.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 100.0f));
        }
    }

    private void CameraMovementWithMouse()
    {
        LockCursorWhileMouseButtonDown(1);

        if (Input.GetMouseButton(1))
        {
            var mouseMovement = new Vector2(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));

            if (useMouseDragInsteadOfFPSControl)
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.x, mouseMovement.y * -1f, 0f);
            }
            else
            {
                cameraGameObject.transform.localEulerAngles += new Vector3(mouseMovement.x * -1f, mouseMovement.y, 0f);
            }
        }
    }

    private void LockCursorWhileMouseButtonDown(int mouseButton)
    {
        if (Input.GetMouseButtonDown(mouseButton))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButtonUp(mouseButton))
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

            vectorNoise = new Vector3(Mathf.PerlinNoise(0, Time.time * cameraShakeSpeed), Mathf.PerlinNoise(1, Time.time * cameraShakeSpeed), Mathf.PerlinNoise(2, Time.time * cameraShakeSpeed)) * cameraShakeAmplitude;

            cameraGameObject.transform.localEulerAngles += vectorNoise;
        }
    }
}
