using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

public class DebugViewController : MonoBehaviour
{
    [SerializeField] FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;

    [ContextMenu("Set Debug View")]
    public void SetDebugView()
    {
        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        hdPipeline.debugDisplaySettings.fullScreenDebugMode = fullScreenDebugMode;
    }
}
