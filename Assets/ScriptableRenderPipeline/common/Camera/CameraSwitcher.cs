using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera[] m_Cameras;

    private int m_CurrentCameraIndex = -1;
    private Camera m_OriginalCamera = null;
    private Vector3 m_OriginalCameraPosition;
    private Quaternion m_OriginalCameraRotation;
    private Camera m_CurrentCamera = null;

    private float m_MessageDuration = 1.0f;
    private float m_MessageTimer = 1000.0f;

    private static string kDebugNext = "Debug Next";
    private static string kDebugPrevious = "Debug Previous";
    private string[] m_RequiredInputButtons = { kDebugNext, kDebugPrevious };
    private bool m_Valid = true;

    void OnEnable()
    {
        m_OriginalCamera = GetComponent<Camera>();
        m_CurrentCamera = m_OriginalCamera;

        m_Valid = Debugging.CheckRequiredInputButtonMapping(m_RequiredInputButtons);
    }

    int GetCameraCount()
    {
        return m_Cameras.Length + 1; // We need +1 for handling the original camera.
    }

    void NextCamera()
    {
        m_CurrentCameraIndex = (m_CurrentCameraIndex + 1) % GetCameraCount();
    }

    void PreviousCamera()
    {
        m_CurrentCameraIndex = m_CurrentCameraIndex - 1;
        if (m_CurrentCameraIndex == -1)
            m_CurrentCameraIndex = m_Cameras.Length;
    }

    Camera GetNextCamera()
    {
        if (m_CurrentCameraIndex == m_Cameras.Length)
            return m_OriginalCamera;
        else
            return m_Cameras[m_CurrentCameraIndex];
    }

    void Update()
    {
        if (m_Valid && Debugging.debugControlEnabled && m_OriginalCamera != null)
        {
            m_MessageTimer += Time.deltaTime;
            bool needUpdateCamera = false;
            if (Input.GetButtonDown(kDebugNext))
            {
                NextCamera();
                needUpdateCamera = true;
            }

            if (Input.GetButtonDown(kDebugPrevious))
            {
                PreviousCamera();
                needUpdateCamera = true;
            }

            if (needUpdateCamera)
            {
                m_MessageTimer = 0.0f;

                if(m_CurrentCamera == m_OriginalCamera)
                {
                    m_OriginalCameraPosition = m_OriginalCamera.transform.position;
                    m_OriginalCameraRotation = m_OriginalCamera.transform.rotation;
                }

                m_CurrentCamera = GetNextCamera();

                if(m_CurrentCamera != null)
                {
                    // If we witch back to the original camera, put back the transform in it.
                    if (m_CurrentCamera == m_OriginalCamera)
                    {
                        m_OriginalCamera.transform.position = m_OriginalCameraPosition;
                        m_OriginalCamera.transform.rotation = m_OriginalCameraRotation;
                    }

                    transform.position = m_CurrentCamera.transform.position;
                    transform.rotation = m_CurrentCamera.transform.rotation;
                }
            }

            if (m_MessageTimer < m_MessageDuration)
            {
                string cameraName = m_CurrentCamera != null ? m_CurrentCamera.name : "NULL";
                string message = string.Format("Switching to camera : {0}", cameraName);
                Debugging.PushDebugMessage(message);
            }
        }
    }
}
