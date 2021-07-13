using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class AutoLoadPipelineAsset : MonoBehaviour
{
    [SerializeField]
    private UniversalRenderPipelineAsset m_PipelineAsset;
    private RenderPipelineAsset m_PreviousPipelineAsset;

    void OnEnable()
    {
        if (m_PipelineAsset && GraphicsSettings.renderPipelineAsset != m_PipelineAsset)
        {
            m_PreviousPipelineAsset = GraphicsSettings.renderPipelineAsset;
            //GraphicsSettings.renderPipelineAsset = m_PipelineAsset;
            QualitySettings.renderPipeline = m_PipelineAsset;
        }
    }

    void OnDisable()
    {
        if (m_PreviousPipelineAsset)
        {
            QualitySettings.renderPipeline = m_PreviousPipelineAsset;
        }
    }
}
