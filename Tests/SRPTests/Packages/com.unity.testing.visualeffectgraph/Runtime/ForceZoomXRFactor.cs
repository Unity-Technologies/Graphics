using System;
using UnityEngine;
using UnityEngine.XR;

namespace Unity.Testing.VisualEffectGraph
{
    public class ForceZoomXRFactor : MonoBehaviour
    {
        public float m_ForceZoomXRFactor = 1.0f;
        float m_OriginalZoomXRFactor;

        void OnEnable()
        {
            m_OriginalZoomXRFactor = XRDisplaySubsystem.activeSubsystemOrStub.fovZoomFactor;
        }

        void Update()
        {
            XRDisplaySubsystem.activeSubsystemOrStub.fovZoomFactor = m_ForceZoomXRFactor;
        }

        void OnDisable()
        {
            XRDisplaySubsystem.activeSubsystemOrStub.fovZoomFactor = m_OriginalZoomXRFactor;
        }
    }
}
