using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        SerializedHDCamera m_SerializedCamera;
        HDCameraUI m_UIState = new HDCameraUI();

        RenderTexture m_PreviewTexture;
        Camera m_PreviewCamera;
        HDAdditionalCameraData m_PreviewAdditionalCameraData;
        PostProcessLayer m_PreviewPostProcessLayer;
        HDCamera m_PreviewHDCamera;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);
            m_UIState.Reset(m_SerializedCamera, Repaint);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            m_PreviewCamera.enabled = false;
            m_PreviewCamera.cameraType = CameraType.Preview; // Must be init before adding HDAdditionalCameraData
            m_PreviewAdditionalCameraData = m_PreviewCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            m_PreviewPostProcessLayer = m_PreviewCamera.gameObject.AddComponent<PostProcessLayer>();
            m_PreviewHDCamera = new HDCamera(m_PreviewCamera);
            m_PreviewHDCamera.Update(m_PreviewPostProcessLayer, m_PreviewAdditionalCameraData.GetFrameSettings());
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
            var s = m_UIState;
            var d = m_SerializedCamera;

            d.Update();
            s.Update();

            HDCameraUI.Inspector.Draw(s, d, this);

            d.Apply();
        }

        RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                var baseDesc = m_PreviewHDCamera.renderTextureDesc;
                baseDesc.width = width;
                baseDesc.height = height;
                CoreUtils.UpdateRenderTextureDescriptor(ref baseDesc, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1, true);
                m_PreviewTexture = new RenderTexture(baseDesc);
            }
            return m_PreviewTexture;
        }
    }
}
