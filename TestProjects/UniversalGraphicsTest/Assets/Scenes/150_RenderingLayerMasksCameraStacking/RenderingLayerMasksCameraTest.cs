using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RenderingLayerMasksCameraTest : MonoBehaviour
{
    [System.Serializable]
    public struct CameraRenderingLayerMaskData
    {
        [SerializeField] public ForwardRendererData forwardRendererData;
        [SerializeField] public uint renderingLayerMask;
        [HideInInspector] public uint prevRenderingLayerMask;
    }

    [SerializeField] public CameraRenderingLayerMaskData m_BaseCamera;
    [SerializeField] public CameraRenderingLayerMaskData m_OverlayCamera;


    void Awake()
    {
        SetupCameraData(ref m_BaseCamera);
        SetupCameraData(ref m_OverlayCamera);
    }

    void OnDestroy()
    {
        m_BaseCamera.forwardRendererData.opaqueRenderingLayerMask = m_BaseCamera.prevRenderingLayerMask;
        m_OverlayCamera.forwardRendererData.opaqueRenderingLayerMask = m_OverlayCamera.prevRenderingLayerMask;
    }

    void SetupCameraData(ref CameraRenderingLayerMaskData camData)
    {
        camData.prevRenderingLayerMask = camData.forwardRendererData.opaqueRenderingLayerMask;
        camData.forwardRendererData.opaqueRenderingLayerMask = camData.renderingLayerMask;
    }
}
