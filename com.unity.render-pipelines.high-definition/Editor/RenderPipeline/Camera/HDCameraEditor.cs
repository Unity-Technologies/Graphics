using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDCamera))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        SerializedHDCamera m_SerializedCamera;

        RenderTexture m_PreviewTexture;
        HDCamera m_PreviewCamera;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(HDCamera)).GetComponent<HDCamera>();
            m_PreviewCamera.enabled = false;
            m_PreviewCamera.cameraType = CameraType.Preview;
            // Say that we are a camera editor preview and not just a regular preview
            m_PreviewCamera.isEditorCameraPreview = true;
        }

        void OnDisable()
        {
            if (m_PreviewTexture != null)
            {
                m_PreviewTexture.Release();
                m_PreviewTexture = null;
            }
            DestroyImmediate(m_PreviewCamera.gameObject);
            m_PreviewCamera = null;
        }

        public override void OnInspectorGUI()
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
