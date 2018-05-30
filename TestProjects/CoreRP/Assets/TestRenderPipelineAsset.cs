using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class TestRenderPipelineAsset : RenderPipelineAsset
{
    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new TestRenderPipeline();
    }
}
