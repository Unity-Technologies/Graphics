using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LensFlareInput : MonoBehaviour
{
    [Header("References")]
    public GameObject cameraGameObject;
    public SRPLensFlareOverride lensFlareComponent;
    public Text lensFlareUIText;

    public GameObject[] skies;
    public SRPLensFlareData[] lensFlares;

    [Header("Light Settings")]
    public GameObject lensFlareLight;
    public float lightDistance;


    [Header("Camera Movement")]
    public float cameraRotationSpeed;
    public bool useMouseDragInsteadOfFPSControl;

    [Header("Camera Shake")]
    public bool enableCameraShake;
    [Range(0, 10)]
    public float cameraShakeSpeed;
    [Range(0, 1)]
    public float cameraShakeAmplitude;

    private Camera cameraComponent;

    private int flareNumber;
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
        ChangeLensFlareWithMiddleMouse();
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

            lensFlareLight.transform.position = cameraComponent.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, lightDistance));
        }
    }

    private void ChangeLensFlareWithMiddleMouse()
    {
        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            flareNumber += 1;

            if (flareNumber == lensFlares.Length)
            {
                flareNumber = 0;
            }

            lensFlareComponent.lensFlareData = lensFlares[flareNumber];
            UpdateFlareNameUI();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            flareNumber -= 1;

            if (flareNumber < 0)
            {
                flareNumber = lensFlares.Length - 1;
            }

            lensFlareComponent.lensFlareData = lensFlares[flareNumber];
            UpdateFlareNameUI();
        }
    }

    private void UpdateFlareNameUI()
    {
        // set the flare name in the UI but only the name 
        lensFlareUIText.text = lensFlares[flareNumber].ToString().Split(char.Parse("("))[0];
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
