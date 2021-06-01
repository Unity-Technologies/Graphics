using UnityEngine.TestTools.Graphics;
using UnityEngine.Rendering;

public class SGInstancedTestSettings : ShaderGraphGraphicsTestSettings
{
    public RenderPipelineAsset SRPBatchingDisabledPipeline;
    private RenderPipelineAsset previousPipeline;

    public override void OnTestBegin()
    {
        if (SRPBatchingDisabledPipeline != null)
        {
            previousPipeline = GraphicsSettings.renderPipelineAsset;
            GraphicsSettings.renderPipelineAsset = SRPBatchingDisabledPipeline;
        }
    }

    public override void OnTestComplete()
    {
        if (previousPipeline != null)
        {
            GraphicsSettings.renderPipelineAsset = previousPipeline;
        }
    }
}
