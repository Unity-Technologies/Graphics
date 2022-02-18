using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
public class TriggerMotionVecView : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        HDRenderPipeline hdrp = UnityEngine.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp != null)
            hdrp.debugDisplaySettings.data.fullScreenDebugMode = FullScreenDebugMode.MotionVectors;
    }
}
