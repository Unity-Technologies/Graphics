using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        enum ProjectionType { Perspective, Orthographic };

        SerializedHDCamera m_SerializedCamera;
        UIState m_UIState = new UIState();

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);
            m_UIState.Reset(m_SerializedCamera, Repaint);
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
    }
}
