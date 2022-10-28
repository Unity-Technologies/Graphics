using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        SerializedHDCamera m_SerializedCamera;

        RenderTexture m_PreviewTexture;
        Camera[] m_PreviewCameras;
        HDAdditionalCameraData[] m_PreviewAdditionalCameraDatas;

        static readonly Type k_SceneViewCameraOverlay = Type.GetType("UnityEditor.SceneViewCameraOverlay,UnityEditor");
        static readonly FieldInfo k_SceneViewCameraOverlay_ForceDisable = k_SceneViewCameraOverlay.GetField("forceDisable", BindingFlags.Static | BindingFlags.NonPublic);

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);

            var targetCount = serializedObject.targetObjects.Length;
            m_PreviewCameras = new Camera[targetCount];
            m_PreviewAdditionalCameraDatas = new HDAdditionalCameraData[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                m_PreviewCameras[i] = EditorUtility.CreateGameObjectWithHideFlags("Preview " + serializedObject.targetObject.name, HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
                m_PreviewCameras[i].enabled = false;
                m_PreviewCameras[i].cameraType = CameraType.Preview; // Must be init before adding HDAdditionalCameraData
                m_PreviewAdditionalCameraDatas[i] = m_PreviewCameras[i].gameObject.AddComponent<HDAdditionalCameraData>();
                // Say that we are a camera editor preview and not just a regular preview
                m_PreviewAdditionalCameraDatas[i].isEditorCameraPreview = true;
            }

            // Disable builtin camera overlay
            k_SceneViewCameraOverlay_ForceDisable.SetValue(null, true);
            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }

        void OnDisable()
        {
            if (m_PreviewTexture != null)
            {
                m_PreviewTexture.Release();
                m_PreviewTexture = null;
            }
            for (int i = 0; i < serializedObject.targetObjects.Length; i++)
                DestroyImmediate(m_PreviewCameras[i].gameObject);
            m_PreviewCameras = null;
            m_PreviewAdditionalCameraDatas = null;

            // Restore builtin camera overlay
            k_SceneViewCameraOverlay_ForceDisable.SetValue(null, false);
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }

        public override void OnInspectorGUI()
        {
            m_SerializedCamera.Update();

            if (HDEditorUtils.IsPresetEditor(this))
            {
                HDCameraUI.PresetInspector.Draw(m_SerializedCamera, this);
            }
            else
            {
                HDCameraUI.Inspector.Draw(m_SerializedCamera, this);
            }

            m_SerializedCamera.Apply();
        }

        RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                m_PreviewTexture = new RenderTexture(width, height, 0, GraphicsFormat.R16G16B16A16_SFloat);
                m_PreviewTexture.enableRandomWrite = true;
                m_PreviewTexture.Create();
            }
            return m_PreviewTexture;
        }
    }
}
