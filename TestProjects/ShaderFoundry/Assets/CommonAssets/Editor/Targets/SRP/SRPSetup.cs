using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SRPSetup : MonoBehaviour
{
    private RenderPipelineAsset m_OriginalAsset;
    public RenderPipelineAsset renderPipeline;

    // Use this for initialization
    void Start()
    {
        m_OriginalAsset = GraphicsSettings.renderPipelineAsset;
        if (m_OriginalAsset != renderPipeline)
            GraphicsSettings.renderPipelineAsset = renderPipeline;
    }

    void OnDestroy()
    {
        if (GraphicsSettings.renderPipelineAsset != m_OriginalAsset)
        {
            GraphicsSettings.renderPipelineAsset = m_OriginalAsset;
        }
    }
}
