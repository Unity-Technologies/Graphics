using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class ForceDebugMotionVector : MonoBehaviour
{
    bool m_MotionVectorForced;
    void OnEnable()
    {
        m_MotionVectorForced = false;
    }

    void Update()
    {
        if (m_MotionVectorForced)
            return;

        var hdInstance = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdInstance != null)
        {
            var debugDisplaySettings = hdInstance.debugDisplaySettings;
            if (debugDisplaySettings != null)
            {
                debugDisplaySettings.data.fullScreenDebugMode = FullScreenDebugMode.MotionVectors;
                m_MotionVectorForced = true;
            }
        }
    }

    void OnDisable()
    {
        if (!m_MotionVectorForced)
            return;

        var hdInstance = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdInstance != null)
        {
            hdInstance.debugDisplaySettings.data.fullScreenDebugMode = FullScreenDebugMode.None;
        }
    }
}
