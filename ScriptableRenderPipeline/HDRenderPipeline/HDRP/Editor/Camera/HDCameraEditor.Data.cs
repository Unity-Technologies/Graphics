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

            AnimBool[] m_AnimBools = new AnimBool[6];

            public AnimBool isSectionExpandedOrthoOptions { get { return m_AnimBools[0]; } }
            public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[1]; } }
            public AnimBool isSectionExpandedOutputSettings { get { return m_AnimBools[2]; } }
            public AnimBool isSectionAvailableRenderLoopSettings { get { return m_AnimBools[3]; } }
            public AnimBool isSectionExpandedXRSettings { get { return m_AnimBools[4]; } }
            public AnimBool isSectionAvailableXRSettings { get { return m_AnimBools[5]; } }

            public bool canOverrideRenderLoopSettings { get; set; }

            public SerializedFrameSettingsUI serializedFrameSettingsUI = new SerializedFrameSettingsUI();

            public UIState()
            {
                for (var i = 0 ; i < m_AnimBools.Length; ++i)
                    m_AnimBools[i] = new AnimBool();

                canOverrideRenderLoopSettings = false;
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
                serializedFrameSettingsUI.Reset(repaint);
            }

            public void Update()
            {
                var renderingPath = (HDAdditionalCameraData.RenderingPath)m_SerializedHdCamera.renderingPath.intValue;
                canOverrideRenderLoopSettings = renderingPath == HDAdditionalCameraData.RenderingPath.Custom;

                isSectionExpandedOrthoOptions.target = !m_SerializedHdCamera.orthographic.hasMultipleDifferentValues && m_SerializedHdCamera.orthographic.boolValue;
                isSectionAvailableXRSettings.target = PlayerSettings.virtualRealitySupported;
                // SRP settings are available only if the rendering path is not the Default one (configured by the SRP asset)
                isSectionAvailableRenderLoopSettings.target = canOverrideRenderLoopSettings;

                serializedFrameSettingsUI.Update();
            }
        }
    }
}
