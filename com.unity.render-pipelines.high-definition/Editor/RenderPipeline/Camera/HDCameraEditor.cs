using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDCameraExtension))]
    [CanEditMultipleObjects] //TODO: check this
    partial class HDCameraEditor : CameraEditor.ExtensionEditor
    {
        SerializedHDCamera m_SerializedCamera;

        RenderTexture m_PreviewTexture;
        Camera m_PreviewCamera;
        HDCameraExtension m_PreviewAdditionalCameraData;

        public Camera[] targets => cameraTargets; //Needed for some UI drawer

        protected override void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(settings, serializedExtension);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            m_PreviewCamera.enabled = false;
            m_PreviewCamera.cameraType = CameraType.Preview; // Must be init before adding HDAdditionalCameraData
            m_PreviewAdditionalCameraData = m_PreviewCamera.CreateExtension<HDCameraExtension>();
            m_PreviewCamera.SwitchActiveExtensionTo<HDCameraExtension>();
            // Say that we are a camera editor preview and not just a regular preview
            m_PreviewAdditionalCameraData.isEditorCameraPreview = true;
        }

        protected override void OnDisable()
        {
            if (m_PreviewTexture != null)
            {
                m_PreviewTexture.Release();
                m_PreviewTexture = null;
            }
            GameObject.DestroyImmediate(m_PreviewCamera.gameObject);
            m_PreviewCamera = null;
        }

        protected override void OnInspectorGUI()
        {
            m_SerializedCamera.Update();

            HDCameraUI.Inspector.Draw(m_SerializedCamera, this);

            m_SerializedCamera.Apply();
        }

        RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                m_PreviewTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_PreviewTexture.enableRandomWrite = true;
                m_PreviewTexture.Create();
            }
            return m_PreviewTexture;
        }
    }
}
