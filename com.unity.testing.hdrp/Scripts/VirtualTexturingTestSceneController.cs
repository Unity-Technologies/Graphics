using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualTexturingTestSceneController : MonoBehaviour
{
    // Scene control isn't activated until a key is pressed.
    private bool activated = false;

    // Rotational directional light
    public GameObject dirLight;
    public bool rotateLight = true;
    public float rotationDegPerS = 45.0f;

    // Camera movement
    private bool cameraMovement = true;
    protected GameObject cameraGO;

    // Rotation
    public float sensitivity = 30.0f;
    public float maxX = 88.0f;
    public float minX = -5.0f;
    private float rotX = 0.0f;
    private float rotY = 0.0f;

    // Zoom
    public float minCameraDist = 1.0f;
    public float maxCameraDist = 15.0f;
    private float currentDist = 5.0f;
    private float scrollSensitivity = 4.5f;

    void Start()
    {
        cameraGO = Camera.main.gameObject;
        currentDist = Vector3.Distance(cameraGO.transform.position, transform.position);
        rotX = cameraGO.transform.eulerAngles.x;
        rotY = cameraGO.transform.eulerAngles.y;
    }

    public void Update()
    {
        // Don't do anything until this has been actived, which is done with any input.
        if (Input.anyKeyDown)
        {
            activated = true;
        }
        else if (!activated)
        {
            return;
        }

        if (Input.GetKeyDown("z"))
        {
            rotateLight = !rotateLight;
            if (dirLight == null) Debug.LogWarning("Warning: dirLight == null");
        }
        if (Input.GetKeyDown("x"))
        {
            cameraMovement = !cameraMovement;
        }
        if (Input.GetKeyDown("t"))
        {
#if ENABLE_VIRTUALTEXTURES
            UnityEngine.Rendering.VirtualTexturing.Debugging.debugTilesEnabled = !UnityEngine.Rendering.VirtualTexturing.Debugging.debugTilesEnabled;
#endif
        }

        if (dirLight != null && rotateLight)
        {
            dirLight.transform.eulerAngles += new Vector3(0.0f, (rotationDegPerS * Time.deltaTime), 0.0f);
        }

        // Camera distance
        currentDist -= Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        if (currentDist < minCameraDist) currentDist = minCameraDist;
        if (currentDist > maxCameraDist) currentDist = maxCameraDist;

        // Camera movement
        if (cameraMovement) MoveCamera();
    }

    private void MoveCamera()
    {
        // Set rotation
        float gainX = Input.GetAxis("Mouse Y");
        float gainY = Input.GetAxis("Mouse X");

        rotX += (gainX * Time.deltaTime * sensitivity);
        rotY -= (gainY * Time.deltaTime * sensitivity);
        if (rotX < minX) rotX = minX;
        if (rotX > maxX) rotX = maxX;
        rotY %= 360.0f;
        if (rotY < 0.0f) rotY += 360.0f;

        cameraGO.transform.eulerAngles = new Vector3(rotX, rotY, 0.0f);

        // Set position
        float sinY = Mathf.Sin(rotY * Mathf.Deg2Rad);
        float sinX = Mathf.Sin(rotX * Mathf.Deg2Rad);
        float cosY = Mathf.Cos(rotY * Mathf.Deg2Rad);
        float cosX = Mathf.Cos(rotX * Mathf.Deg2Rad);
        cameraGO.transform.position =
            GetRotationPointGameObj().transform.position +
            new Vector3(
                (currentDist * -sinY) * cosX,
                (currentDist * sinX),
                (currentDist * -cosY) * cosX);
    }

    protected virtual GameObject GetRotationPointGameObj()
    {
        return gameObject;
    }
}
