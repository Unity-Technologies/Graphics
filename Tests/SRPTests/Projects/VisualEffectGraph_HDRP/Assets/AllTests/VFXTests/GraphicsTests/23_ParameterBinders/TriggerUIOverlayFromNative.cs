using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TriggerUIOverlayFromNative : MonoBehaviour
{
    void Start()
    {
        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
    }

    void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Disabling UI Overlay from HDRP, forcing rendering at the end on native side so screen space UI canvas doesn't appear on output test image
        SupportedRenderingFeatures.active.rendersUIOverlay = false;
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
    }
}
