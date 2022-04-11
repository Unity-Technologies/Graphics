using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ForceRenderScale : MonoBehaviour
{
    static readonly float s_TargetRenderScale = 0.9f;
    float m_previousRenderScale;

    void OnDisable()
    {
        var pipelineAsset = UniversalRenderPipeline.asset;
        if (pipelineAsset == null)
            return;
        pipelineAsset.renderScale = m_previousRenderScale;
    }

    void OnEnable()
    {
        var pipelineAsset = UniversalRenderPipeline.asset;
        if (pipelineAsset == null)
            return;
        m_previousRenderScale = pipelineAsset.renderScale;
        pipelineAsset.renderScale = s_TargetRenderScale;
    }
}
