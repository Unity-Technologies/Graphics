using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        enum ProjectionType { Perspective, Orthographic };

        SerializedHDCamera m_SerializedCamera;
        UIState m_UIState = new UIState();

        RenderTexture m_PreviewTexture;
        Camera m_PreviewCamera;
        HDAdditionalCameraData m_PreviewAdditionalCameraData;
        PostProcessLayer m_PreviewPostProcessLayer;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);
            m_UIState.Reset(m_SerializedCamera, Repaint);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            m_PreviewAdditionalCameraData = m_PreviewCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            m_PreviewPostProcessLayer = m_PreviewCamera.gameObject.AddComponent<PostProcessLayer>();
            m_PreviewCamera.enabled = false;
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

            k_PrimarySection.Draw(s, d, this);

            d.Apply();
        }

        void SynchronizePreviewCameraWithCamera(Camera c)
        {
            EditorUtility.CopySerialized(c.GetComponent<HDAdditionalCameraData>(), m_PreviewAdditionalCameraData);
            var layer = c.GetComponent<PostProcessLayer>();
            if (layer != null)
            {
                if (m_PreviewPostProcessLayer == null)
                    m_PreviewPostProcessLayer = c.gameObject.AddComponent<PostProcessLayer>();
                EditorUtility.CopySerialized(layer, m_PreviewPostProcessLayer);
            }
            else if (m_PreviewPostProcessLayer != null)
            {
                DestroyImmediate(m_PreviewPostProcessLayer);
                m_PreviewPostProcessLayer = null;
            }
        }
    }
}
