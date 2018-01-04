using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDCameraEditor
    {
        public class UIState
        {
            SerializedHDCamera m_SerializedHdCamera;

            AnimBool[] m_AnimBools = new AnimBool[8];

            public AnimBool isSectionExpandedOrthoOptions { get { return m_AnimBools[0]; } }
            public AnimBool isSectionExpandedShaderFeature { get { return m_AnimBools[1]; } }
            public AnimBool isSectionExpandedLightLoop { get { return m_AnimBools[2]; } }
            public AnimBool isSectionExpandedScreenSpace { get { return m_AnimBools[3]; } }
            public AnimBool isSectionExpandedMiscellaneous { get { return m_AnimBools[4]; } }
            public AnimBool isSectionExpandedRenderLoopSettings { get { return m_AnimBools[5]; } }
            public AnimBool isSectionExpandedXR { get { return m_AnimBools[6]; } }
            public AnimBool isSectionExpandedXRSupported { get { return m_AnimBools[7]; } }

            public UIState()
            {
                for (var i = 0 ; i < m_AnimBools.Length; ++i)
                    m_AnimBools[i] = new AnimBool();
            }

            public void Reset(SerializedHDCamera serializedHdCamera, UnityAction repaint)
            {
                m_SerializedHdCamera = serializedHdCamera;

                for (var i = 0; i < m_AnimBools.Length; ++i)
                {
                    m_AnimBools[i].valueChanged.RemoveAllListeners();
                    m_AnimBools[i].valueChanged.AddListener(repaint);
                }

                Update();
            }

            public void Update()
            {
                isSectionExpandedOrthoOptions.target = !m_SerializedHdCamera.orthographic.hasMultipleDifferentValues && m_SerializedHdCamera.orthographic.boolValue;
                isSectionExpandedXRSupported.target = PlayerSettings.virtualRealitySupported;

                // SRP settings are available only if the rendering path is not the Default one (configured by the SRP asset)
                var renderingPath = (HDAdditionalCameraData.RenderingPath)m_SerializedHdCamera.renderingPath.intValue;
                isSectionExpandedRenderLoopSettings.target = renderingPath != HDAdditionalCameraData.RenderingPath.Default;
            }
        }
    }
}
