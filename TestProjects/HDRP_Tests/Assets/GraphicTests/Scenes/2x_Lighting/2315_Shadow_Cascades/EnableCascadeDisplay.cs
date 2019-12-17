using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class EnableCascadeDisplay : MonoBehaviour
{
    public void ShowCascades()
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        if (hdrp != null)
        {
            hdrp.debugDisplaySettings.SetDebugLightingMode(DebugLightingMode.VisualizeCascade);
        }
    }

    void OnDestroy()
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp != null)
        {
            hdrp.debugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);
        }
    }
}
