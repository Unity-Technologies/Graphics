using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDCameraEditor
    {
        public class UIState
        {
            SerializedHDCamera m_SerializedHdCamera;

            AnimBool[] m_AnimBools = new AnimBool[7];

            public AnimBool isSectionExpandedBGColorOptions { get { return m_AnimBools[0]; } }
            public AnimBool isSectionExpandedOrthoOptions { get { return m_AnimBools[1]; } }
            public AnimBool isSectionExpandedTargetEyeOptions { get { return m_AnimBools[2]; } }
            public AnimBool isSectionExpandedShaderFeature { get { return m_AnimBools[3]; } }
            public AnimBool isSectionExpandedLightLoop { get { return m_AnimBools[4]; } }
            public AnimBool isSectionExpandedScreenSpace { get { return m_AnimBools[5]; } }
            public AnimBool isSectionExpandedMiscellaneous { get { return m_AnimBools[6]; } }

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
                var clearFlags = (CameraClearFlags)m_SerializedHdCamera.clearFlags.intValue;
                var clearFlagsHasMultipleValues = m_SerializedHdCamera.clearFlags.hasMultipleDifferentValues;
                isSectionExpandedBGColorOptions.target = !clearFlagsHasMultipleValues && (clearFlags == CameraClearFlags.SolidColor || clearFlags == CameraClearFlags.Skybox);

                isSectionExpandedOrthoOptions.target = !m_SerializedHdCamera.orthographic.hasMultipleDifferentValues && m_SerializedHdCamera.orthographic.boolValue;

                var targetEyeValue = (StereoTargetEyeMask)m_SerializedHdCamera.targetEye.intValue;
                isSectionExpandedTargetEyeOptions.target = targetEyeValue != StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported;
            }
        }
    }
}
