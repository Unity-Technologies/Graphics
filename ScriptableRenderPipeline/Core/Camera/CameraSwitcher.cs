using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class CameraSwitcher : MonoBehaviour
    {
        public Camera[] m_Cameras;

        private int m_CurrentCameraIndex = -1;
        private Camera m_OriginalCamera = null;
        private Vector3 m_OriginalCameraPosition;
        private Quaternion m_OriginalCameraRotation;
        private Camera m_CurrentCamera = null;

        GUIContent[]    m_CameraNames = null;
        int[]           m_CameraIndices = null;

        void OnEnable()
        {
            m_OriginalCamera = GetComponent<Camera>();
            m_CurrentCamera = m_OriginalCamera;

            if(m_OriginalCamera == null)
            {
                Debug.LogError("Camera Switcher needs a Camera component attached");
                return;
            }

            m_CurrentCameraIndex = GetCameraCount() - 1;

            m_CameraNames = new GUIContent[GetCameraCount()];
            m_CameraIndices = new int[GetCameraCount()];

            for (int i = 0; i < m_Cameras.Length; ++i)
            {
                Camera cam = m_Cameras[i];
                if (cam != null)
                {
                    m_CameraNames[i] = new GUIContent(cam.name);
                }
                else
                {
                    m_CameraNames[i] = new GUIContent("null");
                }
                m_CameraIndices[i] = i;
            }

            m_CameraNames[GetCameraCount() - 1] = new GUIContent("Original Camera");
            m_CameraIndices[GetCameraCount() - 1] = GetCameraCount() - 1;

            DebugMenuManager.instance.AddDebugItem<int>("Camera", "Camera Switcher", () => m_CurrentCameraIndex, (value) => SetCameraIndex((int)value), DebugItemFlag.None, new DebugItemHandlerIntEnum(m_CameraNames, m_CameraIndices));
        }

        int GetCameraCount()
        {
            return m_Cameras.Length + 1; // We need +1 for handling the original camera.
        }

        Camera GetNextCamera()
        {
            if (m_CurrentCameraIndex == m_Cameras.Length)
                return m_OriginalCamera;
            else
                return m_Cameras[m_CurrentCameraIndex];
        }

        void SetCameraIndex(int index)
        {
            if(index > 0 || index < GetCameraCount())
            {
                m_CurrentCameraIndex = index;

                if (m_CurrentCamera == m_OriginalCamera)
                {
                    m_OriginalCameraPosition = m_OriginalCamera.transform.position;
                    m_OriginalCameraRotation = m_OriginalCamera.transform.rotation;
                }

                m_CurrentCamera = GetNextCamera();

                if (m_CurrentCamera != null)
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
        }
    }
}
